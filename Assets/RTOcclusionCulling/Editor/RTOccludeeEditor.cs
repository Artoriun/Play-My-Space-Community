#define CUSTOM_INSPECTOR
#if CUSTOM_INSPECTOR
#define FASTARRAYFIELD
#endif
#define LOD_ENABLE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

[CustomEditor(typeof(RTOccludee))]
[CanEditMultipleObjects]
public class RTOccludeeEditor : Editor
{
    private void OnSceneGUI()
    {
        RTOccludee _target = (RTOccludee)target;
        if (!Application.isPlaying && _target != null)
        {
            if (_target.m_DeactiveType == RTOccludee.DeactiveType.GameObjectDeactive)
            {
                Handles.color = Color.green;
                Slider(new Vector3(0.0f, -1.0f, -1.0f), Vector3.forward, true); // -z
                Slider(new Vector3(0.0f, -1.0f, 1.0f), Vector3.forward, false); // +z
                Slider(new Vector3(-1.0f, -1.0f, 0.0f), Vector3.right, true); // -x
                Slider(new Vector3(1.0f, -1.0f, 0.0f), Vector3.right, false); // -x
                Slider(new Vector3(0.0f, -1.0f, 0.0f), Vector3.up, true); // -y
                Slider(new Vector3(0.0f, 1.0f, 0.0f), Vector3.up, false); // -y

                DrawBox(_target.transform, _target.m_CustomBounds.center, _target.m_CustomBounds.size);
            }

            if (_target.m_DeactiveType != RTOccludee.DeactiveType.GameObjectDeactive && _target.m_SpatialDivisionDepth > 0 && m_SpatialNode != null && m_SpatialPreviewDepth > 0)
            {
                DrawSpatialBound(m_SpatialNode, m_SpatialPreviewDepth);
            }
        }
    }

    void DrawSpatialBound(RT.Occludee.KdNode node, int depth)
    {
        if (node == null)
            return;
        if (depth == 0)
        {
            Vector3 min = new Vector3(node.boundselement[0], node.boundselement[2], node.boundselement[4]);
            Vector3 max = new Vector3(node.boundselement[1], node.boundselement[3], node.boundselement[5]);
            DrawBox(null, (min+max)*0.5f, max-min);
        }
        else
        {
            DrawSpatialBound(node.left, depth - 1);
            DrawSpatialBound(node.right, depth - 1);
        }
    }

    static UnityEngine.Object _lastobject;
    static int _lastpredepth;

    private void OnEnable()
    {
        if (_lastobject == target)
            m_SpatialPreviewDepth = _lastpredepth;

        ToolAsset.Init();
    }

    private void OnDisable()
    {
        _lastobject = target;
        _lastpredepth = m_SpatialPreviewDepth;
    }

    #region ToolAsset
    static class ToolAsset
    {
        public static Material material;
        public static Mesh box;

        static public void Init()
        {
            if (material == null)
            {
                material = new Material(Shader.Find("Hidden/Internal-Occlusion"));
                material.SetColor("_Color", new Vector4(0, 1, 0, 0.25f));
                material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }

            if (box == null)
            {
                Vector3[] vtx = new Vector3[8];
                for (int i = 0; i < 8; i++)
                    vtx[i] = new Vector3((i & 1) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1);
                box = new Mesh();
                box.hideFlags = HideFlags.HideAndDontSave;
                box.vertices = vtx;
                box.triangles = new int[] { 0, 1, 3, 0, 3, 2, 4, 7, 5, 4, 6, 7, 0, 5, 1, 0, 4, 5, 1, 7, 3, 1, 5, 7, 3, 6, 2, 3, 7, 6, 2, 4, 0, 2, 6, 4 };
                box.RecalculateBounds();
            }
        }
    }
    #endregion

    #region Mesh
    static int[] _edge = new int[] { 0, 1, 1, 3, 3, 2, 2, 0, 0, 4, 1, 5, 2, 6, 3, 7, 4, 5, 5, 7, 7, 6, 6, 4 };

    void DrawBox(Transform transform, Vector3 center, Vector3 size)
    {
        Vector3[] vtx = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 v = center + new Vector3((i & 1) == 0 ? -size.x : size.x, (i & 4) == 0 ? -size.y : size.y, (i & 2) == 0 ? -size.z : size.z) * 0.5f;
            vtx[i] = transform != null ? transform.TransformPoint(v) : v;
        }

