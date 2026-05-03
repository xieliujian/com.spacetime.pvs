using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 描述一次 PVS 烘焙任务所需的全部信息。
    /// </summary>
    public class BakeInformation
    {
        /// <summary>执行本次烘焙的 Behaviour 实例。</summary>
        public PVSBakingBehaviour BakingBehaviour;

        /// <summary>额外遮挡物渲染器集合（可为 null）。</summary>
        public HashSet<Renderer> AdditionalOccluders;
    }
}
