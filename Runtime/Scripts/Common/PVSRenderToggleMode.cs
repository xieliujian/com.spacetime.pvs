namespace ST.PVS
{
    /// <summary>
    /// 控制渲染器显隐的切换模式枚举。
    /// </summary>
    public enum PVSRenderToggleMode
    {
        /// <summary>直接切换 Renderer 组件的 enabled 属性；开销最大，效率最低。</summary>
        ToggleRendererComponent,

        /// <summary>使用 forceRenderingOff 跳过渲染，效率最高；仅 Unity 2019 及以上可用。</summary>
        ToggleForceRenderingOff
    }
}
