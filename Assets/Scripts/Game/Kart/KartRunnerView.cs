using UnityEngine;

namespace MixVerse.Game.Kart
{
    // カートの代わりに走るスーツ姿の人型。挙動はカートのまま、手足だけわざと破綻気味に振り回して「クソゲー感」を出す
    public sealed class KartRunnerView : MonoBehaviour
    {
        private const float IdleSpeedRatio = 0.02f;
        private const float MinCadence = 3f;
        private const float MaxCadence = 16f;
        private const float AirCadence = 22f;
        private const float FlightPelvisLift = 0.1f;
        private const float FlightPelvisBack = 0.25f;

        public Transform Pelvis;
        public Transform Torso;
        public Transform Head;
        public Transform Tie;
        public Transform Cape;
        public Transform[] Shoulders;
        public Transform[] Elbows;
        public Transform[] Hips;
        public Transform[] Knees;

        private Vector3 _pelvisBase;
        private float _phase;
        private float _armSpin;
        private float _time;
        private float _noiseSeed;

        public void Initialize()
        {
            _pelvisBase = Pelvis.localPosition;
            _noiseSeed = Random.value * 100f;
        }

        // speedRatio は最高速に対する比率（後退時は負）。lean はドリフト方向（-1 / 0 / 1）。flying は昇給辞令の無敵中
        public void Animate(float speedRatio, bool airborne, bool flailing, bool turbo, bool flying, int lean, float deltaTime)
        {
            _time += deltaTime;
            var ratio = Mathf.Clamp01(Mathf.Abs(speedRatio));
            var direction = speedRatio < 0f ? -1f : 1f;
            var cadence = airborne ? AirCadence : ratio < IdleSpeedRatio ? 0f : Mathf.Lerp(MinCadence, MaxCadence, ratio);
            if (turbo) cadence *= 1.4f;
            _phase += deltaTime * cadence * Mathf.PI * 2f * direction;
            _armSpin += deltaTime * cadence * 540f * direction;

            var superman = flying && !flailing;
            if (Cape.gameObject.activeSelf != superman) Cape.gameObject.SetActive(superman);

            if (flailing) Flail();
            else if (superman) Fly(lean);
            else if (airborne) AirPedal(ratio);
            else if (ratio < IdleSpeedRatio) Idle();
            else Run(ratio, turbo, lean);
        }

        private void Run(float ratio, bool turbo, int lean)
        {
            var stride = Mathf.Lerp(35f, 88f, ratio);
            var kneeMax = Mathf.Lerp(30f, 135f, ratio);
            for (var side = 0; side < 2; side++)
            {
                var offset = side == 0 ? 0f : Mathf.PI;
                var swing = Mathf.Sin(_phase + offset);
                Hips[side].localRotation = Quaternion.Euler(-swing * stride, 0f, (side == 0 ? -1f : 1f) * Mathf.Abs(swing) * 12f);
                Knees[side].localRotation = Quaternion.Euler(kneeMax * (0.5f + 0.5f * Mathf.Sin(_phase + offset + 1.3f)), 0f, 0f);
            }

            var bounce = Mathf.Abs(Mathf.Sin(_phase)) * Mathf.Lerp(0.04f, 0.16f, ratio);
            Pelvis.localPosition = _pelvisBase + Vector3.up * bounce;
            Pelvis.localRotation = Quaternion.Euler(0f, Mathf.Sin(_phase) * 20f * ratio, -lean * 28f);

            if (turbo)
            {
                // ターボ中は腕を真後ろに伸ばした忍者走り
                Torso.localRotation = Quaternion.Euler(55f, 0f, Mathf.Sin(_phase * 2f) * 6f);
                Head.localRotation = Quaternion.Euler(-45f, Mathf.Sin(_phase * 3.1f) * 10f, 0f);
                for (var side = 0; side < 2; side++)
                {
                    var outward = side == 0 ? -1f : 1f;
                    Shoulders[side].localRotation = Quaternion.Euler(30f + Mathf.Sin(_phase * 4f + side) * 6f, 0f, outward * 14f);
                    Elbows[side].localRotation = Quaternion.identity;
                }
            }
            else
            {
                Torso.localRotation = Quaternion.Euler(Mathf.Lerp(6f, 32f, ratio), Mathf.Sin(_phase) * 30f * ratio, Mathf.Sin(_phase * 0.5f) * 10f);
                Head.localRotation = Quaternion.Euler(-Mathf.Lerp(6f, 25f, ratio) + Mathf.Sin(_phase * 2f) * 12f,
                    Mathf.Sin(_phase * 1.7f) * 35f, Mathf.Sin(_phase * 2.9f) * 22f);
                for (var side = 0; side < 2; side++)
                {
                    var outward = side == 0 ? -1f : 1f;
                    // 前後に振るのではなく風車のように一回転させる
                    var spin = _armSpin + (side == 0 ? 0f : 180f);
                    Shoulders[side].localRotation = Quaternion.Euler(spin, 0f, outward * (25f + 35f * Mathf.Sin(_phase * 3f + side)));
                    Elbows[side].localRotation = Quaternion.Euler(-(45f + 55f * Mathf.Sin(_phase * 2.3f + side * 1.7f)), 0f, 0f);
                }
            }
            Tie.localRotation = Quaternion.Euler(Mathf.Lerp(10f, 150f, ratio) + Mathf.Sin(_time * 47f) * 18f, Mathf.Sin(_time * 31f) * 25f, 0f);
        }

