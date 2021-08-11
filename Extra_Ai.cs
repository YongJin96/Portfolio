using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Extra_Ai : MonoBehaviour
{
    public GameObject Effect;
    public Transform EffectTransform;

    void Start()
    {
        
    }
    
    void Update()
    {
        
    }

    #region AnimationFunc
    void OnEffect()
    {
        GameObject obj = Instantiate(Effect, EffectTransform.position, EffectTransform.rotation);
        Destroy(obj, 2f);
    }
    #endregion
}
