#define UPDATE_IF_CHANGED
#define DETECT_CROSS_RECT
#define LOD_ENABLE
#if UNITY_5_3_OR_NEWER
#define VR_ENABLE
#endif
//#define DEBUG_LINE
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Camera))]
public class RTOcclusionCulling : MonoBehaviour
{
    public float m_CullTestDelay = 0.5f;
    public float m_IgnoreDistance = 100; // ignore occluder

    public int m_MaxOccluderCount = 6; // 0 unlimit
    public enum CullEvent
    {
        OnPrecull = 0,
        OnPrecullAndRestoreOnPostRender = 1,
        LateUpdate = 2,
    };
    public CullEvent m_CullEvent = CullEvent.OnPrecull;
    public float m_OccludeeRange = 0;
#if VR_ENABLE
    public bool m_VREnable = false;
#endif
#if LOD_ENABLE
    public float m_LodSwitchDistance = 0;
#endif
    Camera m_Camera;

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
    }
#region global
    static public void AddOccluder(RT.Occluder occ) { m_Occluder.Add(occ); m_OccluderUpdateSeq++; }
    static public void RemoveOccluder(RT.Occluder occ) { m_Occluder.Remove(occ); m_OccluderUpdateSeq++; }

    static public void AddOccludee(RT.Occludee occ) { m_Occludee.Add(occ); m_OccludeeUpdateSeq++; }
    static public void RemoveOccludee(RT.Occludee occ) { m_Occludee.Remove(occ); m_OccludeeUpdateSeq++; }

    static List<RT.Occluder> m_Occluder = new List<RT.Occluder>();
    static List<RT.Occludee> m_Occludee = new List<RT.Occludee>();

    static int m_OccluderUpdateSeq = 1;
    static int m_OccludeeUpdateSeq = 1;
#endregion

#if UPDATE_IF_CHANGED
    Matrix4x4 m_LastLocalToWorld = Matrix4x4.identity;
    bool Changed()
    {
        Matrix4x4 localToWorld = m_Camera.transform.localToWorldMatrix * m_Camera.projectionMatrix;
        if (localToWorld != m_LastLocalToWorld)
        {
            m_LastLocalToWorld = localToWorld;
            return true;
        }
        return false;
    }
#endif
    private void OnEnable()
    {
        if (m_Camera == null)
            enabled = false;
    }
    private void OnDisable()
    {
        for (int i = 0; i < m_Occludee.Count; i++)
            m_Occludee[i].ResetVisible();
    }

    static public float IgnoreDistance(float[] boundselement, Vector3 forward, float plane_d, float distance) // -1 or near distance
    {
        float neardistance = boundselement[forward.x < 0 ? 1 : 0] * forward.x + boundselement[forward.y < 0 ? 3 : 2] * forward.y + boundselement[forward.z < 0 ? 5 : 4] * forward.z - plane_d;
        if (neardistance > distance) // far from camera
            return -1;
        float fardistance = boundselement[forward.x > 0 ? 1 : 0] * forward.x + boundselement[forward.y > 0 ? 3 : 2] * forward.y + boundselement[forward.z > 0 ? 5 : 4] * forward.z - plane_d;
        if (fardistance < 0) // behind the camera
            return -1;
        return neardistance < 0 ? 0 : neardistance;
    }

    Vector3 m_CheckPosition;
    Vector3 m_CameraPosition;
    int m_CheckOccluderSeq = 0;
    RT.Occluder[] m_OccluderInDistance = null;

    RT.Occluder[] UpdateOccluderInDistanceList(Vector3 pos)
    {
        float margin = m_IgnoreDistance * 0.25f;

        if (m_OccluderInDistance == null ||
            m_CheckOccluderSeq != m_OccluderUpdateSeq ||
            (Mathf.Abs(m_CheckPosition.x - pos.x) >= margin || Mathf.Abs(m_CheckPosition.y - pos.y) >= margin || Mathf.Abs(m_CheckPosition.z - pos.z) >= margin))
        {
            m_CheckOccluderSeq = m_OccluderUpdateSeq;
            m_CheckPosition = pos;

            List<RT.Occluder> occluder = new List<RT.Occluder>(m_Occluder.Count);

            float checkdistance = m_IgnoreDistance + margin;
            for (int i = 0; i < m_Occluder.Count; i++)
            {
                if (!m_Occluder[i].IsStatic() || !TestAABBOutOfRange(m_Occluder[i].GetBounds(), pos, checkdistance))
                    occluder.Add(m_Occluder[i]);
            }
            m_OccluderInDistance = occluder.ToArray();
        }
        return m_OccluderInDistance;
    }
    
