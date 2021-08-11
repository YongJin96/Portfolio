using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stealth_KillBehaviour : MonoBehaviour
{
    #region Variables

    private Animator Anim;
    private PlayerMovement playerMovement;
    private PlayerStat playerStat;
    private CharacterController PlayerController;
    private float DelayTime;
    static public bool Kill = false;

    public StealthKill _Enemy;
    public Transform KillPosition;
    public float EndTime;

    [Header("Cinemachine")]
    public Cinemachine.CinemachineVirtualCamera VKill;
    public Cinemachine.CinemachineVirtualCamera VKill2;

    public StealthKill Enemy
    {
        get { return _Enemy; }
        set { _Enemy = value; }
    }

    #endregion

    #region Initialization

    private void Start()
    {
        Anim = this.GetComponent<Animator>();
        playerMovement = this.GetComponent<PlayerMovement>();
        playerStat = this.GetComponent<PlayerStat>();
        PlayerController = this.GetComponent<CharacterController>();
    }

    private void Update()
    {
        GetKillPosition();

        Finish();
        Finish_Boss();
    }

    #endregion

    #region Functions

    private void GetKillPosition()
    {
        if (Kill == true)
        {
            this.transform.position = KillPosition.position;
            this.transform.rotation = KillPosition.rotation;
        }
    }

    private void Finish()
    {
        if (Enemy != null && _Enemy.IsBoss == false)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (DelayTime <= Time.time)
                {
                    DelayTime = Time.time + EndTime;
                    VKill.Priority = 11;
                    Kill = true;                    // true되면 KillPosition
                    Anim.SetTrigger("Kill");        // 플레이어 Kill 트리거 작동
                    _Enemy.KillAnimation();
                    playerMovement.enabled = false;
                    PlayerController.enabled = false;
                    StartCoroutine(EndKillStealth());
                    playerStat.SetPotionIncrease(1f);
                }
            }
        }
    }

    private void Finish_Boss()
    {
        if (Enemy != null && _Enemy.IsBoss == true && BossAI.IsFinish == true)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (DelayTime <= Time.time)
                {
                    DelayTime = Time.time + EndTime;
                    VKill2.Priority = 11;
                    Kill = true;                    // true되면 KillPosition
                    Anim.SetTrigger("Kill_Boss");
                    _Enemy.KillAnimation();
                    playerMovement.enabled = false;
                    PlayerController.enabled = false;
                    StartCoroutine(EndKillStealth());
                    playerStat.SetPotionIncrease(1f);
                }
            }
        }
    }

    private IEnumerator EndKillStealth()
    {
        yield return new WaitForSeconds(EndTime);

        playerMovement.enabled = true;
        PlayerController.enabled = true;
        Kill = false;
        _Enemy = null;
        KillPosition = null;

        StealthKill.OnKill = false;
        VKill.Priority = 9;
        VKill2.Priority = 9;
    }

    #endregion
}
