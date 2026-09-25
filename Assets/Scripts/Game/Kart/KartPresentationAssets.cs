using TMPro;
using UnityEngine;

namespace MixVerse.Game.Kart
{
    public sealed class KartPresentationAssets : ScriptableObject
    {
        public TMP_FontAsset japaneseFont;
        public GameObject[] buildingPrefabs;
        public GameObject[] streetLightPrefabs;
        public GameObject[] overheadCablePrefabs;
        public Material[] screenMaterials;
        public Material pavementMaterial;
        public Material skybox;
        public AudioClip bgmClip;
        public AudioClip driftSeClip;
        public AudioClip rocketFireSeClip;
    }
}
