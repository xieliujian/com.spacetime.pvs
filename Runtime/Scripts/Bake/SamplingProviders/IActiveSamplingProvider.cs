using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 采样点激活状态提供者接口，用于自定义哪些世界坐标参与 PVS 烘焙。
    /// </summary>
    public interface IActiveSamplingProvider
    {
        /// <summary>提供者的唯一名称标识。</summary>
        string Name { get; }

        /// <summary>
        /// 初始化提供者（在烘焙开始前调用）。
        /// </summary>
        /// <param name="samplingPointMode">采样点分布模式。</param>
        /// <param name="forceSampleExpPoint">是否强制采样扩展点。</param>
        /// <param name="bakeCellSize">烘焙格子尺寸。</param>
        /// <param name="camMaxDisOffset">相机最大距离偏移。</param>
        void InitializeSamplingProvider(PVSSamplingPointMode samplingPointMode,
            bool forceSampleExpPoint, Vector3 bakeCellSize, float camMaxDisOffset);

        /// <summary>
        /// 判断给定世界坐标的采样点是否应参与烘焙。
        /// </summary>
        /// <param name="pos">采样点世界坐标。</param>
        /// <returns>true 表示该点应被采样。</returns>
        bool IsSamplingPositionActive(Vector3 pos);
    }
}
