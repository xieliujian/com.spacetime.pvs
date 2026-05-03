using System.Collections.Generic;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 全局临时集合缓存，用于在热路径中复用集合实例，减少 GC 压力。
    /// </summary>
    public static class PVSTemp
    {
        /// <summary>可复用的 ushort 标准列表。</summary>
        public static readonly List<ushort> ListUshort = new List<ushort>(2048);

        /// <summary>可复用的 ushort 高性能列表（主）。</summary>
        public static readonly RapidList<ushort> rapidListUshort = new RapidList<ushort>(2048);

        /// <summary>可复用的 ushort 高性能列表（辅助，用于需要同时操作两个列表的场合）。</summary>
        public static readonly RapidList<ushort> rapidListUshort1 = new RapidList<ushort>(2048);

        /// <summary>可复用的 int 标准列表。</summary>
        public static readonly List<int> ListInt = new List<int>(2048);
    }
}
