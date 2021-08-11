using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    #region Variables

    private Rigidbody BulletRig;
    private AudioSource Audio;

    public AudioClip HitSFX;
    public float Speed = 1000f;
    public int Damage = 25;

    #endregion

    #region Initialization

    private void Awake()
    {
        BulletRig = GetComponent<Rigidbody>();
        Audio = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        BulletRig.AddForce(transform.forward * Speed);
    }

    #endregion

    #region Functions

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player"))
        {
            this.gameObject.GetComponent<SphereCollider>().enabled = false;
            Destroy(this.gameObject);
        }
    }

    #endregion
}
