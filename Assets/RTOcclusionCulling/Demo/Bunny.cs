using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5_3_OR_NEWER
using UnityEngine.AI;
#endif

public class Bunny : MonoBehaviour
{    
    enum State
    {
        Idle,
        Move,
    };

    State m_State = State.Idle;
    float m_Timer;    
    NavMeshAgent m_NavMeshAgent;
    Animator m_Animator;

    private void Awake()
    {
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponent<Animator>();
    }

    public void Start()
    {
        Vector3 position = transform.position + new Vector3(UnityEngine.Random.Range(0, 100), 0, UnityEngine.Random.Range(0, 100));
        Vector3 result;
        RandomPosition(position, 10.0f, out result);
        m_State = State.Idle;
        m_Timer = UnityEngine.Random.Range(0, 3);
        m_NavMeshAgent.Warp(result);
        transform.position = result;
    }

    private void Update()
    {
        m_Timer += Time.deltaTime;

        if (m_State == State.Idle)
        {
            if (m_Timer >= 3.0f)
            {
                m_Timer = 0;
                float range = 20;
                Vector3 position = transform.position + new Vector3(UnityEngine.Random.Range(-range, range), 0, UnityEngine.Random.Range(-range, range));
                Vector3 result;
                if (RandomPosition(position, 10.0f, out result))
                {
                    m_State = State.Move;
                    m_Animator.SetBool("move", true);
                    m_NavMeshAgent.SetDestination(result);
                }
            }
        }

        if (m_State == State.Move)
        {
            if (m_NavMeshAgent.pathPending == false && m_NavMeshAgent.hasPath == false)
            {
                m_State = State.Idle;
                m_Timer = 0;
                m_Animator.SetBool("move", false);
            }
        }
    }

    bool RandomPosition(Vector3 position, float range, out Vector3 result)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, range, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        result = Vector3.zero;
        return false;
    }
}