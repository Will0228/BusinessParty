using System.Collections.Generic;
using MixVerse.Game.Model;
using UnityEngine;

namespace MixVerse.Game.Stage
{
    /// <summary>
    /// RhythmStageFactory が組み立てたステージ一式の受け口。
    /// 実行時に生成したマテリアルや AudioClip は、ステージごと破棄されるときにまとめて片付ける。
    /// </summary>
    public sealed class RhythmStageView : MonoBehaviour
    {
        private readonly List<Object> _generatedAssets = new List<Object>();

        public Camera Camera { get; set; }
        public AudioListener Listener { get; set; }
        public StageLaneView LeftLane { get; set; }
        public StageLaneView RightLane { get; set; }
        public StageActorView Player { get; set; }
        public StageActorView LeftCpu { get; set; }
        public StageActorView RightCpu { get; set; }
        public RhythmHudView Hud { get; set; }
        public AudioSource ClickSource { get; set; }
        public AudioClip DownBeatClip { get; set; }
        public AudioClip BeatClip { get; set; }
        public Vector3 NoteScale { get; set; }
        public float SpawnZ { get; set; }
        public float JudgeZ { get; set; }

        public StageLaneView LaneOf(ChartLane lane) => lane == ChartLane.Left ? LeftLane : RightLane;

        public void RegisterGeneratedAsset(Object asset) => _generatedAssets.Add(asset);

        private void OnDestroy()
        {
            foreach (var asset in _generatedAssets)
            {
                if (asset != null)
                {
                    Destroy(asset);
                }
            }

            _generatedAssets.Clear();
        }
    }
}
