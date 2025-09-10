using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(RTConvexTowerOccluder))]
public class RTConvexTowerOccluderEditor : Editor
{
    enum Mode
    {
        ConvexPointInsert = 0,
        Window = 1
    };

    Mode m_Mode = Mode.ConvexPointInsert;

    enum State
    {
        Idle,
        PointMove,
        LineMove,
        ExtranPointMove,
        OnPoint,
        OnLine,
        OnSurface,
        OnExtraPoint,
        OnPlane,
        HotControl,
        EdgeShift,

        OnSide,
        OnSideDrag,
        OnWindow,
        WindowPointMove,
    };

    State m_State = State.Idle;
    int m_PointIndex = -1;
    int m_ExtraPoint = -1;
    int m_LineIndex = -1;
    int m_SelectSide = -1;
    int m_WindowIndex = -1;
    int m_WindowPointIndex = -1;

    Vector3 m_OnMousePoint;

    Vector3[] m_Vertex;
    int[] m_Triangles;

    Mesh m_Mesh;

    bool WindowEditMode() { return m_Mode == Mode.Window ? true : false; }

    static int MAXPOINTS = 30;

    void SetState(State state)
    {
        m_State = state;

        if (state == State.OnLine || state == State.OnPoint || state == State.PointMove || state == State.LineMove)
            m_ExtraPoint = -1;
    }

    void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoOrRedo;

        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;
        if (_target.m_Points.Count == 0)
            ResetBound(_target);