        Handles.color = Color.green;

        for (int i = 0; i < _edge.Length; i += 2)
            Handles.DrawAAPolyLine(vtx[_edge[i]], vtx[_edge[i + 1]]);

        ToolAsset.material.SetPass(0);
        Graphics.DrawMeshNow(ToolAsset.box, (transform != null ? transform.localToWorldMatrix : Matrix4x4.identity) * Matrix4x4.TRS(center, Quaternion.identity, size*0.5f));
    }
    #endregion

    void Slider(Vector3 pos, Vector3 dir, bool negative)
    {
        SceneView view = SceneView.currentDrawingSceneView;
        RTOccludee _target = (RTOccludee)target;

        Vector3 center = _target.m_CustomBounds.center;
        Vector3 size = _target.m_CustomBounds.size;

        Vector3 p1 = _target.transform.TransformPoint(center + new Vector3(size.x * pos.x * 0.5f, size.y * pos.y * 0.5f, size.z * pos.z * 0.5f));

        EditorGUI.BeginChangeCheck();
#if UNITY_5_3_OR_NEWER
        Vector3 p2 = Handles.Slider(p1, _target.transform.TransformVector(negative ? -dir : dir), WorldSize(50, view.camera, p1), Handles.ArrowHandleCap, 0.0f);
#else
        Vector3 p2 = Handles.Slider(p1, _target.transform.TransformVector(negative ? -dir : dir), WorldSize(50, view.camera, p1), Handles.ArrowCap, 0.0f);
#endif
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_target, "Move");

            Vector3 dv = _target.transform.InverseTransformVector(p2 - p1);
            size += negative ? -dv : dv;
            center -= negative ? -dv * 0.5f : -dv * 0.5f;
            _target.m_CustomBounds = new Bounds(center, new Vector3(Mathf.Max(0, size.x), Mathf.Max(0, size.y), Mathf.Max(0, size.z)));
        }
    }

    static float WorldSize(float screensize, Camera camera, Vector3 p)
    {
        return (!camera.orthographic ? Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * camera.transform.InverseTransformPoint(p).z * 2.0f : camera.orthographicSize * 2.0f) * screensize / camera.pixelHeight;
    }

#if CUSTOM_INSPECTOR
#if FASTARRAYFIELD
    static float m_ElementHeight = 18;
    bool m_InternalInspectorPosition = false;
    float m_Offset = 0;
