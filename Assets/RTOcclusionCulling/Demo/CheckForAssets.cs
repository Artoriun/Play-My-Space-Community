using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CheckForAssets : MonoBehaviour
{
    public string m_Assets;
    public string m_Url;
    public string m_AssetStoreId;
    public string m_Title;
#if UNITY_EDITOR
    void OnEnable()
    {
        if (CheckAsset() == false)
        {
            EditorApplication.update -= SceneLoaded;
            EditorApplication.update += SceneLoaded;
        }
    }

    bool CheckAsset()
    {
        var guids = AssetDatabase.FindAssets(m_Assets, null);
        return guids.Length > 0 ? true : false;
    }

    void Alert()
    {
        Selection.objects = new Object[] { gameObject };
        //if (EditorUtility.DisplayDialog("This Scene Needs '" + m_Title + "'", "Do you want to open this asset in asset store window", "Yes", "No"))
        //    Open();
    }

    void Open()
    {
        UnityEditorInternal.AssetStore.Open("content/" + m_AssetStoreId);
    }

    static void SceneLoaded()
    {
        CheckForAssets[] o = GameObject.FindObjectsOfType<CheckForAssets>();
        if (o.Length > 0)
            o[0].Alert();
        EditorApplication.update -= SceneLoaded;
    }

    [CustomEditor(typeof(CheckForAssets))]
    public class CheckForAssetsEditor : Editor
    {
        bool m_Installed;
        float m_DisplayTime;

        void OnEnable()
        {
            CheckForAssets _target = (CheckForAssets)target;
            m_Installed = _target.CheckAsset();
            m_DisplayTime = Time.realtimeSinceStartup;
        }

        public void OnSceneGUI()
        {
            if (m_Installed == false && Time.realtimeSinceStartup - m_DisplayTime < 2.0f)
            {
                CheckForAssets _target = (CheckForAssets)target;

                Handles.BeginGUI();
                GUIStyle dropzoneInfoBackgroundStyle = new GUIStyle("NotificationBackground");
                GUIStyle dropzoneInfoLabelStyle = new GUIStyle("NotificationText");

                GUIContent text = new GUIContent("This Scene Needs '" + _target.m_Title + "'");
                var textSize = dropzoneInfoBackgroundStyle.CalcSize(text);
                var textRect = new Rect(new Vector2(Screen.width / 2, Screen.height / 2) - textSize * 0.5f, textSize);

                GUI.Box(textRect, GUIContent.none, dropzoneInfoBackgroundStyle);
                EditorGUI.LabelField(textRect, text, dropzoneInfoLabelStyle);
                Handles.EndGUI();

                SceneView.RepaintAll();
            }
        }

        public override void OnInspectorGUI()
        {
            CheckForAssets _target = (CheckForAssets)target;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"), new GUIContent("Script"));
            EditorGUI.EndDisabledGroup();

            if (m_Installed == false)
            {
                GUILayout.Space(8);
                EditorGUILayout.HelpBox("\"" + _target.m_Title + "\" is not imported", MessageType.Error);

                if (GUILayout.Button("Import \"" + _target.m_Title + "\"", GUILayout.Height(30.0f)))
                {
                    if (_target.m_AssetStoreId != "")
                        _target.Open();
                    else
                        Application.OpenURL(_target.m_Url);
                }
            }
            else
            {
                GUILayout.Space(8);
                EditorGUILayout.HelpBox("\"" + _target.m_Title + "\" is imported", MessageType.Info);
            }
        }
    }
#endif
}