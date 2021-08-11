using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPC_AI : MonoBehaviour
{
    #region Variables

    public enum MountState
    {
        NONE,
        HORSE
    }

    public enum NPC_State
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
    [HideInInspector]
    public Transform TargetTransform;
    private CapsuleCollider HitCollider;
    private BoxCollider InteractionCollider;
    private AudioSource Audio;
    private InteractionUI npc_UI;
    private PlayerMovement Player;
    private MoveAgent_NPC MoveAgent;

    private float Dist = 0f;
    private float AttackDelayTime;
    private Quaternion SaveRot;

    public HorseAI_NPC Horse;
    public GameObject Katana;
    public BoxCollider AttackCollider;
    public GameObject Trail;

    [Header("NPC State")]
    public MountState MountStates = MountState.NONE;
    public NPC_State States = NPC_State.IDLE;
    public float Damage = 10f;
    public float AttackDist;
    public float TraceDist;
    public float Speed = 0f;
    public float Radius;

    public bool IsAttack = false;
    public bool IsWeapon = false;
    public bool IsBlock = false;
    public bool IsCrouch = false;
    public bool IsPatrol = false;
    public bool IsFollow = true;
    public bool IsHit = false;
    public bool IsDie = false;
    public bool IsMount = false;

    [Header("SFX")]
    public AudioClip[] KatanaSFX;
    public AudioClip FootStepSFX;

    [Header("Tracking")]
    public HeadTracking_NPC Head;

    #endregion

    #region Initialization

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Anim = GetComponent<Animator>();
        HitCollider = GetComponent<CapsuleCollider>();
        InteractionCollider = GetComponent<BoxCollider>();
        Audio = GetComponent<AudioSource>();
        npc_UI = GetComponent<InteractionUI>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        MoveAgent = GetComponent<MoveAgent_NPC>();

        Anim.SetFloat("Offset", Random.Range(0.6f, 0.9f)); // 애니메이션 딜레이  

        SaveRot = transform.localRotation;

        // StartScene
        if (IsWeapon == true)
        {
            Anim.SetTrigger("Equip");

            if (IsMount == true)
            {
                Anim.SetBool("IsWeapon", true);
            }
        }
    }

    private void Update()
    {
        TargetDistance();
        StartCoroutine(CheckState());
        StartCoroutine(Action());
        StartCoroutine(HitCheck());
        StartCoroutine(BlockCheck());

        Crouch();
        HorseMount();

        SaveRot = transform.localRotation;

        if (IsMount == false)
        {
            Speed = Agent.speed;
        }
        else if (IsMount == true)
        {
            Speed = Horse.Speed;
        }

        Anim.SetFloat("Speed", Speed, 0.1f, Time.deltaTime);
    }

    #endregion

    #region Functions

    private IEnumerator CheckState()
    {
        if (IsDie == false && TargetTransform != null)
        {
            Dist = Vector3.Distance(TargetTransform.position, transform.position);

            if (Dist <= AttackDist && IsCrouch == false && TargetTransform.tag == "Enemy")
            {
                States = NPC_State.ATTACK;
            }
            else if (Dist <= TraceDist && IsAttack == true && IsMount == false)
            {
                States = NPC_State.TRACE;
            }
            else if (IsFollow == true && IsMount == false)
            {
                States = NPC_State.FOLLOW;
            }
            else
            {
                if (IsPatrol == false && IsFollow == false)
                {
                    States = NPC_State.IDLE;
                    IsPatrol = false;
                    IsFollow = false;
                }
                else if (IsPatrol == true)
                {
                    States = NPC_State.PATROL;
                    IsPatrol = true;
                    IsFollow = false;
                }
            }
        }
        else
        {
            if (IsPatrol == false)
            {
                States = NPC_State.IDLE;
                IsPatrol = false;
                IsFollow = false;
            }
            else if (IsPatrol == true)
            {
                States = NPC_State.PATROL;
                IsPatrol = true;
                IsFollow = false;
            }
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator Action()
    {
        switch (States)
        {
            case NPC_State.IDLE:
                Anim.SetBool("IsMove", false);
                Agent.speed = 0f;
                break;

            case NPC_State.PATROL:
                MoveAgent.Patrolling = true;
                Anim.SetBool("IsMove", true);
                Agent.speed = MoveAgent.PatrolSpeed;
                Agent.stoppingDistance = 0f;
                AttackDist = 0f;
                break;

            case NPC_State.TRACE:
                Anim.SetBool("IsMove", true);
                MoveAgent.TraceTarget = TargetTransform.position;
                Agent.speed = 2f;
                AttackDist = 2f;

                LookTarget();
                EquipWeapon();
                Head.Radius = 0f; // 적이 있으면 플레이어를 쳐다보지않음
                break;

            case NPC_State.FOLLOW:
                Anim.SetBool("IsMove", true);    
                Agent.destination = TargetTransform.position;

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
                    Agent.speed = 2f;
                }

                LookTarget();              
                UnEquipWeapon();
                Head.Radius = 10f;
                break;

            case NPC_State.ATTACK:
                Anim.SetBool("IsMove", false);
                Agent.speed = 0f;
                LookTarget();
                Attack();
                break;

            case NPC_State.HIT:
                break;

            case NPC_State.DIE:
                Die();
                break;
        }

        yield return null;
    }

    private IEnumerator HitCheck()
    {
        float elapsed = 0f;

        while(elapsed <= 2f && IsHit == true)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        IsHit = false;
    }

    private IEnumerator BlockCheck()
    {
        float elapsed = 0f;

        while (elapsed <= 2f && IsBlock == true)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        IsBlock = false;
    }

    private void Attack()
    {
        if (IsWeapon == true && IsHit == false)
        {
            if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20) 
            {
                AttackDelayTime = Time.time + 1f;
                Anim.SetTrigger("Attack_0");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                Anim.SetTrigger("Attack_1");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                Anim.SetTrigger("Attack_2");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20) 
            {
                AttackDelayTime = Time.time + 1f;
                Anim.SetTrigger("Attack_3"); 
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                Anim.SetTrigger("Block");
            }
        }
    }

    private void Crouch()
    {
        if (States == NPC_State.FOLLOW)
        {
            if (Player.IsCrouch == true)
            {
                IsCrouch = true;
                Anim.SetBool("IsCrouch", true);
            }
            else if (Player.IsCrouch == false)
            {
                IsCrouch = false;
                Anim.SetBool("IsCrouch", false);
            }
        }
        else if (States == NPC_State.TRACE)
        {
            IsCrouch = false;
            Anim.SetBool("IsCrouch", false);
        }
    }

    private void HorseMount()
    {
        if (MountStates == MountState.HORSE)
        {
            if (Horse.IsMount == true)
            {
                Agent.enabled = false;
                IsMount = true;
                Anim.SetBool("IsMount", true);
            }
            else if (Horse.IsMount == false)
            {
                Agent.enabled = true;
                IsMount = false;
                Anim.SetBool("IsMount", false);
            }
        }
    }

    public void TargetDistance()
    {
        if (TargetTransform != null) { return; }

        GameObject[] target = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity; // 가장 짧은 거리 값
        GameObject nearestEnemy = null;          // 가까운 적의 거리값     

        foreach (GameObject enemy in target)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);

            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null && shortestDistance <= Radius)
        {
            TargetTransform = nearestEnemy.transform;
            IsAttack = true;
        }
        else // 범위 내에 적이 없으면 플레이어를 따라오게해줌
        {
            if (IsFollow == true)
            {
                TargetTransform = GameObject.FindGameObjectWithTag("Player").transform;
                IsAttack = false;
            }
            else
            {
                TargetTransform = null;
            }
        }
    }

    public void LookTarget()
    {
        if (TargetTransform == null && IsFollow == false)
        {
            transform.rotation = SaveRot;
        }
        else if (TargetTransform != null && TargetTransform.CompareTag("Enemy") || TargetTransform.CompareTag("Player"))
        {
            Vector3 target = TargetTransform.position - transform.position;
            Vector3 looktarget = Vector3.Slerp(transform.forward, target.normalized, Time.deltaTime * 3f);
            transform.rotation = Quaternion.LookRotation(looktarget);
        }
    }

    public void EquipWeapon()
    {
        if (IsWeapon == false)
        {
            Anim.SetTrigger("Equip");
            IsWeapon = true;
            Anim.SetBool("IsWeapon", true);
        }
    }

    public void UnEquipWeapon()
    {
        if (IsWeapon == true)
        {
            Anim.SetTrigger("UnEquip");
            IsWeapon = false;
            Anim.SetBool("IsWeapon", false);
        }
    }
   
    public void Hit()
    {
        if (IsDie == false)
        {
            IsHit = true;

            if (Random.Range(0, 3) == 0)
            {
                Anim.SetTrigger("Hit_0");
            }
            else if (Random.Range(0, 3) == 1)
            {
                Anim.SetTrigger("Hit_1");
            }
            else if (Random.Range(0, 3) == 2)
            {
                Anim.SetTrigger("Hit_2");
            }
        }
    }

    public void BlockHit()
    {
        Anim.SetTrigger("BlockHit");
    }

    public void PerilousHit()
    {
        IsHit = true;

        if (IsDie == false)
        {
            if (Random.Range(0, 3) == 0)
            {
                Anim.SetTrigger("PerilousHit_0");
            }
            else if (Random.Range(0, 3) == 0)
            {
                Anim.SetTrigger("PerilousHit_0");
            }
            else if (Random.Range(0, 3) == 0)
            {
                Anim.SetTrigger("PerilousHit_0");
            }
        }
    }

    public void KnockDown()
    {
        Anim.SetTrigger("KnockDown");

        IsHit = true;
        IsBlock = false;
        AttackCollider.enabled = false;
        Trail.SetActive(false);
    }

    public void Die()
    {
        if (IsDie == true) { return; }

        Anim.SetTrigger("Die");
        IsDie = true;
        IsMount = false;
        IsPatrol = false;
        AttackCollider.enabled = false;
        Trail.SetActive(false);
        HitCollider.enabled = false;
        InteractionCollider.enabled = false;
        this.gameObject.tag = "Untagged";
        Speed = 0f;
        Agent.enabled = false;
        Destroy(this.gameObject, 10f);
    }

    public void ExplodeDie()
    {
        if (IsDie == true) { return; }

        Anim.SetTrigger("ExplodeDie");
        IsDie = true;
        IsMount = false;
        IsPatrol = false;
        AttackCollider.enabled = false;
        Trail.SetActive(false);
        HitCollider.enabled = false;
        InteractionCollider.enabled = false;
        this.gameObject.tag = "Untagged";
        Speed = 0f;
        Agent.enabled = false;
        Destroy(this.gameObject, 10f);
    }

    public void HorseMountAttack()
    {
        if (IsWeapon == true && IsHit == false && IsMount == true)
        {
            if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                Anim.SetTrigger("HAttack_0");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                Anim.SetTrigger("HAttack_1");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                Anim.SetTrigger("Block");
            }
        }
    }

    public void HorseMountHit()
    {
        if (IsDie == false && IsMount)
        {
            IsHit = true;

            if (Random.Range(0, 3) == 0)
            {
                Anim.SetTrigger("HHit_0");
            }
            else if (Random.Range(0, 3) == 1)
            {
                Anim.SetTrigger("HHit_1");
            }
        }
    }

    public void FallingToHorse()
    {
        Anim.SetTrigger("FallingToHorse");
    }

    #endregion

    #region Animation Func

    private void OnAttack()
    {
        AttackCollider.enabled = true;
        Audio.PlayOneShot(KatanaSFX[Random.Range(0, 3)], 1f);
    }

    private void OffAttack()
    {
        AttackCollider.enabled = false;
    }

    private void Equip()
    {
        Katana.SetActive(true);
    }

    private void UnEquip()
    {
        Katana.SetActive(false);
    }

    private void OnTrail()
    {
        Trail.SetActive(true);
    }

    private void OffTrail()
    {
        Trail.SetActive(false);
    }

    private void FootStep()
    {
        Audio.PlayOneShot(FootStepSFX, 0.1f);
    }

    private void OnBlock()
    {
        IsBlock = true;
        AttackCollider.enabled = false;
        Trail.SetActive(false);
    }

    private void OffBlock()
    {
        IsBlock = false;
    }

    #endregion
}
