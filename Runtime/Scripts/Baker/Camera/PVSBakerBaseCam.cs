using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// PVS 烘焙相机的抽象基类，封装相机初始化、渲染和目标纹理创建逻辑。
    /// 子类通过重写 Init* 方法配置 FOV、朝向、布局等参数。
    /// </summary>
    public abstract class PVSBakerBaseCam
    {
        // ──────────────────────────────────────────
        // 受保护字段
        // ──────────────────────────────────────────

        /// <summary>烘焙相机实例。</summary>
        protected Camera m_Cam;

        /// <summary>烘焙相机的 Transform（缓存以避免重复获取）。</summary>
        protected Transform m_CamTransform;

        /// <summary>单帧单视角渲染宽度（像素）。</summary>
        protected int m_BakerSizeWidth;

        /// <summary>单帧单视角渲染高度（像素）。</summary>
        protected int m_BakerSizeHeight;

        /// <summary>组合图像总宽度（像素）。</summary>
        protected int m_CombinedImageWidth;

        /// <summary>组合图像总高度（像素）。</summary>
        protected int m_CombinedImageHeight;

        /// <summary>各视角的相机 FOV 数组。</summary>
        protected float[] m_CamFovArray;

        /// <summary>各视角的相机宽高比数组。</summary>
        protected float[] m_CamAspectArray;

        /// <summary>各视角的相机旋转数组。</summary>
        protected Quaternion[] m_CamBakeRotationArray;

        /// <summary>各视角在 RenderTexture 上的像素 Rect 数组。</summary>
        protected Rect[] m_CamBakeRectArray;

        // ──────────────────────────────────────────
        // 属性
        // ──────────────────────────────────────────

        /// <summary>组合图像总宽度（像素）。</summary>
        public int combinedImageWidth => m_CombinedImageWidth;

        /// <summary>组合图像总高度（像素）。</summary>
        public int combinedImageHeight => m_CombinedImageHeight;

        // ──────────────────────────────────────────
        // 抽象方法（子类实现）
        // ──────────────────────────────────────────

        /// <summary>初始化单视角渲染分辨率。</summary>
        protected abstract void InitBakerSize(PVSBakeSettings bakeSetting);

        /// <summary>初始化各视角 FOV 数组。</summary>
        protected abstract void InitCamFovArray();

        /// <summary>初始化各视角宽高比数组。</summary>
        protected abstract void InitCamAspectArray();

        /// <summary>初始化组合图像总尺寸。</summary>
        protected abstract void InitCombinedImageSize();

        /// <summary>初始化各视角旋转数组。</summary>
        protected abstract void InitCamBakeRotationArray();

        /// <summary>初始化各视角在 RenderTexture 上的 Rect 数组。</summary>
        protected abstract void InitCamBakeRectArray();

        // ──────────────────────────────────────────
        // 公共方法
        // ──────────────────────────────────────────

        /// <summary>
        /// 按顺序初始化所有参数并创建烘焙相机。
        /// </summary>
        public void Init(PVSBakeSettings bakeSetting)
        {
            InitBakerSize(bakeSetting);
            InitCamFovArray();
            InitCamAspectArray();
            InitCombinedImageSize();
            InitCamBakeRotationArray();
            InitCamBakeRectArray();

            m_Cam = SpawnCamera();
            m_CamTransform = m_Cam.transform;
        }

        /// <summary>
        /// 将烘焙相机移动到采样点并依次从各视角渲染到 RenderTexture。
        /// </summary>
        public void Render(Vector3 samplePos)
        {
            m_CamTransform.position = samplePos;

            int numFaces = m_CamFovArray.Length;
            for (int i = 0; i < numFaces; i++)
            {
                m_Cam.fieldOfView = m_CamFovArray[i];
                m_Cam.aspect = m_CamAspectArray[i];
                m_CamTransform.localRotation = m_CamBakeRotationArray[i];
                m_Cam.pixelRect = m_CamBakeRectArray[i];
                m_Cam.Render();
            }
        }

        /// <summary>销毁烘焙相机的 GameObject。</summary>
        public virtual void Destroy()
        {
            GameObject.DestroyImmediate(m_Cam.gameObject);
        }

        /// <summary>
        /// 创建与组合图像同尺寸的 ARGB32 RenderTexture 并绑定到烘焙相机。
        /// </summary>
        public RenderTexture CreateCamTargetTexture()
        {
            RenderTexture rtCam = RenderTexture.GetTemporary(
                combinedImageWidth, combinedImageHeight,
                32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            rtCam.filterMode = FilterMode.Point;
            rtCam.Create();

            m_Cam.targetTexture = rtCam;
            return rtCam;
        }

        // ──────────────────────────────────────────
        // 私有方法
        // ──────────────────────────────────────────

        /// <summary>创建并配置烘焙专用相机（关闭 MSAA、HDR、遮挡剔除等以确保结果纯净）。</summary>
        Camera SpawnCamera()
        {
            GameObject go = new GameObject("PVS Baker Cam");
            Camera cam = go.AddComponent<Camera>();

            cam.nearClipPlane = 0.001f;
            cam.farClipPlane = 2500f;
            cam.fieldOfView = 90f;
            cam.allowMSAA = false;
            cam.allowHDR = false;
            cam.useOcclusionCulling = false;
            cam.allowDynamicResolution = false;
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.clearFlags = CameraClearFlags.Nothing;
            cam.backgroundColor = Color.black;
            cam.aspect = 1f;
            cam.renderingPath = RenderingPath.Forward;
            cam.cullingMask = 1 << PVSConstants.CamBakeLayer | 1 << PVSConstants.CamBakeDisLayer;
            cam.enabled = false;
            cam.forceIntoRenderTexture = true;

            float[] dists = new float[32];
            dists[PVSConstants.CamBakeDisLayer] = 80;
            cam.layerCullDistances = dists;

            return cam;
        }
    }
}
