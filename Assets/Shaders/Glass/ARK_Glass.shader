// ARK/Glass — 2.5D 场景用的玻璃材质（Built-in 管线 / 手写前向 Pass）
// 特点：半透明底 + 菲涅尔边缘 + 假环境反射 + 定向反光带 + 命中闪光
// 兼容：透视相机、低多边形卡通风、无 cubemap 依赖
Shader "ARK/Glass"
{
    Properties
    {
        [Header(Base)]
        _Color            ("基础色 RGBA", Color) = (0.55, 0.82, 0.92, 0.13)
        _EdgeColor        ("边缘色 RGBA", Color) = (0.75, 0.95, 1.00, 0.75)
        _FresnelPower     ("菲涅尔锐度", Range(0.5, 8)) = 3.2
        _FresnelStrength  ("菲涅尔强度", Range(0, 3)) = 1.0

        [Header(Lighting)]
        _Gloss            ("高光收缩", Range(1, 128)) = 48
        _SpecIntensity    ("高光强度", Range(0, 4)) = 1.1
        _Reflection       ("环境反射强度", Range(0, 2)) = 0.30
        _SkyColor         ("反射-天", Color) = (0.70, 0.82, 0.92, 1)
        _GroundColor      ("反射-地", Color) = (0.26, 0.28, 0.32, 1)
        _AmbientBoost     ("环境光补偿", Range(0, 2)) = 0.55

        [Header(Highlight Band)]
        _BandColor        ("反光带颜色", Color) = (1, 1, 1, 1)
        _BandStrength     ("反光带强度", Range(0, 2)) = 0.55
        _BandFrequency    ("反光带密度", Range(0.05, 3)) = 0.32
        _BandSharpness    ("反光带锐度", Range(1, 20)) = 3.2
        _BandAngle        ("反光带角度", Range(0, 6.283)) = 0.95
        _BandOffset       ("反光带偏移", Range(0, 10)) = 0

        [Header(Edge Rim)]
        _HalfSize         ("Half Size (XY)", Vector) = (1, 1, 0, 0)
        _EdgeWidth        ("边缘宽度", Range(0.005, 0.5)) = 0.11
        _EdgePower        ("边缘强度", Range(0, 3)) = 1.25

        [Header(Hit Reaction)]
        _Flash            ("命中闪光 0-1", Range(0, 1)) = 0
        _FlashColor       ("闪光颜色", Color) = (1, 1, 1, 1)
        _Tint             ("整体染色（受击偏色）", Color) = (1, 1, 1, 1)

        [Header(Advanced)]
        _AlphaScale       ("整体透明度倍率", Range(0, 3)) = 1
        _DepthFade        ("近相机淡出距离", Range(0, 6)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        LOD 200
        ZWrite Off
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "GLASS_FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float3 worldNormal: TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                float3 viewDir    : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _EdgeColor;
            half   _FresnelPower;
            half   _FresnelStrength;
            half   _Gloss;
            half   _SpecIntensity;
            half   _Reflection;
            fixed4 _SkyColor;
            fixed4 _GroundColor;
            half   _AmbientBoost;
            fixed4 _BandColor;
            half   _BandStrength;
            half   _BandFrequency;
            half   _BandSharpness;
            half   _BandAngle;
            half   _BandOffset;
            float4 _HalfSize;
            half   _EdgeWidth;
            half   _EdgePower;
            half   _Flash;
            fixed4 _FlashColor;
            fixed4 _Tint;
            half   _AlphaScale;
            half   _DepthFade;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos         = UnityObjectToClipPos(v.vertex);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir     = _WorldSpaceCameraPos - o.worldPos;
                o.uv          = v.uv;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(i.viewDir);

                // 正视率与菲涅尔：边缘更不透明、更亮，制造玻璃厚度感
                // 注意用 abs()：玻璃是薄板，正反面法线方向取决于绕序，取绝对值才不会整块拉满
                float ndv  = abs(dot(n, v));
                float fres = pow(1.0 - ndv, _FresnelPower) * _FresnelStrength;

                float3 col = _Color.rgb * _Tint.rgb;
                float  a   = _Color.a * _Tint.a;

                // 菲涅尔边缘
                col = lerp(col, _EdgeColor.rgb * _Tint.rgb, saturate(fres));
                a   = saturate(a + fres * _EdgeColor.a);

                // 假环境反射：按反射向量的上下半球做渐变，不依赖 cubemap
                float3 rdir = reflect(-v, n);
                float3 env  = lerp(_GroundColor.rgb, _SkyColor.rgb, saturate(rdir.y * 0.5 + 0.5));
                col += env * _Reflection * (0.35 + fres * 0.9);

                // 方向光高光
                float3 l    = normalize(_WorldSpaceLightPos0.xyz);
                float3 h    = normalize(l + v);
                float  spec = pow(saturate(dot(n, h)), _Gloss) * _SpecIntensity;
                col += spec * _SkyColor.rgb * 0.5;

                // 环境光补偿（避免玻璃在暗处完全发黑）
                col += _Color.rgb * _AmbientBoost;

                // 定向反光带：世界坐标上的斜向宽条纹，模拟窗玻璃上的高光条
                float2 dirBand = float2(cos(_BandAngle), sin(_BandAngle));
                float  coord   = dot(i.worldPos.xy, dirBand) * _BandFrequency + _BandOffset;
                float  f       = frac(coord);
                float  band    = saturate(1.0 - abs(f - 0.5) * 2.0);
                band           = pow(band, _BandSharpness) * _BandStrength;
                col += _BandColor.rgb * band;
                a    = saturate(a + band * 0.3);

                // 边缘高光：UV 是世界尺度下的局部坐标，靠边界的一圈提亮，
                // 让薄板玻璃也有"边缘反光"的体积感（薄棱柱本身侧面太窄看不出来）
                float2 uvN = abs(i.uv) / max(_HalfSize.xy, float2(0.0001, 0.0001));
                float  edgeD = max(uvN.x, uvN.y);
                float  rim   = smoothstep(1.0 - _EdgeWidth, 1.0, edgeD) * _EdgePower;
                col += _EdgeColor.rgb * rim * 0.42;
                a    = saturate(a + rim * 0.32);

                // 命中闪光
                col = lerp(col, _FlashColor.rgb, saturate(_Flash));

                // 贴近相机时淡出，避免糊脸
                float camDist = length(i.viewDir);
                a *= saturate((camDist - _DepthFade * 0.25) / max(_DepthFade, 0.001));
                a  = saturate(a * _AlphaScale);

                return fixed4(col, a);
            }
            ENDCG
        }
    }

    FallBack Off
}
