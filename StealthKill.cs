using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StealthKill : MonoBehaviour
{
    #region Variables

    private Transform PlayerPos;

    // 기본 Enemy Scripts
    private EnemyAI_Mongol enemyAi;
    private InteractionUI EnemyUI;

    // 보스 Scripts
    private BossAI Boss;

    public Animator Anim;
    public Transform KillPosition;

    public QuestWayPoint WayPoint;

    static public bool OnKill = false;
    public bool IsBoss = false;

    #endregion

    #region Initialization

    private void Start()
    {
        if (IsBoss == false)
        {
            enemyAi = GetComponent<EnemyAI_Mongol>();
            EnemyUI = GetComponent<InteractionUI>();
        }
        else if (IsBoss == true)
        {
            Boss = GetComponent<BossAI>();
        }
    }

    #endregion

    #region Functions

    public void KillAnimation()
    {
        if (IsBoss == false)
        {
            Anim.SetTrigger("Kill");
            enemyAi.StealthDie();
        }
        else if (IsBoss == true && Boss.FinishCheck == true)
        {
            Anim.SetTrigger("Finish");
            if (Boss.Phase == BossAI.BossPhase.PHASE_3)
            {
                Boss.Die();
            }
        }
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player"))
        {
            coll.GetComponent<Stealth_KillBehaviour>().Enemy = this;
            coll.GetComponent<Stealth_KillBehaviour>().KillPosition = KillPosition;
            PlayerPos = coll.transform;

            if (IsBoss == false)
            {
                EnemyUI.ViewUI(true);
                WayPoint.Target = this.transform.Find("StealthKill_UI_Transform"); // 암살 키 UI 위치 설정
            }

            OnKill = true; // 캐릭터 E스킬이 같이나가서 안나가기 위해 조건걸음
        }
    }

    private void OnTriggerExit(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player"))
        {
            coll.GetComponent<Stealth_KillBehaviour>().Enemy = null;
            coll.GetComponent<Stealth_KillBehaviour>().KillPosition = null;
            PlayerPos = null;

            if (IsBoss == false)
            {
                EnemyUI.ViewUI(false);
            }

            OnKill = false;
        }
    }

    #endregion
}
