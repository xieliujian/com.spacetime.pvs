using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 描述一个 PVS 采样点的完整信息（世界坐标、包围盒、叶节点索引等）。
    /// </summary>
    public class PVSWorldPosInfo
    {
        /// <summary>采样点世界坐标。</summary>
        public Vector3 pos;

        /// <summary>该采样点对应的包围盒。</summary>
        public Bounds bounds;

        /// <summary>是否为强制采样点（即使被 SamplingProvider 过滤也必须采样）。</summary>
        public bool isForceSamplePos;

        /// <summary>所属叶节点的索引。</summary>
        public int leafNodeIdx;

        /// <summary>采样点位置偏移掩码，用于多层次密度区域判断。</summary>
        public uint samplePosOffsetMask;

        /// <summary>该点是否处于默认采样偏移区域内。</summary>
        public bool isInDefaultSampleOffsetArea;
    }
}
