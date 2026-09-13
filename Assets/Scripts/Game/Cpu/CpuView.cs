using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Game.Cpu
{
    public sealed class CpuView : MonoBehaviour
    {
        [SerializeField] private CpuRole _role;
        [SerializeField] private Transform _focusPoint;
        [SerializeField] private AudioSource _voice;
        [SerializeField] private AudioClip[] _talkClips;
        [SerializeField] private TextMeshProUGUI _statusLabel;
        [SerializeField] private TextMeshProUGUI _healthLabel;
        [SerializeField] private Image _healthFill;
        [SerializeField] private SeniorGunView _gun;

        public CpuRole Role => _role;
        public Transform FocusPoint => _focusPoint;

        public void ResetView()
        {
            Stop();
            _gun?.ResetView();
            SetStatus(_role == CpuRole.Junior ? "後輩は隣で聞いている" : "先輩の話を聞こう");
        }

        public void SetHealth(int health, int maximum)
        {
            _healthLabel.text = $"{(_role == CpuRole.Junior ? "後輩" : "先輩")}  HP {health} / {maximum}";
            _healthFill.fillAmount = (float)health / maximum;
            _healthFill.color = health <= maximum * 0.3f ? new Color(1, 0.23f, 0.25f) : new Color(0.25f, 0.85f, 0.68f);
        }

        public void SetStatus(string text) => _statusLabel.text = text;

        public async UniTask SpeakAsync(CpuTalkLine line, float fallbackDuration, CancellationToken token)
        {
            var index = (int)line;
            var clip = index >= 0 && index < _talkClips.Length ? _talkClips[index] : null;
            _voice.clip = clip;
            if (clip != null) _voice.Play();
            try
            {
                if (clip == null) await UniTask.Delay(System.TimeSpan.FromSeconds(fallbackDuration), cancellationToken: token);
                else await UniTask.WaitUntil(() => !_voice.isPlaying, cancellationToken: token);
            }
            finally { _voice.Stop(); }
        }

        public void SetVoiceVolume(float volume) => _voice.volume = volume;
        public void AimAt(Vector3 target) => _gun.AimAt(target);
        public UniTask FireAsync(CancellationToken token) => _gun.FireAsync(token);
        public void Stop() { _voice.Stop(); _gun?.ResetView(); }
    }
}
