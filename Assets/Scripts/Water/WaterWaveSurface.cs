using UnityEngine;

namespace MixVerse
{
    /// <summary>
    /// 水滴が落ちると波紋が広がる 3D の水面。
    ///
    /// 波の形そのものは WaterWaveShaderURP が Sin 波の重ね合わせで計算する。
    /// このコンポーネントは「いつ・どこに・どれくらいの波紋を追加するか」を
    /// WaterWaveSimulator に伝え、その結果をマテリアルへ渡すだけ。
    ///
    /// マテリアルはアセットのまま書き換えると、同じマテリアルを使う他の水面にも
    /// 波紋が飛び火してしまうので複製して使う。
    ///
    /// メッシュは頂点単位で盛り上げるので、波が見えるだけの細かさに分割しておくこと
    /// (Unity 標準の Plane [10x10 分割] 程度でも動くが、細かい波紋を見せたいなら要調整)。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaterWaveSurface : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("WaterWaveShaderURP を使うマテリアル。複製して使うのでアセット自体は汚れない。")]
        [SerializeField] private Material _material;

        [Tooltip("波を表示する Renderer。空なら自分の Renderer を使う。")]
        [SerializeField] private Renderer _renderer;

        [Tooltip("バウンディングボックスを広げるための MeshFilter。空なら自分の MeshFilter を使う。")]
        [SerializeField] private MeshFilter _meshFilter;

        [Header("Ripple From Droplet")]
        [Tooltip("水滴の半径ぶん、波紋の振幅にどれだけ足すか。")]
        [SerializeField] private float _amplitudePerRadius = 0.6f;

        [Tooltip("着水速度ぶん、波紋の振幅にどれだけ足すか。")]
        [SerializeField] private float _amplitudePerSpeed = 0.05f;

        [Tooltip("波紋の振幅の上限。大きすぎる水滴でメッシュが暴れないようにする。")]
        [SerializeField] private float _maxAmplitude = 0.5f;

        [Tooltip("水滴の半径に対して、波紋の波長をどれだけ取るか。大きい水滴ほど波長も長くなる。")]
        [SerializeField] private float _wavelengthPerRadius = 4f;

        [Header("Propagation")]
        [Tooltip("重力から伝わる速さを計算する(波長が長いほど速くなる)。オフなら常に Base Speed を使う。")]
        [SerializeField] private bool _useDispersion = true;

        [Tooltip("伝わる速さの計算に使う重力加速度。")]
        [SerializeField] private float _gravity = 9.8f;

        [Tooltip("Use Dispersion がオフのときに使う、波紋が広がる速さ。")]
        [SerializeField] private float _baseSpeed = 2f;

        [Tooltip("波紋が広がる速さ全体の倍率。演出の速さ調整用。")]
        [SerializeField] private float _speedMultiplier = 1f;

        [Header("Decay")]
        [Tooltip("その場で 1 周期(山→谷→山)揺れるたびに振幅が何倍になるか。0.5 なら毎周期半分に減る。" +
                 "小さいほど早く落ち着き、1 に近いほどいつまでも同じ高さで揺れ続ける。")]
        [Range(0.05f, 0.95f)]
        [SerializeField] private float _decayPerPeriod = 0.5f;

        private WaterWaveSimulator _simulator;
        private Material _materialInstance;

        public float AmplitudePerRadius
        {
            get => _amplitudePerRadius;
            set => _amplitudePerRadius = value;
        }

        public float AmplitudePerSpeed
        {
            get => _amplitudePerSpeed;
            set => _amplitudePerSpeed = value;
        }

        public float MaxAmplitude
        {
            get => _maxAmplitude;
            set => _maxAmplitude = value;
        }

        public float WavelengthPerRadius
        {
            get => _wavelengthPerRadius;
            set => _wavelengthPerRadius = value;
        }

        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            set => _speedMultiplier = value;
        }

        public float DecayPerPeriod
        {
            get => _decayPerPeriod;
            set => _decayPerPeriod = value;
        }

        private void Awake()
        {
            _renderer ??= GetComponent<Renderer>();
            _meshFilter ??= GetComponent<MeshFilter>();

            if (_material == null || _renderer == null)
            {
                Debug.LogError("[MixVerse] WaterWaveSurface にマテリアルか Renderer が設定されていません。", this);
                enabled = false;
                return;
            }

            _simulator = new WaterWaveSimulator();
            _materialInstance = new Material(_material);
            _renderer.sharedMaterial = _materialInstance;

            ExpandBoundsForWaves();
            _simulator.Apply(_materialInstance);
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && _materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }

        /// <summary>
        /// 水滴が着水した地点に波紋を発生させる。
        /// </summary>
        /// <param name="worldPosition">着水したワールド座標。</param>
        /// <param name="radius">水滴の半径。大きいほど波紋も大きく、長く残る。</param>
        /// <param name="impactSpeed">着水した瞬間の速さ。速いほど波紋が高くなる。</param>
        public void Splash(Vector3 worldPosition, float radius, float impactSpeed)
        {
            if (_simulator == null)
            {
                return;
            }

            var localPosition = transform.InverseTransformPoint(worldPosition);

            _simulator.AddSplash(new Vector2(localPosition.x, localPosition.z), radius, impactSpeed, BuildSettings());
            _simulator.Apply(_materialInstance);
        }

        /// <summary>
        /// ワールド座標 worldPosition の x, z における、いまの水面の高さ(ワールドY)を返す。
        /// 波に浮かぶオブジェクトなどが、自分のいる位置の水面に追従するために使う。
        /// </summary>
        public float SampleHeight(Vector3 worldPosition)
        {
            if (_simulator == null)
            {
                return worldPosition.y;
            }

            var local = transform.InverseTransformPoint(worldPosition);
            var height = _simulator.GetHeight(new Vector2(local.x, local.z), Time.time);
            var localSurfacePoint = new Vector3(local.x, height, local.z);

            return transform.TransformPoint(localSurfacePoint).y;
        }

        private WaterWaveSettings BuildSettings()
        {
            return new WaterWaveSettings
            {
                AmplitudePerRadius = _amplitudePerRadius,
                AmplitudePerSpeed = _amplitudePerSpeed,
                MaxAmplitude = _maxAmplitude,
                WavelengthPerRadius = _wavelengthPerRadius,
                UseDispersion = _useDispersion,
                Gravity = _gravity,
                BaseSpeed = _baseSpeed,
                SpeedMultiplier = _speedMultiplier,
                DecayPerPeriod = _decayPerPeriod,
            };
        }

        /// <summary>
        /// 波で盛り上がる分だけ Mesh の Bounds を広げておく。
        /// 広げないと、画面には波が見えるはずの位置でも視錐台カリングで消えることがある。
        /// </summary>
        private void ExpandBoundsForWaves()
        {
            if (_meshFilter == null || _meshFilter.sharedMesh == null)
            {
                return;
            }

            var mesh = _meshFilter.mesh; // ここで初めてインスタンス化されるので、共有メッシュ自体は変えない
            var bounds = mesh.bounds;
            bounds.Expand(new Vector3(0f, _maxAmplitude, 0f));
            mesh.bounds = bounds;
        }
    }
}
