namespace MixVerse.Game.Model.Kart
{
    public sealed partial class KartRace
    {
        private readonly int[] _cpuLaneIndices = new int[3];
        private readonly float[] _cpuLaneChangeRemaining = new float[3];

        public KartCourseLanes Lanes { get; }

        private void InitializeCpuLanes()
        {
            _cpuLaneIndices[(int)RacerId.Boss] = 0;
            _cpuLaneIndices[(int)RacerId.Junior] = Lanes.Count - 1;
            foreach (var racer in new[] { Boss, Junior })
            {
                racer.Lane = Lanes.CenterAt(_cpuLaneIndices[(int)racer.Id]);
                ScheduleLaneChange(racer);
            }
        }

        private bool CanChangeLane(RacerState racer) => !racer.Finished && racer.DisabledSeconds <= 0f &&
                                                       !racer.IsAirborne && !racer.IsFalling;

        private void UpdateCpuLaneTarget(RacerState racer, float dt)
        {
            if (!CanChangeLane(racer)) return;
            var index = (int)racer.Id;
            _cpuLaneChangeRemaining[index] -= dt;
            if (_cpuLaneChangeRemaining[index] > 0f) return;
            var next = _random.Next(Lanes.Count - 1);
            if (next >= _cpuLaneIndices[index]) next++;
            _cpuLaneIndices[index] = next;
            ScheduleLaneChange(racer);
        }

        private void ScheduleLaneChange(RacerState racer)
        {
            _cpuLaneChangeRemaining[(int)racer.Id] = _settings.cpuLaneChangeMinSeconds +
                (float)_random.NextDouble() * (_settings.cpuLaneChangeMaxSeconds - _settings.cpuLaneChangeMinSeconds);
        }
    }
}
