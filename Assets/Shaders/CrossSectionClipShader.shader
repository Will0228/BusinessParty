Shader "MixVerse/CrossSectionClipShader"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.78, 0.8, 0.86, 1)

        // 切り取る体積。CrossSectionClipTarget が MaterialPropertyBlock で毎フレーム書き換える。
        // 既定は _ClipEnabled = 0（＝触れていない状態）なので、単体で置けば普通に描かれる。
        _ClipEnabled ("Clip Enabled", Float) = 0
        _ClipShape ("Clip Shape (0:Plane 1:Box 2:Sphere)", Float) = 1
        _ClipInside ("Hide Inside (0 = Hide Outside)", Float) = 1
        _ClipVolumeCenter ("Clip Volume Center", Vector) = (0, 0, 0, 0)
        _ClipVolumeAxisX ("Clip Volume Axis X", Vector) = (1, 0, 0, 0)
        _ClipVolumeAxisY ("Clip Volume Axis Y", Vector) = (0, 1, 0, 0)
        _ClipVolumeAxisZ ("Clip Volume Axis Z", Vector) = (0, 0, 1, 0)
        _ClipVolumeExtents ("Clip Volume Extents", Vector) = (0.5, 0.5, 0.5, 0)

        _SectionColor ("Section Color", Color) = (0, 0.85, 1, 1)
        _SectionWidth ("Section Width", Float) = 0.04
        _SectionPower ("Section Power", Float) = 6
        _CapColor ("Cap Color", Color) = (0, 0.32, 0.7, 1)
        _CapPower ("Cap Power", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
        };

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // SRP Batcher の要求どおり、どのパスでも同じ内容にしておく。
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _ClipEnabled;
            float _ClipShape;
            float _ClipInside;
            float4 _ClipVolumeCenter;
            float4 _ClipVolumeAxisX;
            float4 _ClipVolumeAxisY;
            float4 _ClipVolumeAxisZ;
            float4 _ClipVolumeExtents;
            float4 _SectionColor;
            float _SectionWidth;
            float _SectionPower;
            float4 _CapColor;
            float _CapPower;
        CBUFFER_END

        /// 切り取り体積までの符号付き距離をワールド単位で返す。負が体積の内側。
        ///
        /// 体積の姿勢は正規化した 3 軸で受け取り、大きさは _ClipVolumeExtents（ワールドの半径）で持つ。
        /// 行列を渡して逆変換すると非一様スケールで長さが歪み、_SectionWidth が面ごとに
        /// 太さの違う縁になってしまうため、軸へ射影するだけにして長さをワールドのまま保つ。
        float ClipVolumeDistance(float3 positionWS)
        {
            float3 delta = positionWS - _ClipVolumeCenter.xyz;

            float3 local = float3(
                dot(delta, _ClipVolumeAxisX.xyz),
                dot(delta, _ClipVolumeAxisY.xyz),
                dot(delta, _ClipVolumeAxisZ.xyz));

            float3 extents = max(_ClipVolumeExtents.xyz, 1e-4);

            // Plane: 体積の Y 軸を法線とした無限平面
            if (_ClipShape < 0.5)
            {
                return local.y;
            }

            // Box
            if (_ClipShape < 1.5)
            {
                float3 q = abs(local) - extents;
                return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
            }

            // Sphere（軸ごとに半径が違うときは楕円体の近似距離）
            return (length(local / extents) - 1.0) * min(extents.x, min(extents.y, extents.z));
        }

        /// 消す側の面をフラグメント単位で捨てる。_ClipEnabled が 0 のときは何も捨てない。
        void ClipBySection(float distanceToVolume)
        {
            // _ClipInside = 1 なら体積の内側（距離が負）を消す
            float keepSign = lerp(-1.0, 1.0, step(0.5, _ClipInside));
            clip(lerp(1.0, distanceToVolume * keepSign, step(0.5, _ClipEnabled)));
        }

        /// 断面の縁の光り具合。切り口から _SectionWidth の範囲だけ 0 -> 1 で立ち上がる。
        half3 SectionEmission(float distanceToVolume)
        {
            float edge = 1.0 - saturate(abs(distanceToVolume) / max(_SectionWidth, 1e-4));
            return _SectionColor.rgb * (edge * edge * _SectionPower * step(0.5, _ClipEnabled));
        }

        /// 影の面が真っ黒に潰れないよう半ランバートで陰影を付ける。
        half3 ShadeSurface(half3 albedo, float3 normalWS)
        {
            Light mainLight = GetMainLight();
            half diffuse = saturate(dot(normalWS, mainLight.direction)) * 0.5 + 0.5;
            return albedo * (mainLight.color * diffuse + SampleSH(normalWS));
        }

        /// 断面を塞ぐパスは触れていないあいだ出番がないので、頂点を 1 点に集めて
        /// 面積 0 の三角形にし、フラグメントまで走らせない。
        float4 CollapseUnless(float4 positionHCS, float enabled)
        {
            float keep = step(0.5, enabled);
            return float4(positionHCS.xyz * keep, lerp(1.0, positionHCS.w, keep));
        }

        Varyings Vertex(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionHCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            return output;
        }
        ENDHLSL

        // 表側。触れている体積のなかに入った部分だけを消し、切り口の縁を光らせる。
        Pass
        {
            Name "Surface"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            half4 Fragment(Varyings input) : SV_Target
            {
                // clip() より先に読む。捨てたピクセルが混ざるとミップ選択に使う偏微分が乱れるため。
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float distanceToVolume = ClipVolumeDistance(input.positionWS);
                ClipBySection(distanceToVolume);

                half3 color = ShadeSurface(baseColor.rgb, normalize(input.normalWS));

                return half4(color + SectionEmission(distanceToVolume), baseColor.a);
            }
            ENDHLSL
        }

        // 断面のフタ。表側を消すと中身が空洞に見えてしまうので、
        // 同じ判定を裏面に掛けて開口部を発光色で埋める。
        //
        // URP は LightMode タグ 1 つにつきパスを 1 つしか選ばないため、
        // 前面と同じ UniversalForward は使えない。前面のあとに描かれる SRPDefaultUnlit に置く。
        Pass
        {
            Name "SectionCap"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex CapVertex
            #pragma fragment CapFragment

            Varyings CapVertex(Attributes input)
            {
                Varyings output = Vertex(input);
                output.positionHCS = CollapseUnless(output.positionHCS, _ClipEnabled);
                return output;
            }

            half4 CapFragment(Varyings input) : SV_Target
            {
                float distanceToVolume = ClipVolumeDistance(input.positionWS);
                ClipBySection(distanceToVolume);

                half3 color = _CapColor.rgb * _CapPower + SectionEmission(distanceToVolume);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
