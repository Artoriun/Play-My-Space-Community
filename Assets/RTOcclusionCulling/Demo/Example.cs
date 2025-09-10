using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour
{
    public MeshRenderer[] m_MeshRenderers;

    RT.Occludee m_Occludee;
    RT.Occluder m_Occluder;

    private void Awake()
    {
        float scale = UnityEngine.Random.Range(1, 4);
        transform.position = new Vector3(UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(1, 4), UnityEngine.Random.Range(-20, 20));
        transform.localScale = Vector3.one * scale;

        m_Occludee = new RT.Occludee();

        bool shadowonly = false;
        m_Occludee.SetMeshRenderer(m_MeshRenderers, transform, !shadowonly, 0, false, false, false);

        if (scale >= 2)
        {
            m_Occluder = new RT.Occluder();
            m_Occluder.SetBoxVolume(new Bounds(Vector3.zero, Vector3.one), transform.localToWorldMatrix);
            m_Occluder.SetTransform(transform);
        }
    }

    void OnEnable()
    {
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        rigidbody.velocity = Quaternion.Euler(UnityEngine.Random.Range(-10, 10), UnityEngine.Random.Range(0, 360), 0) * (Vector3.up * UnityEngine.Random.Range(2, 3));

        m_Occludee.Enable();
        if (m_Occluder != null)
            m_Occluder.Enable();
    }

    private void OnDisable()
    {
        m_Occludee.Disable();
        if (m_Occluder != null)
            m_Occluder.Disable();
    }
}