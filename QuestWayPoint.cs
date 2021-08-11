using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestWayPoint : MonoBehaviour
{
    private Image WayPointMark;
    private Camera Cam;

    public Transform Player;
    public Transform Target;

    public float CloseEnoughDist;

    void Start()
    {
        WayPointMark = GetComponent<Image>();
        Cam = Camera.main;
    }

    void Update()
    {
        if (Target!= null)
        {
            GetDistance();
            CheckOnScreen();
        }
    }

    void GetDistance()
    {
        float dist = Vector3.Distance(Player.position, Target.position);

        if (dist < CloseEnoughDist)
        {
            Destroy(gameObject);
        }
    }

    void CheckOnScreen()
    {
        float thing = Vector3.Dot((Target.position - Cam.transform.position).normalized, Cam.transform.forward);

        if (thing <= 0)
        {
            ToggleUI(false);
        }
        else
        {
            ToggleUI(true);
            transform.position = Cam.WorldToScreenPoint(Target.position);
        }
    }

    void ToggleUI(bool _value)
    {
        WayPointMark.enabled = _value;
    }
}
