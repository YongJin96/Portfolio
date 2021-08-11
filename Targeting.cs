using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cinemachine;

public class Targeting : MonoBehaviour
{
    #region Variables

    private Camera Cam;
    private PlayerMovement playerMovement;
    private Animator Anim;
    private CharacterController Controller;
    private Transform TargetPos;

    private float InputX;
    private float InputY;
    private float DelayTime;
    private int Count = 0;
    private bool NextDodge = false;

    public float CheckRadius;
    public LayerMask CheckLayer;

    public bool IsTargeting = false;

    public GameObject TargetingUI;

    #endregion

    #region Initialization

    private void Start()
    {
        Cam = Camera.main;
        playerMovement = GetComponent<PlayerMovement>();
        Anim = GetComponent<Animator>();
        Controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        InputTargeting();
        TargetDistance();
        StartCoroutine(DodgeTimer());

        if (IsTargeting == true)
        {
            OnTargeting();
            TargetingUI.SetActive(true); // 타겟팅 UI 오브젝트

            if (!Controller.isGrounded)
            {
                Controller.Move(-transform.up * 3f * Time.deltaTime);
            }
        }
        else if (IsTargeting == false)
        {
            TargetingUI.SetActive(false);
        }

        if (TargetPos == null)
        {
            IsTargeting = false;         
        }

        if (playerMovement.WeaponType == PlayerMovement.E_WeaponType.BOW)
        {
            IsTargeting = false;
        }
    }

    #endregion

    #region Targeting Function

    private IEnumerator DodgeTimer()
    {
        float elapsed = 0f;

        while (elapsed <= 0.5f && NextDodge == true)
        {
            elapsed += Time.deltaTime;

            yield return null;
        }

        NextDodge = false;
    }

    private void InputTargeting()
    {
        if(Input.GetKeyDown(KeyCode.Mouse2) && Count == 0 && TargetPos != null && playerMovement.WeaponType != PlayerMovement.E_WeaponType.BOW)
        {
            IsTargeting = true;
            Anim.SetBool("Targeting", true);
            Count++;
        }
        else if (Input.GetKeyDown(KeyCode.Mouse2) && Count == 1 || TargetPos == null)
        {
            IsTargeting = false;
            Anim.SetBool("Targeting", false);
            Count = 0;
        }
    }

    private void TargetDistance()
    {
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

        // Q버튼 누르면 타겟팅을 제일 가까운 적으로 변경
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TargetPos = nearestEnemy.transform;
        }

        // 타겟팅이 true고 타겟팅할 타겟이 있다면 return으로 TargetPos값 업데이트 하지않게하기 (계속 가까운적이 바뀔때마다 타겟팅도 바뀌어서 수동으로 타겟지정하기 위해)
        if (IsTargeting == true && TargetPos != null && shortestDistance <= CheckRadius)
        {
            if (!TargetPos.gameObject.CompareTag("Enemy")) // 타겟중인 적이 사망할때 태그를 Untagged로 변경해서 가까운적을 리타겟팅
            {
                TargetPos = nearestEnemy.transform;
            }
            else
            {
                return;
            }
        }

        if (nearestEnemy != null && shortestDistance <= CheckRadius)
        {
            TargetPos = nearestEnemy.transform;          
        }
        else
        {
            TargetPos = null;          
        }
    }

    private void OnTargeting()
    {
        if (TargetPos != null)
        {
            if (Physics.CheckSphere(transform.position, CheckRadius, CheckLayer))
            {
                Vector3 dir = TargetPos.transform.position - transform.position;
                transform.rotation = Quaternion.Slerp((Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0)), Quaternion.LookRotation(dir), Time.deltaTime * 5f);
                VisualTargetingUI();
            }
        }
    }

    private void VisualTargetingUI()
    {
        // 타겟팅 UI가 카메라 반대편으로도 보이는 걸 해결하기위해
        float target = Vector3.Dot((TargetPos.position - Cam.transform.position).normalized, Cam.transform.forward);

        if (target <= 0)
        {
            TargetingUI.SetActive(false);
        }
        else
        {
            TargetingUI.SetActive(true);
            // 타겟팅 UI 표시위치 설정
            TargetingUI.transform.position = Cam.WorldToScreenPoint(new Vector3(TargetPos.transform.position.x, TargetPos.transform.position.y + 1.1f, TargetPos.transform.position.z));
        }
    }

    public void InputMagnitude()
    {
        InputX = Input.GetAxis("Horizontal");
        InputY = Input.GetAxis("Vertical");

        // 기본값으로 초기화
        Anim.SetFloat("InputX", InputX, 0, Time.deltaTime);
        Anim.SetFloat("InputY", InputY, 0, Time.deltaTime);
        playerMovement.Speed = 0;
        //

        if (!Input.GetKey(KeyCode.LeftShift))
        {
            Anim.SetFloat("Direction", InputX / 2, 0.1f, Time.deltaTime);
            Anim.SetFloat("Speed", InputY * 1.1f, 0.1f, Time.deltaTime);
        }
        else if(Input.GetKey(KeyCode.LeftShift))
        {
            Anim.SetFloat("Direction", InputX, 0.1f, Time.deltaTime);
            Anim.SetFloat("Speed", InputY * 2.3f, 0.1f, Time.deltaTime);
        }

        Dodge();
    }

    private void Dodge()
    {
        // 타겟팅 회피
        if (Input.GetKey(KeyCode.W) && Input.GetKeyDown(KeyCode.Space) && NextDodge == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.3f;
                Anim.SetTrigger("ForwardDodge");
                NextDodge = true;
            }
        }
        if (Input.GetKey(KeyCode.D) && Input.GetKeyDown(KeyCode.Space) && NextDodge == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.3f;
                Anim.SetTrigger("RightDodge");
                NextDodge = true;
            }
        }
        if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Space) && NextDodge == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.3f;
                Anim.SetTrigger("LeftDodge");
                NextDodge = true;
            }
        }
        if (Input.GetKeyDown(KeyCode.Space) && NextDodge == false)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.3f;
                Anim.SetTrigger("BackDodge");
                NextDodge = true;
            }
        }

        // 타겟팅 구르기
        if (Input.GetKey(KeyCode.W) && Input.GetKeyDown(KeyCode.Space) && NextDodge == true)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.6f;
                Anim.SetTrigger("ForwardRoll");
                NextDodge = false;
            }
        }
        if (Input.GetKey(KeyCode.D) && Input.GetKeyDown(KeyCode.Space) && NextDodge == true)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.6f;
                Anim.SetTrigger("RightRoll");
                NextDodge = false;
            }
        }
        if (Input.GetKey(KeyCode.A) && Input.GetKeyDown(KeyCode.Space) && NextDodge == true)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.6f;
                Anim.SetTrigger("LeftRoll");
                NextDodge = false;
            }
        }
        if (Input.GetKeyDown(KeyCode.Space) && NextDodge == true)
        {
            if (DelayTime <= Time.time)
            {
                DelayTime = Time.time + 0.6f;
                Anim.SetTrigger("BackRoll");
                NextDodge = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, CheckRadius);
    }

    #endregion
}