#if LOD_ENABLE
    public bool CheckLodDistance(float[] boundselement, bool fade, float distance)
    {
        if (distance == 0 || m_LodSwitchDistance == 0)
            distance = m_LodSwitchDistance;
        return distance == 0 ? false : TestAABBOutOfRange(boundselement, m_CameraPosition, fade ? distance * 1.01f : distance);
    }
#endif
    public bool IsOccludeeeInDistance(float[] boundselement)
    {
        return m_OccludeeRange == 0 || TestAABBOutOfRange(boundselement, m_CameraPosition, m_OccludeeRange) == false ? true : false;
    }

    static bool TestAABBOutOfRange(float[] boundselement, Vector3 pos, float distance)
    {
        if (distance < pos.x - boundselement[1] || distance < pos.y - boundselement[3] || distance < pos.z - boundselement[5])
            return true;
        if (boundselement[0] - pos.x > distance || boundselement[2] - pos.y > distance || boundselement[4] - pos.z > distance)
            return true;
        return false;
    }

    RT.Occluder[] m_ActiveOccluder = new RT.Occluder[32];
#if DETECT_CROSS_RECT
    int[] m_OverlapMask = new int[32];
    Rect[] m_RectOccluer = new Rect[32];
#endif
    static Vector3[] _frustumCorners = new Vector3[4];
    static void CalculateFrustumBounds(float[] boundselement, Camera camera)
    {
#if UNITY_5_3_OR_NEWER
        Vector3[] frustumCorners = _frustumCorners;
        Matrix4x4 localToWortld = camera.transform.localToWorldMatrix;

        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        SetBounds(boundselement, frustumCorners, localToWortld);

        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        Encapsulate(boundselement, frustumCorners, localToWortld);
#else
        if (camera.orthographic == false)
        {
            float tanh = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float tanw = tanh * camera.pixelWidth / camera.pixelHeight;
            Matrix4x4 tm = camera.transform.localToWorldMatrix;
            Bounds bounds = new Bounds(tm.MultiplyPoint3x4(new Vector3(tanw * camera.nearClipPlane, tanh * camera.nearClipPlane, camera.nearClipPlane)), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                float z = (i & 4) != 0 ? camera.farClipPlane : camera.nearClipPlane;
                Vector3 p = new Vector3((i & 1) != 0 ? -tanw * z : tanw * z, (i & 2) != 0 ? -tanh * z : tanh * z, z);
                bounds.Encapsulate(tm.MultiplyPoint3x4(p));
            }
            SetBounds(boundselement, bounds);
        }
        else
        {
            Matrix4x4 tm = camera.transform.localToWorldMatrix;
            float h = camera.orthographicSize * 0.5f;
            float w = h * camera.pixelWidth / camera.pixelHeight;
            Bounds bounds = new Bounds(tm.MultiplyPoint3x4(new Vector3(w, h, camera.nearClipPlane)), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                float z = (i & 4) != 0 ? camera.farClipPlane : camera.nearClipPlane;
                Vector3 p = new Vector3((i & 1) != 0 ? -w : w, (i & 2) != 0 ? -h : h, z);
                bounds.Encapsulate(tm.MultiplyPoint3x4(p));
            }
            SetBounds(boundselement, bounds);
        }
#endif
    }
