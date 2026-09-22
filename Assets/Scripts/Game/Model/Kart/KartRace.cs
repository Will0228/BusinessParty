using System;
using System.Collections.Generic;

namespace MixVerse.Game.Model.Kart
{
    public sealed partial class KartRace
    {
        private readonly KartRaceSettings _settings;
        private readonly KartCourseLayout _layout;
        private readonly Random _random;
        private float _blockingTime;
        private float _tailgateTime;
        private float _nextSlip;
        private float _lastPush = -100f;
        private int _direction = 1;
        private int _driftDirection;
        private float _driftTime;
        private float _accumulator;
        private bool _pendingItem;
        private readonly float[] _previousDistances = new float[3];

        public RacerState[] Racers { get; } =
        {
            new RacerState { Id = RacerId.Player, Lane = 2f },
            new RacerState { Id = RacerId.Boss, Distance = 8f, Lane = -2f },
            new RacerState { Id = RacerId.Junior, Distance = 3f, Lane = 2f }
        };
        public RacerState Player => Racers[0];
        public RacerState Boss => Racers[1];
        public RacerState Junior => Racers[2];
        public RacePhase Phase { get; private set; }
        public float Time { get; private set; }
        public float SlipRemaining { get; private set; }
        public float TailgateTime => _tailgateTime;
        public float DriftTime => _driftTime;
        public int DriftTier => _driftTime >= _settings.driftSecondSeconds ? 2 : _driftTime >= _settings.driftFirstSeconds ? 1 : 0;
        public bool IsDrifting => _driftDirection != 0;
        public int Direction => _direction;
        public int Attacks { get; private set; }
        public float FinishGap { get; private set; }
        public float MaximumDistance => Math.Max(Math.Abs(Player.Distance - Boss.Distance), Math.Abs(Junior.Distance - Boss.Distance));
        public string Message { get; private set; } = "部下を止めて、上司に花を持たせよう。";
        public string ResultReason { get; private set; } = "";
        public FailureScene ResultScene { get; private set; }
        public float ResultTime { get; private set; }
        public List<string> CameraEvidence { get; } = new List<string>();
        public int BossMood => Phase == RacePhase.Failed ? 0 : 1;

        public KartRace(KartRaceSettings settings, int seed = 0)
        {
            _settings = settings;
            _settings.Validate();
            _layout = new KartCourseLayout(_settings.courseLength);
            _random = new Random(seed);
            ScheduleSlip();
            BuildObjects();
            if (_settings.debugUnlimitedItem != KartItem.None) Player.Item = _settings.debugUnlimitedItem;
        }

        public CourseSection SectionAt(float distance) => (CourseSection)Math.Min(5, Math.Max(0, (int)(distance / (_settings.courseLength / 6f))));
        public bool InCamera(float distance) => _settings.cameraReviewEnabled && distance >= _settings.courseLength * 0.69f && distance < _settings.courseLength * 0.76f;
        public int Rank(RacerState racer)
        {
            var rank = 1;
            foreach (var other in Racers)
            {
                if (other == racer) continue;
                if (other.Finished && (!racer.Finished || other.FinishTime < racer.FinishTime) ||
                    !other.Finished && !racer.Finished && other.Distance > racer.Distance) rank++;
            }
            return rank;
        }

        public void Tick(float deltaTime, KartInput input)
        {
            if (deltaTime <= 0f) return;
            if (Phase == RacePhase.Failed)
            {
                AdvanceFailure(Math.Min(deltaTime, 0.25f));
                return;
            }
            if (Phase != RacePhase.Racing) return;
            _pendingItem |= input.UseItem;
            _accumulator += Math.Min(deltaTime, 0.25f);
            const float step = 1f / 120f;
            while (_accumulator >= step && Phase == RacePhase.Racing)
            {
                input.UseItem = _pendingItem;
                Step(step, input);
                _pendingItem = false;
                _accumulator -= step;
            }
        }

        private void AdvanceFailure(float dt)
        {
            var previousTime = ResultTime;
            ResultTime += dt;
            if (ResultScene == FailureScene.Distance && previousTime < 0.8f && ResultTime >= 0.8f)
                Disable(Player, false);
            foreach (var racer in Racers)
            {
                if (racer.DisabledSeconds > 0f)
                {
                    racer.DisabledSeconds = Math.Max(0f, racer.DisabledSeconds - dt);
                    continue;
                }
                if (racer.Finished) continue;
                racer.Speed = MoveTowards(racer.Speed, 0f, dt * 8f);
                racer.Distance = Clamp(racer.Distance + racer.Speed * _settings.metersPerSpeedUnit * dt, 0f, _settings.courseLength);
            }
        }

