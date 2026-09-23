using System;
using System.Collections.Generic;

namespace MixVerse.Game.Model.Kart
{
    public enum TrackObjectKind { Crate, ItemBox, Papers, Rocket, Explosion }

    public sealed class TrackObject
    {
        public int Id;
        public TrackObjectKind Kind;
        public float Distance;
        public float Lane;
        public float Lifetime;
        public RacerId Owner;
        public bool Active = true;
        public KartItem Item;
    }

    public sealed partial class KartRace
    {
        private int _objectId;
        private readonly float[] _cpuUseAt = new float[3];
        public List<TrackObject> Objects { get; } = new List<TrackObject>();

        private void BuildObjects()
        {
            for (var distance = 45f; distance < _settings.courseLength - 25f; distance += 60f)
            {
                var index = (int)((distance - 45f) / 60f);
                for (var lane = -1; lane <= 1; lane++)
                    AddObject(TrackObjectKind.ItemBox, distance, lane * 3.4f, 0f, RacerId.Player, (KartItem)(1 + (index + lane + 3) % 3));
                AddObject(TrackObjectKind.Crate, distance + 26f, index % 2 == 0 ? 3.7f : -3.7f);
            }
        }

        private TrackObject AddObject(TrackObjectKind kind, float distance, float lane, float lifetime = 0f, RacerId owner = RacerId.Player, KartItem item = KartItem.None)
        {
            var obj = new TrackObject { Id = ++_objectId, Kind = kind, Distance = distance, Lane = lane, Lifetime = lifetime, Owner = owner, Item = item };
            Objects.Add(obj);
            return obj;
        }

        private void AvoidObstacles(RacerState racer, float dt)
        {
            if (racer.Finished || racer.DisabledSeconds > 0f) return;
            var desired = racer.Id == RacerId.Boss ? -2f : 2f;
            foreach (var obj in Objects)
            {
                if (!obj.Active || obj.Distance < racer.Distance || obj.Distance - racer.Distance > 18f) continue;
                if (obj.Kind == TrackObjectKind.ItemBox && racer.Item == KartItem.None && Math.Abs(obj.Lane - desired) < 2f) desired = obj.Lane;
                if (obj.Kind == TrackObjectKind.Crate && Math.Abs(obj.Lane - desired) < 1.8f) desired = obj.Lane > 0f ? 0.8f : -0.8f;
            }
            racer.Lane = MoveTowards(racer.Lane, desired, dt * 2f);
        }

        private void UpdateItems(float dt, bool useItem)
        {
            if (Phase != RacePhase.Racing) return;
            if (useItem && !Player.Finished && Player.DisabledSeconds <= 0f) UseItem(Player);
            for (var i = 1; i < Racers.Length; i++)
            {
                var cpu = Racers[i];
                if (!cpu.Finished && cpu.DisabledSeconds <= 0f && cpu.Item != KartItem.None && Time >= _cpuUseAt[i]) UseItem(cpu);
            }
            var count = Objects.Count;
            for (var i = 0; i < count && Phase == RacePhase.Racing; i++)
            {
                var obj = Objects[i];
                if (!obj.Active) continue;
                if (obj.Kind == TrackObjectKind.Rocket) { MoveRocket(obj, dt); continue; }
                if (obj.Kind == TrackObjectKind.Papers || obj.Kind == TrackObjectKind.Explosion)
                {
                    obj.Lifetime -= dt;
                    if (obj.Lifetime <= 0f) { obj.Active = false; continue; }
                }
                if (obj.Kind == TrackObjectKind.Explosion) continue;
                for (var j = 0; j < Racers.Length; j++)
                {
                    var racer = Racers[j];
                    if (racer.Finished || racer.DisabledSeconds > 0f) continue;
                    var min = Math.Min(_previousDistances[j], racer.Distance) - 1f;
                    var max = Math.Max(_previousDistances[j], racer.Distance) + 1f;
                    if (obj.Distance < min || obj.Distance > max || Math.Abs(obj.Lane - racer.Lane) > 1.25f) continue;
                    if (obj.Kind == TrackObjectKind.ItemBox)
                    {
                        if (racer.Item != KartItem.None) continue;
                        racer.Item = obj.Item;
                        _cpuUseAt[j] = Time + 0.8f;
                        obj.Active = false;
                    }
                    else if (obj.Kind == TrackObjectKind.Crate)
                    {
                        Disable(racer, false);
                        obj.Active = false;
                        AddObject(TrackObjectKind.Explosion, obj.Distance, obj.Lane, 0.5f);
                        if (racer.Id == RacerId.Junior && Time - _lastPush <= _settings.pushCreditSeconds) RegisterAttack("箱への押し出し成功");
                        if (racer.Id == RacerId.Player) RecordMisconduct("障害物に衝突して横転しました");
                    }
                    else if (obj.Kind == TrackObjectKind.Papers && (obj.Owner != racer.Id || obj.Lifetime < _settings.paperLifetime - 1f))
                    {
                        obj.Active = false;
                        Disable(racer, racer.Id != RacerId.Boss);
                        if (obj.Owner == RacerId.Player && racer.Id == RacerId.Junior) RegisterAttack("書類でスピンさせました");
                        if (obj.Owner == RacerId.Player && racer.Id == RacerId.Boss) Fail("上司があなたの書類で吹き飛びました", FailureScene.BossHit);
                        if (racer.Id == RacerId.Player) RecordMisconduct("スピンが見つかりました");
                    }
                    if (!obj.Active) break;
                }
            }
            Objects.RemoveAll(obj => !obj.Active && obj.Kind != TrackObjectKind.Crate && obj.Kind != TrackObjectKind.ItemBox);
        }