#if DETECT_CROSS_RECT
    public static Rect ExpandRect(Rect rect1, Rect rect2)
    {
        float xMin = Mathf.Min(rect1.xMin, rect2.xMin);
        float yMin = Mathf.Min(rect1.yMin, rect2.yMin);
        return new Rect(xMin, yMin, Mathf.Max(rect1.xMax, rect2.xMax) - xMin, Mathf.Max(rect1.yMax, rect2.yMax) - yMin);
    }

    public static Rect CalcRect(Matrix4x4 m, float[] boundselement)
    {
        float w = boundselement[0] * m.m30 + boundselement[2] * m.m31 + boundselement[4] * m.m32 + m.m33;
        float x = (boundselement[0] * m.m00 + boundselement[2] * m.m01 + boundselement[4] * m.m02 + m.m03) / w;
        float y = (boundselement[0] * m.m00 + boundselement[2] * m.m01 + boundselement[4] * m.m02 + m.m03) / w;

        float xMin, xMax, yMin, yMax;
        xMin = xMax = x;
        yMin = yMax = y;

        for (int i = 1; i < 8; i++)
        {
            int ix = (i & 1) == 0 ? 0 : 1;
            int iy = (i & 2) == 0 ? 2 : 3;
            int iz = (i & 4) == 0 ? 4 : 5;

            w = boundselement[ix] * m.m30 + boundselement[iy] * m.m31 + boundselement[iz] * m.m32 + m.m33;
            x = (boundselement[ix] * m.m00 + boundselement[iy] * m.m01 + boundselement[iz] * m.m02 + m.m03) / w;
            y = (boundselement[ix] * m.m00 + boundselement[iy] * m.m01 + boundselement[iz] * m.m02 + m.m03) / w;

            if (x < xMin) xMin = x;
            if (x > xMax) xMax = x;
            if (y < yMin) yMin = y;
            if (y > yMax) yMax = y;
        }
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }
#endif
    private void LateUpdate()
    {
        if (m_CullEvent == CullEvent.LateUpdate)
            CullScene();
    }

    private void OnPreCull()
    {
        if (m_CullEvent == CullEvent.OnPrecull || m_CullEvent == CullEvent.OnPrecullAndRestoreOnPostRender)
            CullScene();
    }

    Matrix4x4 m_ViewLeft;
    Matrix4x4 m_ViewRight;
    Matrix4x4 m_ViewMono;

    static float[] _viewboundelement = new float[6];

    void CullScene()
    {
        if (m_Occludee.Count == 0)
            return;

#if UPDATE_IF_CHANGED
        bool changed = Changed();
#else
        bool changed = true;
#endif
        float[] viewboundelement = _viewboundelement;
        Plane[] viewfrustum;
        int i;

#if VR_ENABLE
        if (m_VREnable)
        {
            Matrix4x4 leftEyeWorld = m_Camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse * Matrix4x4.Scale(new Vector3(1, 1, -1));
            Matrix4x4 rightEyeWorld = m_Camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse * Matrix4x4.Scale(new Vector3(1, 1, -1));

            Matrix4x4 leftLocalTransform = m_Camera.transform.worldToLocalMatrix * leftEyeWorld;
            Matrix4x4 rightLocalTransform = m_Camera.transform.worldToLocalMatrix * rightEyeWorld;

            Vector3[] leftNearCorners = new Vector3[4];
            Vector3[] leftFarCorners = new Vector3[4];
            Vector3[] rightNearCorners = new Vector3[4];
            Vector3[] rightFarCorners = new Vector3[4];

            m_Camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), m_Camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Left, leftNearCorners);
            m_Camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), m_Camera.farClipPlane, Camera.MonoOrStereoscopicEye.Left, leftFarCorners);
            m_Camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), m_Camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Right, rightNearCorners);
            m_Camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), m_Camera.farClipPlane, Camera.MonoOrStereoscopicEye.Right, rightFarCorners);

            for (i = 0; i < 4; i++)
            {
                leftNearCorners[i] = leftLocalTransform.MultiplyPoint3x4(leftNearCorners[i]);
                leftFarCorners[i] = leftLocalTransform.MultiplyPoint3x4(leftFarCorners[i]);
                rightNearCorners[i] = rightLocalTransform.MultiplyPoint3x4(rightNearCorners[i]);
                rightFarCorners[i] = rightLocalTransform.MultiplyPoint3x4(rightFarCorners[i]);
            }
#if DEBUG_LINE
          for (int i = 0; i < 4; i++)
          {
              Debug.DrawLine(transform.TransformPoint(leftNearCorners[i]), leftEyeWorld.MultiplyPoint3x4(Vector3.zero), Color.yellow);
              Debug.DrawLine(transform.TransformPoint(rightNearCorners[i]), rightEyeWorld.MultiplyPoint3x4(Vector3.zero), Color.yellow);
              Debug.DrawLine(transform.TransformPoint(leftNearCorners[i]), transform.TransformPoint(leftFarCorners[i]), Color.red);
              Debug.DrawLine(transform.TransformPoint(rightNearCorners[i]), transform.TransformPoint(rightFarCorners[i]), Color.blue);
          }
