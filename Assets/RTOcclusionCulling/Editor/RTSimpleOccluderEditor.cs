using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RTSimpleOccluder))]
public class RTSimpleOccluderEditor : Editor
{
    Mesh m_Mesh;
    Mesh m_BoxMesh;
    Mesh m_CylinderMesh;

    private void OnEnable()
    {
        RTSimpleOccluder _target = (RTSimpleOccluder)target;

        if (_target.m_ConvexHullVertices != null && _target.m_ConvexHullVertices.Length > 3)
        {
            int[] polygons;
            RT.Occluder.ConvexHull3d(_target.m_ConvexHullVertices, out polygons);

            int[] triangles = RTConvexTowerOccluderEditor.TriangleList(polygons);
            m_Mesh = RTConvexTowerOccluderEditor.CreateMesh(_target.m_ConvexHullVertices, triangles);
        }
        m_BoxMesh = null;
        m_CylinderMesh = null;
    }

    private void OnDestroy()
    {
        if (m_Mesh != null)
            Mesh.DestroyImmediate(m_Mesh);
        if (m_BoxMesh != null)
            Mesh.DestroyImmediate(m_BoxMesh);
        if (m_CylinderMesh != null)
            Mesh.DestroyImmediate(m_CylinderMesh);
    }

    private void OnSceneGUI()
    {
        RTSimpleOccluder _target = (RTSimpleOccluder)target;

        if (Application.isPlaying)
            return;

        if (_target.m_Type == RTSimpleOccluder.Type.SimpleBox)
        {
            if (m_BoxMesh == null)
                m_BoxMesh = CreateTowerMesh(new Vector2[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) });

            Matrix4x4 tm = _target.transform.localToWorldMatrix * Matrix4x4.TRS(_target.m_Center, Quaternion.identity, _target.m_Size * 0.5f);
            RTConvexTowerOccluderEditor.DrawMesh(m_BoxMesh, tm, 0, new Color(1, 0, 0, 0.5f));
            RTConvexTowerOccluderEditor.DrawMesh(m_BoxMesh, tm, 0, new Color(0, 1, 0, 0.5f), UnityEngine.Rendering.CompareFunction.Greater); // occluder
            return;
        }
        else if (_target.m_Type == RTSimpleOccluder.Type.SimpleCylinder)
        {
            if (m_CylinderMesh == null)
                m_CylinderMesh = CreateCylinder(_target.m_Segment);

            Matrix4x4 tm = _target.transform.localToWorldMatrix * Matrix4x4.TRS(_target.m_Center, Quaternion.identity, new Vector3(_target.m_Radius, _target.m_Height * 0.5f, _target.m_Radius));
            RTConvexTowerOccluderEditor.DrawMesh(m_CylinderMesh, tm, 0, new Color(1, 0, 0, 0.5f));
            RTConvexTowerOccluderEditor.DrawMesh(m_CylinderMesh, tm, 0, new Color(0, 1, 0, 0.5f), UnityEngine.Rendering.CompareFunction.Greater); // occluder       
            return;
        }
        else if (_target.m_Type == RTSimpleOccluder.Type.FromCollider)
        {
            BoxCollider boxcollider = _target.GetComponent<BoxCollider>();

            if (boxcollider)
            {
                if (m_BoxMesh == null)
                    m_BoxMesh = CreateTowerMesh(new Vector2[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) });

                Matrix4x4 tm = _target.transform.localToWorldMatrix * Matrix4x4.TRS(boxcollider.center, Quaternion.identity, boxcollider.size * 0.5f);
                RTConvexTowerOccluderEditor.DrawMesh(m_BoxMesh, tm, 0, new Color(1, 0, 0, 0.5f));
                RTConvexTowerOccluderEditor.DrawMesh(m_BoxMesh, tm, 0, new Color(0, 1, 0, 0.5f), UnityEngine.Rendering.CompareFunction.Greater); // occluder
                return;
            }

