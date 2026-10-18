// 2.5D 角色（Spine / Sprite）法线光照 Shader —— Built-in Render Pipeline
//
// 用途：让 2D 角色真正响应场景里的方向光/点光/聚光，靠法线贴图产生明暗体积感，
//       而不是像 Sprites/Default、Spine/Skeleton 那样永远满亮。
//
// 关键点：
//   1. Spine 生成的 mesh 只有 position / uv / color，没有 normal 和 tangent，
//      所以切线空间在这里由物体坐标轴现场构造（T = +X, B = +Y, N = 叉乘）。
//      对贴在 XY 平面、朝向相机的 2D 角色来说这是正确且零成本的。
//   2. 角色被镜像（scale.x = -1）时叉乘会反向，这里用“法线始终朝向相机”自动纠正。
//   3. 混合沿用 Spine 的预乘 alpha（Blend One OneMinusSrcAlpha），
//      所以 _StraightAlphaInput 是“贴图是直通 alpha，需要就地预乘”的开关。
//
// 材质面板可调：法线强度 / 光照包裹（避免死黑）/ 环境光强度 / 阴影底色 / 边缘光。

Shader "ARK/Spine Normal Mapped Lit"
{
	Properties
	{
		[NoScaleOffset] _MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput ("Straight Alpha Texture", Int) = 0

		[NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Strength", Range(0,3)) = 1.0

		_LightWrap ("Light Wrap (软过渡)", Range(0,1)) = 0.45
		_Brightness ("Brightness (整体曝光)", Range(0,3)) = 1.25
		_AmbientStrength ("Ambient Strength", Range(0,2)) = 1.0
		_ShadowFloor ("Shadow Floor (最暗亮度)", Range(0,1)) = 0.18
		_ShadowTint ("Shadow Tint", Color) = (0.62,0.68,0.85,1)

		_SpecColor ("Specular (A=强度)", Color) = (1,1,1,0.35)
		_Shininess ("Shininess", Range(1,64)) = 24

		_RimColor ("Rim Color", Color) = (1,1,1,0)
		_RimPower ("Rim Power", Range(0.5,8)) = 4.0

		_Cutoff ("Shadow alpha cutoff", Range(0,1)) = 0.1

		[HideInInspector] _StencilRef ("Stencil Reference", Float) = 1.0
		[HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		LOD 200

		Stencil
		{
			Ref [_StencilRef]
			Comp [_StencilComp]
			Pass Keep
		}

		CGINCLUDE
		// 阴影投射 Pass 会自动叠加本 CGINCLUDE，那里有自己的一套 struct/宏，先隔离掉
		#ifndef UNITY_PASS_SHADOWCASTER
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "AutoLight.cginc"

		sampler2D _MainTex;
		float4 _MainTex_ST;
		sampler2D _BumpMap;

		fixed4 _Color;
		fixed4 _ShadowTint;
		fixed4 _RimColor;
		half _Shininess; // _SpecColor 由 Lighting.cginc 提供
		half _BumpScale;
		half _LightWrap;
		half _AmbientStrength;
		half _ShadowFloor;
		half _Brightness;
		half _RimPower;

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
			half4 color : COLOR;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2fNM
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			fixed4 color : COLOR;
			float3 worldPos : TEXCOORD1;
			float3 tangent : TEXCOORD2;
			float3 bitangent : TEXCOORD3;
			float3 normalW : TEXCOORD4;
			UNITY_SHADOW_COORDS(5)
			UNITY_VERTEX_OUTPUT_STEREO
		};

		v2fNM vert (appdata v)
		{
			v2fNM o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT(v2fNM, o);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			o.color = v.color;

			float4 wp = mul(unity_ObjectToWorld, v.vertex);
			o.worldPos = wp.xyz;

			// 2D 角色贴在 XY 平面：切线空间直接取物体轴的 X / Y
			float3 T = mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0));
			float3 B = mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0));
			float3 N = cross(T, B);

			// 镜像（localScale.x = -1）会让 N 翻向背面，用视线方向兜底纠正
			float3 V = _WorldSpaceCameraPos - wp.xyz;
			if (dot(N, V) < 0)
			{
				N = -N;
				T = -T;
			}

			o.tangent = normalize(T);
			o.bitangent = normalize(B);
			o.normalW = normalize(N);

			TRANSFER_SHADOW(o);
			return o;
		}

		// 公共：取法线贴图 → 世界空间法线
		inline half3 WorldNormal(v2fNM i)
		{
			half3 n = UnpackNormal(tex2D(_BumpMap, i.uv));
			n.xy *= _BumpScale;
			n = normalize(n);
			return normalize(i.tangent * n.x + i.bitangent * n.y + i.normalW * n.z);
		}

		// 公共：采样主贴图 + 顶点色，输出已预乘的 albedo
		inline void SampleAlbedo(v2fNM i, out half3 rgb, out half rgba)
		{
			half4 tex = tex2D(_MainTex, i.uv);
		#ifdef _STRAIGHT_ALPHA_INPUT
			tex.rgb *= tex.a;
		#endif
			rgb = tex.rgb * i.color.rgb * _Color.rgb;
			rgba = tex.a * i.color.a * _Color.a;
		}

		inline half3 WrapDiffuse(half ndl)
		{
			return saturate((ndl + _LightWrap) / (1.0 + _LightWrap));
		}
		#endif // !UNITY_PASS_SHADOWCASTER
		ENDCG

		// ---------------- 主光 + 环境光 ----------------
		Pass
		{
			Name "FORWARD_BASE"
			Tags { "LightMode"="ForwardBase" }

			ZWrite Off
			Cull Off
			Blend One OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_fwdbase
			#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
			#pragma fragmentoption ARB_precision_hint_fastest

			half4 frag (v2fNM i) : SV_Target
			{
				half3 albedo; half alpha;
				SampleAlbedo(i, albedo, alpha);

				half3 nW = WorldNormal(i);
				half3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

				UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
				half3 L = normalize(UnityWorldSpaceLightDir(i.worldPos));
				half wrap = WrapDiffuse(dot(nW, L));

				half3 ambient = max(ShadeSH9(half4(nW, 1.0)), half3(0, 0, 0)) * _AmbientStrength;
				half3 lighting = (ambient + _LightColor0.rgb * wrap * atten) * _Brightness;

				half3 rgb = albedo * lighting;
				// 保底亮度，避免背光面糊成纯黑
				rgb = max(rgb, albedo * _ShadowTint.rgb * _ShadowFloor);

				// 高光：让发丝/皮件随光源移动出现亮斑，这是 2D 角色最能读出“受光”的部分
				half3 H = normalize(L + V);
				half spec = pow(saturate(dot(nW, H)), _Shininess) * _SpecColor.a;
				rgb += _SpecColor.rgb * spec * _LightColor0.rgb * saturate(alpha);

				// 边缘光（_RimColor.a = 0 时关闭）
				half rim = pow(saturate(1.0 - saturate(dot(nW, V))), _RimPower) * _RimColor.a;
				rgb += _RimColor.rgb * rim * saturate(alpha);

				return half4(rgb, alpha);
			}
			ENDCG
		}

		// ---------------- 附加光（点光/聚光/第二盏方向光） ----------------
		Pass
		{
			Name "FORWARD_ADD"
			Tags { "LightMode"="ForwardAdd" }

			ZWrite Off
			Cull Off
			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_fwdadd
			#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
			#pragma fragmentoption ARB_precision_hint_fastest

			half4 frag (v2fNM i) : SV_Target
			{
				half3 albedo; half alpha;
				SampleAlbedo(i, albedo, alpha);

				half3 nW = WorldNormal(i);

				UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
				half3 L = normalize(UnityWorldSpaceLightDir(i.worldPos));
				half wrap = WrapDiffuse(dot(nW, L));

				half3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
				half3 H = normalize(L + V);
				half spec = pow(saturate(dot(nW, H)), _Shininess) * _SpecColor.a;
				half3 rgb = albedo * _LightColor0.rgb * wrap * atten * _Brightness;
				rgb += _SpecColor.rgb * spec * _LightColor0.rgb * atten;
				return half4(rgb, 0);
			}
			ENDCG
		}

		// ---------------- 阴影投射 ----------------
		Pass
		{
			Name "Caster"
			Tags { "LightMode"="ShadowCaster" }
			Offset 1, 1

			Fog { Mode Off }
			ZWrite On
			ZTest LEqual
			Cull Off
			Lighting Off

			CGPROGRAM
			#pragma vertex vertShadow
			#pragma fragment fragShadow
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest

			#define SHADOW_CUTOFF _Cutoff
			#include "Assets/Spine/Runtime/spine-unity/Shaders/CGIncludes/Spine-Skeleton-Lit-Common-Shadow.cginc"
			ENDCG
		}
	}

	Fallback Off
}