#endif
            Vector3[] corners = new Vector3[8];
            corners[0] = transform.TransformPoint(new Vector3(Mathf.Min(leftNearCorners[0].x, rightNearCorners[0].x), Mathf.Min(leftNearCorners[0].y, rightNearCorners[0].y), leftNearCorners[0].z));
            corners[1] = transform.TransformPoint(new Vector3(Mathf.Min(leftNearCorners[1].x, rightNearCorners[1].x), Mathf.Max(leftNearCorners[1].y, rightNearCorners[1].y), leftNearCorners[1].z));
            corners[2] = transform.TransformPoint(new Vector3(Mathf.Max(leftNearCorners[2].x, rightNearCorners[2].x), Mathf.Max(leftNearCorners[2].y, rightNearCorners[2].y), leftNearCorners[2].z));
            corners[3] = transform.TransformPoint(new Vector3(Mathf.Max(leftNearCorners[3].x, rightNearCorners[3].x), Mathf.Min(leftNearCorners[3].y, rightNearCorners[3].y), leftNearCorners[3].z));

            corners[4] = transform.TransformPoint(new Vector3(Mathf.Min(leftFarCorners[0].x, rightFarCorners[0].x), Mathf.Min(leftFarCorners[0].y, rightFarCorners[0].y), leftFarCorners[0].z));
            corners[5] = transform.TransformPoint(new Vector3(Mathf.Min(leftFarCorners[1].x, rightFarCorners[1].x), Mathf.Max(leftFarCorners[1].y, rightFarCorners[1].y), leftFarCorners[1].z));
            corners[6] = transform.TransformPoint(new Vector3(Mathf.Max(leftFarCorners[2].x, rightFarCorners[2].x), Mathf.Max(leftFarCorners[2].y, rightFarCorners[2].y), leftFarCorners[2].z));
            corners[7] = transform.TransformPoint(new Vector3(Mathf.Max(leftFarCorners[3].x, rightFarCorners[3].x), Mathf.Min(leftFarCorners[3].y, rightFarCorners[3].y), leftFarCorners[3].z));
#if DEBUG_LINE
            for (int i = 0; i < 4; i++)
            {
                //Debug.DrawLine(corners[i], corners[i + 4], Color.yellow);
                Debug.DrawLine(corners[i], corners[(i+1)%4], Color.yellow);
                Debug.DrawLine(corners[i+4], corners[((i + 1) % 4)+4], Color.yellow);
            }
#endif
            viewfrustum = CalculateFrustumPlanes(corners);
            SetBounds(viewboundelement, corners);
        }
        else
#endif
        {
            viewfrustum = GeometryUtility.CalculateFrustumPlanes(m_Camera);
            CalculateFrustumBounds(viewboundelement, m_Camera);
        }

        Matrix4x4 viewtm = Matrix4x4.identity;
        viewtm.m00 = viewtm.m11 = viewtm.m03 = viewtm.m13 = 0.5f;
#if VR_ENABLE
        Vector3 position1, position2;
        if (m_VREnable)
        {
            Matrix4x4 view1 = m_Camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
            Matrix4x4 view2 = m_Camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

            position1 = view1.inverse.MultiplyPoint3x4(Vector3.zero);
            position2 = view2.inverse.MultiplyPoint3x4(Vector3.zero);

            m_ViewLeft = viewtm * m_Camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) * view1;
            m_ViewRight = viewtm * m_Camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right) * view2;
        }
        else
        {
            position1 = position2 = m_Camera.transform.position;

            m_ViewMono = viewtm * m_Camera.projectionMatrix * m_Camera.worldToCameraMatrix;
        }
#else
        m_ViewMono = viewtm * m_Camera.projectionMatrix * m_Camera.worldToCameraMatrix;
