using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// 烘焙场景着色器，将所有参与烘焙的渲染器替换为唯一颜色材质，
    /// 以便 Compute Shader 通过颜色查找对应的分组索引。
    /// </summary>
    public class PVSSceneColor : IDisposable
    {
        // ──────────────────────────────────────────
        // 字段
        // ──────────────────────────────────────────

        /// <summary>Shader 颜色属性 ID。</summary>
        readonly int m_PropIdColor = Shader.PropertyToID("_Color");

        /// <summary>Shader 剔除属性 ID。</summary>
        readonly int m_PropIdCull = Shader.PropertyToID("_Cull");

        /// <summary>所有烘焙分组数组。</summary>
        PVSBakeGroup[] m_AllGroups;

        /// <summary>额外遮挡物渲染器集合。</summary>
        HashSet<Renderer> m_AdditionalOccluders;

        /// <summary>每个分组对应的颜色（与分组索引一一对应）。</summary>
        Color32[] m_RendererColors;

        /// <summary>颜色到分组索引的哈希表（key = b*65536+g*256+r）。</summary>
        int[] m_Hash;

        /// <summary>双面剔除（CullOff）材质实例。</summary>
        readonly Material m_RendererCullOffMat;

        /// <summary>背面剔除（CullBack）材质实例。</summary>
        readonly Material m_RendererCullBackMat;

        /// <summary>需要在 Dispose 时销毁的对象列表。</summary>
        List<Object> m_DisposeList = new List<Object>();

        /// <summary>是否检查采样点位置偏移掩码（决定是否添加 MeshCollider）。</summary>
        bool m_IsCheckSamplePosOffsetMask;

        // ──────────────────────────────────────────
        // 属性
        // ──────────────────────────────────────────

        /// <summary>颜色到分组索引的哈希表。</summary>
        public int[] Hash => m_Hash;

        /// <summary>每个分组对应的颜色数组。</summary>
        public Color32[] Colors => m_RendererColors;

        // ──────────────────────────────────────────
        // 构造
        // ──────────────────────────────────────────

        /// <summary>
        /// 初始化场景着色：加载材质、生成颜色表、可选地配置所有渲染器。
        /// </summary>
        public PVSSceneColor(PVSBakeGroup[] groups, HashSet<Renderer> additionalOccluders,
            bool setupRenderers, bool isCheckSamplePosOffsetMask)
        {
            m_AllGroups = groups;
            m_AdditionalOccluders = additionalOccluders;
            m_IsCheckSamplePosOffsetMask = isCheckSamplePosOffsetMask;

            m_Hash = new int[256 * 256 * 256];
            m_RendererColors = new Color32[m_AllGroups.Length];

            GenerateColors();

#if UNITY_EDITOR
            m_RendererCullOffMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(PVSConstants.s_MatCullOffPath);
            m_RendererCullBackMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(PVSConstants.s_MatCullBackPath);
#endif

            if (m_RendererCullOffMat == null || m_RendererCullBackMat == null)
            {
                Logger.LogError("PVSSceneColor: 缺少烘焙材质，请检查材质路径配置。");
                return;
            }

            m_RendererCullOffMat = Object.Instantiate(m_RendererCullOffMat);
            m_RendererCullBackMat = Object.Instantiate(m_RendererCullBackMat);
            m_DisposeList.Add(m_RendererCullOffMat);
            m_DisposeList.Add(m_RendererCullBackMat);

#if UNITY_EDITOR_WIN
            m_RendererCullOffMat.enableInstancing = SystemInfo.supportsInstancing;
            m_RendererCullBackMat.enableInstancing = SystemInfo.supportsInstancing;
#else
            m_RendererCullOffMat.enableInstancing = false;
            m_RendererCullBackMat.enableInstancing = false;
#endif

            var flippedMeshes = new Dictionary<Mesh, Mesh>();

            for (int groupIndex = 0; groupIndex < m_AllGroups.Length; ++groupIndex)
            {
                foreach (Renderer renderer in m_AllGroups[groupIndex].renderers)
                {
                    PVSRendererTag tag = renderer.GetComponent<PVSRendererTag>();
                    bool isWater = PVSUtil.IsStylizedWater(renderer);

                    if (!isWater)
                    {
                        if (tag == null || !tag.RenderDoubleSided)
                            continue;
                    }

                    GameObject go = new GameObject(renderer.name + "_Flipped");
                    go.transform.parent = renderer.transform;
                    go.transform.localScale = Vector3.one;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;

                    MeshFilter mf = renderer.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null)
                        continue;

                    if (!flippedMeshes.TryGetValue(mf.sharedMesh, out Mesh mesh))
                    {
                        mesh = Object.Instantiate(mf.sharedMesh);
                        mesh.triangles = mesh.triangles.Reverse().ToArray();
                        flippedMeshes.Add(mf.sharedMesh, mesh);
                    }

                    MeshRenderer newR = go.AddComponent<MeshRenderer>();
                    newR.sharedMaterial = renderer.sharedMaterial;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;

                    m_DisposeList.Add(newR);
                    m_DisposeList.Add(mesh);

                    if (Application.isPlaying)
                        PrepareRenderer(newR, m_RendererColors[groupIndex], new MaterialPropertyBlock());
                    else
                        m_AllGroups[groupIndex].renderers = m_AllGroups[groupIndex].renderers.Append(newR).ToArray();

                    break;
                }
            }

            if (setupRenderers)
                SetupRenderers();
        }

        // ──────────────────────────────────────────
        // IDisposable
        // ──────────────────────────────────────────

        /// <summary>销毁所有临时创建的对象并清理引用。</summary>
        public void Dispose()
        {
            foreach (var obj in m_DisposeList)
                Object.DestroyImmediate(obj);

            m_DisposeList.Clear();
            m_DisposeList = null;
            m_Hash = null;
        }

        // ──────────────────────────────────────────
        // 私有方法
        // ──────────────────────────────────────────

        /// <summary>从颜色表为每个分组生成唯一颜色，并构建颜色到索引的哈希表。</summary>
        void GenerateColors()
        {
            Color32[] colorTableColors = PVSColorTable.Instance.Colors;

            for (int i = 0; i < m_RendererColors.Length; ++i)
            {
                byte r = colorTableColors[i].r;
                byte g = colorTableColors[i].g;
                byte b = colorTableColors[i].b;

#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    unchecked { r = (byte)(g + b); }
                }
#endif
                int index = (b * 256 * 256) + (g * 256) + r;
                m_RendererColors[i] = new Color32(r, g, b, byte.MaxValue);
                m_Hash[index] = i;
            }
        }

        /// <summary>为所有分组渲染器和额外遮挡物设置烘焙材质和图层。</summary>
        void SetupRenderers()
        {
            var propBlock = new MaterialPropertyBlock();
            var allRenderers = new HashSet<Renderer>();

            for (int groupIndex = 0; groupIndex < m_AllGroups.Length; ++groupIndex)
            {
                m_AllGroups[groupIndex].ForeachRenderer((renderer) =>
                {
                    allRenderers.Add(renderer);
                    PrepareRenderer(renderer, m_RendererColors[groupIndex], propBlock);
                });
            }

            if (m_AdditionalOccluders == null)
                return;

            foreach (Renderer renderer in m_AdditionalOccluders)
            {
                if (allRenderers.Contains(renderer))
                    continue;

                PrepareRenderer(renderer, PVSConstants.ClearColor, propBlock);
            }
        }

        /// <summary>为单个渲染器设置烘焙材质、颜色属性块和图层。</summary>
        void PrepareRenderer(Renderer renderer, Color col, MaterialPropertyBlock propBlock)
        {
            Material[] allMaterials = renderer.sharedMaterials;

            renderer.enabled = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.lightmapIndex = -1;
            renderer.realtimeLightmapIndex = -1;

            bool isCullBack = renderer.tag == PVSConstants.s_Tag_ForceCullBack;
            var realMat = isCullBack ? m_RendererCullBackMat : m_RendererCullOffMat;

            if (!Application.isPlaying)
            {
                renderer.gameObject.layer = PVSConstants.CamBakeLayer;
                if (renderer.gameObject.name == "_Cube_Grass")
                    renderer.gameObject.layer = PVSConstants.CamBakeDisLayer;
            }

            AddRenderCollider(renderer);

            for (int materialIndex = 0; materialIndex < allMaterials.Length; ++materialIndex)
            {
                col.a = PVSBridge.onPVSCalcAlpha(allMaterials[materialIndex]);
                allMaterials[materialIndex] = realMat;

                propBlock.SetColor(m_PropIdColor, col);
                renderer.SetPropertyBlock(propBlock, materialIndex);
            }

            renderer.sharedMaterials = allMaterials;
        }

        /// <summary>为渲染器添加 MeshCollider（仅在 isCheckSamplePosOffsetMask 时执行）。</summary>
        void AddRenderCollider(Renderer render)
        {
            if (!m_IsCheckSamplePosOffsetMask)
                return;

            var collider = render.GetComponent<Collider>();
            if (collider != null)
                return;

            collider = render.gameObject.AddComponent<MeshCollider>();
            m_DisposeList.Add(collider);
        }
    }
}
