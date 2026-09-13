using System;

namespace MixVerse.Game.Model
{
    public sealed class PlayerFacing
    {
        private const float EndpointTolerance = 0.002f;
        public float Value { get; private set; } = 0.5f;
        public CpuRole Direction => Value >= 0.5f ? CpuRole.Junior : CpuRole.Senior;
        public float Amount => Math.Abs(Value - 0.5f) * 2;
        public CpuRole? Target => Amount >= 1 - EndpointTolerance ? Direction : (CpuRole?)null;

        public void Set(float value) => Value = Math.Max(0, Math.Min(1, value));
        public bool CanActOn(CpuRole role) => Target == role;
    }
}
