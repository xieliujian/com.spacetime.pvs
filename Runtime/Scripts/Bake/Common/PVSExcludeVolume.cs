using UnityEngine;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// 排除体积组件：在此体积范围内的采样点将被忽略，不参与 PVS 烘焙。
    /// </summary>
    public class PVSExcludeVolume : MonoBehaviour, CustomHandle.IResizableByHandle
    {
        /// <summary>统一包围盒常量，用于 AABB 包含检测。</summary>
        static readonly Bounds UniformBounds = new Bounds(Vector3.zero, Vector3.one);

        /// <summary>排除体积的尺寸（世界单位）。</summary>
        [SerializeField] public Vector3 volumeSize;

        // ──────────────────────────────────────────
        // 属性
        // ──────────────────────────────────────────

        /// <summary>以 Bounds 形式表示的排除体积（中心为 Transform 位置，尺寸为 volumeSize）。</summary>
        public Bounds volumeExcludeBounds
        {
            get => new Bounds(transform.position, volumeSize);
            set
            {
                transform.position = value.center;
                volumeSize = new Vector3(
                    Mathf.Max(1, value.size.x),
                    Mathf.Max(1, value.size.y),
                    Mathf.Max(1, value.size.z));
            }
        }

        /// <summary>实现 IResizableByHandle，将 HandleSized 映射到 volumeExcludeBounds.size。</summary>
        public Vector3 HandleSized
        {
            get => volumeExcludeBounds.size;
            set => volumeExcludeBounds = new Bounds(transform.position, value);
        }

        // ──────────────────────────────────────────
        // 公共方法
        // ──────────────────────────────────────────

        /// <summary>
        /// 判断给定世界坐标是否处于该排除体积内。
        /// </summary>
        /// <param name="pos">要检测的世界坐标。</param>
        /// <returns>true 表示该点位于排除体积内，应被过滤。</returns>
        public bool IsPositionActive(Vector3 pos)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, volumeExcludeBounds.size).inverse;
            return UniformBounds.Contains(matrix.MultiplyPoint3x4(pos));
        }
    }
}
