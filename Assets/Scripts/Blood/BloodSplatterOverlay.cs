using UnityEngine;
using UnityEngine.UI;

namespace MixVerse
{
    /// <summary>
    /// 画面に血しぶきを飛ばし、そのまま垂れていく 2D 演出。
    ///
    /// 血の状態は 1 枚の RenderTexture に持たせている。
    ///   R = 濡れている血の量 / G = 乾いて残った染み / B = 乾き具合 / A = しぶきごとの乱数
    /// しぶきを足すのが BloodStampShaderURP、毎フレーム流すのが BloodFlowShaderURP、
    /// それを血の色に変えて画面へ出すのが BloodDisplayShaderURP。
    ///
    /// マテリアルはアセットのまま書き換えると Play 中に変えた値がそのまま保存されてしまうので、複製して使う。
    /// </summary>
    public sealed class BloodSplatterOverlay : MonoBehaviour
    {
        private static readonly int SplatCenterId = Shader.PropertyToID("_SplatCenter");
        private static readonly int SplatDirectionId = Shader.PropertyToID("_SplatDirection");
        private static readonly int SplatRadiusId = Shader.PropertyToID("_SplatRadius");
        private static readonly int SplatAmountId = Shader.PropertyToID("_SplatAmount");
        private static readonly int SplatSeedId = Shader.PropertyToID("_SplatSeed");
        private static readonly int SplatStretchId = Shader.PropertyToID("_SplatStretch");
        private static readonly int SplatDirectionalityId = Shader.PropertyToID("_SplatDirectionality");
        private static readonly int DropletCountId = Shader.PropertyToID("_DropletCount");
        private static readonly int DropletSpreadId = Shader.PropertyToID("_DropletSpread");
        private static readonly int ImpactStainId = Shader.PropertyToID("_ImpactStain");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        private static readonly int TexelSizeId = Shader.PropertyToID("_TexelSize");
        private static readonly int DeltaTimeId = Shader.PropertyToID("_DeltaTime");
        private static readonly int GravityId = Shader.PropertyToID("_Gravity");
        private static readonly int FilmThicknessId = Shader.PropertyToID("_FilmThickness");
        private static readonly int SurfaceTensionId = Shader.PropertyToID("_SurfaceTension");
        private static readonly int TensionSoftnessId = Shader.PropertyToID("_TensionSoftness");
        private static readonly int DryFrictionId = Shader.PropertyToID("_DryFriction");
        private static readonly int LaneThresholdId = Shader.PropertyToID("_LaneThreshold");
        private static readonly int TrailGainId = Shader.PropertyToID("_TrailGain");
        private static readonly int DryRateId = Shader.PropertyToID("_DryRate");
        private static readonly int DryToStainId = Shader.PropertyToID("_DryToStain");
        private static readonly int AgeRateId = Shader.PropertyToID("_AgeRate");
        private static readonly int WanderId = Shader.PropertyToID("_Wander");
        private static readonly int LaneScaleId = Shader.PropertyToID("_LaneScale");
        private static readonly int WanderScaleId = Shader.PropertyToID("_WanderScale");
        private static readonly int MaxThicknessId = Shader.PropertyToID("_MaxThickness");

        private static readonly int StateTexelSizeId = Shader.PropertyToID("_StateTexelSize");

        [Header("References")]
        [Tooltip("血を映す全画面の RawImage。")]
        [SerializeField] private RawImage _targetImage;

        [Tooltip("BloodStampShaderURP のマテリアル。")]
        [SerializeField] private Material _stampMaterial;

        [Tooltip("BloodFlowShaderURP のマテリアル。")]
        [SerializeField] private Material _flowMaterial;

        [Tooltip("BloodDisplayShaderURP のマテリアル。血の色つやはこちらで調整する。")]
        [SerializeField] private Material _displayMaterial;

        [Header("Resolution")]
        [Tooltip("血の状態テクスチャの縦解像度。横は画面のアスペクト比から決める。")]
        [SerializeField] private int _resolution = 512;

        [Header("Splat")]
        [Tooltip("しぶき 1 発の大きさ。画面の高さに対する割合。")]
        [SerializeField] private float _splatRadius = 0.09f;

        [Tooltip("しぶき 1 発の血の量。膜の厚みを超えたぶんだけ垂れ始める。")]
        [SerializeField] private float _splatAmount = 1.2f;

        [Tooltip("しぶきの大きさと量のばらつき。0 で毎回同じ。")]
        [Range(0f, 1f)]
        [SerializeField] private float _splatVariance = 0.45f;

        [Tooltip("飛んできた向きへの伸び。1 で伸びなし。")]
        [SerializeField] private float _splatStretch = 1.4f;

        [Tooltip("飛沫を進行方向側へ寄せる強さ。0 で全方向へ均等に散る。")]
        [Range(0f, 1f)]
        [SerializeField] private float _splatDirectionality = 0.6f;

        [Tooltip("中心の血だまりのまわりに散る粒の数。")]
        [Range(0, 24)]
        [SerializeField] private int _dropletCount = 14;

