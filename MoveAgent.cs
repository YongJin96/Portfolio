using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveAgent : MonoBehaviour
{
    private NavMeshAgent Agent;

    public List<Transform> WayPoints;
    public int NextIndex;
    public float PatrolSpeed = 1f;
    public float TraceSpeed = 2f;
    public bool RandomPatrol = false;

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

    private void TraceTargetFunc(Vector3 pos)
    {
        if (Agent.isPathStale) return;

        Agent.destination = pos;
        Agent.isStopped = false;
    }

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Agent.autoBraking = false;
        Agent.speed = PatrolSpeed;

        var group = GameObject.Find("WayPointGroup");
        
        if (group != null)
        {
            if (RandomPatrol == true) // true값이면 랜덤으로 순찰
            {
                group.GetComponentsInChildren<Transform>(WayPoints);
                WayPoints.RemoveAt(0); // 부모의 위치도 저장되서 부모의 인덱스를 삭제
        
                NextIndex = Random.Range(0, WayPoints.Count);
            }
        
        }

        MoveWayPoint();
    }

    private void Update()
    {
        if (_Patrolling == false) return;

        // NavMeshAgent가 이동하고 있고 목적지에 도착했는지 여부를 계산
        if (Agent.velocity.sqrMagnitude >= 0.2f * 0.2f && Agent.remainingDistance <= 0.5f)
        {
            // 다음 목적지로
            NextIndex = ++NextIndex % WayPoints.Count;

            if (RandomPatrol == true)
            {
                NextIndex = Random.Range(0, WayPoints.Count); // 랜덤하게 순찰
            }

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
