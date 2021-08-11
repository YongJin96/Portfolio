using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveAgent_NPC : MonoBehaviour
{
    private NavMeshAgent Agent;

    public List<Transform> WayPoints;
    public int NextIndex;
    public float PatrolSpeed = 0.5f;
    public float TraceSpeed = 1f;

    private bool _Patrolling;
    public bool Patrolling
    {
        get { return _Patrolling; }
        set
        {
            if (_Patrolling)
            {
                Agent.speed = PatrolSpeed;
                MoveWayPoint();
            }
            _Patrolling = value;
        }
    }

    public float Speed
    {
        get { return Agent.velocity.magnitude; }
    }

    private Vector3 _TraceTarget;
    public Vector3 TraceTarget
    {
        get { return _TraceTarget; }
        set
        {
            _TraceTarget = value;
            Agent.speed = TraceSpeed;
            TraceTargetFunc(_TraceTarget);
        }
    }

    void TraceTargetFunc(Vector3 pos)
    {
        if (Agent.isPathStale) return;

        Agent.destination = pos;
        Agent.isStopped = false;
    }

    void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Agent.autoBraking = false;
        Agent.speed = PatrolSpeed;

        //var group = GameObject.Find("WayPointGroup_NPC");
        //
        //if (group != null)
        //{
        //    group.GetComponentsInChildren<Transform>(WayPoints);
        //    WayPoints.RemoveAt(0); // 부모의 위치도 저장되서 부모의 인덱스를 삭제
        //}

        MoveWayPoint();
    }

    void Update()
    {
        if (_Patrolling == false) return;

        // NavMeshAgent가 이동하고 있고 목적지에 도착했는지 여부를 계산
        if (Agent.velocity.sqrMagnitude >= 0.2f * 0.2f && Agent.remainingDistance <= 0.5f)
        {
            // 다음 목적지로
            NextIndex = ++NextIndex % WayPoints.Count;

            MoveWayPoint();
        }
    }

    private void MoveWayPoint()
    {
        if (_Patrolling == false) return;

        // 최단 거리 경로 계산이 끝나지 않았으면 return
        if (Agent.isPathStale) return;

        Agent.destination = WayPoints[NextIndex].position;
        Agent.isStopped = false;
    }

    public void Stop()
    {
        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
        _Patrolling = false;
    }
}
