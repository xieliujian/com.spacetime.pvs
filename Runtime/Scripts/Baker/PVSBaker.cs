using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// PVS Baker 抽象基类，定义采样接口和批次大小，并持有烘焙配置。
    /// </summary>
    public abstract class PVSBaker : System.IDisposable
    {
        /// <summary>每批次处理的最大采样点数量。</summary>
        public virtual int BatchCount => PVSConstants.SampleBatchCount;

        /// <summary>烘焙配置参数。</summary>
        protected readonly PVSBakeSettings m_BakeSettings;

        /// <summary>
        /// 以指定配置构造 Baker 实例。
        /// </summary>
        /// <param name="bakeSettings">烘焙参数配置。</param>
        public PVSBaker(PVSBakeSettings bakeSettings)
        {
            m_BakeSettings = bakeSettings;
        }

        /// <summary>
        /// 对指定世界坐标采样，返回可异步读取的句柄。
        /// </summary>
        /// <param name="pos">采样点世界坐标。</param>
        /// <returns>包含 GPU 读回数据的句柄。</returns>
        public abstract PVSBakerHandle SamplePosition(Vector3 pos);

        /// <summary>释放 Baker 持有的所有资源（相机、RenderTexture、材质等）。</summary>
        public abstract void Dispose();
    }
}
