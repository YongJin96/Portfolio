using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cliff : MonoBehaviour
{
    #region Variables

    private PlayerMovement playerMovement;
    private Transform PlayerTransform;
    private Animator Anim;

    private float InputX, InputY;

    static public bool IsCliff = false;

    #endregion

    #region Initialization

    private void Start()
    {
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        PlayerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        Anim = GameObject.FindGameObjectWithTag("Player").GetComponent<Animator>();
    }

    private void Update()
    {
        if (IsCliff)
        {
            playerMovement.enabled = false;
            CliffPosition();
            InputMagnitude();
        }
        else if (!IsCliff)
        {
            playerMovement.enabled = true;
        }
    }

    #endregion

    #region Functions

    private void InputMagnitude()
    {
        InputX = Input.GetAxis("Horizontal");
        InputY = Input.GetAxis("Vertical");

        Anim.SetFloat("InputX", InputX, 0.1f, Time.deltaTime);
        Anim.SetFloat("InputY", InputY, 0.1f, Time.deltaTime);
    }

    private void CliffPosition()
    {
        if (IsCliff == false) { return; }

        
    }

    private IEnumerator Cliffing()
    {
        if (Input.GetKey(KeyCode.E) && IsCliff == false)
        {
            PlayerTransform.position = transform.position;
            PlayerTransform.rotation = transform.rotation;
            Anim.SetBool("IsCliff", true);
            Anim.SetTrigger("Cliff");
            IsCliff = true;
            print("Cliff On");
        }

        yield return new WaitForSeconds(1f);
    }

    private void OnTriggerStay(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player"))
        {
            StartCoroutine(Cliffing());
        }
    }

    private void OnTriggerExit(Collider coll)
    {
        if (coll.gameObject.CompareTag("Player"))
        {
            Anim.SetBool("IsCliff", false);
            IsCliff = false;
            print("Cliff Off");
        }
    }

    #endregion
}
