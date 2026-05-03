namespace ST.PVS
{
    /// <summary>PVS 烘焙完成回调委托。</summary>
    public delegate void PVSVoidFunc();

    /// <summary>PVS 可见性开关回调委托。</summary>
    public delegate void PVSBoolFunc(bool isEnable);

    /// <summary>计算材质 Alpha 值回调委托。</summary>
    public delegate float PVSCalcAlphaFunc(UnityEngine.Material mat);

    /// <summary>
    /// PVS 系统对外桥接类，用于注册烘焙完成、Alpha 计算、距离 LOD 等回调。
    /// </summary>
    public class PVSBridge
    {
        /// <summary>PVS 烘焙完成时触发的回调。</summary>
        static public PVSVoidFunc onPVSBakeFinish;

        /// <summary>计算材质 Alpha 值的回调，由外部实现并注册。</summary>
        static public PVSCalcAlphaFunc onPVSCalcAlpha;

        /// <summary>处理距离 LOD 的回调。</summary>
        static public PVSBoolFunc onPVSProcDistanceLOD;
    }
}
