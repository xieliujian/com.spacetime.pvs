using System.Collections.Generic;
using UnityEngine;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// PVS 烘焙渲染器分组，管理一组渲染器的运行时显隐切换。
    /// </summary>
    [System.Serializable]
    public class PVSBakeGroup
    {
        // ──────────────────────────────────────────
        // 类型定义
        // ──────────────────────────────────────────

        /// <summary>分组类型，用于区分 LOD、用户自定义、植被等场景。</summary>
        public enum GroupType : byte
        {
            /// <summary>其他类型。</summary>
            Other,
            /// <summary>LOD 分组。</summary>
            LOD,
            /// <summary>用户自定义分组。</summary>
            User,
            /// <summary>植被分组。</summary>
            Foliage
        }

        // ──────────────────────────────────────────
        // 静态字段
        // ──────────────────────────────────────────

        /// <summary>渲染器数组对象池，按数组长度分桶，复用内存以降低 GC 压力。</summary>
        static Dictionary<int, Queue<Renderer[]>> FastListPool = new Dictionary<int, Queue<Renderer[]>>();

        const string TERRAIN_NAME_CONTAIN = "Terrain";

        // ──────────────────────────────────────────
        // 序列化字段（烘焙数据）
        // ──────────────────────────────────────────

        /// <summary>分组类型。</summary>
        public GroupType groupType;

        /// <summary>烘焙时保存的渲染器引用数组。</summary>
        public Renderer[] renderers;

        /// <summary>分组内所有渲染器的顶点总数（编辑器统计用）。</summary>
        public int vertexCount;

#if UNITY_EDITOR
        /// <summary>地形渲染器名称列表（编辑器专用，用于地形匹配）。</summary>
        public List<string> m_TerNameList = new List<string>();
#endif

        // ──────────────────────────────────────────
        // 运行时字段
        // ──────────────────────────────────────────

        /// <summary>运行时渲染器数组（从对象池分配）。</summary>
        [System.NonSerialized] Renderer[] runtimeGroupData;

        /// <summary>运行时渲染器列表（使用 List 模式时有效）。</summary>
        [System.NonSerialized] List<Renderer> runtimeGroupDataList;

        /// <summary>运行时渲染器有效数量。</summary>
        [System.NonSerialized] short runtimeGroupDataSize;

        /// <summary>当前显隐状态。</summary>
        [System.NonSerialized] bool bEnable = true;

        /// <summary>运行时数组是否由对象池分配（需要归还）。</summary>
        [System.NonSerialized] bool bAllocData = false;

        /// <summary>是否使用 List 模式（由 InitByData(List) 初始化时为 true）。</summary>
        [System.NonSerialized] bool useListData = false;

        // ──────────────────────────────────────────
        // 初始化 / 清理
        // ──────────────────────────────────────────

        /// <summary>
        /// 清理运行时数据并将渲染器数组归还对象池。
        /// </summary>
        public void Clear()
        {
            if (runtimeGroupData != null && bAllocData)
            {
                for (int i = 0; i < runtimeGroupData.Length; i++)
                    runtimeGroupData[i] = null;

                if (FastListPool.ContainsKey(runtimeGroupData.Length))
                    FastListPool[runtimeGroupData.Length].Enqueue(runtimeGroupData);
                else
                {
                    var data = new Queue<Renderer[]>();
                    data.Enqueue(runtimeGroupData);
                    FastListPool.Add(runtimeGroupData.Length, data);
                }
            }

            runtimeGroupDataList = null;
            runtimeGroupData = null;
            renderers = null;
        }

        /// <summary>
        /// 重置运行时渲染器数组并重新设置显隐状态。
        /// </summary>
        public void ResetData(Renderer[] renders)
        {
            ClearRuntimeRenderers();
            InitByData(renders);
            Toggle(bEnable);
        }

        /// <summary>
        /// 重置运行时渲染器列表并重新设置显隐状态。
        /// </summary>
        public void ResetData(List<Renderer> renders)
        {
            ClearRuntimeRenderers();
            InitByData(renders);
            Toggle(bEnable);
        }

        /// <summary>以外部渲染器数组初始化运行时数据（不从对象池分配）。</summary>
        public void InitByData(Renderer[] tempRenders)
        {
            runtimeGroupData = tempRenders;
            bAllocData = false;
            runtimeGroupDataSize = (short)tempRenders.Length;
        }

        /// <summary>以外部渲染器列表初始化运行时数据（List 模式）。</summary>
        public void InitByData(List<Renderer> tempRenders)
        {
            useListData = true;
            bAllocData = false;
            runtimeGroupDataList = tempRenders;
            runtimeGroupDataSize = (short)tempRenders.Count;
        }

        /// <summary>
        /// 从序列化的 renderers 数组初始化运行时数据（编辑器或场景加载时调用）。
        /// </summary>
        public void Init(int nIdx = 0)
        {
            if (FastListPool.ContainsKey(renderers.Length) && FastListPool[renderers.Length].Count > 0)
                runtimeGroupData = FastListPool[renderers.Length].Dequeue();
            else
                runtimeGroupData = new Renderer[renderers.Length];

            foreach (Renderer r in renderers)
            {
                if (r == null)
                    continue;

                PushRuntimeRenderer(r);
            }
        }

        // ──────────────────────────────────────────
        // 运行时渲染器管理
        // ──────────────────────────────────────────

        /// <summary>检查运行时渲染器列表中是否包含指定渲染器。</summary>
        public bool ContainsRuntimeRenderer(Renderer r)
        {
            if (useListData)
            {
                for (int i = 0; i < runtimeGroupDataList.Count; ++i)
                {
                    if (r == runtimeGroupDataList[i])
                        return true;
                }
            }
            else
            {
                for (int i = 0; i < runtimeGroupDataSize; ++i)
                {
                    if (r == runtimeGroupData[i])
                        return true;
                }
            }

            return false;
        }

        /// <summary>向运行时渲染器数组末尾追加一个渲染器（容量不足时自动翻倍扩容）。</summary>
        public void PushRuntimeRenderer(Renderer renderer)
        {
            if (runtimeGroupDataSize >= runtimeGroupData.Length)
                System.Array.Resize(ref runtimeGroupData, runtimeGroupDataSize * 2);

            runtimeGroupData[runtimeGroupDataSize] = renderer;
            ++runtimeGroupDataSize;
        }

        /// <summary>将运行时渲染器计数重置为 0（不释放内存）。</summary>
        public void ClearRuntimeRenderers()
        {
            runtimeGroupDataSize = 0;
        }

        /// <summary>返回运行时渲染器的有效数量。</summary>
        public int GetRuntimeRendererCount()
        {
            return runtimeGroupDataSize;
        }

        // ──────────────────────────────────────────
        // 材质属性块
        // ──────────────────────────────────────────

        /// <summary>为分组内所有运行时渲染器设置 MaterialPropertyBlock 颜色。</summary>
        public void SetMatPropBlockColor(MaterialPropertyBlock block)
        {
            if (runtimeGroupData == null)
                return;

            for (int i = 0; i < runtimeGroupDataSize; ++i)
            {
                Renderer groupContent = runtimeGroupData[i];
                if (groupContent == null)
                    continue;

                groupContent.SetPropertyBlock(block);
            }
        }

        /// <summary>清除分组内所有运行时渲染器的 MaterialPropertyBlock。</summary>
        public void ClearMatPropBlock()
        {
            if (runtimeGroupData == null)
                return;

            for (int i = 0; i < runtimeGroupDataSize; ++i)
            {
                Renderer groupContent = runtimeGroupData[i];
                if (groupContent == null)
                    continue;

                groupContent.SetPropertyBlock(null);
            }
        }

        // ──────────────────────────────────────────
        // 显隐切换
        // ──────────────────────────────────────────

        /// <summary>
        /// 切换分组内所有渲染器的显隐状态。
        /// </summary>
        /// <param name="rendererEnabled">true 为显示，false 为隐藏。</param>
        /// <param name="forceNullCheck">是否强制 null 检查。</param>
        public void Toggle(bool rendererEnabled, bool forceNullCheck = false)
        {
            bEnable = rendererEnabled;

            if (useListData)
            {
                if (runtimeGroupDataList == null)
                    return;

                for (int i = 0; i < runtimeGroupDataList.Count; ++i)
                    PVSUtil.ToggleRenderer(runtimeGroupDataList[i], rendererEnabled, forceNullCheck);
            }
            else
            {
                if (runtimeGroupData == null)
                    return;

                for (int i = 0; i < runtimeGroupDataSize; ++i)
                    PVSUtil.ToggleRenderer(runtimeGroupData[i], rendererEnabled, forceNullCheck);
            }
        }

        // ──────────────────────────────────────────
        // 查询 / 遍历
        // ──────────────────────────────────────────

        /// <summary>判断序列化渲染器数组中是否包含指定渲染器（编辑器用）。</summary>
        public bool HasRenderer(Renderer render)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == render)
                    return true;
            }