        private void UseItem(RacerState racer)
        {
            var item = racer.Item;
            var isDebugUnlimited = racer.Id == RacerId.Player && item == _settings.debugUnlimitedItem && item != KartItem.None;
            racer.Item = isDebugUnlimited ? item : KartItem.None;
            switch (item)
            {
                case KartItem.Papers:
                    AddObject(TrackObjectKind.Papers, racer.Distance - 3f, racer.Lane, _settings.paperLifetime, racer.Id);
                    break;
                case KartItem.Rocket:
                    AddObject(TrackObjectKind.Rocket, racer.Distance + 2.5f, racer.Lane, 4f, racer.Id);
                    break;
                case KartItem.Drink:
                    racer.TurboSeconds = 3f;
                    break;
            }
        }

        private void MoveRocket(TrackObject rocket, float dt)
        {
            var previous = rocket.Distance;
            rocket.Distance += _settings.rocketSpeed * dt;
            rocket.Lifetime -= dt;
            var hitDistance = float.MaxValue;
            foreach (var racer in Racers)
            {
                if (racer.Id == rocket.Owner || racer.Finished || Math.Abs(racer.Lane - rocket.Lane) > 1.25f) continue;
                if (racer.Distance >= previous - 1.2f && racer.Distance <= rocket.Distance + 1.2f) hitDistance = Math.Min(hitDistance, racer.Distance);
            }
            foreach (var obj in Objects)
            {
                if (!obj.Active || obj.Kind != TrackObjectKind.Crate || Math.Abs(obj.Lane - rocket.Lane) > 1.25f) continue;
                if (obj.Distance >= previous - 1f && obj.Distance <= rocket.Distance + 1f) hitDistance = Math.Min(hitDistance, obj.Distance);
            }
            if (hitDistance < float.MaxValue)
            {
                rocket.Distance = hitDistance;
                Explode(rocket);
            }
            else if (rocket.Lifetime <= 0f || rocket.Distance > _settings.courseLength) rocket.Active = false;
        }

        private void Explode(TrackObject rocket)
        {
            rocket.Active = false;
            foreach (var racer in Racers)
            {
                if (racer.Finished || DistanceSquared(racer.Distance, racer.Lane, rocket.Distance, rocket.Lane) > _settings.explosionRadius * _settings.explosionRadius) continue;
                Disable(racer, false, true);
                if (rocket.Owner == RacerId.Player && racer.Id == RacerId.Boss) Fail("ロケランの爆発に上司を巻き込みました", FailureScene.BossHit);
                if (rocket.Owner == RacerId.Player && racer.Id == RacerId.Junior) RegisterAttack("ロケランで部下を横転させました");
                if (racer.Id == RacerId.Player) RecordMisconduct("爆発に巻き込まれて横転しました");
            }
            foreach (var obj in Objects)
                if (obj.Kind == TrackObjectKind.Crate && DistanceSquared(obj.Distance, obj.Lane, rocket.Distance, rocket.Lane) <= _settings.explosionRadius * _settings.explosionRadius) obj.Active = false;
            AddObject(TrackObjectKind.Explosion, rocket.Distance, rocket.Lane, 0.6f, rocket.Owner, KartItem.Rocket);
        }

        private void Disable(RacerState racer, bool spinning, bool severe = false)
        {
            racer.IsSpinning = spinning;
            if (spinning)
            {
                racer.IsCourseOut = false;
                racer.DisabledSeconds = _settings.spinSeconds;
            }
            else
            {
                racer.IsCourseOut = severe || Math.Abs(racer.Lane) > _settings.roadHalfWidth - _settings.knockbackEdgeMargin;
                racer.DisabledSeconds = _settings.knockbackFlightSeconds + _settings.knockbackRecoverySeconds;
            }
            racer.Speed = 0f;
        }
        private float DistanceSquared(float a, float x, float b, float y) => (a - b) * (a - b) + (x - y) * (x - y);
    }
}
