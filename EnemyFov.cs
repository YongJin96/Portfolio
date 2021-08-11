using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFov : MonoBehaviour
{
    #region Variables

    private Transform EnemyTransform;
    private Transform PlayerTransform;
    private int PlayerLayer;
    private int NPCLayer;
    private int ObstacleLayer;
    private int layerMask;

    public float ViewRange = 15;
    public float ViewAngle = 120f;

    #endregion

    #region Initialization

    void Start()
    {
        EnemyTransform = GetComponent<Transform>();
        PlayerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        
        PlayerLayer = LayerMask.NameToLayer("Player");
        NPCLayer = LayerMask.NameToLayer("NPC");
        ObstacleLayer = LayerMask.NameToLayer("Obstacle");
        layerMask = 1 << PlayerLayer | 1 << ObstacleLayer | 1 << LayerMask.NameToLayer("Wall");
    }

    #endregion

    #region Functions

    public Vector3 CirclePoint(float angle)
    {
        angle += transform.eulerAngles.y;

        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
    }

    public bool IsTracePlayer()
    {
        bool isTrace = false;

        Collider[] colls = Physics.OverlapSphere(EnemyTransform.position, ViewRange, 1 << PlayerLayer);

        foreach (var coll in colls)
        {
            if (coll.gameObject.CompareTag("Player"))
            {
                // 적 캐릭터와 플레이어 사이의 방향 벡터를 계산
                Vector3 dir = (PlayerTransform.position - EnemyTransform.position).normalized;

                if (Vector3.Angle(EnemyTransform.forward, dir) < ViewAngle * 0.5f)
                {
                    isTrace = true;
                }
            }
        }

        return isTrace;
    }

    public bool IsTraceNPC()
    {
        bool isTrace = false;
    
        Collider[] colls = Physics.OverlapSphere(EnemyTransform.position, ViewRange, 1 << NPCLayer);
    
        foreach (var coll in colls)
        {
            if (coll.gameObject.CompareTag("NPC"))
            {
                // 적 캐릭터와 NPC 사이의 방향 벡터를 계산
                Vector3 dir = (transform.GetComponent<EnemyAI_Mongol_NPC>().NPCTransform.position - EnemyTransform.position).normalized;
    
                if (Vector3.Angle(EnemyTransform.forward, dir) < ViewAngle * 0.5f)
                {
                    isTrace = true;
                }
            }
        }
    
        return isTrace;
    }

    public bool IsViewPlayer()
    {
        bool isView = false;
        RaycastHit hit;
    
        Vector3 dir = (PlayerTransform.position - EnemyTransform.position).normalized;
    
        if (Physics.Raycast(EnemyTransform.position + transform.TransformDirection(0f, 0.5f, 0f), dir, out hit, ViewRange, layerMask))
        {
            isView = (hit.collider.CompareTag("Player"));
        }

        return isView;
    }

    #endregion
}
