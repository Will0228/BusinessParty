Shader "Custom/WaterDropShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Size("Size", Float) = 1
        _T("Time", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #define S(a, b, t) smoothstep(a, b, t)

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _Size;
                float _T; 
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            // ノイズ生成
            float N2to1(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _T;
                half4 color = 0;

                float2 aspect= float2(2, 1);
                float2 uv = IN.uv * _Size * aspect;
                uv.y += t * 0.25;
                float2 gv = frac(uv) - .5;
                float2 id = floor(uv);

                float w = IN.uv.y * 10;
                float x = sin(3*w)*pow(sin(w), 6) * 0.45;
                float y = -sin(t + sin(t + sin(t) * 0.5)) * 0.45;
                y -= (gv.x-x)*(gv.x-x);
                
                float2 dropPos = (gv - float2(x, y)) / aspect;
                float drop = S(0.05, 0.03, length(dropPos));

                float2 trailPos = (gv - float2(x, t * 0.25)) / aspect;
                trailPos.y = (frac(trailPos.y * 8) - 0.5) / 8 ;
                float trail = S(0.03, 0.01, length(trailPos));
                trail *= S(-0.05, 0.05, dropPos.y);
                trail *= S(0.5, y, gv.y);

                color += drop;
                color += trail;
                if (gv.x >= 0.48 || gv.y >= 0.49)
                {
                    color = half4(1, 0, 0, 1);
                }
                color *= 0;
                color.rg = id * 0.1;
                
                return color;
            }
            ENDHLSL
        }
    }
}
