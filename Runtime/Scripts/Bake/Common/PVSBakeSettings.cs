using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 传递给 Baker 的烘焙参数集合。
    /// </summary>
    public class PVSBakeSettings
    {
        /// <summary>参与烘焙的渲染器分组数组。</summary>
        public PVSBakeGroup[] Groups;

        /// <summary>额外遮挡物渲染器集合（不参与可见性记录，仅作为遮挡体使用）。</summary>
        public HashSet<Renderer> AdditionalOccluders;

        /// <summary>烘焙相机渲染宽度（像素）。</summary>
        public int Width;

        /// <summary>烘焙相机渲染高度（像素）。</summary>
        public int Height;

        /// <summary>是否检查采样点位置偏移掩码。</summary>
        public bool isCheckSamplePosOffsetMask;

        /// <summary>Baker 是否使用 FOV 90 相机模式。</summary>
        public bool isBakerUseFov90;

        /// <summary>
        /// 单个采样点的位置和激活状态描述。
        /// </summary>
        public struct SamplingLocation
        {
            /// <summary>采样点的世界坐标。</summary>
            public readonly Vector3 Position;

            /// <summary>该采样点是否参与实际烘焙。</summary>
            public readonly bool Active;

            /// <summary>
            /// 构造采样点描述。
            /// </summary>
            /// <param name="position">世界坐标。</param>
            /// <param name="active">是否激活。</param>
            public SamplingLocation(Vector3 position, bool active)
            {
                Position = position;
                Active = active;
            }
        }
    }
}
