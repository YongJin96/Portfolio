using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStat_Mongol : MonoBehaviour
{
    #region Variables

    private EnemyAI_Mongol enemyAi;
    private AudioSource Audio;
    private PlayerStat playerStat;

    [Header("Effect")]
    public GameObject BloodEffect;
    public GameObject BloodEffect2;
    public Transform BloodTransform; // StealthKill할때 피 위치
    public GameObject SparkEffect;
    public Transform SparkTransform;
    
    [Header("EnemySFX")]
    public AudioClip[] HitSFX;
    public AudioClip[] BlockHitSFX;
    public AudioClip[] KickSFX;
    public AudioClip KillSFX;
    public AudioClip StabSFX;

    [Header("EnemyUI")]
    public GameObject HealthBarUI;
    public Slider slider;

    private bool CheckCoroutine = true;

    public float MaxHealth;
    public float CurrentHealth;
    public int Damage = 25;

    #endregion

    #region Initialization

    private void Awake()
    {
        enemyAi = GetComponent<EnemyAI_Mongol>();
        playerStat = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStat>();
        Audio = GetComponent<AudioSource>();

        CurrentHealth = MaxHealth;
        slider.value = 1f;
    }

    #endregion

    #region Functions

    private void OnCollisionEnter(Collision coll)
    {
        // 플레이어 공격 충돌체크
        if (coll.gameObject.layer == LayerMask.NameToLayer("Blade"))
        {
            if (CheckCoroutine == true)
            {
                CheckCoroutine = false;
                StartCoroutine(HitDelay(coll));
            }
        }

        // NPC 공격 충돌체크
        if (coll.gameObject.layer == LayerMask.NameToLayer("NPCAttack"))
        {
            StartCoroutine(HitDelay_NPC(coll));
        }

        // 화살 트리거
        if (coll.gameObject.CompareTag("Arrow"))
        {
            StartCoroutine(ArrowHitDelay(coll));
        }
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.CompareTag("Kick"))
        {
            enemyAi.Knockdown();
            Audio.PlayOneShot(KickSFX[Random.Range(0, 2)], 1f);
            playerStat.Shake.ShakeCamera(5f, 0.2f);        
        }      
    }

    private IEnumerator HitDelay(Collision coll)
    {
        // Perilous Attack 발동시 맞아도 HIT모션 안함
        if (EnemyAI_Mongol.IsPerilous == true && PlayerMovement.IsPerilousAttack == false && PlayerMovement.IsFireWeapon == false)
        {
            CurrentHealth -= playerStat.Damage;
            slider.value = CurrentHealth / MaxHealth;
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            playerStat.Shake.ShakeCamera(4f, 0.1f);

            // 의지(체력회복) 획득량
            playerStat.SetPotionIncrease(Random.Range(0.05f, 0.08f));
        }

        // 피니쉬
        if (PlayerMovement.IsCut == true)
        {
            Audio.PlayOneShot(KillSFX, 1f);
            enemyAi.ParryingToDie();

            // 의지(체력회복) 획득량
            playerStat.SetPotionIncrease(Random.Range(0.5f, 0.8f));
        }

        // 기본 상태
        if (enemyAi.IsBlock == false && EnemyAI_Mongol.IsPerilous == false && PlayerMovement.IsPerilousAttack == false && PlayerMovement.IsFireWeapon == false)
        {
            CurrentHealth -= playerStat.Damage;
            slider.value = CurrentHealth / MaxHealth;
            enemyAi.Hit();
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            playerStat.Shake.ShakeCamera(4f, 0.1f);

            // 의지(체력회복) 획득량
            playerStat.SetPotionIncrease(Random.Range(0.05f, 0.08f));
        }
        else if (enemyAi.IsBlock == true && PlayerMovement.IsPerilousAttack == false && PlayerMovement.IsFireWeapon == false) // 막기 상태
        {
            enemyAi.BlockHit();
            Audio.PlayOneShot(BlockHitSFX[Random.Range(0, 8)], 1f);
            playerStat.Shake.ShakeCamera(4f, 0.2f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.position, SparkTransform.rotation);
            Destroy(spark, 1f);
        }
        else if (PlayerMovement.IsFireWeapon == true) // 플레이어 무기에 붙 붙은경우 가드불가
        {
            CurrentHealth -= playerStat.Damage;
            slider.value = CurrentHealth / MaxHealth;
            enemyAi.Hit();
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            playerStat.Shake.ShakeCamera(4f, 0.1f);

            // 의지(체력회복) 획득량
            playerStat.SetPotionIncrease(Random.Range(0.07f, 0.1f));
        }
        else if (PlayerMovement.IsPerilousAttack == true) // 플레이어 PerilousAttack
        {
            CurrentHealth -= 100f;
            slider.value = CurrentHealth / MaxHealth;
            enemyAi.Hit();
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            playerStat.Shake.ShakeCamera(4f, 0.1f);

            // 의지(체력회복) 획득량
            playerStat.SetPotionIncrease(Random.Range(0.5f, 0.6f));
        }

        Die();

        yield return new WaitForSeconds(0.2f);

        CheckCoroutine = true;
    }

    private IEnumerator ArrowHitDelay(Collision coll)
    {
        if (EnemyAI_Mongol.IsPerilous == true) // 공격 불가 발동시 맞아도 HIT모션 안함
        {
            CurrentHealth -= PlayerArrow.Damage;
            slider.value = CurrentHealth / MaxHealth;
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
        }

        if (enemyAi.IsBlock == false && EnemyAI_Mongol.IsPerilous == false)
        {
            CurrentHealth -= PlayerArrow.Damage;
            slider.value = CurrentHealth / MaxHealth;
            enemyAi.Hit();
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
        }

        Die();

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator HitDelay_NPC(Collision coll)
    {
        if (enemyAi.IsBlock == false)
        {
            CurrentHealth -= coll.gameObject.GetComponentInParent<NPC_AI>().Damage;
            slider.value = CurrentHealth / MaxHealth;
            enemyAi.Hit();
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
        }
        else if (enemyAi.IsBlock == true)
        {
            enemyAi.BlockHit();
            Audio.PlayOneShot(BlockHitSFX[Random.Range(0, 9)], 1f);

            GameObject spark = Instantiate(SparkEffect, SparkTransform.position, SparkTransform.rotation);
            Destroy(spark, 1f);
        }

        Die();

        yield return new WaitForSeconds(0.2f);
    }

    private void ShowBloodEffect(Collision coll)
    {
        Vector3 pos = coll.contacts[0].point;
        Vector3 normal = coll.contacts[0].normal;
        Quaternion rot = Quaternion.FromToRotation(-Vector3.forward, normal);

        GameObject blood = Instantiate<GameObject>(BloodEffect, pos, rot);
        Destroy(blood, 2f);
    }

    private void Die()
    {
        if (CurrentHealth <= 0 && enemyAi.IsDie == false)
        {
            enemyAi.Die();
            Destroy(HealthBarUI);
        }
    }

    #endregion

    #region Animation Func

    private void OnBlood()
    {
        Instantiate(BloodEffect2, BloodTransform.position, BloodTransform.rotation);
    }

    private void KillSound()
    {
        Audio.PlayOneShot(KillSFX, 1f);
    }

    private void StabSound()
    {
        Audio.PlayOneShot(StabSFX, 1f);
    }

    #endregion
}
