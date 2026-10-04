using UnityEngine;

namespace MixVerse.Game.Kart
{
    // ロケランなどで吹っ飛ばされている間だけ使うカメラ。道路の先ではなくカート自身を見続け、どこへ飛ばされたのか分かるようにする
    public sealed class KartBlowAwayCamera
    {
        private const float BlowAwayFieldOfView = 70f;
        private const float MinimumRoadClearance = 3.5f;
        private readonly Transform _camera;
        private Vector3 _position;
        private bool _initialized;

        public float FieldOfView { get; private set; } = BlowAwayFieldOfView;

        public KartBlowAwayCamera(Transform camera)
        {
            _camera = camera;
        }

        public void Reset()
        {
            _initialized = false;
            FieldOfView = BlowAwayFieldOfView;
        }

        // 走行カメラから引き継ぐ際、今の見え方からそのまま動かし始めてカットインにならないようにする
        public void Takeover(float fieldOfView)
        {
            _position = _camera.localPosition;
            _initialized = true;
            FieldOfView = fieldOfView;
        }

        public void Follow(Vector3 kart, Vector3 road, Quaternion track, float deltaTime)
        {
            var away = kart - road;
            away.y = 0f;
            var pullBack = away.sqrMagnitude > 0.01f ? away.normalized * Mathf.Min(away.magnitude * 0.55f, 7f) : Vector3.zero;
            var desired = kart + track * new Vector3(0f, 5.2f, -9.5f) - pullBack;
            desired.y = Mathf.Max(desired.y, road.y + MinimumRoadClearance);
            if (!_initialized)
            {
                _position = desired;
                _initialized = true;
            }
            _position = Vector3.Lerp(_position, desired, Damp(6f, deltaTime));
            FieldOfView = Mathf.Lerp(FieldOfView, BlowAwayFieldOfView, Damp(3f, deltaTime));
            _camera.localPosition = _position;
            _camera.localRotation = Quaternion.LookRotation(kart - _position, Vector3.up);
        }

        private float Damp(float sharpness, float deltaTime) => 1f - Mathf.Exp(-sharpness * deltaTime);
    }
}
