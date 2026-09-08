Shader "Unlit/WormholeShader"
{
    Properties
    {
        // 向こう側の景色。WormholeView が RenderTexture を流し込む。
        _PortalTex ("Portal Texture (RT)", 2D) = "black" {}

        _Radius ("Disc Radius", Range(0.05, 0.5)) = 0.5

        [Header(Gravitational Lens)]
        // 穴のフチに近いピクセルほど、サンプリング位置を外側へずらす。
        // 向こう側の景色がフチに向かって圧縮され、光が曲げられているように見える。
        _LensStrength ("Lens Strength", Range(-1, 1)) = 0.22
        _LensPower ("Lens Power", Range(0.5, 8)) = 3
        _Swirl ("Swirl", Range(-3, 3)) = 0.35
        _Aberration ("Chromatic Aberration", Range(0, 0.5)) = 0.05

        [Header(Surface Ripple)]
        _RippleStrength ("Ripple Strength", Range(0, 0.2)) = 0.015
        _RippleScale ("Ripple Scale", Range(1, 40)) = 14
        _RippleSpeed ("Ripple Speed", Range(0, 5)) = 0.6

        [Header(Event Horizon)]
        _RimColor ("Rim Color", Color) = (0.35, 0.75, 1, 1)
        _RimWidth ("Rim Width", Range(0, 0.6)) = 0.16
        _RimIntensity ("Rim Intensity", Range(0, 8)) = 2.2
        _RimSharpness ("Rim Sharpness", Range(0.5, 8)) = 2.5

        // 環境によって向こう側の絵が上下反転して見えるときの逃げ道。
        [Toggle(_FLIPPORTALY_ON)] _FlipPortalY ("Flip Portal Texture Vertically", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "WormholeSurface"

            // 表からも裏からも覗けるワームホールにするため両面を描く
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _FLIPPORTALY_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 positionNDC : TEXCOORD1;
            };

            TEXTURE2D(_PortalTex);
            SAMPLER(sampler_PortalTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _PortalTex_ST;
                float _Radius;
                float _LensStrength;
                float _LensPower;
                float _Swirl;
                float _Aberration;
                float _RippleStrength;
                float _RippleScale;
                float _RippleSpeed;
                half4 _RimColor;
                float _RimWidth;
                float _RimIntensity;
                float _RimSharpness;
                float _FlipPortalY;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionNDC = positionInputs.positionNDC;
                output.uv = input.uv;

                return output;
            }

            /// 円盤が画面上で何ピクセルの半径に見えているかを UV の微分から求める。
            /// これを歪み量に掛けることで、遠くて小さく見えるときも歪み方が変わらない。
            float GetDiscPixelRadius(float2 uv)
            {
                float uvPerPixel = max(
                    abs(ddx(uv.x)) + abs(ddy(uv.x)),
                    abs(ddx(uv.y)) + abs(ddy(uv.y)));

                return clamp(_Radius / max(uvPerPixel, 1e-6), 1.0, 4096.0);
            }

            half4 frag (Varyings input) : SV_Target
            {
                // 円盤の中心を原点、フチを 1 とした座標へ直す
                float2 centered = (input.uv - 0.5) / max(_Radius, 1e-4);
                float radius = length(centered);

                // 四角いメッシュから円形の穴だけを残す
                clip(1.0 - radius);

                float2 direction = radius > 1e-5 ? (centered / radius) : float2(0.0, 1.0);
                float falloff = pow(saturate(radius), _LensPower);

                // 中心ほど強くねじる。渦を巻きながら吸い込まれていくように見せる
                float swirl = _Swirl * (1.0 - saturate(radius));
                float sinSwirl;
                float cosSwirl;
                sincos(swirl, sinSwirl, cosSwirl);

                float2 bendDirection = float2(
                    (direction.x * cosSwirl) - (direction.y * sinSwirl),
                    (direction.x * sinSwirl) + (direction.y * cosSwirl));

                float ripple = sin((radius * _RippleScale) - (_Time.y * _RippleSpeed * TWO_PI))
                    * _RippleStrength * saturate(radius);

                float discPixelRadius = GetDiscPixelRadius(input.uv);
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float2 lensOffset = bendDirection * ((_LensStrength * falloff) + ripple) * discPixelRadius * texelSize;
                float2 aberrationOffset = bendDirection * (_Aberration * falloff) * discPixelRadius * texelSize;

                // 向こう側のカメラは覗く側と同じ画角・同じ解像度で描いているので、
                // スクリーン座標をそのまま UV に使えば継ぎ目なくつながる。
                float2 screenUV = input.positionNDC.xy / input.positionNDC.w;
                #ifdef _FLIPPORTALY_ON
                    screenUV.y = 1.0 - screenUV.y;
                #endif

                float2 centerUV = screenUV + lensOffset;

                half3 portalColor;
                portalColor.r = SAMPLE_TEXTURE2D(_PortalTex, sampler_PortalTex, saturate(centerUV + aberrationOffset)).r;
                portalColor.g = SAMPLE_TEXTURE2D(_PortalTex, sampler_PortalTex, saturate(centerUV)).g;
                portalColor.b = SAMPLE_TEXTURE2D(_PortalTex, sampler_PortalTex, saturate(centerUV - aberrationOffset)).b;

                float rim = pow(smoothstep(1.0 - _RimWidth, 1.0, radius), _RimSharpness);

                return half4(portalColor + (_RimColor.rgb * rim * _RimIntensity), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
