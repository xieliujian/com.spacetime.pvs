using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// PVS 颜色表资产，存储预计算的唯一颜色集合，用于烘焙时将渲染器 ID 编码为颜色。
    /// </summary>
    [PreferBinarySerialization]
    public class PVSColorTable : ScriptableObject
    {
        // ──────────────────────────────────────────
        // 单例
        // ──────────────────────────────────────────

        /// <summary>全局颜色表单例（从 Resources 目录加载）。</summary>
        static PVSColorTable m_Instance;

        /// <summary>获取全局颜色表单例实例。</summary>
        public static PVSColorTable Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    var all = Resources.LoadAll<PVSColorTable>(string.Empty);
                    if (all != null && all.Length > 0)
                    {
                        m_Instance = all[0];
                    }
                    else
                    {
                        Debug.LogWarning("[PVS] No PVSColorTable asset found in Resources. " +
                            "Create one via Assets > Create > SpaceTime > PVS > PVSColorTable " +
                            "and place it in a Resources folder, then right-click > Generate.");
                        m_Instance = ScriptableObject.CreateInstance<PVSColorTable>();
                        m_Instance.Generate();
                    }
                }

                return m_Instance;
            }
        }

        // ──────────────────────────────────────────
        // 序列化字段
        // ──────────────────────────────────────────

        /// <summary>预计算的唯一颜色数组。</summary>
        [SerializeField] Color32[] m_Colors;

        /// <summary>颜色数组（只读引用）。</summary>
        public Color32[] Colors => m_Colors;

        // ──────────────────────────────────────────
        // 编辑器工具
        // ──────────────────────────────────────────

        /// <summary>生成随机排列的唯一颜色表（使用 GB 两通道编码，跳过纯黑清屏色）。</summary>
        [ContextMenu("Generate")]
        void Generate()
        {
            var colors = new List<Color32>(PVSConstants.MaxRenderers);

            for (int g = 0; g <= byte.MaxValue; ++g)
            {
                for (int b = 0; b <= byte.MaxValue; ++b)
                {
                    Color32 col = new Color32(0, (byte)g, (byte)b, byte.MaxValue);

                    if (col == PVSConstants.ClearColor)
                        continue;

                    colors.Add(col);
                }
            }

            int count = colors.Count;
            while (count > 1)
            {
                count--;
                int index = UnityEngine.Random.Range(0, count + 1);
                (colors[index], colors[count]) = (colors[count], colors[index]);
            }

            m_Colors = colors.ToArray();
        }
    }
}
