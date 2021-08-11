using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    #region Variables

    private NavMeshAgent Agent;
    private PlayerStat PlayerStat;
    private Transform PlayerTransform;
    private CapsuleCollider BossHitCollider;
    private AudioSource Audio;

    private float AnimDelayTime = 0f;

    public enum BossState
    {
        IDLE,
        RUN,
        WALK,
        ATTACK,
        BLOCK,
        HIT,
        DIE
    }

    public enum BossPhase
    {
        PHASE_1,
        PHASE_2,
        PHASE_3
    }

    public enum BossWeapon
    {
        KATANA,
        SPEAR
    }

    [HideInInspector]
    public Animator Anim;
    public BoxCollider KickCollider;

    [Header("Katana")]
    public GameObject Katana;
    public BoxCollider KatanaCollider;
    public GameObject KatanaTrail;

    [Header("Spear")]
    public GameObject Spear;
    public BoxCollider SpearCollider;
    public GameObject SpearTrail;
    public GameObject UnEquipSpear;

    [Header("Gun")]
    public GameObject Gun;
    public GameObject GunEffect;
    public GameObject Bullet;
    public Transform FireTransform;

    [Header("Effect")]
    public GameObject PerilousEffect;
    public GameObject ExplosionEffect;
    public GameObject[] SkillEffect;

    [Header("UI")]
    public GameObject HealthBar;
    public GameObject PostureBar;
    public GameObject FinishUI;

    [Header("Audio Setting")]
    public AudioClip[] AttackSFX;
    public AudioClip[] HitSFX;
    public AudioClip PerilousSFX;
    public AudioClip[] WalkSFX;
    public AudioClip[] RunSFX;
    public AudioClip GunSFX;
    public AudioClip StabSFX;
    public SoundTrack BGM;

    [Header("Boss State")]
    public BossState State = BossState.IDLE;
    public BossPhase Phase = BossPhase.PHASE_1;
    public BossWeapon Weapon = BossWeapon.KATANA;

    [Header("Boss Stat")]
    public float AttackDist;
    public float RunDist;
    public float WalkDist;
    public float Speed;

    [Header("Animation Event")]
    public Transform BloodTransform;
    public GameObject BloodEffect;

    [Header("Cinemachine Setting")]
    public CinemachineShake Shake;

    public bool IsAttack = false;
    public bool IsBlock = false;
    static public bool IsPerilous = false;
    public bool IsHit = false;
    public bool IsDie = false;
    public bool IsDelay = false;
    public bool IsInvincibility = false;
    public bool FinishCheck = false;        // 체간게이지차서 스턴 상태일때 피니쉬 가능여부 체크
    static public bool IsFinish = false;

    #endregion

    #region Intialization

    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        PlayerStat = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStat>();
        PlayerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        BossHitCollider = GetComponent<CapsuleCollider>();
        Audio = GetComponent<AudioSource>();
        Anim = GetComponent<Animator>();   
    }

    void Update()
    {
        StartCoroutine(AgentState());
        StartCoroutine(Action());
        StartCoroutine(OffPerilous());
        StartCoroutine(StunDelay());

        Speed = Agent.speed;
        Anim.SetFloat("Speed", Speed, 0.1f, Time.deltaTime);
      
        ViewUi();
    }

    private void LateUpdate()
    {
        FireTransform.transform.LookAt(new Vector3(PlayerTransform.position.x, PlayerTransform.position.y + 1.4f, PlayerTransform.position.z));
    }

    #endregion

    #region Function

    IEnumerator AgentState()
    {
        if (!IsDie)
        {
            float dist = Vector3.Distance(PlayerTransform.position, this.transform.position);

            if (dist <= AttackDist)
            {
                State = BossState.ATTACK;
            }
            else if (dist >= WalkDist && dist <= RunDist && Phase == BossPhase.PHASE_3 && Random.Range(0, 100) <= 40)
            {
                GunAttack();
            }
            else if (dist <= WalkDist)
            {
                State = BossState.WALK;
            }
            else if (dist <= RunDist)
            {
                State = BossState.RUN;
            }
            else
            {
                State = BossState.IDLE;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator Action()
    {
        if (!IsDie)
        {
            switch (State)
            {
                case BossState.IDLE:
                    Agent.isStopped = true;
                    Agent.speed = 0f;

                    break;

                case BossState.WALK:
                    Agent.isStopped = false;
                    Agent.speed = 1f;
                    Agent.destination = PlayerTransform.position;

                    LookTarget();
                    break;

                case BossState.RUN:
                    Agent.isStopped = false;
                    Agent.speed = 2f;
                    Agent.destination = PlayerTransform.position;
                    BGM.Play_BGM("Boss"); 
                    LookTarget();
                    break;

                case BossState.ATTACK:
                    Agent.isStopped = true;
                    Agent.speed = 0f;

                    LookTarget();
                    if (Phase == BossPhase.PHASE_1)
                    {
                        Anim.SetBool("Phase_1", true);
                        Anim.SetBool("Phase_2", false);
                        Anim.SetBool("Phase_3", false);
                        WalkDist = 4;
                        AttackDist = 2;
                        Attack_Phase1();
                    }
                    else if (Phase == BossPhase.PHASE_2)
                    {
                        Anim.SetBool("Phase_1", false);
                        Anim.SetBool("Phase_2", true);
                        Anim.SetBool("Phase_3", false);
                        WalkDist = 5;
                        AttackDist = 3;
                        Attack_Phase2();
                    }
                    else if (Phase == BossPhase.PHASE_3)
                    {
                        Anim.SetBool("Phase_1", false);
                        Anim.SetBool("Phase_2", false);
                        Anim.SetBool("Phase_3", true);
                        WalkDist = 5;
                        AttackDist = 3;
                        Attack_Phase3();
                    }
                    break;

                case BossState.BLOCK:
                    Block();
                    break;

                case BossState.HIT:

                    break;

                case BossState.DIE:

                    break;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator OffPerilous()
    {
        float elapsed = 0f;

        while (elapsed <= 1f && IsPerilous == true)
        {
            elapsed += Time.deltaTime;
            PerilousEffect.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            yield return null;
        }

        OffPerilousEffect();
    }

    IEnumerator StunDelay()
    {
        float elapsed = 0f;

        while (elapsed <= 3 && IsDelay == true)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        IsDelay = false;
        FinishCheck = false; // 체간게이지차서 스턴 상태일때 피니쉬 가능여부 체크
    }

    void LookTarget()
    {
        if (PlayerTransform != null)
        {
            Vector3 target = PlayerTransform.position - this.transform.position;
            Vector3 lookTarget = Vector3.Slerp(this.transform.forward, target.normalized, Time.deltaTime * 3f);
            transform.rotation = Quaternion.LookRotation(lookTarget);
        }
    }

    void Attack_Phase1()
    {
        if (IsDelay == false)
        {
            if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AnimDelayTime = Time.time + 4f;
                IsPerilous = false;
                Anim.SetTrigger("Attack_1");
            }
            else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AnimDelayTime = Time.time + 4f;
                IsPerilous = false;
                Anim.SetTrigger("Attack_2");
            }
            else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AnimDelayTime = Time.time + 4f;
                IsPerilous = false;
                Anim.SetTrigger("Attack_3");
            }
            else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AnimDelayTime = Time.time + 4f;
                IsPerilous = false;
                Anim.SetTrigger("Attack_4");
            }
            else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 50)
            {
                AnimDelayTime = Time.time + 2f;
                IsPerilous = true;
                Anim.SetTrigger("PerilousAttack_1");
            }           
        }
    }

    void Attack_Phase2()
    {
        if (IsDelay == false)
        {
            if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AnimDelayTime = Time.time + 2.5f;
                IsPerilous = false;
                Anim.SetTrigger("Attack_1");
            }
            else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AnimDelayTime = Time.time + 4f;
                IsPerilous = false;
                Anim.SetTrigger("Attack_2");
            }
            else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AnimDelayTime = Time.time + 4f;
                IsPerilous = false;
                Anim.SetTrigger("Attack_3");
            }
            else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AnimDelayTime = Time.time + 4f;
                IsPerilous = false;
                Anim.SetTrigger("Attack_4");
            }
            else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
            {
                AnimDelayTime = Time.time + 4f;
                IsPerilous = false;
                Anim.SetTrigger("Attack_5");
            }
            else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 50)
            {
                AnimDelayTime = Time.time + 3f;
                IsPerilous = true;
                Anim.SetTrigger("PerilousAttack_1");
            }
        }
    }

    void Attack_Phase3()
    {
        if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 2.5f;
            IsPerilous = false;
            Anim.SetTrigger("Attack_1");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 4f;
            IsPerilous = false;
            Anim.SetTrigger("Attack_2");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 4f;
            IsPerilous = false;
            Anim.SetTrigger("Attack_3");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 4f;
            IsPerilous = false;
            Anim.SetTrigger("Attack_4");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 4f;
            IsPerilous = false;
            Anim.SetTrigger("Attack_5");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 0.5f;
            IsPerilous = false;
            Anim.SetTrigger("Dodge_1");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 0.5f;
            IsPerilous = false;
            Anim.SetTrigger("Dodge_2");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 0.5f;
            IsPerilous = false;
            Anim.SetTrigger("Dodge_3");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 0.5f;
            IsPerilous = false;
            Anim.SetTrigger("Dodge_4");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 50)
        {
            AnimDelayTime = Time.time + 2f;
            IsPerilous = true;
            Anim.SetTrigger("PerilousAttack_1");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 50)
        {
            AnimDelayTime = Time.time + 2f;
            IsPerilous = true;
            Anim.SetTrigger("PerilousAttack_2");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 50)
        {
            AnimDelayTime = Time.time + 2f;
            IsPerilous = true;
            Anim.SetTrigger("PerilousAttack_3");
        }
    }

    void GunAttack()
    {
        if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 2f;
            IsPerilous = false;
            Anim.SetTrigger("GunAttack_1");
        }
        else if (AnimDelayTime <= Time.time && Random.Range(0, 100) <= 20)
        {
            AnimDelayTime = Time.time + 2f;
            IsPerilous = false;
            Anim.SetTrigger("GunAttack_2");
        }
    }

    public void Equip()
    {
        if (Phase == BossPhase.PHASE_2)
        {
            Katana.SetActive(false);
            Weapon = BossWeapon.SPEAR;
            Anim.SetTrigger("Equip_Spear");
        }
    }

    void Block()
    {
        Anim.SetTrigger("Block");
    }

    public void Hit()
    {
        int rand = Random.Range(0, 3);       

        if (rand == 0)
        {
            Anim.SetTrigger("Hit_0");
        }
        else if (rand == 1)
        {
            Anim.SetTrigger("Hit_1");
        }
        else if (rand == 2)
        {
            Anim.SetTrigger("Hit_2");
        }
        else if (rand == 3)
        {
            Anim.SetTrigger("Hit_3");
        }
    }

    public void ParryingToStun()
    {
        //IsDelay = true;
        Anim.SetTrigger("ParryStun");
        AnimDelayTime = Time.time + 0.5f;
        KatanaCollider.enabled = false;
        KatanaTrail.SetActive(false);
        SpearCollider.enabled = false;
        SpearTrail.SetActive(false);
    }

    public void Stun()
    {
        IsDelay = true;
        FinishCheck = true;
        Anim.SetTrigger("Stun");        
        OffPerilousEffect();
    }

    public void Regeneration()
    {
        if (Phase != BossPhase.PHASE_3)
        Anim.SetTrigger("Regeneration");

        KatanaCollider.enabled = false;
        KatanaTrail.SetActive(false);
        SpearCollider.enabled = false;
        SpearTrail.SetActive(false);
    }

    public void Die()
    {
        State = BossState.DIE;
        IsDie = true;
        Anim.SetTrigger("Die");
        Agent.enabled = false;
        BossHitCollider.enabled = false;
        KatanaCollider.enabled = false;
        KatanaTrail.SetActive(false);
        SpearCollider.enabled = false;
        SpearTrail.SetActive(false);
        HealthBar.SetActive(false);
        PostureBar.SetActive(false);
        Agent.tag = "Untagged";
        OffPerilousEffect();
        Destroy(this.gameObject, 15f);
        BGM.Play_BGM("Original");
    }

    void ViewUi()
    {
        if (State != BossState.IDLE)
        {
            HealthBar.SetActive(true);
            PostureBar.SetActive(true);
        }
        else
        {
            HealthBar.SetActive(false);
            PostureBar.SetActive(false);
        }

        if (FinishCheck == true)
        {
            FinishUI.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            FinishUI.SetActive(true);
            IsFinish = true; // Steath_KillBehaviour 에서 IsFinish값이 true되면 피니쉬 사용 가능
        }
        else if (FinishCheck == false)
        {
            FinishUI.SetActive(false);
            IsFinish = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Map"))
        {
            if (State == BossState.WALK)
            {
                Audio.PlayOneShot(WalkSFX[Random.Range(0, 3)], 0.4f);
            }
            else if (State == BossState.RUN)
            {
                Audio.PlayOneShot(RunSFX[Random.Range(0, 3)], 0.4f);
            }
        }
    }

    #endregion

    #region Animation Func

    void OnAttack()
    {
        KatanaCollider.enabled = true;
        Audio.PlayOneShot(AttackSFX[Random.Range(0, 3)], 1f);
    }

    void OffAttack()
    {
        KatanaCollider.enabled = false;
    }

    void OnTrail()
    {
        KatanaTrail.SetActive(true);
    }

    void OffTrail()
    {
        KatanaTrail.SetActive(false);
    }

    void OnAttack_Spear()
    {
        SpearCollider.enabled = true;
        Audio.PlayOneShot(AttackSFX[Random.Range(0, 3)], 1f);
    }

    void OffAttack_Spear()
    {
        SpearCollider.enabled = false;
    }

    void OnTrail_Spear()
    {
        SpearTrail.SetActive(true);
    }

    void OffTrail_Spear()
    {
        SpearTrail.SetActive(false);
    }

    void OnGun()
    {
        Gun.SetActive(true);
    }

    void OffGun()
    {
        Gun.SetActive(false);
    }

    void FireEffect()
    {

    }

    void GunFire()
    {       
        GameObject bullet = Instantiate(Bullet, FireTransform.position, FireTransform.rotation);
        Destroy(bullet, 20);
    }

    void FireSFX()
    {
        Audio.PlayOneShot(GunSFX, 1f);
    }

    void OnBlock()
    {
        IsBlock = true;
    }

    void OffBlock()
    {
        IsBlock = false;
    }

    void OnReset()
    {
        IsDelay = false;
        AttackDist = 2f;
        WalkDist = 4f;
        RunDist = 15f;
        IsInvincibility = false;
    }

    void OffReset()
    {
        IsDelay = true;
        AttackDist = 0f;
        WalkDist = 0f;
        RunDist = 0f;
        KatanaCollider.enabled = false;
        KatanaTrail.SetActive(false);
        IsInvincibility = true;
    }

    void OnPerilousEffect()
    {
        Audio.PlayOneShot(PerilousSFX, 1f);
        PerilousEffect.SetActive(true);
    }

    void OffPerilousEffect()
    {
        PerilousEffect.SetActive(false);
    }

    void Explosion()
    {
        GameObject explosionEffect = Instantiate(ExplosionEffect, transform.position, transform.rotation);
        Destroy(explosionEffect, 5f);
    }

    void SkillEffect_1()
    {
        var Cam = Camera.main;
        
        GameObject skillEffect = Instantiate(SkillEffect[0], PlayerTransform.position, Quaternion.LookRotation(Cam.transform.forward));
        Destroy(skillEffect, 10f);

        // Position.y + 0.1f y값이 0이면 땅에 안닿아서 뚫고 떨어짐 그래서 y값만 +0.1f
        GameObject skillEffect2 = Instantiate(SkillEffect[1], new Vector3(PlayerTransform.position.x, PlayerTransform.position.y + 0.1f, PlayerTransform.position.z), PlayerTransform.rotation);
        Destroy(skillEffect2, 8f);

        Shake.ShakeCamera(7f, 0.5f);
    }

    void Equip_Spear()
    {
        Spear.SetActive(true);
        UnEquipSpear.SetActive(false);
    }

    void OnKick()
    {
        KickCollider.enabled = true;
    }

    void OffKick()
    {
        KickCollider.enabled = false;
    }

    void OnBlood()
    {
        Audio.PlayOneShot(StabSFX, 1f);

        GameObject blood = Instantiate(BloodEffect, BloodTransform.position, BloodTransform.rotation);

        Destroy(blood, 2);
    }

    #endregion
}
