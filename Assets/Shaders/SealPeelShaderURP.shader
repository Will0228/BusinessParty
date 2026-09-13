Shader "Unlit/SealPeelShaderURP"
{
    Properties
    {
        [MainTexture] _MainTex ("Image A (Sticker)", 2D) = "white" {}
        _BottomTex ("Image B (Underneath)", 2D) = "white" {}
        _Progress ("Peel Progress", Range(0, 1)) = 0
        _PeelDirection ("Peel Direction (UV)", Vector) = (1, -1, 0, 0)
        _Aspect ("Width / Height", Float) = 1
        _Padding ("Space Around Sticker", Range(0, 0.5)) = 0.3
        _BackWidth ("Curl Radius", Range(0.01, 0.25)) = 0.075
        _LiftAngle ("Lift Angle", Range(5, 60)) = 22
        _BackColor ("Sticker Back", Color) = (0.6, 0.6, 0.6, 1)
        _ShadowWidth ("Shadow Softness", Range(0.005, 0.15)) = 0.035
        _ShadowPower ("Shadow Strength", Range(0, 1)) = 0.42
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Cull Off
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float2 positionOS : TEXCOORD1;
            };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BottomTex);
            SAMPLER(sampler_BottomTex);
            float _Progress, _Aspect, _Padding, _BackWidth, _LiftAngle, _ShadowWidth, _ShadowPower;
            float4 _PeelDirection, _BackColor, _ClipRect;

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.positionOS = input.positionOS.xy;
                o.uv = input.uv;
                o.color = input.color;
                return o;
            }

            float Coverage(float2 uv, float2 aa)
            {
                float2 edge = min(uv, 1.0 - uv);
                aa = max(aa, 0.00001);
                return saturate(edge.x / aa.x + 0.5) * saturate(edge.y / aa.y + 0.5);
            }

            half4 Sticker(float2 position, float2 scale, float2 aa)
            {
                float2 uv = position / scale;
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv));
                c.a *= Coverage(uv, aa);
                return c;
            }

            half4 Over(half4 below, half4 above)
            {
                return half4(above.rgb * above.a + below.rgb * (1.0 - above.a),
                    above.a + below.a * (1.0 - above.a));
            }

            // Invert the projected cylinder and lifted sheet, retaining the original sticker silhouette.
            float FoldShadow(float2 p, float2 dir, float2 scale, float crease, float radius, float cosine, float softness)
            {
                float x = dot(p, dir);
                float d = crease - x;
                float sourceX;
                if (d >= 0.0)
                    sourceX = crease - radius * (PI - asin(saturate(d / radius)));
                else
                    sourceX = crease - PI * radius - (x - crease) / cosine;
                float silhouette = 1.0 - smoothstep(radius - softness, radius + softness, d);
                return Sticker(p + dir * (sourceX - x), scale, softness / scale).a * silhouette;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 scale = float2(max(_Aspect, 0.01), 1.0);
                float2 uv = (input.uv - 0.5) * (1.0 + 2.0 * _Padding) + 0.5;
                float2 aa = max(fwidth(uv), 0.00001);
                float2 p = uv * scale;
                float2 rawDir = _PeelDirection.xy * scale;
                float2 dir = dot(rawDir, rawDir) > 0.00001 ? normalize(rawDir) : normalize(float2(1, -1));
                float start = dot(min(dir, 0.0), scale);
                float extent = dot(abs(dir), scale);
                float radius = max(_BackWidth, 0.005) * min(scale.x, scale.y);
                float angle = radians(clamp(_LiftAngle, 5.0, 60.0));
                float cosine = cos(angle);
                float crease = start + lerp(-radius, extent * (1.0 + _Padding) + PI * radius, _Progress);
                float x = dot(p, dir);
                float d = crease - x;
                half4 bottom = SAMPLE_TEXTURE2D(_BottomTex, sampler_BottomTex, saturate(uv));
                bottom.a *= Coverage(uv, aa);
                half4 result = half4(bottom.rgb * bottom.a, bottom.a);

                if (_Progress < 1.0)
                {
                    half4 top = Sticker(p, scale, aa);
                    top.a *= step(d, 0.0);
                    result = Over(result, top);

                    float2 shadowOffset = dir * radius * 0.6 + float2(-0.02, 0.028);
                    float2 shadowPoint = p + shadowOffset;
                    float softness = max(_ShadowWidth, 0.001) * min(scale.x, scale.y);
                    float2 perpendicular = float2(-dir.y, dir.x) * softness;
                    float shadow = FoldShadow(shadowPoint, dir, scale, crease, radius, cosine, softness) * 0.4;
                    shadow += FoldShadow(shadowPoint + dir * softness, dir, scale, crease, radius, cosine, softness) * 0.15;
                    shadow += FoldShadow(shadowPoint - dir * softness, dir, scale, crease, radius, cosine, softness) * 0.15;
                    shadow += FoldShadow(shadowPoint + perpendicular, dir, scale, crease, radius, cosine, softness) * 0.15;
                    shadow += FoldShadow(shadowPoint - perpendicular, dir, scale, crease, radius, cosine, softness) * 0.15;
                    result.rgb *= 1.0 - shadow * _ShadowPower;

                    if (d >= 0.0 && d <= radius)
                    {
                        float theta = asin(saturate(d / radius));
                        float sourceX = crease - radius * theta;
                        half4 front = Sticker(p + dir * (sourceX - x), scale, aa);
                        front.rgb *= 0.58 + 0.42 * cos(theta);
                        result = Over(result, front);

                        theta = PI - theta;
                        sourceX = crease - radius * theta;
                        half4 back = Sticker(p + dir * (sourceX - x), scale, aa);
                        float lighting = 0.52 + 0.38 * abs(cos(theta)) + 0.23 * pow(sin(theta), 6.0);
                        back.rgb = _BackColor.rgb * lighting;
                        back.a *= _BackColor.a;
                        result = Over(result, back);
                    }
                    else if (d < 0.0)
                    {
                        float distance = (x - crease) / cosine;
                        float sourceX = crease - PI * radius - distance;
                        half4 back = Sticker(p + dir * (sourceX - x), scale, aa);
                        float height = 2.0 * radius + distance * sin(angle);
                        back.rgb = _BackColor.rgb * (0.90 + 0.12 * saturate(height / (radius * 5.0)));
                        back.a *= _BackColor.a;
                        result = Over(result, back);
                    }
                }

                result.rgb *= input.color.rgb * input.color.a;
                result.a *= input.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                float2 inside = step(_ClipRect.xy, input.positionOS) * step(input.positionOS, _ClipRect.zw);
                result *= inside.x * inside.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDHLSL
        }
    }
}
