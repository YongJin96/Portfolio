using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HorseAI_NPC : MonoBehaviour
{
    #region Variables

    public enum E_HorseState
    {
        IDLE,
        PATROL,
        TRACE,
        FOLLOW,
        ATTACK,
        HIT,
        DIE
    }

    private NavMeshAgent Agent;
    private Animator Anim;
    private Transform TargetTransform;
    private CapsuleCollider HitCollider;
    private AudioSource Audio;    
    private PlayerMovement Player;
    private MoveAgent_NPC MoveAgent;

    private float Dist = 0f;
    private float SoundDelayTime = 0f;
    private Quaternion SaveRot;

    public float Speed = 0f;

    public float AttackDist;
    public float TraceDist;

    [Header("NPC Settings")]
    public NPC_AI NPC;

    [Header("Horse Prefabs")]
    public Transform MountPos;
    public SphereCollider FrontLeftLeg;
    public SphereCollider FrontRightLeg;
    public SphereCollider BackLeftLeg;
    public SphereCollider BackRightLeg;

    [Header("Horse SFX")]
    public AudioClip[] HorseBreating;
    public AudioClip[] StepSFX;
    public AudioClip[] GallopSFX;
    public AudioClip[] SquealSFX;
    public AudioClip JumpSFX;
    public AudioClip HitSFX;
    public AudioClip DieSFX;

    [Header("Horse State")]
    public E_HorseState HorseState = E_HorseState.IDLE;
    public bool IsAttack = false;
    public bool IsWeapon = false;
    public bool IsBlock = false;
    public bool IsCrouch = false;
    public bool IsPatrol = false;
    public bool IsFollow = true;
    public bool IsHit = false;
    public bool IsDie = false;
    public bool IsMount = false;
    static public bool IsMountCheck = false;

    #endregion

    #region Initialization

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Anim = GetComponent<Animator>();
        HitCollider = GetComponent<CapsuleCollider>();
        Audio = GetComponent<AudioSource>();     
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        MoveAgent = GetComponent<MoveAgent_NPC>();

        SaveRot = transform.localRotation;
    }

    private void LateUpdate()
    {
        StartCoroutine(HorseStateCheck());
        StartCoroutine(HorseAction());
        RideTransform();
        SlopeAngle();

        Speed = Agent.speed;
        Anim.SetFloat("Speed", Speed, 0.1f, Time.deltaTime);
    }

    #endregion

    #region Functions

    private IEnumerator HorseStateCheck()
    {
        if (IsDie == false && NPC.TargetTransform != null)
        {
            Dist = Vector3.Distance(NPC.TargetTransform.position, transform.position);

            if (Dist <= AttackDist && IsCrouch == false && NPC.TargetTransform.tag == "Enemy")
            {
                HorseState = E_HorseState.ATTACK;
            }
            else if (Dist <= TraceDist && IsAttack == true)
            {
                HorseState = E_HorseState.TRACE;
            }
            else if (NPC.IsFollow == true)
            {
                HorseState = E_HorseState.FOLLOW;
                IsFollow = true;
            }
            else
            {
                if (IsPatrol == false && IsFollow == false)
                {
                    HorseState = E_HorseState.IDLE;
                    IsPatrol = false;
                    IsFollow = false;
                }
                else if (IsPatrol == true)
                {
                    HorseState = E_HorseState.PATROL;
                    IsPatrol = true;
                    IsFollow = false;
                }
            }
        }
        else
        {
            if (IsPatrol == false)
            {
                HorseState = E_HorseState.IDLE;
                IsPatrol = false;
                IsFollow = false;
            }
            else if (IsPatrol == true)
            {
                HorseState = E_HorseState.PATROL;
                IsPatrol = true;
                IsFollow = false;
            }
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator HorseAction()
    {
        switch (HorseState)
        {
            case E_HorseState.IDLE:
                Agent.isStopped = true;
                Agent.speed = 0f;
                break;

            case E_HorseState.PATROL:
                Agent.isStopped = false;
                MoveAgent.Patrolling = true;
                Agent.speed = MoveAgent.PatrolSpeed;
                AttackDist = 0f;
                break;

            case E_HorseState.TRACE:
                Agent.isStopped = false;
                MoveAgent.TraceTarget = NPC.TargetTransform.position;
                Agent.stoppingDistance = 1f;
                Agent.speed = 1f;
                AttackDist = 1f;

                LookTarget();
                NPC.EquipWeapon();
                break;

            case E_HorseState.FOLLOW:
                Agent.isStopped = false;
                Agent.destination = NPC.TargetTransform.position;
                Agent.stoppingDistance = 5f;
                PlayerSpeed();

                LookTarget();
                NPC.UnEquipWeapon();
                break;

            case E_HorseState.ATTACK:
                Agent.isStopped = true;
                Anim.SetBool("IsMove", false);
                Agent.speed = 0f;

                LookTarget();
                NPC.HorseMountAttack();
                break;

            case E_HorseState.HIT:

                break;

            case E_HorseState.DIE:

                break;
        }

        yield return null;
    }

    public void LookTarget()
    {
        if (NPC.TargetTransform == null && IsFollow == false)
        {
            transform.rotation = SaveRot;
        }
        else if (NPC.TargetTransform != null && NPC.TargetTransform.CompareTag("Enemy") || NPC.TargetTransform.CompareTag("Player"))
        {
            Vector3 target = NPC.TargetTransform.position - transform.position;
            Vector3 looktarget = Vector3.Slerp(transform.forward, target.normalized, Time.deltaTime * 3f);
            transform.rotation = Quaternion.LookRotation(looktarget);
        }
    }

    private void RideTransform()
    {
        if (IsMount == true)
        {
            NPC.transform.position = MountPos.transform.position;
            NPC.transform.rotation = MountPos.transform.rotation;
        }
    }

    private void SlopeAngle()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0f, 0.5f, 0.57f)), Vector3.down, out hit, 1 << LayerMask.NameToLayer("Map")))
        {
            Quaternion normalRot = Quaternion.FromToRotation(transform.up, hit.normal);
            transform.rotation = Quaternion.Lerp(transform.rotation, normalRot * transform.rotation, Time.deltaTime * 3f);
        }

        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0f, 0.5f, 0.57f)), Vector3.down, Color.yellow);
    }

    public void Hit()
    {
        if (IsDie == false)
        {
            Audio.PlayOneShot(HitSFX, 1f);

            if (Random.Range(0, 1) == 0)
            {
                Anim.SetTrigger("Hit_0");
                
            }
            else if (Random.Range(0, 1) == 1)
            {
                Anim.SetTrigger("Hit_1");
            }
        }
    }

    public void Die()
    {
        if (IsDie == true) { return; }

        Anim.SetTrigger("Die");
        Audio.PlayOneShot(DieSFX, 1f);
        IsDie = true;
        IsMount = false;
        NPC.IsMount = false;
        IsPatrol = false;
        Speed = 0f;
        MoveAgent.enabled = false;
        Destroy(this.gameObject, 10f);
    }

    public void ExplodeDie()
    {
        if (IsDie == true) { return; }

        Anim.SetTrigger("ExplodeDie");
        Audio.PlayOneShot(DieSFX, 1f);
        IsDie = true;
        IsMount = false;
        NPC.IsMount = false;
        IsPatrol = false;
        Speed = 0f;
        MoveAgent.enabled = false;
        Destroy(this.gameObject, 10f);
    }

    private void PlayerSpeed()
    {
        // 플레이어 거리에 비해 속도조절
        if (Dist <= 3)
        {
            Agent.speed = 0f;
        }
        else if (Dist <= 5) // 플레이어가 걸으면 NPC도 걷게
        {
            Agent.speed = 0.5f;
        }
        else  // 플레이어가 뛰면 NPC도 뛰게
        {
            Agent.speed = 3f;
        }
    }

    #endregion

    #region Animation Function

    private void WalkSound()
    {
        if (SoundDelayTime <= Time.time)
        {
            SoundDelayTime = Time.time + 0.2f;
            Audio.PlayOneShot(StepSFX[Random.Range(0, 4)], 1f);
        }
    }

    private void GallopSound()
    {
        if (SoundDelayTime <= Time.time)
        {
            SoundDelayTime = Time.time + 0.5f;
            Audio.PlayOneShot(GallopSFX[Random.Range(0, 3)], 1f);
        }
    }
    #endregion
}
