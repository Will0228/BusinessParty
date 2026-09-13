using UnityEngine;

namespace MixVerse
{
    /// <summary>
    /// 水滴 1 個ぶんの波紋。輪が広がりながら Sin 波として振動し、寿命が来ると消える。
    /// </summary>
    public readonly struct WaterRipple
    {
        /// <summary>波紋の中心。x = ローカル X、y = ローカル Z。</summary>
        public Vector2 LocalOrigin { get; }

        /// <summary>発生した時刻(Time.time)。</summary>
        public float StartTime { get; }

        /// <summary>発生してから消えるまでの時間(秒)。</summary>
        public float Life { get; }

        public float Amplitude { get; }
        public float Wavelength { get; }

        /// <summary>波紋が外側へ広がる速さ。</summary>
        public float Speed { get; }

        public WaterRipple(Vector2 localOrigin, float startTime, float life, float amplitude, float wavelength, float speed)
        {
            LocalOrigin = localOrigin;
            StartTime = startTime;
            Life = life;
            Amplitude = amplitude;
            Wavelength = wavelength;
            Speed = speed;
        }
    }

    /// <summary>
    /// 水滴の大きさ・落下速度から波紋 1 個ぶんのパラメータを計算するための設定値。
    /// WaterWaveSurface が自分の SerializeField から組み立てて渡す。
    /// </summary>
    public struct WaterWaveSettings
    {
        public float AmplitudePerRadius;
        public float AmplitudePerSpeed;
        public float MaxAmplitude;
        public float WavelengthPerRadius;
        public bool UseDispersion;
        public float Gravity;
        public float BaseSpeed;
        public float SpeedMultiplier;
        public float LifeBase;
        public float LifePerRadius;
    }

    /// <summary>
    /// 水面に落ちた波紋を最大 RippleMax 個までのリングバッファで持ち、
    /// WaterWaveShaderURP へ配列として渡す。
    ///
    /// 波どうしがぶつかったときの盛り上がり・打ち消し合いは、シェーダー側で各波紋の
    /// Sin 波をそのまま足し合わせるだけで表現される(重ね合わせの原理)ので、
    /// ここでは「いまどの波紋が生きているか」を管理するだけでよい。
    ///
    /// MonoBehaviour に依存しないので、WaterWaveSurface が保持して使う。
    /// </summary>
    public sealed class WaterWaveSimulator
    {
        // WaterWaveShaderURP.shader の RIPPLE_MAX と揃えること。
        public const int RippleMax = 16;

        private static readonly int RippleData0Id = Shader.PropertyToID("_RippleData0");
        private static readonly int RippleData1Id = Shader.PropertyToID("_RippleData1");

        private readonly WaterRipple[] _ripples = new WaterRipple[RippleMax];
        private readonly Vector4[] _data0Buffer = new Vector4[RippleMax];
        private readonly Vector4[] _data1Buffer = new Vector4[RippleMax];

        private int _nextIndex;

        /// <summary>
        /// 水滴 1 個が着水したときの波紋を追加する。
        /// 振幅は水滴の大きさと落下速度から、波長と寿命は大きさから決まる。
        /// </summary>
        public void AddSplash(Vector2 localPosition, float radius, float impactSpeed, in WaterWaveSettings settings)
        {
            radius = Mathf.Max(0f, radius);
            impactSpeed = Mathf.Max(0f, impactSpeed);

            var amplitude = Mathf.Min(
                settings.MaxAmplitude,
                (settings.AmplitudePerRadius * radius) + (settings.AmplitudePerSpeed * impactSpeed));

            var wavelength = Mathf.Max(0.05f, settings.WavelengthPerRadius * radius);

            // 深水波の分散関係(速さは波長が長いほど増す)を使うと、大きな水滴の波ほど速く広がる。
            var speed = settings.UseDispersion
                ? Mathf.Sqrt(settings.Gravity * wavelength / (2f * Mathf.PI)) * settings.SpeedMultiplier
                : settings.BaseSpeed * settings.SpeedMultiplier;

            var life = settings.LifeBase + (settings.LifePerRadius * radius);

            AddRipple(new WaterRipple(
                localPosition,
                Time.time,
                Mathf.Max(0.01f, life),
                amplitude,
                wavelength,
                Mathf.Max(0.01f, speed)));
        }

        /// <summary>
        /// 波紋を直接追加する。古い波紋から上書きするリングバッファなので、
        /// 同時に生きていられるのは最大 RippleMax 個まで。
        /// </summary>
        public void AddRipple(WaterRipple ripple)
        {
            _ripples[_nextIndex] = ripple;
            _nextIndex = (_nextIndex + 1) % RippleMax;
        }

        /// <summary>いま持っている波紋の状態をマテリアルへ書き込む。波紋を追加した直後にだけ呼べばよい。</summary>
        public void Apply(Material material)
        {
            for (var i = 0; i < RippleMax; i++)
            {
                var ripple = _ripples[i];

                _data0Buffer[i] = new Vector4(ripple.LocalOrigin.x, ripple.LocalOrigin.y, ripple.StartTime, ripple.Life);
                _data1Buffer[i] = new Vector4(ripple.Amplitude, ripple.Wavelength, ripple.Speed, 0f);
            }

            material.SetVectorArray(RippleData0Id, _data0Buffer);
            material.SetVectorArray(RippleData1Id, _data1Buffer);
        }
    }
}