#if UNITY_EDITOR
            if (render != null)
            {
                foreach (var name in m_TerNameList)
                {
                    if (name == render.name)
                        return true;
                }
            }
#endif
            return false;
        }

        /// <summary>遍历序列化渲染器数组并对每个渲染器执行回调。</summary>
        public void ForeachRenderer(System.Action<Renderer> actionForRenderer)
        {
            if (renderers == null)
                return;

            foreach (Renderer r in renderers)
                actionForRenderer.Invoke(r);
        }

        /// <summary>遍历运行时渲染器数组并对每个非空渲染器执行回调。</summary>
        public void ForeachRendererRuntime(System.Action<Renderer> actionForRenderer)
        {
            if (runtimeGroupData == null)
                return;

            foreach (Renderer r in runtimeGroupData)
            {
                if (r == null)
                    continue;

                actionForRenderer.Invoke(r);
            }
        }

        // ──────────────────────────────────────────
        // 编辑器工具
        // ──────────────────────────────────────────

        /// <summary>
        /// 统计分组内所有渲染器的顶点数（仅 Edit 模式，Static Batching 合并后无效）。
        /// </summary>
        /// <returns>是否成功统计（遇到无效 Renderer/MeshFilter 时返回 false）。</returns>
        public bool CollectMeshStats()
        {
#if UNITY_EDITOR
            int totalVertexCount = 0;

            foreach (Renderer rend in renderers)
            {
                if (rend == null)
                {
                    Logger.LogWarning("PVSBakeGroup: 检测到缺失的渲染器引用。");
                    return false;
                }

                MeshFilter mf = rend.GetComponent<MeshFilter>();
                if (mf == null)
                {
                    Logger.LogWarningF("PVSBakeGroup: 渲染器 {0} 缺少 MeshFilter。", rend.name);
                    continue;
                }

                if (mf.sharedMesh == null)
                {
                    Logger.LogWarningF("PVSBakeGroup: 渲染器 {0} 的 Mesh 为空。", rend.name);
                    continue;
                }

                totalVertexCount += mf.sharedMesh.vertexCount;
            }

            vertexCount = totalVertexCount;
            return true;
#else
            return true;
#endif
        }

        /// <summary>
        /// 刷新地形名称列表（仅编辑器模式）。
        /// </summary>
        public void RefreshTerrain()
        {
#if UNITY_EDITOR
            m_TerNameList.Clear();

            if (renderers == null || renderers.Length <= 0)
                return;

            foreach (var render in renderers)
            {
                if (render == null)
                    continue;

                var name = render.name;
                if (!name.Contains(TERRAIN_NAME_CONTAIN))
                    continue;

                m_TerNameList.Add(name);
            }
#endif
        }
    }
}
