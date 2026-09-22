Shader "MixVerse/ExplosionShaderURP"
{
    Properties
    {
        [Header(Progress)]
        _Progress ("Progress", Range(0, 1)) = 0

        [Header(Fireball Colors)]
        _CoreColor ("Core Color", Color) = (1, 0.95, 0.6, 1)
        _MidColor ("Mid Color", Color) = (1, 0.42, 0.08, 1)
        _EdgeColor ("Edge / Smoke Color", Color) = (0.1, 0.04, 0.03, 1)

        [Header(Turbulence)]
        _NoiseScale ("Noise Scale", Float) = 3.2
        _NoiseSpeed ("Noise Speed", Float) = 1.8
        _RiseSpeed ("Ember Rise Speed", Float) = 0.8
        _DisplaceStrength ("Vertex Displace", Range(0, 0.5)) = 0.2

        [Header(Glow)]
        _RimColor ("Ignition Flash Color", Color) = (1, 0.85, 0.5, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _Intensity ("Emission Intensity", Range(0, 6)) = 2.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "ExplosionForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

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
                float3 normalOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Progress;
                half4 _CoreColor;
                half4 _MidColor;
                half4 _EdgeColor;
                float _NoiseScale;
                float _NoiseSpeed;
                float _RiseSpeed;
                float _DisplaceStrength;
                half4 _RimColor;
                float _RimPower;
                float _Intensity;
            CBUFFER_END

            float Hash(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash(i + float3(0, 0, 0));
                float n100 = Hash(i + float3(1, 0, 0));
                float n010 = Hash(i + float3(0, 1, 0));
                float n110 = Hash(i + float3(1, 1, 0));
                float n001 = Hash(i + float3(0, 0, 1));
                float n101 = Hash(i + float3(1, 0, 1));
                float n011 = Hash(i + float3(0, 1, 1));
                float n111 = Hash(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            // 3 オクターブ重ねただけの簡易 fbm。爆炎のモコモコした乱流を表すのに十分な荒さ
            float FireNoise(float3 p)
            {
                float n = ValueNoise(p) * 0.55;
                n += ValueNoise(p * 2.07 + 11.3) * 0.28;
                n += ValueNoise(p * 4.13 + 27.1) * 0.17;
                return n;
            }

            // 球の法線＝中心からの方向をそのままノイズ座標に使う。UV 不要でシームも出ない
            float3 NoiseCoord(float3 normalOS)
            {
                return normalOS * _NoiseScale + float3(0, -_Time.y * _RiseSpeed, 0) + _Time.y * _NoiseSpeed;
            }

            Varyings vert (Attributes input)
            {
                Varyings output;

                float3 normalOS = normalize(input.normalOS);
                float displace = FireNoise(NoiseCoord(normalOS)) - 0.5;
                // 進行が進むほど大きく暴れさせ、きれいな球から煙のような不定形へ崩す
                float3 positionOS = input.positionOS.xyz + normalOS * displace * _DisplaceStrength * (0.35 + _Progress);

                output.normalOS = normalOS;
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 normalOS = normalize(input.normalOS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDirWS)), _RimPower);

                float turbulence = FireNoise(NoiseCoord(normalOS));

                // Progress が進むほど閾値が上がり、燃え残りの塊がどんどん透けて消えていく
                float burnThreshold = lerp(-0.15, 1.1, _Progress);
                float mask = smoothstep(burnThreshold, burnThreshold + 0.32, turbulence);

                half3 color = lerp(_EdgeColor.rgb, _MidColor.rgb, smoothstep(0.25, 0.65, turbulence));
                color = lerp(color, _CoreColor.rgb, smoothstep(0.6, 0.95, turbulence) * saturate(1.0 - _Progress * 1.2));
                color += _RimColor.rgb * fresnel * saturate(1.0 - _Progress) * 1.5;
                color *= _Intensity;

                float alpha = mask * saturate(1.0 - _Progress * 0.9);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
