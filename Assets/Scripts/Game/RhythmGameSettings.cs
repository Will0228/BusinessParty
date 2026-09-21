using System;
using UnityEngine;

namespace MixVerse.Game
{
    /// <summary>
    /// インゲーム（リズムゲーム）の調整値。GameView の Inspector から触る。
    /// </summary>
    [Serializable]
    public sealed class RhythmGameSettings
    {
        [Header("Rhythm")]
        [Min(1f)] public float bpm = 130f;

        [Tooltip("開始してから最初のノーツが到達するまでの拍数。")]
        [Min(1)] public int leadInBeats = 8;

        [Tooltip("ノーツが画面上に出てから手前の判定ラインへ着くまでの拍数。")]
        [Min(1f)] public float noteLeadBeats = 4f;

        [Header("Judgement")]
        [Min(0.005f)] public float perfectSeconds = 0.05f;
        [Min(0.005f)] public float goodSeconds = 0.12f;

        [Header("Stage")]
        [Tooltip("部屋のオブジェクトと重ならないよう、ステージはシーンの離れた場所に建てる。")]
        public Vector3 stageOrigin = new Vector3(0f, 1000f, 0f);

        [Min(8f)] public float laneLength = 32f;

        [Tooltip("レーン 1 本ぶんの幅。左右で 2 本並べる。")]
        [Min(0.5f)] public float laneWidth = 3f;

        [Min(0f)] public float laneGap = 0.6f;

        [Tooltip("判定ラインから見たプレイヤーの位置。手前へずらすとより画面下に映る。")]
        public float playerOffsetZ = -1.2f;

        [Min(1f)] public float cpuSpacing = 3.5f;

        [Header("Camera")]
        [Min(1f)] public float cameraHeight = 10f;
        [Min(0f)] public float cameraDistance = 6f;
        [Range(10f, 89f)] public float cameraPitch = 45f;
        [Range(20f, 90f)] public float fieldOfView = 60f;

        [Header("Presentation")]
        [Min(0f)] public float fadeDuration = 0.6f;
        public bool playMetronome = true;

        /// <summary>レーンの中心が原点から左右へ離れている距離。</summary>
        public float LaneOffsetX => (laneWidth + laneGap) * 0.5f;

        public float LaneWidthTotal => laneWidth * 2f + laneGap;

        public void Validate()
        {
            bpm = Mathf.Max(1f, bpm);
            leadInBeats = Mathf.Max(1, leadInBeats);
            noteLeadBeats = Mathf.Max(1f, noteLeadBeats);
            perfectSeconds = Mathf.Max(0.005f, perfectSeconds);
            goodSeconds = Mathf.Max(perfectSeconds, goodSeconds);
            laneLength = Mathf.Max(8f, laneLength);
            laneWidth = Mathf.Max(0.5f, laneWidth);
            laneGap = Mathf.Max(0f, laneGap);
            cpuSpacing = Mathf.Max(1f, cpuSpacing);
            cameraHeight = Mathf.Max(1f, cameraHeight);
            cameraDistance = Mathf.Max(0f, cameraDistance);
            fadeDuration = Mathf.Max(0f, fadeDuration);
        }
    }
}
