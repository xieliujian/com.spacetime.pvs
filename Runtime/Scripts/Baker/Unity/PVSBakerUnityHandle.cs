using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 基于 Compute Shader Append Buffer 的 GPU 读回句柄，通过 ComputeBuffer 获取可见渲染器索引。
    /// </summary>
    public class PVSBakerUnityHandle : PVSBakerHandle
    {
        /// <summary>存储唯一颜色值的 Append Buffer。</summary>
        public ComputeBuffer appendBuf;

        /// <summary>用于读取 Append Buffer 计数的 IndirectArguments Buffer。</summary>
        public ComputeBuffer countBuf;

        /// <summary>颜色到分组索引的哈希表引用（来自 PVSSceneColor）。</summary>
        public int[] m_Hash;

        /// <summary>可复用的整数输出数组（主线程专用）。</summary>
        static readonly int[] m_Out = new int[PVSConstants.MaxRenderers];

        /// <summary>可复用的计数输出数组（主线程专用）。</summary>
        static readonly int[] m_CounterOutput = new int[1] { 0 };

        // ──────────────────────────────────────────
        // PVSBakerHandle 重写
        // ──────────────────────────────────────────

        /// <inheritdoc/>
        protected override void DoComplete()
        {
            ComputeBuffer.CopyCount(appendBuf, countBuf, 0);
            countBuf.GetData(m_CounterOutput);

            int count = m_CounterOutput[0];
            indices = new ushort[count];

            if (count > 0)
            {
                appendBuf.GetData(m_Out, 0, 0, count);

                for (int i = 0; i < count; ++i)
                {
                    int q = m_Out[i];
                    int b = q / (256 * 256);
                    q -= b * 256 * 256;
                    int g = q / 256;
                    int r = q % 256;

                    int index = (b * 256 * 256) + (g * 256) + r;
                    indices[i] = (ushort)m_Hash[index];
                }

                System.Array.Sort(indices);
            }

            appendBuf.Dispose();
            countBuf.Dispose();
            appendBuf = null;
            countBuf = null;
        }
    }
}
