#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// PVSSettings 的 Inspector 自定义绘制器。
    /// </summary>
    [CustomEditor(typeof(PVSSettings))]
    public class PVSSettingsEditor : Editor
    {
        static readonly string DisplayVersion = "1.0.0";

        SerializedObject m_So;
        SerializedProperty m_UseUnityForRendering;
        SerializedProperty m_UseUnityForRenderingCpuCompute;
        SerializedProperty m_RenderTransparency;
        SerializedProperty m_BakeCameraResolution;
        SerializedProperty m_BakeAverageSamplingSpeedMs;

        void OnEnable()
        {
            m_So = new SerializedObject(target);
            m_UseUnityForRendering = m_So.FindProperty("useUnityForRendering");
            m_UseUnityForRenderingCpuCompute = m_So.FindProperty("useUnityForRenderingCpuCompute");
            m_RenderTransparency = m_So.FindProperty("renderTransparency");
            m_BakeCameraResolution = m_So.FindProperty("bakeCameraResolution");
            m_BakeAverageSamplingSpeedMs = m_So.FindProperty("bakeAverageSamplingSpeedMs");
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox($"\n*** SpaceTime PVS ***\n\n 版本: {DisplayVersion}\n", MessageType.Info);

            m_So.Update();
            {
                EditorGUILayout.PropertyField(m_UseUnityForRendering, new GUIContent("使用 Unity 内置渲染"));

                if (m_UseUnityForRendering.boolValue)
                {
                    ++EditorGUI.indentLevel;

                    EditorGUILayout.PropertyField(m_UseUnityForRenderingCpuCompute, new GUIContent("CPU 计算模式（无 Compute Shader 支持）"));

                    if (m_UseUnityForRenderingCpuCompute.boolValue)
                    {
                        EditorGUILayout.HelpBox(
                            "CPU 计算模式性能较低，建议降低烘焙相机分辨率（推荐从 32 开始）。",
                            MessageType.Warning);
                    }

                    --EditorGUI.indentLevel;
                }

                m_BakeCameraResolution.intValue = Mathf.Clamp(
                    Mathf.ClosestPowerOfTwo(m_BakeCameraResolution.intValue), 16, 2048);

                EditorGUILayout.PropertyField(m_RenderTransparency, new GUIContent("渲染透明材质"));

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(m_BakeCameraResolution, new GUIContent("烘焙相机分辨率"));

                    if (GUILayout.Button("<", GUILayout.Width(25)))
                    {
                        int prev = 16;
                        int cur = Mathf.ClosestPowerOfTwo(m_BakeCameraResolution.intValue);
                        while (true)
                        {
                            int next = Mathf.NextPowerOfTwo(prev + 1);
                            if (next >= cur) break;
                            prev = next;
                        }
                        m_BakeCameraResolution.intValue = prev;
                    }

                    if (GUILayout.Button(">", GUILayout.Width(25)))
                        m_BakeCameraResolution.intValue = Mathf.NextPowerOfTwo(m_BakeCameraResolution.intValue + 1);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(m_BakeAverageSamplingSpeedMs, new GUIContent("平均采样速度 (ms)"));
            }
            m_So.ApplyModifiedProperties();
        }
    }
}
#endif
