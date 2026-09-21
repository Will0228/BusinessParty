using UnityEngine;

namespace MixVerse.Game.Stage
{
    /// <summary>
    /// 画面奥から判定ラインへ流れてくるノーツ 1 つ。
    /// </summary>
    public sealed class NoteView : MonoBehaviour
    {
        private double _hitTime;
        private double _leadSeconds;
        private float _spawnZ;
        private float _judgeZ;

        public int Id { get; private set; }

        public void Initialize(int id, double hitTime, double leadSeconds, float spawnZ, float judgeZ)
        {
            Id = id;
            _hitTime = hitTime;
            _leadSeconds = leadSeconds;
            _spawnZ = spawnZ;
            _judgeZ = judgeZ;
            gameObject.SetActive(true);
        }

        public void SetSongTime(double songTime)
        {
            // 判定ラインを過ぎたぶんも同じ速さで流したいので、補間はクランプしない
            var rate = (float)((_hitTime - songTime) / _leadSeconds);
            var localPosition = transform.localPosition;
            localPosition.z = Mathf.LerpUnclamped(_judgeZ, _spawnZ, rate);
            transform.localPosition = localPosition;
        }

        public void Release() => gameObject.SetActive(false);
    }
}
