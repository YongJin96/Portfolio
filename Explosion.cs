using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    private AudioSource Audio;

    public GameObject ExplosionPrefabs;
    public CinemachineShake Shake;
    public AudioClip ExplosionSFX;

    public float ExplodeRange = 10f;

    private void Start()
    {
        Audio = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Arrow"))
        {
            Explode();
        }
    }

    public void Explode()
    {
        GameObject explosion = Instantiate(ExplosionPrefabs, transform.position, transform.rotation);
        Shake.ShakeCamera(5f, 1f);
        Audio.PlayOneShot(ExplosionSFX, 1f);
        ExplodeDamage_Enemy();
        ExplodeDamage_Enemy_NPC();
        ExplodeDamage_NPC();
        ExplodeDamage_Horse_NPC();
        Destroy(explosion, 2f);
        this.enabled = false;
    }
    
    void ExplodeDamage_Enemy()
    {
        Collider[] colls = Physics.OverlapSphere(transform.position, ExplodeRange, 1 << LayerMask.NameToLayer("Enemy"));
    
        foreach (var coll in colls)
        {
            coll.GetComponent<EnemyAI_Mongol>().ExplodeDie();
        }
    }

    void ExplodeDamage_Enemy_NPC()
    {
        Collider[] colls = Physics.OverlapSphere(transform.position, ExplodeRange, 1 << LayerMask.NameToLayer("Enemy"));

        foreach (var coll in colls)
        {
            coll.GetComponent<EnemyAI_Mongol_NPC>().ExplodeDie();
        }
    }

    void ExplodeDamage_NPC()
    {
        Collider[] colls = Physics.OverlapSphere(transform.position, ExplodeRange, 1 << LayerMask.NameToLayer("NPC"));

        foreach (var coll in colls)
        {
            coll.GetComponent<NPC_AI>().ExplodeDie();
        }
    }

    void ExplodeDamage_Horse_NPC()
    {
        Collider[] colls = Physics.OverlapSphere(transform.position, ExplodeRange, 1 << LayerMask.NameToLayer("Horse_NPC"));

        foreach (var coll in colls)
        {
            coll.GetComponent<HorseAI_NPC>().ExplodeDie();
        }
    }
}