#endif
        m_CameraPosition = m_Camera.transform.position;

        RT.Occluder[] occluderindistance = UpdateOccluderInDistanceList(m_CameraPosition);
        Vector3 forward = m_Camera.transform.forward;
        float plane_d = Vector3.Dot(forward, m_Camera.transform.position);

        List<int> reorder = new List<int>(occluderindistance.Length);

        RT.Occluder[] occluder = new RT.Occluder [occluderindistance.Length];
        float[] distance = new float [occluderindistance.Length];
        int pos = 0;
        float lastdistance = 0.0f;
        bool sorted = true;

        for (i = 0; i < occluderindistance.Length; i++)
        {
            RT.Occluder occ = occluderindistance[i];
            float[] b = occ.GetBounds();

            float neardistance = IgnoreDistance(b, forward, plane_d, m_IgnoreDistance); // far from the camera or behind the camera
            if (neardistance < 0)
            {
                occluderindistance[pos++] = occ;
                continue;
            }

            int idx = reorder.Count;
            reorder.Add(idx);
            occluder[idx] = m_OccluderInDistance[i];
            distance[idx] = neardistance;

            if (neardistance < lastdistance)
                sorted = false;
            lastdistance = neardistance;
        }

        if (sorted == false)
        {
            reorder.Sort(delegate (int i1, int i2) { return distance[i1].CompareTo(distance[i2]); });
        }

        int count = 0;
        int maxcount = m_MaxOccluderCount == 0 ? 31 : Mathf.Min(31, m_MaxOccluderCount);
        for (i = 0; i < reorder.Count && count < maxcount; i++)
        {
            RT.Occluder occ = occluder[reorder[i]];
            occluderindistance[pos++] = occ;
#if VR_ENABLE
            if (occ.MakeFrustum(m_Camera, viewfrustum, m_VREnable, m_VREnable ? m_ViewLeft : m_ViewMono, m_ViewRight, position1, position2, changed))
#else
            if (occ.MakeFrustum(m_Camera, viewfrustum, m_ViewMono, changed))
#endif
            {
                float[] boundelement = occ.GetBounds();

                int j;
                for (j = 0; j < count; j++)
                {
                    if (m_ActiveOccluder[j].CullTest(boundelement))
                        break;
                }
                if (j == count)
                {
                    m_ActiveOccluder[count] = occ;
                    count++;
                }
            }
        }
        for (; i < reorder.Count; i++)
        {
            occluderindistance[pos++] = occluder[reorder[i]];
        }
#if DETECT_CROSS_RECT
        for (i = 0; i < count; i++)
        {
            float[] boundelement = m_ActiveOccluder[i].GetBounds();
#if VR_ENABLE
            if (m_VREnable)
                m_RectOccluer[i] = ExpandRect(CalcRect(m_ViewLeft, boundelement), CalcRect(m_ViewRight, boundelement));
            else
#endif
                m_RectOccluer[i] = CalcRect(m_ViewMono, boundelement);
        }

        for (i = 0; i < count; i++)
        {
            int mask = 0;
            for (int j = i + 1; j < count; j++)
            {
                if (m_RectOccluer[i].Overlaps(m_RectOccluer[j]))
                    mask |= 1 << j;
            }
            m_OverlapMask[i] = mask;
        }
#endif
        if (count < m_ActiveOccluder.Length)
            m_ActiveOccluder[count] = null;

        m_ViewFrustumElement = RT.Occluder.GetPlaneElement(viewfrustum);

        for (i = 0; i < m_Occludee.Count; i++)
            m_Occludee[i].UpdateOcclusion(this, m_ViewFrustumElement, viewboundelement, m_CullTestDelay);
#if UNITY_EDITOR
        m_ViewFrustum = viewfrustum;
