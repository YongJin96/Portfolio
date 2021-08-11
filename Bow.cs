using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class Bow : MonoBehaviour
{
    #region Variables

    private PlayerMovement playerMovement;
    private Camera Cam;
    private CinemachineVirtualCamera VZoom; // base
    private CinemachineVirtualCamera VZoom_Horse;
    private Animator Anim;

    public CinemachineShake Shake;
    public PlayerArrow playerArrow;
    public Transform FirePos;
    public Image CrossHair;

    [Header("Chest Born")]
    private Transform Chest;
    public Vector3 ChestOffset;
    public Vector3 ChestDirection;

    #endregion

    #region Initialization

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        VZoom = GameObject.Find("CM VZoom").GetComponent<CinemachineVirtualCamera>();
        VZoom_Horse = GameObject.Find("CM VZoom_Horse").GetComponent<CinemachineVirtualCamera>();
        Cam = Camera.main;
        Anim = GetComponent<Animator>();

        Chest = Anim.GetBoneTransform(HumanBodyBones.Spine); 
    }

    private void LateUpdate()
    {
        Aim();
    }

    #endregion

    #region Functions

    public void Aim()
    {
        if (playerMovement.IsAiming == true && playerMovement.IsMount == false)
        {
            //transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(transform.forward), Quaternion.LookRotation(Cam.transform.forward), Time.deltaTime * 10f);
            ChestDirection = Cam.transform.position + Cam.transform.forward * 50f;
            Chest.LookAt(ChestDirection);
            Chest.rotation = Chest.rotation * Quaternion.Euler(ChestOffset);
            VZoom.Priority = 11;
            VZoom_Horse.Priority = 9;
            CrossHair.enabled = true;
        }
        else if (playerMovement.IsAiming == true && playerMovement.IsMount == true)
        {
            ChestDirection = Cam.transform.position + Cam.transform.forward * 50f;
            Chest.LookAt(ChestDirection);
            Chest.rotation = Chest.rotation * Quaternion.Euler(ChestOffset);
            VZoom.Priority = 9;
            VZoom_Horse.Priority = 11;
            CrossHair.enabled = true;
        }
        else if (playerMovement.IsAiming == false)
        {
            VZoom.Priority = 9;
            VZoom_Horse.Priority = 9;
            CrossHair.enabled = false;
        }        
    }

    #endregion

    #region Animation Func

    private void Fire()
    {
        if (playerMovement.IsAiming == true)
        {
            Shake.ShakeCamera(2f, 0.1f);
            var arrow = Instantiate(playerArrow.Arrow, FirePos.transform.position, Quaternion.LookRotation(Cam.transform.forward));
            Destroy(arrow, 5f);
        }
    }

    #endregion
}