        private void Step(float dt, KartInput input)
        {
            Time += dt;
            for (var i = 0; i < Racers.Length; i++) _previousDistances[i] = Racers[i].Distance;
            if (input.Master < 0.35f) _direction = -1;
            else if (input.Master > 0.65f) _direction = 1;
            UpdateDrift(dt, input.Jog);
            var steer = Math.Abs(input.Steering) < _settings.steeringDeadZone ? 0f : input.Steering;
            steer = Math.Sign(steer) * steer * steer;
            if (Player.DisabledSeconds <= 0f && !Player.Finished)
            {
                var movement = Math.Min(1f, Math.Abs(Player.Speed) / 25f);
                var turnRate = _layout.TurnRateAt(Player.Distance);
                var driftAssist = _driftDirection != 0 && _driftDirection == Math.Sign(turnRate) ? 0.38f : 1f;
                var outwardSlip = -Math.Sign(turnRate) * Math.Max(0f, Math.Abs(Player.Speed) - 30f) * 0.18f *
                                  Math.Min(2f, Math.Abs(turnRate) / 0.07854f) * driftAssist;
                Player.Lane += ((steer + _driftDirection * 0.4f) * _settings.steeringSpeed * movement * _direction + outwardSlip) * dt;
                if (Math.Abs(Player.Lane) > _settings.roadHalfWidth - 0.7f)
                {
                    Player.Lane = Clamp(Player.Lane, -_settings.roadHalfWidth + 0.7f, _settings.roadHalfWidth - 0.7f);
                    Player.Speed *= 0.6f;
                    RecordMisconduct("壁に激突しました", false);
                }
            }
            var bossTarget = SectionAt(Boss.Distance) == CourseSection.Uphill ? 44f : 50f;
            if (Boss.TurboSeconds > 0f) bossTarget *= 1.2f;
            var juniorTarget = 70f;
            var ahead = Player.Distance - Junior.Distance;
            var blocking = !Player.Finished && ahead > 0f && ahead < _settings.blockingDistance && Math.Abs(Player.Lane - Junior.Lane) < 1.45f && Player.Speed < 70f;
            if (blocking)
            {
                juniorTarget = Math.Max(0f, Math.Min(70f, Player.Speed) - (ahead < 3f ? 15f : 0f));
                _blockingTime += dt;
                if (_blockingTime >= _settings.blockingSeconds) { RegisterAttack("ブロッキング成功"); _blockingTime = 0f; }
                if (ahead < 6f) Junior.Lane = MoveTowards(Junior.Lane, Player.Lane > 0f ? -1f : 3.5f, dt * 0.75f);
            }
            else _blockingTime = 0f;
            AvoidObstacles(Boss, dt);
            if (!blocking && Time - _lastPush > _settings.pushCreditSeconds) AvoidObstacles(Junior, dt);
            Move(Player, Clamp(input.Gain, 0f, 1f) * (Player.TurboSeconds > 0f ? 120f : 100f) * _direction, dt);
            Move(Boss, bossTarget, dt);
            Move(Junior, juniorTarget, dt);
            if (Phase != RacePhase.Racing) return;
            CheckContacts(dt);
            if (_direction < 0 && Player.Speed < -1f) RecordMisconduct("バック走行が見つかりました");
            UpdateItems(dt, input.UseItem);
            CheckRules(dt);
        }

        private void Move(RacerState racer, float target, float dt)
        {
            if (racer.Finished) return;
            racer.TurboSeconds = Math.Max(0f, racer.TurboSeconds - dt);
            if (racer.DisabledSeconds > 0f)
            {
                racer.DisabledSeconds = Math.Max(0f, racer.DisabledSeconds - dt);
                racer.Speed = 0f;
                return;
            }
            racer.Speed = MoveTowards(racer.Speed, target, dt * (Math.Abs(target) < Math.Abs(racer.Speed) ? _settings.braking : _settings.acceleration * (racer.TurboSeconds > 0f ? 1.5f : 1f)));
            var previous = racer.Distance;
            racer.Distance = Math.Max(0f, previous + racer.Speed * _settings.metersPerSpeedUnit * dt);
            if (racer.Distance >= _settings.courseLength)
            {
                racer.FinishTime = Time - dt + dt * (_settings.courseLength - previous) / Math.Max(0.0001f, racer.Distance - previous);
                racer.Distance = _settings.courseLength;
            }
        }

