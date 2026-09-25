Shader "MixVerse/KartRainbowBody"
{
    Properties
    {
        [Header(Rainbow)]
        _RainbowScale ("Pattern Scale", Float) = 0.4
        _RainbowSpeed ("Animation Speed", Float) = 2.2
        _Saturation ("Saturation", Range(0, 1)) = 0.95
        _Brightness ("Brightness", Range(0, 4)) = 1.4

        [Header(Rim)]
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _RimBoost ("Rim Boost", Range(0, 4)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "RainbowForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float _RainbowScale;
                float _RainbowSpeed;
                float _Saturation;
                float _Brightness;
                float _RimPower;
                float _RimBoost;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            // 色相だけをRGBへ変換する簡易関数。彩度・明度はfrag側で掛け合わせる
            float3 HueToRGB(float hue)
            {
                return saturate(float3(
                    abs(hue * 6.0 - 3.0) - 1.0,
                    2.0 - abs(hue * 6.0 - 2.0),
                    2.0 - abs(hue * 6.0 - 4.0)));
            }

            half4 frag(Varyings input) : SV_Target
            {
                // オブジェクト座標＋経過時間で色相を流し、車体の各パーツに模様が流れるアニメーションを作る
                float hue = frac((input.positionOS.x + input.positionOS.y * 1.6) * _RainbowScale + _Time.y * _RainbowSpeed);
                float3 rainbow = lerp(float3(1, 1, 1), HueToRGB(hue), _Saturation);

                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDirWS)), _RimPower);

                float3 color = rainbow * _Brightness + rainbow * fresnel * _RimBoost;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
