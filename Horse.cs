using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class Horse : MonoBehaviour
{
    #region Horse Variables

    public enum E_HorseState
    {
        IDLE,
        WALK,
        RUN,
        JUMP
    }

    private Animator Anim;
    private CharacterController Controller;
    private PlayerMovement playerMovement;
    private Camera Cam;
    private AudioSource Audio;
    private InteractionUI HorseUI;
    private CinemachineVirtualCamera VPlayer;
    private CinemachineVirtualCamera VHorse;
    private CinemachineVirtualCamera VHorseRun;
    public CinemachineShake HorseShake;
    public CinemachineShake HorseRunShake;

    private float InputX;
    private float InputY;
    public float Speed;

    private float MoveSpeed;
    private float DelayTime;
    private float SoundDelayTime;

    private Vector3 DesiredMoveDirection;

    [Header("Horse Prefabs")]
    public Transform MountPos;
    public Transform DisMountPos;
    public SphereCollider FrontLeftLeg;
    public SphereCollider FrontRightLeg;
    public SphereCollider BackLeftLeg;
    public SphereCollider BackRightLeg;

    [Header("Horse SFX")]
    public AudioClip[] HorseBreating;
    public AudioClip[] StepSFX;
    public AudioClip[] GallopSFX;
    public AudioClip[] SquealSFX;
    public AudioClip CallSFX;
    public AudioClip HitSFX;
    public AudioClip DieSFX;

    [Header("Horse State")]
    public E_HorseState HorseState;
    public bool IsMount = false;
    public bool IsGrounded = false;
    public bool IsJump = false;
    public bool IsFalling = false;
    public bool IsDie = false;
    static public bool IsMountCheck = false;
    public bool IsAutoRun = false;

    [Header("Horse Setting")]
    public float DesiredRotationSpeed = 0.2f;
    public float AllowHorseRotation = 0f;
    public float WalkSpeed = 0.5f;
    public float RunSpeed = 1f;
    public float JumpForce = 3f;
    public float Gravity = 3f;
    public float JumpGravity = 3f;
    public float SlopeForceRayLength;
    public float SlopeForce;

    #endregion

    #region Initialization

    private void Awake()
    {
        Anim = GetComponent<Animator>();
        Controller = GetComponent<CharacterController>();
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        Cam = Camera.main;
        Audio = GetComponent<AudioSource>();
        HorseUI = GetComponent<InteractionUI>();
        VPlayer = GameObject.Find("CM VPlayer").GetComponent<CinemachineVirtualCamera>();
        VHorse = GameObject.Find("CM VHorse").GetComponent<CinemachineVirtualCamera>();
        VHorseRun = GameObject.Find("CM VHorse_Run").GetComponent<CinemachineVirtualCamera>();
    }

    private void LateUpdate()
    {
        StartCoroutine(CheckState());
        StartCoroutine(Action());

        if (Controller.isGrounded)
        {
            IsGrounded = true;
            Anim.SetBool("IsGrounded", true);                  
            IsJump = false;
            Anim.SetBool("IsJump", false);
        }

        if (IsGrounded == true)
        {
            SlopeAngle();
        }

        if (IsMount == true)
        {
            VHorse.Priority = 10;
            playerMovement.OffController();

            // 플레이어 위치
            playerMovement.transform.position = MountPos.transform.position;
            playerMovement.transform.rotation = MountPos.transform.rotation;

            Falling();
            FallFromEdge();

            if (IsGrounded)
            {
                HorseInputMagnitude();
                Jump();
            }
            else if (!IsGrounded)
            {
                Controller.Move(Vector3.down * Gravity * Time.deltaTime);
            }

            playerMovement.HorseState();
            DisMount();          
        }
        else if (IsMount == false && Stealth_KillBehaviour.Kill == false)
        {
            VHorse.Priority = 9;           

            Controller.Move(Vector3.down * Gravity * Time.deltaTime);

            Anim.SetFloat("InputX", 0f, 0.1f, Time.deltaTime);
            Anim.SetFloat("InputY", 0f, 0.1f, Time.deltaTime);

            HorseCall();
        }      

        AutoRun();
    }

    #endregion

    #region Horse Function

    private IEnumerator CheckState()
    {
        if (IsGrounded)
        {
            if (InputX == 0 && InputY == 0)
            {
                HorseState = E_HorseState.IDLE;               
            }
            else if (MoveSpeed == WalkSpeed)
            {
                HorseState = E_HorseState.WALK;
            }
            else if (MoveSpeed == RunSpeed)
            {
                HorseState = E_HorseState.RUN;
            }
        }
        else
        {
            HorseState = E_HorseState.JUMP;
        }

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator Action()
    {
        switch (HorseState)
        {
            case E_HorseState.IDLE:
                break;
            case E_HorseState.WALK:
                VHorseRun.Priority = 9;
                break;
            case E_HorseState.RUN:
                VHorseRun.Priority = 10;
                Anim.SetFloat("InputX", InputX * 2.2f, 0.1f, Time.deltaTime);
                Anim.SetFloat("InputY", InputY * 2.2f, 0.1f, Time.deltaTime);
                break;
            case E_HorseState.JUMP:
                Anim.SetBool("IsJump", true);
                Anim.SetBool("IsGrounded", false);
                break;
        }

        yield return null;
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 2f;
                IsGrounded = false;
                IsJump = true;
                StartCoroutine(JumpCoroutine());
                Anim.SetTrigger("Jump");
                Audio.PlayOneShot(SquealSFX[Random.Range(0, 3)], 1f);
            }
        }
    }

    private IEnumerator JumpCoroutine()
    {
        float jump = JumpForce;
        do
        {
            Controller.Move(Vector3.up * jump * Time.deltaTime);
            if (Speed >= 0.5f)
            {
                Controller.Move(transform.forward * 2f * Time.deltaTime);
            }
            jump -= Time.deltaTime * JumpGravity;

            yield return null;
        }
        while (!IsGrounded);
    }

    private void HorseMoveAndRotation()
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

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(DesiredMoveDirection), DesiredRotationSpeed);
    }

    private void HorseInputMagnitude()
    {
        InputX = Input.GetAxis("Horizontal");
        InputY = Input.GetAxis("Vertical");

        Anim.SetFloat("InputX", InputX, 0.1f, Time.deltaTime);
        Anim.SetFloat("InputY", InputY, 0.1f, Time.deltaTime);

        Speed = new Vector2(InputX, InputY).normalized.sqrMagnitude;

        MoveSpeed = Input.GetKey(KeyCode.LeftShift) ? RunSpeed : WalkSpeed;

        Controller.SimpleMove(DesiredMoveDirection * MoveSpeed);

        if (Speed > AllowHorseRotation)
        {
            //HorseMoveAndRotation();
        }
        else if (Speed < AllowHorseRotation)
        {

        }

        if (OnSlope() && IsJump == false)
        {
            Controller.Move(Vector3.down * SlopeForce * Time.deltaTime);
        }
    }

    private bool OnSlope()
    {
        if (IsJump)
        {
            return false;
        }

        RaycastHit hit;

        if (Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0f, 0.2f, 0.57f)), Vector3.down, out hit, Controller.height / 2 * SlopeForceRayLength))
        {
            if (hit.normal != Vector3.up)
            {
                return true;
            }
        }

        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0f, 0.2f, 0.57f)), Vector3.down * (Controller.height / 2 * SlopeForceRayLength), Color.red);

        return false;
    }

    private void Falling()
    {
        if (IsGrounded == true) { return; }

        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("Map");

        if (Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0f, 0f, 0.57f)), Vector3.down, out hit, layerMask))
        {
            if (hit.distance >= 3f)
            {
                Anim.SetBool("IsFalling", true);
                IsFalling = true;
            }
            else if (hit.distance < 3f)
            {
                Anim.SetBool("IsFalling", false);
                IsFalling = false;
            }
        }

        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0f, 0f, 0.57f)), Vector3.down, Color.blue);
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.layer == LayerMask.NameToLayer("Map") || coll.gameObject.layer == LayerMask.NameToLayer("Obstacle") || coll.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (IsJump == false)
            {
                IsGrounded = true;
                Anim.SetBool("IsGrounded", true);
            }

            if (SoundDelayTime <= Time.time && HorseState == E_HorseState.WALK)
            {           
                SoundDelayTime = Time.time + 0.2f;
                Audio.PlayOneShot(StepSFX[Random.Range(0, 4)], 1);

            }
            else if (SoundDelayTime <= Time.time && HorseState == E_HorseState.RUN)
            {
                SoundDelayTime = Time.time + 0.5f;
                Audio.PlayOneShot(GallopSFX[Random.Range(0, 3)], 1f);
            }

        }

        if (coll.gameObject.layer == LayerMask.NameToLayer("Blade") && IsMount == false && IsDie == false || coll.gameObject.layer == LayerMask.NameToLayer("Arrow") && IsMount == false && IsDie == false)
        {
            Hit();
        }

        // 말 UI
        if (coll.gameObject.CompareTag("Player") && IsMount == false)
        {
            HorseUI.ViewUI(true);
        }
    }

    private void OnTriggerStay(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player"))
        {
            Mount();
        }
    }

    private void OnTriggerExit(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player") || IsMount == true)
        {
            HorseUI.ViewUI(false);
        }
    }

    private void Mount()
    {
        IsMountCheck = true;

        if (Input.GetKeyDown(KeyCode.E) && IsMount == false && IsDie == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                playerMovement.HorseMount();
                playerMovement.OffController();
                IsMount = true;
                VHorse.enabled = true;
                VHorseRun.enabled = true;
            }
        }
    }

    private void DisMount()
    {
        if (Input.GetKeyDown(KeyCode.E) && IsMount == true)
        {
            IsMountCheck = false;

            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 1f;
                playerMovement.HorseMount();
                IsMount = false;
                // Cinemachine Off
                VHorse.enabled = false;
                VHorseRun.enabled = false;
            }
        }
    }

    private void HorseCall()
    {
        if (Input.GetKeyDown(KeyCode.H) && IsDie == false)
        {
            playerMovement.HorseCall();
            Controller.enabled = false;
            Audio.PlayOneShot(CallSFX, 1f);
            transform.position = new Vector3(Cam.transform.position.x, playerMovement.transform.position.y, Cam.transform.position.z);
            transform.rotation = playerMovement.transform.rotation;
            Controller.enabled = true;
        }
    }

    private void Hit()
    {
        float rand = Random.Range(0, 2);
        Audio.PlayOneShot(HitSFX, 1f);

        if (rand == 0)
        {
            Anim.SetTrigger("Hit_0");
        }
        else if (rand == 1)
        {
            Anim.SetTrigger("Hit_1");
        }
    }

    public void Die()
    {
        IsDie = true;
        IsMount = false;
        playerMovement.IsMount = false;
        IsMountCheck = false;
        VHorse.enabled = false;
        VHorseRun.enabled = false;
        Speed = 0f;
        Anim.SetTrigger("Die");
        Audio.PlayOneShot(DieSFX, 1f);
    }

    private void SlopeAngle() // 터레인 경사면 각도를 말에 적용
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0f, 0.5f, 0.57f)), Vector3.down, out hit, 1 << LayerMask.NameToLayer("Map")))
        {
            Quaternion normalRot = Quaternion.FromToRotation(transform.up, hit.normal);
            transform.rotation = Quaternion.Lerp(transform.rotation, normalRot * transform.rotation, Time.deltaTime * 3f);
        }

        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0f, 0.5f, 0.57f)), Vector3.down, Color.green);
    }

    private void AutoRun() // StartScene 시작 연출용
    {
        if (IsAutoRun == true)
        {
            HorseState = E_HorseState.RUN;
            VHorseRun.Priority = 11;

            Speed = 3;
            Anim.SetFloat("InputY", Speed, 0.1f, Time.deltaTime * 2);
        }
    }

    private void FallFromEdge()
    {
        if (IsJump == true && IsGrounded == false) { return; }

        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("Map");

        if (Physics.Raycast(transform.position + transform.TransformDirection(new Vector3(0f, 0.5f, 2f)), Vector3.down, out hit, layerMask))
        {
            if (hit.distance >= 2f && IsFalling == false)
            {
                Anim.SetTrigger("FallFromEdge");
                IsFalling = true;
                IsGrounded = false;
                Anim.SetBool("IsGrounded", false);
            }
            else if (hit.distance < 2f)
            {
                IsFalling = false;
            }
        }

        Debug.DrawRay(transform.position + transform.TransformDirection(new Vector3(0f, 0.5f, 2f)), Vector3.down, Color.green);
    }

    #endregion

    #region Animation Function

    private void LandShake()
    {
        HorseShake.ShakeCamera(5f, 0.3f);
        HorseRunShake.ShakeCamera(5f, 0.3f);
    }

    #endregion

    #region Cinemachine Transitions

    public void OnShake()
    {
        HorseShake.ShakeCamera(5f, 0.3f);
        HorseRunShake.ShakeCamera(5f, 0.3f);
    }

    public void SquealSound()
    {
        Audio.PlayOneShot(SquealSFX[Random.Range(0, 3)], 1f);
    }

    #endregion
}