        private void UpdateDrift(float dt, int jog)
        {
            if (Player.DisabledSeconds > 0f || Player.Finished || Math.Abs(Player.Speed) < 20f) jog = 0;
            jog = Math.Sign(jog);
            if (_driftDirection != 0 && jog != _driftDirection) _driftTime = 0f;
            _driftDirection = jog;
            if (jog != 0) _driftTime += dt;
        }

        private void CheckContacts(float dt)
        {
            if (!Player.Finished && !Boss.Finished && Math.Abs(Player.Distance - Boss.Distance) < 2.1f && Math.Abs(Player.Lane - Boss.Lane) < 1.35f)
                Fail("上司のカートに接触しました");
            if (!Player.Finished && !Junior.Finished && Math.Abs(Player.Distance - Junior.Distance) < 2f && Math.Abs(Player.Lane - Junior.Lane) < 1.4f && Math.Abs(Player.Lane - Junior.Lane) > 0.65f)
            {
                var side = Junior.Lane >= Player.Lane ? 1f : -1f;
                Junior.Lane = Clamp(Junior.Lane + side * dt * 6f, -_settings.roadHalfWidth + 0.7f, _settings.roadHalfWidth - 0.7f);
                _lastPush = Time;
            }
        }

        private void CheckRules(float dt)
        {
            if (Phase != RacePhase.Racing) return;
            var behind = Boss.Distance - Player.Distance;
            if (!Player.Finished && !Boss.Finished && behind > 0f && behind < _settings.tailgateDistance && Math.Abs(Player.Lane - Boss.Lane) < 1.6f)
                _tailgateTime += dt;
            else _tailgateTime = 0f;
            if (_tailgateTime >= _settings.tailgateSeconds) Fail("上司を煽り続けました");
            if (MaximumDistance >= _settings.maximumBossDistance) Fail("上司との距離が離れすぎました", FailureScene.Distance);
            if (SlipRemaining > 0f)
            {
                SlipRemaining -= dt;
                if (SlipRemaining <= 0f) Fail("失言を10秒以内にたしなめませんでした");
            }
            else if (Time >= _nextSlip && !Junior.Finished && Rank(Junior) == 1)
            {
                SlipRemaining = _settings.slipResponseSeconds;
                Message = "部下『課長、遅くないっすか？』 10秒以内に攻撃！";
                ScheduleSlip();
            }
            CheckFinish();
        }

        private void CheckFinish()
        {
            if (Phase != RacePhase.Racing) return;
            var first = float.MaxValue;
            foreach (var racer in Racers) if (racer.Finished) first = Math.Min(first, racer.FinishTime);
            if (first == float.MaxValue) return;
            if (!Boss.Finished || Boss.FinishTime > first) { Fail("上司が1位でゴールできませんでした"); return; }
            var second = float.MaxValue;
            if (Player.Finished) second = Math.Min(second, Player.FinishTime);
            if (Junior.Finished) second = Math.Min(second, Junior.FinishTime);
            FinishGap = (second < float.MaxValue ? second : Time) - Boss.FinishTime;
            if (FinishGap >= 3f) { Fail("上司と2位の着差が3秒以上でした"); return; }
            if (second == float.MaxValue) return;
            if (CameraEvidence.Count > 0) { Fail("監視カメラの確認：" + CameraEvidence[0]); return; }
            Phase = RacePhase.Cleared;
            ResultReason = "接待成功！ 上司が僅差で1位です。";
        }

        private void ScheduleSlip() => _nextSlip = Time + _settings.slipMinSeconds + (float)_random.NextDouble() * (_settings.slipMaxSeconds - _settings.slipMinSeconds);
        private void RegisterAttack(string message)
        {
            Attacks++;
            SlipRemaining = 0f;
            Message = message + " — 部下への妨害はセーフ";
            ScheduleSlip();
        }
        private void RecordMisconduct(string reason, bool failInGallery = true)
        {
            if (failInGallery && SectionAt(Player.Distance) == CourseSection.Gallery) Fail("ギャラリー区間：" + reason);
            if (InCamera(Player.Distance) && !CameraEvidence.Contains(reason)) CameraEvidence.Add(reason);
        }
        private void Fail(string reason, FailureScene scene = FailureScene.None)
        {
            if (Phase != RacePhase.Racing) return;
            Phase = RacePhase.Failed;
            ResultReason = reason;
            ResultScene = scene;
        }
        private float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        private float MoveTowards(float value, float target, float step) => value + Clamp(target - value, -step, step);
    }
}
