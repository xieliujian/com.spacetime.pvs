using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 采样点完成后返回的句柄基类，持有 GPU 读回的可见渲染器索引数组。
    /// 警告：未调用 <see cref="Complete"/> 会导致内存泄漏。
    /// </summary>
    public abstract class PVSBakerHandle
    {
        /// <summary>GPU 读回的可见渲染器索引数组（调用 Complete 后有效）。</summary>
        public ushort[] indices;

        /// <summary>
        /// 完成 GPU 读回并填充 <see cref="indices"/>，同时做重复索引合法性检查。
        /// </summary>
        public void Complete()
        {
            DoComplete();

            for (int i = 1; i < indices.Length; ++i)
            {
                if (indices[i - 1] == indices[i])
                {
                    Debug.LogError($"PVS: GPU 返回了重复索引，数据可能已损坏。值: {indices[i - 1]} 和 {indices[i]}，下标: {i - 1} 和 {i}。建议重新烘焙。");
                }
            }
        }

        /// <summary>子类实现具体的 GPU 读回逻辑并填充 indices。</summary>
        protected abstract void DoComplete();
    }
}
