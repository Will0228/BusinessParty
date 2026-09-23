using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MixVerse.Game.Kart
{
    public sealed class KartExplosionView : MonoBehaviour
    {
        private Transform _fireball;
        private Material _fireMaterial;
        private TextMeshPro _caption;
        private Camera _camera;
        private float _radius;
        private float _age;
        private bool _rocket;
        private KartExplosionDebrisView _debris;
        private readonly List<MeshRenderer> _groundCracks = new List<MeshRenderer>();
        private readonly List<Mesh> _groundMeshes = new List<Mesh>();
        private MaterialPropertyBlock _groundProperties;
        private static readonly int AgeId = Shader.PropertyToID("_Age");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        public bool IsAlive => _age < (_groundCracks.Count > 0 ? 5f : _rocket ? KartExplosionDebrisView.VisibleSeconds : 0.9f);
        public float Impact => _rocket ? Mathf.Pow(Mathf.Clamp01(1f - _age / 0.42f), 2f) : 0f;

        public void Initialize(Transform fireball, Material fireMaterial, TextMeshPro caption, Camera camera, float radius, bool rocket, KartExplosionDebrisView debris = null)
        {
            _fireball = fireball;
            _fireMaterial = fireMaterial;
            _caption = caption;
            _camera = camera;
            _radius = radius;
            _rocket = rocket;
            _debris = debris;
            Render();
        }

        public void AddGroundCrack(MeshRenderer renderer, Mesh mesh)
        {
            _groundCracks.Add(renderer);
            _groundMeshes.Add(mesh);
            if (_groundProperties == null) _groundProperties = new MaterialPropertyBlock();
            Render();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            Render();
        }

        private void Render()
        {
            if (_debris != null && _debris.gameObject.activeSelf) _debris.Render(_age);
            if (_groundCracks.Count > 0)
            {
                _groundProperties.SetFloat(AgeId, _age);
                foreach (var crack in _groundCracks)
                {
                    crack.SetPropertyBlock(_groundProperties);
                    crack.enabled = _age < 5f;
                }
            }
            var progress = Mathf.Clamp01(_age / (_rocket ? 0.8f : 0.5f));
            _fireball.localScale = Vector3.one * Mathf.Lerp(0.3f, _radius * 2.2f, 1f - Mathf.Pow(1f - progress, 3f));
            _fireball.localPosition = Vector3.up * (_age * 1.5f);
            _fireMaterial.SetFloat(ProgressId, progress);
            _fireball.gameObject.SetActive(progress < 1f);
            if (_caption == null) return;
            var captionAge = Mathf.Max(0f, _age - 0.08f);
            _caption.gameObject.SetActive(_age >= 0.08f && _age < 1.35f);
            _caption.transform.localPosition = Vector3.up * (_radius + 0.6f + captionAge * 1.7f);
            _caption.transform.localScale = Vector3.one * (1f + Mathf.Sin(captionAge * 22f) * Mathf.Exp(-captionAge * 8f) * 0.28f);
            _caption.alpha = Mathf.Clamp01((1.35f - _age) / 0.3f);
        }

        private void LateUpdate()
        {
            if (_caption != null && _camera != null)
                _caption.transform.rotation = _camera.transform.rotation * Quaternion.Euler(0f, 0f, -12f);
        }

        private void OnDestroy()
        {
            foreach (var mesh in _groundMeshes) if (mesh != null) Destroy(mesh);
            if (_fireMaterial != null) Destroy(_fireMaterial);
        }
    }
}
