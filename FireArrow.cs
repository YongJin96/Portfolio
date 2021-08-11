using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireArrow : MonoBehaviour
{
    private Rigidbody ArrowRig;

    public GameObject Arrow;
    public AudioSource Audio;
    public AudioClip ArrowSFX;

    public static float Damage = 20f;
    public float Speed = 1000f;

    void Awake()
    {
        ArrowRig = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        ArrowRig.AddForce(transform.forward * Speed);
    }

    private void OnCollisionEnter(Collision coll)
    {
        ArrowRig.isKinematic = true;
        Audio.PlayOneShot(ArrowSFX, 1f);
        Destroy(this.gameObject, 10f);
    }
}