#endif
    }

    float[] m_ViewFrustumElement;

    public bool CullTest(Bounds bounds)
    {
        if (m_ViewFrustumElement == null)
            return false;
        float[] boundselement = new float[] { bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y, bounds.min.z, bounds.max.z };
        bool infrustum = RT.Occludee.TestPlanesAABB(m_ViewFrustumElement, boundselement);
        return infrustum ? CullTest(boundselement) : true;
    }

    public bool CullTest(float[] boundelement)
    {
        for (int i = 0; i < m_ActiveOccluder.Length && m_ActiveOccluder[i] != null; i++)
        {
#if DETECT_CROSS_RECT
            int mask = m_OverlapMask[i];
            if (mask == 0)
            {
                if (m_ActiveOccluder[i].CullTest(boundelement))
                    return true;
            }
            else
            {
                int state = m_ActiveOccluder[i].CullTestWithClip(boundelement);
                if (state == -1)
                    return true;
                if (state >= 0)
                {
#if VR_ENABLE
                    if (m_VREnable)
                    {
                        Rect rect1 = CalcRect(m_ViewLeft, boundelement);
                        Rect rect2 = CalcRect(m_ViewRight, boundelement);

                        int rectstate = (m_ActiveOccluder[i].ClipRect(ref rect1, state, false) ? 1 : 0) |
                            (m_ActiveOccluder[i].ClipRect(ref rect2, state, true) ? 2 : 0);

                        for (i++; i < m_ActiveOccluder.Length && m_ActiveOccluder[i] != null; i++)
                        {
                            if ((mask & (1 << i)) != 0 &&
                                (((rectstate&1) != 0 && m_RectOccluer[i].Overlaps(rect1)) || ((rectstate&2) != 0 && m_RectOccluer[i].Overlaps(rect2))))
                            {
                                if (m_ActiveOccluder[i].CullTestWithClip(boundelement, ref rect1, ref rect2, ref rectstate) == -1)
                                    return true;
                            }
                        }
                    }
                    else
#endif
                    {
                        Rect rect = CalcRect(m_ViewMono, boundelement);
#if VR_ENABLE
                        m_ActiveOccluder[i].ClipRect(ref rect, state, false);
#else
                        m_ActiveOccluder[i].ClipRect(ref rect, state);
#endif
                        for (i++; i < m_ActiveOccluder.Length && m_ActiveOccluder[i] != null; i++)
                        {
                            if ((mask & (1 << i)) != 0 && m_RectOccluer[i].Overlaps(rect))
                            {
                                if (m_ActiveOccluder[i].CullTestWithClip(boundelement, ref rect) == -1)
                                    return true;
                            }
                        }
                    }
                    return false;
                }
            }
#else
            if (m_ActiveOccluder[i].CullTest(boundelement))
                return true;
#endif

        }
        return false;
    }

    private void OnPostRender()
    {
        if (m_CullEvent == CullEvent.OnPrecullAndRestoreOnPostRender)
        {
            for (int i = 0; i < m_Occludee.Count; i++)
                m_Occludee[i].ResetVisible();
        }
    }

