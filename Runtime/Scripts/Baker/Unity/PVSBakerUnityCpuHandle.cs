using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 基于 CPU 像素遍历的 GPU 读回句柄，适用于不支持 Compute Shader 的设备。
    /// </summary>
    public class PVSBakerUnityCpuHandle : PVSBakerHandle
    {
        const int TotalColors = 256 * 256 * 256;

        /// <summary>从 RenderTexture 读取的像素数组。</summary>
        public Color32[] Pixels;

        /// <summary>颜色到分组索引的哈希表引用（来自 PVSSceneColor）。</summary>
        public int[] m_Hash;

        /// <summary>可复用的颜色命中标记数组（主线程专用）。</summary>
        static readonly bool[] hashes = new bool[TotalColors];

        /// <summary>可复用的临时索引列表（主线程专用）。</summary>
        static readonly List<ushort> tmpIndices = new List<ushort>();

        // ──────────────────────────────────────────
        // PVSBakerHandle 重写
        // ──────────────────────────────────────────

        /// <inheritdoc/>
        protected override void DoComplete()
        {
            System.Array.Clear(hashes, 0, TotalColors);
            tmpIndices.Clear();

            int count = Pixels.Length;

            for (int indexPixel = 0; indexPixel < count; ++indexPixel)
            {
                Color32 pixel = Pixels[indexPixel];
                int index = (pixel.b * 256 * 256) + (pixel.g * 256) + pixel.r;

                if (index <= 0 || hashes[index])
                    continue;

                hashes[index] = true;
                tmpIndices.Add((ushort)m_Hash[index]);
            }

            tmpIndices.Sort();

            indices = new ushort[tmpIndices.Count];
            tmpIndices.CopyTo(indices);
        }
    }
}
