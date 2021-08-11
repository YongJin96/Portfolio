using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HorseAI_Stat : MonoBehaviour
{
    private HorseAI_NPC HorseAI;

    private bool CheckCoroutine = true;

    public float MaxHealth;
    public float CurrentHealth;

    public bool Invincible = false;

    void Awake()
    {
        HorseAI = GetComponent<HorseAI_NPC>();

        SetHealth();
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.layer == LayerMask.NameToLayer("EnemyAttack"))
        {
            if (CheckCoroutine == true && Invincible == false)
            {
                CheckCoroutine = false;
                StartCoroutine(HitDelay(coll));
            }
        }

        if (coll.gameObject.layer == LayerMask.NameToLayer("Arrow"))
        {
            if (CheckCoroutine == true && Invincible == false)
            {
                CheckCoroutine = false;
                StartCoroutine(ArrowHitDelay(coll));
            }
        }
    }

    IEnumerator HitDelay(Collider coll)
    {
        TakeDamage(25);
        HorseAI.Hit();
        HorseAI.NPC.HorseMountHit();    

        if (CurrentHealth <= 0)
        {
            HorseAI.Die();
            HorseAI.NPC.FallingToHorse();
        }

        yield return new WaitForSeconds(0.2f);

        CheckCoroutine = true;
    }

    IEnumerator ArrowHitDelay(Collider coll)
    {
        HorseAI.Hit();
        HorseAI.NPC.HorseMountHit();

        if (CurrentHealth <= 0)
        {
            HorseAI.Die();
            HorseAI.NPC.FallingToHorse();
        }

        yield return new WaitForSeconds(0.2f);

        CheckCoroutine = true;
    }

    void SetHealth()
    {
        CurrentHealth = MaxHealth;
    }

    void TakeDamage(int _damage)
    {
        CurrentHealth -= _damage;
    }
}