        [Tooltip("粒が飛び散る距離。")]
        [SerializeField] private float _dropletSpread = 1.7f;

        [Tooltip("着弾した瞬間にその場へ焼き付く跡の濃さ。垂れ落ちても消えない。")]
        [Range(0f, 1f)]
        [SerializeField] private float _impactStain = 0.75f;

        [Header("Flow")]
        [Tooltip("垂れ落ちる速さ。1 秒あたりに進むテクセル数の目安。")]
        [SerializeField] private float _gravity = 130f;

        [Tooltip("壁に貼り付いたまま動かない膜の厚み。垂れた跡はこれが残ったもの。大きいほど早く垂れ止まる。")]
        [SerializeField] private float _filmThickness = 0.3f;

        [Tooltip("表面張力。膜を超えた血がこの量を上回ると動き出す。")]
        [SerializeField] private float _surfaceTension = 0.35f;

        [Tooltip("動き出すときの効き方。小さいほど、しきい値を境にきっぱり動く。")]
        [SerializeField] private float _tensionSoftness = 0.25f;

        [Tooltip("乾いた面へ進む先端にかかるブレーキ。小さいほど先端に血が溜まって玉になる。")]
        [Range(0f, 1f)]
        [SerializeField] private float _dryFriction = 0.4f;

        [Tooltip("垂れる筋の出やすさ。小さいほど多くの筋が流れ出す。")]
        [Range(0f, 1f)]
        [SerializeField] private float _laneThreshold = 0.55f;

        [Tooltip("通った跡に残す血の濃さ。垂れた筋の濃さ。")]
        [Range(0f, 1f)]
        [SerializeField] private float _trailGain = 0.2f;

        [Tooltip("濡れた血が乾いて動かなくなる速さ。大きいほど早く垂れ止まる。")]
        [SerializeField] private float _dryRate = 0.12f;

        [Tooltip("乾いた血のうち、染みとして残る割合。")]
        [Range(0f, 1f)]
        [SerializeField] private float _dryToStain = 0.6f;

        [Tooltip("色がどす黒くなっていく速さ。")]
        [SerializeField] private float _ageRate = 0.04f;

        [Tooltip("垂れる筋の横揺れ。0 で真下へまっすぐ落ちる。")]
        [SerializeField] private float _wander = 0.4f;

        [Tooltip("流れやすい縦筋の細かさ。大きいほど筋が細くなる。")]
        [SerializeField] private float _laneScale = 70f;

        [Tooltip("横揺れの細かさ。")]
        [SerializeField] private float _wanderScale = 5f;

        [Tooltip("1 ピクセルに溜められる血の量の上限。")]
        [SerializeField] private float _maxThickness = 3f;

        [Tooltip("垂れる演出全体の速さ。デバッグでゆっくり見たいときに下げる。")]
        [SerializeField] private float _timeScale = 1f;

        private RenderTexture _state;
        private Material _stampInstance;
        private Material _flowInstance;
        private Material _displayInstance;

        public float SplatRadius
        {
            get => _splatRadius;
            set => _splatRadius = value;
        }

        public float SplatAmount
        {
            get => _splatAmount;
            set => _splatAmount = value;
        }

        public float Gravity
        {
            get => _gravity;
            set => _gravity = value;
        }

        public float SurfaceTension
        {
            get => _surfaceTension;
            set => _surfaceTension = value;
        }

        public float FilmThickness
        {
            get => _filmThickness;
            set => _filmThickness = value;
        }

        public float DryRate
        {
            get => _dryRate;
            set => _dryRate = value;
        }

        public float TimeScale
        {
            get => _timeScale;
            set => _timeScale = value;
        }

        private void Awake()
        {
            if (_stampMaterial == null || _flowMaterial == null || _displayMaterial == null)
            {
                Debug.LogError("[MixVerse] BloodSplatterOverlay にマテリアルが設定されていません。", this);
                enabled = false;
                return;
            }

            _stampInstance = new Material(_stampMaterial);
            _flowInstance = new Material(_flowMaterial);
            _displayInstance = new Material(_displayMaterial);

            CreateState();
        }

        private void Update()
        {
            if (_state == null)
            {
                return;
            }

            // 画面サイズが変わるとアスペクト比がずれて円が楕円に見えるので作り直す
            if (_state.width != CalculateWidth() || _state.height != _resolution)
            {
                CreateState();
            }

            Simulate();
        }

        private void OnDestroy()
        {
            if (_state != null)
            {
                _state.Release();
                _state = null;
            }

            DestroyInstance(_stampInstance);
            DestroyInstance(_flowInstance);
            DestroyInstance(_displayInstance);
        }

