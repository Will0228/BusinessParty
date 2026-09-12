using UnityEngine;

namespace MixVerse.Seal
{
    /// <summary>
    /// カメラを一時的に間借りして、指定したレイヤーマスクだけを 1 枚のテクスチャへ撮る。
    ///
    /// ワームホールと違って覗く視点そのものは変わらないので、専用カメラは常設せず、
    /// 対象カメラの設定（カリングマスク・描画先・enabled）を出し入れするだけで済ませている。
    /// </summary>
    public sealed class SealSnapshotCapturer
    {
        /// <summary>
        /// 指定したカメラで 1 枚撮る。戻り値は呼び出し側が RenderTexture.ReleaseTemporary で返すこと。
        /// </summary>
        public RenderTexture Capture(Camera camera, int cullingMask, int width, int height)
        {
            var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, format);

            var originalEnabled = camera.enabled;
            var originalCullingMask = camera.cullingMask;
            var originalTargetTexture = camera.targetTexture;

            // enabled を切ることで、この直後に来る Unity 本来の自動描画と二重に走らないようにする
            camera.enabled = false;
            camera.cullingMask = cullingMask;
            camera.targetTexture = renderTexture;
            camera.Render();

            camera.targetTexture = originalTargetTexture;
            camera.cullingMask = originalCullingMask;
            camera.enabled = originalEnabled;

            return renderTexture;
        }
    }
}
