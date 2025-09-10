using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RTConvexTowerOccluder : MonoBehaviour
{
    public List<Vector2> m_Points = new List<Vector2>();
    public float m_Height = 1;

    public List<Vector3> m_ExtraPoints = new List<Vector3>();

    [Serializable]
    public class Window
    {
        public int index;
        public Rect rect;
    };

    public List<Window> m_Window = new List<Window>();

    public bool m_Static = true;
    public bool m_UndergroundBottom = false;

    [HideInInspector] public int[] m_HullTriangle;
    [HideInInspector] public int[] m_Edge; // planeindex A, B, vertex A, B

    RT.Occluder m_ConvexOccluder = null;

    void Awake()
    {
        if (m_Points.Count < 3)
        {
            enabled = false;
            return;
        }

        Vector3[] vtx = new Vector3[m_Points.Count * 2 + m_ExtraPoints.Count];
        for (int i = 0; i < m_Points.Count; i++)
        {
            vtx[i] = new Vector3(m_Points[i].x, 0, m_Points[i].y);
            vtx[i + m_Points.Count] = vtx[i] + Vector3.up * m_Height;
        }
        for (int i = 0; i < m_ExtraPoints.Count; i++)
        {
            vtx[m_Points.Count * 2 + i] = m_ExtraPoints[i];
        }

        m_ConvexOccluder = new RT.Occluder();
        if (m_Window.Count > 0)
        {
            int[] sideidx = new int[m_Window.Count];
            Rect[] rect = new Rect[m_Window.Count];
            for (int i = 0; i < sideidx.Length; i++)
            {
                sideidx[i] = m_Window[i].index;
                rect[i] = m_Window[i].rect;
            }
            m_ConvexOccluder.SetTowerVolumeWithWindow(m_Points.ToArray(), Vector3.zero, m_Height, transform.localToWorldMatrix, sideidx, rect, m_UndergroundBottom);
        }
        else
            m_ConvexOccluder.SetVolume(vtx, m_HullTriangle, m_Edge, transform.localToWorldMatrix, m_UndergroundBottom);
        if (!m_Static)
            m_ConvexOccluder.SetTransform(transform);
    }

    void OnEnable()
    {
        if (m_ConvexOccluder != null)
            m_ConvexOccluder.Enable();
    }

    void OnDisable()
    {
        if (m_ConvexOccluder != null)
            m_ConvexOccluder.Disable();
    }

#if UNITY_EDITOR
    public void OnDrawGizmosSelected()
    {
        DrawOccluder();
    }

    void DrawOccluder()
    {
        if (m_Points.Count > 0 && (Application.isPlaying || Selection.gameObjects.Length > 1) && m_Edge != null)
        {
            Vector3[] vtx = new Vector3[m_Points.Count * 2 + m_ExtraPoints.Count];
            Vector3 up = transform.localToWorldMatrix.MultiplyVector(Vector3.up * m_Height);
            for (int i = 0; i < m_Points.Count; i++)
            {
                vtx[i] = transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(m_Points[i].x, 0, m_Points[i].y));
                vtx[i + m_Points.Count] = vtx[i] + up;
            }
            for (int i = 0; i < m_ExtraPoints.Count; i++)
            {
                vtx[m_Points.Count * 2 + i] = transform.localToWorldMatrix.MultiplyPoint3x4(m_ExtraPoints[i]);
            }

            Handles.color = Color.white;
            for (int i = 0; i < m_Edge.Length; i += 4)
                Handles.DrawAAPolyLine(vtx[m_Edge[i + 2]], vtx[m_Edge[i + 3]]);
        }
    }
#endif
}