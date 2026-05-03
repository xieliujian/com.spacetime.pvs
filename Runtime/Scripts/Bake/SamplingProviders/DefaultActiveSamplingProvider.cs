using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 默认采样点激活状态提供者：将处于 PVSExcludeVolume 内的采样点标记为非激活。
    /// </summary>
    public class DefaultActiveSamplingProvider : IActiveSamplingProvider
    {
        // ──────────────────────────────────────────
        // 字段
        // ──────────────────────────────────────────

        /// <summary>场景中所有排除体积的缓存数组。</summary>
        PVSExcludeVolume[] m_ExcludeVolumes;

        // ──────────────────────────────────────────
        // IActiveSamplingProvider
        // ──────────────────────────────────────────

        /// <summary>提供者名称。</summary>
        public static string DefaultName => nameof(DefaultActiveSamplingProvider);

        /// <inheritdoc/>
        public string Name => DefaultName;

        /// <inheritdoc/>
        public void InitializeSamplingProvider(PVSSamplingPointMode samplingPointMode,
            bool forceSampleExpPoint, Vector3 bakeCellSize, float camMaxDisOffset)
        {
            m_ExcludeVolumes = Object.FindObjectsOfType<PVSExcludeVolume>();
        }

        /// <inheritdoc/>
        public bool IsSamplingPositionActive(Vector3 pos)
        {
            if (m_ExcludeVolumes != null)
            {
                foreach (var bound in m_ExcludeVolumes)
                {
                    if (bound.IsPositionActive(pos))
                        return false;
                }
            }

            return true;
        }
    }
}
