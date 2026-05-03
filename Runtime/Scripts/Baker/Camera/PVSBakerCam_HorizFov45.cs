using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 使用水平 FOV 45° 的 8 视角烘焙相机（6 水平 + 上下各 1）。
    /// 组合图像布局：4 列 × 2 行。
    /// </summary>
    public class PVSBakerCam_HorizFov45 : PVSBakerBaseCam
    {
        /// <inheritdoc/>
        protected override void InitBakerSize(PVSBakeSettings bakeSetting)
        {
            m_BakerSizeWidth = 1024;
            m_BakerSizeHeight = 512;
        }

        /// <inheritdoc/>
        protected override void InitCombinedImageSize()
        {
            m_CombinedImageWidth = m_BakerSizeWidth * 4;
            m_CombinedImageHeight = m_BakerSizeHeight * 2;
        }

        /// <inheritdoc/>
        protected override void InitCamFovArray()
        {
            m_CamFovArray = new float[]
            {
                45f, 45f, 45f, 45f, 45f, 45f,
                135f, 135f
            };
        }

        /// <inheritdoc/>
        protected override void InitCamAspectArray()
        {
            m_CamAspectArray = new float[]
            {
                2f, 2f, 2f, 2f, 2f, 2f,
                2f, 2f
            };
        }

        /// <inheritdoc/>
        protected override void InitCamBakeRotationArray()
        {
            m_CamBakeRotationArray = new Quaternion[]
            {
                Quaternion.Euler(0f,   0f, 0f),
                Quaternion.Euler(0f,  60f, 0f),
                Quaternion.Euler(0f, 120f, 0f),
                Quaternion.Euler(0f, 180f, 0f),
                Quaternion.Euler(0f, 240f, 0f),
                Quaternion.Euler(0f, 300f, 0f),
                Quaternion.Euler(-90f, 0f, 0f),
                Quaternion.Euler( 90f, 0f, 0f),
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
                new Rect(w * 3, h * 0, w, h),
                new Rect(w * 0, h * 1, w, h),
                new Rect(w * 1, h * 1, w, h),
                new Rect(w * 2, h * 1, w, h),
                new Rect(w * 3, h * 1, w, h),
            };
        }
    }
}