#endif

    void ObjectArrayField(string name, GUIContent guicontent, SerializedObject serializedObject, System.Type type)
    {
#if FASTARRAYFIELD
        string focus = GUI.GetNameOfFocusedControl();
#endif
        GUILayout.BeginVertical();

        Event guiEvent = Event.current;

        SerializedProperty prop = serializedObject.FindProperty(name);
        bool dragadd = PropertyField(prop, guicontent, serializedObject) && guiEvent.type == EventType.DragPerform ? true : false;
        Rect rect = GUILayoutUtility.GetLastRect();

        if (prop.isExpanded)
        {
            EditorGUI.indentLevel = 1;

            PropertyField(prop.FindPropertyRelative("Array.size"), new GUIContent("Size"), serializedObject);
            int arraySize = prop.arraySize - (dragadd ? 1 : 0);
#if FASTARRAYFIELD
            if (guiEvent.type == EventType.Repaint)
                m_ElementHeight = GUILayoutUtility.GetLastRect().yMin - rect.yMin;

            if (m_InternalInspectorPosition)
            {
                int begin = Mathf.Clamp((int)(-m_Offset / m_ElementHeight) - 5, 0, arraySize);
                int end = Mathf.Clamp((int)((-m_Offset + Screen.height) / m_ElementHeight) + 5, begin, arraySize);

                if (begin > 0)
                    EditorGUILayout.LabelField("", GUILayout.Height(begin * m_ElementHeight));

                SerializedProperty p = null;

                for (int i = begin; i < end; i++)
                {
                    if (p == null)
                        p = prop.GetArrayElementAtIndex(i);

                    GUI.SetNextControlName("Element" + i);
                    PropertyField(p, new GUIContent("Element" + i), serializedObject);
                    p.Next(false);
                }
                if (end < arraySize - 1)
                    EditorGUILayout.LabelField("", GUILayout.Height(((arraySize - 1) - end) * m_ElementHeight));

                if (end < arraySize)
                {
                    GUI.SetNextControlName("Element" + (arraySize - 1));
                    PropertyField(prop.GetArrayElementAtIndex(arraySize - 1), new GUIContent("Element" + (arraySize - 1)), serializedObject);
                }
            }
            else
#endif
            {
                SerializedProperty p = arraySize > 0 ? prop.GetArrayElementAtIndex(0) : null;
                for (int i = 0; i < arraySize; i++)
                {
                    PropertyField(p, new GUIContent("Element" + i), serializedObject);
                    p.Next(false);
                }
            }
        }
#if FASTARRAYFIELD
        if (m_InternalInspectorPosition && focus != GUI.GetNameOfFocusedControl() && guiEvent.type == EventType.Layout)
            GUI.FocusControl(focus);
#endif
        GUILayout.EndVertical();
    }

    static bool PropertyField(string property, GUIContent guicontent, SerializedObject serializedObject)
    {
        return PropertyField(serializedObject.FindProperty(property), guicontent, serializedObject);
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

    float FloatSlider(GUIContent guicontent, float value, float min, float max)
    {
        EditorGUI.BeginChangeCheck();
        value = EditorGUILayout.Slider(guicontent, value, min, max);
        if (EditorGUI.EndChangeCheck())
            Undo.RecordObject(target, "UpdateValue");
        return value;
    }                      


    Enum EnumPopup(string label, Enum value)
    {
        EditorGUI.BeginChangeCheck();
        Enum ret = EditorGUILayout.EnumPopup(label, value);
        if (EditorGUI.EndChangeCheck())
            Undo.RecordObject(target, "UpdateValue");
        return ret;
    }

    EditorWindow m_InspectorWindow;
    Rect m_Rect;

    enum DeactiveVolumeType
    {
        Static = RTOccludee.VolumeType.Static,
        NonStatic = RTOccludee.VolumeType.NonStatic,
    }
#endif

    static Mesh GetMesh(Renderer renderer)
    {
        MeshFilter m = renderer.GetComponent<MeshFilter>();
        if (m != null)
            return m.sharedMesh;
        SkinnedMeshRenderer skinnedmesh = renderer.GetComponent<SkinnedMeshRenderer>();
        if (skinnedmesh != null)
            return skinnedmesh.sharedMesh;
        return null;
    }

    int m_SpatialPreviewDepth = 0;
    RT.Occludee.KdNode m_SpatialNode = null;

    public override void OnInspectorGUI()
    {
#if CUSTOM_INSPECTOR
        if (targets.Length == 1)
        {
            RTOccludee _target = (RTOccludee)target;
            Event guiEvent = Event.current;

            EditorGUI.BeginDisabledGroup(true);
            PropertyField("m_Script", new GUIContent("Script"), serializedObject);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(Application.isPlaying ? true : false);

            if (PropertyField("m_DeactiveType", new GUIContent("DeactiveType"), serializedObject))
            {
                if (_target.m_DeactiveType == RTOccludee.DeactiveType.GameObjectDeactive && _target.m_VolumeType == RTOccludee.VolumeType.StaticSpatialPartitioning)
                {
                    Undo.RecordObject(_target, "UpdateVolumeType");
                    _target.m_VolumeType = RTOccludee.VolumeType.Static;
                }
                if (_target.m_DeactiveType == RTOccludee.DeactiveType.GameObjectDeactive && _target.m_VolumeType == RTOccludee.VolumeType.UpdateBoundsAlways)
                {
                    Undo.RecordObject(_target, "UpdateVolumeType");
                    _target.m_VolumeType = RTOccludee.VolumeType.NonStatic;
                }
            }

            if (_target.m_DeactiveType == RTOccludee.DeactiveType.GameObjectDeactive)
            {
                _target.m_VolumeType = (RTOccludee.VolumeType)EnumPopup("VolumeType", (DeactiveVolumeType)_target.m_VolumeType);
            }
            else
            {
                _target.m_VolumeType = (RTOccludee.VolumeType)EnumPopup("VolumeType", _target.m_VolumeType);
            }

            if (_target.m_DeactiveType != RTOccludee.DeactiveType.GameObjectDeactive)
            {
                GUILayout.Space(4);
                EditorGUI.indentLevel = 1;

                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginDisabledGroup(_target.m_VolumeType != RTOccludee.VolumeType.StaticSpatialPartitioning);
                PropertyField("m_SpatialDivisionDepth", new GUIContent("SpatialDivisionDepth"), serializedObject);
                EditorGUI.EndDisabledGroup();

                if (_target.m_VolumeType == RTOccludee.VolumeType.StaticSpatialPartitioning && _target.m_SpatialDivisionDepth > 0)
                {
                    m_SpatialPreviewDepth = EditorGUILayout.IntSlider("SpatialPreview", m_SpatialPreviewDepth, 0, _target.m_SpatialDivisionDepth);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (m_SpatialNode == null)
                            m_SpatialNode = RT.Occludee.SpatialDivision(_target.m_MeshRenderer, _target.m_SpatialDivisionDepth);
                        SceneView.RepaintAll();
                    }
                }
#if LOD_ENABLE
                EditorGUI.BeginDisabledGroup(_target.m_BakeLod != null && _target.m_BakeLod.Length > 0 ? true : false);
                bool lod = EditorGUILayout.Toggle("GenerateLOD", _target.m_BakeLod != null && _target.m_BakeLod.Length > 0 ? true : false);
                if (lod == true && (_target.m_BakeLod == null || _target.m_BakeLod.Length == 0))
                {
                    Undo.RecordObject(_target, "LoadEnable");
                    _target.m_BakeLod = new RTOccludee.Lod[] { new RTOccludee.Lod() };
                    serializedObject.Update();
                }
                EditorGUI.EndDisabledGroup();

                if (lod)
                {
                    PropertyField("m_LodSwitchDistance", new GUIContent("LOD Switch Distance*"), serializedObject);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField("* 0 = to apply the value from the camera");
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("UnityMeshSimplifier Setting", EditorStyles.boldLabel);
                    PropertyField("m_BakeLod.Array.data[0].preserveBorderEdges", new GUIContent("PreserveBorderEdges"), serializedObject);
                    PropertyField("m_BakeLod.Array.data[0].preserveUVSeamEdges", new GUIContent("PreserveUVSeamEdges"), serializedObject);
                    PropertyField("m_BakeLod.Array.data[0].preserveUVFoldoverEdges", new GUIContent("PreserveUVFoldoverEdges"), serializedObject);
                    PropertyField("m_BakeLod.Array.data[0].enableSmartLink", new GUIContent("EnableSmartLink"), serializedObject);
                    PropertyField("m_BakeLod.Array.data[0].vertexLinkDistanceSqr", new GUIContent("VertexLinkDistanceSqr"), serializedObject);
                    PropertyField("m_BakeLod.Array.data[0].maxIterationCount", new GUIContent("MaxIterationCount"), serializedObject);
                    PropertyField("m_BakeLod.Array.data[0].agressiveness", new GUIContent("Agressiveness"), serializedObject);
                    _target.m_BakeLod[0].quality = FloatSlider(new GUIContent("Quality"), _target.m_BakeLod[0].quality, 0, 1.0f);

                    if (_target.m_BakeLod[0].lodasset != null)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField("SavedFile", _target.m_BakeLod[0].lodasset, typeof(UnityEngine.Object), false);
                        EditorGUI.EndDisabledGroup();
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(_target.m_VolumeType == RTOccludee.VolumeType.StaticSpatialPartitioning ? "Bake LOD (depth " + m_SpatialPreviewDepth + ")" : "Bake LOD", GUILayout.Height(30.0f), GUILayout.MinWidth(80), GUILayout.MaxWidth(Screen.width)))
                    {
                        int i = 0;
                        for (; i < _target.m_MeshRenderer.Length; i++)
                        {
                            Mesh m = GetMesh(_target.m_MeshRenderer[i]);
                            if (m == null)
                            {
                                EditorUtility.DisplayDialog("Error", "Null-mesh is included", "OK");
                                break;
                            }
                        }

                        if (i == _target.m_MeshRenderer.Length)
                        {
                            string fname = "";
                            if (_target.m_BakeLod[0].lodasset != null)
                                fname = AssetDatabase.GetAssetPath(_target.m_BakeLod[0].lodasset);
                            if (fname == "")
                                fname = EditorUtility.SaveFilePanelInProject("Save LOD Mesh", "MeshSimplifier_" + _target.name + ".asset", "asset", "Please enter a file name to save the LOD-mesh");
                            if (fname != "")
                            {
                                Undo.RecordObject(target, "Reset LOD");
                                if (_target.m_VolumeType == RTOccludee.VolumeType.StaticSpatialPartitioning)
                                    _target.m_MeshRenderer = BakeLodMeshWithSpatialDivision(_target.m_MeshRenderer, _target.m_SpatialDivisionDepth, _target.m_BakeLod[0], m_SpatialPreviewDepth, fname);
                                else
                                    BakeLodMesh(_target.transform, _target.m_MeshRenderer, _target.m_BakeLod[0], fname);
                            }
                        }
                    }
                    if (GUILayout.Button("Clear LOD", GUILayout.Height(30.0f), GUILayout.MinWidth(80), GUILayout.MaxWidth(Screen.width)))
                    {
                        Undo.RecordObject(target, "Reset LOD");

                        if (_target.m_BakeLod[0].lodasset != null)
                        {
                            string fname = AssetDatabase.GetAssetPath(_target.m_BakeLod[0].lodasset);
                            if (EditorUtility.DisplayDialog("Delete the LOD-mesh?", fname, "Delete", "Do Not Delte"))
                            {
                                string dataPath = Application.dataPath;
                                File.Delete(dataPath.Substring(0, dataPath.Length - 6) + fname);
                                UnityEditor.AssetDatabase.Refresh();
                            }
                        }

                        _target.m_BakeLod = null;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
#endif
                EditorGUI.indentLevel = 0;

                EditorGUI.BeginDisabledGroup(_target.m_VolumeType == RTOccludee.VolumeType.UpdateBoundsAlways);
                PropertyField("m_StaticBatching", new GUIContent("StaticBatching"), serializedObject);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                PropertyField("m_CustomBounds", new GUIContent("CustomBounds"), serializedObject);
            }

#if FASTARRAYFIELD
            if (guiEvent.type == EventType.Layout)
            {
                if (m_InspectorWindow == null)
                {
                    EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>() as EditorWindow[];
                    foreach (EditorWindow editor in windows)
                    {
                        if (editor.titleContent.text == "Inspector")
                        {
                            m_InspectorWindow = editor;
                            Type type = m_InspectorWindow.GetType();
                            m_InternalInspectorPosition = type.GetField("m_ScrollPosition") != null ? true : false;
                            break;
                        }
                    }
                }

                if (m_InternalInspectorPosition)
                {
                    Type type = m_InspectorWindow.GetType();
                    FieldInfo field = type.GetField("m_ScrollPosition");
                    Vector2 scrollposition = (Vector2)field.GetValue(m_InspectorWindow);
                    m_Offset = m_Rect.yMin - scrollposition.y;
                }
            }
#endif
            if (_target.m_DeactiveType != RTOccludee.DeactiveType.GameObjectDeactive)
                ObjectArrayField("m_MeshRenderer", new GUIContent("MeshRenderer"), serializedObject, typeof(Renderer));
            else
                ObjectArrayField("m_GameObjects", new GUIContent("GameObjects"), serializedObject, typeof(GameObject));

            EditorGUI.EndDisabledGroup();

            if (guiEvent.type == EventType.Repaint)
                m_Rect = GUILayoutUtility.GetLastRect();
        }
        else
#endif
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying ? true : false);
            base.OnInspectorGUI();
            EditorGUI.EndDisabledGroup();
        }

        GUILayout.Space(8);

        EditorGUI.BeginDisabledGroup(Application.isPlaying ? true : false);

        if (GUILayout.Button("Gather Child-MeshRenderer", GUILayout.Height(30.0f)))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                RTOccludee _target = (RTOccludee)targets[i];

                Undo.RecordObject(_target, "Gather");
                MeshRenderer[] meshrenderer = _target.gameObject.GetComponentsInChildren<MeshRenderer>();
                SkinnedMeshRenderer[] skinmeshrenderer = _target.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                List<Renderer> list = new List<Renderer>();
                List<Material> material = new List<Material>(); // sortkey

                for (int j = 0; j < meshrenderer.Length; j++)
                {
                    list.Add(meshrenderer[j]);
                    material.Add(meshrenderer[j].sharedMaterial);
                }

                for (int j = 0; j < skinmeshrenderer.Length; j++)
                {
                    list.Add(skinmeshrenderer[j]);
                    material.Add(skinmeshrenderer[j].sharedMaterial);
                }

                list.Sort(delegate (Renderer m1, Renderer m2)
                {
                    int d = material.IndexOf(m1.sharedMaterial) - material.IndexOf(m2.sharedMaterial);
                    return d == 0 ? (int)(m1.transform.position.y - m2.transform.position.y) : d;
                });
                _target.m_MeshRenderer = list.ToArray();
            }
        }

        GUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Exclude isPartOfStaticBatch,null", GUILayout.Height(30.0f), GUILayout.MinWidth(80), GUILayout.MaxWidth(Screen.width)))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                RTOccludee _target = (RTOccludee)targets[i];
                Renderer[] renderers = RT.Occludee.ExcludeStaticMesh(_target.m_MeshRenderer);
                if (renderers.Length != _target.m_MeshRenderer.Length)
                {
                    Undo.RecordObject(_target, "Exclude isPartOfStaticBatch");
                    Debug.Log("Exclude " + (_target.m_MeshRenderer.Length - renderers.Length) + " renderers");
                    _target.m_MeshRenderer = renderers;
                }
            }
        }
        if (GUILayout.Button("Select isPartOfStaticBatch", GUILayout.Height(30.0f), GUILayout.MinWidth(80), GUILayout.MaxWidth(Screen.width)))
        {
            List<UnityEngine.Object> selobjs = new List<UnityEngine.Object>();
            for (int i = 0; i < targets.Length; i++)
            {
                RTOccludee _target = (RTOccludee)targets[i];
                for (int j = 0; j < _target.m_MeshRenderer.Length; j++)
                {
                    if (_target.m_MeshRenderer[j].isPartOfStaticBatch)
                        selobjs.Add(_target.m_MeshRenderer[j].gameObject);
                }
            }
            if (selobjs.Count > 0)
                Selection.objects = selobjs.ToArray();
        }

        if (GUILayout.Button("Reset", GUILayout.Height(30.0f), GUILayout.MinWidth(80)))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                RTOccludee _target = (RTOccludee)targets[i];
                Undo.RecordObject(_target, "Reset");
                _target.m_MeshRenderer = new MeshRenderer[0];
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
        GUILayout.Space(8);

        EditorGUI.BeginDisabledGroup(targets.Length == 1 ? false : true);
        if (GUILayout.Button("Select MeshRenderers", GUILayout.Height(30.0f)))
        {
            List<UnityEngine.Object> selobjs = new List<UnityEngine.Object>();
            for (int i = 0; i < targets.Length; i++)
            {
                RTOccludee _target = (RTOccludee)targets[i];
                for (int j = 0; j < _target.m_MeshRenderer.Length; j++)
                    selobjs.Add(_target.m_MeshRenderer[j].gameObject);
            }
            if (selobjs.Count > 0)
                Selection.objects = selobjs.ToArray();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(8);

        if (GUILayout.Button("Open RTOccluder Workspace", GUILayout.Height(30.0f)))
        {
            RTOccluderWindow window = (RTOccluderWindow)EditorWindow.GetWindow(typeof(RTOccluderWindow), false, "RTOccluder Workspace");
            window.Show();
        }
    }
