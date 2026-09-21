using UnityEngine;

namespace MixVerse.Game.Stage
{
    /// <summary>
    /// レーン 1 本ぶん。ノーツの置き場所と、叩いたときに光る判定ラインを持つ。
    /// </summary>
    public sealed class StageLaneView : MonoBehaviour
    {
        private const float FlashSeconds = 0.18f;

        private Material _judgeMaterial;
        private Color _idleColor;
        private Color _flashColor;
        private float _elapsed = FlashSeconds;

        public Transform NoteRoot { get; private set; }
        public Material NoteMaterial { get; private set; }

        public void Initialize(Transform noteRoot, Material noteMaterial, Material judgeMaterial, Color idleColor,
            Color flashColor)
        {
            NoteRoot = noteRoot;
            NoteMaterial = noteMaterial;
            _judgeMaterial = judgeMaterial;
            _idleColor = idleColor;
            _flashColor = flashColor;
            _judgeMaterial.color = idleColor;
            _elapsed = FlashSeconds;
        }

        public void Flash() => _elapsed = 0f;

        private void Update()
        {
            if (_judgeMaterial == null || _elapsed >= FlashSeconds)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            _judgeMaterial.color = Color.Lerp(_flashColor, _idleColor, Mathf.Clamp01(_elapsed / FlashSeconds));
        }
    }
}
