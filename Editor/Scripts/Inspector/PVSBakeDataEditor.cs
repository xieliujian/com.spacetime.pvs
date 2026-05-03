#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// PVSBakeData 的 Inspector 自定义绘制器，显示烘焙版本、状态和文件大小。
    /// </summary>
    [CustomEditor(typeof(PVSBakeData), true)]
    public class PVSBakeDataEditor : Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            PVSBakeData data = target as PVSBakeData;

            if (!data.bakeCompleted)
                EditorGUILayout.HelpBox("该烘焙数据未正常完成，可能无法正常工作。", MessageType.Error);

            GUILayout.Label($"版本: {data.bakeDataVersion}");
            GUILayout.Label("烘焙信息", EditorStyles.boldLabel);

            data.DrawInspectorGUI();

            GUILayout.Space(10);

            if (PVSEditorUtil.TryGetAssetBakeSize(target as PVSBakeData, out string strBakeSize))
                GUILayout.Label($"文件大小: {strBakeSize}");
        }
    }
}
#endif