#region calculate frustum planes
    static Plane[] CalculateFrustumPlanes(Transform transform, Vector3[] nearCorner, Vector3[] farCorner)
    {
        Vector3[] vtx = new Vector3[8];
        for (int i = 0; i < 4; i++)
        {
            vtx[i] = transform.TransformPoint(nearCorner[i]);
            vtx[i + 4] = transform.TransformPoint(farCorner[i]);
        }
        return CalculateFrustumPlanes(vtx);
    }

    static readonly int[] _frustum = { 0, 1, 5, 2, 3, 7, 3, 0, 4, 1, 2, 6, 0, 2, 1, 4, 5, 6 };

    static Plane[] CalculateFrustumPlanes(Vector3[] vtx)
    {
        Plane[] plane = new Plane[6];
        for (int i = 0, off = 0; i < 6; i++, off += 3)
            plane[i] = new Plane(vtx[_frustum[off]], vtx[_frustum[off + 1]], vtx[_frustum[off + 2]]);
        return plane;
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

    static public void SetBounds(float[] boundselement, Vector3[] vertex, Matrix4x4 localToWorld)
    {
        Vector3 v = localToWorld.MultiplyPoint3x4(vertex[0]);
        boundselement[0] = boundselement[1] = v.x;
        boundselement[2] = boundselement[3] = v.y;
        boundselement[4] = boundselement[5] = v.z;
        Encapsulate(boundselement, vertex, localToWorld, 1, vertex.Length-1);
    }

    static public void Encapsulate(float[] boundselement, Vector3[] vertex, Matrix4x4 localToWorld)
    {
        Encapsulate(boundselement, vertex, localToWorld, 0, vertex.Length);
    }

    static void Encapsulate(float[] boundselement, Vector3[] vertex, Matrix4x4 localToWorld, int off, int len)
    {
        for (int i = off; i < off+len; i++)
        {
            Vector3 v = localToWorld.MultiplyPoint3x4(vertex[i]);
            if (v.x < boundselement[0]) boundselement[0] = v.x;
            if (v.x > boundselement[1]) boundselement[1] = v.x;
            if (v.y < boundselement[2]) boundselement[2] = v.y;
            if (v.y > boundselement[3]) boundselement[3] = v.y;
            if (v.z < boundselement[4]) boundselement[4] = v.z;
            if (v.z > boundselement[5]) boundselement[5] = v.z;
        }
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
#endregion

#if UNITY_EDITOR
    Plane[] m_ViewFrustum;

    public void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        SceneView sceneview = SceneView.currentDrawingSceneView;
        if ((sceneview != null && Camera.current != sceneview.camera) || (sceneview == null && Camera.current != m_Camera))
            return;

        if (enabled == false)
            return;

        List<Vector3> vtx = new List<Vector3>();
        List<Vector3> point = new List<Vector3>();

        for (int i=0; i < m_ActiveOccluder.Length && m_ActiveOccluder[i] != null; i++)
            m_ActiveOccluder[i].GatherOcclusionEdge(m_Camera, vtx, point);

        if (vtx.Count > 0)
        {
            Vector3 forward = m_Camera.transform.forward;
#if VR_ENABLE
            if (m_VREnable)
            {
                Matrix4x4 viewtm = m_Camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
                forward = viewtm.MultiplyVector(-Vector3.forward);
            }
#endif
            float farplane = m_Camera.farClipPlane;
            int quadvtxcnt = vtx.Count;

            for (int i = 0; i < quadvtxcnt; i += 2)
            {
                for (int j = 0; j < 4; j++)
                {
                    float d1 = m_ViewFrustum[j].GetDistanceToPoint(vtx[i+0]);
                    float d2 = m_ViewFrustum[j].GetDistanceToPoint(vtx[i+1]);

                    if (d1 * d2 < 0)
                        vtx[i + (d1 < 0 ? 0 : 1)] = Vector3.Lerp(vtx[i + 0], vtx[i + 1], d1 / (d1 - d2));
                }

                Vector3 position = point[i / 2];
                float dot2 = Vector3.Dot(vtx[i+1] - position, forward);
                float dot1 = Vector3.Dot(vtx[i] - position, forward);
                if (m_Camera.orthographic)
                {
                    vtx.Add(vtx[i+1] + forward * (farplane - dot2));
                    vtx.Add(vtx[i] + forward * (farplane - dot1));
                }
                else
                {
                    vtx.Add(position + (vtx[i + 1] - position) * (farplane / dot2));
                    vtx.Add(position + (vtx[i] - position) * (farplane / dot1));
                }
            }

            if (Camera.current == m_Camera)
            {
                for (int i = 0; i < quadvtxcnt; i += 2)
                    Handles.DrawLine(vtx[i + 0], vtx[i + 1]);
            }
            else
            {
                List<int> triangles = new List<int>((vtx.Count - 2) * 3);
                for (int i = 0; i < quadvtxcnt; i += 2)
                {
                    for (int j = 2; j < 4; j++)
                    {
                        triangles.Add(i);
                        triangles.Add(i + j + (j < 2 ? 0 : quadvtxcnt -2 ));
                        triangles.Add(i + j - 1 + (j - 1 < 2 ? 0 : quadvtxcnt - 2));
                    }
                    Handles.DrawLine(vtx[i + 0], vtx[i + 1]);
                    Handles.DrawLine(vtx[i + 0], vtx[i + quadvtxcnt + 1]);
                    Handles.DrawLine(vtx[i + 1], vtx[i + quadvtxcnt]);
                }

                Mesh m = new Mesh();
                m.hideFlags = HideFlags.HideAndDontSave;
                m.vertices = vtx.ToArray();
                m.SetTriangles(triangles, 0);
                m.RecalculateNormals();

                if (ToolAsset.material.SetPass(0))
                    Graphics.DrawMeshNow(m, Matrix4x4.identity);

                DestroyImmediate(m);
            }
        }

        for (int i = 0; i < m_Occludee.Count; i++)
        {
            m_Occludee[i].DrawCulledBound();
        }
    }

    public void OnChanged()
    {
#if UPDATE_IF_CHANGED
        m_LastLocalToWorld = Matrix4x4.identity;
#endif
    }

    static class ToolAsset
    {
        static public Material material;

        static ToolAsset()
        {
            material = new Material(Shader.Find("Hidden/Internal-Occlusion"));
            material.SetColor("_Color", new Color(1, 1, 0, 0.25f));
            material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        }
    };
#endif
}