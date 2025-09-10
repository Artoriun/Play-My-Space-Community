#define UPDATE_IF_CHANGED
#define DETECT_CROSS_RECT
#if UNITY_5_3_OR_NEWER
#define VR_ENABLE
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RT
{
    public class Occluder
    {
        public void SetVolume(Vector3[] vtx, int[] hulltriangle, int[] edge, Matrix4x4 localToWorldMatrix, bool undergroundbottom = false)
        {
            m_LocalVertex = vtx;
            m_HullTriangle = hulltriangle;
            m_Edge = edge;

            m_Vertex = new Vector3[vtx.Length];
            m_Plane = new Plane[m_HullTriangle.Length / 3];
            m_Frustum = new Plane[m_Plane.Length + m_Edge.Length / 4];
            m_FrustumElement = new float[(m_Plane.Length + m_Edge.Length / 4) * 4];
#if DETECT_CROSS_RECT
            m_Frustum2D1 = new Vector3[m_Plane.Length + m_Edge.Length / 4];
#if VR_ENABLE
            m_Frustum2D2 = new Vector3[m_Plane.Length + m_Edge.Length / 4];
#endif
            m_FrustumCount1 = 0;
            m_FrustumMask = 0;
#endif
#if UNITY_EDITOR
            m_VisibleEdge = new Vector3[m_Frustum.Length * 3 + (m_WindowVolumeElement1 != null ? m_WindowVolumeElement1.Length / (4 * 4) * 3 : 0)];
            m_VisibleEdgeCount = 0;
#endif
            m_FrustumCount = 0;

            m_OpenPlane = -1;

            if (undergroundbottom)
            {
                for (int i = 0, j = 0; i < m_HullTriangle.Length; i += 3, j++)
                {
                    Plane p = new Plane(m_LocalVertex[m_HullTriangle[i]], m_LocalVertex[m_HullTriangle[i + 2]], m_LocalVertex[m_HullTriangle[i + 1]]);
                    float dot = Vector3.Dot(p.normal, Vector3.up);
                    if (dot >= 1.0f - 0.001f)
                        m_OpenPlane = j;
                }
            }

            UpdateVolume(localToWorldMatrix);

            if (_clipmask.Length < m_Vertex.Length)
                _clipmask = new int[m_Vertex.Length];
        }


        static readonly int[] _box_edge = new int[] { 0, 4, 0, 1, 0, 1, 1, 3, 0, 2, 3, 2, 0, 3, 2, 0, 4, 1, 1, 5, 1, 2, 3, 7, 2, 3, 2, 6, 3, 4, 0, 4, 4, 5, 4, 5, 1, 5, 5, 7, 2, 5, 7, 6, 3, 5, 6, 4 }; // 12x2
        static readonly int[] _box_face = new int[] { 0, 2, 3, 1, 3, 7, 3, 2, 6, 2, 0, 4, 0, 1, 5, 7, 6, 4 }; // -z+x+y-x-y+z

        public void SetBoxVolume(Bounds bounds, Matrix4x4 localToWorldMatrix, bool undergroundbottom = false)
        {
            Vector3[] vtx = new Vector3[8];
            Vector3 boundsmin = bounds.min;
            Vector3 boundsmax = bounds.max;
            for (int i = 0; i < 8; i++)
                vtx[i] = new Vector3((i & 1) != 0 ? boundsmax.x : boundsmin.x, (i & 2) != 0 ? boundsmax.y : boundsmin.y, (i & 4) != 0 ? boundsmax.z : boundsmin.z);
            SetVolume(vtx, _box_face, _box_edge, localToWorldMatrix, undergroundbottom);
        }

        public void SetTowerVolume(Vector2[] point, Vector3 offset, float h, Matrix4x4 localToWorldMatrix, bool makeconvex = false, bool undergroundbottom = false)
        {
            Vector2[] p = makeconvex == true ? ConvexHull2d(point) : point;
            Vector3[] v = new Vector3[p.Length * 2];

            for (int i = 0; i < p.Length; i++)
            {
                v[i] = new Vector3(p[i].x, 0, p[i].y) + offset;
                v[i + p.Length] = new Vector3(p[i].x, h, p[i].y) + offset;
            }

            List<int> hull = new List<int>();
            for (int i = 0; i < p.Length; i++)
                hull.AddRange(new int[] { i, i + p.Length, (i + 1) % p.Length });
            hull.AddRange(new int[] { 0, 1, 2 }); // bottom
            hull.AddRange(new int[] { p.Length + 0, p.Length + 2, p.Length + 1 }); // top

            List<int> edge = new List<int>();
            for (int i = 0; i < p.Length; i++)
            {
                int next = (i + 1) % p.Length;
                edge.AddRange(new int[] { i, next, next, next + p.Length }); // side
                edge.AddRange(new int[] { i, p.Length, i, next }); // bottom
                edge.AddRange(new int[] { i, p.Length + 1, i + p.Length, next + p.Length }); // top
            }

            SetVolume(v, hull.ToArray(), edge.ToArray(), localToWorldMatrix, undergroundbottom);
        }

        public void SetTowerVolumeWithWindow(Vector2[] point, Vector3 offset, float h, Matrix4x4 localToWorldMatrix, int[] sideidx, Rect[] rect, bool undergroundbottom)
        {
            if (sideidx != null && sideidx.Length > 0)
            {
                List<int> windowindex = new List<int>(sideidx.Length);
                List<Vector3> localvertex = new List<Vector3>(rect.Length * 4);
                for (int i = 0; i < sideidx.Length; i++)
                {
                    int idx = sideidx[i];
                    if (idx >= point.Length)
                        continue;
                    Rect r = rect[i];

                    int idx2 = (idx + 1) % point.Length;
                    Vector3 pos = new Vector3(point[idx].x, 0, point[idx].y);
                    Vector3 udir = new Vector3(point[idx2].x, 0, point[idx2].y) - pos;
                    Vector3 vdir = Vector3.up * h;

                    windowindex.Add(idx);
                    localvertex.Add(pos + offset + udir * r.xMin + vdir * r.yMin);
                    localvertex.Add(pos + offset + udir * r.xMax + vdir * r.yMin);
                    localvertex.Add(pos + offset + udir * r.xMax + vdir * r.yMax);
                    localvertex.Add(pos + offset + udir * r.xMin + vdir * r.yMax);
                }
                m_WindowIndex = windowindex.ToArray();
                m_WindowLocalVertex = localvertex.ToArray();
                m_WindowVolumeElement1 = new float[m_WindowIndex.Length * 4 * 4];
#if VR_ENABLE
                m_WindowVolumeElement2 = new float[m_WindowIndex.Length * 4 * 4];
#endif
            }

            SetTowerVolume(point, offset, h, localToWorldMatrix, false, undergroundbottom);
        }

        static public void GetVolumeEdgeAndHull(int[] polygons, out int[] out_hull, out int[] out_edge)
        {
            List<int> hull = new List<int>();
            List<int> edge = new List<int>();

            for (int i = 0, n = 0; i < polygons.Length; n++)
            {
                int l = 0;
                for (l = 0; polygons[i + l] != -1; l++) ;

                hull.Add(polygons[i + 0]);
                hull.Add(polygons[i + 2]);
                hull.Add(polygons[i + 1]);

                for (int i1 = l - 1, i2 = 0; i2 < l; i1 = i2++)
                {
                    int v1 = polygons[i + i1];
                    int v2 = polygons[i + i2];
                    int j;
                    for (j = 0; j < edge.Count; j += 4)
                    {
                        if (edge[j + 2] == v1 && edge[j + 3] == v2)
                        {
                            edge[j + 1] = n;
                            break;
                        }
                    }
                    if (j == edge.Count)
                        edge.AddRange(new int[] { n, n, v2, v1 });
                }
                i += l + 1;
            }

            out_hull = hull.ToArray();
            out_edge = edge.ToArray();
        }

        public void SetConvexVolume(Vector3[] points, Matrix4x4 localToWorldMatrix, bool undergroundbottom = false, int limitvertex = 0)
        {
            int[] out_polygons;
            int[] out_hull;
            int[] out_edge;

            Vector3[] vtx = EliminateDuplicatedVertices(points);

            ConvexHull3d(vtx, out out_polygons);
            GetVolumeEdgeAndHull(out_polygons, out out_hull, out out_edge);
            SetVolume(vtx, out_hull, out_edge, localToWorldMatrix, undergroundbottom);
        }

        public void SetTransform(Transform transform)
        {
            m_Transform = transform;
            if (transform != null)
                UpdateVolume(transform.localToWorldMatrix);
        }

        public void UpdateVolume(Matrix4x4 localToWorldMatrix)
        {
            for (int i = 0; i < m_LocalVertex.Length; i++)
                m_Vertex[i] = localToWorldMatrix.MultiplyPoint3x4(m_LocalVertex[i]);

            for (int i = 0, j = 0; i < m_HullTriangle.Length; i += 3, j++)
                m_Plane[j] = new Plane(m_Vertex[m_HullTriangle[i]], m_Vertex[m_HullTriangle[i + 2]], m_Vertex[m_HullTriangle[i + 1]]);

            CalculateBounds(m_BoundsElement, m_Vertex);
            m_BoundsCenter = new Vector3((m_BoundsElement[0] + m_BoundsElement[1]) * 0.5f, (m_BoundsElement[2] + m_BoundsElement[3]) * 0.5f, (m_BoundsElement[4] + m_BoundsElement[5]) * 0.5f);

            m_LastLocalToWorld = localToWorldMatrix;
        }

        #region variable
        Vector3[] m_LocalVertex;
        int[] m_HullTriangle;
        int[] m_Edge; // (planeindex A, B, vertex A, B)
        int m_OpenPlane;

        int[] m_WindowIndex = null; // planeindex
        Vector3[] m_WindowLocalVertex = null;

        bool HasWindow { get { return m_WindowIndex != null && m_WindowIndex.Length > 0 ? true : false; } }

        Transform m_Transform = null;

#if UPDATE_IF_CHANGED
        enum CachedState { None, Visible, Invisible }
        CachedState m_CachedState = CachedState.None;
#endif
        Matrix4x4 m_LastLocalToWorld = Matrix4x4.identity;
        Vector3[] m_Vertex;
        Plane[] m_Plane;

        float[] m_BoundsElement = new float[6];
        Vector3 m_BoundsCenter;

        Plane[] m_Frustum;
        float[] m_FrustumElement;

#if DETECT_CROSS_RECT
        Vector3[] m_Frustum2D1;
#if VR_ENABLE
        Vector3[] m_Frustum2D2;
#endif
        int m_SurfaceCount;
        int m_FrustumCount1;
        ulong m_FrustumMask;
#endif
        int m_FrustumCount;

        float[] m_WindowVolumeElement1;
        int m_WindowPlaneCount1 = 0;
#if VR_ENABLE
        float[] m_WindowVolumeElement2;
        int m_WindowPlaneCount2 = 0;
#endif

#if UNITY_EDITOR
        Vector3[] m_VisibleEdge;
        int m_VisibleEdgeCount;
#endif
        #endregion

        public float[] GetBounds()
        {
            return m_BoundsElement;
        }

        public bool IsStatic()
        {
            return m_Transform == null ? true : false;
        }

        public void Enable()
        {
            RTOcclusionCulling.AddOccluder(this);
        }

        public void Disable()
        {
            RTOcclusionCulling.RemoveOccluder(this);
        }

        #region MakeFrustum
#if VR_ENABLE
        public bool MakeFrustum(Camera camera, Plane[] frustum, bool stereo, Matrix4x4 viewtm1, Matrix4x4 viewtm2, Vector3 campos1, Vector3 campos2, bool cameraChanged = true)
#else
    public bool MakeFrustum(Camera camera, Plane[] frustum, Matrix4x4 viewtm1, bool cameraChanged = true)
#endif
        {
            bool updatevolume = false;
            if (m_Transform != null && m_LastLocalToWorld != m_Transform.localToWorldMatrix)
            {
                UpdateVolume(m_Transform.localToWorldMatrix);
                updatevolume = true;
            }
#if UPDATE_IF_CHANGED
            if (cameraChanged == false && updatevolume == false)
            {
                if (m_CachedState != CachedState.None)
                {
                    return m_CachedState == CachedState.Visible ? true : false;
                }
#if UNITY_EDITOR
                m_VisibleEdgeCount = 0;
#endif
                bool visible;
                if (Distance(m_BoundsElement, camera.transform.position) <= camera.nearClipPlane)
                    visible = false;
                else
                {
#if VR_ENABLE
                    if (stereo)
                        visible = MakeFrustumStereo(frustum, viewtm1, viewtm2, campos1, campos2);
                    else
#endif
                        visible = MakeFrustumMono(frustum, viewtm1, camera.transform.position, camera.orthographic, camera.transform.forward);
                }
                m_CachedState = visible ? CachedState.Visible : CachedState.Invisible;
                return visible;
            }
            m_CachedState = CachedState.None;
#endif
#if UNITY_EDITOR
            m_VisibleEdgeCount = 0;
#endif
            Vector3 position = camera.transform.position;
            if (Distance(m_BoundsElement, position) <= camera.nearClipPlane)
                return false;
#if VR_ENABLE
            if (stereo)
                return MakeFrustumStereo(frustum, viewtm1, viewtm2, campos1, campos2);
#endif
            return MakeFrustumMono(frustum, viewtm1, position, camera.orthographic, camera.transform.forward);
        }

        static int[] _clipmask = new int[32];

        bool MakeFrustumMono(Plane[] frustum, Matrix4x4 viewtm, Vector3 position, bool cameraortho, Vector3 cameraforward)
        {
            int[] clipmask = _clipmask;
            int clip_and = 0xff;
            for (int i = 0; i < m_Vertex.Length; i++)
            {
                Vector3 v = m_Vertex[i];
                int mask = 0;
                for (int j = 0; j < frustum.Length; j++)
                    mask |= frustum[j].GetDistanceToPoint(v) < 0 ? (1 << j) : 0;
                clipmask[i] = mask;
                clip_and &= mask;
            }
            if (clip_and != 0) // out of sight from the camera
                return false;

            ulong[] side = new ulong[(m_Plane.Length + 63) / 64];
            int pn = 0;

            for (int i = 0; i < m_Plane.Length; i++)
            {
                if (m_Plane[i].GetDistanceToPoint(position) < 0)
                {
                    side[i >> 6] |= 1ul << (i & 63);
                    m_Frustum[pn++] = m_Plane[i];
                }
            }
            if (pn == 0)
                return false;

            if (HasWindow)
            {
                m_WindowPlaneCount1 = 0;

                for (int i = 0, off = 0; i < m_WindowIndex.Length; i++, off += 4)
                {
                    int idx = m_WindowIndex[i];
                    if ((side[idx >> 6] & (1ul << (idx & 63))) != 0)
                    {
                        Vector3 v0 = m_LastLocalToWorld.MultiplyPoint3x4(m_WindowLocalVertex[off + 0]);
                        Vector3 v1 = m_LastLocalToWorld.MultiplyPoint3x4(m_WindowLocalVertex[off + 1]);
                        Vector3 v2 = m_LastLocalToWorld.MultiplyPoint3x4(m_WindowLocalVertex[off + 2]);
                        Vector3 v3 = m_LastLocalToWorld.MultiplyPoint3x4(m_WindowLocalVertex[off + 3]);

                        SetPlaneElement(m_WindowVolumeElement1, m_WindowPlaneCount1 + 0, v1, v0, position);
                        SetPlaneElement(m_WindowVolumeElement1, m_WindowPlaneCount1 + 4, v2, v1, position);
                        SetPlaneElement(m_WindowVolumeElement1, m_WindowPlaneCount1 + 8, v3, v2, position);
                        SetPlaneElement(m_WindowVolumeElement1, m_WindowPlaneCount1 + 12, v0, v3, position);
                        m_WindowPlaneCount1 += 16;
#if UNITY_EDITOR
                        AddDebugEdge(v0, v1, position);
                        AddDebugEdge(v1, v2, position);
                        AddDebugEdge(v2, v3, position);
                        AddDebugEdge(v3, v0, position);
#endif
                    }
                }
            }

            int openside = m_OpenPlane >= 0 ? m_OpenPlane : -1;

#if DETECT_CROSS_RECT
            m_SurfaceCount = pn;

            Vector2[] viewvertex1 = new Vector2[m_Vertex.Length];
            for (int i = 0; i < m_Vertex.Length; i++)
                viewvertex1[i] = Proj2D(viewtm, m_Vertex[i]);
#endif
            for (int i = 0; i < m_Edge.Length; i += 4)
            {
                int e1 = m_Edge[i];
                int e2 = m_Edge[i + 1];
                if ((((side[e1 >> 6] >> (e1 & 63)) ^ (side[e2 >> 6] >> (e2 & 63))) & 1) != 0) // check if the edge is edged
                {
                    int i1 = m_Edge[i + 2];
                    int i2 = m_Edge[i + 3];

                    if ((clipmask[i1] & clipmask[i2]) != 0) // check if the edge is out of view frustum
                        continue;

                    if (m_Edge[i] == openside || m_Edge[i + 1] == openside)
                        continue;

                    Vector3 v1 = m_Vertex[i1];
                    Vector3 v2 = m_Vertex[i2];

                    Plane p = !cameraortho ? new Plane(v1, v2, position) : new Plane(Vector3.Cross(cameraforward, v2 - v1), v1);
                    bool flip = p.GetDistanceToPoint(m_BoundsCenter) < 0 ? true : false;

                    if (flip)
                    {
                        p.normal = -p.normal;
                        p.distance = -p.distance;
                    }
#if DETECT_CROSS_RECT
                    m_Frustum2D1[pn] = flip ? MakePlane2D(viewvertex1[i2], viewvertex1[i1]) : MakePlane2D(viewvertex1[i1], viewvertex1[i2]);
#endif
                    m_Frustum[pn++] = p;
#if UNITY_EDITOR
                    AddDebugEdge(flip ? v1 : v2, flip ? v2 : v1, position);
#endif
                }
            }
            m_FrustumCount = pn;
#if DETECT_CROSS_RECT
            m_FrustumCount1 = pn;
            m_FrustumMask = 0;
#endif
            SetPlaneElement(m_FrustumElement, m_Frustum, m_FrustumCount);
            return true;
        }
#if VR_ENABLE
        bool MakeFrustumStereo(Plane[] frustum, Matrix4x4 viewtm1, Matrix4x4 viewtm2, Vector3 position1, Vector3 position2)
        {
            int[] clipmask = _clipmask;
            int clip_and = 0xff;
            for (int i = 0; i < m_Vertex.Length; i++)
            {
                Vector3 v = m_Vertex[i];
                int mask = 0;
                for (int j = 0; j < frustum.Length; j++)
                    mask |= frustum[j].GetDistanceToPoint(v) < 0 ? (1 << j) : 0;
                clipmask[i] = mask;
                clip_and &= mask;
            }

            if (clip_and != 0) // out of sight from the camera
                return false;

            ulong[] side1 = new ulong[(m_Plane.Length + 63) / 64];
            ulong[] side2 = new ulong[(m_Plane.Length + 63) / 64];
            int pn = 0;

            for (int i = 0; i < m_Plane.Length; i++)
            {
                if (m_Plane[i].GetDistanceToPoint(position1) < 0)
                {
                    side1[i >> 6] |= 1ul << (i & 63);

                    if (m_Plane[i].GetDistanceToPoint(position2) < 0)
                    {
                        side2[i >> 6] |= 1ul << (i & 63);
                        m_Frustum[pn++] = m_Plane[i];
                    }
                }
                else if (m_Plane[i].GetDistanceToPoint(position2) < 0)
                {
                    side2[i >> 6] |= 1ul << (i & 63);
                }
            }

            if (pn == 0)
                return false;

            if (HasWindow)
            {
                m_WindowPlaneCount1 = 0;
                m_WindowPlaneCount2 = 0;

                for (int i = 0, off = 0; i < m_WindowIndex.Length; i++, off += 4)
                {
                    int idx = m_WindowIndex[i];

                    if ((side1[idx >> 6] & (1ul << (idx & 63))) != 0 ||
                        (side2[idx >> 6] & (1ul << (idx & 63))) != 0)
                    {
                        Vector3 v0 = m_LastLocalToWorld.MultiplyPoint3x4(m_WindowLocalVertex[off + 0]);
                        Vector3 v1 = m_LastLocalToWorld.MultiplyPoint3x4(m_WindowLocalVertex[off + 1]);
                        Vector3 v2 = m_LastLocalToWorld.MultiplyPoint3x4(m_WindowLocalVertex[off + 2]);
                        Vector3 v3 = m_LastLocalToWorld.MultiplyPoint3x4(m_WindowLocalVertex[off + 3]);

                        if ((side1[idx >> 6] & (1ul << (idx & 63))) != 0)
                        {
                            SetPlaneElement(m_WindowVolumeElement1, m_WindowPlaneCount1 + 0, v1, v0, position1);
                            SetPlaneElement(m_WindowVolumeElement1, m_WindowPlaneCount1 + 4, v2, v1, position1);
                            SetPlaneElement(m_WindowVolumeElement1, m_WindowPlaneCount1 + 8, v3, v2, position1);
                            SetPlaneElement(m_WindowVolumeElement1, m_WindowPlaneCount1 + 12, v0, v3, position1);
                            m_WindowPlaneCount1 += 16;
#if UNITY_EDITOR
                            AddDebugEdge(v0, v1, position1);
                            AddDebugEdge(v1, v2, position1);
                            AddDebugEdge(v2, v3, position1);
                            AddDebugEdge(v3, v0, position1);
#endif
                        }
                        if ((side2[idx >> 6] & (1ul << (idx & 63))) != 0)
                        {
                            SetPlaneElement(m_WindowVolumeElement2, m_WindowPlaneCount2 + 0, v1, v0, position2);
                            SetPlaneElement(m_WindowVolumeElement2, m_WindowPlaneCount2 + 4, v2, v1, position2);
                            SetPlaneElement(m_WindowVolumeElement2, m_WindowPlaneCount2 + 8, v3, v2, position2);
                            SetPlaneElement(m_WindowVolumeElement2, m_WindowPlaneCount2 + 12, v0, v3, position2);
                            m_WindowPlaneCount2 += 16;
                        }
                    }
                }
            }

            int openside = m_OpenPlane >= 0 ? m_OpenPlane : -1;

#if DETECT_CROSS_RECT
            m_FrustumMask = 0;
            m_SurfaceCount = pn;

            Vector2[] viewvertex1 = new Vector2[m_Vertex.Length];
            Vector2[] viewvertex2 = new Vector2[m_Vertex.Length];

            for (int i = 0; i < m_Vertex.Length; i++)
                viewvertex1[i] = Proj2D(viewtm1, m_Vertex[i]);

            for (int i = 0; i < m_Vertex.Length; i++)
                viewvertex2[i] = Proj2D(viewtm2, m_Vertex[i]);
#endif
            for (int i = 0; i < m_Edge.Length; i += 4)
            {
                int e1 = m_Edge[i];
                int e2 = m_Edge[i + 1];
                bool edge1 = (((side1[e1 >> 6] >> (e1 & 63)) ^ (side1[e2 >> 6] >> (e2 & 63))) & 1) != 0;

                if (edge1) // left and both
                {
                    int i1 = m_Edge[i + 2];
                    int i2 = m_Edge[i + 3];

                    if ((clipmask[i1] & clipmask[i2]) != 0) // check if the edge is out of view frustum
                        continue;

                    if (m_Edge[i] == openside || m_Edge[i + 1] == openside)
                        continue;

                    Vector3 v1 = m_Vertex[i1];
                    Vector3 v2 = m_Vertex[i2];

                    Plane p = new Plane(v1, v2, position1);
                    bool flip = p.GetDistanceToPoint(m_BoundsCenter) < 0 ? true : false;

                    if (flip)
                    {
                        p.normal = -p.normal;
                        p.distance = -p.distance;
                    }

                    bool edge2 = (((side2[e1 >> 6] >> (e1 & 63)) ^ (side2[e2 >> 6] >> (e2 & 63))) & 1) != 0;
                    if (edge2 == true)
                    {
#if DETECT_CROSS_RECT
                        m_FrustumMask |= 1UL << pn;
#endif
                        if (p.GetDistanceToPoint(position2) < 0)
                        {
#if UNITY_EDITOR
                            edge1 = false;
#endif
                            p = flip ? new Plane(v2, v1, position2) : new Plane(v1, v2, position2);
                        }
                    }
#if DETECT_CROSS_RECT
                    m_Frustum2D1[pn] = flip ? MakePlane2D(viewvertex1[i2], viewvertex1[i1]) : MakePlane2D(viewvertex1[i1], viewvertex1[i2]);
                    m_Frustum2D2[pn] = flip ? MakePlane2D(viewvertex2[i2], viewvertex2[i1]) : MakePlane2D(viewvertex2[i1], viewvertex2[i2]);
#endif
                    m_Frustum[pn] = p;
                    pn++;
#if UNITY_EDITOR
                    AddDebugEdge(flip ? v1 : v2, flip ? v2 : v1, edge1 == true ? position1 : position2);
#endif
                }
            }
#if DETECT_CROSS_RECT
            m_FrustumCount1 = pn;
#endif
            for (int i = 0; i < m_Edge.Length; i += 4)
            {
                int e1 = m_Edge[i];
                int e2 = m_Edge[i + 1];

                bool edge1 = (((side1[e1 >> 6] >> (e1 & 63)) ^ (side1[e2 >> 6] >> (e2 & 63))) & 1) != 0;
                bool edge2 = (((side2[e1 >> 6] >> (e1 & 63)) ^ (side2[e2 >> 6] >> (e2 & 63))) & 1) != 0;

                if (!edge1 && edge2) // right edge
                {
                    int i1 = m_Edge[i + 2];
                    int i2 = m_Edge[i + 3];

                    if ((clipmask[i1] & clipmask[i2]) != 0) // check if the edge is out of view frustum
                        continue;

                    Vector3 v1 = m_Vertex[i1];
                    Vector3 v2 = m_Vertex[i2];

                    Plane p = new Plane(v1, v2, position2);
                    bool flip = p.GetDistanceToPoint(m_BoundsCenter) < 0 ? true : false;

                    if (flip)
                    {
                        p.normal = -p.normal;
                        p.distance = -p.distance;
                    }

#if DETECT_CROSS_RECT
                    m_FrustumMask |= 1UL << pn;
                    m_Frustum2D1[pn] = flip ? MakePlane2D(viewvertex1[i2], viewvertex1[i1]) : MakePlane2D(viewvertex1[i1], viewvertex1[i2]);
                    m_Frustum2D2[pn] = flip ? MakePlane2D(viewvertex2[i2], viewvertex2[i1]) : MakePlane2D(viewvertex2[i1], viewvertex2[i2]);
#endif
                    m_Frustum[pn] = p;
                    pn++;
#if UNITY_EDITOR
                    AddDebugEdge(flip ? v1 : v2, flip ? v2 : v1, position2);
#endif
                }
            }
            m_FrustumCount = pn;
            SetPlaneElement(m_FrustumElement, m_Frustum, m_FrustumCount);
            return true;
        }
#endif
        #endregion

        #region CullTest
        public bool CullTest(float[] bounds)
        {
            for (int i = 0, off = 0; i < m_FrustumCount; i++, off += 4)
            {
                float d = bounds[m_FrustumElement[off] < 0 ? 1 : 0] * m_FrustumElement[off] +
                    bounds[m_FrustumElement[off + 1] < 0 ? 3 : 2] * m_FrustumElement[off + 1] +
                    bounds[m_FrustumElement[off + 2] < 0 ? 5 : 4] * m_FrustumElement[off + 2] +
                    m_FrustumElement[off + 3];
                if (d <= 0)
                    return false;
            }
            if (HasWindow)
            {
                if (WindowOverlapTest(bounds, m_WindowVolumeElement1, m_WindowPlaneCount1) == true)
                    return false;
#if VR_ENABLE
                if (WindowOverlapTest(bounds, m_WindowVolumeElement2, m_WindowPlaneCount2) == true)
                    return false;
#endif
            }
            return true;
        }

        static bool WindowOverlapTest(float[] bounds, float[] volumeelenemt, int length)
        {
            for (int i = 0; i < length; i += 16)
            {
                int off;
                for (off = i; off < i + 16; off += 4)
                {
                    float d = bounds[volumeelenemt[off] < 0 ? 1 : 0] * volumeelenemt[off] +
                        bounds[volumeelenemt[off + 1] < 0 ? 3 : 2] * volumeelenemt[off + 1] +
                        bounds[volumeelenemt[off + 2] < 0 ? 5 : 4] * volumeelenemt[off + 2] +
                        volumeelenemt[off + 3];
                    if (d > 0)
                        break;
                }
                if (off == i + 16)
                    return true;
            }
            return false;
        }

#if DETECT_CROSS_RECT
#if VR_ENABLE
        public bool ClipRect(ref Rect rect, int planeidx, bool extra)
        {
            return Split(ref rect, extra == false ? m_Frustum2D1[planeidx] : m_Frustum2D2[planeidx]);
        }
#else
    public bool ClipRect(ref Rect rect, int planeidx)
    {
        return Split(ref rect, m_Frustum2D1[planeidx]);
    }
#endif
        public int CullTestWithClip(float[] bounds) // -1 cull_complete  -2 fail 0~ overlaped solo plane index
        {
            int overlapidx = -1;

            for (int i = 0, off = 0; i < m_FrustumCount; i++, off += 4)
            {
                float d = bounds[m_FrustumElement[off] < 0 ? 1 : 0] * m_FrustumElement[off] +
                    bounds[m_FrustumElement[off + 1] < 0 ? 3 : 2] * m_FrustumElement[off + 1] +
                    bounds[m_FrustumElement[off + 2] < 0 ? 5 : 4] * m_FrustumElement[off + 2] +
                    m_FrustumElement[off + 3];

                if (d <= 0)
                {
                    if (i < m_SurfaceCount)
                        return -2;

                    if (overlapidx != -1)
                        return -2;

                    d = bounds[m_FrustumElement[off] > 0 ? 1 : 0] * m_FrustumElement[off] +
                        bounds[m_FrustumElement[off + 1] > 0 ? 3 : 2] * m_FrustumElement[off + 1] +
                        bounds[m_FrustumElement[off + 2] > 0 ? 5 : 4] * m_FrustumElement[off + 2] +
                        m_FrustumElement[off + 3];
                    if (d <= 0)
                        return -2;
                    overlapidx = i;
                }
            }
            if (HasWindow)
            {
                if (WindowOverlapTest(bounds, m_WindowVolumeElement1, m_WindowPlaneCount1) == true)
                    return -2;
            }
            return overlapidx;
        }
#if VR_ENABLE
        public int CullTestWithClip(float[] bounds, ref Rect rect1, ref Rect rect2, ref int rectstate) // -1 cull_complete  -2 fail 0~ overlaped solo plane index
        {
            int overlapidx = -1;

            for (int i = 0, off = 0; i < m_SurfaceCount; i++, off += 4)
            {
                float d = bounds[m_FrustumElement[off] < 0 ? 1 : 0] * m_FrustumElement[off] +
                    bounds[m_FrustumElement[off + 1] < 0 ? 3 : 2] * m_FrustumElement[off + 1] +
                    bounds[m_FrustumElement[off + 2] < 0 ? 5 : 4] * m_FrustumElement[off + 2] +
                    m_FrustumElement[off + 3];
                if (d <= 0)
                    return -2;
            }

            if (HasWindow)
            {
                if (WindowOverlapTest(bounds, m_WindowVolumeElement1, m_WindowPlaneCount1) == true ||
                    WindowOverlapTest(bounds, m_WindowVolumeElement2, m_WindowPlaneCount2) == true)
                    return -2;
            }

            if ((rectstate & 1) != 0)
            {
                for (int i = m_SurfaceCount; i < m_FrustumCount1; i++)
                {
                    Vector3 p = m_Frustum2D1[i];
                    if (p.x * (p.x < 0 ? rect1.xMax : rect1.xMin) + p.y * (p.y < 0 ? rect1.yMax : rect1.yMin) + p.z <= 0)
                    {
                        if (overlapidx != -1)
                            return -2;
                        overlapidx = i;
                    }
                }
            }
            if ((rectstate & 2) != 0)
            {
                for (int i = m_SurfaceCount; i < m_FrustumCount; i++)
                {
                    if (i == overlapidx || (m_FrustumMask & (1UL << i)) == 0)
                        continue;

                    Vector3 p = m_Frustum2D2[i];
                    if (p.x * (p.x < 0 ? rect2.xMax : rect2.xMin) + p.y * (p.y < 0 ? rect2.yMax : rect2.yMin) + p.z <= 0)
                    {
                        if (overlapidx != -1)
                            return -2;
                        overlapidx = i;
                    }
                }
            }
            if (overlapidx != -1)
            {
                rectstate = ((rectstate & 1) != 0 && Split(ref rect1, m_Frustum2D1[overlapidx]) ? 1 : 0) |
                    ((rectstate & 2) != 0 && Split(ref rect2, m_Frustum2D2[overlapidx]) ? 2 : 0);
            }
            return overlapidx;
        }
#endif
        public int CullTestWithClip(float[] bounds, ref Rect rect1) // -1 cull_complete  -2 fail 0~ overlaped solo plane index
        {
            int overlapidx = -1;

            for (int i = 0, off = 0; i < m_FrustumCount; i++, off += 4)
            {
                float d = bounds[m_FrustumElement[off] < 0 ? 1 : 0] * m_FrustumElement[off] +
                    bounds[m_FrustumElement[off + 1] < 0 ? 3 : 2] * m_FrustumElement[off + 1] +
                    bounds[m_FrustumElement[off + 2] < 0 ? 5 : 4] * m_FrustumElement[off + 2] +
                    m_FrustumElement[off + 3];
                if (d <= 0)
                {
                    if (i < m_SurfaceCount)
                        return -2;
                    if (overlapidx != -1)
                        return -2;

                    d = bounds[m_FrustumElement[off] > 0 ? 1 : 0] * m_FrustumElement[off] +
                        bounds[m_FrustumElement[off + 1] > 0 ? 3 : 2] * m_FrustumElement[off + 1] +
                        bounds[m_FrustumElement[off + 2] > 0 ? 5 : 4] * m_FrustumElement[off + 2] +
                        m_FrustumElement[off + 3];
                    if (d <= 0)
                        return -2;
                    overlapidx = i;
                }
            }
            if (HasWindow)
            {
                if (WindowOverlapTest(bounds, m_WindowVolumeElement1, m_WindowPlaneCount1) == true)
                    return -2;
            }
            if (overlapidx != -1)
            {
                if (Split(ref rect1, m_Frustum2D1[overlapidx]) == false)
                    return -1;
            }
            return overlapidx;
        }
#endif
        #endregion
#if UNITY_EDITOR
        void AddDebugEdge(Vector3 v0, Vector3 v1, Vector3 p)
        {
            m_VisibleEdge[m_VisibleEdgeCount * 3 + 0] = v0;
            m_VisibleEdge[m_VisibleEdgeCount * 3 + 1] = v1;
            m_VisibleEdge[m_VisibleEdgeCount * 3 + 2] = p;
            m_VisibleEdgeCount++;
        }

        public void GatherOcclusionEdge(Camera camera, List<Vector3> edge, List<Vector3> point)
        {
            for (int i = 0; i < m_VisibleEdgeCount; i++)
            {
                edge.Add(m_VisibleEdge[i * 3 + 0]);
                edge.Add(m_VisibleEdge[i * 3 + 1]);
                point.Add(m_VisibleEdge[i * 3 + 2]);
            }
        }
#endif

        #region geometry util
        static void CalculateBounds(float[] boundselement, Vector3[] p)
        {
            boundselement[0] = boundselement[1] = p[0].x;
            boundselement[2] = boundselement[3] = p[0].y;
            boundselement[4] = boundselement[5] = p[0].z;
            for (int i = 1; i < p.Length; i++)
            {
                Vector3 v = p[i];
                if (v.x < boundselement[0]) boundselement[0] = v.x;
                if (v.x > boundselement[1]) boundselement[1] = v.x;
                if (v.y < boundselement[2]) boundselement[2] = v.y;
                if (v.y > boundselement[3]) boundselement[3] = v.y;
                if (v.z < boundselement[4]) boundselement[4] = v.z;
                if (v.z > boundselement[5]) boundselement[5] = v.z;
            }
        }

        static float Distance(float[] boundselement, Vector3 pos)
        {
            return Mathf.Max(pos.x - boundselement[1], pos.y - boundselement[3], pos.z - boundselement[5], boundselement[0] - pos.x, boundselement[2] - pos.y, boundselement[4] - pos.z);
        }

        static Vector2 Proj2D(Matrix4x4 m, Vector3 v)
        {
            float w = v.x * m.m30 + v.y * m.m31 + v.z * m.m32 + m.m33;
            return new Vector2((v.x * m.m00 + v.y * m.m01 + v.z * m.m02 + m.m03) / w, (v.x * m.m10 + v.y * m.m11 + v.z * m.m12 + m.m13) / w);
        }

        static void MakePlaneSet(Vector3[] convex_vtx, int[] convex_polygons, out int[] out_hull_triangles, out int[] out_edge)
        {
            List<int> hull = new List<int>();
            List<int> edge = new List<int>();

            for (int i = 0, n = 0; i < convex_polygons.Length; n++)
            {
                int l = 0;
                for (l = 0; convex_polygons[i + l] != -1; l++) ;

                hull.Add(convex_polygons[i + 0]);
                hull.Add(convex_polygons[i + 2]);
                hull.Add(convex_polygons[i + 1]);

                for (int i1 = l - 1, i2 = 0; i2 < l; i1 = i2++)
                {
                    int v1 = convex_polygons[i + i1];
                    int v2 = convex_polygons[i + i2];
                    int j;
                    for (j = 0; j < edge.Count; j += 4)
                    {
                        if (edge[j + 2] == v1 && edge[j + 3] == v2)
                        {
                            edge[j + 1] = n;
                            break;
                        }
                    }
                    if (j == edge.Count)
                        edge.AddRange(new int[] { n, n, v2, v1 });
                }
                i += l + 1;
            }

            out_hull_triangles = hull.ToArray();
            out_edge = edge.ToArray();
        }


#if DETECT_CROSS_RECT
        static Vector3 MakePlane2D(Vector2 p1, Vector2 p2)
        {
            Vector2 v = p2 - p1;
            float a = -v.y;
            float b = v.x;
            return new Vector3(a, b, -(a * p1.x + b * p1.y));
        }

        static float[] _d = new float[4];
        static float[] _p2 = new float[4 * 2];

        static bool Split(ref Rect rect, Vector3 plane)
        {
            float[] d = _d;
            float[] p = _p2;

            for (int i = 0, off = 0; i < 4; i++, off += 2)
            {
                float x = (i & 1) == 0 ? rect.xMin : rect.xMax;
                float y = (i & 2) == 0 ? rect.yMin : rect.yMax;
                d[i] = plane.x * x + plane.y * y + plane.z;
                p[off] = x;
                p[off + 1] = y;

                if (d[i] <= 0)
                {
                    float xMin, xMax, yMin, yMax;
                    xMin = xMax = x;
                    yMin = yMax = y;

                    for (i = i + 1, off += 2; i < 4; i++, off += 2)
                    {
                        x = (i & 1) == 0 ? rect.xMin : rect.xMax;
                        y = (i & 2) == 0 ? rect.yMin : rect.yMax;
                        d[i] = plane.x * x + plane.y * y + plane.z;
                        p[off] = x;
                        p[off + 1] = y;

                        if (d[i] <= 0)
                        {
                            if (x < xMin) xMin = x;
                            if (x > xMax) xMax = x;
                            if (y < yMin) yMin = y;
                            if (y > yMax) yMax = y;
                        }
                    }

                    for (int i2 = 0, i1 = 3; i2 < 4; i1 = i2++)
                    {
                        if (d[i1] * d[i2] < 0)
                        {
                            float w = d[i1] / (d[i1] - d[i2]);
                            x = p[i1 * 2 + 0] + (p[i2 * 2 + 0] - p[i1 * 2 + 1]) * w;
                            y = p[i1 * 2 + 1] + (p[i2 * 2 + 1] - p[i1 * 2 + 1]) * w;
                            if (x < xMin) xMin = x;
                            if (x > xMax) xMax = x;
                            if (y < yMin) yMin = y;
                            if (y > yMax) yMax = y;
                        }
                    }
                    rect.Set(xMin, yMin, xMax - xMin, yMax - yMin);
                    return true;
                }
            }

            return false;
        }
#endif
        #endregion
        #region ConvexHull(2D)
        public static Vector2[] ConvexHull2d(Vector2[] v) //Jarvis Gift Wrapping Algorithm (2d)
        {
            int p0 = 0;
            for (int i = 1; i < v.Length; i++) // lowest vertex
            {
                if (v[i].y < v[p0].y)
                    p0 = i;
            }

            uint[] mask = new uint[(v.Length + 31) / 32];
            for (int i = 0; i < mask.Length; i++)
                mask[i] = 0;

            List<Vector2> result = new List<Vector2>();
            Vector2 dir = Vector2.left;

            int p = p0;
            do
            {
                float maxdot = 1;
                int next = -1;
                for (int i = 0; i < v.Length; i++)
                {
                    if (i == p || (mask[i / 32] & (1U << (i % 32))) != 0)
                        continue;
                    Vector2 v1 = v[i] - v[p];
                    float dot = Vector2.Dot(dir, v1) / v1.magnitude;
                    if (dot < maxdot)
                    {
                        maxdot = dot;
                        next = i;
                    }
                }
                if (maxdot == -1 && result.Count > 0)
                    result[result.Count - 1] = v[next];
                else
                    result.Add(v[next]);
                mask[next / 32] |= 1U << (next % 32);
                dir = Vector3.Normalize(v[p] - v[next]);
                p = next;
            } while (p != p0);
            return result.ToArray();
        }
        #endregion

        #region ConvexHull(3D)
        static public void ConvexHull3d(Vector3[] vtx, out int[] out_polygons) //Jarvis Gift Wrapping Algorithm (3d)
        {
            int p0 = 0; // find x min, corner vertex
            float minx = vtx[p0].x;
            for (int i = 1; i < vtx.Length; i++)
            {
                if (vtx[i].x < minx || (vtx[i].x == minx && vtx[i].z > vtx[p0].z))
                {
                    p0 = i;
                    minx = vtx[p0].x;
                }
            }

            int p1 = -1;
            float maxy = -1;
            Vector3 v0 = vtx[p0];
            for (int i = 0; i < vtx.Length; i++)
            {
                if (i != p0)
                {
                    Vector3 vv = vtx[i] - v0;
                    float l = Mathf.Sqrt(Vector2.SqrMagnitude(vv));
                    if (l != 0)
                    {
                        float dot = vv.y / l;
                        if (p1 == -1 || dot > maxy || (dot == maxy && vtx[i].z > vtx[p1].z))
                        {
                            p1 = i;
                            maxy = dot;
                        }
                    }
                }
            }

            int[] polygons = GatherPolygon(vtx, p0, p1, Vector3.back);

            out_polygons = polygons;
        }

        static public Vector3[] Quantization(Vector3[] vtx, int resolution)
        {
            Bounds b = new Bounds(vtx[0], Vector3.zero);
            for (int i = 1; i < vtx.Length; i++)
                b.Encapsulate(vtx[i]);

            Vector3 min = b.min;
            float unit = Mathf.Max(b.size.x, b.size.y, b.size.z) * (1.0f / resolution);

            List <Vector3> v = new List<Vector3> ();
            HashSet<int> set = new HashSet<int>();

            int pitchy = (resolution + 1);
            int pitchz = pitchy * (resolution + 1);

            for (int i = 0; i < vtx.Length; i++)
            {
                int x = (int)((vtx[i].x - min.x) / unit);
                int y = (int)((vtx[i].y - min.y) / unit);
                int z = (int)((vtx[i].z - min.z) / unit);

                if (set.Add(x + y * pitchy + z * pitchz))
                    v.Add(min + (new Vector3((x + 0.5f) * unit, (y + 0.5f) * unit, (z + 0.5f) * unit)));
            }
            return v.ToArray();
        }

        static public Vector3[] ReduceVertexForConvex(Vector3[] vtx, int[] polygons, int tragetcount)
        {
            List<Vector3> v = new List<Vector3>(vtx);

            while (v.Count > tragetcount)
            {
                v.Sort((A, B) => { return A.x.CompareTo(B.x); });

                int p0 = 0;
                int p1 = 1;
                float length = Vector3.Distance(v[p0], v[p1]);

                for (int i = 0; i < v.Count; i++)
                {
                    float minx = v[i].x;
                    for (int j = i + 1; j < v.Count && v[i].x - minx <= length; j++)
                    {
                        float l = Vector3.Distance(v[i], v[j]);
                        if (l < length)
                        {
                            p0 = i;
                            p1 = j;
                            length = l;
                        }
                    }
                }
                v[p0] = (v[p0] + v[p1]) * 0.5f;
                v.RemoveAt(p1);
            }

            return v.ToArray();
        }

        static public Vector3[] EliminateDuplicatedVertices(Vector3[] v)
        {
            List<Vector3> p = new List<Vector3>(v.Length);
            HashSet<float> x = new HashSet<float>();
            for (int i = 0; i < v.Length; i++)
            {
                if (x.Add(v[i].x))
                {
                    p.Add(v[i]);
                }
                else
                {
                    int j = 0;
                    for (j = 0; j < p.Count; j++)
                        if (p[j] == v[i])
                            break;
                    if (j == p.Count)
                        p.Add(v[i]);
                }
            }
            return p.ToArray();
        }

        class Queue
        {
            public int v1;
            public int v2;
            public Vector3 sidedirection;
        }

        static int[] GatherPolygon(Vector3[] vtx, int p1, int p2, Vector3 sidedirection0)
        {
            List<int> polygons = new List<int>();
            Stack<Queue> pool = new Stack<Queue>();

            Queue<Queue> queue = new Queue<Queue>();
            queue.Enqueue(new Queue() { v1 = p1, v2 = p2, sidedirection = sidedirection0 });

            HashSet<int> complete_edge = new HashSet<int>();
            HashSet<int> ready_edge = new HashSet<int>();

            while (queue.Count > 0)
            {
                Queue q = queue.Dequeue();
                pool.Push(q);

                if (complete_edge.Contains(q.v1 + (q.v2 << 16)))
                    continue;

                int[] polygon = FindTopPolygon(vtx, q.v1, q.v2, q.sidedirection);
                if (polygon.Length < 3)
                    continue;

                for (int i2 = 0, i1 = polygon.Length - 1; i2 < polygon.Length; i1 = i2++)
                    complete_edge.Add(polygon[i1] + (polygon[i2]<<16));

                for (int i = polygon.Length - 1; i >= 0; i--)
                    polygons.Add(polygon[i]);
                polygons.Add(-1);

                Vector3 up = Vector3.Cross(vtx[polygon[2]] - vtx[polygon[0]], vtx[polygon[1]] - vtx[polygon[0]]);
                for (int i2 = 0, i1 = polygon.Length - 1; i2 < polygon.Length; i1 = i2++)
                {
                    if (ready_edge.Add(polygon[i2] + (polygon[i1]<<16)))
                    {
                        q = pool.Count > 0 ? pool.Pop() : new Queue();
                        q.v1 = polygon[i2];
                        q.v2 = polygon[i1];
                        q.sidedirection = Vector3.Normalize(Vector3.Cross(up, vtx[q.v1] - vtx[q.v2]));
                        queue.Enqueue(q);
                    }
                }
            }
            return polygons.ToArray();
        }

        static float _threshold = 0.001f;

        /*static int[] FindPolygon(Vector3[] vtx, int p1, int p2, Vector3 sidedirection)
        {
            Vector3 v1 = vtx[p1];
            Vector3 v2 = vtx[p2];
            int[] points = FindTopPolygon(vtx, p1, p2, sidedirection);
            Vector3 inNormal = Vector3.Normalize(Vector3.Cross(v2 - v1, vtx[points[2]] - v1));
            return MakeConvex(vtx, points, v2 - v1, inNormal);
        }*/

        static int[] FindTopPolygon(Vector3[] vtx, int p1, int p2, Vector3 sidedirection)
        {
            float maxdot = -1;
            int pick = -1;

            Vector3 v1 = vtx[p1];
            Vector3 v2 = vtx[p2];
            Vector3 axis = Vector3.Normalize(v2 - v1);
            for (int i = 0; i < vtx.Length; i++)
            {
                if (i != p1 && i != p2)
                {
                    Vector3 v = vtx[i] - v1;
                    float dot = Vector3.Dot(sidedirection, Vector3.Normalize(v - axis * Vector3.Dot(axis, v)));
                    if (dot > maxdot)
                    {
                        pick = i;
                        maxdot = dot;
                    }
                }
            }

            List<int> polygon = new List<int>();
            polygon.Add(p1);
            polygon.Add(p2);
            polygon.Add(pick);

            Vector3 up = Vector3.Normalize(Vector3.Cross(axis, vtx[pick] - v1));

            for (int i = 0; i < vtx.Length; i++)
            {
                if (i != p1 && i != p2 && i != pick)
                {
                    float d = Vector3.Dot(Vector3.Normalize(vtx[i] - v1), up);
                    if (d > -_threshold)
                        polygon.Add(i);
                }
            }
            return MakeConvex(vtx, polygon.ToArray(), axis, up);
        }

        static int[] MakeConvex(Vector3[] vtx, int[] p, Vector3 axis, Vector3 up)
        {
            int p0 = FindMinPoint(vtx, p, axis);
            Vector3 dir = Vector3.Normalize(Vector3.Cross(axis, up));

            List<int> result = new List<int>();

            for (int i = 0; i < p.Length; i++)
            {
                int next = FindNextPoint(vtx, p, p0, dir);
                if (result.IndexOf(next) != -1)
                    break;
                result.Add(next);

                dir = Vector3.Normalize(vtx[next] - vtx[p0]);
                p0 = next;
            }

            if (Vector3.Dot(Vector3.Cross(vtx[result[1]] - vtx[result[0]], vtx[result[2]] - vtx[result[0]]), up) < 0)
            {
                return result.ToArray();
            }
//                result.Reverse();

            return result.ToArray();
        }

        static int FindMinPoint(Vector3[] vtx, int[] p, Vector3 axis)
        {
            int p0 = -1;
            float mind = -1;
            for (int i = 0; i < p.Length; i++)
            {
                float d = Vector3.Dot(axis, vtx[p[i]]);
                if (p0 == -1 || d < mind)
                {
                    p0 = p[i];
                    mind = d;
                }
            }
            return p0;
        }

        static int FindNextPoint(Vector3[] vtx, int[] p, int p0, Vector3 dir)
        {
            int p1 = -1;
            float maxdot = -1;
            Vector3 v0 = vtx[p0];
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i] != p0)
                {
                    Vector3 v = vtx[p[i]] - v0;
                    float dot = Vector3.Dot(Vector3.Normalize(v), dir);
                    if (p1 == -1 || dot > maxdot - _threshold)
                    {
                        if (p1 != -1 && dot < maxdot + 0.0001f && Vector3.Magnitude(v) < Vector3.Magnitude(vtx[p1] - v0))
                            continue;

                        p1 = p[i];
                        maxdot = dot;
                    }
                }
            }
            return p1;
        }
#endregion

#region
        public static void SetPlaneElement(float[] planeelement, Plane[] plane, int count)
        {
            for (int i = 0, offset=0; i < count; i++, offset+=4)
            {
                Vector3 normal = plane[i].normal;
                planeelement[offset + 0] = normal.x;
                planeelement[offset + 1] = normal.y;
                planeelement[offset + 2] = normal.z;
                planeelement[offset + 3] = plane[i].distance;
            }
        }

        public static void SetPlaneElement(float[] planeelement, int offset, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Vector3 normal = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));
            planeelement[offset + 0] = normal.x;
            planeelement[offset + 1] = normal.y;
            planeelement[offset + 2] = normal.z;
            planeelement[offset + 3] = -Vector3.Dot(normal, p0);
        }

        public static float[] GetPlaneElement(Plane[] plane)
        {
            float[] planeelement = new float[plane.Length * 4];
            SetPlaneElement(planeelement, plane, plane.Length);
            return planeelement;
        }
#endregion
    }
}