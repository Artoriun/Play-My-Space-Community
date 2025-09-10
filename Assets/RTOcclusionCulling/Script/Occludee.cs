#define LOD_ENABLE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RT
{
    public class Occludee
    {
        public bool SetMeshRenderer(Renderer[] renderer, Transform transform, bool hideall, int spatialmanage_depth, bool staticbatch, bool statictransform, bool updatebounds)
        {
            if (renderer == null || renderer.Length == 0)
                return false;

            m_MeshRenderer = renderer;
            m_Transform = transform;
            m_DeactiveType = hideall ? DeactiveType.MeshRendererDisable : DeactiveType.ShadowsCastOnly;
            m_StaticBatching = !updatebounds && staticbatch ? true : false;

            if (spatialmanage_depth > 0 && m_MeshRenderer.Length > 0)
            {
                m_VolumeType = VolumeType.StaticSpatialPartitioning;
                BuildSpatialMeshGroup(spatialmanage_depth);
            }
            else
            {
                m_VolumeType = statictransform ? VolumeType.Static : (updatebounds ? VolumeType.UpdateBoundsAlways : VolumeType.NonStatic);
            }

            return true;
        }
#if LOD_ENABLE
        public bool SetMeshRendererWithLOD(Renderer[] renderer, Transform transform, bool hideall, RTOccludee.Lod lod, bool staticbatch, float switchdistance, bool statictransform)
        {
            if (renderer == null || renderer.Length == 0)
                return false;

            m_MeshRenderer = renderer;
            m_Transform = transform;
            m_DeactiveType = hideall ? DeactiveType.MeshRendererDisable : DeactiveType.ShadowsCastOnly;
            m_StaticBatching = staticbatch;
            m_LODSwitchDistance = switchdistance;

            if (lod.nodebyes == null || lod.nodebyes.Length == 0)
            {
                m_VolumeType = statictransform ? VolumeType.Static : VolumeType.NonStatic;

                List<Renderer> collect = new List<Renderer>();
                for (int i=0; i < lod.lodmesh.Length; i++)
                {
                    GameObject o = new GameObject();
                    o.name = "#" + lod.lodmesh[i].mesh.name;
                    o.transform.SetParent(transform);
                    o.transform.localRotation = Quaternion.identity;
                    o.transform.localPosition = Vector3.zero;

                    MeshFilter meshfilter = o.AddComponent<MeshFilter>();
                    meshfilter.sharedMesh = lod.lodmesh[i].mesh;
                    MeshRenderer meshrenderer = o.AddComponent<MeshRenderer>();
                    meshrenderer.sharedMaterials = lod.lodmesh[i].materials;
                    meshrenderer.enabled = false;
                    collect.Add(meshrenderer);
                }

                Build();

                m_Node = new KdNode();
                m_Node.lodmeshgroup = CreateMeshGroup(collect.ToArray(), transform, false);;
                m_Node.offset = 0;
                m_Node.length = m_MeshGroup.Length;
                m_Node.state = KdNode.State.all_visible;
            }
            else
            {
                m_VolumeType = VolumeType.StaticSpatialPartitioning;

                int count = 0;
                for (int i = 0; i < lod.nodelength.Length; i++)
                    count += lod.nodelength[i];
                if (count != m_MeshRenderer.Length)
                    return false;

                RestoreNode(lod.nodebyes, lod.nodelength, m_MeshRenderer, lod.lodmesh, staticbatch, transform);
            }
            return true;
        }
#endif
        public bool SetGameObject(Bounds bounds, Transform transform, GameObject[] o, bool statictransform)
        {
            m_GameObjects = o;
            m_CustomBounds = bounds;
            m_Transform = transform;
            m_DeactiveType = DeactiveType.GameObjectDeactive;
            m_VolumeType = statictransform ? VolumeType.Static : VolumeType.NonStatic;
            return true;
        }

        enum DeactiveType
        {
            MeshRendererDisable = 0,
            ShadowsCastOnly = 1,
            GameObjectDeactive = 2,
        };

        DeactiveType m_DeactiveType = DeactiveType.MeshRendererDisable;

        Renderer[] m_MeshRenderer;
        GameObject[] m_GameObjects;
        Bounds m_CustomBounds;
#if LOD_ENABLE
        float m_LODSwitchDistance = 0;
#endif
        Transform m_Transform;

        enum VolumeType
        {
            Static = 0,
            StaticSpatialPartitioning = 1,
            NonStatic = 2,
            UpdateBoundsAlways = 3,
        };

        VolumeType m_VolumeType = VolumeType.Static;

        bool m_StaticBatching = false;

#if UNITY_EDITOR
        public class MeshGroup
#else
        class MeshGroup
#endif
        {
            public Renderer[] meshrenderer;
            public float[] boundselement = new float[6];

            public float visibletime = 0.0f;
            public bool deactive = false;
        };

        MeshGroup[] m_MeshGroup = null;

        float[] m_BoundsElement = new float[6];
        Vector3 m_LocalBoundsCenter;
        float m_LocalBoundsSize;

        public bool Enable()
        {
            if ((m_DeactiveType != DeactiveType.GameObjectDeactive && (m_MeshRenderer == null || m_MeshRenderer.Length == 0)) ||
                (m_DeactiveType == DeactiveType.GameObjectDeactive && (m_GameObjects == null || m_GameObjects.Length == 0)))
            {
                return false;
            }

            if (m_MeshGroup == null)
            {
                if (m_DeactiveType != DeactiveType.GameObjectDeactive)
                {
                    if (Build() == false)
                        return false;
                }
                else
                {
                    CalculateBounds(m_BoundsElement, m_CustomBounds, m_Transform);

                    MeshGroup g = new MeshGroup();
                    g.meshrenderer = null;
                    SetBounds(g.boundselement, m_BoundsElement);
                    m_MeshGroup = new MeshGroup[] { g };
                }

                if (m_VolumeType == VolumeType.NonStatic)
                {
                    m_LocalBoundsCenter = m_Transform.InverseTransformPoint(new Vector3((m_BoundsElement[0] + m_BoundsElement[1]) * 0.5f, (m_BoundsElement[2] + m_BoundsElement[3]) * 0.5f, (m_BoundsElement[4] + m_BoundsElement[5]) * 0.5f));
                    m_LocalBoundsSize = Vector3.Magnitude(new Vector3(m_BoundsElement[1] - m_BoundsElement[0], m_BoundsElement[3] - m_BoundsElement[2], m_BoundsElement[5] - m_BoundsElement[4])) * 0.5f;
                }
            }

            RTOcclusionCulling.AddOccludee(this);

            if (m_StaticBatching)
                TrunBakeMesh(true);
            return true;
        }

        public bool Disable()
        {
            if ((m_DeactiveType != DeactiveType.GameObjectDeactive && (m_MeshRenderer == null || m_MeshRenderer.Length == 0)) ||
                (m_DeactiveType == DeactiveType.GameObjectDeactive && (m_GameObjects == null || m_GameObjects.Length == 0)))
            {
                return false;
            }

            RTOcclusionCulling.RemoveOccludee(this);
#if LOD_ENABLE
            if (m_Node != null)
                ResetLodMeshGroup(m_Node);
#endif
            ResetVisible();

            if (m_StaticBatching)
                TrunBakeMesh(false);
            return true;
        }

        bool Build()
        {
            m_MeshRenderer = ExcludeStaticMesh(m_MeshRenderer);

            if (m_MeshRenderer.Length == 0)
                return false;

            m_MeshGroup = new MeshGroup[] { CreateMeshGroup(m_MeshRenderer, m_Transform, m_StaticBatching) };

            SetBounds(m_BoundsElement, m_MeshGroup[0].boundselement);
            for (int i = 1; i < m_MeshGroup.Length; i++)
                Encapsulate(m_BoundsElement, m_MeshGroup[i].boundselement);
            return true;
        }

        bool BuildSpatialMeshGroup(int depth)
        {
            List<MeshGroup> group = new List<MeshGroup>();
            m_Node = SpatialDivision(m_MeshRenderer, depth, group, m_Transform, m_Transform.gameObject.name, m_StaticBatching);
            m_MeshGroup = group.ToArray();
            SetBounds(m_BoundsElement, m_Node.boundselement);
            return true;
        }

        void TrunBakeMesh(bool on)
        {
            if (on)
            {
                EnableMeshRenderer(m_MeshRenderer, false);
                for (int i = 0; i < m_MeshGroup.Length; i++)
                    EnableMeshRenderer(m_MeshGroup[i].meshrenderer, on);
            }
            else
            {
                for (int i = 0; i < m_MeshGroup.Length; i++)
                    EnableMeshRenderer(m_MeshGroup[i].meshrenderer, on);
                EnableMeshRenderer(m_MeshRenderer, true);
            }
        }

        public void ResetVisible()
        {
            if (m_DeactiveCount == 0)
                return ;

            for (int i = 0; i < m_MeshGroup.Length; i++)
            {
                if (m_MeshGroup[i].deactive == true)
                    Deactive(m_MeshGroup[i], false);
            }
        }

        float m_FrustumDelay = 0;

        void UpdateBounds()
        {
            if (m_VolumeType == VolumeType.NonStatic)
            {
                Vector3 center = m_Transform.TransformPoint(m_LocalBoundsCenter);
                m_BoundsElement[0] = center.x - m_LocalBoundsSize;
                m_BoundsElement[1] = center.x + m_LocalBoundsSize;
                m_BoundsElement[2] = center.y - m_LocalBoundsSize;
                m_BoundsElement[3] = center.y + m_LocalBoundsSize;
                m_BoundsElement[4] = center.z - m_LocalBoundsSize;
                m_BoundsElement[5] = center.z + m_LocalBoundsSize;
            }
            else if (m_VolumeType == VolumeType.UpdateBoundsAlways)
            {
                for (int i = 0; i < m_MeshGroup.Length; i++)
                    SetBounds(m_MeshGroup[i].boundselement, GetBounds(m_MeshGroup[i].meshrenderer));

                SetBounds(m_BoundsElement, m_MeshGroup[0].boundselement);
                for (int i = 1; i < m_MeshGroup.Length; i++)
                    Encapsulate(m_BoundsElement, m_MeshGroup[i].boundselement);
            }
        }

        public void UpdateOcclusion(RTOcclusionCulling occ, float[] frustumelement, float[] viewboundselement, float culldelay)
        {
            if (m_FrustumDelay != 0 && Time.realtimeSinceStartup < m_FrustumDelay)
                return;

            if (m_VolumeType == VolumeType.UpdateBoundsAlways || m_VolumeType == VolumeType.NonStatic)
            {
                UpdateBounds();
            }

            if (m_Node != null)
            {
                bool infrustum = UpdateOcclusionRecur(m_Node, m_BoundsElement, occ, frustumelement, viewboundselement, culldelay);
                m_FrustumDelay = infrustum == false && m_DeactiveCount == 0 ? Time.realtimeSinceStartup + culldelay : 0;
            }
            else
            {
                bool infrustum = IntersectsBounds(viewboundselement, m_BoundsElement) && TestPlanesAABB(frustumelement, m_BoundsElement);
                m_FrustumDelay = infrustum == false && m_DeactiveCount == 0 ? Time.realtimeSinceStartup + culldelay : 0;

                if (infrustum)
                {
                    if (m_DeactiveCount == 0 && !occ.IsOccludeeeInDistance(m_BoundsElement))
                        return;

                    for (int i = 0; i < m_MeshGroup.Length; i++)
                        UpdateOcclusionMeshGroup(m_MeshGroup[i], occ, frustumelement, viewboundselement, culldelay);
                }
            }
        }

#if UNITY_EDITOR
        public class KdNode
#else
        class KdNode
#endif
        {
            public KdNode left, right;
            public int offset, length;
            public float[] boundselement = new float[6];
#if LOD_ENABLE
            public MeshGroup lodmeshgroup = null;
#endif
            public enum State
            {
                all_visible = 0,
                all_occluded = 1,
                idle = 2,
                lod_enabled = 3,
                lod_occluded = 4,
            };
            public State state;
        };

        KdNode m_Node = null;

        bool UpdateOcclusionRecur(KdNode node, float[] boundselement, RTOcclusionCulling occ, float[] frustumelement, float[] viewboundselement, float culldelay)
        {
            if (IntersectsBounds(viewboundselement, boundselement) && TestPlanesAABB(frustumelement, boundselement))
            {
#if LOD_ENABLE
                if (node.lodmeshgroup != null)
                {
                    if (node.state != KdNode.State.lod_enabled && node.state != KdNode.State.lod_occluded)
                    {
                        if (occ.CheckLodDistance(boundselement, true, m_LODSwitchDistance))
                            EnableLodMeshGroup(node, true);
                    }
                    else if (!occ.CheckLodDistance(boundselement, false, m_LODSwitchDistance))
                    {
                        EnableLodMeshGroup(node, false);
                    }

                    if (node.state == KdNode.State.lod_enabled || node.state == KdNode.State.lod_occluded)
                    {
                        if (node.state == KdNode.State.all_visible && !occ.IsOccludeeeInDistance(boundselement))
                            return true;

                        node.state = UpdateOcclusionMeshGroup(node.lodmeshgroup, occ, frustumelement, viewboundselement, culldelay) ? KdNode.State.lod_occluded : KdNode.State.lod_enabled;
                        return true;
                    }
                }
#endif
                if (node.state == KdNode.State.all_occluded)
                {
                    if (occ.CullTest(boundselement))
                        return true;
                }

                if (node.state == KdNode.State.all_visible && !occ.IsOccludeeeInDistance(boundselement))
                    return true;

                if (node.left != null)
                {
                    UpdateOcclusionRecur(node.left, node.left.boundselement, occ, frustumelement, viewboundselement, culldelay);
                    UpdateOcclusionRecur(node.right, node.right.boundselement, occ, frustumelement, viewboundselement, culldelay);

                    KdNode.State lstate = node.left.state >= KdNode.State.lod_enabled ? (KdNode.State)(node.left.state - KdNode.State.lod_enabled) : node.left.state;
                    KdNode.State rstate = node.right.state >= KdNode.State.lod_enabled ? (KdNode.State)(node.right.state - KdNode.State.lod_enabled) : node.right.state;

                    node.state = lstate == rstate ? lstate : KdNode.State.idle;
                }
                else
                {
                    int occluded = 0;
                    for (int i = 0; i < node.length; i++)
                        occluded += UpdateOcclusionMeshGroup(m_MeshGroup[node.offset + i], occ, frustumelement, viewboundselement, culldelay) ? 1 : 0;
                    node.state = occluded == node.length ? KdNode.State.all_occluded : (occluded == 0 ? KdNode.State.all_visible : KdNode.State.idle);
                }
                return true;
            }
            return false;
        }

        bool UpdateOcclusionMeshGroup(MeshGroup g, RTOcclusionCulling occ, float[] frustumelement, float[] viewboundselement, float culldelay)
        {
            if (g.deactive == false && g.visibletime != 0 && Time.realtimeSinceStartup - g.visibletime < culldelay)
                return false;

            float[] boundselement = m_DeactiveType == DeactiveType.GameObjectDeactive || m_VolumeType == VolumeType.NonStatic ? m_BoundsElement : g.boundselement;

            if (IntersectsBounds(viewboundselement, boundselement) && TestPlanesAABB(frustumelement, boundselement))
            {
                if (occ.CullTest(boundselement))
                {
                    if (g.deactive == false)
                        Deactive(g, true);
                    return true;
                }

                if (g.deactive == true)
                    Deactive(g, false);
            }

            m_Seq = (m_Seq + 1.0f / 61.0f) % 1.0f; // irregular
            g.visibletime = Time.realtimeSinceStartup + (culldelay > 0 ? culldelay * 0.25f * m_Seq : 0);
            return g.deactive;
        }

        static float m_Seq = 0.0f;

        int m_DeactiveCount = 0;

        void Deactive(MeshGroup g, bool deactive)
        {
            if (g.deactive == deactive)
                return;
            m_DeactiveCount += deactive ? 1 : -1;

            g.deactive = deactive;
            g.visibletime = 0;

            switch (m_DeactiveType)
            {
                case DeactiveType.MeshRendererDisable:
                    for (int i = 0; i < g.meshrenderer.Length; i++)
                    {
                        if (g.meshrenderer[i] != null)
                            g.meshrenderer[i].enabled = deactive == true ? false : true;
                    }
                    break;
                case DeactiveType.ShadowsCastOnly:
                    for (int i = 0; i < g.meshrenderer.Length; i++)
                    {
                        if (g.meshrenderer[i] != null)
                            g.meshrenderer[i].shadowCastingMode = deactive == true ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
                    }
                    break;
                case DeactiveType.GameObjectDeactive:
                    for (int i = 0; i < m_GameObjects.Length; i++)
                    {
                        if (m_GameObjects[i] != null)
                            m_GameObjects[i].SetActive(deactive ? false : true);
                    }
                    break;
            }
        }
#if LOD_ENABLE
        void EnableLodMeshGroup(KdNode node, bool enable)
        {
            node.state = enable ? KdNode.State.lod_enabled : KdNode.State.all_visible;

            EnableMeshRenderer(node.lodmeshgroup.meshrenderer, enable);
            for (int i = 0, offset=node.offset; i < node.length; i++, offset++)
            {
                if (m_MeshGroup[offset].deactive == true)
                    Deactive(m_MeshGroup[offset], false);
                EnableMeshRenderer(m_MeshGroup[offset].meshrenderer, !enable);
            }
        }

        void ResetLodMeshGroup(KdNode node)
        {
            if (node == null)
                return;

            if (node.state == KdNode.State.lod_enabled || node.state == KdNode.State.lod_occluded)
                EnableLodMeshGroup(node, false);

            if (node.left != null)
            {
                ResetLodMeshGroup(node.left);
                ResetLodMeshGroup(node.right);
            }
        }
#endif
#region util
        static void EnableMeshRenderer(Renderer[] renderer, bool enable)
        {
            for (int i = 0; i < renderer.Length; i++)
            {
                if (renderer[i] != null)
                    renderer[i].enabled = enable;
            }
        }

        public static Renderer[] ExcludeStaticMesh(Renderer[] renderer)
        {
            List<Renderer> list = null;
            for (int i = 0; i < renderer.Length; i++)
            {
                if (renderer[i] == null || renderer[i].isPartOfStaticBatch)
                {
                    if (list == null)
                    {
                        list = new List<Renderer>(renderer.Length);
                        for (int j = 0; j < i; j++)
                            list.Add(renderer[j]);
                    }
                }
                else if (list != null)
                    list.Add(renderer[i]);
            }
            return list != null ? list.ToArray() : renderer;
        }

        static Bounds GetBounds(Renderer[] items)
        {
            Bounds b = items[0].bounds;
            for (int i = 1; i < items.Length; i++)
                b.Encapsulate(items[i].bounds);
            return b;
        }

#endregion
#region bounds element
        static void SetBounds(float[] boundselement, Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            boundselement[0] = min.x;
            boundselement[1] = max.x;
            boundselement[2] = min.y;
            boundselement[3] = max.y;
            boundselement[4] = min.z;
            boundselement[5] = max.z;
        }

        static void SetBounds(float[] boundselement, float[] boundselement2)
        {
            for (int i = 0; i < 6; i++)
                boundselement[i] = boundselement2[i];
        }

        static void SetBounds(float[] boundselement, Vector3[] vertex)
        {
            Vector3 v = vertex[0];
            boundselement[0] = boundselement[1] = v.x;
            boundselement[2] = boundselement[3] = v.y;
            boundselement[4] = boundselement[5] = v.z;
            for (int i = 1; i < vertex.Length; i++)
            {
                v = vertex[i];
                if (v.x < boundselement[0]) boundselement[0] = v.x;
                if (v.x > boundselement[1]) boundselement[1] = v.x;
                if (v.y < boundselement[2]) boundselement[2] = v.y;
                if (v.y > boundselement[3]) boundselement[3] = v.y;
                if (v.z < boundselement[4]) boundselement[4] = v.z;
                if (v.z > boundselement[5]) boundselement[5] = v.z;
            }
        }

        static void Encapsulate(float[] boundselement, float[] boundselement2)
        {
            for (int i = 0; i < 6; i += 2)
            {
                if (boundselement2[i] < boundselement[i])
                    boundselement[i] = boundselement2[i];
                if (boundselement2[i+1] > boundselement[i+1])
                    boundselement[i+1] = boundselement2[i+1];
            }
        }

        static void CalculateBounds(float[] resultelement, Bounds bounds, Transform transform)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Matrix4x4 localToWorld = transform.localToWorldMatrix;

            Vector3 v = localToWorld.MultiplyPoint3x4(min);
            resultelement[0] = resultelement[1] = v.x;
            resultelement[2] = resultelement[3] = v.y;
            resultelement[4] = resultelement[5] = v.z;

            for (int i = 1; i < 8; i++)
            {
                v = localToWorld.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z));
                if (v.x < resultelement[0]) resultelement[0] = v.x;
                if (v.x > resultelement[1]) resultelement[1] = v.x;
                if (v.y < resultelement[2]) resultelement[2] = v.y;
                if (v.y > resultelement[3]) resultelement[3] = v.y;
                if (v.z < resultelement[4]) resultelement[4] = v.z;
                if (v.z > resultelement[5]) resultelement[5] = v.z;
            }
        }

        static bool IntersectsBounds(float[] b1, float[] b2)
        {
            return b1[1] < b2[0] || b1[0] > b2[1] || b1[3] < b2[2] || b1[2] > b2[3] || b1[5] < b2[4] || b1[4] > b2[5] ? false : true;
        }

        static public bool TestPlanesAABB(float[] frustumelement, float[] boundelement)
        {
            for (int i = 0, off = 0; i < 6; i++, off += 4)
            {
                float d = boundelement[frustumelement[off + 0] > 0 ? 1 : 0] * frustumelement[off + 0] +
                    boundelement[frustumelement[off + 1] > 0 ? 3 : 2] * frustumelement[off + 1] +
                    boundelement[frustumelement[off + 2] > 0 ? 5 : 4] * frustumelement[off + 2] +
                    frustumelement[off + 3];
                if (d < 0)
                    return false;
            }
            return true;
        }
