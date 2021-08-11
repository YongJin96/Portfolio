using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI_Mongol : MonoBehaviour
{
    #region Variables

    private NavMeshAgent Agent;
    private Transform EnemyTransform;
    [HideInInspector]
    public Animator EnemyAnim;
    private MoveAgent moveAgent;
    private CapsuleCollider EnemyCollider;
    private BoxCollider StealthKillCollider;
    private AudioSource Audio;
    private EnemyFov enemyFov;
    private EnemyStat_Mongol enemyStat;
    private InteractionUI EnemyUI;

    // 플레이어 스크립트
    [HideInInspector]
    public Transform PlayerTransform;
    private PlayerMovement playerMovement;
    private PlayerStat playerStat;

    private float AttackDelayTime = 0f;
    private int RandomHit;
    
    public enum E_EnemyState
    {
        IDLE,
        PATROL,
        TRACE,
        ATTACK,
        HIT,
        DIE
    }

    public enum E_EnemyWeapon
    {
        KATANA,
        BOW,
        SPEAR,
        SWORD
    }

    [Header("Enemy Info")]
    public E_EnemyState EnemyStates = E_EnemyState.IDLE;
    public E_EnemyWeapon EnemyWeaponState = E_EnemyWeapon.KATANA;
  
    public float AttackDist = 0f;
    public float TraceDist = 0f;
    public float FindRange = 0f;
    public bool IsPatrol;
    public bool IsHit = false;
    public bool IsStun = false;
    public bool IsWeapon = false;
    public bool IsBlock = false;
    public bool IsCover = false;
    public bool IsFind = false; // true값으로 설정하면 FindPlayer()함수 사용
    public bool IsDie = false;
    public static bool IsPerilous = false;

    [Header("EnemyCollider")]
    public BoxCollider AttackColl_R;
    public BoxCollider AttackColl_L;

    [Header("EnemySFX")]
    public AudioClip[] AttackSFX;
    public AudioClip PerilousSFX;
    public AudioClip EquipSFX;
    public AudioClip UnEquipSFX;
    public AudioClip[] StepSFX;   

    [Header("EnemyPrefabs")]
    public GameObject EquipWeapon;
    public GameObject UnEquipWeapon;
    public GameObject EquipWeapon_L;
    public GameObject Trail_R;
    public GameObject Trail_L;
    public GameObject DropWeapon; // 사망시 무기를 떨어지게
    public GameObject DropWeapon2;
    public GameObject PerilousAttackEffect;

    [Header("Divide Body")]
    public GameObject Head;
    public GameObject CuttingHead;

    #endregion

    #region Initialization

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        EnemyTransform = GetComponent<Transform>();
        EnemyAnim = GetComponent<Animator>();
        moveAgent = GetComponent<MoveAgent>();
        EnemyCollider = GetComponent<CapsuleCollider>();
        StealthKillCollider = GetComponent<BoxCollider>();
        Audio = GetComponent<AudioSource>();
        enemyFov = GetComponent<EnemyFov>();
        enemyStat = GetComponent<EnemyStat_Mongol>();
        EnemyUI = GetComponent<InteractionUI>();

        PlayerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        playerStat = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStat>();

        EnemyAnim.SetFloat("WalkSpeed", Random.Range(0.7f, 1.2f)); // 애니메이션 모션속도
        EnemyAnim.SetFloat("Offset", Random.Range(0f, 1f));        // 애니메이션 딜레이
    }

    private void OnEnable()
    {
        StartCoroutine(CheckState());
        StartCoroutine(Action());              
    }

    private void Update()
    {
        StartCoroutine(HitCheck());
        StartCoroutine(KnockdownCheck());
        StartCoroutine(BlockCheck());
        StartCoroutine(OffPerilous());

        SetSpeed();
        StopTrace();
        EquipmentWeapon();
    }

    #endregion

    #region Functions

    private IEnumerator CheckState()
    {
        // 오브젝트 풀에 생성 시 다른 스크립트의 초기화를 위해 대기
        yield return new WaitForSeconds(1f);

        while (IsDie == false)
        {
            float dist = Vector3.Distance(PlayerTransform.position, EnemyTransform.position);

            if (dist <= AttackDist && playerMovement.IsStealth == false)
            {             
                EnemyStates = E_EnemyState.ATTACK;
            }
            else if (enemyFov.IsViewPlayer() && enemyFov.IsTracePlayer() && playerMovement.IsStealth == false) // 추적 반경 및 시야각에 들어왔는지 판단
            {
                FindPlayer();
                EnemyStates = E_EnemyState.TRACE;
            }
            else
            {
                if (IsPatrol == false)
                {
                    EnemyStates = E_EnemyState.IDLE;
                }
                else if (IsPatrol == true)
                {
                    EnemyStates = E_EnemyState.PATROL;
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator Action()
    {
        while (IsDie == false)
        {
            switch (EnemyStates)
            {
                case E_EnemyState.IDLE:
                    //moveAgent.Stop();
                    EnemyAnim.SetBool("IsMove", false);
                    Agent.speed = 0f;
                    break;

                case E_EnemyState.PATROL:
                    moveAgent.Patrolling = true;
                    EnemyAnim.SetBool("IsMove", true);

                    // 순찰할때 적 공격거리값
                    Agent.stoppingDistance = 0f;
                    AttackDist = 0f;

                    break;

                case E_EnemyState.TRACE:
                    Agent.isStopped = false;
                    EnemyAnim.SetBool("IsMove", true);
                    moveAgent.TraceTarget = PlayerTransform.position;

                    // 발견하면 값 변경
                    Agent.stoppingDistance = 2f;
                    SetWeaponDist();
                    LookTarget();
                    break;

                case E_EnemyState.ATTACK:
                    moveAgent.Stop();
                    EnemyAnim.SetBool("IsMove", false);
                    Agent.speed = 0f;
                    SetWeaponAttack();
                    LookTarget();                   
                    break;

                case E_EnemyState.HIT:
                    EnemyAnim.SetTrigger("Hit");
                    break;

                case E_EnemyState.DIE:
                    print("Die");
                    break;
            }

            yield return null;
        }
    }

    private IEnumerator OffPerilous()
    {
        float elapsed = 0f;
       
        while (elapsed <= 1f && IsPerilous == true && EnemyWeaponState != E_EnemyWeapon.BOW)
        {
            elapsed += Time.deltaTime;
            PerilousAttackEffect.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            yield return null;
        }

        OffPerilousEffect();
    }

    private IEnumerator HitCheck()
    {
        float elapsed = 0f;

        while (elapsed <= 2f && IsHit == true)
        {
            elapsed += Time.deltaTime;
            AttackColl_R.enabled = false;
            Trail_R.SetActive(false);

            yield return null;
        }

        IsHit = false;
        SetViewAngle(); //ViewAngle값을 초기값으로
    }

    private IEnumerator KnockdownCheck()
    {
        float elapsed = 0f;

        while (elapsed <= 2.5f && IsStun == true)
        {
            elapsed += Time.deltaTime;

            yield return null;
        }

        moveAgent.TraceSpeed = 1.5f;
        IsStun = false;
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

    private void LookTarget()
    {
        if (PlayerTransform != null && IsStun == false)
        {
            Vector3 target = PlayerTransform.position - EnemyTransform.position;
            Vector3 lookTarget = Vector3.Slerp(EnemyTransform.forward, target.normalized, Time.deltaTime * 5f);
            transform.rotation = Quaternion.LookRotation(lookTarget);
        }
    }

    private void KatanaAttack()
    {
        if (IsWeapon == true && IsHit == false && IsStun == false)
        {
            if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Attack0");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20) 
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Attack1");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20) 
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Attack2"); 
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Attack3"); 
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 10) 
            {
                AttackDelayTime = Time.time + 1.5f;
                IsPerilous = true; 
                EnemyAnim.SetTrigger("Perilous Attack");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20) 
            {
                AttackDelayTime = Time.time + 1.5f;
                EnemyAnim.SetTrigger("Block"); 
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 15)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Dodge_Back");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 15)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Dodge_Right");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 15)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Dodge_Left");
            }
        }
    }

    private void BowAttack()
    {
        if (AttackDelayTime <= Time.time)
        {
            AttackDelayTime = Time.time + 7f;
            EnemyAnim.SetTrigger("BowAttack");
        }
    }

    private void SpearAttack()
    {
        if (IsWeapon == true && IsHit == false && IsStun == false)
        {
            if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1.5f;
                EnemyAnim.SetTrigger("Attack0");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1.5f;
                EnemyAnim.SetTrigger("Attack1");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1.5f;
                EnemyAnim.SetTrigger("Attack2");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1.5f;
                EnemyAnim.SetTrigger("Attack3");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1.5f;
                EnemyAnim.SetTrigger("Attack4");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1.5f;
                EnemyAnim.SetTrigger("Attack5");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 10)
            {
                AttackDelayTime = Time.time + 1.5f;
                IsPerilous = true;
                EnemyAnim.SetTrigger("Perilous Attack");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 15)
            {
                AttackDelayTime = Time.time + 2f;
                EnemyAnim.SetTrigger("Block");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 15)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Dodge");
            }
        }
    }

    private void SwordAttack()
    {
        if (IsWeapon == true && IsHit == false && IsStun == false)
        {
            if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 2f;
                EnemyAnim.SetTrigger("Attack0");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Attack1");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Attack2");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Attack3");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 10)
            {
                AttackDelayTime = Time.time + 1.5f;
                IsPerilous = true;
                EnemyAnim.SetTrigger("Perilous Attack");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 15)
            {
                AttackDelayTime = Time.time + 1.5f;
                EnemyAnim.SetTrigger("Block");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 15)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Dodge_Back");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 15)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Dodge_Right");
            }
            else if (AttackDelayTime <= Time.time && Random.Range(0, 100) <= 15)
            {
                AttackDelayTime = Time.time + 1f;
                EnemyAnim.SetTrigger("Dodge_Left");
            }
        }
    }

    private void EquipmentWeapon()
    {
        if (IsWeapon == false && EnemyStates == E_EnemyState.TRACE && EnemyWeaponState != E_EnemyWeapon.BOW)
        {
            EnemyAnim.SetTrigger("Equip");
            IsWeapon = true;
        }
        else if (IsWeapon == true && EnemyStates == E_EnemyState.PATROL) // 순찰중이면 무기 넣음
        {
            EnemyAnim.SetTrigger("UnEquip");
            IsWeapon = false;
        }
    }

    public void Hit()
    {
        RandomHit = Random.Range(0, 4);
        IsHit = true;
        IsStun = false;
        enemyFov.ViewAngle = 360; // 플레이어에게 맞으면 시야범위가 증가
        FindPlayer();
        if (RandomHit == 0)
        {
            EnemyAnim.SetTrigger("Hit1");              
        }
        else if (RandomHit == 1)
        {
            EnemyAnim.SetTrigger("Hit2");
        }
        else if (RandomHit == 2)
        {
            EnemyAnim.SetTrigger("Hit3");
        }
        else if (RandomHit == 3)
        {
            EnemyAnim.SetTrigger("Hit4");
        }
    }

    public void BlockHit()
    {
        EnemyAnim.SetTrigger("BlockHit");
    }

    public void Knockdown()
    {
        EnemyAnim.SetTrigger("KnockDown");
        moveAgent.TraceSpeed = 0f;
        AttackColl_R.enabled = false;
        Trail_R.SetActive(false);
        IsBlock = false;
        IsStun = true;
    }

    public void ParryingToStun()
    {
        EnemyAnim.SetTrigger("ParryStun");
    }

    public void Die()
    {
        EnemyStates = E_EnemyState.DIE;
        EnemyAnim.SetTrigger("Die");
        IsDie = true;
        EnemyCollider.enabled = false;
        StealthKillCollider.enabled = false;
        AttackColl_R.gameObject.SetActive(false);
        DropWeapons();
        moveAgent.enabled = false;
        Agent.enabled = false;
        EnemyUI.ViewUI(false);
        OffPerilousEffect();
        Agent.tag = "Untagged"; // 적 사망시 타켓팅 바로 해제하기위해 Enemy 태그변경
        Destroy(this.gameObject, 10f);
        Destroy(DropWeapon.gameObject, 10f);
        Destroy(DropWeapon2.gameObject, 10f);
    }

    public void StealthDie()
    {
        EnemyStates = E_EnemyState.DIE;
        IsDie = true;
        EnemyCollider.enabled = false;
        StealthKillCollider.enabled = false;
        AttackColl_R.gameObject.SetActive(false);
        if (IsWeapon)
        {
            DropWeapons();
        }
        moveAgent.enabled = false;
        Agent.enabled = false;
        EnemyUI.ViewUI(false);
        Agent.tag = "Untagged";
        Destroy(this.gameObject, 8f);
        Destroy(DropWeapon.gameObject, 8f);
        Destroy(DropWeapon2.gameObject, 8f);
    }

    private void SetSpeed()
    {
        // 순찰중일때와 추적할때 속도값을 넣어줌
        EnemyAnim.SetFloat("Speed", moveAgent.Speed, 0.1f, Time.deltaTime);
    }

    public void SetWeaponDist()
    {
        if (EnemyWeaponState == E_EnemyWeapon.KATANA)
        {
            AttackDist = 2f;
        }
        else if (EnemyWeaponState == E_EnemyWeapon.BOW)
        {
            AttackDist = 20f;
        }
        else if (EnemyWeaponState == E_EnemyWeapon.SPEAR)
        {
            AttackDist = 2f;
        }
        else if (EnemyWeaponState == E_EnemyWeapon.SWORD)
        {
            AttackDist = 2f;
        }
    }

    private void SetWeaponAttack()
    {
        if (EnemyWeaponState == E_EnemyWeapon.KATANA) { KatanaAttack(); }
        else if (EnemyWeaponState == E_EnemyWeapon.BOW) { BowAttack(); }
        else if (EnemyWeaponState == E_EnemyWeapon.SPEAR) { SpearAttack(); }
        else if (EnemyWeaponState == E_EnemyWeapon.SWORD) { SwordAttack(); }
    }

    private void SetViewAngle()
    {
        if (EnemyWeaponState != E_EnemyWeapon.BOW)
        {
            enemyFov.ViewAngle = 140;
        }
        else if (EnemyWeaponState == E_EnemyWeapon.BOW)
        {
            enemyFov.ViewAngle = 160;
        }
    }

    public void ParryingToDie()
    {
        EnemyAnim.SetTrigger("ParryingToDie");
        Die();
    }

    public void ExplodeDie()
    {
        EnemyAnim.SetTrigger("ExplodeDie");
        IsDie = true;
        EnemyCollider.enabled = false;
        StealthKillCollider.enabled = false;
        AttackColl_R.gameObject.SetActive(false);
        DropWeapons();
        moveAgent.enabled = false;
        Agent.enabled = false;
        EnemyUI.ViewUI(false);
        OffPerilousEffect();
        Agent.tag = "Untagged"; // 적 사망시 타켓팅 바로 해제하기위해 Enemy 태그변경
        Destroy(this.gameObject, 10f);
        Destroy(DropWeapon.gameObject, 10f);
        Destroy(DropWeapon2.gameObject, 10f);
    }

    private void FindPlayer() // 적이 플레이어를 발견하거나 공격받으면 주변 적들도 플레이어를 공격하러옴
    {
        Collider[] colls = Physics.OverlapSphere(transform.position, FindRange, 1 << LayerMask.NameToLayer("Enemy"));

        foreach (var coll in colls)
        {
            if (IsFind == false) { return; }

            if (coll.GetComponent<EnemyAI_Mongol>().EnemyStates != E_EnemyState.TRACE && IsFind == true)
            {
                coll.GetComponent<EnemyAI_Mongol>().EnemyStates = E_EnemyState.TRACE;
            }
        }
    }

    private void DropWeapons()
    {
        DropWeapon.transform.SetParent(transform.parent);
        DropWeapon.SetActive(true);
        EquipWeapon.SetActive(false);
        if (DropWeapon2 != null)
        {
            DropWeapon2.transform.SetParent(transform.parent);
            DropWeapon2.SetActive(true);
        }
        if (EquipWeapon_L != null)
        {
            EquipWeapon_L.SetActive(false);
        }
    }

    public void Cover()
    {
        EnemyAnim.SetBool("IsCover", true);
        EnemyAnim.SetTrigger("Cover");
        IsCover = true;
    }

    public void End_Cover()
    {
        EnemyAnim.SetBool("IsCover", false);
        IsCover = false;
    }

    private void StopTrace()
    {
        // 플레이어 사망시 추적 못하게
        if (playerStat.CurrentHealth <= 0)
        {
            AttackDist = 0f;
            TraceDist = 0f;
        }
    }

    #endregion

    #region Animation Func

    private void OnAttack()
    {
        AttackColl_R.enabled = true;
        Audio.PlayOneShot(AttackSFX[Random.Range(0, 4)], 1f);
        EnemyCollider.enabled = true;       
        IsBlock = false;
    }

    private void OffAttack()
    {
        AttackColl_R.enabled = false;       
    }

    private void OnAttack2()
    {
        AttackColl_L.enabled = true;
        Audio.PlayOneShot(AttackSFX[Random.Range(0, 4)], 1f);
        EnemyCollider.enabled = true;
        Trail_R.SetActive(true);
    }

    private void OffAttack2()
    {
        AttackColl_L.enabled = false;
        Trail_R.SetActive(false);
    }

    private void OnTrail()
    {
        Trail_R.SetActive(true);
    }

    private void OffTrail()
    {
        Trail_R.SetActive(false);
    }

    private void OnPerilousEffect()
    {
        Audio.PlayOneShot(PerilousSFX, 1f);
        PerilousAttackEffect.SetActive(true);
    }

    private void OffPerilousEffect()
    {
        if (EnemyWeaponState != E_EnemyWeapon.BOW)
        {
            IsPerilous = false;
            PerilousAttackEffect.SetActive(false);
        }
    }

    private void Equip()
    {
        EquipWeapon.SetActive(true);
        UnEquipWeapon.SetActive(false);
        Audio.PlayOneShot(EquipSFX, 1f);
    }

    private void UnEquip()
    {
        EquipWeapon.SetActive(false);
        UnEquipWeapon.SetActive(true);
        Audio.PlayOneShot(UnEquipSFX, 1f);
    }

    private void OnDodge()
    {
        EnemyCollider.enabled = false;
    }

    private void OffDodge()
    {
        EnemyCollider.enabled = true;
    }

    private void OnBlock()
    {
        IsBlock = true;
    }

    private void OffBlock()
    {
        IsBlock = false;
    }

    private void OnFinish()
    {
        EnemyStates = E_EnemyState.DIE;
        IsDie = true;
        EnemyCollider.enabled = false;
        moveAgent.enabled = false;
        Agent.enabled = false;
        Destroy(this.gameObject, 5f);
    }

    private void Step()
    {
        Audio.PlayOneShot(StepSFX[Random.Range(0, 4)], 0.1f);
    }

    private void Cut()
    {
        Head.SetActive(false);
        CuttingHead.SetActive(true);
    }

    #endregion
}