#if LOD_ENABLE
    static public void BakeLodMesh(Transform parent, Renderer[] renderers, RTOccludee.Lod lod, string meshfilename)
    {
        EditorUtility.DisplayProgressBar("Generate LOD", "", 0);

        lod.nodebyes = null;
        lod.nodelength = null;

        Renderer[] finalmesh = SimplifyMesh(parent, renderers, lod, 0.0f, 1.0f);

        List<RTOccludee.RenderMesh> simplifiedMesh = new List<RTOccludee.RenderMesh>();
        for (int i = 0; i < finalmesh.Length; i++)
        {
            Mesh m = GetMesh(finalmesh[i]);
            simplifiedMesh.Add(new RTOccludee.RenderMesh() { mesh = m, materials = finalmesh[i].sharedMaterials, offset = 0, length = 1 });
        }

        for (int i = 0; i < finalmesh.Length; i++)
            GameObject.DestroyImmediate(finalmesh[i].gameObject);

        Debug.Log("Save:" + meshfilename);
        EditorUtility.ClearProgressBar();

        Mesh prefab = new Mesh();
        AssetDatabase.CreateAsset(prefab, meshfilename);

        for (int i = 0; i < simplifiedMesh.Count; i++)
            AssetDatabase.AddObjectToAsset(simplifiedMesh[i].mesh, prefab);

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();

        lod.lodmesh = simplifiedMesh.ToArray();
        lod.lodasset = prefab;
    }

    static public Renderer[] BakeLodMeshWithSpatialDivision(Renderer[] renderers, int spatialdivision, RTOccludee.Lod lod, int bakedepth, string meshfilename)
    {
        List<RT.Occludee.MeshGroup> meshgroup = new List<RT.Occludee.MeshGroup>();

        EditorUtility.DisplayProgressBar("Generate LOD", "", 0);

        RT.Occludee.KdNode node = RT.Occludee.SpatialDivision(RT.Occludee.ExcludeStaticMesh(renderers), spatialdivision, meshgroup, null, "", false);
        List<byte> nodebytes = new List<byte>();
        MakeNodeBytes(node, nodebytes);
        lod.nodebyes = nodebytes.ToArray();

        List<Renderer> list = new List<Renderer>();
        List<int> nodelen = new List<int>();
        for (int i = 0; i < meshgroup.Count; i++)
        {
            list.AddRange(meshgroup[i].meshrenderer);
            nodelen.Add(meshgroup[i].meshrenderer.Length);
        }
        lod.nodelength = nodelen.ToArray();

        List<RTOccludee.RenderMesh> simplifiedMesh = new List<RTOccludee.RenderMesh>();
        BakeLodMeshWithSpatialDivision(node, lod, list.ToArray(), nodelen.ToArray(), bakedepth, simplifiedMesh, 0.1f, 1.0f);

        Debug.Log("Save:" + meshfilename);
        EditorUtility.ClearProgressBar();

        Mesh prefab = new Mesh();
        AssetDatabase.CreateAsset(prefab, meshfilename);

        for (int i = 0; i < simplifiedMesh.Count; i++)
            AssetDatabase.AddObjectToAsset(simplifiedMesh[i].mesh, prefab);

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();

        lod.lodmesh = simplifiedMesh.ToArray();
        lod.lodasset = prefab;

        return list.ToArray();
    }

    static void MakeNodeBytes(RT.Occludee.KdNode node, List<byte> list)
    {
        if (node == null)
            return;
        list.Add((byte)((node.left != null ? 1 : 0) + (node.right != null ? 2 : 0)));
        MakeNodeBytes(node.left, list);
        MakeNodeBytes(node.right, list);
    }

    static void BakeLodMeshWithSpatialDivision(RT.Occludee.KdNode node, RTOccludee.Lod lod, Renderer[] renderer, int[] nodelen, int depth, List<RTOccludee.RenderMesh> simplifiedMesh, float p1, float p2)
    {
        if (node == null)
            return;

        if (depth != 0)
        {
            BakeLodMeshWithSpatialDivision(node.left, lod, renderer, nodelen, depth - 1, simplifiedMesh, p1, (p1 + p2) * 0.5f);
            BakeLodMeshWithSpatialDivision(node.right, lod, renderer, nodelen, depth - 1, simplifiedMesh, (p1 + p2) * 0.5f, p2);
        }
        else if (depth == 0)
        {
            int j = 0;
            for (int i = 0; i < node.offset; i++)
                j += nodelen[i];

            List<Renderer> list = new List<Renderer>();
            for (int i = 0; i < node.length; i++)
            {
                for (int k = 0; k < nodelen[node.offset + i]; j++, k++)
                    list.Add(renderer[j]);
            }

            Renderer[] finalmesh = SimplifyMesh(null, list.ToArray(), lod, p1, p2);

            for (int i = 0; i < finalmesh.Length; i++)
            {
                Mesh m = GetMesh(finalmesh[i]);
                simplifiedMesh.Add(new RTOccludee.RenderMesh() { mesh = m, materials = finalmesh[i].sharedMaterials, offset = node.offset, length = node.length });
            }

            for (int i = 0; i < finalmesh.Length; i++)
                GameObject.DestroyImmediate(finalmesh[i].gameObject);
        }
    }

    static Renderer[] SimplifyMesh(Transform parent, Renderer[] list, RTOccludee.Lod lod, float p1, float p2)
    {
        Renderer[] mergedmesh = RT.Occludee.CreateMergedMesh(parent, list, true, true);

        List<Renderer> simplified = new List<Renderer>();

        List<GameObject> removeobj = new List<GameObject>();
        List<Mesh> removemesh = new List<Mesh>();

        long originaltri = 0;
        long reducetri = 0;

        for (int i = 0; i < mergedmesh.Length; i++)
        {
            UnityMeshSimplifier.MeshSimplifier meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();

            meshSimplifier.PreserveBorderEdges = lod.preserveBorderEdges;
            meshSimplifier.PreserveUVSeamEdges = lod.preserveUVSeamEdges;
            meshSimplifier.PreserveUVFoldoverEdges = lod.preserveUVFoldoverEdges;
            meshSimplifier.EnableSmartLink = lod.enableSmartLink;
            meshSimplifier.VertexLinkDistance = lod.vertexLinkDistanceSqr;
            meshSimplifier.MaxIterationCount = lod.maxIterationCount;
            meshSimplifier.Agressiveness = lod.agressiveness;

            Mesh sharedMesh = mergedmesh[i].GetComponent<MeshFilter>().sharedMesh;
            Mesh mesh = sharedMesh;

            if (IndexOf(list, mergedmesh[i]) != -1)
            {
                mesh = UnityEngine.Object.Instantiate(sharedMesh);
                Matrix4x4 tm = (parent == null ? Matrix4x4.identity : parent.worldToLocalMatrix) * mergedmesh[i].localToWorldMatrix;

                Vector3[] vertices = new Vector3[mesh.vertices.Length];
                for (int k = 0; k < vertices.Length; k++)
                    vertices[k] = tm.MultiplyPoint(mesh.vertices[k]);
                mesh.vertices = vertices;

                if (mesh.normals != null)
                {
                    Vector3[] normals = new Vector3[mesh.normals.Length];
                    for (int k = 0; k < normals.Length; k++)
                        normals[k] = tm.MultiplyVector(mesh.normals[k]);
                    mesh.normals = normals;
                }
                if (mesh.tangents != null)
                {
                    Vector4[] tangents = new Vector4[mesh.tangents.Length];
                    for (int k = 0; k < tangents.Length; k++)
                    {
                        Vector3 t = tm.MultiplyVector((Vector3)mesh.tangents[k]);
                        tangents[k] = new Vector4(t.x, t.y, t.z, mesh.tangents[k].w); ;
                    }
                    mesh.tangents = tangents;
                }
                removemesh.Add(mesh);
            }
            else
            {
                removeobj.Add(mergedmesh[i].gameObject);
            }

            meshSimplifier.Initialize(mesh);

            meshSimplifier.SimplifyMesh(lod.quality, (float p) => {
                EditorUtility.DisplayProgressBar("Generate LOD", "v:" + sharedMesh.vertexCount + ",t:" + (sharedMesh.triangles.Length / 3), p1 + (p2 - p1) * (i + p) / mergedmesh.Length);
            });

            Mesh result = meshSimplifier.ToMesh();
            simplified.Add(CreateMeshRenderer(result, mergedmesh[i].sharedMaterials));

            originaltri += mesh.triangles.Length / 3;
            reducetri += result.triangles.Length / 3;
        }

        Renderer[] finalmesh = RT.Occludee.CreateMergedMesh(null, simplified.ToArray(), false);
        for (int i = 0; i < finalmesh.Length; i++)
        {
            Mesh m = GetMesh(finalmesh[i]);
            m.name = "simplified," + originaltri + "," + i + "," + (m.triangles.Length / 3) + "/" + reducetri;
        }

        for (int i = 0; i < simplified.Count; i++)
        {
            if (IndexOf(finalmesh, simplified[i]) == -1)
                GameObject.DestroyImmediate(simplified[i].gameObject);
        }
        for (int i = 0; i < removeobj.Count; i++)
            GameObject.DestroyImmediate(removeobj[i]);
        for (int i = 0; i < removemesh.Count; i++)
            GameObject.DestroyImmediate(removemesh[i]);

        return finalmesh;
    }
#endif
    static int IndexOf<T>(T[] array, T value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].Equals(value))
                return i;
        }
        return -1;
    }

    static MeshRenderer CreateMeshRenderer(Mesh mesh, Material[] materials)
    {
        GameObject o = new GameObject();
        MeshFilter meshfilter = o.AddComponent<MeshFilter>();
        meshfilter.sharedMesh = mesh;
        MeshRenderer meshrenderer = o.AddComponent<MeshRenderer>();
        meshrenderer.sharedMaterials = materials;
        return meshrenderer;
    }
}