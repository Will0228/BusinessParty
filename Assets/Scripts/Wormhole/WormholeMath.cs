using UnityEngine;

namespace MixVerse.Wormhole
{
    /// <summary>
    /// ワームホールの入口越しに見える景色を、出口の側で撮り直すための行列計算。
    /// MonoBehaviour に依存しないので、使う View 側で生成して持ち回る。
    /// </summary>
    public sealed class WormholeMath
    {
        // 入口の表から入ると出口の裏から出る、という「扉」の向きを作るための半回転。
        // これを挟まないと出口で反対向きの景色を撮ってしまう。
        private static readonly Matrix4x4 HalfTurn = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));

        /// <summary>
        /// 入口のローカル空間へ移してから出口のローカル空間へ置き直す行列。
        /// </summary>
        public Matrix4x4 GetWarpMatrix(Transform entrance, Transform exit)
        {
            return exit.localToWorldMatrix * HalfTurn * entrance.worldToLocalMatrix;
        }

        /// <summary>
        /// 覗く側のカメラを出口の側へ移した位置と向きを求める。
        /// </summary>
        public void GetVirtualPose(Matrix4x4 warp, Transform viewer, out Vector3 position, out Quaternion rotation)
        {
            var virtualMatrix = warp * viewer.localToWorldMatrix;

            position = virtualMatrix.GetColumn(3);

            // Matrix4x4.rotation はスケールが混ざると崩れるので、前方向と上方向から組み立てる
            rotation = Quaternion.LookRotation(virtualMatrix.GetColumn(2), virtualMatrix.GetColumn(1));
        }

        /// <summary>覗いている側が入口の表なら +1、裏なら -1 を返す。</summary>
        public float GetViewerSide(Transform entrance, Vector3 viewerPosition)
        {
            return Vector3.Dot(viewerPosition - entrance.position, entrance.forward) >= 0f ? 1f : -1f;
        }

        /// <summary>
        /// 出口の面より手前にある物を写さないための斜めニアクリップ平面を、カメラ空間で返す。
        /// 法線が向いている側だけが残る。
        /// </summary>
        public Vector4 GetCameraSpaceClipPlane(Camera camera, Vector3 pointWS, Vector3 normalWS, float offset)
        {
            var offsetPoint = pointWS + (normalWS * offset);
            var worldToCamera = camera.worldToCameraMatrix;

            var pointCS = worldToCamera.MultiplyPoint(offsetPoint);
            var normalCS = worldToCamera.MultiplyVector(normalWS).normalized;

            return new Vector4(normalCS.x, normalCS.y, normalCS.z, -Vector3.Dot(pointCS, normalCS));
        }
    }
}
