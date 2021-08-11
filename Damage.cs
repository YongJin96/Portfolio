using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damage : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerStat playerStat;
    private Animator Anim;

    public int SkillDamage = 50;

    void Start()
    {
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        playerStat = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStat>();
        Anim = GameObject.FindGameObjectWithTag("Player").GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player") && playerMovement.IsDodge == false)
        {
            playerStat.CurrentHealth -= SkillDamage;
            Anim.SetTrigger("SkillHit");
        }
    }
}
