using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5_3_OR_NEWER
using UnityEngine.UI;
using UnityEngine.AI;
#endif

public class SampleController : MonoBehaviour
{
    public Camera m_Camera;

    public RectTransform m_Pad;
    public Camera m_UICamera;

    public float m_Speed = 0.75f;
    public NavMeshAgent m_Agent;

    Vector2 m_TouchRotatePosition;
    int m_MoveButton = -1;
    int m_RotateButton = -1;

    void Update()
    {
#if UNITY_EDITOR || UNITY_WEBGL || UNITY_STANDALONE_WIN
        if (Input.GetMouseButtonDown(0))
        {
            OnTouchDown(0, Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnTouchUp(0);
        }
        else if (Input.GetMouseButton(0))
        {
            OnTouchMove(0, Input.mousePosition);
        }
#else
		Touch[] touches = Input.touches;
		for (int i = 0; i < touches.Length; i++)
		{
			Touch touch = touches[i];

			if (touch.phase == TouchPhase.Began)
			{
				OnTouchDown(touch.fingerId, touch.position);
			}
			else if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
			{
				OnTouchUp(touch.fingerId);
			}
			else
			{
				OnTouchMove(touch.fingerId, touch.position);
			}
		}
#endif
        if (m_MoveButton == -1)
        {
            float xx = Input.GetAxis("Horizontal") * m_Speed * Time.deltaTime;
            float yy = Input.GetAxis("Vertical") * m_Speed * Time.deltaTime;

            if (xx != 0 || yy != 0)
                Move(xx, yy);
        }

        m_Camera.transform.position = transform.position;
        m_Camera.transform.rotation = Quaternion.Euler(m_RotationX, m_RotationY, 0);
    }

    void OnTouchDown(int fingerId, Vector2 mousePosition)
    {
        if (m_Pad != null)
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Pad, mousePosition, m_UICamera, out localpoint);
            if (m_Pad.rect.Contains(localpoint))
            {
                m_MoveButton = fingerId;
                return;
            }
        }
        if (m_RotateButton == -1)
        {
            m_RotateButton = fingerId;
            m_TouchRotatePosition = mousePosition;
        }
    }

    void OnTouchMove(int fingerId, Vector2 mousePosition)
    {
        if (m_MoveButton == fingerId)
        {
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Pad, mousePosition, m_UICamera, out localpoint);

            float d = Mathf.Min(localpoint.magnitude / (m_Pad.sizeDelta.x * 0.5f) * 1.2f - 0.2f, 1);
            Vector2 dir = localpoint.normalized * d * m_Speed * Time.deltaTime;
            Move(dir.x, dir.y);
        }
        if (m_RotateButton == fingerId)
        {                
            Vector2 dd = mousePosition - m_TouchRotatePosition;
            m_TouchRotatePosition = mousePosition;
            Rotate(dd.x, dd.y);
        }
    }

    void OnTouchUp(int fingerId)
    {
        if (fingerId == m_RotateButton)
            m_RotateButton = -1;

        if (fingerId == m_MoveButton)
            m_MoveButton = -1;
    }

    void Move(float x, float y)
    {
        Vector3 v = transform.TransformDirection(new Vector3(x, 0, y));
        if (m_Agent)
            m_Agent.Move(v);
        else
            transform.position += v;
    }

    float m_RotationX = 0;
    float m_RotationY = 0;

    void Rotate(float dx, float dy)
    {
        m_RotationX = Mathf.Clamp(m_RotationX - dy * 180.0f / Screen.width, -60, 60);
        m_RotationY += dx * 180.0f / 720.0f;// Screen.width;

        transform.rotation = Quaternion.Euler(0, m_RotationY, 0);
    }
}