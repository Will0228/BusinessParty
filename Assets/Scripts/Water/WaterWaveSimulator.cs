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

        /// <summary>その場で 1 周期(山→谷→山)揺れるたびに振幅が何倍になるか。0.5 なら 1 周期ごとに半分になる。</summary>
        public float DecayPerPeriod { get; }

        public WaterRipple(
            Vector2 localOrigin,
            float startTime,
            float life,
            float amplitude,
            float wavelength,
            float speed,
            float decayPerPeriod)
        {
            LocalOrigin = localOrigin;
            StartTime = startTime;
            Life = life;
            Amplitude = amplitude;
            Wavelength = wavelength;
            Speed = speed;
            DecayPerPeriod = decayPerPeriod;
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

        /// <summary>その場で 1 周期揺れるたびに振幅が何倍になるか。0.5 なら 1 周期ごとに半分になる。</summary>
        public float DecayPerPeriod;
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

        // 振幅がこの割合まで下がったら、実用上ゼロ(見えない)とみなして寿命を打ち切る。
        private const float FadeEpsilon = 0.02f;

        private const float MinLife = 0.2f;
        private const float MaxLife = 30f;

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
            var speed = Mathf.Max(0.01f, settings.UseDispersion
                ? Mathf.Sqrt(settings.Gravity * wavelength / (2f * Mathf.PI)) * settings.SpeedMultiplier
                : settings.BaseSpeed * settings.SpeedMultiplier);

            var decayPerPeriod = Mathf.Clamp(settings.DecayPerPeriod, 0.01f, 0.99f);

            // 寿命は「振幅が FadeEpsilon まで減衰するのに何周期かかるか」から逆算する。
            // 波長が長い(=水滴が大きい)ほど 1 周期が長くなるので、寿命も自然に長くなる。
            var period = wavelength / speed;
            var periodsUntilFade = Mathf.Log(FadeEpsilon) / Mathf.Log(decayPerPeriod);
            var life = Mathf.Clamp(period * periodsUntilFade, MinLife, MaxLife);

            AddRipple(new WaterRipple(
                localPosition,
                Time.time,
                life,
                amplitude,
                wavelength,
                speed,
                decayPerPeriod));
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

        /// <summary>
        /// いま持っている波紋から、ローカル座標 posXZ・時刻 time における水面の高さ(オブジェクト空間のY)を求める。
        /// WaterWaveShaderURP.shader の AccumulateRipple と同じ式を CPU 側でも計算したもの
        /// (浮かぶオブジェクトの追従などに使う)。シェーダー側を直すときはこちらも揃えること。
        /// </summary>
        public float GetHeight(Vector2 localPosition, float time)
        {
            var height = 0f;

            for (var i = 0; i < RippleMax; i++)
            {
                height += EvaluateRippleHeight(_ripples[i], localPosition, time);
            }

            return height;
        }

        private static float EvaluateRippleHeight(in WaterRipple ripple, Vector2 posXZ, float time)
        {
            var amplitude = ripple.Amplitude;
            var age = time - ripple.StartTime;

            if (amplitude <= 0f || age < 0f || age >= ripple.Life)
            {
                return 0f;
            }

            var r = Vector2.Distance(posXZ, ripple.LocalOrigin);

            var wavelength = ripple.Wavelength;
            var speed = ripple.Speed;
            var k = (2f * Mathf.PI) / Mathf.Max(wavelength, 1e-4f);
            var omega = k * speed;

            var front = speed * age;
            var frontFade = 1f - SmoothStep(front - wavelength, front + wavelength, r);

            var spread = 1f / Mathf.Sqrt(Mathf.Max(r, wavelength * 0.25f));

            var decayPerPeriod = Mathf.Max(ripple.DecayPerPeriod, 1e-4f);
            var period = wavelength / Mathf.Max(speed, 1e-4f);
            var timeDecay = Mathf.Pow(decayPerPeriod, age / Mathf.Max(period, 1e-4f));

            var envelope = amplitude * frontFade * spread * timeDecay;

            return envelope * Mathf.Sin((k * r) - (omega * age));
        }

        /// <summary>HLSL の smoothstep(edge0, edge1, x) と同じ式。Mathf.SmoothStep とは引数の意味が違うので自前で持つ。</summary>
        private static float SmoothStep(float edge0, float edge1, float x)
        {
            var t = Mathf.Clamp01((x - edge0) / Mathf.Max(edge1 - edge0, 1e-6f));
            return t * t * (3f - (2f * t));
        }

        /// <summary>いま持っている波紋の状態をマテリアルへ書き込む。波紋を追加した直後にだけ呼べばよい。</summary>
        public void Apply(Material material)
        {
            for (var i = 0; i < RippleMax; i++)
            {
                var ripple = _ripples[i];

                _data0Buffer[i] = new Vector4(ripple.LocalOrigin.x, ripple.LocalOrigin.y, ripple.StartTime, ripple.Life);
                _data1Buffer[i] = new Vector4(ripple.Amplitude, ripple.Wavelength, ripple.Speed, ripple.DecayPerPeriod);
            }

            material.SetVectorArray(RippleData0Id, _data0Buffer);
            material.SetVectorArray(RippleData1Id, _data1Buffer);
        }
    }
}
