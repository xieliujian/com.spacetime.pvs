using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// 基于 Unity 渲染管线和 Compute Shader 的 PVS Baker 实现。
    /// </summary>
    public class PVSBakerUnity : PVSBaker
    {
        // ──────────────────────────────────────────
        // 字段
        // ──────────────────────────────────────────

        /// <summary>场景颜色管理器，负责将渲染器替换为唯一颜色材质。</summary>
        PVSSceneColor m_SceneColor;

        /// <summary>当前使用的烘焙相机实现（FOV 45 或 FOV 90）。</summary>
        PVSBakerBaseCam m_CamBaker;

        /// <summary>提取唯一像素的 Compute Shader。</summary>
        readonly ComputeShader m_ImageComputeShader;

        /// <summary>CSMain 内核 ID。</summary>
        readonly int m_KernelMain;

        /// <summary>CSExtract 内核 ID。</summary>
        readonly int m_KernelExtract;

        /// <summary>256×256 的输出哈希 RenderTexture（用于颜色去重）。</summary>
        readonly RenderTexture m_OutputHashRT;

        /// <summary>烘焙开始前保存的图形管线资产，结束后恢复。</summary>
        readonly RenderPipelineAsset m_ActiveGraphicsPipeline;

        /// <summary>烘焙开始前保存的质量管线资产，结束后恢复。</summary>
        readonly RenderPipelineAsset m_ActiveQualityPipeline;

        /// <summary>烘焙开始前保存的 LOD Bias，结束后恢复。</summary>
        float m_SaveLodBias = 2f;

        // Shader 属性 ID
        readonly int m_PropInput = Shader.PropertyToID("Input");
        readonly int m_PropOutputWrite = Shader.PropertyToID("Output_Write");
        readonly int m_PropOutputRead = Shader.PropertyToID("Output_Read");
        readonly int m_PropAppendDataBuffer = Shader.PropertyToID("AppendDataBuffer");

        // ──────────────────────────────────────────
        // 构造
        // ──────────────────────────────────────────

        /// <summary>
        /// 初始化烘焙器：切换管线、准备场景、创建相机和 RenderTexture。
        /// </summary>
        public PVSBakerUnity(PVSBakeSettings bakeSettings) : base(bakeSettings)
        {
            m_ActiveGraphicsPipeline = GraphicsSettings.renderPipelineAsset;
            GraphicsSettings.renderPipelineAsset = null;

#if UNITY_2019_1_OR_NEWER
            m_ActiveQualityPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = null;
            m_SaveLodBias = QualitySettings.lodBias;
            QualitySettings.lodBias = 200;
#endif

#if UNITY_EDITOR
            m_ImageComputeShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(PVSConstants.ComputeShaderPath);
#endif

            if (m_ImageComputeShader == null)
            {
                Logger.LogError("PVSBakerUnity: 无法加载 Compute Shader，请检查资源路径。");
                return;
            }

            PrepareScene();

            m_SceneColor = new PVSSceneColor(bakeSettings.Groups,
                m_BakeSettings.AdditionalOccluders, true, m_BakeSettings.isCheckSamplePosOffsetMask);

            m_OutputHashRT = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            m_OutputHashRT.autoGenerateMips = false;
            m_OutputHashRT.enableRandomWrite = true;
            m_OutputHashRT.Create();

            if (bakeSettings.isBakerUseFov90)
            {
                m_CamBaker = new PVSBakerCam_HorizFov90();
                Logger.LogDebugF("PVSBakerUnity: 使用 FOV 90 相机模式。");
            }
            else
            {
                m_CamBaker = new PVSBakerCam_HorizFov45();
                Logger.LogDebugF("PVSBakerUnity: 使用 FOV 45 相机模式。");
            }

            m_CamBaker.Init(bakeSettings);

            m_KernelMain = m_ImageComputeShader.FindKernel("CSMain");
            m_KernelExtract = m_ImageComputeShader.FindKernel("CSExtract");
        }

        // ──────────────────────────────────────────
        // PVSBaker 重写
        // ──────────────────────────────────────────

        /// <inheritdoc/>
        public override void Dispose()
        {
            GraphicsSettings.renderPipelineAsset = m_ActiveGraphicsPipeline;

#if UNITY_2019_1_OR_NEWER
            QualitySettings.renderPipeline = m_ActiveQualityPipeline;
            QualitySettings.lodBias = m_SaveLodBias;
#endif

            m_SceneColor.Dispose();
            m_SceneColor = null;

            GameObject.DestroyImmediate(m_OutputHashRT);
            m_CamBaker.Destroy();
        }

        /// <inheritdoc/>
        public override PVSBakerHandle SamplePosition(Vector3 pos)
        {
            RenderTexture rtCam = m_CamBaker.CreateCamTargetTexture();

            RenderTexture.active = rtCam;
            GL.Clear(true, true, PVSConstants.ClearColor);

            m_CamBaker.Render(pos);

            if (PVSSettings.Instance.useUnityForRenderingCpuCompute)
            {
                int w = m_CamBaker.combinedImageWidth;
                int h = m_CamBaker.combinedImageHeight;

                Texture2D cpuTxt = new Texture2D(w, h, TextureFormat.RGBA32, false);
                cpuTxt.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                cpuTxt.Apply();

                Color32[] pixels = cpuTxt.GetPixels32();
                Object.DestroyImmediate(cpuTxt);

                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rtCam);

                return new PVSBakerUnityCpuHandle()
                {
                    m_Hash = m_SceneColor.Hash,
                    Pixels = pixels
                };
            }

            RenderTexture.active = null;

            PVSBakerHandle handle = GetResult(rtCam);
            RenderTexture.ReleaseTemporary(rtCam);

            return handle;
        }

        // ──────────────────────────────────────────
        // 私有方法
        // ──────────────────────────────────────────

        /// <summary>禁用场景中所有相机、光照、反射探针和渲染器，确保烘焙结果纯净。</summary>
        void PrepareScene()
        {
            foreach (Camera cam in Object.FindObjectsOfType<Camera>())
                cam.enabled = false;

            foreach (Light light in Object.FindObjectsOfType<Light>())
            {
                MonoBehaviour[] allMonos = light.GetComponents<MonoBehaviour>();
                for (int i = allMonos.Length - 1; i >= 0; --i)
                {
                    MonoBehaviour m = allMonos[i];
                    if (m == null || m.GetType() == null || m.GetType().Name == null)
                        continue;

                    if (m.GetType().Name.ToLower().StartsWith("universaladditional"))
                        Object.DestroyImmediate(m);
                }

                Object.DestroyImmediate(light);
            }

            foreach (ReflectionProbe probe in Object.FindObjectsOfType<ReflectionProbe>())
                Object.DestroyImmediate(probe);

            foreach (Renderer r in Object.FindObjectsOfType<Renderer>())
                r.enabled = false;

            RenderSettings.fog = false;

#if UNITY_2020_1_OR_NEWER && UNITY_EDITOR
            try
            {
                UnityEditor.Lightmapping.lightingSettings.autoGenerate = false;
            }
            catch (Exception) { }
#endif
        }

        /// <summary>通过 Compute Shader 从 RenderTexture 提取唯一颜色并返回 GPU 读回句柄。</summary>
        PVSBakerHandle GetResult(Texture input)
        {
            int w = m_CamBaker.combinedImageWidth;
            int h = m_CamBaker.combinedImageHeight;

            ComputeBuffer appendBuf = new ComputeBuffer(PVSConstants.MaxRenderers, sizeof(int), ComputeBufferType.Append);
            ComputeBuffer countBuf = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);

            appendBuf.SetCounterValue(0);

            m_ImageComputeShader.SetTexture(m_KernelMain, m_PropInput, input);
            m_ImageComputeShader.SetTexture(m_KernelMain, m_PropOutputWrite, m_OutputHashRT);
            m_ImageComputeShader.SetTexture(m_KernelExtract, m_PropOutputWrite, m_OutputHashRT);
            m_ImageComputeShader.SetTexture(m_KernelExtract, m_PropOutputRead, m_OutputHashRT);
            m_ImageComputeShader.SetBuffer(m_KernelExtract, m_PropAppendDataBuffer, appendBuf);

            m_ImageComputeShader.Dispatch(m_KernelMain, w / 16, h / 16, 1);
            m_ImageComputeShader.Dispatch(m_KernelExtract, 256 / 16, 256 / 16, 1);

            return new PVSBakerUnityHandle()
            {
                appendBuf = appendBuf,
                countBuf = countBuf,
                m_Hash = m_SceneColor.Hash
            };
        }
    }
}
