using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireMachine : MonoBehaviour
{
    private AudioSource Audio;

    public Transform FireTransform;
    public GameObject FireArrows;

    public AudioClip FireSFX;

    public float StartTime = 1f;
    public float FireTime = 5f;

    void Start()
    {
        Audio = GetComponent<AudioSource>();

        InvokeRepeating("Fire", StartTime, FireTime);
    }

    void Fire()
    {
        Audio.PlayOneShot(FireSFX, 1f);
        GameObject fireArrows = Instantiate(FireArrows, FireTransform.position, FireTransform.rotation);
        Destroy(fireArrows, 10f);
    }
}
