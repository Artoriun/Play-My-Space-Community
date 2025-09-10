using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RTOcclusionCulling))]
public class RTOcclusionCullingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.BeginDisabledGroup(true);
        PropertyField("m_Script", new GUIContent("Script"), serializedObject);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(4);

        PropertyField("m_CullEvent", new GUIContent("Cull Event"), serializedObject);
        PropertyField("m_VREnable", new GUIContent("VR Enable"), serializedObject);

        GUILayout.Label("Occluder", EditorStyles.boldLabel);
        EditorGUI.indentLevel = 1;
        PropertyField("m_IgnoreDistance", new GUIContent("Ignore Distance"), serializedObject);
        PropertyField("m_MaxOccluderCount", new GUIContent("Limit Count*"), serializedObject);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.LabelField("* 0 = maximum");
        EditorGUI.EndDisabledGroup();
        EditorGUI.indentLevel = 0;

        GUILayout.Label("Occludee", EditorStyles.boldLabel);
        EditorGUI.indentLevel = 1;
        PropertyField("m_CullTestDelay", new GUIContent("Delay Per CullTest"), serializedObject);
        PropertyField("m_OccludeeRange", new GUIContent("Ignore Distance**"), serializedObject);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.LabelField("** 0 = maximum to far distance of camera");
        EditorGUI.EndDisabledGroup();
        PropertyField("m_LodSwitchDistance", new GUIContent("LOD Switch Distance***"), serializedObject);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.LabelField("*** 0 = to disable lod switching");
        EditorGUI.EndDisabledGroup();

        EditorGUI.indentLevel = 0;

        if (EditorGUI.EndChangeCheck() && Application.isPlaying)
        {
            RTOcclusionCulling _target = (RTOcclusionCulling)target;
            _target.OnChanged();
        }

        GUILayout.Space(8);

        if (GUILayout.Button("Open RTOccluder Workspace", GUILayout.Height(30.0f)))
        {
            RTOccluderWindow window = (RTOccluderWindow)EditorWindow.GetWindow(typeof(RTOccluderWindow), false, "RTOccluder Workspace");
            window.Show();
        }
    }

    static bool PropertyField(string property, GUIContent guicontent, SerializedObject serializedObject)
    {
        SerializedProperty p = serializedObject.FindProperty(property);
        return p == null ? false : PropertyField(p, guicontent, serializedObject);
    }

    static bool PropertyField(SerializedProperty p, GUIContent guicontent, SerializedObject serializedObject)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(p, guicontent);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            return true;
        }
        return false;
    }
}