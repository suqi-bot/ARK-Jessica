// 2D 角色基础光照 Shader —— Built-in Render Pipeline
//
// 最朴素的「会反光的精灵」：
//   - 不用法线贴图：法线固定为「面片朝向相机」的平面法线，整张精灵均匀受光；
//   - 只算主方向光的 Lambert（带包裹）+ 环境光（SH 球谐），
//     点光/聚光用一个很低的上限叠一点点（闪光、火堆能染色，但永远不会过曝）；
//   - 输出限制在 LDR（<=1），不会喂给 Bloom 变成"角色在发光"；
//   - 光栅状态与 Sprites/Default 对齐：Transparent 队列、ZWrite Off、Cull Off、
//     预乘输出 + Blend One OneMinusSrcAlpha，和 Spine / 粒子混排不会错。
//
// 注意：_MainTex 必须保留原名 —— SpriteRenderer 绘制时会把当前帧的贴图绑到这个属性；
//       顶点色 i.color 里带的是 SpriteRenderer.color。

Shader "ARK/Sprite Basic Lit"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)

		[Header(Light Response)]
		_Brightness ("整体亮度", Range(0,2)) = 1.0
		_MinBrightness ("最暗保底", Range(0,1)) = 0.45
		_AmbientAmount ("环境光", Range(0,2)) = 0.55
		_KeyLightAmount ("主光", Range(0,2)) = 0.90
		_LightWrap ("光照包裹 (软过渡)", Range(0,1)) = 0.35
		_ShadowTint ("阴影色调", Color) = (0.72,0.78,0.92,1)

		[Header(Local Lights)]
		_LocalLightAmount ("点光/聚光上限", Range(0,1)) = 0.30
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		LOD 100

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		CGINCLUDE
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "AutoLight.cginc"

		sampler2D _MainTex;

		fixed4 _Color;
		fixed4 _ShadowTint;

		half _Brightness;
		half _MinBrightness;
		half _AmbientAmount;
		half _KeyLightAmount;
		half _LightWrap;
		half _LocalLightAmount;

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
			half4 color : COLOR;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			fixed4 color : COLOR;
			float3 worldPos : TEXCOORD1;
			UNITY_SHADOW_COORDS(2)
			UNITY_VERTEX_OUTPUT_STEREO
		};

		v2f vert (appdata v)
		{
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT(v2f, o);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			o.color = v.color * _Color;
			o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

			TRANSFER_SHADOW(o);
			return o;
		}

		// 平面法线：2D 精灵没有厚度，法线就是"朝向相机"
		half3 SpriteNormal(v2f i)
		{
			return normalize(_WorldSpaceCameraPos - i.worldPos);
		}

		// 半兰伯特包裹：背光面不会一下变全黑
		half WrapLit(half ndl)
		{
			return saturate((ndl + _LightWrap) / (1.0 + _LightWrap));
		}

		// 保留色相地把光强压到 0..1：近距离强光只会"变亮颜色"，不会"洗成白色"
		half3 ClampLight(half3 c, out half peak)
		{
			peak = max(max(c.r, c.g), max(c.b, (half)0.0001));
			return c / peak;
		}
		ENDCG

		// ---------------- 主光 + 环境光 ----------------
		Pass
		{
			Name "SPRITE_BASIC_BASE"
			Tags { "LightMode"="ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_fwdbase
			#pragma fragmentoption ARB_precision_hint_fastest

			half4 frag (v2f i) : SV_Target
			{
				half4 tex = tex2D(_MainTex, i.uv);
				half alpha = tex.a * i.color.a;
				clip(alpha - 0.004);

				half3 N = SpriteNormal(i);
				half3 L = normalize(UnityWorldSpaceLightDir(i.worldPos));
				half lit = WrapLit(dot(N, L));

				// 环境光：跟随场景的球谐（阴天/夜里会自然变暗）
				half3 ambient = max(ShadeSH9(half4(N, 1.0)), half3(0, 0, 0)) * _AmbientAmount;

				// 主光：强度封顶在 1，避免 HDR 强光把角色照爆
				half peak;
				half3 hue = ClampLight(_LightColor0.rgb, peak);
				half3 direct = hue * (min(peak, (half)1.0) * lit * _KeyLightAmount);

				half3 tint = lerp(_ShadowTint.rgb, half3(1, 1, 1), lit);
				half3 factor = max(ambient + direct, _MinBrightness) * _Brightness;

				half3 rgb = tex.rgb * i.color.rgb * tint * factor;
				rgb = min(rgb, 1);

				// 与 Sprites-Default 相同的预乘输出（贴图是直通 alpha）
				return half4(rgb * alpha, alpha);
			}
			ENDCG
		}

		// ---------------- 附加光（点光 / 聚光 / 第二盏方向光） ----------------
		Pass
		{
			Name "SPRITE_BASIC_ADD"
			Tags { "LightMode"="ForwardAdd" }

			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_fwdadd
			#pragma fragmentoption ARB_precision_hint_fastest

			half4 frag (v2f i) : SV_Target
			{
				half4 tex = tex2D(_MainTex, i.uv);
				half alpha = tex.a * i.color.a;
				clip(alpha - 0.004);

				half3 N = SpriteNormal(i);
				half3 L = normalize(UnityWorldSpaceLightDir(i.worldPos));

				UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
				half lit = WrapLit(dot(N, L)) * atten;

				// 附加光只做一个"限幅的染色"：最多 _LocalLightAmount 的亮度，
				// 所以枪口闪光/火堆能给角色一点暖色，但不会把角色烧白。
				half peak;
				half3 hue = ClampLight(_LightColor0.rgb, peak);
				half3 add = hue * (min(peak, (half)1.0) * lit * _LocalLightAmount);

				half3 rgb = tex.rgb * i.color.rgb * add;
				rgb = min(rgb, 1);

				return half4(rgb * alpha, 0);
			}
			ENDCG
		}
	}

	Fallback Off
}
