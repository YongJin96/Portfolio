using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HeadTracking : MonoBehaviour
{
    #region Variables

    private List<LookAtPoint> Points;
    private float RadiusSqr;
    private Vector3 OriginPos;

    public Transform Target;
    public Rig HeadRig;
    public float Radius = 10f;
    public float RetargetSpeed = 5f;
    public float MaxAngle = 90f;

    #endregion

    #region Initialization

    private void Start()
    {
        Points = FindObjectsOfType<LookAtPoint>().ToList();
        RadiusSqr = Radius * Radius;
        OriginPos = Target.position;
    }

    private void Update()
    {
        Transform tracking = null;
        
        foreach (LookAtPoint point in Points)
        {
            Vector3 delta = point.transform.position - transform.position;

            if (delta.sqrMagnitude < RadiusSqr)
            {
                float angle = Vector3.Angle(transform.forward, delta);
                if (angle < MaxAngle)
                {
                    tracking = point.transform;
                    break;
                }
            }
        }

        float rigWeight = 0;
        Vector3 targetPos = transform.position + (transform.forward * 2f);

        if (tracking != null)
        {
            targetPos = tracking.position;
            rigWeight = 1;
        }
        Target.position = Vector3.Lerp(Target.position, targetPos, Time.deltaTime * RetargetSpeed);
        HeadRig.weight = Mathf.Lerp(HeadRig.weight, rigWeight, Time.deltaTime * 2);
    }

    #endregion
}
