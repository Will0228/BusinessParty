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
        public GameObject bossBodyPrefab;
        public GameObject bossWeaponPrefab;
        public AudioClip bgmClip;
        public AudioClip explosionSeClip;
        public AudioClip rocketFireSeClip;
        public AudioClip blowAwaySeClip;
        public AudioClip itemGetSeClip;
        public AudioClip distanceAlertSeClip;
        public AudioClip engineSeClip;
        public AudioClip gameOverSeClip;
    }
}
