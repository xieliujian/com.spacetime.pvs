namespace ST.PVS
{
    /// <summary>
    /// 将烘焙位置与对应 Baker 句柄关联的内部数据结构。
    /// </summary>
    class PVSBakeHandle
    {
        /// <summary>格子一维索引。</summary>
        public int Index;

        /// <summary>对应的 Baker 句柄，持有 GPU 读回数据。</summary>
        public PVSBakerHandle Handle;
    }
}
