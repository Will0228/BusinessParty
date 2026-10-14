using System.Collections.Generic;
using MixVerse.Game.Model.Kart;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixVerse.Game.Kart
{
    public sealed class KartTestPlayStageView : MonoBehaviour
    {
        private const float DriftSlipDegrees = 25f;
        private const float EngineSeMinSpeed = 1f;
        private const float EngineSeMaxSpeed = 100f;

        // 本番（KartStageView）と同じく、溜め具合で黄→青→オレンジと変える
        private static readonly Color[] DriftSparkColors =
        {
            new Color(1f, 0.88f, 0.15f),
            new Color(0.05f, 0.55f, 1f),
            new Color(1f, 0.35f, 0f),
        };

        public Camera Camera;
        public Light Sun;
        public Transform Kart;
        public Transform Ground;
        public float GroundTile;
        public ParticleSystem[] DriftSparks;
        public AudioSource EngineAudio;
        public KartChaseCamera ChaseCamera;
        public readonly List<Object> GeneratedAssets = new List<Object>();

        private float _slip;

        private void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        // 平行光源はシーン全体を照らしてしまうので、テストプレイ用カメラの描画中だけ点ける
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera rendering)
        {
            if (Sun != null) Sun.enabled = rendering == Camera;
        }

        public void Render(KartTestDrive drive)
        {
            var deltaTime = Time.deltaTime;
            var position = new Vector3(drive.X, 0.45f, drive.Z);
            var heading = Quaternion.Euler(0f, drive.Heading * Mathf.Rad2Deg, 0f);
            _slip = Mathf.Lerp(_slip, drive.DriftDirection * DriftSlipDegrees, 1f - Mathf.Exp(-8f * deltaTime));
            Kart.localPosition = position;
            Kart.localRotation = heading * Quaternion.Euler(0f, _slip, 0f);

            // 地面はタイル単位でカートに付いて来させ、どこまで走っても端に着かないようにする
            Ground.localPosition = new Vector3(Mathf.Round(drive.X / GroundTile) * GroundTile, 0f,
                Mathf.Round(drive.Z / GroundTile) * GroundTile);

            ChaseCamera.Follow(position, heading, Mathf.Abs(drive.Speed) / 100f, _slip, deltaTime);
            Camera.fieldOfView = ChaseCamera.FieldOfView;
            UpdateDriftSparks(drive);
            UpdateEngineSe(drive);
        }

        private void UpdateDriftSparks(KartTestDrive drive)
        {
            var color = DriftSparkColors[drive.DriftTier];
            foreach (var spark in DriftSparks)
            {
                var active = drive.IsDrifting && Mathf.Sign(spark.transform.localPosition.x) == drive.DriftDirection;
                var emission = spark.emission;
                emission.enabled = active;
                if (!active) continue;
                var main = spark.main;
                main.startColor = color;
            }
        }

        private void UpdateEngineSe(KartTestDrive drive)
        {
            if (EngineAudio.clip == null) return;
            var speed = Mathf.Abs(drive.Speed);
            if (speed <= EngineSeMinSpeed)
            {
                if (EngineAudio.isPlaying) EngineAudio.Stop();
                return;
            }

            if (!EngineAudio.isPlaying) EngineAudio.Play();
            var speedRatio = Mathf.Clamp01(speed / EngineSeMaxSpeed);
            EngineAudio.volume = Mathf.Lerp(0.25f, 0.7f, speedRatio);
            EngineAudio.pitch = Mathf.Lerp(0.85f, 1.6f, speedRatio);
        }

        private void OnDestroy()
        {
            foreach (var asset in GeneratedAssets) if (asset != null) Destroy(asset);
        }
    }
}
