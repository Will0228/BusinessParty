Shader "MixVerse/SwordFractureURP"
{
    Properties
    {
        _BaseColor ("Surface", Color) = (0.15, 0.65, 0.95, 1)
        _InsideColor ("Cut surface", Color) = (0.95, 0.45, 0.16, 1)
        _Progress ("Sword impact", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float3 center : TEXCOORD1;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float exterior : TEXCOORD1;
            };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _InsideColor;
                float _Progress;
            CBUFFER_END
            float3 Rotate(float3 value, float3 axis, float angle)
            {
                float s, c;
                sincos(angle, s, c);
                return value * c + cross(axis, value) * s + axis * dot(axis, value) * (1 - c);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 center = input.center;
                float delay = 0.08 + saturate((center.x - center.y + 2) / 4) * 0.35;
                float t = saturate((_Progress - delay) / (1 - delay));
                float3 random = frac(sin(float3(dot(center, float3(12.9, 78.2, 37.7)), dot(center, float3(39.3, 11.1, 83.2)), dot(center, float3(73.1, 52.7, 19.4)))) * 43758.5453);
                float3 axis = normalize(random + float3(0.1, 0.2, 0.3));
                float3 direction = normalize(center + (random - 0.5) * 0.3 + float3(0, 0.15, 0));
                float3 offset = direction * t * (2.4 + random.x) + float3(t * 0.6, t * 1.1 - t * t * 2.1, 0);
                float angle = t * (random.y - 0.5) * 7;
                float3 position = center + Rotate(input.positionOS.xyz - center, axis, angle) + offset;
                output.positionCS = TransformObjectToHClip(position);
                output.normalWS = TransformObjectToWorldNormal(Rotate(input.normalOS, axis, angle));
                output.exterior = input.color.r;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float light = 0.3 + 0.7 * saturate(dot(normal, normalize(float3(-0.4, 0.8, -0.6))));
                half3 color = lerp(_InsideColor.rgb, _BaseColor.rgb, input.exterior);
                return half4(color * light, 1);
            }
            ENDHLSL
        }
    }
}
