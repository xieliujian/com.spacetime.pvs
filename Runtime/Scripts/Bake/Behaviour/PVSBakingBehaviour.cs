using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 采样点分布模式枚举。
    /// </summary>
    public enum PVSSamplingPointMode
    {
        /// <summary>高密度采样。</summary>
        High,
        /// <summary>高密度 + 左右采样。</summary>
        High_LeftRight,
        /// <summary>高密度 + 左右 + 向下采样。</summary>
        High_LeftRight_Down,
        /// <summary>手动分割采样。</summary>
        ManualSplit,
    }

    /// <summary>
    /// PVS 烘焙 Behaviour 抽象基类，提供采样点管理、分组控制等通用功能。
    /// 子类可通过重写抽象方法实现具体的采样策略和数据存储。
    /// </summary>
    [ExecuteInEditMode]
    public abstract partial class PVSBakingBehaviour : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // 采样提供者
        // ──────────────────────────────────────────

        /// <summary>当前注册的采样点激活状态提供者集合。</summary>
        public HashSet<IActiveSamplingProvider> SamplingProviders = new HashSet<IActiveSamplingProvider>();

        /// <summary>注册采样点激活状态提供者。</summary>
        public void AddSamplingProvider(IActiveSamplingProvider samplingProvider) =>
            SamplingProviders.Add(samplingProvider);

        /// <summary>移除采样点激活状态提供者。</summary>
        public void RemoveSamplingProvider(IActiveSamplingProvider samplingProvider) =>
            SamplingProviders.Remove(samplingProvider);

        /// <summary>初始化所有已注册的采样点提供者。</summary>
        public void InitializeAllSamplingProviders()
        {
            float dist = CalcCamMaxDisOffset();

            foreach (var provider in SamplingProviders)
                provider.InitializeSamplingProvider(samplingPointMode, forceSampleExpPoint, bakeCellSize, dist);
        }

        /// <summary>
        /// 判断给定坐标是否被所有采样提供者接受（任意一个拒绝即返回 false）。
        /// </summary>
        public bool SamplingProvidersIsPositionActive(Vector3 pos)
        {
            foreach (var provider in SamplingProviders)
            {
                if (!provider.IsSamplingPositionActive(pos))
                    return false;
            }

            return true;
        }

        /// <summary>手动分割模式下，判断指定坐标是否激活。子类须重写此方法。</summary>
        public abstract bool ManualSplitSamplingProviderIsPosActive(Vector3 pos);

        // ──────────────────────────────────────────
        // 序列化字段
        // ──────────────────────────────────────────

        /// <summary>是否使用流式数据模式（运行时按需加载）。</summary>
        [SerializeField]
        bool m_bStreamMode = false;

        /// <summary>烘焙渲染器分组数组（序列化存储，运行时由 Init 初始化）。</summary>
        [SerializeField]
        [HideInInspector]
        public PVSBakeGroup[] bakeGroups = Array.Empty<PVSBakeGroup>();

        /// <summary>额外的遮挡物渲染器列表（仅作为遮挡体，不参与可见性记录）。</summary>
        [SerializeField] public List<Renderer> additionalOccluders = new List<Renderer>();

        /// <summary>草丛分组的起始索引（-1 表示无草丛分组）。</summary>
        [SerializeField] public int grassGroupBegin;

        /// <summary>采样点分布模式。</summary>
        [SerializeField] public PVSSamplingPointMode samplingPointMode = PVSSamplingPointMode.High_LeftRight_Down;

        /// <summary>是否强制采样扩展点。</summary>
        [SerializeField] public bool forceSampleExpPoint = false;

        /// <summary>单个格子尺寸（需能整除体积尺寸）。</summary>
        [Tooltip("单个格子的尺寸，需能整除体积的缩放值。")]
        [SerializeField] public Vector3 bakeCellSize = new Vector3(10, 5, 10);

        /// <summary>是否开启相机最大距离偏移。</summary>
        [SerializeField] public bool openCamMaxDisOffset;

        /// <summary>相机最大距离偏移值（米）。</summary>
        [SerializeField] public float camMaxDisOffset = 15f;

        /// <summary>是否开启 Baker FOV 选择。</summary>
        [SerializeField] public bool openBakerFovSel;

        /// <summary>Baker 是否使用 FOV 90 模式（false 则使用 FOV 45）。</summary>
        [SerializeField] public bool bakerFov90;

        /// <summary>是否开启忽略射线检查默认偏移类型。</summary>
        [SerializeField] public bool openIgnoreRayCheckDefaultOffsetType;

        /// <summary>是否忽略射线检查默认偏移类型。</summary>
        [SerializeField] public bool ignoreRayCheckDefaultOffsetType;

        // ──────────────────────────────────────────
        // 非序列化运行时字段
        // ──────────────────────────────────────────

        /// <summary>是否处于单点烘焙模式（调试用）。</summary>
        [NonSerialized] public bool isSinglePointBakeMode = false;

        /// <summary>单点烘焙模式下指定的采样点世界坐标。</summary>
        [NonSerialized] public Vector3 singleBakePoint;

        /// <summary>是否检查采样点位置偏移掩码。</summary>
        protected bool m_CheckSamplePosOffsetMask = true;

        /// <summary>是否检查采样点位置偏移掩码（只读）。</summary>
        public bool checkSamplePosOffsetMask => m_CheckSamplePosOffsetMask;

        // ──────────────────────────────────────────
        // 抽象属性
        // ──────────────────────────────────────────

        /// <summary>关联的烘焙数据资产（子类须提供具体实现）。</summary>
        public virtual PVSBakeData BakeData { get; } = null;

        // ──────────────────────────────────────────
        // 生命周期
        // ──────────────────────────────────────────

        /// <summary>场景启动时初始化各分组的运行时渲染器数据。</summary>
        public virtual void Start()
        {
            int nIdx = 0;
            foreach (PVSBakeGroup group in bakeGroups)
            {
                if (!m_bStreamMode)
                    group.Init(nIdx);

                nIdx++;
            }
        }

        // ──────────────────────────────────────────
        // 公共方法
        // ──────────────────────────────────────────

        /// <summary>是否处于流式数据模式。</summary>
        public bool IsStreamMode() => m_bStreamMode;

        /// <summary>Baker 是否使用 FOV 90 模式。未开启 FOV 选择时默认使用 FOV 90。</summary>
        public bool IsBakerFov90()
        {
            if (openBakerFovSel)
                return bakerFov90;

            return true;
        }

        /// <summary>是否忽略射线检查默认偏移类型。</summary>
        public bool IsIgnoreRayCheckDefaultOffsetType()
        {
            if (openIgnoreRayCheckDefaultOffsetType)
                return ignoreRayCheckDefaultOffsetType;

            return false;
        }

        /// <summary>清理指定 pvsId 对应的分组运行时数据。</summary>
        public void RemoveGroup(int pvsId)
        {
            if (pvsId < 0)
                return;

            if (bakeGroups.Length <= pvsId)
                return;

            bakeGroups[pvsId].Clear();
        }

        /// <summary>以数组重置指定分组的运行时渲染器。</summary>
        public void AddGroup(int pvsId, Renderer[] renders)
        {
            if (pvsId < 0)
                return;

            if (bakeGroups.Length <= pvsId)
                return;

            bakeGroups[pvsId].ResetData(renders);
        }

        /// <summary>以列表重置指定分组的运行时渲染器。</summary>
        public void AddGroup(int pvsId, List<Renderer> renders)
        {
            if (pvsId < 0)
                return;

            if (bakeGroups.Length <= pvsId)
                return;

            bakeGroups[pvsId].ResetData(renders);
        }

        /// <summary>清理所有分组并切换到流式数据模式。</summary>
        public void ProcessStreamMode()
        {
            foreach (PVSBakeGroup group in bakeGroups)
                group.Clear();

            m_bStreamMode = true;
        }

        /// <summary>批量切换指定状态的渲染器显隐（按 visibleRenderers 掩码过滤）。</summary>
        public void ToggleAllRenderersByState(bool state, bool[] visibleRenderers, bool forceNullCheck = false)
        {
            int nIdx = 0;
            int nMaxCnt = bakeGroups.Length;
            if (grassGroupBegin != -1)
                nMaxCnt = grassGroupBegin;

            for (int i = 0; i < nMaxCnt; i++)
            {
                if (!visibleRenderers[nIdx])
                    bakeGroups[i].Toggle(state, forceNullCheck);

                nIdx++;
            }
        }

        /// <summary>切换所有分组渲染器的显隐状态。</summary>
        public void ToggleAllRenderers(bool state, bool forceNullCheck = false)
        {
            foreach (PVSBakeGroup r in bakeGroups)
                r.Toggle(state, forceNullCheck);
        }

        // ──────────────────────────────────────────
        // 抽象方法（子类须实现）
        // ──────────────────────────────────────────

        /// <summary>计算相机最大距离偏移值。</summary>
        public abstract float CalcCamMaxDisOffset();

        /// <summary>获取指定空间内所有采样点信息列表。</summary>
        public virtual List<PVSWorldPosInfo> GetSamplingPosInfoList(Space space, bool isSampleNeighbor,
            Vector3 volumePos, Quaternion volumeRot, Vector3 volumeSize, Vector3 cellNumVec, Vector3 cellSize)
            => throw new NotImplementedException();

        /// <summary>根据世界坐标获取可见渲染器索引列表。</summary>
        public virtual int GetIndicesForWorldPos(Vector3 worldPos, RapidList<ushort> indices, bool isSampleData,
            out Vector3 samplePos, out int leafNodeIdx, out uint samplePosOffsetMask)
            => throw new NotImplementedException();

        /// <summary>根据世界坐标获取格子一维索引。</summary>
        public virtual int GetIndexForWorldPos(Vector3 worldPos, out bool isOutOfBounds) => throw new NotImplementedException();

        /// <summary>根据世界坐标和格子尺寸获取格子一维索引。</summary>
        public virtual int GetIndexForWorldPos(Vector3 pos, Vector3 cellSize, out bool isOutOfBounds) => throw new NotImplementedException();

        /// <summary>根据格子索引获取可见渲染器索引列表。</summary>
        public virtual void GetIndicesForIndex(int index, RapidList<ushort> indices, Vector3 vPos, bool isSampleData,
            out Vector3 samplePos, out int leafNodeIdx, out uint samplePosOffsetMask)
            => BakeData.SampleAtIndex(index, indices, vPos, isSampleData, out samplePos, out leafNodeIdx, out samplePosOffsetMask);

        /// <summary>烘焙前预处理场景（子类实现）。</summary>
        public virtual void PreBakeProcessScene() => throw new NotImplementedException();

        /// <summary>烘焙前准备（子类实现；返回 false 表示中止）。</summary>
        public virtual bool PreBake() => throw new NotImplementedException();

        /// <summary>烘焙后收尾（子类实现）。</summary>
        public virtual void PostBake() => throw new NotImplementedException();

        /// <summary>根据激活采样点总数设置是否保存大索引。</summary>
        public virtual void SetSaveBigVisIndex(int activeSamplingPositionsCount) => throw new NotImplementedException();

        /// <summary>获取所有采样点信息列表（含强制点）。</summary>
        public virtual List<PVSWorldPosInfo> FindWorldPosInfoList() => throw new NotImplementedException();

        /// <summary>从额外遮挡物中剔除不合适的渲染器。</summary>
        protected virtual void CullAdditionalOccluders(ref HashSet<Renderer> additionalOccluders) => throw new NotImplementedException();

        /// <summary>判断采样点是否合法。</summary>
        public virtual bool IsSamplePosValid(Vector3 samplePos, bool isMaxDensityAreaExist) => throw new NotImplementedException();

        /// <summary>判断是否存在最大密度采样区域。</summary>
        public virtual bool IsMaxDensityAreaExist(bool isReCollect) => throw new NotImplementedException();

        /// <summary>计算采样点位置偏移掩码。</summary>
        public virtual void CalcSamplePosOffsetMask(List<PVSBakeSettings.SamplingLocation> samplingLocations) => throw new NotImplementedException();

        // ──────────────────────────────────────────
        // 私有方法
        // ──────────────────────────────────────────

        /// <summary>完成所有待处理的烘焙句柄并写入数据。</summary>
        void CompletePending(List<PVSBakeHandle> pending)
        {
            for (int k = 0; k < pending.Count; ++k)
            {
                pending[k].Handle.Complete();
                BakeData.SetRawData(pending[k].Index, pending[k].Handle.indices);
            }

            pending.Clear();
        }
    }
}
