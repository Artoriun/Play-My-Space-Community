using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CarMovement : MonoBehaviour
{
    public List<Vector3> m_Points = new List<Vector3>();

    Vector3[] m_LineList;
    float[] m_DistanceList;

    void Awake()
    {
        UpdatePath(16);
    }

    public Vector3 GetPosition(float d)
    {
        float len = m_DistanceList[m_DistanceList.Length-1];
        if (d < 0)
            d = len - (-d % len);
        else
            d %= len;
        return GetPosition(d, m_DistanceList, m_LineList);
    }

    static float DistancePointToLineSegment(Vector3 p0, Vector3 p1, Vector3 pos, out float distance)
    {
        Vector3 v = p1 - p0;
        float w = Mathf.Clamp01(Vector3.Dot(pos - p0, v) / Vector3.Dot(v, v));
        distance = Vector3.Magnitude((p0 + v * w) - pos);
        return w;
    }

    public float GetPosition(Vector3 p)
    {
        float nearone = 10000;
        float position = 0;

        for (int i = 0; i < m_LineList.Length; i++)
        {
            float distance;
            float w = DistancePointToLineSegment(m_LineList[i], m_LineList[(i + 1) % m_LineList.Length], p, out distance);
            if (i == 0 || distance < nearone)
            {
                float left = i == 0 ? 0 : m_DistanceList[i-1];
                float right = m_DistanceList[i];
                position = Mathf.Lerp(left, right, w);
                nearone = distance;
            }
        }
        return position;
    }

    void UpdatePath(int segment)
    {
        m_LineList = new Vector3[m_Points.Count * segment];

        for (int i = 0; i < m_Points.Count; i++)
        {
            Vector3 p0 = m_Points[(i - 1 + m_Points.Count) % m_Points.Count];
            Vector3 p1 = m_Points[i % m_Points.Count];
            Vector3 p2 = m_Points[(i + 1) % m_Points.Count];
            Vector3 p3 = m_Points[(i + 2) % m_Points.Count];

            Vector3 p = p1;

            for (int n = 1, off = i * segment; n <= segment; n++, off++)
            {
                m_LineList[off] = transform.TransformPoint(p);

                p = CatmullRom(p0, p1, p2, p3, (float)n / segment);
            }
        }

        m_DistanceList = new float[m_Points.Count * segment];
        float total = 0;
        for (int i = 0; i < m_LineList.Length; i++)
        {
            float distance = Vector3.Magnitude(m_LineList[(i + 1) % m_LineList.Length] - m_LineList[i]);
            total += distance;
            m_DistanceList[i] = total;
        }
    }

    Vector3 GetPosition(float d, float[] distance, Vector3[] linelist)
    {
        while (d < 0)
            d += distance[distance.Length - 1];

        d = d % distance[distance.Length - 1];

        int i = 0;
        for (i = 0; i < distance.Length; i++)
        {
            float d1 = i == 0 ? 0 : distance[i - 1];
            float d2 = distance[i];

            if (d >= d1 && d < d2)
                break;
        }

        float d3 = i == 0 ? 0 : distance[i - 1];
        float d4 = distance[i];

        float w = (d - d3) / (d4 - d3);
        Vector3 pos = Vector3.Lerp(linelist[i], linelist[(i + 1) % linelist.Length], w);
        return pos;
    }

    public static Vector3 CatmullRom(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
    {
        float t2 = t * t;
        float t3 = t * t * t;
        return ((-t3 + 2 * t2 - t) * 0.5f) * p1 + ((3 * t3 - 5 * t2 + 2) * 0.5f) * p2 + ((-3 * t3 + 4 * t2 + t) * 0.5f) * p3 + ((t3 - t2) * 0.5f) * p4;
    }

    public class Car
    {
        public float distance;
        public float speed;
        public float maxspeed;
        public float accel;
        public Transform transform;
        public Transform[] wheel;
    }

    public Transform[] m_Transform;

    public float m_MaxSpeed = 8;
    public float m_BrakeDistance = 12;
    public float m_BrakeSpeed = 6;
    public float m_AccelSpeed = 8;

    List<Car> m_Car = new List<Car>();

    void OnEnable()
    {
        m_Car.Clear();

        for (int i = 0; i < m_Transform.Length; i++)
        {
            int j = 0; 
            for (j = 0; j < m_Car.Count; j++)
            {
                if (m_Car[j].transform == m_Transform[i])
                    break;
            }
            if (j < m_Car.Count)
                continue;

            Car c = new Car();
            c.distance = GetPosition(m_Transform[i].position); // 현재위치 구하기
            c.speed = 0;
            c.maxspeed = m_MaxSpeed * Random.Range(0.9f, 1.1f);
            c.accel = m_AccelSpeed * Random.Range(0.9f, 1.1f);
            c.transform = m_Transform[i];

            List<Transform> wheel = new List<Transform>();
            for (j = 0; j < m_Transform[i].childCount; j++)
                if (m_Transform[i].GetChild(j).name.IndexOf("Wheel") != -1)
                    wheel.Add(m_Transform[i].GetChild(j));
            c.wheel = wheel.ToArray();
            m_Car.Add(c);
        }

        m_Car.Sort(delegate (Car c1, Car c2)
        {
            if (c1.distance < c2.distance)
                return -1;
            else if (c1.distance > c2.distance)
                return 1;
            return 0;
        });
    }

    void Update()
    {
        for (int i = 0; i < m_Car.Count; i++)
        {
            Vector3 nextcarposition = m_Car[(i + 1) % m_Car.Count].transform.position;
            float accel = m_Car.Count > 1 && Vector3.Magnitude(nextcarposition - m_Car[i].transform.position) <= m_BrakeDistance ? -m_BrakeSpeed : m_Car[i].accel;

            Vector3 forwardposition = GetPosition(m_Car[i].distance + 20);
            float curveweight = Mathf.Clamp(Vector3.Dot((forwardposition - m_Car[i].transform.position).normalized, m_Car[i].transform.forward), 0.5f, 1.0f);

            m_Car[i].speed = Mathf.Clamp(m_Car[i].speed + accel * Time.deltaTime, 0, m_Car[i].maxspeed * curveweight);
            float distance = Time.deltaTime * m_Car[i].speed;
            m_Car[i].distance += distance;
            Vector3 currentposition = GetPosition(m_Car[i].distance);

            float wheelradius = 0.3f;
            float rad = Vector3.Magnitude(currentposition - m_Car[i].transform.forward * 2.0f - m_Car[i].transform.position) / (wheelradius * Mathf.PI * 2);

            for (int j = 0; j < m_Car[i].wheel.Length; j++)
                m_Car[i].wheel[j].localRotation *= Quaternion.Euler(new Vector3(rad * Mathf.Rad2Deg, 0, 0));

            Vector3 prevposition = GetPosition(m_Car[i].distance - 4.0f);
            m_Car[i].transform.rotation = Quaternion.LookRotation(currentposition - prevposition);
            m_Car[i].transform.position = currentposition - m_Car[i].transform.forward * 2.0f;
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(CarMovement))]
    public class CarMovementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(8);

            if (GUILayout.Button("Reverse", GUILayout.Height(30.0f)))
            {
                Undo.RecordObject(target, "Reverse");
                CarMovement _target = (CarMovement)target;
                List<Vector3> points = new List<Vector3>();
                for (int i = 0; i < _target.m_Points.Count; i++)
                    points.Insert(0, _target.m_Points[i]);
                _target.m_Points = points;
            }
        }
        void OnSceneGUI()
        {
            Event evt = Event.current;
            
            if (Application.isPlaying && evt.type != EventType.Repaint)
                return;

            CarMovement _target = (CarMovement)target;
            SceneView sceneview = SceneView.currentDrawingSceneView;

            if (evt.type == EventType.Repaint)
            {
                Vector3[] points = _target.m_Points.ToArray();

                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 p0 = _target.transform.TransformPoint(points[(i - 1 + points.Length) % points.Length]);
                    Vector3 p1 = _target.transform.TransformPoint(points[i]);
                    Vector3 p2 = _target.transform.TransformPoint(points[(i + 1) % points.Length]);
                    Vector3 p3 = _target.transform.TransformPoint(points[(i + 2) % points.Length]);

                    float unitsize = WorldSize(10, sceneview.camera, p1) * 0.5f;
                    Handles.color = Color.white;
                    Handles.DrawSolidDisc(p1, _target.transform.up, unitsize);
                    //Handles.Label(p1, "" + i);

                    Handles.color = Color.green;
                    //Handles.DrawLine(p1, p2);

                    Vector3 v0 = p1;
                    for (int n = 1; n <= 12; n++)
                    {
                        Vector3 v1 = CatmullRom(p0, p1, p2, p3, (float)n / 12);
                        Handles.DrawLine(v0, v1);
                        v0 = v1;
                    }
                }
            }
            else if (evt.type == EventType.Layout)
            {                
                if (evt.control == true)
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
            else if (evt.type == EventType.MouseDown && evt.button == 0 && evt.control == true)
            {
                Ray mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);

                Vector3 mouseposition = IntersectPlane(_target.transform.up, _target.transform.position, mouseRay);

                float unitsize = WorldSize(10, sceneview.camera, mouseposition) * 0.5f;

                for (int i = 0; i < _target.m_Points.Count; i++)
                {
                    if (DistanceRayToPoint(_target.transform.TransformPoint(_target.m_Points[i]), mouseRay) < unitsize)
                    {
                        Undo.RecordObject(_target, "Remove Point");
                        _target.m_Points.RemoveAt(i);
                        HandleUtility.Repaint();
                        return;
                    }
                }

                Vector3 localMousePoisition = _target.transform.InverseTransformPoint(mouseposition);

                Undo.RecordObject(_target, "Add Point");
                int newindex = _target.m_Points.Count;
                float mindistance = -1;

                for(int i=0; i< _target.m_Points.Count; i++)
                {
                    float distance;
                    DistancePointToLineSegment(_target.m_Points[i], _target.m_Points[(i + 1) % _target.m_Points.Count], localMousePoisition, out distance);
                    if (mindistance == -1 || distance < mindistance)
                    {
                        mindistance = distance;
                        newindex = i+1;
                    }
                }

                _target.m_Points.Insert(newindex, localMousePoisition);
                HandleUtility.Repaint();
            }
        }

        static float DistanceRayToPoint(Vector3 p2, Ray ray)
        {
            Vector3 p = ray.origin + ray.direction * Vector3.Dot(p2 - ray.origin, ray.direction);
            return Vector3.Magnitude(p2 - p);
        }

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
    }
#endif
}