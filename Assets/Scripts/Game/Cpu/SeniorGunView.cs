using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MixVerse.Game.Cpu
{
    public sealed class SeniorGunView : MonoBehaviour
    {
        [SerializeField] private GameObject _gunPrefab;
        [SerializeField] private Transform _mount;
        [SerializeField] private Vector3 _modelOffset = new Vector3(0.32f, 0.18f, 0.35f);
        [SerializeField] private Vector3 _modelEuler;
        [SerializeField] private float _scale = 5f;
        [SerializeField] private Light _muzzleFlash;
        [SerializeField] private AudioSource _shotSource;
        [SerializeField] private AudioClip _shotClip;
        [SerializeField] private Transform _upperArm;
        [SerializeField] private Transform _forearm;
        [SerializeField] private Transform _hand;

        private Quaternion _upperArmRest;
        private Quaternion _forearmRest;
        private bool _posed;

        private GameObject _gun;
        private AudioClip _generatedShot;

        public void AimAt(Vector3 target)
        {
            if (_upperArm != null && _forearm != null && _hand != null && !_posed)
            {
                _upperArmRest = _upperArm.localRotation;
                _forearmRest = _forearm.localRotation;
                _upperArm.rotation = Quaternion.FromToRotation(_forearm.position - _upperArm.position,
                    target - _upperArm.position) * _upperArm.rotation;
                _forearm.rotation = Quaternion.FromToRotation(_hand.position - _forearm.position,
                    target - _forearm.position) * _forearm.rotation;
                _posed = true;
                _mount.position = _hand.position;
            }
            if (_gun == null)
            {
                _gun = Instantiate(_gunPrefab, _mount);
                _gun.transform.localPosition = _modelOffset;
                _gun.transform.localRotation = Quaternion.Euler(_modelEuler);
                _gun.transform.localScale = Vector3.one * _scale;
            }
            _mount.rotation = Quaternion.LookRotation(target - _mount.position, Vector3.up);
            _gun.SetActive(true);
        }

        public async UniTask FireAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _shotSource.PlayOneShot(_shotClip != null ? _shotClip : GetShotClip());
            _muzzleFlash.enabled = true;
            try { await UniTask.Delay(TimeSpan.FromSeconds(0.08f), cancellationToken: token); }
            finally { _muzzleFlash.enabled = false; }
        }

        private AudioClip GetShotClip()
        {
            if (_generatedShot != null) return _generatedShot;
            const int sampleRate = 22050;
            var samples = new float[sampleRate / 4];
            var random = new System.Random(731);
            for (var i = 0; i < samples.Length; i++)
            {
                var time = (float)i / sampleRate;
                samples[i] = ((float)random.NextDouble() * 2 - 1) * Mathf.Exp(-time * 30) * 0.65f
                    + Mathf.Sin(time * 2 * Mathf.PI * 75) * Mathf.Exp(-time * 22) * 0.3f;
            }
            _generatedShot = AudioClip.Create("Gunshot", samples.Length, 1, sampleRate, false);
            _generatedShot.SetData(samples, 0);
            return _generatedShot;
        }

        public void ResetView()
        {
            if (_posed)
            {
                _upperArm.localRotation = _upperArmRest;
                _forearm.localRotation = _forearmRest;
                _posed = false;
            }
            if (_gun != null) _gun.SetActive(false);
            if (_muzzleFlash != null) _muzzleFlash.enabled = false;
            if (_shotSource != null) _shotSource.Stop();
        }

        private void OnDestroy() { if (_generatedShot != null) Destroy(_generatedShot); }
    }
}
