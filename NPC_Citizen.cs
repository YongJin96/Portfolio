using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class NPC_Citizen : MonoBehaviour
{
    #region Variables

    private NavMeshAgent Agent;
    private Animator Anim;
    private BoxCollider InteractionCollider;

    private float Speed = 0f;

    public enum EStates
    {
        IMPRISON,
        RELEASE,
    }

    public EStates States = EStates.IMPRISON;

    [Header("UI")]
    public GameObject UI;
    public Image PressButtonImage;
    public Image GaugeImage;

    public Transform WayPoint;

    public bool IsRelease = false;
    public bool IsLive = false;

    #endregion

    #region Initialization

    private void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        Anim = GetComponent<Animator>();
        InteractionCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        StartCoroutine(CheckState());
        StartCoroutine(Action());
        StartCoroutine(PressGauge());
        StartCoroutine(LookAtCamera());

        Speed = Agent.speed;
    }

    #endregion

    #region Functions

    private IEnumerator CheckState()
    {
        if (IsRelease == false && IsLive == false)
        {
            States = EStates.IMPRISON;
        }
        else if (IsRelease == true && GaugeImage.fillAmount == 1f)
        {
            States = EStates.RELEASE;
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator Action()
    {
        switch (States)
        {
            case EStates.IMPRISON:
                Anim.SetBool("IsRelease", false);               
                break;

            case EStates.RELEASE:
                Release();

                Agent.destination = WayPoint.transform.position;
                Agent.speed = 1;
                Anim.SetFloat("Speed", Speed, 0.1f, Time.deltaTime);

                Destroy(this.gameObject, 15f);
                break;
        }

        yield return null;
    }

    private IEnumerator PressGauge()
    {
        while (GaugeImage.fillAmount <= 1 && IsRelease == true)
        {
            GaugeImage.fillAmount += Time.deltaTime * 0.01f;
            yield return null;
        }

        IsRelease = false;
        GaugeImage.fillAmount = 0f;
    }

    private IEnumerator LookAtCamera()
    {
        PressButtonImage.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        GaugeImage.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        yield return new WaitForSeconds(0.2f);
    }

    private void OnTriggerStay(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player"))
        {
            PressButtonImage.enabled = true;

            if (Input.GetKey(KeyCode.E) && GaugeImage.fillAmount <= 1f)
            {
                IsRelease = true;
            }
            else if (Input.GetKeyUp(KeyCode.E))
            {
                IsRelease = false;
                GaugeImage.fillAmount = 0f;
            }
        }
    }

    private void OnTriggerExit(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player"))
        {
            PressButtonImage.enabled = false;
            IsRelease = false;
            GaugeImage.fillAmount = 0f;
        }
    }

    private void Release()
    {
        IsLive = true;
        IsRelease = false;
        Anim.SetBool("IsRelease", true);

        UI.SetActive(false);
    }

    #endregion
}