#endregion

#region spatial
#if LOD_ENABLE
        void RestoreNode(byte[] nodebytes, int[] nodelen, Renderer[] renderer, RTOccludee.RenderMesh[] lodmesh, bool staticbatch, Transform parent)
        {
            List<MeshGroup> group = new List<MeshGroup>();
            int off = 0;
            int count = 0;

            m_Node = DecodeRecur(nodebytes, ref off, ref count, lodmesh, parent, nodelen, renderer, group, staticbatch);
            m_MeshGroup = group.ToArray();

            SetBounds(m_BoundsElement, m_MeshGroup[0].boundselement);
            for (int i = 1; i < m_MeshGroup.Length; i++)
                Encapsulate(m_BoundsElement, m_MeshGroup[i].boundselement);
        }

        static KdNode DecodeRecur(byte[] nodebytes, ref int off, ref int count, RTOccludee.RenderMesh[] lodmesh, Transform parent, int[] nodelen, Renderer[] renderer, List<MeshGroup> group, bool staticbatch)
        {
            KdNode node = new KdNode();
            int mask = nodebytes[off++];
            if (mask == 0)
            {
                List<Renderer> m = new List<Renderer>();
                int rendereroff = 0;
                for (int i = 0; i < count; i++)
                    rendereroff += nodelen[i];
                for(int i=0; i<nodelen[count]; i++)
                    m.Add(renderer[rendereroff + i]);
                MeshGroup meshgroup = CreateMeshGroup(m.ToArray(), parent, staticbatch);
                group.Add(meshgroup);

                node.offset = count++;
                node.left = null;
                node.right = null;
                SetBounds(node.boundselement, meshgroup.boundselement);
                node.length = 1;
                node.state = KdNode.State.all_visible;
            }
            else
            {
                GameObject l = new GameObject("node");
                l.transform.SetParent(parent);
                l.transform.localRotation = Quaternion.identity;
                l.transform.localPosition = Vector3.zero;

                GameObject r = new GameObject("node");
                r.transform.SetParent(parent);
                r.transform.localRotation = Quaternion.identity;
                r.transform.localPosition = Vector3.zero;

                node.offset = count;
                node.left = DecodeRecur(nodebytes, ref off, ref count, lodmesh, l.transform, nodelen, renderer, group, staticbatch);
                node.right = DecodeRecur(nodebytes, ref off, ref count, lodmesh, r.transform, nodelen, renderer, group, staticbatch);
                node.length = count - node.offset;
                node.state = KdNode.State.all_visible;

                SetBounds(node.boundselement, node.left.boundselement);
                Encapsulate(node.boundselement, node.right.boundselement);
            }

            if (lodmesh != null)
            {
                for (int i = 0; i < lodmesh.Length; i++)
                {
                    if (node.offset == lodmesh[i].offset && node.length == lodmesh[i].length)
                    {
                        List<Renderer> collect = new List<Renderer>();
                        for (int j = i; j < lodmesh.Length && node.offset == lodmesh[j].offset && node.length == lodmesh[j].length; j++)
                        {
                            GameObject o = new GameObject();
                            o.name = "#" + lodmesh[j].mesh.name;
                            o.transform.SetParent(parent);
                            MeshFilter meshfilter = o.AddComponent<MeshFilter>();
                            meshfilter.sharedMesh = lodmesh[j].mesh;
                            MeshRenderer meshrenderer = o.AddComponent<MeshRenderer>();
                            meshrenderer.sharedMaterials = lodmesh[j].materials;
                            meshrenderer.enabled = false;
                            collect.Add(meshrenderer);
                        }
                        MeshGroup g = CreateMeshGroup(collect.ToArray(), parent, false);
                        //EnableMeshRenderer(g.meshrenderer, false);
                        node.lodmeshgroup = g;
                        node.state = KdNode.State.all_visible;
                        return node;
                    }
                }
            }
            return node;
        }
