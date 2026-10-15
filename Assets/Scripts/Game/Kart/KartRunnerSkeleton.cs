using System.Collections.Generic;
using UnityEngine;

namespace MixVerse.Game.Kart
{
    // KartRunnerView が回す見えない関節の姿勢を、スキンメッシュのボーンへ毎フレーム写す
    public sealed class KartRunnerSkeleton : MonoBehaviour
    {
        private readonly struct Binding
        {
            public readonly Transform Joint;
            public readonly Transform Bone;
            public readonly Quaternion Offset;

            public Binding(Transform joint, Transform bone)
            {
                Joint = joint;
                Bone = bone;
                Offset = Quaternion.Inverse(joint.rotation) * bone.rotation;
            }
        }

        private readonly List<Binding> _bindings = new List<Binding>();

        // 親ボーンから順に登録する。最初に登録したボーンだけは位置も関節に追従させる
        public void Bind(Transform joint, Transform bone)
        {
            _bindings.Add(new Binding(joint, bone));
        }

        private void LateUpdate()
        {
            if (_bindings.Count == 0) return;
            _bindings[0].Bone.position = _bindings[0].Joint.position;
            foreach (var binding in _bindings) binding.Bone.rotation = binding.Joint.rotation * binding.Offset;
        }
    }
}
