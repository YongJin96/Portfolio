using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class PlayerMovement : MonoBehaviour
{
    #region Player Variables

    private CharacterController PlayerController;
    private Animator PlayerAnim;
    private Camera Cam;
    private CinemachineCameraOffset CamOffset;
    public CinemachineShake Shake;
    [HideInInspector]
    public AudioSource Audio;
    private Targeting targeting;
    private FreeClimb freeClimb;
    private Bow bow;
    private Horse horse;

    private float InputX;
    private float InputY;
    [HideInInspector]
    public float Speed;
    private float DelayTime;
    private float DelayTime2;
    private float BuffTime = 20f;
    private int AttackCount = 0;
    private int StrongAttackCount = 0;
    private int HorseAttackCount = 0;

    private Vector3 DesiredMoveDirection;

    public enum E_PlayerState
    {
        IDLE,
        WALK,
        RUN,
        JUMP,
        ATTACK,
        HIT,
        DIE
    }

    public enum E_AttackType
    {
        TYPE_A,
        TYPE_B,
        TYPE_C
    }

    public enum E_WeaponType
    {
        KATANA,
        BOW
    }

    [Header("Player Info")]
    public E_PlayerState PlayerState;
    public float WalkSpeed;
    public float RunSpeed;
    public float JumpForce;
    public float Gravity;

    public float AllowPlayerRotation;
    public float DesiredRotationSpeed;

    public float SlopeForce;
    public float SlopeForceRayLength;

    public bool BlockRotationPlayer = false;
    public bool IsGrounded = true;
    public bool IsJump = false;
    public bool IsHit = false;
    public bool IsBlock = false;
    public bool IsParrying = false;
    public bool IsCrouch = false; 
    public bool IsDodge = false;
    public bool IsStealth = false;
    public bool IsAiming = false;
    public bool IsVault = false;
    public bool IsLedgeGrab = false;
    public bool IsMount = false;
    public bool IsDie = false;   

    [Header("Player Weapons")]
    public E_AttackType AttackType;
    public E_WeaponType WeaponType;
    public GameObject Katana;
    public GameObject Katana2;
    public GameObject Dagger;
    public GameObject Bow_Equip;
    public GameObject Bow_UnEquip;
    public BoxCollider BladeCollider;
    public BoxCollider LeftKickCollider;
    public BoxCollider RightKickCollider;

    [Header("Weapon Effect")]
    public GameObject Trail;
    public MeshRenderer WeaponRenderer;
    public Material OriginKatanaMat;
    public Material FireMat;
    public GameObject FireEffect;
    public GameObject FireEffect2;

    public bool IsWeapon = false;
    static public bool IsFireWeapon = false;
    static public bool IsPerilousAttack = false;

    [Header("Player SFX")]
    public AudioClip[] KatanaSFX;
    public AudioClip EquipSFX;
    public AudioClip UnEquipSFX;
    public AudioClip[] HitSFX;
    public AudioClip[] BlockHitSFX;
    public AudioClip StealthKillSFX;
    public AudioClip[] WalkSFX;
    public AudioClip[] RunSFX;
    public AudioClip[] LandSFX;
    public AudioClip HighLandSFX;
    public AudioClip ChargeAttackSFX;
    public AudioClip Whistle;

    [Header("Effect")]
    public GameObject Effect;
    public Transform EffectPos;

    [Header("Item")]
    public GameObject Fog;
    public Transform LeftHand;

    static public bool IsCut = false;

    #endregion

    #region Initialization

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void Awake()
    {
        PlayerController = GetComponent<CharacterController>();
        PlayerAnim = GetComponent<Animator>();
        Cam = Camera.main;
        CamOffset = GameObject.Find("CM VPlayer").GetComponent<CinemachineCameraOffset>();
        //Shake = Cam.gameObject.GetComponent<CinemachineShake>();
        Audio = GetComponent<AudioSource>();
        targeting = GetComponent<Targeting>();
        freeClimb = GetComponent<FreeClimb>();
        bow = GetComponent<Bow>();
        horse = GameObject.FindGameObjectWithTag("Horse").GetComponent<Horse>();

        PlayerAnim.SetBool("TypeA", true); // 시작시 기본 타입 A로 설정

        BuffTime = 20f;

        // StartScene에서 게임시작시 말타고 시작하기 위해
        if (IsMount == true && horse.IsMount == true)
        {
            PlayerAnim.SetBool("IsMount", true);

            if (IsWeapon == true)
            {
                PlayerAnim.SetTrigger("Equip");
                PlayerAnim.SetBool("IsWeapon", true);
                WeaponType = E_WeaponType.KATANA;
            }
        }
    }

    private void Update()
    {
        if (IsDie == false && targeting.IsTargeting == false && IsAiming == false && IsMount == false && Stealth_KillBehaviour.Kill == false && Cliff.IsCliff == false && IsLedgeGrab == false && IsVault == false)
        {
            InputMagnitude();
        }
        else if (IsDie == false && targeting.IsTargeting == true || IsAiming == true) // 적 타겟팅 시 또는 활 조준 시
        {
            targeting.InputMagnitude();
        }

        StartCoroutine(CheckState());
        WeaponTypeState();
        StartCoroutine(HitCheck());
        StartCoroutine(BuffTimer());
        StartCoroutine(ParryingTimer());

        if (PlayerController.isGrounded)
        {
            IsGrounded = true;
            PlayerAnim.SetBool("IsGrounded", true);
            IsJump = false;
            PlayerAnim.SetBool("IsJump", false);
            IsLedgeGrab = false;
        }

        if (IsGrounded == true)
        {
            Equip();         
            Block();
            FireWeapon();

            if (IsMount == false)
            {
                if(IsDie == false && targeting.IsTargeting == false && IsVault == false) { Jump(); }   
                if (WeaponType == E_WeaponType.KATANA)
                {
                    AttackTypeA();
                    AttackTypeB();
                    AttackTypeC();

                    StrongAttack_TypeA();
                    StrongAttack_TypeB();
                    StrongAttack_TypeC();

                    ChargeAttack();
                    ParryingToAttack();
                }
                else if (WeaponType == E_WeaponType.BOW)
                {
                    BowAttack();
                }
                Dodge();
                RunAttack();
                Parry();
                Crouch();
                Kick();               
                CheckVault();
            }
            else if (IsMount == true)
            {
                Speed = 0f;

                if (WeaponType == E_WeaponType.KATANA)
                {
                    HorseAttack();
                }
                else if (WeaponType == E_WeaponType.BOW)
                {
                    BowAttack();
                }
            }

            freeClimb.RayDistance = 1f;
        }

        if (IsGrounded == false && IsJump == true)
        {
            freeClimb.CheckForClimb();
            LedgeGrab();
        }
        if (IsLedgeGrab == true)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, transform.position.y + 1.4f, transform.position.z), Time.deltaTime);
        }

        JumpAttack();
        JumpKick();
        Falling();
        Throw();

        // Cinemachine Camera Offset
        CrouchCameraOffset();
    }

    #endregion

    #region Player Function

    private IEnumerator CheckState()
    {
        if (IsDie == false)
        {
            switch (PlayerState)
            {
                case E_PlayerState.IDLE:
                    PlayerAnim.SetFloat("Speed", Speed, 0.1f, Time.deltaTime); // dampTime 부드럽게 전환해주는 값     
                    if (targeting.IsTargeting == false) { PlayerAnim.SetFloat("Direction", 0f, 0.1f, Time.deltaTime); } // targeting.IsTargeting == false되면 DIrection값 초기화
                    PlayerAnim.SetBool("IsRun", false);
                    break;

                case E_PlayerState.WALK:
                    PlayerAnim.SetFloat("Speed", Speed / 2, 0.1f, Time.deltaTime);
                    if (targeting.IsTargeting == false) { PlayerAnim.SetFloat("Direction", 0f, 0.1f, Time.deltaTime); }              
                    PlayerAnim.SetBool("IsRun", false);
                    break;

                case E_PlayerState.RUN:
                    PlayerAnim.SetFloat("Speed", Speed, 0.1f, Time.deltaTime);
                    if (targeting.IsTargeting == false) { PlayerAnim.SetFloat("Direction", 0f, 0.1f, Time.deltaTime); }
                    PlayerAnim.SetBool("IsRun", true);
                    break;

                case E_PlayerState.JUMP:
                    PlayerAnim.SetBool("IsGrounded", false);
                    PlayerAnim.SetBool("IsJump", true);
                    break;

                case E_PlayerState.ATTACK:
                    break;

                case E_PlayerState.HIT:
                    PlayerAnim.SetTrigger("Hit");
                    break;

                case E_PlayerState.DIE:
                    Die();
                    break;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void WeaponTypeState()
    {
        switch (WeaponType)
        {
            case E_WeaponType.KATANA:
                PlayerAnim.SetBool("IsKatana", true);
                PlayerAnim.SetBool("IsBow", false);
                break;

            case E_WeaponType.BOW:
                PlayerAnim.SetBool("IsKatana", false);
                PlayerAnim.SetBool("IsBow", true);
                break;
        }
    }

    private void PlayerMoveAndRotation()
    {
        InputX = Input.GetAxis("Horizontal");
        InputY = Input.GetAxis("Vertical");

        var camera = Camera.main;
        var forward = Cam.transform.forward;
        var right = Cam.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        DesiredMoveDirection = forward * InputY + right * InputX;
        DesiredMoveDirection.Normalize();

        if (BlockRotationPlayer == false)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(DesiredMoveDirection), DesiredRotationSpeed);
        }
    }

    private void InputMagnitude()
    {
        InputX = Input.GetAxis("Horizontal");
        InputY = Input.GetAxis("Vertical");

        PlayerAnim.SetFloat("InputX", InputX, 0.1f, Time.deltaTime);
        PlayerAnim.SetFloat("InputY", InputY, 0.1f, Time.deltaTime);

        Speed = new Vector2(InputX, InputY).normalized.sqrMagnitude;

        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? RunSpeed : WalkSpeed;

        //PlayerController.SimpleMove(DesiredMoveDirection * moveSpeed); // 루트모션써서 필요없음       

        if (Speed > AllowPlayerRotation)
        {
            PlayerMoveAndRotation();
        }
        else if (Speed < AllowPlayerRotation)
        {
            
        }

        if (IsGrounded == true)
        {
            if (InputX == 0 && InputY == 0)
            {
                DesiredMoveDirection = Vector3.zero;
                PlayerState = E_PlayerState.IDLE;
            }
            else
            {
                if (moveSpeed == WalkSpeed)
                {
                    PlayerState = E_PlayerState.WALK;
                }
                else if (moveSpeed == RunSpeed)
                {
                    PlayerState = E_PlayerState.RUN;
                }
            }
        }
        else
        {
            PlayerState = E_PlayerState.JUMP;
        }

        if (OnSlope() && IsJump == false && IsMount == false)
        {
            PlayerController.Move(Vector3.down * SlopeForce * Time.deltaTime);
        }

        PlayerAnim.SetBool("IsGrounded", IsGrounded);
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                IsGrounded = false;
                IsJump = true;
                StartCoroutine(JumpCoroutine());
                PlayerAnim.SetTrigger("Jump");
            }
        }
    }

    private IEnumerator JumpCoroutine()
    {
        float jump = JumpForce;
        do
        {
            PlayerController.Move(Vector3.up * jump * Time.deltaTime);
            if (Speed >= 0.5f)
            {
                PlayerController.Move(transform.forward * 6f * Time.deltaTime);
            }
            jump -= Time.deltaTime * Gravity;
            yield return null;
        }
        while (IsGrounded == false);
    }

    private IEnumerator HitCheck()
    {
        float elapsed = 0f;

        while (elapsed <= 1f && IsHit == true)
        {
            elapsed += Time.deltaTime;
            BladeCollider.enabled = false;
            Trail.SetActive(false);
            IsPerilousAttack = false;
            IsBlock = false;
            IsParrying = false;

            yield return null;
        }
        IsHit = false;
    }

    private IEnumerator BuffTimer()
    {
        float elapsed = 0f;

        while (elapsed <= BuffTime && IsFireWeapon == true)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        IsFireWeapon = false;
        Material[] mat = WeaponRenderer.GetComponent<MeshRenderer>().materials;
        mat[1] = OriginKatanaMat;
        WeaponRenderer.GetComponent<MeshRenderer>().materials = mat;
        FireEffect.SetActive(false);
    }

    private IEnumerator ParryingTimer()
    {
        float elapsed = 0f;

        while (elapsed <= 0.1f && IsParrying == true)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        IsParrying = false;
    }

    private bool OnSlope()
    {
        if (IsJump)
        {
            return false;
        }

        RaycastHit hit;

        if (Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0f, 0.2f, 0f)), Vector3.down, out hit, PlayerController.height / 2 * SlopeForceRayLength))
        {
            if (hit.normal != Vector3.up)
            {
                return true;
            }
        }

        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0f, 0.2f, 0f)), Vector3.down * (PlayerController.height / 2 * SlopeForceRayLength), Color.yellow);

        return false;
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.layer == LayerMask.NameToLayer("Map") || coll.gameObject.layer == LayerMask.NameToLayer("Obstacle") || coll.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (PlayerState == E_PlayerState.WALK)
            {
                Audio.PlayOneShot(WalkSFX[Random.Range(0, 3)], 0.2f);
            }
            else if (PlayerState == E_PlayerState.RUN)
            {
                Audio.PlayOneShot(RunSFX[Random.Range(0, 3)], 0.2f);
            }

            IsGrounded = true;
            PlayerAnim.SetBool("IsGrounded", true);
        }
    }

    private void OnTriggerStay(Collider coll)
    {
        if (coll.gameObject.CompareTag("Bush") && IsCrouch == true)
        {
            IsStealth = true;
        }
    }

    private void OnTriggerExit(Collider coll)
    {
        if (coll.gameObject.CompareTag("Bush"))
        {
            IsStealth = false;
        }
    }

    private void AttackTypeA()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKey(KeyCode.LeftShift) && IsWeapon == true && IsBlock == false && AttackType == E_AttackType.TYPE_A && IsHit == false && PlayerStat.ParryingSuccess == false)
        {
            StrongAttackCount = 0;

            if (DelayTime <= Time.time && AttackCount == 0)
            {
                DelayTime = Time.time + 0.5f;
                PlayerAnim.SetTrigger("Attack1");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 1)
            {
                DelayTime = Time.time + 0.5f;
                PlayerAnim.SetTrigger("Attack2");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 2)
            {
                DelayTime = Time.time + 0.6f;
                PlayerAnim.SetTrigger("Attack3");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 3)
            {
                DelayTime = Time.time + 0.5f;
                PlayerAnim.SetTrigger("Attack4");
                AttackCount = 0;
            }
        }
    }

    private void AttackTypeB()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKey(KeyCode.LeftShift) && IsWeapon == true && IsBlock == false && AttackType == E_AttackType.TYPE_B && IsHit == false && PlayerStat.ParryingSuccess == false)
        {
            StrongAttackCount = 0;

            if (DelayTime <= Time.time && AttackCount == 0)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("Attack_B1");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 1)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("Attack_B2");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 2)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("Attack_B3");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 3)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("Attack_B4");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 4)
            {
                DelayTime = Time.time + 0.6f;
                PlayerAnim.SetTrigger("Attack_B5");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 5)
            {
                DelayTime = Time.time + 0.8f;
                PlayerAnim.SetTrigger("Attack_B6");
                AttackCount = 0;
            }
        }
    }

    private void AttackTypeC()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && !Input.GetKey(KeyCode.LeftShift) && IsWeapon == true && IsBlock == false && AttackType == E_AttackType.TYPE_C && IsHit == false && PlayerStat.ParryingSuccess == false)
        {
            StrongAttackCount = 0;

            if (DelayTime <= Time.time && AttackCount == 0)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("Attack_C1");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 1)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("Attack_C2");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 2)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("Attack_C3");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 3)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("Attack_C4");
                ++AttackCount;
            }
            else if (DelayTime <= Time.time && AttackCount == 4)
            {
                DelayTime = Time.time + 1f;
                PlayerAnim.SetTrigger("Attack_C5");
                AttackCount = 0;
            }
        }
    }

    private void StrongAttack_TypeA()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && Input.GetKey(KeyCode.LeftShift) && IsWeapon == true && IsBlock == false && AttackType == E_AttackType.TYPE_A && IsHit == false && PlayerStat.ParryingSuccess == false)
        {
            AttackCount = 0;

            if (DelayTime <= Time.time && StrongAttackCount == 0)
            {
                DelayTime = Time.time + 0.6f;
                PlayerAnim.SetTrigger("S_Attack_A1");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 1)
            {
                DelayTime = Time.time + 0.7f;
                PlayerAnim.SetTrigger("S_Attack_A2");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 2)
            {
                DelayTime = Time.time + 0.5f;
                PlayerAnim.SetTrigger("S_Attack_A3");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 3)
            {
                DelayTime = Time.time + 1.4f;
                PlayerAnim.SetTrigger("S_Attack_A4");
                StrongAttackCount = 0;
            }
        }
    }

    private void StrongAttack_TypeB()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && Input.GetKey(KeyCode.LeftShift) && IsWeapon == true && IsBlock == false && AttackType == E_AttackType.TYPE_B && IsHit == false && PlayerStat.ParryingSuccess == false)
        {
            AttackCount = 0;

            if (DelayTime <= Time.time && StrongAttackCount == 0)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("S_Attack_B1");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 1)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("S_Attack_B2");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 2)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("S_Attack_B3");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 3)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("S_Attack_B4");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 4)
            {
                DelayTime = Time.time + 1.6f;
                PlayerAnim.SetTrigger("S_Attack_B5");
                StrongAttackCount = 0;
            }
        }
    }

    private void StrongAttack_TypeC()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && Input.GetKey(KeyCode.LeftShift) && IsWeapon == true && IsBlock == false && AttackType == E_AttackType.TYPE_C && IsHit == false && PlayerStat.ParryingSuccess == false)
        {
            AttackCount = 0;

            if (DelayTime <= Time.time && StrongAttackCount == 0)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("S_Attack_C1");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 1)
            {
                DelayTime = Time.time + 0.4f;
                PlayerAnim.SetTrigger("S_Attack_C2");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 2)
            {
                DelayTime = Time.time + 0.5f;
                PlayerAnim.SetTrigger("S_Attack_C3");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 3)
            {
                DelayTime = Time.time + 0.5f;
                PlayerAnim.SetTrigger("S_Attack_C4");
                ++StrongAttackCount;
            }
            else if (DelayTime <= Time.time && StrongAttackCount == 4)
            {
                DelayTime = Time.time + 0.8f;
                PlayerAnim.SetTrigger("S_Attack_C5");
                StrongAttackCount = 0;
            }
        }
    }

    private void RunAttack()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                PlayerAnim.SetTrigger("RunAttack");

                if (IsWeapon == false)
                {
                    PlayerAnim.SetBool("IsWeapon", true);
                }
            }
        }
    }

    private void JumpAttack()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && IsWeapon == true && IsJump == true && IsHit == false)
        {
            if (DelayTime2 <= Time.time)
            {
                DelayTime2 = Time.time + 2f;
                PlayerAnim.SetTrigger("JumpAttack");
            }
        }
    }

    public void BowAttack()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            PlayerAnim.SetTrigger("Aim");
            if (Input.GetKey(KeyCode.Mouse1))
            {             
                PlayerAnim.SetBool("IsAiming", true);
                IsAiming = true;
            }
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            PlayerAnim.SetBool("IsAiming", false);
            IsAiming = false;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) && IsAiming == true)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.5f;
                PlayerAnim.SetTrigger("Fire");            
            }
        }
    }

    public void ChangeWeapon()
    {
        if (WeaponType == E_WeaponType.BOW)
        {
            PlayerAnim.SetTrigger("ChangeWeapon_Bow");
            WeaponType = E_WeaponType.BOW;

            IsWeapon = true;
            PlayerAnim.SetBool("IsWeapon", true);

            // Bow 외 다른무기 false
            Katana.SetActive(false);
            Katana2.SetActive(true);
        }
        else if (WeaponType == E_WeaponType.KATANA)
        {
            PlayerAnim.SetTrigger("Equip");
            WeaponType = E_WeaponType.KATANA;

            IsWeapon = true;
            PlayerAnim.SetBool("IsWeapon", true);

            // Katana 외 다른무기 false
            Bow_Equip.SetActive(false);
            Bow_UnEquip.SetActive(true);
        }
    }

    private void Block()
    {
        if (Input.GetKey(KeyCode.Mouse1) && IsWeapon == true && WeaponType != E_WeaponType.BOW && IsHit == false && IsDodge == false)
        {
            PlayerAnim.SetTrigger("Block");
            PlayerAnim.SetBool("IsBlock", true);
            IsBlock = true;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            PlayerAnim.SetBool("IsBlock", false);
            IsBlock = false;
        }
    }

    private void Parry()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1) && IsWeapon == true && WeaponType != E_WeaponType.BOW && IsHit == false)
        {
            IsParrying = true;
        }
    }

    private void Equip()
    {
        if (Input.GetKeyDown(KeyCode.R) && IsWeapon == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                PlayerAnim.SetTrigger("Equip");
                PlayerAnim.SetBool("IsWeapon", true);
                WeaponType = E_WeaponType.KATANA;
            }
        }
        else if (Input.GetKeyDown(KeyCode.R) && IsWeapon == true && WeaponType == E_WeaponType.KATANA)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 3f;
                PlayerAnim.SetTrigger("UnEquip");
                PlayerAnim.SetBool("IsWeapon", false);

                AttackCount = 0;
                // 공격타입 A로 초기화
                AttackType = E_AttackType.TYPE_A;
                PlayerAnim.SetBool("TypeA", true);
                PlayerAnim.SetBool("TypeB", false);
                PlayerAnim.SetBool("TypeC", false);

                WeaponType = E_WeaponType.KATANA;
            }
        }
        else if (Input.GetKeyDown(KeyCode.R) && IsWeapon == true && WeaponType == E_WeaponType.BOW)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                PlayerAnim.SetTrigger("UnEquip_Bow");
                PlayerAnim.SetBool("IsWeapon", false);
                IsWeapon = false;
                AttackCount = 0;

                WeaponType = E_WeaponType.KATANA;
            }
        }
    }

    public void ChangeType()
    {
        if (WeaponType == E_WeaponType.KATANA)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && IsWeapon == true && AttackType != E_AttackType.TYPE_A)
            {
                if (DelayTime <= Time.time)
                {
                    PlayerAnim.SetTrigger("ChangeType");
                    DelayTime = Time.time + 0.5f;
                    PlayerAnim.SetBool("TypeA", true);
                    PlayerAnim.SetBool("TypeB", false);
                    PlayerAnim.SetBool("TypeC", false);
                    AttackType = E_AttackType.TYPE_A;
                    AttackCount = 0;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) && IsWeapon == true && AttackType != E_AttackType.TYPE_B)
            {
                if (DelayTime <= Time.time)
                {
                    PlayerAnim.SetTrigger("ChangeType");
                    DelayTime = Time.time + 0.5f;
                    PlayerAnim.SetBool("TypeA", false);
                    PlayerAnim.SetBool("TypeB", true);
                    PlayerAnim.SetBool("TypeC", false);
                    AttackType = E_AttackType.TYPE_B;
                    AttackCount = 0;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) && IsWeapon == true && AttackType != E_AttackType.TYPE_C)
            {
                if (DelayTime <= Time.time)
                {
                    PlayerAnim.SetTrigger("ChangeType");
                    DelayTime = Time.time + 0.5f;
                    PlayerAnim.SetBool("TypeA", false);
                    PlayerAnim.SetBool("TypeB", false);
                    PlayerAnim.SetBool("TypeC", true);
                    AttackType = E_AttackType.TYPE_C;
                    AttackCount = 0;
                }
            }
        }
    }

    private void ChargeAttack()
    {
        if (StealthKill.OnKill == false && IsFireWeapon == true && IsMount == false)
        {
            if (Input.GetKey(KeyCode.E) && IsWeapon == true)
            {
                if (DelayTime <= Time.time)
                {
                    DelayTime = Time.time + 1f;
                    PlayerAnim.SetTrigger("Charge");
                }
            }
            else if (Input.GetKeyUp(KeyCode.E) && IsWeapon == true)
            {
                PlayerAnim.SetTrigger("ChargeAttack");
                Audio.PlayOneShot(ChargeAttackSFX, 1);
                IsPerilousAttack = true;
                IsFireWeapon = false;
                Material[] mat = WeaponRenderer.GetComponent<MeshRenderer>().materials;
                mat[1] = OriginKatanaMat;
                WeaponRenderer.GetComponent<MeshRenderer>().materials = mat;
                FireEffect.SetActive(false);
            }
        }
    }

    private void ParryingToAttack()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && PlayerStat.ParryingSuccess == true)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                PlayerAnim.SetTrigger("ParryingToAttack");             
            }
        }
    }

    private void Kick()
    {
        if (Input.GetKeyDown(KeyCode.F) && !Input.GetKey(KeyCode.LeftShift) && IsJump == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                PlayerAnim.SetTrigger("Kick");
            }
        }
        if (Input.GetKeyDown(KeyCode.F) && Input.GetKey(KeyCode.LeftShift) && IsJump == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                PlayerAnim.SetTrigger("RunKick");
            }
        }
    }

    private void JumpKick()
    {
        if (Input.GetKeyDown(KeyCode.F) && IsJump == true)
        {
            if (DelayTime2 <= Time.time)
            {
                DelayTime2 = Time.time + 1f;
                PlayerAnim.SetTrigger("JumpKick");
            }
        }
    }

    private void Dodge()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && targeting.IsTargeting == false && horse.IsMount == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.6f;
                PlayerAnim.SetTrigger("Dodge");
            }
        }
    }

    private void Crouch()
    {
        if (Input.GetKeyDown(KeyCode.C) && IsCrouch == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.2f;
                PlayerAnim.SetBool("IsCrouch", true);
                IsCrouch = true;

                // 콜라이더 크기 줄이기
                PlayerController.center = new Vector3(0f, 0.5f, 0f);
                PlayerController.height = 1f;
            }
        }
        else if (Input.GetKeyDown(KeyCode.C) && IsCrouch == true)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.2f;
                PlayerAnim.SetBool("IsCrouch", false);
                IsCrouch = false;
                IsStealth = false;

                // 기본값으로 초기화
                PlayerController.center = new Vector3(0f, 1f, 0f);
                PlayerController.height = 1.8f;
            }
        }
    }

    private void Falling()
    {
        if (IsGrounded == true || IsLedgeGrab == true) { return; }

        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("Map");

        if (Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0f, 0.1f, 0f)), -transform.up, out hit, layerMask))
        {
            if (hit.distance >= 3f)
            {
                PlayerAnim.SetBool("IsFalling", true);
            }
            else if (hit.distance < 3f)
            {
                PlayerAnim.SetBool("IsFalling", false);              
            }
        }

        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0f, 0.1f, 0f)), -transform.up * 3f, Color.yellow);
    }

    public void Hit()
    {
        PlayerAnim.SetTrigger("Hit");
        Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
        IsHit = true;
    }

    public void PerilousHit()
    {
        PlayerAnim.SetTrigger("Perilous Hit");
        Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
        IsHit = false;
    }

    public void BlockHit()
    {
        PlayerAnim.SetTrigger("BlockHit");
        Audio.PlayOneShot(BlockHitSFX[Random.Range(0, 8)], 1f);
    }

    public void Stun()
    {
        PlayerAnim.SetTrigger("Stun");
    }

    public void Die()
    {
        PlayerAnim.SetTrigger("Die");
        IsDie = true;
        this.enabled = false;
        PlayerController.enabled = false;
        PlayerController.gameObject.tag = "Untagged"; // 적이 죽어도 추적해서 추적못하게 태그를 변경
        targeting.IsTargeting = false;
        targeting.TargetingUI.SetActive(false);
        targeting.enabled = false;
    }

    public void OnController()
    {
        PlayerController.enabled = true;
    }

    public void OffController()
    {
        PlayerController.enabled = false;
    }

    public void HorseMount()
    {
        if (IsMount == false)
        {
            PlayerAnim.SetBool("IsMount", true);
            IsMount = true;
        }
        else if (IsMount == true)
        {
            PlayerAnim.SetBool("IsMount", false);
            IsMount = false;
        }
    }

    public void HorseState()
    {
        InputX = Input.GetAxis("Horizontal");
        InputY = Input.GetAxis("Vertical");

        PlayerAnim.SetFloat("InputX", InputX, 0.1f, Time.deltaTime);
        PlayerAnim.SetFloat("InputY", InputY, 0.1f, Time.deltaTime);

        if (horse.HorseState == Horse.E_HorseState.IDLE)
        {
            PlayerAnim.SetFloat("InputX", InputX, 0.1f, Time.deltaTime);
            PlayerAnim.SetFloat("InputY", InputY, 0.1f, Time.deltaTime);           
        }
        else if (horse.HorseState == Horse.E_HorseState.WALK)
        {
            PlayerAnim.SetFloat("InputX", InputX * 0.1f, 0.1f, Time.deltaTime);
            PlayerAnim.SetFloat("InputY", InputY * 0.1f, 0.1f, Time.deltaTime);
        }
        else if (horse.HorseState == Horse.E_HorseState.RUN)
        {
            PlayerAnim.SetFloat("InputX", InputX, 0.1f, Time.deltaTime);
            PlayerAnim.SetFloat("InputY", InputY, 0.1f, Time.deltaTime);
        }
    }

    private void HorseAttack()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) &&  IsWeapon == true && HorseAttackCount == 0)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.7f;
                PlayerAnim.SetTrigger("HorseAttack0");
                ++HorseAttackCount;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Mouse0) && IsWeapon == true && HorseAttackCount == 1)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.7f;
                PlayerAnim.SetTrigger("HorseAttack1");
                HorseAttackCount = 0;
            }
        }
    }

    public void HorseCall()
    {
        Audio.PlayOneShot(Whistle, 1f);
    }

    private void CheckVault()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position + transform.TransformDirection(0f, 0.5f, 0f), transform.forward, out hit, 0.5f, 1 << LayerMask.NameToLayer("Wall")))
        {
            IsVault = true;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                PlayerAnim.SetTrigger("Vault");
                OffController();
            }

            PlayerController.transform.position = Vector3.Lerp(PlayerController.transform.position, hit.point, Time.deltaTime * 5f);
        }
        
        if (IsVault == true)
        {           
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                IsVault = false;
                OnController();
            }
        }

        Debug.DrawRay(transform.position + transform.TransformDirection(0f, 0.5f, 0f), transform.forward * 0.5f, Color.green);
    }

    private void FireWeapon()
    {
        if (Input.GetKeyDown(KeyCode.T) && IsWeapon == true && WeaponType == E_WeaponType.KATANA && IsFireWeapon == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                PlayerAnim.SetTrigger("FireWeapon");
                IsFireWeapon = true;
            }
        }      
    }

    private void Throw()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            PlayerAnim.SetTrigger("Throw");
        }
    }

    public void FallingToHorse()
    {
        PlayerAnim.SetTrigger("FallingToHorse");
        PlayerAnim.SetBool("IsMount", false);
    }

    private void LedgeGrab()
    {
        if (IsLedgeGrab == true) { return; }

        RaycastHit hit;

        if (Physics.Raycast(transform.position + transform.TransformDirection(0f, 2.3f, 0f), transform.forward, out hit, 0.5f, 1 << LayerMask.NameToLayer("LedgeGrab")))
        {
            PlayerAnim.SetTrigger("LedgeGrab");
            IsLedgeGrab = true;
            OffController();
        }        

        Debug.DrawRay(transform.position + transform.TransformDirection(0f, 2.3f, 0f), transform.forward * 0.5f, Color.green);
    }

    #endregion

    #region Cinemachine Function

    private void CrouchCameraOffset()
    {
        if (IsCrouch == true)
        {
            CamOffset.m_Offset = Vector3.Lerp(CamOffset.m_Offset, new Vector3(-0.25f, 0f, 1f), Time.deltaTime * 5f);
        }
        else if (IsCrouch == false)
        {
            CamOffset.m_Offset = Vector3.Lerp(CamOffset.m_Offset, new Vector3(0f, 0f, 0f), Time.deltaTime * 5f);
        }
    }

    #endregion

    #region Animation Function

    private void OnWeapon()
    {
        Katana.SetActive(true);
        Katana2.SetActive(false);
        Audio.PlayOneShot(EquipSFX, 0.5f);
        IsWeapon = true;
    }

    private void OffWeapon()
    {
        Katana.SetActive(false);
        Katana2.SetActive(true);
        Audio.PlayOneShot(UnEquipSFX, 0.5f);
        IsWeapon = false;
    }

    private void OnDagger()
    {
        Dagger.SetActive(true);
    }

    private void OffDagger()
    {
        Dagger.SetActive(false);
    }

    private void OnBow()
    {
        Bow_Equip.SetActive(true);
        Bow_UnEquip.SetActive(false);
    }

    private void OffBow()
    {
        Bow_Equip.SetActive(false);
        Bow_UnEquip.SetActive(true);
    }

    private void OnAttack()
    {
        BladeCollider.enabled = true;
        IsBlock = false;
        Audio.PlayOneShot(KatanaSFX[Random.Range(0, 5)], 1f);
    }

    private void OffAttack()
    {
        BladeCollider.enabled = false;
        IsBlock = false;
    }

    private void OffPerilousAttack()
    {
        IsPerilousAttack = false;
    }

    private void OnTrail()
    {
        Trail.SetActive(true);
    }

    private void OffTrail()
    {
        Trail.SetActive(false);
    }

    private void OnKick_L()
    {
        LeftKickCollider.enabled = true;
    }

    private void OffKick_L()
    {
        LeftKickCollider.enabled = false;
    }

    private void OnKick_R()
    {
        RightKickCollider.enabled = true;
    }

    private void OffKick_R()
    {
        RightKickCollider.enabled = false;
    }

    private void OnParrying()
    {
        IsParrying = true;
    }

    private void OffParrying()
    {
        IsParrying = false;
    }

    private void OnEffect()
    {
        GameObject effect = Instantiate(Effect, EffectPos.position, EffectPos.rotation);
        Destroy(effect, 1f);
    }

    private void OnJump()
    {
        // 점프했을때 위치로 콜라이더 위치 변경
        PlayerController.center = new Vector3(0f, 1.8f, 0f);
        PlayerController.height = 1.5f;
    }

    private void OffJump()
    {
        // 기본값으로 초기화
        PlayerController.center = new Vector3(0f, 1f, 0f);
        PlayerController.height = 1.8f;
    }

    private void OnDodge()
    {
        IsDodge = true;
    }

    private void OffDodge()
    {
        IsDodge = false;
    }

    private void OnSound()
    {
        Audio.PlayOneShot(StealthKillSFX, 1f);
    }

    private void OnShake()
    {
        Shake.ShakeCamera(5f, 0.5f);
    }

    private void OnShake_Low()
    {
        Shake.ShakeCamera(2f, 0.5f);
    }

    private void OnAttackShake()
    {
        Shake.ShakeCamera(3f, 0.5f);
    }

    private void Step(AnimationEvent animEvent)
    {
        if (animEvent.animatorClipInfo.weight > 0.5f && PlayerState == E_PlayerState.WALK)
        {
            //Audio.PlayOneShot(WalkSFX[Random.Range(0, 3)], 0.1f);
        }
    }

    private void RunStep(AnimationEvent animEvent)
    {
        if (animEvent.animatorClipInfo.weight > 0.5f && PlayerState == E_PlayerState.RUN)
        {
            //Audio.PlayOneShot(RunSFX[Random.Range(0, 3)], 0.15f);
        }
    }

    private void LandSound()
    {
        Audio.PlayOneShot(LandSFX[Random.Range(0, 3)], 0.2f);
    }

    private void HighLandSound()
    {
        Audio.PlayOneShot(HighLandSFX, 1f);
    }

    public void SlowMotionEnter()
    {
        if (Random.Range(0, 100) <= 15)
        {
            Time.timeScale = 0.3f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    public void SlowMotionExit()
    {
        if (Time.timeScale != 1)
        {
           Time.timeScale = 1f;
           Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    private void OnSlow()
    {
        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void OffSlow()
    {
        if (Time.timeScale != 1)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    private void OnFireEffect()
    {
        Material[] mat = WeaponRenderer.GetComponent<MeshRenderer>().materials;
        mat[1] = FireMat;
        WeaponRenderer.GetComponent<MeshRenderer>().materials = mat;
        FireEffect.SetActive(true);
    }

    private void OnFireEffect2()
    {
        FireEffect2.SetActive(true);
    }

    private void OffFireEffect2()
    {
        FireEffect2.SetActive(false);
    }

    private void OnCut()
    {
        IsCut = true;
    }

    private void OffCut()
    {
        IsCut = false;
    }

    private void FogEffect()
    {
        GameObject fog = Instantiate(Fog, LeftHand.position, LeftHand.rotation);
    }

    private void ComboReset()
    {
        AttackCount = 0;
        StrongAttackCount = 0;
    }

    private void Climb()
    {
        
    }

    #endregion
}