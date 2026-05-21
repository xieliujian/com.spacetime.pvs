using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// PVS 系统全局常量定义。
    /// </summary>
    public static class PVSConstants
    {
        // ──────────────────────────────────────────
        // 路径
        // ──────────────────────────────────────────

        /// <summary>PVS 资源在工程中的根目录。</summary>
        public const string BaseFolder = @"Assets/SpaceTime";

        /// <summary>用于烘焙可见性计算的 Compute Shader 路径（位于包内 Shader 目录）。</summary>
        public static readonly string ComputeShaderPath = @"Packages/com.spacetime.pvs/Shader/ExtractUniquePoints.compute";

        /// <summary>烘焙用双面剔除材质路径（位于包内 Shader/Materials 目录）。</summary>
        public static readonly string s_MatCullOffPath = @"Packages/com.spacetime.pvs/Shader/Materials/PerfectCulling_UnlitTag_CullOff.mat";

        /// <summary>烘焙用背面剔除材质路径（位于包内 Shader/Materials 目录）。</summary>
        public static readonly string s_MatCullBackPath = @"Packages/com.spacetime.pvs/Shader/Materials/PerfectCulling_UnlitTag_CullBack.mat";

        /// <summary>多场景烘焙临时场景路径。</summary>
        public static readonly string MultiSceneTempPath = System.IO.Path.Combine(BaseFolder, @"PVS_Temp.unity");

        // ──────────────────────────────────────────
        // 标签
        // ──────────────────────────────────────────

        /// <summary>强制使用背面剔除的渲染器 Tag。</summary>
        public static readonly string s_Tag_ForceCullBack = "PVS_ForceCullBack";

        // ──────────────────────────────────────────
        // 限制
        // ──────────────────────────────────────────

        /// <summary>
        /// 支持的最大渲染器数量（使用 ushort 存储索引，上限为 65535）。
        /// 建议使用重叠体积或减少独立渲染器数量，而非修改此值。
        /// </summary>
        public const int MaxRenderers = ushort.MaxValue;

        /// <summary>
        /// 每批次采样点数量（影响 GPU 内存占用与烘焙速度）。
        /// 增大可加速烘焙，减小可降低内存压力。
        /// </summary>
        public const int SampleBatchCount = 2048;

        // ──────────────────────────────────────────
        // 相机与层级
        // ──────────────────────────────────────────

        /// <summary>烘焙相机使用的主渲染层（Layer 30）。</summary>
        public const int CamBakeLayer = 30;

        /// <summary>烘焙相机使用的距离剔除层（Layer 31）。</summary>
        public const int CamBakeDisLayer = 31;

        // ──────────────────────────────────────────
        // 渲染控制
        // ──────────────────────────────────────────

        /// <summary>渲染器显隐切换模式（默认使用 forceRenderingOff 以获得最佳性能）。</summary>
        public const PVSRenderToggleMode ToggleRenderMode = PVSRenderToggleMode.ToggleForceRenderingOff;

        /// <summary>烘焙相机渲染的清屏颜色（黑色，不参与颜色哈希）。</summary>
        public static Color ClearColor = Color.black;

        // ──────────────────────────────────────────
        // 调试 / 安全
        // ──────────────────────────────────────────

        /// <summary>开启后会对数据做额外合法性检查，有轻微性能开销。</summary>
        public const bool SafetyChecks = true;

        /// <summary>是否允许烘焙结束后重新加载场景（关闭可用于调试）。</summary>
        public static bool AllowSceneReload = true;

        // ──────────────────────────────────────────
        // 支持的渲染器类型
        // ──────────────────────────────────────────

        /// <summary>PVS 烘焙支持的渲染器类型集合。</summary>
        public static readonly HashSet<System.Type> SupportedRendererTypes = new HashSet<System.Type>()
        {
            typeof(MeshRenderer),
            typeof(SkinnedMeshRenderer),
        };
    }
}
