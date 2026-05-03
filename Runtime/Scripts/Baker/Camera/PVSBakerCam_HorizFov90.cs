using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 使用水平 FOV 90° 的 6 视角烘焙相机（标准立方体贴图视角）。
    /// 组合图像布局：3 列 × 2 行。
    /// </summary>
    public class PVSBakerCam_HorizFov90 : PVSBakerBaseCam
    {
        /// <inheritdoc/>
        protected override void InitBakerSize(PVSBakeSettings bakeSetting)
        {
            m_BakerSizeWidth = 1024;
            m_BakerSizeHeight = 1024;
        }

        /// <inheritdoc/>
        protected override void InitCamFovArray()
        {
            m_CamFovArray = new float[] { 90f, 90f, 90f, 90f, 90f, 90f };
        }

        /// <inheritdoc/>
        protected override void InitCamAspectArray()
        {
            m_CamAspectArray = new float[] { 1f, 1f, 1f, 1f, 1f, 1f };
        }

        /// <inheritdoc/>
        protected override void InitCombinedImageSize()
        {
            m_CombinedImageWidth = m_BakerSizeWidth * 3;
            m_CombinedImageHeight = m_BakerSizeHeight * 2;
        }

        /// <inheritdoc/>
        protected override void InitCamBakeRotationArray()
        {
            m_CamBakeRotationArray = new Quaternion[]
            {
                Quaternion.Euler(  0f,  90f, 0f),
                Quaternion.Euler(  0f, -90f, 0f),
                Quaternion.Euler(-90f,   0f, 0f),
                Quaternion.Euler( 90f,   0f, 0f),
                Quaternion.Euler(  0f, 180f, 0f),
                Quaternion.Euler(  0f,   0f, 0f),
            };
        }

        /// <inheritdoc/>
        protected override void InitCamBakeRectArray()
        {
            int w = m_BakerSizeWidth;
            int h = m_BakerSizeHeight;

            m_CamBakeRectArray = new Rect[]
            {
                new Rect(w * 0, h * 0, w, h),
                new Rect(w * 1, h * 0, w, h),
                new Rect(w * 2, h * 0, w, h),
                new Rect(w * 0, h * 1, w, h),
                new Rect(w * 1, h * 1, w, h),
                new Rect(w * 2, h * 1, w, h),
            };
        }
    }
}
