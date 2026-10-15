using System.Collections.Generic;
using UnityEngine;

namespace MixVerse.Game.Kart
{
    // プリミティブを組み合わせてスーツ姿のランナーを作る。原点はカートと同じく地面から 0.45m 上
    public sealed class KartRunnerFactory
    {
        private const float GroundOffset = -0.45f;
        private const float ThighLength = 0.48f;
        private const float ShinLength = 0.45f;
        private const float UpperArmLength = 0.32f;
        private const float ForearmLength = 0.3f;
        // 武器モデルは cm 単位で書き出されているので 100 倍にする。柄は原点（鍔）から 15cm 下
        private const float WeaponScale = 100f;
        private const float WeaponGripHeight = -0.15f;
        private static readonly Vector3 SkinnedCapeAnchor = new Vector3(0f, 0.45f, -0.25f);
        private readonly KartMaterialLibrary _materials;

        public KartRunnerFactory(KartMaterialLibrary materials)
        {
            _materials = materials;
        }

        public KartRunnerView Build(Transform root, Color accent)
        {
            var suit = _materials.Lit(Color.Lerp(new Color(0.08f, 0.09f, 0.12f), accent, 0.35f), 0.35f);
            var pants = _materials.Lit(new Color(0.06f, 0.065f, 0.08f), 0.3f);
            var shirt = _materials.Lit(new Color(0.92f, 0.93f, 0.95f), 0.2f);
            var tie = _materials.Lit(accent, 0.6f, 0f, accent * 0.15f);
            var skin = _materials.Lit(new Color(0.96f, 0.76f, 0.62f), 0.25f);
            var hair = _materials.Lit(new Color(0.04f, 0.035f, 0.03f), 0.5f);
            var shoe = _materials.Lit(new Color(0.03f, 0.02f, 0.02f), 0.85f, 0.2f);
            var leather = _materials.Lit(new Color(0.32f, 0.18f, 0.08f), 0.55f);
            var cape = CapeMaterial();

            var view = root.gameObject.AddComponent<KartRunnerView>();
            view.Pelvis = Pivot(root, "Pelvis", new Vector3(0f, GroundOffset + ThighLength + ShinLength + 0.06f, 0f));
            Shape(view.Pelvis, "Hip block", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.42f, 0.2f, 0.26f), pants);

            view.Torso = Pivot(view.Pelvis, "Torso", new Vector3(0f, 0.08f, 0f));
            Shape(view.Torso, "Jacket", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(0.52f, 0.6f, 0.3f), suit);
            Shape(view.Torso, "Shirt", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.14f), new Vector3(0.18f, 0.34f, 0.04f), shirt);
            view.Tie = Pivot(view.Torso, "Tie pivot", new Vector3(0f, 0.6f, 0.17f));
            Shape(view.Tie, "Tie", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(0.09f, 0.4f, 0.03f), tie);
            // 昇給辞令で飛んでいる間だけ出すマント
            view.Cape = Pivot(view.Torso, "Cape pivot", new Vector3(0f, 0.6f, -0.17f));
            Shape(view.Cape, "Cape", PrimitiveType.Cube, new Vector3(0f, -0.55f, 0f), new Vector3(0.58f, 1.1f, 0.02f), cape);
            view.Cape.gameObject.SetActive(false);

            view.Head = Pivot(view.Torso, "Neck", new Vector3(0f, 0.64f, 0f));
            Shape(view.Head, "Face", PrimitiveType.Sphere, new Vector3(0f, 0.18f, 0f), new Vector3(0.3f, 0.34f, 0.3f), skin);
            Shape(view.Head, "Hair", PrimitiveType.Sphere, new Vector3(0f, 0.26f, -0.03f), new Vector3(0.32f, 0.24f, 0.32f), hair);
            for (var side = -1; side <= 1; side += 2)
                Shape(view.Head, "Eye", PrimitiveType.Cube, new Vector3(side * 0.06f, 0.2f, 0.14f), new Vector3(0.05f, 0.03f, 0.02f), hair);

            view.Shoulders = new Transform[2];
            view.Elbows = new Transform[2];
            view.Hips = new Transform[2];
            view.Knees = new Transform[2];
            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? -1f : 1f;
                view.Shoulders[i] = Pivot(view.Torso, "Shoulder", new Vector3(side * 0.31f, 0.56f, 0f));
                Limb(view.Shoulders[i], "Upper arm", UpperArmLength, 0.13f, suit);
                view.Elbows[i] = Pivot(view.Shoulders[i], "Elbow", new Vector3(0f, -UpperArmLength, 0f));
                Limb(view.Elbows[i], "Forearm", ForearmLength, 0.11f, suit);
                Shape(view.Elbows[i], "Hand", PrimitiveType.Sphere, new Vector3(0f, -ForearmLength - 0.05f, 0f), Vector3.one * 0.11f, skin);

