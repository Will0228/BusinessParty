using UnityEngine;

namespace MixVerse.Seal
{
    /// <summary>
    /// カメラを一時的に間借りして、指定したレイヤーマスク・範囲だけを 1 枚のテクスチャへ撮る。
    ///
    /// ワームホールと違って覗く視点そのものは変わらないので、専用カメラは常設せず、
    /// 対象カメラの設定（カリングマスク・描画先・enabled）を出し入れするだけで済ませている。
    /// </summary>
    public sealed class SealSnapshotCapturer
    {
        /// <summary>
        /// 指定したカメラで、画面上の pixelRect の範囲だけを切り出して撮る。
        /// 一度画面全体を撮ってから範囲外を捨てる方式で、シェーダーの UV 計算に歪みを持ち込まない。
        /// 戻り値は呼び出し側が RenderTexture.ReleaseTemporary で返すこと。
        /// </summary>
        public RenderTexture Capture(Camera camera, int cullingMask, Rect pixelRect)
        {
            var fullWidth = Mathf.Max(1, camera.pixelWidth);
            var fullHeight = Mathf.Max(1, camera.pixelHeight);
            var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            var fullCapture = RenderTexture.GetTemporary(fullWidth, fullHeight, 24, format);

            var originalEnabled = camera.enabled;
            var originalCullingMask = camera.cullingMask;
            var originalTargetTexture = camera.targetTexture;

            // enabled を切ることで、この直後に来る Unity 本来の自動描画と二重に走らないようにする
            camera.enabled = false;
            camera.cullingMask = cullingMask;
            camera.targetTexture = fullCapture;
            camera.Render();

            camera.targetTexture = originalTargetTexture;
            camera.cullingMask = originalCullingMask;
            camera.enabled = originalEnabled;

            var cropWidth = Mathf.Clamp(Mathf.RoundToInt(pixelRect.width), 1, fullWidth);
            var cropHeight = Mathf.Clamp(Mathf.RoundToInt(pixelRect.height), 1, fullHeight);
            var cropX = Mathf.Clamp(Mathf.RoundToInt(pixelRect.x), 0, fullWidth - cropWidth);
            var cropY = Mathf.Clamp(Mathf.RoundToInt(pixelRect.y), 0, fullHeight - cropHeight);

            // 拡大縮小を挟まない単純なピクセルコピーで切り出す（引き伸ばすと境界の見た目が歪むため）
            var cropped = RenderTexture.GetTemporary(cropWidth, cropHeight, 0, format);
            Graphics.CopyTexture(fullCapture, 0, 0, cropX, cropY, cropWidth, cropHeight, cropped, 0, 0, 0, 0);

            RenderTexture.ReleaseTemporary(fullCapture);

            return cropped;
        }
    }
}
