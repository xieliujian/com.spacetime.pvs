#if UNITY_EDITOR
using UnityEditor;

namespace ST.PVS
{
    /// <summary>
    /// PVSColorTable 的 Inspector 自定义绘制器，仅显示提示信息（不可手动编辑）。
    /// </summary>
    [CustomEditor(typeof(PVSColorTable))]
    public class PVSColorTableEditor : Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "此资产存储用于烘焙的预计算唯一颜色表，无需手动编辑。\n如需重新生成，请右键资产选择 Generate。",
                MessageType.Info);
        }
    }
}
#endif