                view.Hips[i] = Pivot(view.Pelvis, "Hip", new Vector3(side * 0.12f, 0f, 0f));
                Limb(view.Hips[i], "Thigh", ThighLength, 0.17f, pants);
                view.Knees[i] = Pivot(view.Hips[i], "Knee", new Vector3(0f, -ThighLength, 0f));
                Limb(view.Knees[i], "Shin", ShinLength, 0.14f, pants);
                Shape(view.Knees[i], "Shoe", PrimitiveType.Cube, new Vector3(0f, -ShinLength - 0.03f, 0.07f), new Vector3(0.13f, 0.08f, 0.3f), shoe);
            }
            // 右手のカバンも腕と一緒にぶん回される
            Shape(view.Elbows[1], "Briefcase", PrimitiveType.Cube, new Vector3(0f, -ForearmLength - 0.2f, 0f), new Vector3(0.1f, 0.28f, 0.38f), leather);
            view.Initialize();
            return view;
        }

        // 人型リグのモデル（UE 系ボーン名）を置き、同じ関節構成の見えないピボットで手足を動かす
        public KartRunnerView BuildSkinned(Transform root, GameObject bodyPrefab, GameObject weaponPrefab)
        {
            var body = Object.Instantiate(bodyPrefab, root, false).transform;
            body.localPosition = new Vector3(0f, GroundOffset, 0f);
            body.localRotation = Quaternion.identity;
            foreach (var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen = true;
            var bones = new Dictionary<string, Transform>();
            foreach (var bone in body.GetComponentsInChildren<Transform>(true)) bones.TryAdd(bone.name, bone);
            // A ポーズの腕を真下に垂らし、手続きアニメの基準姿勢（気をつけ）に揃える
            foreach (var side in new[] { "l", "r" })
            {
                HangDown(root, bones["upperarm_" + side], bones["lowerarm_" + side]);
                HangDown(root, bones["lowerarm_" + side], bones["hand_" + side]);
            }

            var view = root.gameObject.AddComponent<KartRunnerView>();
            var skeleton = root.gameObject.AddComponent<KartRunnerSkeleton>();
            view.Pelvis = Joint(root, "Pelvis", bones["pelvis"], skeleton);
            view.Torso = Joint(view.Pelvis, "Torso", bones["spine_01"], skeleton);
            view.Head = Joint(view.Torso, "Neck", bones["neck_01"], skeleton);
            view.Tie = Pivot(view.Torso, "Tie pivot", Vector3.zero);
            view.Cape = Pivot(view.Torso, "Cape pivot", SkinnedCapeAnchor);
            Shape(view.Cape, "Cape", PrimitiveType.Cube, new Vector3(0f, -0.65f, 0f), new Vector3(0.75f, 1.3f, 0.02f), CapeMaterial());
            view.Cape.gameObject.SetActive(false);

            view.Shoulders = new Transform[2];
            view.Elbows = new Transform[2];
            view.Hips = new Transform[2];
            view.Knees = new Transform[2];
            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? "l" : "r";
                view.Shoulders[i] = Joint(view.Torso, "Shoulder", bones["upperarm_" + side], skeleton);
                view.Elbows[i] = Joint(view.Shoulders[i], "Elbow", bones["lowerarm_" + side], skeleton);
                view.Hips[i] = Joint(view.Pelvis, "Hip", bones["thigh_" + side], skeleton);
                view.Knees[i] = Joint(view.Hips[i], "Knee", bones["calf_" + side], skeleton);
            }
            if (weaponPrefab != null) Grip(view.Elbows[1], weaponPrefab, bones["hand_r"], bones["middle_01_r"]);
            view.Initialize();
            return view;
        }

        private Material CapeMaterial() => _materials.Lit(new Color(0.85f, 0.06f, 0.08f), 0.45f, 0f, new Color(0.3f, 0.01f, 0.02f));

        private void HangDown(Transform root, Transform bone, Transform child)
        {
            bone.rotation = Quaternion.FromToRotation(child.position - bone.position, -root.up) * bone.rotation;
        }

        // ボーンと同じ位置にピボットを置くので、回転を写すだけでピボットとボーンの位置も一致し続ける
        private Transform Joint(Transform parent, string name, Transform bone, KartRunnerSkeleton skeleton)
        {
            var joint = Pivot(parent, name, parent.InverseTransformPoint(bone.position));
            skeleton.Bind(joint, bone);
            return joint;
        }

        // 垂らした腕の拳で柄を握り、刃先を正面へ向けて刃は縦に立てる
        private void Grip(Transform forearm, GameObject weaponPrefab, Transform hand, Transform finger)
        {
            var weapon = Object.Instantiate(weaponPrefab, forearm, false).transform;
            weapon.localRotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f);
            weapon.localScale = Vector3.one * WeaponScale;
            var grip = forearm.InverseTransformPoint(Vector3.Lerp(hand.position, finger.position, 0.6f));
            weapon.localPosition = grip - weapon.localRotation * Vector3.up * WeaponGripHeight;
        }

        private Transform Pivot(Transform parent, string name, Vector3 position)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            return pivot;
        }

        // Capsule は高さ 2 なので、長さの半分をスケールに入れて関節から下へ垂らす
        private void Limb(Transform joint, string name, float length, float thickness, Material material)
        {
            Shape(joint, name, PrimitiveType.Capsule, new Vector3(0f, -length * 0.5f, 0f),
                new Vector3(thickness, length * 0.5f + thickness * 0.25f, thickness), material);
        }

        private GameObject Shape(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(primitive);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }
    }
}
