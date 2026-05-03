using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// PVS 烘焙数据的抽象基类，负责存储和访问可见性数据。
    /// </summary>
    public abstract class PVSBakeData : ScriptableObject
    {
#pragma warning disable 414
        /// <summary>烘焙数据版本号，用于兼容性校验。</summary>
        [SerializeField]
        public int bakeDataVersion = 4;

        /// <summary>烘焙是否已正常完成；未完成时数据可能不完整。</summary>
        [HideInInspector]
        [SerializeField]
        public bool bakeCompleted = false;
#pragma warning restore 414

        // ──────────────────────────────────────────
        // 抽象接口
        // ──────────────────────────────────────────

        /// <summary>填充指定格子的流式数据。</summary>
        public virtual void FillStreamData(int nIdx, byte[] datas, int pvsSize, bool useNative) => throw new System.NotImplementedException();

        /// <summary>移除指定格子的流式数据。</summary>
        public virtual void RemoveStreamData(int idx) => throw new System.NotImplementedException();

        /// <summary>以原始索引数组写入指定格子的可见性数据。</summary>
        public virtual void SetRawData(int index, ushort[] indices, bool validateData = true) => throw new System.NotImplementedException();

        /// <summary>
        /// 采样指定格子索引处的可见渲染器索引列表。
        /// </summary>
        /// <param name="index">格子一维索引。</param>
        /// <param name="indices">输出的可见渲染器索引集合。</param>
        /// <param name="pos">采样世界坐标。</param>
        /// <param name="isSampleData">是否为采样模式。</param>
        /// <param name="samplePos">输出的实际采样点坐标。</param>
        /// <param name="leafNodeIdx">输出的叶节点索引。</param>
        /// <param name="samplePosOffsetMask">输出的采样偏移掩码。</param>
        public virtual void SampleAtIndex(int index, RapidList<ushort> indices, Vector3 pos, bool isSampleData,
            out Vector3 samplePos, out int leafNodeIdx, out uint samplePosOffsetMask)
            => throw new System.NotImplementedException();

        /// <summary>完成烘焙后执行的收尾操作（压缩数据等）。</summary>
        public virtual void CompleteBake() => throw new System.NotImplementedException();

        /// <summary>在 Inspector 中绘制烘焙数据详情（由 Editor 调用）。</summary>
        public virtual void DrawInspectorGUI() => throw new System.NotImplementedException();

        /// <summary>获取指定叶节点的格子尺寸。</summary>
        public virtual Vector3 GetCellSize(int leafNodeIdx) => throw new System.NotImplementedException();

        /// <summary>设置是否保存大索引可见性数据。</summary>
        public virtual void SetSaveBigVisIndex(bool saveBigVisIndex) => throw new System.NotImplementedException();
    }
}