#endif
#if UNITY_EDITOR
        static public KdNode SpatialDivision(Renderer[] m, int depth)
        {
            List<MeshGroup> meshgroup = new List<MeshGroup>();
            return SpatialDivision(m, depth, meshgroup, null, "", false);
        }

        static public KdNode SpatialDivision(Renderer[] m, int depth, List<MeshGroup> group, Transform parent, string name, bool bakeable, int axismask = 7)
#else
        static KdNode SpatialDivision(Renderer[] m, int depth, List<MeshGroup> group, Transform parent, string name, bool bakeable, int axismask = 7)
#endif
        {
            if (depth <= 0)
            {
                KdNode node = new KdNode();
                node.offset = group.Count;

                MeshGroup g = CreateMeshGroup(m, parent, bakeable);
                group.Add(g);

                node.left = node.right = null;
                node.length = 1;
                SetBounds(node.boundselement, g.boundselement);
                node.state = KdNode.State.all_visible;
                return node;
            }

            Bounds b = GetBounds(m);
            Vector3 axis = ((axismask & 1) != 0 && b.size.x > b.size.z) || (axismask & 2) == 0 ? Vector3.right : Vector3.forward;
            if ((axismask & 4) != 0 && b.size.y > b.size.x && b.size.y > b.size.z)
                axis = Vector3.up;

            List<Renderer> list = new List<Renderer>(m.Length);
            float center = Vector3.Dot(b.center, axis);
            for (int i = 0; i < m.Length; i++)
            {
                if (Vector3.Dot(m[i].bounds.max, axis) > center)
                    list.Add(m[i]);
            }

            if (list.Count == 0 || list.Count == m.Length)
            {
                if (axismask == 7)
                    return SpatialDivision(m, depth, group, parent, name, bakeable, axis.x > 0 ? 2 : 1);

                KdNode node = new KdNode();
                node.offset = group.Count;

                group.Add(CreateMeshGroup(m, parent, bakeable));

                SetBounds(node.boundselement, b);
                node.left = node.right = null;
                node.length = 1;
                node.state = KdNode.State.all_visible;
                return node;
            }
            else
            {
                KdNode node = new KdNode();
                node.offset = group.Count;

                if (parent != null && bakeable == true)
                {
                    GameObject o = new GameObject(name + "/" + list.Count);
                    o.transform.SetParent(parent);
                    o.transform.localRotation = Quaternion.identity;
                    o.transform.localPosition = Vector3.zero;

                    node.left = SpatialDivision(list.ToArray(), depth - 1, group, o.transform, name, bakeable);
                }
                else
                {
                    node.left = SpatialDivision(list.ToArray(), depth - 1, group, parent, name, bakeable);
                }

                List<Renderer> r = new List<Renderer>();
                for (int i = 0, j = 0; i < m.Length; i++)
                {
                    if (j < list.Count && m[i] == list[j])
                        j++;
                    else
                        r.Add(m[i]);
                }

                if (parent != null && bakeable == true)
                {
                    GameObject o = new GameObject(name + "/" + r.Count);
                    o.transform.SetParent(parent);
                    o.transform.localRotation = Quaternion.identity;
                    o.transform.localPosition = Vector3.zero;

                    node.right = SpatialDivision(r.ToArray(), depth - 1, group, o.transform, name, bakeable);
                }
                else
                {
                    node.right = SpatialDivision(r.ToArray(), depth - 1, group, parent, name, bakeable);
                }

                SetBounds(node.boundselement, b);
                node.length = group.Count - node.offset;
                node.state = KdNode.State.all_visible;
                return node;
            }
        }
