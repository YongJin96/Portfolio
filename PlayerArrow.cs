using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerArrow : MonoBehaviour
{
    #region Variables

    private Rigidbody ArrowRig;

    public GameObject Arrow;
    public TrailRenderer ArrowTrail;

    public static float Damage = 20f;
    public float Speed = 1000f;

    #endregion

    #region Initialization

    private void Awake()
    {
        ArrowRig = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        ArrowRig.AddForce(transform.forward * Speed);
    }

    #endregion

    #region Functions

    private void OnCollisionEnter(Collision coll)
    {
        if (coll.gameObject.CompareTag("Enemy"))
        {
            Destroy(this.gameObject);
        }
        if (coll.gameObject.layer == LayerMask.NameToLayer("Map") || coll.gameObject.layer == LayerMask.NameToLayer("Default") || coll.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            ArrowRig.isKinematic = true;
            ArrowTrail.enabled = false;
        }
    }

    #endregion
}
