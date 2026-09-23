using UnityEngine;

namespace MixVerse.Game.Kart
{
    // イニシャルD風の低いチェイスカメラ。進行方向の追従をわざと遅らせ、コーナーでマシンの横腹とドリフト角が見えるようにする
    public sealed class KartChaseCamera
    {
        private const float BaseFieldOfView = 60f;
        private const float TopSpeedFieldOfView = 78f;
        private readonly Transform _camera;
        private Vector3 _position;
        private Quaternion _heading;
        private float _roll;
        private bool _initialized;

        public float FieldOfView { get; private set; } = BaseFieldOfView;

        public KartChaseCamera(Transform camera)
        {
            _camera = camera;
        }

        public void Reset()
        {
            _initialized = false;
            FieldOfView = BaseFieldOfView;
        }

        public void Follow(Vector3 kart, Quaternion track, float speedRatio, float turnAngle, float deltaTime)
        {
            speedRatio = Mathf.Clamp01(speedRatio);
            var distance = Mathf.Lerp(4.6f, 5.8f, speedRatio);
            var height = Mathf.Lerp(1.9f, 1.55f, speedRatio);
            if (!_initialized)
            {
                _heading = track;
                _position = kart + track * new Vector3(0f, height, -distance);
                _initialized = true;
            }
            _heading = Quaternion.Slerp(_heading, track, Damp(3.2f, deltaTime));
            var desired = kart + _heading * new Vector3(0f, height, -distance);
            _position = Vector3.Lerp(_position, desired, Damp(10f, deltaTime));
            var look = kart + track * new Vector3(0f, 0.9f, 9f);
            _roll = Mathf.Lerp(_roll, Mathf.Clamp(-turnAngle * 0.18f, -5f, 5f), Damp(2.5f, deltaTime));
            FieldOfView = Mathf.Lerp(FieldOfView, Mathf.Lerp(BaseFieldOfView, TopSpeedFieldOfView, speedRatio), Damp(2f, deltaTime));
            Apply(look);
        }

        public void Frame(Vector3 focus, Quaternion direction, float deltaTime)
        {
            var desired = focus + direction * new Vector3(0f, 4.2f, -9.5f);
            if (!_initialized)
            {
                _position = desired;
                _initialized = true;
            }
            _heading = direction;
            _position = Vector3.Lerp(_position, desired, Damp(5f, deltaTime));
            _roll = Mathf.Lerp(_roll, 0f, Damp(4f, deltaTime));
            FieldOfView = Mathf.Lerp(FieldOfView, BaseFieldOfView, Damp(2f, deltaTime));
            Apply(focus + direction * new Vector3(0f, 0.6f, 2f));
        }

        private void Apply(Vector3 look)
        {
            _camera.localPosition = _position;
            _camera.localRotation = Quaternion.LookRotation(look - _position, Vector3.up) * Quaternion.Euler(0f, 0f, _roll);
        }

        private float Damp(float sharpness, float deltaTime) => 1f - Mathf.Exp(-sharpness * deltaTime);
    }
}