#endregion

#region meshgroup
        static MeshGroup CreateMeshGroup(Renderer[] items, Transform parent, bool bakeable)
        {
            MeshGroup g = new MeshGroup();
            g.meshrenderer = bakeable && items.Length > 1 ? CreateMergedMesh(parent, items) : items;
            SetBounds(g.boundselement, GetBounds(items));
            return g;
        }

        static Renderer[] SortByMaterial(Renderer[] renderer)
        {
            List<Material> mat = new List<Material>();
            List<Renderer> list = new List<Renderer>();
            for (int i = 0; i < renderer.Length; i++)
            {
                if (mat.IndexOf(renderer[i].sharedMaterial) == -1)
                    mat.Add(renderer[i].sharedMaterial);
                list.Add(renderer[i]);
            }
            list.Sort((Renderer r1, Renderer r2) => { return mat.IndexOf(r1.sharedMaterial) - mat.IndexOf(r2.sharedMaterial);});
            return list.ToArray();
        }

        static Renderer[] SortByAxis(Renderer[] renderer)
        {
            if (renderer.Length == 0)
                return renderer;

            Bounds bounds = renderer[0].bounds;
            for (int i = 1; i < renderer.Length; i++)
            {
                bounds.Encapsulate(renderer[i].bounds);
            }

            bool xaxis = bounds.size.x > bounds.size.z ? true : false; ;

            List<Renderer> list = new List<Renderer>(renderer);
            list.Sort((Renderer r1, Renderer r2) => { return xaxis ? r2.bounds.min.x.CompareTo(r1.bounds.min.x) : r2.bounds.min.z.CompareTo(r1.bounds.min.z); });
            return list.ToArray();
        }

        static public Renderer[] CreateMergedMesh(Transform parent, Renderer[] list, bool sort=true, bool groupmaterialonly=false, int maxvertex = 32768)
        {
            if (sort)
            {
                list = SortByAxis(list);
                list = SortByMaterial(list);
            }

            List<Renderer> group = new List<Renderer>();
            for (int i = 0; i < list.Length;)
            {
                List<Renderer> items = new List<Renderer>();
                int total = 0, j;
                for (j = i; j < list.Length; j++)
                {
                    Renderer item = list[j];
                    MeshFilter m = item.GetComponent<MeshFilter>();
                    Mesh mesh = null;
                    if (m != null)
                        mesh = m.sharedMesh;
                    else
                    {
                        SkinnedMeshRenderer skinnedmesh = item.GetComponent<SkinnedMeshRenderer>();
                        if (skinnedmesh != null)
                            mesh = skinnedmesh.sharedMesh;
                    }

                    if (mesh != null)
                    {
                        if (m.sharedMesh.vertexCount >= maxvertex)
                        {
                            group.Add(item);
                        }
                        else
                        {
                            total += m.sharedMesh.vertexCount;
                            if (items.Count > 0 && (total >= maxvertex || (groupmaterialonly == true && items[0].sharedMaterial != item.sharedMaterial)))
                                break;
                            items.Add(item);
                        }
                    }
                    else
                    {
                        group.Add(item);
                    }
                }
                if (items.Count > 0)
                {
                    MeshRenderer meshrenderer = CreateMeshrenderer(parent, items.ToArray());
                    group.Add(meshrenderer);
                }
                i = j;
            }

            return group.ToArray();
        }

        static MeshRenderer CreateMeshrenderer(Transform parent, Renderer[] m)
        {
            GameObject o = new GameObject();
            if (parent != null)
            {
                o.transform.SetParent(parent);
                o.name = "[" + parent.gameObject.name + "]+" + m.Length;
                o.transform.localPosition = Vector3.zero;
                o.transform.localRotation = Quaternion.identity;
                o.transform.localScale = Vector3.one;
            }

            MeshFilter meshfilter = o.AddComponent<MeshFilter>();
            MeshRenderer meshrenderer = o.AddComponent<MeshRenderer>();
            UpdateMesh(meshrenderer, meshfilter, m);
            return meshrenderer;
        }

        static void UpdateMesh(MeshRenderer meshrenderer, MeshFilter meshfilter, Renderer[] m)
        {
            List<Material> mat = new List<Material>();

            for (int i = 0; i < m.Length; i++)
            {
                for (int j = 0; j < m[i].sharedMaterials.Length; j++)
                    if (mat.IndexOf(m[i].sharedMaterials[j]) == -1)
                        mat.Add(m[i].sharedMaterials[j]);
            }
            meshrenderer.sharedMaterials = mat.ToArray();

            if (mat.Count == 1)
                meshfilter.mesh = MergeMesh(m, mat[0], meshrenderer.transform.worldToLocalMatrix);
            else
            {
                CombineInstance[] combine = new CombineInstance[mat.Count];
                for (int i = 0; i < mat.Count; i++)
                {
                    combine[i].mesh = MergeMesh(m, mat[i], meshrenderer.transform.worldToLocalMatrix);
                    combine[i].transform = Matrix4x4.identity;
                }

                Mesh combinemesh = new Mesh();
                combinemesh.CombineMeshes(combine, false);
                meshfilter.sharedMesh = combinemesh;

                for (int i = 0; i < combine.Length; i++)
                    Object.DestroyImmediate(combine[i].mesh);
            }
        }

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
#endregion

