using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawn : MonoBehaviour
{
    public GameObject m_Prefab;
    public int m_Count = 20;

    private void Start()
    {
        for (int i = 0; i < m_Count; i++)
            GameObject.Instantiate(m_Prefab);
    }
}