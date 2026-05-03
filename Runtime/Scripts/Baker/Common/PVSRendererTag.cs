using UnityEngine;
using UnityEngine.Serialization;

namespace ST.PVS
{
    /// <summary>
    /// 标记组件，用于为渲染器指定 PVS 烘焙的特殊处理方式（排除、双面渲染等）。
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class PVSRendererTag : MonoBehaviour
    {
        /// <summary>是否将该渲染器排除在 PVS 烘焙之外。</summary>
        public bool ExcludeRendererFromBake => m_ExcludeRendererFromBake;

        /// <summary>烘焙时是否对该渲染器进行双面渲染（生成翻转网格）。</summary>
        public bool RenderDoubleSided => m_RenderDoubleSided;

        /// <summary>是否将该渲染器排除在 PVS 烘焙之外（序列化字段）。</summary>
        [SerializeField] bool m_ExcludeRendererFromBake = false;

        /// <summary>烘焙时是否双面渲染（序列化字段）。</summary>
        [SerializeField] bool m_RenderDoubleSided = false;
    }
}
