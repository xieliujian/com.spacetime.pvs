#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ST.Core;

namespace ST.PVS
{
    /// <summary>
    /// PVSExcludeVolume 的 Inspector 和 SceneGUI 自定义绘制器，支持拖拽调整排除体积大小。
    /// </summary>
    [CustomEditor(typeof(PVSExcludeVolume))]
    public class PVSExcludeVolumeEditor : Editor
    {
        /// <summary>用于在 Scene 视图绘制体积尺寸句柄的辅助对象。</summary>
        readonly CustomHandle.ActualHandle<PVSExcludeVolume, float> m_Handle =
            new CustomHandle.ActualHandle<PVSExcludeVolume, float>();

        SerializedObject m_So;
        SerializedProperty m_VolumeSize;

        void OnEnable()
        {
            PVSExcludeVolume excludeVolume = target as PVSExcludeVolume;
            m_So = new SerializedObject(excludeVolume);
            m_VolumeSize = m_So.FindProperty("volumeSize");
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            m_So.Update();
            {
                GUILayout.Label("排除体积配置", EditorStyles.boldLabel);

                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.PropertyField(m_VolumeSize, new GUIContent("体积尺寸"));
                }
                GUILayout.EndVertical();
            }
            m_So.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            PVSExcludeVolume excludeVolume = target as PVSExcludeVolume;

            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "FrameSelected")
            {
                Event.current.commandName = "";
                Event.current.Use();
                SceneView.lastActiveSceneView.Frame(excludeVolume.volumeExcludeBounds, false);
                return;
            }

            m_Handle.DrawHandle(excludeVolume);

            Handles.matrix = excludeVolume.transform.localToWorldMatrix;
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = Color.red;
            Handles.DrawWireCube(Vector3.zero, excludeVolume.volumeSize);
        }
    }
}
#endif
