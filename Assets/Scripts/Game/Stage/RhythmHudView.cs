using MixVerse.Game.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Game.Stage
{
    public sealed class RhythmHudView : MonoBehaviour
    {
        private const float JudgementHoldSeconds = 0.45f;

        private static readonly Color PerfectColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color GoodColor = new Color(0.4f, 0.9f, 1f);
        private static readonly Color MissColor = new Color(1f, 0.35f, 0.4f);

        private CanvasGroup _group;
        private Image _fadeOverlay;
        private TextMeshProUGUI _scoreLabel;
        private TextMeshProUGUI _judgementLabel;
        private TextMeshProUGUI _hintLabel;
        private float _judgementElapsed = JudgementHoldSeconds;

        public CanvasGroup Group => _group;

        public void Initialize(CanvasGroup group, Image fadeOverlay, TextMeshProUGUI scoreLabel,
            TextMeshProUGUI judgementLabel, TextMeshProUGUI hintLabel)
        {
            _group = group;
            _fadeOverlay = fadeOverlay;
            _scoreLabel = scoreLabel;
            _judgementLabel = judgementLabel;
            _hintLabel = hintLabel;
            _judgementLabel.text = string.Empty;
            SetScore(0, 0);
        }

        public void SetFadeAlpha(float alpha)
        {
            var color = _fadeOverlay.color;
            color.a = alpha;
            _fadeOverlay.color = color;
        }

        public void SetScore(int score, int combo)
            => _scoreLabel.text = combo > 1 ? $"SCORE {score}\n{combo} COMBO" : $"SCORE {score}";

        public void SetHint(string text) => _hintLabel.text = text;

        public void ShowJudgement(NoteJudgement judgement)
        {
            _judgementLabel.text = judgement.ToString().ToUpperInvariant();
            _judgementLabel.color = ColorOf(judgement);
            _judgementElapsed = 0f;
        }

        /// <summary>譜面を流し終えたときの表示。判定の文字はここで止めて残す。</summary>
        public void ShowResult(ScoreBoard score)
        {
            _judgementLabel.text = "FINISH";
            _judgementLabel.color = Color.white;
            _judgementElapsed = JudgementHoldSeconds;
            _hintLabel.text =
                $"SCORE {score.Score}　MAX COMBO {score.MaxCombo}　" +
                $"PERFECT {score.PerfectCount} / GOOD {score.GoodCount} / MISS {score.MissCount}　　ESC でホームへ戻る";
        }

        private Color ColorOf(NoteJudgement judgement)
        {
            switch (judgement)
            {
                case NoteJudgement.Perfect: return PerfectColor;
                case NoteJudgement.Good: return GoodColor;
                default: return MissColor;
            }
        }

        private void Update()
        {
            if (_judgementLabel == null || _judgementElapsed >= JudgementHoldSeconds)
            {
                return;
            }

            _judgementElapsed += Time.deltaTime;

            var color = _judgementLabel.color;
            color.a = 1f - Mathf.Clamp01(_judgementElapsed / JudgementHoldSeconds);
            _judgementLabel.color = color;
        }
    }
}
