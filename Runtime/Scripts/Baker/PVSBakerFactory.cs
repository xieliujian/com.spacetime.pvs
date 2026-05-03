namespace ST.PVS
{
    /// <summary>
    /// PVS Baker 工厂类，根据配置创建对应的 Baker 实例。
    /// </summary>
    public class PVSBakerFactory
    {
        /// <summary>
        /// 根据烘焙配置创建合适的 Baker。
        /// </summary>
        /// <param name="bakeSettings">烘焙参数配置。</param>
        /// <returns>可用的 <see cref="PVSBaker"/> 实例。</returns>
        public static PVSBaker CreateBaker(PVSBakeSettings bakeSettings)
        {
            return new PVSBakerUnity(bakeSettings);
        }
    }
}
