namespace MixVerse.UIRebuild
{
    public readonly struct CanvasRebuildStats
    {
        public readonly float UpdateCanvasesMs;
        public readonly float SendWillRenderCanvasesMs;
        public readonly float BuildBatchMs;
        public readonly float FrameMs;

        public CanvasRebuildStats(float updateCanvasesMs, float sendWillRenderCanvasesMs, float buildBatchMs, float frameMs)
        {
            UpdateCanvasesMs = updateCanvasesMs;
            SendWillRenderCanvasesMs = sendWillRenderCanvasesMs;
            BuildBatchMs = buildBatchMs;
            FrameMs = frameMs;
        }
    }
}
