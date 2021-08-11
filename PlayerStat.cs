using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStat : MonoBehaviour
{
    #region Variables

    private PlayerMovement playerMovement;
    private Animator Anim;
    private Camera Cam;
    public CinemachineShake Shake;

    [Header("Player UI")]
    public Image BloodScreen;
    public Image DeathImage;
    public Image HealthBar;
    public RectTransform PostureRectTransform;
    public Image[] Potions;

    private bool CheckCoroutine = true;

    // 플레이어 체간 게이지 UI의 길이와 높이
    private const float POSTURE_BAR_WIDTH = 380f;
    private const float POSTURE_BAR_HEIGHT = 40f;

    private float StunDelay = 0f;
    private float SlowDelay = 0f;

    private int PotionCount = 0;
    private const int PotionMaxCount = 5;

    [Header("Effect Setting")]
    public GameObject SparkEffect;
    public Transform SparkTransform;
    public GameObject ParryingEffect;

    public float MaxHealth = 100f;
    public float CurrentHealth;
    public int Damage = 20;

    public bool IsStun = false;
    static public bool ParryingSuccess = false;

    #endregion

    #region Initialization

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        Anim = GetComponent<Animator>();
        Cam = Camera.main;
        CurrentHealth = MaxHealth;
        SetHealth(1);
        SetPosture(0);
    }

    private void Update()
    {
        StartCoroutine(ResetSlowMotion());
        StartCoroutine(StunTimer());
        Invoke("PostureDecrease", 1f);
        Stun();

        UsePotion(); // 포션 사용 키        

        LookAtCamera();
    }

    #endregion

    #region Functions

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.layer == LayerMask.NameToLayer("EnemyAttack"))
        {         
            if (CheckCoroutine == true)
            {
                CheckCoroutine = false;
                StartCoroutine(HitCheck(coll));
            }
        }

        if (coll.gameObject.layer == LayerMask.NameToLayer("BossAttack"))
        {
            if (CheckCoroutine == true)
            {
                CheckCoroutine = false;
                StartCoroutine(HitCheck_Boss(coll));
            }
        }

        if (coll.gameObject.layer == LayerMask.NameToLayer("WerewolfAttack"))
        {
            if (CheckCoroutine == true)
            {
                CheckCoroutine = false;
                StartCoroutine(HitCheck_Werewolf(coll));
            }
        }

        if (coll.gameObject.layer == LayerMask.NameToLayer("EnemyKick"))
        {
            playerMovement.Stun();
        }

        if (coll.gameObject.layer == LayerMask.NameToLayer("EnemyArrow"))
        {
            StartCoroutine(ArrowHitCheck(coll));
        }

        if (coll.gameObject.layer == LayerMask.NameToLayer("Bullet"))
        {
            StartCoroutine(BulletHitCheck(coll));
        }
    }

    private IEnumerator HitCheck(Collider coll)
    {
        // 막기 불가 공격
        if (EnemyAI_Mongol.IsPerilous == true && playerMovement.IsDodge == false)
        {
            TakeDamage(40);
            PostureIncrease(80);
            playerMovement.PerilousHit();
            Shake.ShakeCamera(3f, 0.5f);
            StartCoroutine(ShowBloodScreen());

            IsStun = false; // 맞으면 스턴상태 해제
        }

        // 패링
        if (playerMovement.IsParrying == true && EnemyAI_Mongol.IsPerilous == false)
        {
            ParryingSuccess = true;           // 패링 성공여부 체크
            Parrying();
            coll.GetComponentInParent<EnemyAI_Mongol>().ParryingToStun();
            coll.GetComponentInParent<EnemyAI_Mongol>().IsHit = true;
            playerMovement.SlowMotionEnter(); // 슬로우모션
            playerMovement.Audio.PlayOneShot(playerMovement.BlockHitSFX[Random.Range(0, 8)], 1f);
            Shake.ShakeCamera(3f, 0.5f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.transform.position, SparkTransform.transform.rotation);
            Destroy(spark, 1f);
        }

        if (playerMovement.IsBlock == false && playerMovement.IsDodge == false && playerMovement.IsParrying == false)
        {
            //TakeDamage(coll.GetComponentInParent<EnemyStat_Samurai>().Damage); // coll 게임오브젝트에 부모 오브젝트에 있는 스크립트 정보를 가져옴
            TakeDamage(25);
            PostureIncrease(50);
            playerMovement.Hit();
            Shake.ShakeCamera(3f, 0.3f);
            StartCoroutine(ShowBloodScreen());

            IsStun = false; // 맞으면 스턴상태 해제
        }
        else if (playerMovement.IsBlock == true && playerMovement.IsParrying == false && EnemyAI_Mongol.IsPerilous == false)
        { // 방어
            playerMovement.BlockHit();
            PostureIncrease(40);
            Shake.ShakeCamera(3f, 0.5f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.transform.position, SparkTransform.transform.rotation);
            Destroy(spark, 1f);
        }

        if (CurrentHealth <= 0)
        {
            playerMovement.Die();
            DeathImage.enabled = true;
        }

        yield return new WaitForSeconds(0.2f);

        CheckCoroutine = true;
        playerMovement.IsDodge = false;
        ParryingSuccess = false;
    }

    private IEnumerator HitCheck_Boss(Collider coll)
    {
        // 막기 불가 공격
        if (BossAI.IsPerilous == true && playerMovement.IsDodge == false)
        {
            TakeDamage(100);
            PostureIncrease(200);
            playerMovement.PerilousHit();
            Shake.ShakeCamera(3f, 0.5f);
            StartCoroutine(ShowBloodScreen());

            IsStun = false; // 맞으면 스턴상태 해제
        }

        // 패링
        if (playerMovement.IsParrying == true && BossAI.IsPerilous == false)
        {
            Parrying();
            coll.GetComponentInParent<BossAI>().ParryingToStun();
            coll.GetComponentInParent<BossAI>().IsHit = true;
            coll.GetComponentInParent<BossStat>().PostureIncrease(50);
            playerMovement.SlowMotionEnter(); // 슬로우모션
            playerMovement.Audio.PlayOneShot(playerMovement.BlockHitSFX[Random.Range(0, 8)], 1f);
            Shake.ShakeCamera(3f, 0.5f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.transform.position, SparkTransform.transform.rotation);
            Destroy(spark, 1f);
        }

        if (playerMovement.IsBlock == false && playerMovement.IsDodge == false && playerMovement.IsParrying == false && BossAI.IsPerilous == false)
        {
            TakeDamage(coll.GetComponentInParent<BossStat>().Damage); // coll 게임오브젝트에 부모 오브젝트에 있는 스크립트 정보를 가져옴
            PostureIncrease(60);
            playerMovement.Hit();
            Shake.ShakeCamera(3f, 0.3f);
            StartCoroutine(ShowBloodScreen());

            IsStun = false; // 맞으면 스턴상태 해제
        }
        else if (playerMovement.IsBlock == true && playerMovement.IsParrying == false && BossAI.IsPerilous == false)
        { // 방어
            playerMovement.BlockHit();
            PostureIncrease(80);
            Shake.ShakeCamera(3f, 0.5f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.transform.position, SparkTransform.transform.rotation);
            Destroy(spark, 1f);
        }

        if (CurrentHealth <= 0)
        {
            playerMovement.Die();
            DeathImage.enabled = true;
        }

        yield return new WaitForSeconds(0.2f);

        CheckCoroutine = true;
        playerMovement.IsDodge = false;
    }

    private IEnumerator HitCheck_Werewolf(Collider coll)
    {
        // 막기 불가 공격
        if (EnemyAI_Mongol.IsPerilous == true && playerMovement.IsDodge == false)
        {
            TakeDamage(50);
            PostureIncrease(100);
            playerMovement.PerilousHit();
            Shake.ShakeCamera(3f, 0.5f);
            StartCoroutine(ShowBloodScreen());

            IsStun = false; // 맞으면 스턴상태 해제
        }

        // 패링
        if (playerMovement.IsParrying == true && EnemyAI_Mongol.IsPerilous == false)
        {
            coll.GetComponentInParent<WereWolf>().ParryingToStun();
            playerMovement.SlowMotionEnter(); // 슬로우모션
            playerMovement.Audio.PlayOneShot(playerMovement.BlockHitSFX[Random.Range(0, 8)], 1f);
            Shake.ShakeCamera(3f, 0.3f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.transform.position, SparkTransform.transform.rotation);
            Destroy(spark, 1f);
        }

        if (playerMovement.IsBlock == false && playerMovement.IsDodge == false)
        {
            TakeDamage(coll.GetComponentInParent<WereWolfStat>().Damage); // coll 게임오브젝트에 부모 오브젝트에 있는 스크립트 정보를 가져옴
            PostureIncrease(50);
            playerMovement.Hit();
            Shake.ShakeCamera(3f, 0.5f);
            StartCoroutine(ShowBloodScreen());

            IsStun = false; // 맞으면 스턴상태 해제
        }
        else if (playerMovement.IsBlock == true && EnemyAI_Mongol.IsPerilous == false)
        { // 방어
            playerMovement.BlockHit();
            PostureIncrease(60);
            Shake.ShakeCamera(3f, 0.5f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.transform.position, SparkTransform.transform.rotation);
            Destroy(spark, 1f);
        }

        if (CurrentHealth <= 0)
        {
            playerMovement.Die();
            DeathImage.enabled = true;
        }

        yield return new WaitForSeconds(0.2f);

        CheckCoroutine = true;
        playerMovement.IsDodge = false;
    }

    private IEnumerator ArrowHitCheck(Collider coll)
    {
        if (playerMovement.IsBlock == false && playerMovement.IsDodge == false)
        {
            TakeDamage(coll.GetComponent<EnemyArrow>().Damage);
            PostureIncrease(50);
            playerMovement.Hit();
            Shake.ShakeCamera(3f, 0.3f);
            StartCoroutine(ShowBloodScreen());

            IsStun = false; // 맞으면 스턴상태 해제
        }
        else if (playerMovement.IsBlock == true)
        { // 방어
            playerMovement.BlockHit();
            PostureIncrease(40);
            Shake.ShakeCamera(3f, 0.5f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.transform.position, SparkTransform.transform.rotation);
            Destroy(spark, 1f);
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator BulletHitCheck(Collider coll)
    {
        if (playerMovement.IsBlock == false && playerMovement.IsDodge == false)
        {
            TakeDamage(coll.GetComponent<EnemyBullet>().Damage);
            PostureIncrease(50);
            playerMovement.Hit();
            Shake.ShakeCamera(3f, 0.3f);
            StartCoroutine(ShowBloodScreen());

            IsStun = false; // 맞으면 스턴상태 해제
        }
        else if (playerMovement.IsBlock == true)
        { // 방어
            playerMovement.BlockHit();
            PostureIncrease(40);
            Shake.ShakeCamera(3f, 0.5f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.transform.position, SparkTransform.transform.rotation);
            Destroy(spark, 1f);
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator ShowBloodScreen()
    {
        BloodScreen.color = new Color(1, 0, 0, UnityEngine.Random.Range(0.3f, 0.5f));
        yield return new WaitForSeconds(0.2f);
        BloodScreen.color = Color.clear;
    }

    private IEnumerator ResetSlowMotion()
    {
        SlowDelay = 0f;

        while (SlowDelay <= 1f && Time.timeScale != 1)
        {
            SlowDelay += Time.deltaTime;

            yield return null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private IEnumerator StunTimer()
    {
        StunDelay = 0f;

        while (StunDelay <= 1f && IsStun == true)
        {
            StunDelay += Time.deltaTime;

            yield return null;
        }

        IsStun = false;
    }

    private void SetHealth(float _healthNormalized)
    {
        HealthBar.fillAmount = _healthNormalized;
    }

    private void SetPosture(float _postureNormalized)
    {
        PostureRectTransform.sizeDelta = new Vector2(_postureNormalized * POSTURE_BAR_WIDTH, PostureRectTransform.sizeDelta.y);
    }

    private void TakeDamage(int _damage)
    {
        CurrentHealth -= _damage;
        HealthBar.fillAmount = CurrentHealth / MaxHealth;
    }

    private void PostureIncrease(int _amount)
    {
        if (PostureRectTransform.sizeDelta.x <= POSTURE_BAR_WIDTH)
        {
            PostureRectTransform.sizeDelta += new Vector2(_amount, 0f); // 체간게이지 증가
        }
    }

    private void PostureDecrease()
    {
        if (PostureRectTransform.sizeDelta.x > 0)
        {
            PostureRectTransform.sizeDelta -= new Vector2(0.1f, 0); // 체간 게이지 감소
        }
    }

    private void Parrying()
    {
        int rand = Random.Range(0, 5);

        GameObject parryingEffect = Instantiate(ParryingEffect, SparkTransform.position, Quaternion.LookRotation(Cam.transform.forward));
        Destroy(parryingEffect, 0.3f);

        if (rand == 0)
        {
            Anim.SetTrigger("Parry_1");
        }
        else if (rand == 1)
        {
            Anim.SetTrigger("Parry_2");
        }
        else if (rand == 2)
        {
            Anim.SetTrigger("Parry_3");
        }
        else if (rand == 3)
        {
            Anim.SetTrigger("Parry_4");
        }
        else if (rand == 4)
        {
            Anim.SetTrigger("Parry_5");
        }
    }

    private void Stun()
    {
        if (PostureRectTransform.sizeDelta.x >= POSTURE_BAR_WIDTH)
        {
            PostureRectTransform.sizeDelta = new Vector2(0f, POSTURE_BAR_HEIGHT);
            playerMovement.Stun();
            IsStun = true;
        }
    }

    public void SetPotionIncrease(float _increase)
    {
        if (PotionCount == PotionMaxCount) { return; }

        Potions[PotionCount].fillAmount += _increase;

        if (Potions[PotionCount].fillAmount >= 1f)
        {
            ++PotionCount;
        }
    }

    private void UsePotion()
    {
        if (HealthBar.fillAmount >= 1f || CurrentHealth >= MaxHealth) { return; }

        if (Input.GetKeyDown(KeyCode.X) && PotionCount > 0 && playerMovement.IsDie == false)
        {
            PotionCount--;
            Potions[PotionCount].fillAmount = 0f;
            CurrentHealth += 50f;
            HealthBar.fillAmount = CurrentHealth / MaxHealth;
        }

        // 체력회복시 MaxHealth값보다 오버되지않게 초기화
        if (CurrentHealth > MaxHealth) { CurrentHealth = MaxHealth; }
    }

    private void LookAtCamera()
    {
        // PerilousSymbol rotation값을 카메라가 보는 방향으로 
        
    }

    #endregion
}
