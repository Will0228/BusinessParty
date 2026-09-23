using UnityEngine;

namespace MixVerse.Game.Kart
{
    public sealed class KartExplosionDebrisView : MonoBehaviour
    {
        private readonly Transform[] _chunks = new Transform[22];
        private readonly Vector3[] _velocities = new Vector3[22];
        private readonly Vector3[] _spins = new Vector3[22];
        private readonly Vector3[] _scales = new Vector3[22];
        private const float Gravity = 24f;

        public void Initialize(Mesh mesh, Material material, int seed)
        {
            var random = new System.Random(seed);
            for (var i = 0; i < _chunks.Length; i++)
            {
                var chunk = new GameObject("Asphalt chunk", typeof(MeshFilter), typeof(MeshRenderer));
                chunk.transform.SetParent(transform, false);
                chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
                chunk.GetComponent<MeshRenderer>().sharedMaterial = material;
                _chunks[i] = chunk.transform;
                var angle = (i + (float)random.NextDouble()) * Mathf.PI * 2f / _chunks.Length;
                var speed = 3f + (float)random.NextDouble() * 6f;
                _velocities[i] = new Vector3(Mathf.Cos(angle) * speed, 8f + (float)random.NextDouble() * 7f, Mathf.Sin(angle) * speed);
                _spins[i] = new Vector3(160f + (float)random.NextDouble() * 360f, (float)random.NextDouble() * 540f, 120f + (float)random.NextDouble() * 360f);
                var size = 0.5f + (float)random.NextDouble() * 0.85f;
                _scales[i] = new Vector3(size, size * (0.35f + (float)random.NextDouble() * 0.3f), size * 0.8f);
            }
            Render(0f);
        }

        public void Render(float age)
        {
            var fade = Mathf.Clamp01((5.5f - age) / 1.1f);
            for (var i = 0; i < _chunks.Length; i++)
            {
                var velocity = _velocities[i];
                var firstLanding = 2f * velocity.y / Gravity;
                var bounceSpeed = velocity.y * 0.24f;
                var bounceDuration = 2f * bounceSpeed / Gravity;
                var flight = Mathf.Min(age, firstLanding);
                var bounce = Mathf.Clamp(age - firstLanding, 0f, bounceDuration);
                var horizontalTime = flight + bounce * 0.3f;
                var height = velocity.y * flight - Gravity * flight * flight * 0.5f
                    + bounceSpeed * bounce - Gravity * bounce * bounce * 0.5f;
                var chunk = _chunks[i];
                chunk.localPosition = new Vector3(velocity.x * horizontalTime, -0.68f + _scales[i].y * 0.5f + Mathf.Max(0f, height), velocity.z * horizontalTime);
                var rotation = _spins[i] * (flight + bounce * 0.5f);
                var settle = Mathf.Clamp01((age - firstLanding - bounceDuration) / 0.15f);
                chunk.localRotation = Quaternion.Slerp(Quaternion.Euler(rotation), Quaternion.Euler(0f, rotation.y, 0f), settle);
                chunk.localScale = _scales[i] * fade;
            }
            gameObject.SetActive(age < 5.5f);
        }
    }
}