        private void AirPedal(float ratio)
        {
            for (var side = 0; side < 2; side++)
            {
                var outward = side == 0 ? -1f : 1f;
                var offset = side == 0 ? 0f : Mathf.PI;
                Hips[side].localRotation = Quaternion.Euler(-50f - Mathf.Sin(_phase + offset) * 55f, 0f, outward * 10f);
                Knees[side].localRotation = Quaternion.Euler(70f + Mathf.Cos(_phase + offset) * 60f, 0f, 0f);
                // 空中では両腕を鳥のようにバタつかせる
                Shoulders[side].localRotation = Quaternion.Euler(Mathf.Sin(_time * 9f) * 20f, 0f, outward * (110f + Mathf.Sin(_time * 34f) * 60f));
                Elbows[side].localRotation = Quaternion.Euler(-Mathf.Abs(Mathf.Sin(_time * 34f)) * 50f, 0f, 0f);
            }
            Pelvis.localPosition = _pelvisBase;
            Pelvis.localRotation = Quaternion.identity;
            Torso.localRotation = Quaternion.Euler(-10f + Mathf.Sin(_time * 12f) * 8f, 0f, Mathf.Sin(_time * 17f) * 12f);
            Head.localRotation = Quaternion.Euler(-20f, Mathf.Sin(_time * 21f) * 40f, 0f);
            Tie.localRotation = Quaternion.Euler(170f + Mathf.Sin(_time * 50f) * 25f, 0f, 0f);
        }

        // 体を水平に倒し、右拳を前へ突き出して左腕は体に沿わせるスーパーマンの飛行ポーズ
        private void Fly(int lean)
        {
            Pelvis.localPosition = _pelvisBase + new Vector3(0f, FlightPelvisLift + Mathf.Sin(_time * 3f) * 0.06f, -FlightPelvisBack);
            Pelvis.localRotation = Quaternion.Euler(82f + Mathf.Sin(_time * 2.4f) * 3f, 0f, -lean * 25f);
            Torso.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            Head.localRotation = Quaternion.Euler(-70f, 0f, 0f);

            Shoulders[1].localRotation = Quaternion.Euler(176f, 0f, 0f);
            Elbows[1].localRotation = Quaternion.identity;
            Shoulders[0].localRotation = Quaternion.Euler(-12f, 0f, -14f);
            Elbows[0].localRotation = Quaternion.Euler(-20f, 0f, 0f);
            for (var side = 0; side < 2; side++)
            {
                var outward = side == 0 ? -1f : 1f;
                var flutter = Mathf.Sin(_time * 5f + side * Mathf.PI);
                Hips[side].localRotation = Quaternion.Euler(-4f + flutter * 4f, 0f, outward * 3f);
                Knees[side].localRotation = Quaternion.Euler(10f + flutter * 6f, 0f, 0f);
            }

            Tie.localRotation = Quaternion.Euler(15f + Mathf.Sin(_time * 40f) * 12f, Mathf.Sin(_time * 27f) * 20f, 0f);
            Cape.localRotation = Quaternion.Euler(30f + Mathf.Sin(_time * 16f) * 12f, 0f, Mathf.Sin(_time * 11f) * 8f);
        }

