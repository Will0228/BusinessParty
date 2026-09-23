using System;
using System.Collections.Generic;

namespace MixVerse.Game.Model.Kart
{
    public enum TrackObjectKind { Crate, ItemBox, Papers, Rocket, Mine, Explosion }

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
        public int VisualIndex;
        public float CreatedAt;
        public float StartDistance;
        public float StartLane;
    }

    public sealed partial class KartRace
    {
        private int _objectId;
        private readonly float[] _cpuUseAt = new float[3];
        public List<TrackObject> Objects { get; } = new List<TrackObject>();
        public float PlayerBlindSeconds { get; private set; }
        public int PaperAttackSerial { get; private set; }
        public RacerId PaperAttackSource { get; private set; }

        private void BuildObjects()
        {
            for (var distance = 45f; distance < _settings.courseLength - 25f; distance += 60f)
            {
                var index = (int)((distance - 45f) / 60f);
                for (var lane = -1; lane <= 1; lane++)
                    AddObject(TrackObjectKind.ItemBox, distance, lane * 3.4f, 0f, RacerId.Player, (KartItem)(1 + (index + lane + 4) % 4));
                AddObject(TrackObjectKind.Crate, distance + 26f, index % 2 == 0 ? 3.7f : -3.7f);
            }
        }

        private TrackObject AddObject(TrackObjectKind kind, float distance, float lane, float lifetime = 0f, RacerId owner = RacerId.Player, KartItem item = KartItem.None)
        {
            var obj = new TrackObject
            {
                Id = ++_objectId,
                Kind = kind,
                Distance = distance,
                Lane = lane,
                Lifetime = lifetime,
                Owner = owner,
                Item = item,
                CreatedAt = Time,
                StartDistance = distance,
                StartLane = lane
            };
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
            PlayerBlindSeconds = Math.Max(0f, PlayerBlindSeconds - dt);
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
                if (obj.Kind == TrackObjectKind.Papers || obj.Kind == TrackObjectKind.Mine || obj.Kind == TrackObjectKind.Explosion)
                {
                    obj.Lifetime -= dt;
                    if (obj.Lifetime <= 0f) { obj.Active = false; continue; }
                }
                if (obj.Kind == TrackObjectKind.Explosion) continue;
                if (obj.Kind == TrackObjectKind.Mine && Time - obj.CreatedAt < _settings.mineFlightSeconds) continue;
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
                    else if (obj.Kind == TrackObjectKind.Mine)
                    {
                        Explode(obj, KartItem.Mine);
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
                    UsePapers(racer);
                    break;
                case KartItem.Rocket:
                    AddObject(TrackObjectKind.Rocket, racer.Distance + 2.5f, racer.Lane, 4f, racer.Id);
                    break;
                case KartItem.Mine:
                    UseMines(racer);
                    break;
                case KartItem.Drink:
                    racer.TurboSeconds = 3f;
                    break;
            }
        }

        private void UseMines(RacerState racer)
        {
            var half = (_settings.mineCount - 1) * 0.5f;
            for (var i = 0; i < _settings.mineCount; i++)
            {
                var spread = half <= 0f ? 0f : (i - half) / half;
                var forward = _settings.mineFanDistance - Math.Abs(spread) * 3f;
                var lane = Clamp(racer.Lane + spread * _settings.mineFanWidth, -_settings.roadHalfWidth + 0.7f, _settings.roadHalfWidth - 0.7f);
                var mine = AddObject(TrackObjectKind.Mine, racer.Distance + forward, lane, _settings.mineLifetime, racer.Id, KartItem.Mine);
                mine.StartDistance = racer.Distance + 1f;
                mine.StartLane = racer.Lane;
                mine.VisualIndex = i;
            }
        }

        private void UsePapers(RacerState racer)
        {
            const int sheetCount = 5;
            for (var i = 0; i < sheetCount; i++)
            {
                var sheet = AddObject(TrackObjectKind.Papers, racer.Distance, racer.Lane, _settings.paperEffectSeconds, racer.Id, KartItem.Papers);
                sheet.VisualIndex = i;
            }
            if (racer.Id == RacerId.Player)
            {
                Boss.SlowedSeconds = Math.Max(Boss.SlowedSeconds, _settings.cpuPaperSlowSeconds);
                Junior.SlowedSeconds = Math.Max(Junior.SlowedSeconds, _settings.cpuPaperSlowSeconds);
                return;
            }
            if (racer.Distance <= Player.Distance) return;
            PlayerBlindSeconds = Math.Max(PlayerBlindSeconds, _settings.paperEffectSeconds);
            PaperAttackSource = racer.Id;
            PaperAttackSerial++;
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
                Explode(rocket, KartItem.Rocket);
            }
            else if (rocket.Lifetime <= 0f || rocket.Distance > _settings.courseLength) rocket.Active = false;
        }

        private void Explode(TrackObject explosive, KartItem item)
        {
            explosive.Active = false;
            foreach (var racer in Racers)
            {
                if (racer.Finished || DistanceSquared(racer.Distance, racer.Lane, explosive.Distance, explosive.Lane) > _settings.explosionRadius * _settings.explosionRadius) continue;
                Disable(racer, false, true);
                var itemName = item == KartItem.Mine ? "地雷" : "ロケラン";
                if (explosive.Owner == RacerId.Player && racer.Id == RacerId.Boss) Fail(itemName + "の爆発に上司を巻き込みました", FailureScene.BossHit);
                if (explosive.Owner == RacerId.Player && racer.Id == RacerId.Junior) RegisterAttack(itemName + "で部下を横転させました");
                if (racer.Id == RacerId.Player) RecordMisconduct("爆発に巻き込まれて横転しました");
            }
            foreach (var obj in Objects)
                if (obj.Kind == TrackObjectKind.Crate && DistanceSquared(obj.Distance, obj.Lane, explosive.Distance, explosive.Lane) <= _settings.explosionRadius * _settings.explosionRadius) obj.Active = false;
            AddObject(TrackObjectKind.Explosion, explosive.Distance, explosive.Lane, 0.6f, explosive.Owner, item);
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
