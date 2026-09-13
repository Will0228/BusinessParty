using UnityEngine;

namespace MixVerse.Game
{
    [CreateAssetMenu(fileName = "PartyGameSettings", menuName = "Business Party/Game Settings")]
    public sealed class PartyGameSettings : ScriptableObject
    {
        [Min(1)] public int maxHealth = 100;
        [Min(1)] public int missedApplauseDamage = 20;
        [Min(0.1f)] public float responseSeconds = 3f;
        [Min(1)] public int requiredClaps = 1;
        [Min(0.1f)] public float minTalkInterval = 6f;
        [Min(0.1f)] public float maxTalkInterval = 12f;
        [Min(0.1f)] public float missingVoiceDuration = 2f;
        [Min(0.1f)] public float reactionDuration = 1.5f;
        [Min(0.1f)] public float gunAimSeconds = 1.5f;
        [Min(0.1f)] public float shotShakeSeconds = 0.75f;
        [Min(0)] public float shotShakeAngle = 18f;
        [Min(0)] public float shotShakeDistance = 0.35f;
        [Min(0.1f)] public float resultSeconds = 4f;

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            missedApplauseDamage = Mathf.Max(1, missedApplauseDamage);
            requiredClaps = Mathf.Max(1, requiredClaps);
            responseSeconds = Mathf.Max(0.1f, responseSeconds);
            minTalkInterval = Mathf.Max(0.1f, minTalkInterval);
            maxTalkInterval = Mathf.Max(minTalkInterval, maxTalkInterval);
        }
    }
}