#region combine meshes
        static Mesh MergeMesh(Renderer[] m, Material sharedMaterial, Matrix4x4 worldToLocal)
        {
            List<CombineInstance> combine = new List<CombineInstance>();
            List<Mesh> remove = new List<Mesh>();
            for (int i = 0; i < m.Length; i++)
            {
                Mesh sharedMesh = GetMesh(m[i]);
                for (int j = 0; j < m[i].sharedMaterials.Length; j++)
                {
                    if (m[i].sharedMaterials[j] == sharedMaterial)
                    {
                        CombineInstance c = new CombineInstance();
                        if (sharedMesh.subMeshCount == 1)
                        {
                            c.mesh = sharedMesh;
                            c.transform = worldToLocal * m[i].transform.localToWorldMatrix;
                        }
                        else
                        {
                            c.mesh = ExtractMesh(sharedMesh, j, worldToLocal * m[i].transform.localToWorldMatrix);
                            c.transform = Matrix4x4.identity;
                            remove.Add(c.mesh);
                        }
                        combine.Add(c);
                    }
                }
            }
            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combine.ToArray(), true);
            for (int i = 0; i < remove.Count; i++)
                Object.DestroyImmediate(remove[i]);
            return mesh;
        }

        static Mesh ExtractMesh(Mesh mesh, int submeshindex, Matrix4x4 tm)
        {
            int[] trianges = mesh.GetIndices(submeshindex);

            int[] cur2new = new int [mesh.vertexCount];
            List<int> new2cur = new List<int>(mesh.vertexCount);
            byte[] mask = new byte[(mesh.vertexCount + 7) / 8];
            int[] newtriangles = new int[trianges.Length];

            for (int i = 0; i < trianges.Length; i++)
            {
                int v = trianges[i];
                if ((mask[v / 8] & (1 << (v & 7))) == 0)
                {
                    cur2new[v] = new2cur.Count;
                    new2cur.Add(v);
                    mask[v / 8] |= (byte)(1 << (v & 7));
                }
                newtriangles[i] = cur2new[v];
            }

            int[] table = new2cur.ToArray();

            Mesh m = new Mesh();
            m.vertices = TransformPoint(mesh.vertices, table, tm);
            if (mesh.normals != null && mesh.normals.Length > 0)
                m.normals = TransformNormal(mesh.normals, table, tm);
            if (mesh.tangents != null && mesh.tangents.Length > 0)
                m.tangents = TransformTangents(mesh.tangents, table, tm);
            if (mesh.colors != null && mesh.colors.Length > 0)
                m.colors = CopyVertex(mesh.colors, table);
            if (mesh.uv != null && mesh.uv.Length > 0)
                m.uv = CopyVertex(mesh.uv, table);
            if (mesh.uv2 != null && mesh.uv2.Length > 0)
                m.uv2 = CopyVertex(mesh.uv2, table);
            if (mesh.uv3 != null && mesh.uv3.Length > 0)
                m.uv3 = CopyVertex(mesh.uv3, table);
            if (mesh.uv4 != null && mesh.uv4.Length > 0)
                m.uv4 = CopyVertex(mesh.uv4, table);
            m.triangles = newtriangles;
            m.RecalculateBounds();
            return m;
        }

        static Vector3[] TransformPoint(Vector3[] v, int[] table, Matrix4x4 tm)
        {
            Vector3[] p = new Vector3[table.Length];
            for (int i = 0; i < p.Length; i++)
                p[i] = tm.MultiplyPoint3x4(v[table[i]]);
            return p;
        }

        static Vector3[] TransformNormal(Vector3[] v, int[] table, Matrix4x4 tm)
        {
            Vector3[] p = new Vector3[table.Length];
            for (int i = 0; i < p.Length; i++)
                p[i] = tm.MultiplyVector(v[table[i]]);
            return p;
        }

        static Vector4[] TransformTangents(Vector4[] v, int[] table, Matrix4x4 tm)
        {
            Vector4[] p = new Vector4[table.Length];
            for (int i = 0; i < p.Length; i++)
            {
                Vector4 tangent = v[table[i]];
                Vector3 t = tm.MultiplyVector((Vector3)tangent);
                p[i] = new Vector4(t.x, t.y, t.z, tangent.w);
            }
            return p;
        }

        static T[] CopyVertex<T>(T[] v, int[] table)
        {
            T[] p = new T[table.Length];
            for (int i = 0; i < p.Length; i++)
                p[i] = v[table[i]];
            return p;
        }
#endregion

#if UNITY_EDITOR
        public void DrawCulledBound()
        {
            Gizmos.color = new Color(0, 1, 0, 0.25f);

            for (int i = 0; i < m_MeshGroup.Length; i++)
            {
                if (m_MeshGroup[i].deactive == true)
                {
                    float[] boundselement = m_DeactiveType == DeactiveType.GameObjectDeactive || m_VolumeType == VolumeType.NonStatic ? m_BoundsElement : m_MeshGroup[i].boundselement;
                    Vector3 min = new Vector3(boundselement[0], boundselement[2], boundselement[4]);
                    Vector3 max = new Vector3(boundselement[1], boundselement[3], boundselement[5]);
                    Gizmos.DrawCube((min+max)*0.5f, max-min);
                }
            }
        }
#endif
    }
}