using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Game.Kart
{
    public sealed class KartWindshieldRainView : MonoBehaviour
    {
        private static readonly int RainAmountId = Shader.PropertyToID("_RainAmount");
        private const float TunnelFadeSpeed = 4f;

        public RawImage Image;
        public Material Material;

        private float _amount = 1f;

        public void SetTunnel(bool inTunnel)
        {
            var target = inTunnel ? 0f : 1f;
            _amount = Mathf.Lerp(_amount, target, 1f - Mathf.Exp(-TunnelFadeSpeed * Time.deltaTime));
            Material.SetFloat(RainAmountId, _amount);
        }

        private void OnDestroy()
        {
            if (Material != null) Destroy(Material);
        }
    }
}
