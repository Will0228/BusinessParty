using System;

namespace MixVerse.Game.Model
{
    public sealed class CpuState
    {
        public CpuRole Role { get; }
        public int MaxHealth { get; }
        public int Health { get; private set; }
        public bool IsDepleted => Health == 0;

        public CpuState(CpuRole role, int maxHealth)
        {
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            Role = role;
            MaxHealth = maxHealth;
            Reset();
        }

        public void Reset() => Health = MaxHealth;

        public int TakeDamage(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            var before = Health;
            Health = Math.Max(0, Health - damage);
            return before - Health;
        }
    }
}