        /// <summary>
        /// 画面 UV（左下が 0, 0）へしぶきを 1 発飛ばす。飛んできた向きは適当に選ぶ。
        /// </summary>
        public void Splat(Vector2 screenUv)
        {
            var angle = Random.value * Mathf.PI * 2f;
            Splat(screenUv, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        }

        /// <summary>
        /// 向きを指定してしぶきを 1 発飛ばす。指定した向き側へ粒が散り、その向きへ伸びる。
        /// </summary>
        public void Splat(Vector2 screenUv, Vector2 direction)
        {
            var variance = 1f + Random.Range(-_splatVariance, _splatVariance);
            Splat(screenUv, direction, _splatRadius * variance, _splatAmount * variance);
        }

        public void Splat(Vector2 screenUv, Vector2 direction, float radius, float amount)
        {
            if (_state == null || _stampInstance == null)
            {
                return;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.right;
            }

            _stampInstance.SetVector(SplatCenterId, screenUv);
            _stampInstance.SetVector(SplatDirectionId, direction.normalized);
            _stampInstance.SetFloat(SplatRadiusId, Mathf.Max(0.001f, radius));
            _stampInstance.SetFloat(SplatAmountId, amount);
            _stampInstance.SetFloat(SplatSeedId, Random.value);
            _stampInstance.SetFloat(SplatStretchId, _splatStretch);
            _stampInstance.SetFloat(SplatDirectionalityId, _splatDirectionality);
            _stampInstance.SetFloat(DropletCountId, _dropletCount);
            _stampInstance.SetFloat(DropletSpreadId, _dropletSpread);
            _stampInstance.SetFloat(ImpactStainId, _impactStain);
            _stampInstance.SetFloat(AspectId, _state.width / (float)_state.height);

            ApplyPass(_stampInstance);
        }

        /// <summary>
        /// 1 か所へまとめて浴びせる。1 発では物足りない大きな出血用。
        /// </summary>
        public void SplatBurst(Vector2 screenUv, int count, float scatter = 0.12f)
        {
            var angle = Random.value * Mathf.PI * 2f;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            for (var i = 0; i < count; i++)
            {
                Splat(screenUv + Random.insideUnitCircle * scatter, direction + Random.insideUnitCircle * 0.4f);
            }
        }

        /// <summary>血をすべて消す。</summary>
        public void Clear()
        {
            if (_state == null)
            {
                return;
            }

            var previous = RenderTexture.active;
            RenderTexture.active = _state;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
        }

        private void Simulate()
        {
            // 処理落ちしたフレームで血が飛びすぎないよう、進める時間に上限を設ける
            var deltaTime = Mathf.Min(Time.deltaTime, 1f / 30f) * _timeScale;

            if (deltaTime <= 0f)
            {
                return;
            }

            _flowInstance.SetVector(TexelSizeId, new Vector4(1f / _state.width, 1f / _state.height, _state.width, _state.height));
            _flowInstance.SetFloat(DeltaTimeId, deltaTime);
            _flowInstance.SetFloat(GravityId, _gravity);
            _flowInstance.SetFloat(FilmThicknessId, _filmThickness);
            _flowInstance.SetFloat(SurfaceTensionId, _surfaceTension);
            _flowInstance.SetFloat(TensionSoftnessId, _tensionSoftness);
            _flowInstance.SetFloat(DryFrictionId, _dryFriction);
            _flowInstance.SetFloat(LaneThresholdId, _laneThreshold);
            _flowInstance.SetFloat(TrailGainId, _trailGain);
            _flowInstance.SetFloat(DryRateId, _dryRate);
            _flowInstance.SetFloat(DryToStainId, _dryToStain);
            _flowInstance.SetFloat(AgeRateId, _ageRate);
            _flowInstance.SetFloat(WanderId, _wander);
            _flowInstance.SetFloat(LaneScaleId, _laneScale);
            _flowInstance.SetFloat(WanderScaleId, _wanderScale);
            _flowInstance.SetFloat(MaxThicknessId, _maxThickness);

            ApplyPass(_flowInstance);
        }

        /// <summary>
        /// 同じ RenderTexture を入力と出力に同時には使えないので、一時的な 1 枚を挟んで書き戻す。
        /// </summary>
        private void ApplyPass(Material material)
        {
            var buffer = RenderTexture.GetTemporary(_state.descriptor);

            Graphics.Blit(_state, buffer, material);
            Graphics.Blit(buffer, _state);

            RenderTexture.ReleaseTemporary(buffer);
        }

        private void CreateState()
        {
            if (_state != null)
            {
                _state.Release();
            }

            // 血の量は 1 を超えるので、8bit では厚みの差が潰れてしまう
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                ? RenderTextureFormat.ARGBHalf
                : RenderTextureFormat.ARGB32;

            _state = new RenderTexture(CalculateWidth(), _resolution, 0, format, RenderTextureReadWrite.Linear)
            {
                name = "BloodState",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            _state.Create();
            Clear();

            _displayInstance.SetVector(StateTexelSizeId, new Vector4(1f / _state.width, 1f / _state.height, _state.width, _state.height));

            if (_targetImage != null)
            {
                _targetImage.material = _displayInstance;
                _targetImage.texture = _state;
            }
        }

        private int CalculateWidth()
        {
            var aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(_resolution * aspect));
        }

        private static void DestroyInstance(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }
    }
}
