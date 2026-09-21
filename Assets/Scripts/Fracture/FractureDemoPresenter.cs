using UnityEngine;

namespace MixVerse.Fracture
{
    public interface IFractureDemoPresenter
    {
        float Progress { get; }
        bool IsPlaying { get; }
        void SetProgress(float value);
        void Play();
        void Tick(float deltaTime);
    }

    public sealed class FractureDemoPresenter : IFractureDemoPresenter
    {
        private readonly FractureDemoView _view;
        public float Progress { get; private set; }
        public bool IsPlaying { get; private set; }

        public FractureDemoPresenter(FractureDemoView view) { _view = view; }

        public void SetProgress(float value)
        {
            IsPlaying = false;
            Progress = Mathf.Clamp01(value);
            _view.RenderProgress(Progress);
        }

        public void Play()
        {
            SetProgress(0f);
            IsPlaying = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsPlaying) return;
            Progress = Mathf.Min(1f, Progress + Mathf.Max(0f, deltaTime) / 2.5f);
            _view.RenderProgress(Progress);
            if (Progress >= 1f) IsPlaying = false;
        }
    }
}