        UpdateMesh();
        SetState(State.Idle);
        m_ExtraPoint = -1;
    }

    void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoOrRedo;
    }

    void OnUndoOrRedo()
    {
        UpdateMesh();
        Repaint();
    }

    static string[] _tab = new string[] { "Convex Point Insert", "Window" };

    public override void OnInspectorGUI()
    {
        m_ActiveTarget = targets.Length >= 1 ? (RTConvexTowerOccluder)targets[0] : null;

        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
            UpdateMesh();

        GUILayout.Space(12);

        EditorGUI.BeginChangeCheck();

        if (targets.Length >= 1)
        {
            RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;

            EditorGUI.BeginDisabledGroup(_target.m_ExtraPoints.Count > 0 || _target.m_Window.Count > 0 ? true : false);
            if (m_Mode != Mode.Window && _target.m_Window.Count > 0)
                m_Mode = Mode.Window;

            Mode mode = (Mode)GUILayout.Toolbar((int)m_Mode, _tab);
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                m_Mode = mode;
                SceneView.RepaintAll();
            }

            if (m_Mode == Mode.ConvexPointInsert)
                EditorGUILayout.HelpBox("Click with the control key to add additional points for convex shape.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("click with the control key to add or delete the window, darg the point to modify window rect.", MessageType.Info);

            if (mode == Mode.ConvexPointInsert)
            {
                EditorGUI.BeginDisabledGroup(_target.m_ExtraPoints.Count == 0 ? true : false);
                if (GUILayout.Button("Reset ExtraPoints", GUILayout.Height(30.0f)))
                {
                    Undo.RecordObject(_target, "Reset");
                    _target.m_ExtraPoints.Clear();
                    UpdateMesh();
                    SceneView.RepaintAll();
                }
                EditorGUI.EndDisabledGroup();
            }
            else if (mode == Mode.Window)
            {
                EditorGUI.BeginDisabledGroup(_target.m_Window.Count == 0 ? true : false);
                if (GUILayout.Button("Reset Window", GUILayout.Height(30.0f)))
                {
                    Undo.RecordObject(_target, "Reset Window");
                    _target.m_Window.Clear();
                    SceneView.RepaintAll();
                }
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Space(12);

            EditorGUI.BeginDisabledGroup(targets.Length > 1 ? true : false);
            if (GUILayout.Button("Set Default Bound", GUILayout.Height(30.0f)))
            {
                Undo.RecordObject(_target, "Reset");
                ResetBound(_target);
                UpdateMesh();
                SceneView.RepaintAll();
            }
            EditorGUI.EndDisabledGroup();
        }

        if (GUILayout.Button("Open RTOccluder Workspace", GUILayout.Height(30.0f)))
        {
            RTOccluderWindow window = (RTOccluderWindow)EditorWindow.GetWindow(typeof(RTOccluderWindow), false, "RTOccluder Workspace");
            window.Show();
        }
    }

    static Vector3 Center(Vector3[] v)
    {
        Vector3 p = v[0];
        for (int i = 1; i < v.Length; i++)
            p += v[i];
        return p * (1.0f / v.Length);
    }

    void UpdateConvexPoints(RTConvexTowerOccluder _target)
    {
        Vector2[] p = _target.m_Points.ToArray();
        Vector2[] convex = RT.Occluder.ConvexHull2d(p);

        for (int i = 0; i < _target.m_Window.Count;)
        {
            int match = -1, matchnext = -1;
            for (int j = 0; j < convex.Length; j++)
            {
                if (p[_target.m_Window[i].index] == convex[j])
                    match = j;
                else if (p[(_target.m_Window[i].index + 1) % p.Length] == convex[j])
                    matchnext = j;
            }

            if (match != -1)
                _target.m_Window[i++].index = match;
            else if (matchnext != -1)
                _target.m_Window[i++].index = (match + convex.Length - 1) % convex.Length;
            else
            {
                Debug.Log("Remove windows due to merged edges.");
                _target.m_Window.RemoveAt(i);
            }
        }

        _target.m_Points.Clear();
        _target.m_Points.AddRange(convex);
    }

    RTConvexTowerOccluder m_ActiveTarget = null;

    private void OnSceneGUI()
    {
        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;

        if (Application.isPlaying)
        {
            //Matrix4x4 tm = _target.transform.localToWorldMatrix;
            //DrawMesh(tm, 0, new Color(1, 0, 0, 0.5f));
            //DrawMesh(tm, 0, new Color(0, 1, 0, 0.5f), UnityEngine.Rendering.CompareFunction.Greater); // occluder
            return;
        }

        if (m_ActiveTarget != null && _target != m_ActiveTarget)
            return;

        Event guiEvent = Event.current;
        SceneView view = SceneView.currentDrawingSceneView;

        if (_target.m_Points == null)
            _target.m_Points = new List <Vector2> ();

        if (_target.m_ExtraPoints == null)
            _target.m_ExtraPoints = new List<Vector3>();

        if (guiEvent.type == EventType.Repaint)
        {
            DrawGrid(view.camera, _target.transform.position, _target.transform.right, _target.transform.forward);
        }

        if (guiEvent.type == EventType.Repaint)
        {
            Matrix4x4 tm = _target.transform.localToWorldMatrix;
            if (m_Mesh != null)
            {
                DrawMesh(m_Mesh, tm, 0, new Color(1, 0, 0, 0.5f));
                DrawMesh(m_Mesh, tm, 0, new Color(0, 1, 0, 0.5f), UnityEngine.Rendering.CompareFunction.Greater); // occluder
            }
        }

        // CONVEX TOWER

        Vector3 h = _target.transform.TransformVector(Vector3.up * _target.m_Height);

        Vector3[] vtx = new Vector3[_target.m_Points.Count * 2 + _target.m_ExtraPoints.Count];
        Vector2[] point = _target.m_Points.ToArray();
        Vector3[] xtrapoint = _target.m_ExtraPoints.ToArray();

        if (m_State == State.PointMove)
        {
            Vector2[] convex = RT.Occluder.ConvexHull2d(point);
            for (int i = 0; i < convex.Length; i++)
                vtx[i] = _target.transform.TransformPoint((new Vector3(convex[i].x, 0, convex[i].y)));

            Handles.color = Color.green;

            for (int i = 0; i < convex.Length; i++)
                Handles.DrawAAPolyLine(vtx[i], vtx[(i + 1) % convex.Length]);
        }

        for (int i = 0; i < point.Length; i++)
        {
            vtx[i] = _target.transform.TransformPoint((new Vector3(point[i].x, 0, point[i].y)));
            vtx[i + point.Length] = vtx[i] + h;
        }

        for (int i = 0; i < xtrapoint.Length; i++)
        {
            vtx[i + point.Length * 2] = _target.transform.TransformPoint(_target.m_ExtraPoints[i]);
        }

        int[] edges = _target.m_Edge;

        Handles.color = Color.white;
        if (edges != null)
        {
            for (int i = 0, off=0; i < edges.Length; i+=4, off+=2)
                Handles.DrawAAPolyLine(vtx[edges[i + 2]], vtx[edges[i + 3]]);
        }

        if (m_State != State.PointMove)
        {
            for (int i = 0; i < point.Length; i++)
            {
                Handles.color = (m_State == State.OnLine || m_State == State.LineMove) && i == m_LineIndex ? Color.red : Color.green;
                Handles.DrawAAPolyLine(vtx[i], vtx[(i + 1) % point.Length]);
            }
        }

        for (int i = 0; i < point.Length; i++)
        {
            float unit = WorldSize(10, view.camera, vtx[i]) * 0.5f;

            Vector3 axis = _target.transform.up;

            if ((m_State == State.OnPoint || m_State == State.PointMove) && i == m_PointIndex)
            {
                Handles.color = Color.red;
                Handles.DrawSolidDisc(vtx[i], axis, unit);
                DrawDisc(vtx[i], unit * 4, axis);
            }
            else
            {
                Handles.color = Color.green;
                Handles.DrawSolidDisc(vtx[i], axis, unit);
            }
        }

        // CONVEX TOWER - HEIGHT

        Vector2 localCenter = Vector2.zero;
        for (int i = 0; i < _target.m_Points.Count; i++)
            localCenter += _target.m_Points[i] * (1.0f / _target.m_Points.Count);
        Vector3 top = _target.transform.TransformPoint(new Vector3(localCenter.x, _target.m_Height, localCenter.y));

        Handles.color = Color.green;
        EditorGUI.BeginChangeCheck();
#if UNITY_5_3_OR_NEWER
        top = Handles.Slider(top, _target.transform.up, WorldSize(50, view.camera, top), Handles.ArrowHandleCap, 0.0f);
#else
        top = Handles.Slider(top, _target.transform.up, WorldSize(50, view.camera, top), Handles.ArrowCap, 0.0f);
#endif
        if (EditorGUI.EndChangeCheck())
        {
            float y = _target.transform.InverseTransformPoint(top).y;
            if (y > 0)
            {
                Undo.RecordObject(_target, "Update Occluder Height");

                _target.m_Height = y;
                UpdateMesh();

                m_ExtraPoint = -1;
            }
        }

        // CONVEX TOWER SHIFT EDGE

        if (m_State == State.Idle || m_State == State.OnSurface || m_State == State.HotControl || m_State == State.EdgeShift || m_State == State.OnSide)
        {
            Vector3 center = Center(vtx);

            for (int i = 0; i < point.Length; i++)
            {
                Vector3 v1 = Vector3.Lerp(vtx[i], vtx[(i + 1) % point.Length], 0.5f);
                Vector3 dir = Vector3.Cross(vtx[(i + 1) % point.Length] - vtx[i], _target.transform.up);

                if (Vector3.Dot(dir, center - vtx[i]) > 0)
                    dir *= -1.0f;

                EditorGUI.BeginChangeCheck();
#if UNITY_5_3_OR_NEWER
                Vector3 v2 = Handles.Slider(v1, dir, WorldSize(50, view.camera, v1), Handles.ArrowHandleCap, 0.0f);
#else
                Vector3 v2 = Handles.Slider(v1, dir, WorldSize(50, view.camera, v1), Handles.ArrowCap, 0.0f);
#endif
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Shift Edge");

                    SetState(State.EdgeShift);

                    Vector3 dv = _target.transform.InverseTransformDirection(v2 - v1);
                    _target.m_Points[i] += new Vector2(dv.x, dv.z);
                    _target.m_Points[(i + 1) % _target.m_Points.Count] += new Vector2(dv.x, dv.z);

                    UpdateMesh();
                    m_ExtraPoint = -1;
                }
            }
        }

        if (!WindowEditMode())
        {
            //  EXTRA POINTS

            for (int i = 0; i < xtrapoint.Length; i++)
            {
                Vector3 p = _target.transform.TransformPoint(xtrapoint[i]);
                float unit = WorldSize(10, view.camera, p) * 0.5f;

                Vector3 axis = _target.transform.up;

                if (i == m_ExtraPoint)
                {
                    Handles.color = Color.red;

                    Handles.DrawSolidDisc(p, axis, unit);
                    DrawDisc(p, unit * 4, axis);

                    Handles.color = Color.green;
                    EditorGUI.BeginChangeCheck();

                    Vector3 offset = Vector3.up * unit * 3;
#if UNITY_5_3_OR_NEWER
                    p = Handles.Slider(p + offset, _target.transform.up, WorldSize(50, view.camera, p), Handles.ArrowHandleCap, 0.0f) - offset;
#else
                    p = Handles.Slider(p + offset, _target.transform.up, WorldSize(50, view.camera, p), Handles.ArrowCap, 0.0f) - offset;
#endif
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_target, "Update Occluder Height");

                        _target.m_ExtraPoints[i] = _target.transform.InverseTransformPoint(p);
                        UpdateMesh();
                    }
                }
                else
                {
                    Handles.color = Color.blue;
                    Handles.DrawSolidDisc(p, axis, unit);
                }
            }
        }

        Vector2[] points = _target.m_Points.ToArray();

        for (int i = 0; i < _target.m_Window.Count; i++)
        {
            int side = _target.m_Window[i].index;
            Rect rect = _target.m_Window[i].rect;

            Vector3 p1 = _target.transform.TransformPoint(new Vector3(points[side].x, 0, points[side].y));
            Vector3 p2 = _target.transform.TransformPoint(new Vector3(points[(side + 1) % points.Length].x, 0, points[(side + 1) % points.Length].y));
            Vector3 p3 = p1 + _target.transform.TransformVector(Vector3.up * _target.m_Height);
            //Vector3 p4 = p2 + _target.transform.TransformVector(Vector3.up * _target.m_Height);

            Vector3 n = Vector3.Normalize(Vector3.Cross(p3 - p1, p2 - p1));
            Plane plane = new Plane(n, p1);

            if (plane.GetDistanceToPoint(view.camera.transform.position) > 0)
            {
                Vector2 portalmin = new Vector2(rect.xMin, rect.yMin);
                Vector2 portalmax = new Vector2(rect.xMax, rect.yMax);

                Vector3[] vv = new Vector3[4];
                for (int j = 0; j < 4; j++)
                {
                    Vector2 uv = new Vector2((j & 1) == 0 ? portalmin.x : portalmax.x, (j & 2) == 0 ? portalmin.y : portalmax.y);
                    vv[j] = p1 + uv.x * (p2 - p1) + uv.y * (p3 - p1);
                }

                Handles.color = WindowEditMode() ? Color.red : Color.white;
                Handles.DrawLine(vv[0], vv[1]);
                Handles.DrawLine(vv[1], vv[3]);
                Handles.DrawLine(vv[3], vv[2]);
                Handles.DrawLine(vv[2], vv[0]);

                if (WindowEditMode())
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Handles.color = (m_State == State.OnWindow || m_State == State.WindowPointMove) && i == m_WindowIndex && j == m_WindowPointIndex ? Color.red : Color.white;
                        Handles.DrawSolidDisc(vv[j], n, WorldSize(5, view.camera, vv[j]));
                    }
                }
            }
        }
        
        if (guiEvent.type == EventType.Layout)
        {
            if ((m_State != State.Idle && m_State != State.HotControl && m_State != State.OnSide) || (WindowEditMode() && guiEvent.control))
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        if ((guiEvent.type == EventType.MouseMove || guiEvent.type == EventType.MouseDown || guiEvent.type == EventType.MouseDrag || guiEvent.type == EventType.MouseUp) && guiEvent.button == 0)
        {
            UpdateMouseInfo(guiEvent.mousePosition, guiEvent.type, view, guiEvent.modifiers == EventModifiers.Control ? true : false);

            if (m_State != State.Idle && m_State != State.HotControl && m_State != State.OnSide)
                guiEvent.Use();

            HandleUtility.Repaint();
        }

        if ((m_State != State.HotControl && m_State != State.EdgeShift) && GUIUtility.hotControl != 0)
        {
            SetState(State.HotControl);
        }
        else if ((m_State == State.HotControl || m_State == State.EdgeShift) && GUIUtility.hotControl == 0)
        {
            if (m_State == State.EdgeShift)
            {
                Undo.RecordObject(_target, "Shift Edge");
                UpdateConvexPoints(_target);
                UpdateMesh();
            }

            SetState(State.Idle);
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
        {
            SetState(State.Idle);
        }

        Rect fullrect = new Rect(0, 0, Screen.width, Screen.height);
        if ((m_State == State.OnLine || m_State == State.OnSurface || m_State == State.OnSide) && guiEvent.modifiers == EventModifiers.Control)
            EditorGUIUtility.AddCursorRect(fullrect, MouseCursor.ArrowPlus);
        else if (m_State == State.PointMove || m_State == State.LineMove)
            EditorGUIUtility.AddCursorRect(fullrect, MouseCursor.MoveArrow);
        else if (m_State == State.OnExtraPoint || m_State == State.OnPoint || m_State == State.OnWindow)
            EditorGUIUtility.AddCursorRect(fullrect, guiEvent.modifiers == EventModifiers.Control ? MouseCursor.ArrowMinus : MouseCursor.MoveArrow);
    }

    bool ContainsPoint(Ray mouseray, SceneView sceneview, Vector2 point)
    {
        return ContainsPoint(mouseray, sceneview, new Vector3(point.x, 0, point.y));
    }

    bool ContainsPoint(Ray mouseray, SceneView sceneview, Vector3 point)
    {
        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;
        Vector3 p = _target.transform.TransformPoint(point);
        float handleRadius = WorldSize(10.0f, sceneview.camera, p) * 0.5f * 1.25f;
        return DistanceRayToPoint(p, mouseray) < handleRadius ? true : false;
    }

    void UpdateMouseState(Ray mouseray, SceneView sceneview, bool controldown)
    {
        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;

        for (int i = 0; i < _target.m_Points.Count; i++)
        {
            if (ContainsPoint(mouseray, sceneview, _target.m_Points[i]))
            {
                if (m_State != State.OnPoint || i != m_PointIndex)
                {
                    m_PointIndex = i;
                    SetState(State.OnPoint);
                    Repaint();
                }
                return;
            }
        }

        for (int i = 0; i < _target.m_Points.Count; i++)
        {
            Vector3 p1 = _target.transform.TransformPoint(new Vector3(_target.m_Points[i].x, 0, _target.m_Points[i].y));
            Vector3 p2 = _target.transform.TransformPoint(new Vector3(_target.m_Points[(i + 1) % _target.m_Points.Count].x, 0, _target.m_Points[(i + 1) % _target.m_Points.Count].y));

            Vector3 touchpos;
            float distance = DistanceRayToLineSegment(p1, p2, mouseray, out touchpos);
            if (distance < WorldSize(5.0f, sceneview.camera, (p1 + p2) * 0.5f) * 0.5f)
            {
                if (m_State != State.OnLine || m_LineIndex != i)
                {
                    m_LineIndex = i;
                    SetState(State.OnLine);
                    Repaint();
                }
                return;
            }
        }

        for (int i = 0; i < _target.m_ExtraPoints.Count; i++)
        {
            if (ContainsPoint(mouseray, sceneview, _target.m_ExtraPoints[i]))
            {
                if (m_State != State.OnExtraPoint || m_ExtraPoint != i)
                {
                    m_ExtraPoint = i;
                    SetState(State.OnExtraPoint);
                    Repaint();
                }
                return;
            }
        }

        if (m_Vertex != null && controldown)
        {
            Ray localRay = new Ray(_target.transform.InverseTransformPoint(mouseray.origin), _target.transform.InverseTransformVector(mouseray.direction));
            float mint = 1000.0f;
            for (int i = 0; i < m_Triangles.Length; i += 3)
                mint = IntersectTest(localRay, m_Vertex[m_Triangles[i]], m_Vertex[m_Triangles[i + 1]], m_Vertex[m_Triangles[i + 2]], mint);
            if (mint < 1000.0f)
            {
                m_OnMousePoint = localRay.origin + localRay.direction * mint;
                SetState(State.OnSurface);
                return;
            }
        }

        SetState(State.Idle);
    }

    bool UpdateMouseStateWindow(Ray mouseray, EventType type, bool controldown, SceneView sceneview, Vector2 mouseposition)
    {
        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;

        m_SelectSide = GetSelectSide(mouseposition);

        if (m_SelectSide != -1)
        {
            Vector2[] points = _target.m_Points.ToArray();

            int selectwindow = -1;
            int selectwindowpoint = -1;

            for (int i = 0; i < _target.m_Window.Count; i++)
            {
                int side = _target.m_Window[i].index;
                if (m_SelectSide != side)
                    continue;

                Vector3 p1 = _target.transform.TransformPoint(new Vector3(points[side].x, 0, points[side].y));
                Vector3 p2 = _target.transform.TransformPoint(new Vector3(points[(side + 1) % points.Length].x, 0, points[(side + 1) % points.Length].y));
                Vector3 p3 = p1 + _target.transform.TransformVector(Vector3.up * _target.m_Height);
                //Vector3 p4 = p2 + _target.transform.TransformVector(Vector3.up * _target.m_Height);

                Vector3 n = Vector3.Normalize(Vector3.Cross(p3 - p1, p2 - p1));
                Vector3 pick = IntersectPlane(n, p1, mouseray);

                Vector2 portalmin = new Vector2(_target.m_Window[i].rect.xMin, _target.m_Window[i].rect.yMin);
                Vector2 portalmax = new Vector2(_target.m_Window[i].rect.xMax, _target.m_Window[i].rect.yMax);

                for (int j = 0; j < 4; j++)
                {
                    Vector2 uv = new Vector2((j & 1) == 0 ? portalmin.x : portalmax.x, (j & 2) == 0 ? portalmin.y : portalmax.y);
                    Vector3 vv = p1 + uv.x * (p2 - p1) + uv.y * (p3 - p1);

                    float handleRadius = WorldSize(10.0f, sceneview.camera, vv) * 1.5f;
                    if (Vector3.Distance(pick, vv) < handleRadius)
                    {
                        selectwindow = i;
                        selectwindowpoint = j;

                        m_CrossUV = new Vector2((j & 1) == 0 ? portalmax.x : portalmin.x, (j & 2) == 0 ? portalmax.y : portalmin.y);
                        break;
                    }
                }

                if (selectwindowpoint != -1)
                    break;
            }

            if (m_WindowPointIndex != selectwindowpoint)
            {
                m_WindowIndex = selectwindow;
                m_WindowPointIndex = selectwindowpoint;
            }

            if (m_WindowPointIndex != -1 && m_WindowIndex != -1)
                SetState(State.OnWindow);
            else
            {
                if (m_SelectSide != -1)
                    SetState(State.OnSide);

                Repaint();
            }

            return true;
        }
        
        SetState(State.Idle);
        return false;
    }

    void UpdateMouseInfo(Vector2 mousePosition, EventType type, SceneView sceneview, bool controldown)
    {
        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;

        Ray mouseray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (WindowEditMode() &&
            (m_State == State.Idle || m_State == State.OnSide || m_State == State.OnWindow || m_State == State.OnLine))
        {
            Vector3 mouseposition = mousePosition;
            UpdateMouseStateWindow(mouseray, type, controldown, sceneview, mouseposition);
        }

        if (m_State == State.Idle || m_State == State.OnPoint || m_State == State.OnLine || m_State == State.OnExtraPoint || m_State == State.OnSurface)
        {
            UpdateMouseState(mouseray, sceneview, controldown);
        }

        if (type == EventType.MouseDown)
        {
            if (m_State == State.OnPoint)
            {
                if (controldown == true && _target.m_Points.Count > 3)
                {
                    Undo.RecordObject(_target, "Remove Point");

                    for (int i = 0; i < _target.m_Window.Count; i++)
                    {
                        if (_target.m_Window[i].index >= m_PointIndex)
                            _target.m_Window[i].index--;
                    }

                    _target.m_Points.RemoveAt(m_PointIndex);
                    m_PointIndex = -1;
                    UpdateMesh();                    
                }
                else
                {
                    SetState(State.PointMove);

                    Ray localRay = new Ray(_target.transform.InverseTransformPoint(mouseray.origin), _target.transform.InverseTransformVector(mouseray.direction));
                    Vector3 mouseposition = IntersectPlane(Vector3.up, Vector3.zero, localRay);

                    m_OnMousePoint = mouseposition;
                }
            }
            else if (m_State == State.OnExtraPoint)
            {
                if (controldown == true)
                {
                    Undo.RecordObject(_target, "Remove Extra Point");

                    _target.m_ExtraPoints.RemoveAt(m_ExtraPoint);
                    m_ExtraPoint = -1;
                    UpdateMesh();
                }
                else
                {
                    SetState(State.ExtranPointMove);

                    Ray localRay = new Ray(_target.transform.InverseTransformPoint(mouseray.origin), _target.transform.InverseTransformVector(mouseray.direction));
                    Vector3 mouseposition = IntersectPlane(Vector3.up, _target.m_ExtraPoints[m_ExtraPoint], localRay);

                    m_OnMousePoint = mouseposition;
                }
            }
            else if (m_State == State.OnSurface && controldown)
            {
                Undo.RecordObject(_target, "Add Extra Point");

                m_ExtraPoint = _target.m_ExtraPoints.Count;
                _target.m_ExtraPoints.Add(m_OnMousePoint);
                SetState(State.ExtranPointMove);
            }
            else if (m_State == State.OnSide && controldown == true)
            {
                m_OnMousePoint = mousePosition;
                m_State = State.OnSideDrag;
            }
            else if (m_State == State.OnWindow)
            {
                if (controldown == true)
                {
                    Undo.RecordObject(_target, "Remove Window");
                    _target.m_Window.RemoveAt(m_WindowIndex);
                    m_WindowIndex = -1;
                    m_WindowPointIndex = -1;
                }
                else
                {
                    SetState(State.WindowPointMove);
                }
            }

            if (m_State == State.OnLine && controldown == true)
            {
                if (_target.m_Points.Count < MAXPOINTS)
                {
                    Undo.RecordObject(_target, "Add Point");

                    for (int i = 0; i < _target.m_Window.Count; i++)
                    {
                        if (_target.m_Window[i].index >= m_LineIndex)
                            _target.m_Window[i].index++;
                    }

                    Ray localRay = new Ray(_target.transform.InverseTransformPoint(mouseray.origin), _target.transform.InverseTransformVector(mouseray.direction));
                    Vector3 mouseposition = IntersectPlane(Vector3.up, Vector3.zero, localRay);

                    int newindex = m_LineIndex + 1;
                    _target.m_Points.Insert(newindex, new Vector2(mouseposition.x, mouseposition.z));
                    m_PointIndex = newindex;
                    m_LineIndex = -1;

                    SetState(State.PointMove);
                    m_OnMousePoint = mouseposition;
                }
            }
            if (m_State == State.OnLine && controldown == false)
            {
                SetState(State.LineMove);

                Ray localRay = new Ray(_target.transform.InverseTransformPoint(mouseray.origin), _target.transform.InverseTransformVector(mouseray.direction));
                Vector3 mouseposition = IntersectPlane(Vector3.up, Vector3.zero, localRay);
                m_OnMousePoint = mouseposition;
            }
        }

        if (type == EventType.MouseDrag)
        {
            if (m_State == State.PointMove || m_State == State.ExtranPointMove)
            {
                Ray localRay = new Ray(_target.transform.InverseTransformPoint(mouseray.origin), _target.transform.InverseTransformVector(mouseray.direction));
                Vector3 mouseposition = IntersectPlane(Vector3.up, m_State == State.PointMove ? Vector3.zero : _target.m_ExtraPoints[m_ExtraPoint], localRay);
                Vector3 v = mouseposition - m_OnMousePoint;
                m_OnMousePoint = mouseposition;

                Undo.RecordObject(_target, m_State == State.PointMove ? "Move Point" : "Move ExtraPoint");
                if (m_State == State.PointMove)
                    _target.m_Points[m_PointIndex] += new Vector2(v.x, v.z);
                else
                    _target.m_ExtraPoints[m_ExtraPoint] += new Vector3(v.x, 0, v.z);
                UpdateMesh();
            }
            else if (m_State == State.LineMove)
            {
                Ray localRay = new Ray(_target.transform.InverseTransformPoint(mouseray.origin), _target.transform.InverseTransformVector(mouseray.direction));
                Vector3 mouseposition = IntersectPlane(Vector3.up, Vector3.zero, localRay);
                Vector3 v = mouseposition - m_OnMousePoint;
                m_OnMousePoint = mouseposition;

                Undo.RecordObject(_target, "Line Move");

                //Vector2 line = _target.m_Points[m_LineIndex] - _target.m_Points[(m_LineIndex + 1) % _target.m_Points.Count];
                //Vector2 n = (new Vector2(-line.y, line.x)).normalized;
                //Vector2 mv = Vector2.Dot(new Vector2(v.x, v.z), n) * n;
                Vector2 mv = new Vector3(v.x, v.z);

                _target.m_Points[m_LineIndex] += mv;
                _target.m_Points[(m_LineIndex + 1) % _target.m_Points.Count] += mv;

                UpdateMesh();
            }
            else if (m_State == State.OnSideDrag)
            {
                if (Vector3.Magnitude((Vector3)mousePosition - m_OnMousePoint) > 1.5f)
                {
                    Undo.RecordObject(_target, "Create Window");

                    SetState(State.WindowPointMove);
                    int newindex = _target.m_Window.Count;
                    RTConvexTowerOccluder.Window w = new RTConvexTowerOccluder.Window();
                    w.index = m_SelectSide;
                    w.rect = new Rect(m_AnchorUV.x, m_AnchorUV.y, 0, 0);
                    _target.m_Window.Insert(newindex, w);
                    m_WindowIndex = newindex;
                    m_CrossUV = m_AnchorUV;
                }
            }
            else if (m_State == State.WindowPointMove)
            {
                Undo.RecordObject(_target, "Move Window");

                Vector2 uv;
                if (RayIntersect(m_SelectSide, mouseray, out uv))
                {
                    uv = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));
                    RTConvexTowerOccluder.Window w = _target.m_Window[m_WindowIndex];
                    w.rect = Rect.MinMaxRect(Mathf.Min(m_CrossUV.x, uv.x), Mathf.Min(m_CrossUV.y, uv.y), Mathf.Max(m_CrossUV.x, uv.x), Mathf.Max(m_CrossUV.y, uv.y));
                }
            }
            else
            {
                SetState(State.Idle);
            }
        }

        if (type == EventType.MouseUp)
        {
            if (m_State == State.PointMove || m_State == State.LineMove)
            {
                Undo.RecordObject(_target, "Move Point");

                UpdateConvexPoints(_target);
                UpdateMesh();

                SetState(State.Idle);
                UpdateMesh();
            }
            else if (WindowEditMode())
            {
                if (m_State != State.OnSide)
                    SetState(State.Idle);

                if (m_State == State.WindowPointMove)
                {
                    SetState(State.OnSide);
                    m_WindowIndex = -1;
                }
            }
            else
            {
                SetState(State.Idle);
            }
        }
    }

    Vector2 m_AnchorUV;
    Vector2 m_CrossUV;

    int GetSelectSide(Vector2 mouseposition)
    {
        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;

        Vector2[] points = _target.m_Points.ToArray();
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(mouseposition);
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 pick;
            if (RayIntersect(i, mouseRay, out pick))
            {
                m_AnchorUV = pick;
                if (pick.x >= 0 && pick.x <= 1 && pick.y >= 0 && pick.y <= 1)
                    return i;
            }
        }
        return -1;
    }

    static void DrawDisc(Vector3 pos, float radius, Vector3 up, int segment = 12)
    {
        Vector3[] vtx = new Vector3[segment+1];
        Vector3 x = Vector3.Cross(Mathf.Abs(up.y) > 0.5f ? Vector3.right : Vector3.up, up);
        Vector3 z = Vector3.Cross(x, up);
        for (int i = 0; i <= segment; i++)
        {
            float rad = (i) * Mathf.PI * 2 / segment;
            vtx[i] = pos + x * (Mathf.Sin(rad) * radius) + z * (Mathf.Cos(rad) * radius);
        }
        Handles.DrawAAPolyLine(vtx);
    }

    static void DrawGrid(Camera camera, Vector3 position, Vector3 right, Vector3 up)
    {
        float scale = WorldSize(Screen.width, camera, position) / 20.0f;
        scale = Mathf.Pow(2, Mathf.Floor(Mathf.Log(scale, 2)));

        // 그리드
        for (int i = -10; i <= 10; i++)
        {
            Handles.color = i == 0 ? new Color(0.75f, 0.75f, 0.75f) : Color.gray;

            Vector3 v1 = position + i * scale * right;
            Handles.DrawLine(v1 - (11 * scale) * up, v1 + (11 * scale) * up);

            Vector3 v2 = position + i * scale * up;
            Handles.DrawLine(v2 - (11 * scale) * right, v2 + (11 * scale) * right);
        }
    }

