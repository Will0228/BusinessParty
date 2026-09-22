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
        public TextMeshProUGUI Warning;
        public TextMeshProUGUI Radio;
        public TextMeshProUGUI Drift;
        public TextMeshProUGUI Controls;
        public TextMeshProUGUI ModalTitle;
        public TextMeshProUGUI ModalBody;
        public TextMeshProUGUI ActionLabel;
        public GameObject Modal;
        public Button ActionButton;
        public Button ExitButton;
        public Image Progress;
        public Image Gain;
        public Image DriftFill;
        public Transform TailgateZone;
        public AudioSource Audio;
        public AudioClip RadioClip;
        public AudioClip AlertClip;
        public readonly List<Object> GeneratedAssets = new List<Object>();
        public readonly Dictionary<int, Transform> ObjectViews = new Dictionary<int, Transform>();
        public readonly List<Vector3> Path = new List<Vector3>();
        public readonly List<KeyValuePair<float, GameObject>> Gates = new List<KeyValuePair<float, GameObject>>();
        public KartStageFactory Factory;
        private float _lastSlip;
        private int _lastAttacks;
        private RacePhase _lastPhase;
        private readonly List<int> _expired = new List<int>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private KartRaceSettings _settings;
        private bool _debug;
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
                Karts[i].localPosition = position + Vector3.up * 0.45f;
                var roll = racer.DisabledSeconds > 0f && !racer.IsSpinning ? 80f + Mathf.Sin(race.Time * 10f) * 12f : 0f;
                var spin = racer.DisabledSeconds > 0f && racer.IsSpinning ? race.Time * 650f : 0f;
                Karts[i].localRotation = DirectionAt(racer.Distance) * Quaternion.Euler(0f, spin, roll);
                Tags[i].localPosition = position + Vector3.up * 3.2f;
                Tags[i].rotation = Camera.transform.rotation;
                TagLabels[i].text = $"{race.Rank(racer)}  {_racerNames[i]}";
            }
            var focus = Point(race.Player.Distance);
            var direction = DirectionAt(race.Player.Distance);
            Camera.transform.localPosition = focus + direction * new Vector3(0f, 11f, -17f);
            Camera.transform.LookAt(transform.TransformPoint(focus + direction * new Vector3(0f, 0f, 13f)));
            Camera.backgroundColor = race.SectionAt(race.Player.Distance) == CourseSection.Tunnel ? new Color(0.025f, 0.035f, 0.06f) : new Color(0.28f, 0.48f, 0.62f);
            foreach (var gate in Gates) gate.Value.SetActive(gate.Key > race.Player.Distance + 15f);
            RenderObjects(race);
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
            Item.text = "SYNC / SPACE\n<size=30>" + ItemName(race.Player.Item) + "</size>";
            Drift.text = race.Player.TurboSeconds > 0f ? "TURBO  加速中" : $"DRIFT  {race.DriftTier} / 2";
            DriftFill.fillAmount = Mathf.Clamp01(race.DriftTime / _settings.driftSecondSeconds);
            Gain.fillAmount = gain;
            Progress.fillAmount = race.Player.Distance / _settings.courseLength;
            var distance = race.MaximumDistance;
            Warning.color = distance > _settings.maximumBossDistance * 0.7f || race.TailgateTime > 0.2f ? new Color(1f, 0.42f, 0.3f) : new Color(0.65f, 0.87f, 0.87f);
            Warning.text = $"上司からの最大距離  {distance:0} / {_settings.maximumBossDistance:0} m";
            if (race.TailgateTime > 0.2f) Warning.text += $"   煽り注意！ {_settings.tailgateSeconds - race.TailgateTime:0.0}s";
            if (section == CourseSection.Gallery) Warning.text += "\n監視中：バック・壁衝突・スピン・横転は厳禁";
            else if (race.InCamera(race.Player.Distance)) Warning.text += "\n監視カメラ録画中：ゴール後に確認";
            Radio.gameObject.SetActive(!ready && countdown <= 0f && race.Phase == RacePhase.Racing);
            Radio.text = race.SlipRemaining > 0f ? $"<color=#FFAA70>無線：課長、遅くないっすか？</color>\nあと <size=36>{race.SlipRemaining:0.0}</size> 秒以内に部下を攻撃！" : race.Message;
            Controls.text = "W/S 速度   A/D 操舵   Q/E ドリフト   X 前後切替   SPACE アイテム   TAB 判定範囲   ESC ホーム" + (midi ? "   • MIDI 接続中" : "");
            TailgateZone.gameObject.SetActive(_debug);
            TailgateZone.localPosition = Point(race.Boss.Distance - _settings.tailgateDistance * 0.5f, race.Boss.Lane) + Vector3.up * 0.05f;
            TailgateZone.localRotation = DirectionAt(race.Boss.Distance);
            Modal.SetActive(ready || countdown > 0f || race.Phase != RacePhase.Racing);
            ActionButton.gameObject.SetActive(countdown <= 0f);
            ExitButton.gameObject.SetActive(countdown <= 0f);
            if (ready)
            {
                ModalTitle.text = "接待カートレース";
                ModalBody.text = "上司を、僅差で1位に。\n\nあなた 100  /  部下 70  /  上司 50\n部下の前で減速してブロック。箱・書類・ロケランでも妨害できます。\n上司への接触・煽りは禁止。離れすぎても失敗です。\n失言には10秒以内に攻撃。ゴールは上司1位・着差3秒未満！\n\nW/Sで速度を調節  •  A/Dで左右へ  •  SPACEでアイテム\n最初はGAIN 50%。Wを押して部下の前へ出ましょう。";
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
                ModalBody.text = race.ResultReason + $"\n\n走行時間 {race.Time:0.0} 秒   /   部下への攻撃 {race.Attacks} 回\n" +
                    (race.Boss.Finished ? $"上司と2位の着差 {race.FinishGap:0.00} 秒\n" : "") +
                    (race.CameraEvidence.Count > 0 ? "\n監視記録：" + string.Join(" / ", race.CameraEvidence) : "") + "\n\n" + FailureHint(race.ResultReason);
                ActionLabel.text = "もう一度接待する  [ R / ENTER ]";
            }
            if (race.SlipRemaining > _lastSlip + 1f) Audio.PlayOneShot(RadioClip, 0.6f);
            if (race.Attacks > _lastAttacks) Audio.PlayOneShot(AlertClip, 0.35f);
            if (race.Phase != _lastPhase && race.Phase == RacePhase.Failed) Audio.PlayOneShot(RadioClip, 0.6f);
            _lastSlip = race.SlipRemaining;
            _lastAttacks = race.Attacks;
            _lastPhase = race.Phase;
        }

        private string FailureHint(string reason)
        {
            if (reason.Contains("距離")) return "部下を放置せず、止めすぎず。距離が70mを超えたら調整を。";
            if (reason.Contains("接触") || reason.Contains("煽")) return "上司の横を通るときは車線を空け、真後ろに居続けないように。";
            if (reason.Contains("1位") || reason.Contains("着差")) return "最後は上司を先に通し、あなたか部下が3秒以内にゴール。";
            if (reason.Contains("失言")) return "部下の前に出て速度を落とすブロッキングも攻撃になります。";
            return "ギャラリーでは丁寧に走行。ロケランは上司から離して使いましょう。";
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
                if (obj.Kind == TrackObjectKind.Explosion) view.localScale = Vector3.one * (1f - obj.Lifetime / 0.65f) * _settings.explosionRadius * 2f;
            }
            _expired.Clear();
            foreach (var pair in ObjectViews) if (!_seen.Contains(pair.Key)) _expired.Add(pair.Key);
            foreach (var id in _expired) { Destroy(ObjectViews[id].gameObject); ObjectViews.Remove(id); }
        }

        public void ResetObjects()
        {
            foreach (var view in ObjectViews.Values) if (view != null) Destroy(view.gameObject);
            ObjectViews.Clear();
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
