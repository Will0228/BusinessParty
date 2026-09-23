using System.Collections.Generic;
using MixVerse.Game.Model.Kart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Game.Kart
{
    public sealed class KartStageView : MonoBehaviour
    {
        public Camera Camera;
        public AudioListener Listener;
        public Transform[] Karts;
        public Transform[] Tags;
        public TextMeshPro[] TagLabels;
        public TextMeshProUGUI Ranking;
        public TextMeshProUGUI Section;
        public TextMeshProUGUI Speed;
        public TextMeshProUGUI Item;
        public GameObject[] ItemIcons;
        public GameObject EmptyItemIcon;
        public TextMeshProUGUI Warning;
        public TextMeshProUGUI Radio;
        public TextMeshProUGUI Drift;
        public TextMeshProUGUI Controls;
        public TextMeshProUGUI ModalTitle;
        public TextMeshProUGUI ModalBody;
        public TextMeshProUGUI ActionLabel;
        public GameObject Modal;
        public GameObject RaceHud;
        public Button ActionButton;
        public Button ExitButton;
        public Image Progress;
        public Image Gain;
        public Image DriftFill;
        public Transform TailgateZone;
        public AudioSource Audio;
        public AudioClip RadioClip;
        public AudioClip AlertClip;
        public ParticleSystem[] DriftSparks;
        public readonly List<Object> GeneratedAssets = new List<Object>();
        public readonly Dictionary<int, Transform> ObjectViews = new Dictionary<int, Transform>();
        public readonly Dictionary<int, KartExplosionView> Explosions = new Dictionary<int, KartExplosionView>();
        public readonly List<Vector3> Path = new List<Vector3>();
        public readonly List<KeyValuePair<float, GameObject>> Gates = new List<KeyValuePair<float, GameObject>>();
        public KartStageFactory Factory;
        private float _lastSlip;
        private int _lastAttacks;
        private RacePhase _lastPhase;
        private readonly List<int> _expired = new List<int>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<Transform> _resultRockets = new List<Transform>();
        private readonly List<Vector3> _rocketStarts = new List<Vector3>();
        private readonly HashSet<int> _rocketImpacts = new HashSet<int>();
        private const float BossTurnTime = 0.85f;
        private const float BossCrashTime = 1.55f;
        private KartRaceSettings _settings;
        private bool _debug;
        private bool _worldLabelsHidden;
        private bool _bossCrashExploded;
        private readonly string[] _sectionNames = { "01  市街地", "02  峠の上り", "03  ギャラリー", "04  トンネル", "05  連続ヘアピン", "06  ゴール前直線" };
        private readonly string[] _racerNames = { "あなた", "上司", "部下" };

        public void Initialize(KartRaceSettings settings) => _settings = settings;
        public void ToggleDebug() => _debug = !_debug;

        public Vector3 Point(float distance, float lane = 0f)
        {
            var value = Mathf.Clamp(distance / 5f, 0f, Path.Count - 1.001f);
            var index = Mathf.FloorToInt(value);
            var forward = (Path[index + 1] - Path[index]).normalized;
            return Vector3.Lerp(Path[index], Path[index + 1], value - index) + Vector3.Cross(Vector3.up, forward).normalized * lane;
        }
        public Quaternion DirectionAt(float distance)
        {
            return Quaternion.LookRotation(Point(distance + 1f) - Point(Mathf.Max(0f, distance - 1f)), Vector3.up);
        }

        public void Render(KartRace race, float gain, bool ready, float countdown, bool midi)
        {
            for (var i = 0; i < race.Racers.Length; i++)
            {
                var racer = race.Racers[i];
                var position = Point(racer.Distance, racer.Lane);
                var trackRotation = DirectionAt(racer.Distance);
                var spin = racer.DisabledSeconds > 0f && racer.IsSpinning ? race.Time * 650f : 0f;
                var knockbackOffset = Vector3.zero;
                var knockbackTumble = Quaternion.identity;
                if (racer.DisabledSeconds > 0f && !racer.IsSpinning)
                {
                    var total = _settings.knockbackFlightSeconds + _settings.knockbackRecoverySeconds;
                    var elapsed = Mathf.Max(0f, total - racer.DisabledSeconds);
                    var flightT = Mathf.Clamp01(elapsed / _settings.knockbackFlightSeconds);
                    var side = racer.Lane >= 0f ? 1f : -1f;
                    if (racer.IsCourseOut)
                    {
                        var fallT = Mathf.Clamp01((elapsed - _settings.knockbackFlightSeconds * 0.4f) / (_settings.knockbackFlightSeconds * 0.6f));
                        var spinProgress = elapsed / _settings.knockbackFlightSeconds;
                        knockbackOffset = new Vector3(side * flightT * flightT * 11f, Mathf.Sin(flightT * Mathf.PI * 0.6f) * 8f - fallT * fallT * 13f, -flightT * 4f);
                        knockbackTumble = Quaternion.Euler(spinProgress * 760f, 0f, spinProgress * 560f * side);
                    }
                    else
                    {
                        var grounded = elapsed >= _settings.knockbackFlightSeconds;
                        knockbackOffset = new Vector3(Mathf.Sin(flightT * Mathf.PI) * side * 1.4f, Mathf.Sin(flightT * Mathf.PI) * 4f, -Mathf.Sin(flightT * Mathf.PI) * 1.2f);
                        knockbackTumble = grounded
                            ? Quaternion.Euler(0f, 0f, 78f + Mathf.Sin(race.Time * 10f) * 10f)
                            : Quaternion.Euler(flightT * 420f, 0f, flightT * 300f * side);
                    }
                }
                if (race.ResultScene == FailureScene.Distance && i == (int)RacerId.Player && race.ResultTime >= 0.8f)
                {
                    var flight = Mathf.Clamp01((race.ResultTime - 0.8f) / 1.8f);
                    knockbackOffset = new Vector3(14f * flight, Mathf.Sin(flight * Mathf.PI) * 12f, -8f * flight);
                    knockbackTumble = Quaternion.Euler(flight * 1440f, flight * 550f, flight * 1080f);
                }
                if (race.ResultScene == FailureScene.BossHit && i == (int)RacerId.Player && race.ResultTime >= BossCrashTime)
                {
                    var impact = Mathf.Clamp01((race.ResultTime - BossCrashTime) / 1.1f);
                    knockbackOffset = new Vector3(5f * impact, Mathf.Sin(impact * Mathf.PI) * 4.5f, -7f * impact);
                    knockbackTumble = Quaternion.Euler(impact * 760f, impact * 320f, impact * 620f);
                }
                var kartPosition = position + Vector3.up * 0.45f + trackRotation * knockbackOffset;
                var kartRotation = trackRotation * Quaternion.Euler(0f, spin, 0f) * knockbackTumble;
                if (race.ResultScene == FailureScene.BossHit && i == (int)RacerId.Boss)
                {
                    var launch = Mathf.Clamp01(race.ResultTime / BossTurnTime);
                    var launchOffset = new Vector3(-11f * launch, Mathf.Sin(launch * Mathf.PI * 0.5f) * 9f, -5f * launch);
                    kartPosition = position + Vector3.up * 0.45f + trackRotation * launchOffset;
                    kartRotation = trackRotation * Quaternion.Euler(launch * 760f, launch * 300f, -launch * 620f);
                    if (race.ResultTime >= BossTurnTime)
                    {
                        var chase = Mathf.Clamp01((race.ResultTime - BossTurnTime) / (BossCrashTime - BossTurnTime));
                        var easedChase = chase * chase * (3f - 2f * chase);
                        var launchEnd = position + Vector3.up * 0.45f + trackRotation * new Vector3(-11f, 9f, -5f);
                        var target = Karts[(int)RacerId.Player].localPosition + Vector3.up * 0.25f;
                        kartPosition = Vector3.Lerp(launchEnd, target, easedChase) + Vector3.up * (Mathf.Sin(chase * Mathf.PI) * 5f);
                        var approach = target - kartPosition;
                        if (approach.sqrMagnitude > 0.001f)
                            kartRotation = Quaternion.LookRotation(approach.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, chase * 900f);
                        if (race.ResultTime >= BossCrashTime)
                        {
                            var rebound = Mathf.Clamp01((race.ResultTime - BossCrashTime) / 1.1f);
                            kartPosition = target + trackRotation * new Vector3(-3f * rebound, Mathf.Sin(rebound * Mathf.PI) * 2.5f, -4f * rebound);
                            kartRotation = trackRotation * Quaternion.Euler(rebound * 680f, rebound * 260f, -rebound * 780f);
                        }
                    }
                }
                Karts[i].localPosition = kartPosition;
                Karts[i].localRotation = kartRotation;
                Tags[i].gameObject.SetActive(race.Phase == RacePhase.Racing);
                Tags[i].localPosition = position + Vector3.up * 3.2f;
                Tags[i].rotation = Camera.transform.rotation;
                TagLabels[i].text = $"{race.Rank(racer)}  {_racerNames[i]}";
            }
            var followBoss = race.ResultScene == FailureScene.BossHit;
            var focusRacer = followBoss ? race.Boss : race.Player;
            var focus = race.Phase == RacePhase.Failed ? Karts[(int)focusRacer.Id].localPosition : Point(race.Player.Distance);
            var direction = DirectionAt(focusRacer.Distance);
            Camera.transform.localPosition = focus + direction * new Vector3(0f, 11f, -17f);
            Camera.transform.LookAt(transform.TransformPoint(focus + direction * new Vector3(0f, 0f, race.Phase == RacePhase.Failed ? 2f : 13f)));
            Camera.backgroundColor = race.SectionAt(race.Player.Distance) == CourseSection.Tunnel ? new Color(0.025f, 0.035f, 0.06f) : new Color(0.28f, 0.48f, 0.62f);
            foreach (var gate in Gates) gate.Value.SetActive(gate.Key > race.Player.Distance + 15f);
            RenderObjects(race);
            RenderResultBarrage(race);
            RenderBossCrash(race);
            RenderExplosionImpact();
            UpdateDriftSparks(race);
            var ranks = new string[3];
            foreach (var racer in race.Racers)
            {
                var rank = race.Rank(racer) - 1;
                while (rank < 2 && ranks[rank] != null) rank++;
                ranks[rank] = $"{rank + 1}  {_racerNames[(int)racer.Id]}    {racer.Distance:0} m";
            }
            Ranking.text = string.Join("\n", ranks);
            var section = race.SectionAt(race.Player.Distance);
            Section.text = _sectionNames[(int)section] + $"\n残り {Mathf.Max(0f, _settings.courseLength - race.Player.Distance):0} m";
            Speed.text = $"<size=64>{Mathf.Abs(race.Player.Speed):00}</size> <size=20>/ 100</size>\n{(race.Direction > 0 ? "DRIVE" : "REVERSE")}   GAIN {gain * 100f:0}%";
            Item.text = "所持アイテム\n<size=25>" + ItemName(race.Player.Item) + "</size>\n<size=16>SYNC / SPACE で使用</size>";
            EmptyItemIcon.SetActive(race.Player.Item == KartItem.None);
            for (var i = 1; i < ItemIcons.Length; i++) ItemIcons[i].SetActive((int)race.Player.Item == i);
            Drift.text = race.Player.TurboSeconds > 0f ? "TURBO  加速中" : $"DRIFT  {race.DriftTier} / 2";
            DriftFill.fillAmount = Mathf.Clamp01(race.DriftTime / _settings.driftSecondSeconds);
            Gain.fillAmount = gain;
            Progress.fillAmount = race.Player.Distance / _settings.courseLength;
            var distance = race.MaximumDistance;
            Warning.color = distance > _settings.maximumBossDistance * 0.7f || race.TailgateTime > 0.2f ? new Color(1f, 0.42f, 0.3f) : new Color(0.65f, 0.87f, 0.87f);
            Warning.text = $"上司からの最大距離  {distance:0} / {_settings.maximumBossDistance:0} m";
            if (race.TailgateTime > 0.2f) Warning.text += $"   煽り注意！ {_settings.tailgateSeconds - race.TailgateTime:0.0}s";
            if (section == CourseSection.Gallery) Warning.text += "\n監視中：バック・スピン・横転は厳禁";
            else if (race.InCamera(race.Player.Distance)) Warning.text += "\n監視カメラ録画中：ゴール後に確認";
            Radio.gameObject.SetActive(!ready && countdown <= 0f && race.Phase == RacePhase.Racing);
            Radio.text = race.SlipRemaining > 0f ? $"<color=#FFAA70>無線：課長、遅くないっすか？</color>\nあと <size=36>{race.SlipRemaining:0.0}</size> 秒以内に部下を攻撃！" : race.Message;
            Controls.text = "W/S 速度   A/D 操舵   Q/E ドリフト   X 前後切替   SPACE アイテム   TAB 判定範囲   ESC ホーム" + (midi ? "   • MIDI 接続中" : "");
            TailgateZone.gameObject.SetActive(_debug);
            TailgateZone.localPosition = Point(race.Boss.Distance - _settings.tailgateDistance * 0.5f, race.Boss.Lane) + Vector3.up * 0.05f;
            TailgateZone.localRotation = DirectionAt(race.Boss.Distance);
            var result = !ready && countdown <= 0f && race.Phase != RacePhase.Racing;
            RaceHud.SetActive(!result);
            SetWorldLabelsVisible(!result);
            ConfigureModal(result);
            Modal.SetActive(ready || countdown > 0f || result);
            ActionButton.gameObject.SetActive(countdown <= 0f);
            ExitButton.gameObject.SetActive(countdown <= 0f);
            if (ready)
            {
                ModalTitle.text = "接待カートレース";
                ModalBody.text = "上司を、僅差で1位に。\n\nあなた 100  /  部下 70  /  上司 50\n部下の前で減速してブロック。箱・書類・ロケランでも妨害できます。\n上司への接触・煽りは禁止。離れすぎても失敗です。\n失言には10秒以内に攻撃。ゴールは上司1位・着差3秒未満！\n\nW/Sで速度を調節  •  A/Dで左右へ  •  SPACEでアイテム\n最初はGAIN 0%。WかCC #9で加速して部下の前へ。\nDJ: CC #10は0で右・1で左、CC #24でドリフト。";
                ActionLabel.text = "接待を始める  [ ENTER / SYNC ]";
            }
            else if (countdown > 0f)
            {
                ModalTitle.text = Mathf.CeilToInt(countdown).ToString();
                ModalBody.text = "上司には触れず、部下の前へ。";
            }
            else if (race.Phase != RacePhase.Racing)
            {
                ModalTitle.text = race.Phase == RacePhase.Cleared ? "接待成功！" : "接待失敗";
                ModalBody.text = race.ResultReason;
                ActionLabel.text = "もう一度プレイする";
            }
            if (race.SlipRemaining > _lastSlip + 1f) Audio.PlayOneShot(RadioClip, 0.6f);
            if (race.Attacks > _lastAttacks) Audio.PlayOneShot(AlertClip, 0.35f);
            if (race.Phase != _lastPhase && race.Phase == RacePhase.Failed) Audio.PlayOneShot(RadioClip, 0.6f);
            _lastSlip = race.SlipRemaining;
            _lastAttacks = race.Attacks;
            _lastPhase = race.Phase;
        }

        private void ConfigureModal(bool result)
        {
            ((RectTransform)Modal.transform).sizeDelta = result ? new Vector2(650f, 290f) : new Vector2(1100f, 650f);
            ModalTitle.rectTransform.sizeDelta = result ? new Vector2(600f, 65f) : new Vector2(1000f, 90f);
            ModalTitle.rectTransform.anchoredPosition = result ? new Vector2(0f, -26f) : new Vector2(0f, -40f);
            ModalBody.rectTransform.sizeDelta = result ? new Vector2(600f, 90f) : new Vector2(1000f, 370f);
            ModalBody.rectTransform.anchoredPosition = result ? new Vector2(0f, 12f) : new Vector2(0f, 18f);
            ((RectTransform)ActionButton.transform).sizeDelta = result ? new Vector2(260f, 58f) : new Vector2(610f, 60f);
            ((RectTransform)ActionButton.transform).anchoredPosition = result ? new Vector2(-145f, 22f) : new Vector2(-145f, 45f);
            ActionLabel.rectTransform.sizeDelta = result ? new Vector2(250f, 58f) : new Vector2(610f, 60f);
            ((RectTransform)ExitButton.transform).sizeDelta = result ? new Vector2(260f, 58f) : new Vector2(235f, 60f);
            ((RectTransform)ExitButton.transform).anchoredPosition = result ? new Vector2(145f, 22f) : new Vector2(335f, 45f);
            var exitLabel = ExitButton.GetComponentInChildren<TextMeshProUGUI>();
            exitLabel.rectTransform.sizeDelta = result ? new Vector2(250f, 58f) : new Vector2(235f, 60f);
            exitLabel.text = result ? "ホームに戻る" : "ホームへ [ ESC ]";
        }

        private void SetWorldLabelsVisible(bool visible)
        {
            if (_worldLabelsHidden == !visible) return;
            _worldLabelsHidden = !visible;
            foreach (var label in GetComponentsInChildren<TextMeshPro>(true)) label.enabled = visible;
        }

        public string ItemName(KartItem item)
        {
            switch (item)
            {
                case KartItem.Papers: return "書類ばら撒き";
                case KartItem.Rocket: return "ロケラン";
                case KartItem.Drink: return "栄養ドリンク";
                default: return "アイテムなし";
            }
        }

        private void RenderObjects(KartRace race)
        {
            _seen.Clear();
            foreach (var obj in race.Objects)
            {
                if (!obj.Active || Mathf.Abs(obj.Distance - race.Player.Distance) > 160f) continue;
                _seen.Add(obj.Id);
                if (!ObjectViews.TryGetValue(obj.Id, out var view))
                {
                    view = Factory.CreateObject(this, obj);
                    ObjectViews.Add(obj.Id, view);
                }
                view.localPosition = Point(obj.Distance, obj.Lane) + Vector3.up * (obj.Kind == TrackObjectKind.Papers ? 0.08f : 0.8f);
                view.localRotation = DirectionAt(obj.Distance);
                if (obj.Kind == TrackObjectKind.ItemBox) view.localRotation *= Quaternion.Euler(0f, race.Time * 70f, 10f);
            }
            _expired.Clear();
            foreach (var pair in ObjectViews)
            {
                if (_seen.Contains(pair.Key)) continue;
                if (Explosions.TryGetValue(pair.Key, out var effect) && effect.IsAlive) continue;
                _expired.Add(pair.Key);
            }
            foreach (var id in _expired)
            {
                Destroy(ObjectViews[id].gameObject);
                ObjectViews.Remove(id);
                Explosions.Remove(id);
            }
        }

        private void RenderResultBarrage(KartRace race)
        {
            if (race.ResultScene != FailureScene.Distance) return;
            if (_resultRockets.Count == 0)
            {
                for (var i = 0; i < 18; i++)
                {
                    var rocket = Factory.CreateResultRocket();
                    var x = 0.1f + ((i * 7) % 17) / 21f;
                    var start = Camera.ViewportToWorldPoint(new Vector3(x, 1.12f + (i % 3) * 0.09f, 31f));
                    _resultRockets.Add(rocket);
                    _rocketStarts.Add(transform.InverseTransformPoint(start));
                }
            }
            for (var i = 0; i < _resultRockets.Count; i++)
            {
                var rocket = _resultRockets[i];
                var flight = (race.ResultTime - i * 0.055f) / 0.62f;
                rocket.gameObject.SetActive(flight >= 0f && flight < 1f);
                if (flight < 0f) continue;
                var target = Karts[(int)RacerId.Player].localPosition + Vector3.up * 0.5f;
                if (flight >= 1f)
                {
                    if (i % 3 == 0 && _rocketImpacts.Add(i))
                    {
                        var id = -100 - i;
                        var explosion = Factory.CreateResultExplosion(id, target);
                        ObjectViews.Add(id, explosion.transform);
                        Explosions.Add(id, explosion);
                    }
                    continue;
                }
                rocket.localPosition = Vector3.Lerp(_rocketStarts[i], target, Mathf.Clamp01(flight));
                rocket.localRotation = Quaternion.LookRotation(target - rocket.localPosition, Vector3.up);
            }
        }

        private void RenderBossCrash(KartRace race)
        {
            if (race.ResultScene != FailureScene.BossHit || race.ResultTime < BossCrashTime || _bossCrashExploded) return;
            _bossCrashExploded = true;
            const int id = -500;
            var collision = Vector3.Lerp(Karts[(int)RacerId.Player].localPosition, Karts[(int)RacerId.Boss].localPosition, 0.5f) + Vector3.up * 0.5f;
            var explosion = Factory.CreateResultExplosion(id, collision);
            ObjectViews.Add(id, explosion.transform);
            Explosions.Add(id, explosion);
        }

        // マリオカート同様、ドリフトの溜め具合（DriftTier）で火花の色を白→水色→オレンジと変える
        private static readonly Color[] DriftSparkColors =
        {
            new Color(0.92f, 0.92f, 0.97f),
            new Color(0.35f, 0.75f, 1f),
            new Color(1f, 0.55f, 0.1f),
        };

        private void UpdateDriftSparks(KartRace race)
        {
            if (DriftSparks == null) return;
            var active = race.Phase == RacePhase.Racing && race.IsDrifting;
            var color = DriftSparkColors[race.DriftTier];
            foreach (var spark in DriftSparks)
            {
                var emission = spark.emission;
                emission.enabled = active;
                if (!active) continue;
                var main = spark.main;
                main.startColor = color;
            }
        }

        private void RenderExplosionImpact()
        {
            var impact = 0f;
            foreach (var effect in Explosions.Values)
            {
                var distance = Vector3.Distance(Camera.transform.position, effect.transform.position);
                impact = Mathf.Max(impact, effect.Impact * Mathf.Clamp01(1f - distance / 65f));
            }
            Camera.fieldOfView = 58f + impact * 4f;
            Camera.transform.position += Camera.transform.right * (Mathf.Sin(Time.time * 93f) * impact * 0.24f)
                + Camera.transform.up * (Mathf.Cos(Time.time * 117f) * impact * 0.16f);
            Camera.transform.Rotate(0f, 0f, Mathf.Sin(Time.time * 71f) * impact * 1.2f);
        }

        public void ResetObjects()
        {
            foreach (var rocket in _resultRockets) if (rocket != null) Destroy(rocket.gameObject);
            _resultRockets.Clear();
            _rocketStarts.Clear();
            _rocketImpacts.Clear();
            _bossCrashExploded = false;
            foreach (var view in ObjectViews.Values) if (view != null) Destroy(view.gameObject);
            ObjectViews.Clear();
            Explosions.Clear();
            Camera.fieldOfView = 58f;
            _lastSlip = 0f;
            _lastAttacks = 0;
            _lastPhase = RacePhase.Racing;
        }

        private void OnDestroy()
        {
            foreach (var asset in GeneratedAssets) if (asset != null) Destroy(asset);
        }
    }
}