#region GeomUtil
    static Vector3 IntersectPlane(Vector3 inNormal, Vector3 inPoint, Ray mouseRay)
    {
        Plane p = new Plane(inNormal, inPoint);
        float dstToDrawPlane = p.GetDistanceToPoint(mouseRay.origin);
        return mouseRay.origin + mouseRay.direction * (dstToDrawPlane / Vector3.Dot(-p.normal, mouseRay.direction));
    }

    static float WorldSize(float screensize, Camera camera, Vector3 p)
    {
        return (!camera.orthographic ? Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * camera.transform.InverseTransformPoint(p).z * 2.0f : camera.orthographicSize * 2.0f) * screensize / camera.pixelHeight;
    }

    static float DistanceRayToPoint(Vector3 p2, Ray ray)
    {
        Vector3 p = ray.origin + ray.direction * Vector3.Dot(p2 - ray.origin, ray.direction);
        return Vector3.Magnitude(p2 - p);
    }

    static float DistanceRayToLineSegment(Vector3 p0, Vector3 p1, Ray ray, out Vector3 on_line)
    {
        Vector3 v = p0 - p1;
        Vector3 p2 = p1 + v * (Vector3.Dot(ray.origin - p1, v) / Vector3.Dot(v, v));
        Vector3 va = p2 - ray.origin;
        Vector3 vb = ray.direction - v * (Vector3.Dot(ray.direction, v) / Vector3.Dot(v, v));
        Vector3 p = ray.origin + ray.direction * (Vector3.Dot(va, vb) / Vector3.Dot(vb, vb));
        on_line = p1 + (p0 - p1) * Mathf.Clamp(Vector3.Dot(p - p1, v) / Vector3.Dot(v, v), 0, 1);
        return Vector3.Magnitude(on_line - p);
    }

    static Bounds TransformBounds(Transform t, Bounds b)
    {
        Vector3 min = b.min;
        Vector3 max = b.max;
        Bounds bounds = new Bounds(t.InverseTransformPoint(min), Vector3.zero);
        for (int i = 1; i < 8; i++)
            bounds.Encapsulate(t.InverseTransformPoint(new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z)));
        return bounds;
    }

    static float kEpsilon = 0.000001f;
    // https://en.wikipedia.org/wiki/Möller–Trumbore_intersection_algorithm
    static float IntersectTest(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, float mint)
    {
        // edges from v1 & v2 to v0.     
        Vector3 e1 = v1 - v0;
        Vector3 e2 = v2 - v0;

        Vector3 h = Vector3.Cross(ray.direction, e2);
        float a = Vector3.Dot(e1, h);
        if ((a > -kEpsilon) && (a < kEpsilon))
            return mint;

        float f = 1.0f / a;
        Vector3 s = ray.origin - v0;
        float u = f * Vector3.Dot(s, h);
        if ((u < 0.0f) || (u > 1.0f))
            return mint;

        Vector3 q = Vector3.Cross(s, e1);
        float v = f * Vector3.Dot(ray.direction, q);
        if ((v < 0.0f) || (u + v > 1.0f))
            return mint;

        float t = f * Vector3.Dot(e2, q);
        if (t > kEpsilon && t < mint)
        {
            return t;
        }
        return mint;
    }