        private void Idle()
        {
            for (var side = 0; side < 2; side++)
            {
                var outward = side == 0 ? -1f : 1f;
                // スタート待ちでもその場で小刻みに足踏みさせる
                var tap = Mathf.Max(0f, Mathf.Sin(_time * 14f + side * Mathf.PI));
                Hips[side].localRotation = Quaternion.Euler(-tap * 25f, 0f, 0f);
                Knees[side].localRotation = Quaternion.Euler(tap * 50f, 0f, 0f);
                Shoulders[side].localRotation = Quaternion.Euler(Mathf.Sin(_time * 7f + side) * 15f, 0f, outward * (8f + Mathf.Sin(_time * 11f) * 6f));
                Elbows[side].localRotation = Quaternion.Euler(-20f, 0f, 0f);
            }
            Pelvis.localPosition = _pelvisBase;
            Pelvis.localRotation = Quaternion.identity;
            Torso.localRotation = Quaternion.Euler(4f, 0f, Mathf.Sin(_time * 3f) * 3f);
            Head.localRotation = Quaternion.Euler(0f, Mathf.Sin(_time * 2.3f) * 25f, 0f);
            Tie.localRotation = Quaternion.Euler(Mathf.Sin(_time * 5f) * 8f, 0f, 0f);
        }

        // 被弾中は全関節をノイズで勝手にぐるぐる回して、関節の可動域を完全に無視させる
        private void Flail()
        {
            for (var side = 0; side < 2; side++)
            {
                var seed = _noiseSeed + side * 13f;
                Hips[side].localRotation = Quaternion.Euler(Noise(seed, 1f) * 240f, 0f, Noise(seed, 2f) * 120f);
                Knees[side].localRotation = Quaternion.Euler(Mathf.Abs(Noise(seed, 3f)) * 300f, 0f, 0f);
                Shoulders[side].localRotation = Quaternion.Euler(Noise(seed, 4f) * 720f, Noise(seed, 5f) * 180f, Noise(seed, 6f) * 360f);
                Elbows[side].localRotation = Quaternion.Euler(Noise(seed, 7f) * 300f, 0f, 0f);
            }
            Pelvis.localPosition = _pelvisBase;
            Pelvis.localRotation = Quaternion.Euler(Noise(_noiseSeed, 8f) * 40f, Noise(_noiseSeed, 9f) * 60f, 0f);
            Torso.localRotation = Quaternion.Euler(Noise(_noiseSeed, 10f) * 70f, Noise(_noiseSeed, 11f) * 90f, Noise(_noiseSeed, 12f) * 50f);
            Head.localRotation = Quaternion.Euler(Noise(_noiseSeed, 13f) * 90f, Noise(_noiseSeed, 14f) * 360f, Noise(_noiseSeed, 15f) * 90f);
            Tie.localRotation = Quaternion.Euler(Noise(_noiseSeed, 16f) * 360f, 0f, 0f);
        }

        private float Noise(float seed, float channel) => Mathf.PerlinNoise(_time * 6f + seed, channel * 7.3f) * 2f - 1f;
    }
}
