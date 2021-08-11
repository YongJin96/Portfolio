using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPCStat : MonoBehaviour
{
    private NPC_AI npcAi;
    private AudioSource Audio;
    private bool CheckCoroutine = true;
    private float DelayTime;

    public GameObject BloodEffect;
    public GameObject SparkEffect;
    public Transform SparkTransform;

    [Header("SFX")]
    public AudioClip[] HitSFX;
    public AudioClip[] BlockHitSFX;
    public AudioClip[] KickSFX;

    public float MaxHealth;
    public float CurrentHealth;
    public int Damage = 25;
    public bool IsStun = false;

    void Start()
    {
        npcAi = GetComponent<NPC_AI>();
        Audio = GetComponent<AudioSource>();

        CurrentHealth = MaxHealth;
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.layer == LayerMask.NameToLayer("EnemyAttack"))
        {
            if (CheckCoroutine == true)
            {
                CheckCoroutine = false;
                StartCoroutine(HitDelay(coll));
            }
        }

        // 화살 트리거
        if (coll.gameObject.CompareTag("Arrow"))
        {
            StartCoroutine(ArrowHitDelay(coll));
        }

        if (coll.gameObject.layer == LayerMask.NameToLayer("EnemyKick"))
        {
            npcAi.KnockDown();
            Audio.PlayOneShot(KickSFX[Random.Range(0, 2)], 1f);
        }
    }

    IEnumerator HitDelay(Collider coll)
    {
        if (EnemyAI_Mongol_NPC.IsPerilous == true) // 공격 불가 발동시 맞아도 HIT모션 안함
        {
            CurrentHealth -= 30;
            npcAi.PerilousHit();
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
        }

        if (npcAi.IsBlock == false && EnemyAI_Mongol_NPC.IsPerilous == false)
        {
            CurrentHealth -= 25;
            npcAi.KnockDown();
            npcAi.Hit();
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            IsStun = false;
        }
        else if (npcAi.IsBlock == true)
        {
            npcAi.BlockHit();
            Audio.PlayOneShot(BlockHitSFX[Random.Range(0, 8)], 1f);
            GameObject spark = Instantiate(SparkEffect, SparkTransform.position, SparkTransform.rotation);
            Destroy(spark, 1f);
        }

        if (CurrentHealth <= 0)
        {
            npcAi.Die();
        }

        yield return new WaitForSeconds(0.2f);

        CheckCoroutine = true;
    }

    IEnumerator ArrowHitDelay(Collider coll)
    {
        if (EnemyAI_Mongol_NPC.IsPerilous == true) // 공격 불가 발동시 맞아도 HIT모션 안함
        {
            //CurrentHealth -= 20;
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
        }

        if (npcAi.IsBlock == false && EnemyAI_Mongol_NPC.IsPerilous == false)
        {
            //CurrentHealth -= 20;
            //npcAi.KnockDown();
            npcAi.Hit();
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            IsStun = false;
        }

        if (CurrentHealth <= 0)
        {
            npcAi.Die();
        }

        yield return new WaitForSeconds(0.2f);
    }

    void ShowBloodEffect(Collision coll)
    {
        Vector3 pos = coll.contacts[0].point;
        Vector3 normal = coll.contacts[0].normal;
        Quaternion rot = Quaternion.FromToRotation(-Vector3.forward, normal);
    
        GameObject blood = Instantiate<GameObject>(BloodEffect, pos, rot);
        Destroy(blood, 2f);
    }

    void ShowBloodEffect(Collider coll)
    {
        GameObject blood = Instantiate<GameObject>(BloodEffect, new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z), transform.rotation);
        Destroy(blood, 2f);
    }
}