#endregion

    bool RayIntersect(int side, Ray mouseRay, out Vector2 uv)
    {
        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;
        Vector2[] points = _target.m_Points.ToArray();
        Vector3 p1 = _target.transform.TransformPoint(new Vector3(points[side].x, 0, points[side].y));
        Vector3 p2 = _target.transform.TransformPoint(new Vector3(points[(side + 1) % points.Length].x, 0, points[(side + 1) % points.Length].y));
        Vector3 p3 = p1 + _target.transform.TransformVector(Vector3.up * _target.m_Height);
        //Vector3 p4 = p2 + _target.transform.TransformVector(Vector3.up * _target.m_Height);

        Vector3 n = Vector3.Normalize(Vector3.Cross(p3 - p1, p2 - p1));

        if (Vector3.Dot(mouseRay.direction, n) < 0)
        {
            Vector3 p = IntersectPlane(n, p1, mouseRay);
            float u = Vector3.Dot(p - p1, p2 - p1) / Vector3.Dot(p2 - p1, p2 - p1);
            float v = Vector3.Dot(p - p1, p3 - p1) / Vector3.Dot(p3 - p1, p3 - p1);

            uv = new Vector2(u, v);
            //m_PickPortalTemp = uv;
            return true;
        }
        uv = Vector3.zero;
        return false;
    }

    void ResetBound(RTConvexTowerOccluder target)
    {
        MeshRenderer[] m = target.GetComponentsInChildren<MeshRenderer>();
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);

        if (m != null && m.Length > 0)
        {
            bounds = TransformBounds(target.transform, m[0].bounds);
            for (int i = 1; i < m.Length; i++)
                bounds.Encapsulate(TransformBounds(target.transform, m[i].bounds));
        }

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3 bottom = new Vector3((min.x + max.x) * 0.5f, 0, (min.z + max.z) * 0.5f);

        target.m_Height = max.y - min.y;
        target.m_Points.Clear();
        target.m_Points.AddRange(RT.Occluder.ConvexHull2d(new Vector2[] {
                new Vector2(min.x - bottom.x, min.z - bottom.z),
                new Vector2(min.x - bottom.x, max.z - bottom.z),
                new Vector2(max.x - bottom.x, max.z - bottom.z),
                new Vector2(max.x - bottom.x, min.z - bottom.z)}));
    }

    void UpdateConvex(Vector3[] points)
    {
        int[] polygons;
        int[] hull;
        int[] edge;

        Vector3[] vtx = RT.Occluder.EliminateDuplicatedVertices(points);

        RT.Occluder.ConvexHull3d(vtx, out polygons);
        RT.Occluder.GetVolumeEdgeAndHull(polygons, out hull, out edge);

        m_Vertex = vtx;
        m_Triangles = TriangleList(polygons);

        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;
        _target.m_HullTriangle = hull;
        _target.m_Edge = edge;
    }

    static public int[] TriangleList(int[] polygons)
    {
        List<int> triangles = new List<int>();
        for (int i1 = 0, i0 = 0; i1 < polygons.Length; i1++)
        {
            if (polygons[i1] == -1)
            {
                for (int i = i0; i < i1 - 2; i++)
                {
                    triangles.Add(polygons[i0]);
                    triangles.Add(polygons[i + 2]);
                    triangles.Add(polygons[i + 1]);
                }
                i0 = i1 + 1;
            }
        }
        return triangles.ToArray();
    }

    void UpdateTower(Vector2[] points, Vector3 up, int count)
    {
        Vector3[] v = new Vector3[points.Length];
        List<Vector3> vtx = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < v.Length; i++)
        {
            v[i] = new Vector3(points[i].x, 0, points[i].y);
            vtx.AddRange(new Vector3[] { v[i], v[i] + up });
        }

        for (int i = 2; i < v.Length; i++)
            triangles.AddRange(new int[] { 0, (i - 1) * 2, i * 2, 1, i * 2 + 1, (i - 1) * 2 + 1 });

        for (int i2 = 0, i1 = points.Length - 1; i2 < points.Length; i1 = i2++)
            triangles.AddRange(new int[] { i1 * 2, i1 * 2+1, i2 * 2 + 1, i1 * 2, i2 * 2 + 1, i2 * 2 });

        m_Vertex = vtx.ToArray();
        m_Triangles = triangles.ToArray();

        List<int> hull = new List<int>();
        for (int i = 0; i < count; i++)
            hull.AddRange(new int[] { i, i + count, (i + 1) % count });
        hull.AddRange(new int[] { 0, 1, 2 }); // bottom
        hull.AddRange(new int[] { count + 0, count + 2, count + 1 }); // top

        List<int> edge = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            edge.AddRange(new int[] { i, next, next, next + count }); // side
            edge.AddRange(new int[] { i, count, i, next }); // bottom
            edge.AddRange(new int[] { i, count + 1, i + count, next + count }); // top
        }

        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;
        _target.m_HullTriangle = hull.ToArray();
        _target.m_Edge = edge.ToArray();
    }

