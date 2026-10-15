using System;
using UnityEngine;

namespace MixVerse.UIRebuild
{
    [Serializable]
    public sealed class GraphicRebuildDemoSettings
    {
        [Tooltip("動かさない Image を並べる列数。多いほど同じ Canvas のリビルドが重くなる。")]
        public int staticColumns = 128;

        [Tooltip("動かさない Image を並べる行数。")]
        public int staticRows = 72;

        [Tooltip("毎フレーム位置・回転・色を変える Image の数。")]
        public int animatedCount = 64;

        [Tooltip("計測値を平均して表示する間隔（秒）。")]
        public float statsInterval = 0.5f;

        public GraphicRebuildMode initialMode = GraphicRebuildMode.SingleCanvas;

        public int StaticCount => Mathf.Max(0, staticColumns) * Mathf.Max(0, staticRows);
    }
}
