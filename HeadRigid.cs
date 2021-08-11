using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadRigid : MonoBehaviour
{
    #region Variables

    private Rigidbody HeadRig;
    public float Force;

    #endregion

    #region Initialization

    private void Awake()
    {
        HeadRig = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        transform.SetParent(null);
        HeadRig.AddForce(transform.up * Force);
        Destroy(gameObject, 10f);       
    }

    #endregion
}
