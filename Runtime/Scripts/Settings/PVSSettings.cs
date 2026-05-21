using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// PVS 全局配置资产，通过 Resources.LoadAll 加载单例实例。
    /// </summary>
    [CreateAssetMenu(menuName = "SpaceTime/PVS/PVSSettings")]
    public class PVSSettings : ScriptableObject
    {
        // ──────────────────────────────────────────
        // 单例
        // ──────────────────────────────────────────

        /// <summary>全局配置单例（从 Resources 目录加载）。</summary>
        static PVSSettings m_Instance;

        /// <summary>获取全局配置单例实例。</summary>
        public static PVSSettings Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    var all = Resources.LoadAll<PVSSettings>(string.Empty);
                    if (all != null && all.Length > 0)
                    {
                        m_Instance = all[0];
                    }
                    else
                    {
                        // No PVSSettings asset found in any Resources folder.
                        // Create a transient default so the system can still run.
                        Debug.LogWarning("[PVS] No PVSSettings asset found in Resources. " +
                            "Create one via Assets > Create > SpaceTime > PVS > PVSSettings " +
                            "and place it in a Resources folder.");
                        m_Instance = ScriptableObject.CreateInstance<PVSSettings>();
                    }
                }

                return m_Instance;
            }
        }

        // ──────────────────────────────────────────
        // 序列化字段
        // ──────────────────────────────────────────

        /// <summary>是否使用 Unity 内置渲染器进行烘焙（不使用原生库时开启）。</summary>
        [Tooltip("使用 Unity 内置渲染库进行烘焙，而非原生渲染库。")]
        public bool useUnityForRendering = false;

        /// <summary>是否在 CPU 上执行 GPU Readback 计算（不支持 Compute Shader 时开启，性能较低）。</summary>
        [Tooltip("在 CPU 上执行 GPU Readback 计算，性能较低，建议先降低烘焙分辨率。")]
        public bool useUnityForRenderingCpuCompute = false;

        /// <summary>是否渲染透明材质（关闭则强制将透明物体渲染为不透明）。</summary>
        [Tooltip("关闭后，透明物体将以不透明方式参与烘焙。")]
        public bool renderTransparency = true;

        /// <summary>单帧相机渲染分辨率（影响烘焙精度与内存占用）。</summary>
        [Range(16, 2048)]
        [Tooltip("单张相机渲染图的分辨率（1/6 视角），增大可减少远距离物体弹出，减小可降低内存占用。")]
        public int bakeCameraResolution = 1024;

        /// <summary>平均采样速度（毫秒），用于估算烘焙剩余时间。</summary>
        [Tooltip("单次采样的平均耗时（ms），用于 ETA 估算。")]
        public float bakeAverageSamplingSpeedMs = 4f;

        // ──────────────────────────────────────────
        // 属性
        // ──────────────────────────────────────────

        /// <summary>烘焙相机渲染宽度（等于 bakeCameraResolution）。</summary>
        public int bakeCameraResolutionWidth => bakeCameraResolution;

        /// <summary>烘焙相机渲染高度（等于 bakeCameraResolution）。</summary>
        public int bakeCameraResolutionHeight => bakeCameraResolution;
    }
}
