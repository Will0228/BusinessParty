using System;
using UnityEngine.Profiling;

namespace MixVerse.UIRebuild
{
    /// <summary>
    /// Profiler に出るのと同じサンプラーの計測値を、画面にも出せるようフレームをまたいで平均する。
    /// </summary>
    public sealed class CanvasRebuildSampler : IDisposable
    {
        public const string UpdateCanvasesSampler = "PostLateUpdate.PlayerUpdateCanvases";
        public const string SendWillRenderCanvasesSampler = "Canvas.SendWillRenderCanvases";
        public const string BuildBatchSampler = "Canvas.BuildBatch";

        private readonly Recorder _updateCanvases = Recorder.Get(UpdateCanvasesSampler);
        private readonly Recorder _sendWillRenderCanvases = Recorder.Get(SendWillRenderCanvasesSampler);
        private readonly Recorder _buildBatch = Recorder.Get(BuildBatchSampler);
        private double _updateCanvasesNs;
        private double _sendWillRenderCanvasesNs;
        private double _buildBatchNs;
        private double _frameSeconds;
        private int _frames;

        public CanvasRebuildSampler()
        {
            SetEnabled(true);
        }

        public void Sample(float deltaTime)
        {
            _updateCanvasesNs += _updateCanvases.elapsedNanoseconds;
            _sendWillRenderCanvasesNs += _sendWillRenderCanvases.elapsedNanoseconds;
            _buildBatchNs += _buildBatch.elapsedNanoseconds;
            _frameSeconds += deltaTime;
            _frames++;
        }

        public CanvasRebuildStats Flush()
        {
            var frames = Math.Max(1, _frames);
            var stats = new CanvasRebuildStats(
                (float)(_updateCanvasesNs / frames / 1_000_000d),
                (float)(_sendWillRenderCanvasesNs / frames / 1_000_000d),
                (float)(_buildBatchNs / frames / 1_000_000d),
                (float)(_frameSeconds / frames * 1000d));
            Reset();
            return stats;
        }

        public void Reset()
        {
            _updateCanvasesNs = 0d;
            _sendWillRenderCanvasesNs = 0d;
            _buildBatchNs = 0d;
            _frameSeconds = 0d;
            _frames = 0;
        }

        public void Dispose() => SetEnabled(false);

        private void SetEnabled(bool enabled)
        {
            _updateCanvases.enabled = enabled;
            _sendWillRenderCanvases.enabled = enabled;
            _buildBatch.enabled = enabled;
        }
    }
}
