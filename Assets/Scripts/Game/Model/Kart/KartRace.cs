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
        private float _lastPush = -100f;
        private int _juniorPassingSide;
        private int _direction = 1;
        private int _driftDirection;
        private float _driftTime;
        private float _accumulator;
        private bool _pendingItem;
        private int _pendingDriftRelease;
        private int _pendingDriftPulses;
        private readonly float[] _previousDistances = new float[3];

        public RacerState[] Racers { get; } =
        {
            new RacerState { Id = RacerId.Player, Lane = 0f },
            new RacerState { Id = RacerId.Boss },
            new RacerState { Id = RacerId.Junior }
        };
        public RacerState Player => Racers[0];
        public RacerState Boss => Racers[1];
        public RacerState Junior => Racers[2];
        public KartJumpRamp Jump { get; }
        public RacePhase Phase { get; private set; }
        public float Time { get; private set; }
        public float TailgateTime => _tailgateTime;
        public float DriftTime => _driftTime;
        public int DriftTier => _driftTime >= _settings.driftSecondSeconds ? 2 : _driftTime >= _settings.driftFirstSeconds ? 1 : 0;
        public bool IsDrifting => _driftDirection != 0;
        public bool IsOnRoadEdge { get; private set; }
        public int DriftDirection => _driftDirection;
        public int Direction => _direction;
        public int Attacks { get; private set; }
        public float FinishGap { get; private set; }
        public float MaximumDistance => Math.Max(Math.Abs(Player.Distance - Boss.Distance), Math.Abs(Junior.Distance - Boss.Distance));
        public float RaceBossDistanceLimit => _settings.maximumBossDistance * _settings.raceBossDistanceMultiplier;
        public float GoalBossDistanceLimit => _settings.maximumBossDistance;
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
            Lanes = new KartCourseLanes(_settings.roadHalfWidth);
            Jump = new KartJumpRamp(_settings);
            _random = new Random(seed);
            BuildObjects();
            InitializeCpuLanes();
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
            if (input.ReleasedDriftDirection != 0) _pendingDriftRelease = input.ReleasedDriftDirection;
            _pendingDriftPulses += input.DriftPulses;
            _accumulator += Math.Min(deltaTime, 0.25f);
            const float step = 1f / 120f;
            while (_accumulator >= step && Phase == RacePhase.Racing)
            {
                input.UseItem = _pendingItem;
                input.ReleasedDriftDirection = _pendingDriftRelease;
                input.DriftPulses = _pendingDriftPulses;
                Step(step, input);
                _pendingItem = false;
                _pendingDriftRelease = 0;
                _pendingDriftPulses = 0;
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
                var previous = racer.Distance;
                if (racer.DisabledSeconds > 0f) racer.DisabledSeconds = Math.Max(0f, racer.DisabledSeconds - dt);
                else if (!racer.Finished)
                {
                    racer.Speed = MoveTowards(racer.Speed, 0f, dt * 8f);
                    racer.Distance = Clamp(racer.Distance + racer.Speed * _settings.metersPerSpeedUnit * dt, 0f, _settings.courseLength);
                }
                UpdateJump(racer, previous, dt);
            }
        }

        private void Step(float dt, KartInput input)
        {
            Time += dt;
            for (var i = 0; i < Racers.Length; i++) _previousDistances[i] = Racers[i].Distance;
            if (input.Master < 0.35f) _direction = -1;
            else if (input.Master > 0.65f) _direction = 1;
            UpdateDrift(dt, input.Jog, input.ReleasedDriftDirection);
            var steer = Math.Abs(input.Steering) < _settings.steeringDeadZone ? 0f : input.Steering;
            steer = Math.Sign(steer) * steer * steer;
            var onRoadEdge = false;
            if (Player.DisabledSeconds <= 0f && !Player.Finished)
            {
                // ステアリング/ドリフト入力だけが機首の向き(HeadingOffset、トラック接線からの相対角)を動かす。
                // 無操作ならHeadingOffsetはトラックの方位変化をそのまま打ち消す方向に動く＝ワールド座標上の機首は一定に保たれる。
                var driftYawRate = input.PulseDrift ? 0f : _driftDirection * _settings.driftYawRate;
                var yawRate = (steer * _settings.steeringYawRate * SteeringAuthority(steer) + driftYawRate) * _direction;
                var trackYawRatePerSecond = _layout.TurnRateAt(Player.Distance) * Player.Speed * _settings.metersPerSpeedUnit;
                var maxHeadingOffset = _settings.maxHeadingOffsetDegrees * (float)Math.PI / 180f;
                var driftPulseOffset = DriftPulseOffset(input.DriftPulses) * _direction;
                Player.HeadingOffset = Clamp(Player.HeadingOffset + (yawRate - trackYawRatePerSecond) * dt + driftPulseOffset, -maxHeadingOffset, maxHeadingOffset);
                Player.Lane += Player.Speed * _settings.metersPerSpeedUnit * (float)Math.Sin(Player.HeadingOffset) * dt;
                // 見た目の車体傾きは移動計算用のHeadingOffsetから作るが、非ドリフト時は最大15度、ドリフト時も最大40度程度まで抑えて回転を緩める。
                var visualMaxHeadingOffset = _driftDirection != 0
                    ? _settings.driftMaxVisualTiltDegrees * (float)Math.PI / 180f
                    : _settings.nonDriftMaxVisualTiltDegrees * (float)Math.PI / 180f;
                Player.VisualHeadingOffset = Clamp(Player.HeadingOffset, -visualMaxHeadingOffset, visualMaxHeadingOffset);
                if (Math.Abs(Player.Lane) > _settings.roadHalfWidth - 0.7f)
                {
                    Player.Lane = Clamp(Player.Lane, -_settings.roadHalfWidth + 0.7f, _settings.roadHalfWidth - 0.7f);
                    onRoadEdge = true;
                    RecordMisconduct("壁に激突しました");
                }
            }
            var bossTarget = SectionAt(Boss.Distance) == CourseSection.Uphill ? _settings.BossUphillSpeed : _settings.BossMaxSpeed;
            var juniorTarget = _settings.JuniorMaxSpeed;
            var ahead = Player.Distance - Junior.Distance;
            var blocking = !Player.Finished && ahead > 0f && ahead < _settings.blockingDistance &&
                           Math.Abs(Player.Lane - Junior.Lane) < _settings.juniorOvertakeLaneOffset && Player.Speed < juniorTarget;
            if (blocking)
            {
                juniorTarget = Math.Max(0f, Math.Min(_settings.JuniorMaxSpeed, Player.Speed) - (ahead < 3f ? 15f : 0f));
                _blockingTime += dt;
                if (_blockingTime >= _settings.blockingSeconds) { RegisterAttack("ブロッキング成功"); _blockingTime = 0f; }
                if (_juniorPassingSide == 0) _juniorPassingSide = PassingSide();
            }
            else _blockingTime = 0f;
            UpdateCpuLaneTarget(Boss, dt);
            UpdateCpuLaneTarget(Junior, dt);
            AvoidObstacles(Boss, dt);
            UpdateJuniorLane(dt);
            var playerTarget = Clamp(input.Gain, 0f, 1f) * _settings.PlayerMaxSpeed * _direction;
            if (onRoadEdge) playerTarget *= _settings.roadEdgeSpeedMultiplier;
            Move(Player, playerTarget, dt);
            Move(Boss, bossTarget, dt);
            Move(Junior, juniorTarget, dt, blocking);
            for (var i = 0; i < Racers.Length; i++) UpdateJump(Racers[i], _previousDistances[i], dt);
            IsOnRoadEdge = onRoadEdge;
            if (Phase != RacePhase.Racing) return;
            CheckContacts(dt);
            if (_direction < 0 && Player.Speed < -1f) RecordMisconduct("バック走行が見つかりました");
            UpdateItems(dt, input.UseItem);
            CheckRules(dt);
        }

        private void Move(RacerState racer, float target, float dt, bool capped = false)
        {
            if (racer.Finished) return;
            racer.TurboSeconds = Math.Max(0f, racer.TurboSeconds - dt);
            if (racer.TurboSeconds > 0f && !capped) target *= _settings.turboSpeedMultiplier;
            if (racer.DisabledSeconds > 0f)
            {
                racer.DisabledSeconds = Math.Max(0f, racer.DisabledSeconds - dt);
                racer.Speed = 0f;
                return;
            }
            if (!racer.IsAirborne)
                racer.Speed = MoveTowards(racer.Speed, target, dt * (Math.Abs(target) < Math.Abs(racer.Speed) ? _settings.braking : _settings.acceleration * (racer.TurboSeconds > 0f ? 1.5f : 1f)));
            var previous = racer.Distance;
            racer.Distance = Math.Max(0f, previous + racer.Speed * _settings.metersPerSpeedUnit * dt);
            if (racer.Distance >= _settings.courseLength)
            {
                racer.FinishTime = Time - dt + dt * (_settings.courseLength - previous) / Math.Max(0.0001f, racer.Distance - previous);
                racer.Distance = _settings.courseLength;
            }
        }

        private void UpdateDrift(float dt, int jog, int releasedDirection)
        {
            var canDrift = Player.DisabledSeconds <= 0f && !Player.Finished && Player.Speed >= 20f && _direction > 0;
            jog = canDrift ? Math.Sign(jog) : 0;
            var released = _driftDirection != 0 && (jog == 0 || releasedDirection == _driftDirection);
            if (released && canDrift && DriftTier > 0)
                Player.TurboSeconds = Math.Max(Player.TurboSeconds, _settings.turboSeconds * DriftTier);
            if (_driftDirection != jog || released) _driftTime = 0f;
            _driftDirection = jog;
            if (jog != 0) _driftTime += dt;
        }

        private void CheckContacts(float dt)
        {
            if (!Player.Finished && !Boss.Finished && SameLevel(Player, Boss) && Math.Abs(Player.Distance - Boss.Distance) < 2.1f && Math.Abs(Player.Lane - Boss.Lane) < 1.35f)
                Fail("上司のカートに接触しました");
            ResolveJuniorContact(dt);
        }

        private void UpdateJuniorLane(float dt)
        {
            if (!CanChangeLane(Junior)) return;
            if (_juniorPassingSide != 0)
            {
                var roadEdge = _settings.roadHalfWidth - 0.7f;
                var targetLane = Clamp(Player.Lane + _juniorPassingSide * _settings.juniorOvertakeLaneOffset, -roadEdge, roadEdge);
                Junior.Lane = MoveTowards(Junior.Lane, targetLane, dt * _settings.juniorOvertakeSteeringSpeed);
                if (Junior.Distance > Player.Distance + _settings.racerContactLength ||
                    Player.Distance - Junior.Distance > _settings.blockingDistance * 1.5f)
                    _juniorPassingSide = 0;
                return;
            }

            if (Time - _lastPush > _settings.pushCreditSeconds) AvoidObstacles(Junior, dt);
        }

        private int PassingSide()
        {
            var roadEdge = _settings.roadHalfWidth - 0.7f;
            var leftSpace = Player.Lane + roadEdge;
            var rightSpace = roadEdge - Player.Lane;
            return rightSpace >= leftSpace ? 1 : -1;
        }

        private void ResolveJuniorContact(float dt)
        {
            if (Player.Finished || Junior.Finished || Player.IsFalling || Junior.IsFalling) return;
            var laneDifference = Junior.Lane - Player.Lane;
            if (Math.Abs(laneDifference) >= _settings.racerContactWidth) return;

            var previousDifference = _previousDistances[(int)RacerId.Player] - _previousDistances[(int)RacerId.Junior];
            var currentDifference = Player.Distance - Junior.Distance;
            var crossed = previousDifference * currentDifference <= 0f;
            if (!crossed && Math.Abs(currentDifference) >= _settings.racerContactLength) return;

            if (Math.Abs(laneDifference) > 0.65f)
            {
                var side = laneDifference > 0f ? 1f : -1f;
                Junior.Lane = Clamp(Junior.Lane + side * dt * 6f, -_settings.roadHalfWidth + 0.7f, _settings.roadHalfWidth - 0.7f);
                _lastPush = Time;
                return;
            }

            var playerWasAhead = previousDifference >= 0f;
            if (playerWasAhead)
            {
                Junior.Distance = Math.Max(0f, Player.Distance - _settings.racerContactLength);
                Junior.Speed = Math.Min(Junior.Speed, Math.Max(0f, Player.Speed));
            }
            else
            {
                Player.Distance = Math.Max(0f, Junior.Distance - _settings.racerContactLength);
                Player.Speed = Math.Min(Player.Speed, Math.Max(0f, Junior.Speed));
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
            if (MaximumDistance >= RaceBossDistanceLimit) Fail("上司との距離が離れすぎました", FailureScene.Distance);
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
            if (MaximumDistance >= GoalBossDistanceLimit) { Fail("ゴール時に上司との距離が離れすぎていました"); return; }
            if (CameraEvidence.Count > 0) { Fail("監視カメラの確認：" + CameraEvidence[0]); return; }
            Phase = RacePhase.Cleared;
            ResultReason = "接待成功！ 上司が僅差で1位です。";
        }

        private void RegisterAttack(string message)
        {
            Attacks++;
            Message = message + " — 部下への妨害はセーフ";
        }
        private void RecordMisconduct(string reason)
        {
            if (InCamera(Player.Distance) && !CameraEvidence.Contains(reason)) CameraEvidence.Add(reason);
        }
        private void Fail(string reason, FailureScene scene = FailureScene.None)
        {
            if (Phase != RacePhase.Racing) return;
            Phase = RacePhase.Failed;
            ResultReason = reason;
            ResultScene = scene;
        }
        private bool SameLevel(RacerState a, RacerState b) => Math.Abs(a.Height - b.Height) < 1.2f;
        // ステアリングだけでは機首を大きく振らせない。steeringMaxHeadingOffsetDegrees に近づくほど効きを弱め、
        // 入力を入れ続けても浅い傾きのまま横へ流れるだけに留める（ドリフトの振り角はこの制限を受けない）
        private float SteeringAuthority(float steer)
        {
            if (steer == 0f) return 0f;
            var limit = _settings.steeringMaxHeadingOffsetDegrees * (float)Math.PI / 180f;
            var ratio = Clamp(Player.HeadingOffset / limit, -1f, 1f);
            return steer * ratio * _direction > 0f ? 1f - Math.Abs(ratio) : 1f;
        }

        private float DriftPulseOffset(int pulses)
        {
            if (_driftDirection == 0 || Math.Sign(pulses) != _driftDirection) return 0f;
            return pulses * _settings.driftTiltDegreesPerInput * (float)Math.PI / 180f;
        }

        private float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        private float MoveTowards(float value, float target, float step) => value + Clamp(target - value, -step, step);
    }
}
