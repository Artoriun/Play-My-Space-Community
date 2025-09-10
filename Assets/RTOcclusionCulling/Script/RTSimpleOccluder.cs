using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RTSimpleOccluder : MonoBehaviour
{
    public enum Type {
        FromCollider,
        FromMeshFilter,
        SimpleBox,
        SimpleCylinder,
        FromCustomMesh
    }

    public Type m_Type = Type.SimpleBox;

    [HideInInspector] public Vector3[] m_ConvexHullVertices;
    [HideInInspector] public int[] m_ConvexHullTriangles;
    [HideInInspector] public int[] m_ConvexHullEdge;

    public Vector3 m_Center = Vector3.zero; // box, cylinder
    public Vector3 m_Size = Vector3.one; // box
    public Vector3 m_Offset = Vector3.zero;

    [Range(4, 10)] public int m_Segment = 6;

    public float m_Height = 1.0f; // cylinder
    public float m_Radius = 0.5f; // cylinder

    public Mesh m_CustomMesh;

    [Range(4, 32)] public int m_LimitConvexVertex = 16;
    [Range(0, 0.2f)] public float m_InnerDepth = 0.01f;

    RT.Occluder m_Occluder = null;

    void Awake()
    {
        if (m_Type == Type.FromCollider)
        {
            BoxCollider boxcollider = GetComponent<BoxCollider>();
            if (boxcollider != null)
            {
                SetBoxOccluder(boxcollider.center, boxcollider.size);
                return;
            }

            CapsuleCollider capsulecollider = GetComponent<CapsuleCollider>();
            if (capsulecollider != null)
            {
                SetCylinder(capsulecollider.center, capsulecollider.radius, capsulecollider.height - capsulecollider.radius * 2, m_Segment);
                return;
            }
        }
        if (m_Type == Type.SimpleBox)
        {
            SetBoxOccluder(m_Center, m_Size);
            return;
        }
        if (m_Type == Type.SimpleCylinder)
        {
            SetCylinder(m_Center, m_Radius, m_Height, m_Segment);
            return;
        }

        if (m_ConvexHullVertices != null && m_ConvexHullVertices.Length > 0)
        {
            m_Occluder = new RT.Occluder();
            m_Occluder.SetVolume(m_ConvexHullVertices, m_ConvexHullTriangles, m_ConvexHullEdge, transform.localToWorldMatrix);
            m_Occluder.SetTransform(transform);
        }
    }

    void SetBoxOccluder(Vector3 center, Vector3 size)
    {
        m_Occluder = new RT.Occluder();
        m_Occluder.SetBoxVolume(new Bounds(center, size), transform.localToWorldMatrix);
        m_Occluder.SetTransform(transform);
    }

    void SetCylinder(Vector3 center, float radius, float height, int segment)
    {
        Vector2[] vtx = new Vector2[segment];
        for (int i = 0; i < segment; i++)
        {
            float rad = i * Mathf.PI * 2 / m_Segment;
            vtx[i] = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }
        m_Occluder = new RT.Occluder();
        m_Occluder.SetTowerVolume(vtx, center - Vector3.up * height * 0.5f, height, transform.localToWorldMatrix);
        m_Occluder.SetTransform(transform);
    }

    void OnEnable()
    {
        if (m_Occluder != null)
            m_Occluder.Enable();
    }

    void OnDisable()
    {
        if (m_Occluder != null)
            m_Occluder.Disable();
    }
}