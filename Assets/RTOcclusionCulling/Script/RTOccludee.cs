#define LOD_ENABLE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RTOccludee : MonoBehaviour
{
    public enum DeactiveType
    {
        MeshRendererDisable = 0,
        ShadowsCastOnly = 1,
        GameObjectDeactive = 2,
    };

    public DeactiveType m_DeactiveType = DeactiveType.MeshRendererDisable;

    public Renderer[] m_MeshRenderer = new Renderer[0]; // MeshRendererDisable, ShadowsCastOnly
    public GameObject[] m_GameObjects = null; // GameObjectDeactive
    public Bounds m_CustomBounds;

    public enum VolumeType
    {
        Static = 0,
        StaticSpatialPartitioning = 1,
        NonStatic = 2,
        UpdateBoundsAlways = 3,
    };

    public VolumeType m_VolumeType = VolumeType.NonStatic;

    public int m_SpatialDivisionDepth = 0;
    public bool m_StaticBatching = false;

    [Serializable]
    public class RenderMesh
    {
        public Mesh mesh;
        public Material[] materials;
        public int offset;
        public int length;
    }
#if LOD_ENABLE
    [Serializable]
    public class Lod
    {
        public bool preserveBorderEdges = true;
        public bool preserveUVSeamEdges = false;
        public bool preserveUVFoldoverEdges = false;
        public bool enableSmartLink = true;
        public double vertexLinkDistanceSqr = double.Epsilon;
        public int maxIterationCount = 100;
        public double agressiveness = 7.0;
        public float quality = 0.5f;

        public byte[] nodebyes;
        public int[] nodelength;
        public UnityEngine.Object lodasset;
        public RenderMesh[] lodmesh;
    };

    public Lod [] m_BakeLod = null;
    public float m_LodSwitchDistance = 0;
#endif
    RT.Occludee m_Occludee;

    private void Awake()
    {
        m_Occludee = new RT.Occludee();

        if (m_DeactiveType == DeactiveType.GameObjectDeactive)
        {
            m_Occludee.SetGameObject(m_CustomBounds, transform, m_GameObjects, m_VolumeType == VolumeType.Static ? true : false);
        }
        else
        {
            if (m_MeshRenderer == null || m_MeshRenderer.Length == 0)
            {
                MeshRenderer m = GetComponent<MeshRenderer>();
                if (m != null)
                    m_Occludee.SetMeshRenderer(new MeshRenderer[] { m }, transform, m_DeactiveType == DeactiveType.MeshRendererDisable ? true : false, m_SpatialDivisionDepth, m_StaticBatching, m_VolumeType == VolumeType.Static || m_VolumeType == VolumeType.StaticSpatialPartitioning ? true : false, m_VolumeType == VolumeType.UpdateBoundsAlways ? true : false);
            }
            else
            {
#if LOD_ENABLE
                if (m_BakeLod != null && m_BakeLod.Length > 0 && m_BakeLod[0].lodasset != null && m_BakeLod[0].lodmesh.Length > 0)
                {
                    if (IsValid(m_BakeLod[0].lodmesh))
                    {
                        if (m_Occludee.SetMeshRendererWithLOD(m_MeshRenderer, transform, m_DeactiveType == DeactiveType.MeshRendererDisable ? true : false, m_BakeLod[0], m_StaticBatching, m_LodSwitchDistance, m_VolumeType == VolumeType.Static || m_VolumeType == VolumeType.StaticSpatialPartitioning ? true : false) == true)
                            return;
                    }
                }
#endif
                m_Occludee.SetMeshRenderer(m_MeshRenderer, transform, m_DeactiveType == DeactiveType.MeshRendererDisable ? true : false, m_SpatialDivisionDepth, m_StaticBatching, m_VolumeType == VolumeType.Static || m_VolumeType == VolumeType.StaticSpatialPartitioning ? true : false, m_VolumeType == VolumeType.UpdateBoundsAlways ? true : false);
            }
        }
    }

    bool IsValid(RenderMesh[] list)
    {
        for (int i = 0; i < list.Length; i++)
            if (list[i].mesh == null)
                return false;
        return true;
    }

    private void OnEnable()
    {
        if (m_Occludee.Enable() == false)
        {
            Debug.Log("Ocludee setting error:" + name);
            enabled = false;
        }
    }

    private void OnDisable()
    {
        m_Occludee.Disable();
    }
}