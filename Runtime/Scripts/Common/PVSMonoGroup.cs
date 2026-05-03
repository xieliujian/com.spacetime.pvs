using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// 持有一组 Renderer 引用的抽象 MonoBehaviour 基类，供 PVS 系统识别和管理渲染器分组。
    /// </summary>
    public abstract class PVSMonoGroup : MonoBehaviour
    {
        /// <summary>该分组包含的所有渲染器列表；子类须提供具体实现。</summary>
        public virtual List<Renderer> Renderers => throw new System.NotImplementedException();
    }
}
