Shader "MixVerse/KartGroundCracks"
{
    Properties
    {
        _Age ("Age", Float) = 0
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float _Age;
                float _Seed;
            CBUFFER_END
            float2 Hash(float2 p)
            {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3))) + _Seed) * 43758.5453);
            }
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2 - 1;
                float r = length(p);
                float2 warped = p * 4.5 + float2(sin(p.y * 29), sin(p.x * 23)) * 0.09;
                float2 cell = floor(warped);
                float nearest = 100;
                float second = 100;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 neighbor = cell + float2(x, y);
                    float2 delta = neighbor + Hash(neighbor) - warped;
                    float d = dot(delta, delta);
                    if (d < nearest) { second = nearest; nearest = d; }
                    else second = min(second, d);
                }
                float edge = (sqrt(second) - sqrt(nearest)) * 0.5;
                float angle = atan2(p.y, p.x);
                float spokes = angle * 1.4323945 + sin(r * 21 + _Seed) * 0.09;
                float radialEdge = abs(frac(spokes + 0.5) - 0.5) * r * 0.7;
                edge = min(edge, radialEdge);
                float aa = max(fwidth(edge), 0.002);
                float crack = 1 - smoothstep(0.017, 0.017 + aa, edge);
                float chipped = 1 - smoothstep(0.04, 0.04 + aa, edge);
                float irregularRadius = r + sin(p.x * 19 + sin(p.y * 13)) * 0.035;
                float reveal = 1 - smoothstep(saturate(_Age / 0.28) * 1.08 - 0.1, saturate(_Age / 0.28) * 1.08, irregularRadius);
                float border = 1 - smoothstep(0.68, 1, irregularRadius);
                float scorch = (1 - smoothstep(0.06, 0.72, r)) * 0.6;
                float fade = 1 - smoothstep(6.5, 8, _Age);
                float3 color = lerp(float3(0.12, 0.095, 0.065), float3(0.36, 0.31, 0.24), chipped);
                color = lerp(color, float3(0.012, 0.015, 0.018), crack);
                float alpha = max(scorch, max(crack * 0.96, chipped * 0.7)) * border * reveal * fade;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
