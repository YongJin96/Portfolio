using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyArrow : MonoBehaviour
{
    private PlayerMovement Player;
    private Rigidbody ArrowRig;
    private AudioSource Audio;
    private Transform HitPos;
    private TrailRenderer ArrowTrail;
    
    public AudioClip HitSFX;
    public float Speed = 1000f;
    public int Damage = 25;

    private void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        ArrowRig = GetComponent<Rigidbody>();
        Audio = GetComponent<AudioSource>();
        HitPos = GameObject.Find("ArrowHitPos").transform;
        ArrowTrail = GetComponent<TrailRenderer>();
    }

    private void OnEnable()
    {
        ArrowRig.AddForce(transform.forward * Speed);
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player") && Player.IsBlock == false)
        {
            ArrowRig.isKinematic = true;          
            this.gameObject.transform.SetParent(HitPos);
            this.gameObject.GetComponent<BoxCollider>().enabled = false;
            ArrowTrail.enabled = false;
        }
        else if (coll.gameObject.CompareTag("Player") && Player.IsBlock == true)
        {
            this.gameObject.GetComponent<BoxCollider>().enabled = false;
            ArrowTrail.enabled = false;
        }
    }
}