#region Mesh
    static class InternalMaterial
    {
        public static Material material;
        static InternalMaterial() { material = new Material(Shader.Find("Hidden/Internal-Occlusion")); }
    }

    void UpdateMesh()
    {
        RTConvexTowerOccluder _target = (RTConvexTowerOccluder)target;

        if (_target.m_ExtraPoints.Count > 0)
        {
            Vector3[] vtx = new Vector3[_target.m_Points.Count * 2 + _target.m_ExtraPoints.Count];
            for (int i = 0; i < _target.m_Points.Count; i++)
            {
                vtx[i + 0] = new Vector3(_target.m_Points[i].x, 0, _target.m_Points[i].y);
                vtx[i + _target.m_Points.Count] = vtx[i + 0] + Vector3.up * _target.m_Height;
            }
            for (int i = 0; i < _target.m_ExtraPoints.Count; i++)
                vtx[_target.m_Points.Count * 2 + i] = _target.m_ExtraPoints[i];

            UpdateConvex(vtx);
        }
        else
        {
            Vector2[] points = RT.Occluder.ConvexHull2d(_target.m_Points.ToArray());
            UpdateTower(points, _target.m_Height * Vector3.up, _target.m_Points.Count);
        }

        if (m_Mesh)
        {
            if (m_Mesh.vertexCount == m_Vertex.Length && m_Mesh.triangles.Length == m_Triangles.Length)
            {
                m_Mesh.vertices = m_Vertex;
                m_Mesh.triangles = m_Triangles;
                return;
            }
            DestroyImmediate(m_Mesh);
        }

        m_Mesh = CreateMesh(m_Vertex, m_Triangles);
    }

    static public void DrawMesh(Mesh mesh, Matrix4x4 tm, int pass, Color c, UnityEngine.Rendering.CompareFunction zfunc = UnityEngine.Rendering.CompareFunction.LessEqual)
    {
        if (mesh != null)
        {
            Material m = InternalMaterial.material;
            if (m != null)
            {
                m.SetColor("_Color", c);
                m.SetInt("_ZTest", (int)zfunc);
                m.SetPass(pass);
                Graphics.DrawMeshNow(mesh, tm);
            }
        }
    }

    static public Mesh CreateMesh(Vector3[] vtx, int[] triangles)
    {
        Mesh m = new Mesh();
        m.hideFlags = HideFlags.HideAndDontSave;
        m.vertices = vtx;
        m.triangles = triangles;
        return m;
    }
#endregion
}