            CapsuleCollider capsulecollider = _target.GetComponent<CapsuleCollider>();
            if (capsulecollider != null)
            {
                if (m_CylinderMesh == null)
                    m_CylinderMesh = CreateCylinder(_target.m_Segment);
                Matrix4x4 tm = _target.transform.localToWorldMatrix * Matrix4x4.TRS(capsulecollider.center, Quaternion.identity, new Vector3(capsulecollider.radius, capsulecollider.height * 0.5f - capsulecollider.radius, capsulecollider.radius));

                RTConvexTowerOccluderEditor.DrawMesh(m_CylinderMesh, tm, 0, new Color(1, 0, 0, 0.5f));
                RTConvexTowerOccluderEditor.DrawMesh(m_CylinderMesh, tm, 0, new Color(0, 1, 0, 0.5f), UnityEngine.Rendering.CompareFunction.Greater); // occluder
                return;
            }
        }

        if ((_target.m_Type == RTSimpleOccluder.Type.FromCollider || _target.m_Type == RTSimpleOccluder.Type.FromMeshFilter || _target.m_Type == RTSimpleOccluder.Type.FromCustomMesh) &&
            _target.m_ConvexHullVertices != null && _target.m_ConvexHullVertices.Length > 3)
        {
            int[] edges = _target.m_ConvexHullEdge;
            Vector3[] vtx = _target.m_ConvexHullVertices;
            Handles.color = Color.green;
            for (int i = 0, off = 0; i < edges.Length; i += 4, off += 2)
                Handles.DrawAAPolyLine(_target.transform.TransformPoint(vtx[edges[i + 2]]), _target.transform.TransformPoint(vtx[edges[i + 3]]));
        }

        if (m_Mesh != null)
        {
            Matrix4x4 tm = _target.transform.localToWorldMatrix;
            RTConvexTowerOccluderEditor.DrawMesh(m_Mesh, tm, 0, new Color(1, 0, 0, 0.5f));
            RTConvexTowerOccluderEditor.DrawMesh(m_Mesh, tm, 0, new Color(0, 1, 0, 0.5f), UnityEngine.Rendering.CompareFunction.Greater); // occluder
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginDisabledGroup(true);
        PropertyField("m_Script", new GUIContent("Script"), serializedObject);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(4);

        PropertyField("m_Type", new GUIContent("Type"), serializedObject);

        GUILayout.Space(4);

        RTSimpleOccluder _target = (RTSimpleOccluder)target;

        if (_target.m_Type == RTSimpleOccluder.Type.FromCollider)
        {
            BoxCollider boxcollider = _target.GetComponent<BoxCollider>();
            CapsuleCollider capsulecollider = _target.GetComponent<CapsuleCollider>();
            MeshCollider meshcollider = _target.GetComponent<MeshCollider>();

            if (boxcollider != null)
            {
                EditorGUILayout.HelpBox("Box Collider Detected", MessageType.Info);
            }
            else if (capsulecollider != null)
            {
                if (PropertyField("m_Segment", new GUIContent("Side Segment"), serializedObject))
                {
                    if (m_CylinderMesh != null)
                    {
                        Mesh.DestroyImmediate(m_CylinderMesh);
                        m_CylinderMesh = null;
                    }
                }

                EditorGUILayout.HelpBox("Capsule Collider Detected", MessageType.Info);
            }
            else if (meshcollider != null)
            {
                PropertyField("m_Limit Convex Vertex", new GUIContent("LimitConvex Vertex"), serializedObject);
                PropertyField("m_InnerDepth", new GUIContent("Inner Depth"), serializedObject);
                PropertyField("m_Offset", new GUIContent("Offset"), serializedObject);

                if (_target.m_ConvexHullVertices == null || _target.m_ConvexHullVertices.Length < 3)
                {
                    EditorGUILayout.HelpBox("It needs to update Occluder Mesh as Convex", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("Convex Collider Detected", MessageType.Info);
                }
                if (GUILayout.Button("Update Occluder as Convex Mesh", GUILayout.Height(30.0f)))
                {
                    Undo.RecordObject(target, "Update");
                    UpdateOccluder(meshcollider.sharedMesh, _target.m_LimitConvexVertex, _target.m_InnerDepth, _target.m_Offset);
                    SceneView.RepaintAll();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Needs Box Collider or MeshCollider", MessageType.Error);
            }
        }
        else if (_target.m_Type == RTSimpleOccluder.Type.SimpleBox)
        {
            PropertyField("m_Center", new GUIContent("Center"), serializedObject);
            PropertyField("m_Size", new GUIContent("Size"), serializedObject);
        }
        else if (_target.m_Type == RTSimpleOccluder.Type.SimpleCylinder)
        {
            PropertyField("m_Center", new GUIContent("Center"), serializedObject);
            PropertyField("m_Radius", new GUIContent("Radius"), serializedObject);
            PropertyField("m_Height", new GUIContent("Height"), serializedObject);
        }
        else if (_target.m_Type == RTSimpleOccluder.Type.FromMeshFilter)
        {
            PropertyField("m_LimitConvexVertex", new GUIContent("Limit ConvexVertex"), serializedObject);
            PropertyField("m_InnerDepth", new GUIContent("Inner Depth"), serializedObject);
            PropertyField("m_Offset", new GUIContent("Offset"), serializedObject);

            MeshFilter meshfilter = _target.GetComponent<MeshFilter>();
            if (meshfilter != null)
            {
                if (_target.m_ConvexHullVertices == null || _target.m_ConvexHullVertices.Length < 3)
                {
                    EditorGUILayout.HelpBox("It needs to update Occluder Mesh as Convex", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("MeshFilter Detected", MessageType.Info);
                }

                EditorGUI.BeginDisabledGroup(meshfilter.sharedMesh == null ? true : false);
                if (GUILayout.Button("Update Occluder as Convex Mesh", GUILayout.Height(30.0f)))
                {
                    Undo.RecordObject(target, "Update");
                    UpdateOccluder(meshfilter.sharedMesh, _target.m_LimitConvexVertex, _target.m_InnerDepth, _target.m_Offset);
                    SceneView.RepaintAll();
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox("Needs MeshFilter", MessageType.Error);
            }

        }
        else if (_target.m_Type == RTSimpleOccluder.Type.FromCustomMesh)
        {
            PropertyField("m_LimitConvexVertex", new GUIContent("Limit Convex Vertex"), serializedObject);
            PropertyField("m_InnerDepth", new GUIContent("Inner Depth"), serializedObject);
            PropertyField("m_Offset", new GUIContent("Offset"), serializedObject);

            GUILayout.Space(4);

            PropertyField("m_CustomMesh", new GUIContent("Custom Mesh"), serializedObject);

            if (_target.m_CustomMesh == null)
            {
                EditorGUILayout.HelpBox("It Needs custom mesh", MessageType.Error);
            }
            else if (_target.m_ConvexHullVertices == null || _target.m_ConvexHullVertices.Length < 3)
            {
                EditorGUILayout.HelpBox("It Needs to update Occluder Mesh as Convex", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("Custom Mesh Detected", MessageType.Info);
            }
            EditorGUI.BeginDisabledGroup(_target.m_CustomMesh == null ? true : false);
            if (GUILayout.Button("Update Convex Occluder From Custom Mesh", GUILayout.Height(30.0f)))
            {
                Undo.RecordObject(target, "Update");
                UpdateOccluder(_target.m_CustomMesh, _target.m_LimitConvexVertex, _target.m_InnerDepth, _target.m_Offset);
                SceneView.RepaintAll();
            }
            EditorGUI.EndDisabledGroup();
        }
    }

    void UpdateOccluder(Mesh mesh, int limitvertex, float innerdepth, Vector3 offset)
    {
        UpdateOccluder(mesh.vertices, limitvertex, innerdepth, offset);
    }

    void UpdateOccluder(Vector3[] vertices, int limitvertex, float innerdepth, Vector3 offset)
    {
        Vector3[] vtx;
        int[] polygons;
        int[] hull;
        int[] edge;

        if (limitvertex == 0)
        {
            vtx = RT.Occluder.EliminateDuplicatedVertices(vertices);
            RT.Occluder.ConvexHull3d(vtx, out polygons);
        }
        else
        {
            vtx = RT.Occluder.Quantization(vertices, 10);

            RT.Occluder.ConvexHull3d(vtx, out polygons);

            int[] vtxtable = new int[vtx.Length];
            for (int i = 0; i < vtxtable.Length; i++)
                vtxtable[i] = -1;

            List<Vector3> v = new List<Vector3>();
            for (int i = 0; i < polygons.Length; i++)
            {
                int p = polygons[i];
                if (p != -1)
                {
                    if (vtxtable[p] == -1)
                    {
                        vtxtable[p] = v.Count;
                        v.Add(vtx[p]);
                    }
                    polygons[i] = vtxtable[p];
                }
            }

            vtx = v.ToArray();

            if (vtx.Length > limitvertex || innerdepth > 0.0f)
            {
                if (innerdepth > 0.0f)
                {
                    Vector3 center = Vector3.zero;

                    for (int i = 0; i < vtx.Length; i++)
                        center += vtx[i] * (1.0f / vtx.Length);

                    for (int i = 0; i < vtx.Length; i++)
                        vtx[i] = Vector3.Lerp(vtx[i], center, innerdepth);
                }

                vtx = RT.Occluder.ReduceVertexForConvex(vtx, polygons, limitvertex);
                RT.Occluder.ConvexHull3d(vtx, out polygons);
            }
        }

        RT.Occluder.GetVolumeEdgeAndHull(polygons, out hull, out edge);

        for (int i = 0; i < vtx.Length; i++)
            vtx[i] += offset;

        RTSimpleOccluder _target = (RTSimpleOccluder)target;
        _target.m_ConvexHullVertices = vtx;
        _target.m_ConvexHullTriangles = hull;
        _target.m_ConvexHullEdge = edge;

        if (m_Mesh != null)
            Mesh.DestroyImmediate(m_Mesh);

        int[] triangles = RTConvexTowerOccluderEditor.TriangleList(polygons);
        m_Mesh = RTConvexTowerOccluderEditor.CreateMesh(vtx, triangles);
    }

    static Mesh CreateTowerMesh(Vector2[] points)
    {
        Vector3[] vtx = new Vector3[points.Length * 2];
        for (int i = 0; i < points.Length; i++)
        {
            vtx[i] = new Vector3(points[i].x, -1.0f, points[i].y);
            vtx[i + points.Length] = vtx[i] + Vector3.up * 2.0f;
        }
        List<int> triangles = new List<int>((points.Length - 1) * 2 * 3 + (points.Length - 2) * 3);
        for (int i = 2; i < points.Length; i++)
        {
            triangles.Add(0);
            triangles.Add(i-1);
            triangles.Add(i);
            triangles.Add(0 + points.Length);
            triangles.Add(i + points.Length);
            triangles.Add(i-1 + points.Length);
        }
        for (int i2=0, i1=points.Length-1; i2 < points.Length; i1=i2++)
        {
            triangles.Add(i1);
            triangles.Add(i1+points.Length);
            triangles.Add(i2+points.Length);
            triangles.Add(i1);
            triangles.Add(i2+points.Length);
            triangles.Add(i2);
        }
        return RTConvexTowerOccluderEditor.CreateMesh(vtx, triangles.ToArray());
    }

    static Mesh CreateCylinder(int segment)
    {
        Vector2[] points = new Vector2[segment];
        for (int i = 0; i < points.Length; i++)
            points[i] = new Vector2(Mathf.Cos(i * 2 * Mathf.PI / points.Length), Mathf.Sin(i * 2 * Mathf.PI / points.Length));
        return CreateTowerMesh(points);
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