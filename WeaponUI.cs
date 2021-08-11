using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    #region Variables

    private PlayerMovement playerMovement;

    [Header("WeaponUI")]
    public GameObject Weapon;
    public Text Katana;
    public Text Bow;

    [Header("TypeUI")]
    public GameObject TypeUI;
    public GameObject TypeA_Select;
    public GameObject TypeA_UnSelect;
    public GameObject TypeB_Select;
    public GameObject TypeB_UnSelect;
    public GameObject TypeC_Select;
    public GameObject TypeC_UnSelect;

    [Header("Screen Effect")]
    public Image ScreenEffect;

    #endregion

    #region Initialization

    private void Start()
    {
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        OnWeaponUI();
        OffWeaponUI();
 
        VisualWeaponUI();
        VisualTypeUI();
    }

    #endregion

    #region Functions

    private void OnWeaponUI()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            Weapon.SetActive(true);
            TypeUI.SetActive(true);
            ScreenEffect.color = new Color(0f, 0f, 0f, 1f);
            ChangeWeaponUI();        
            playerMovement.ChangeType();
            playerMovement.SlowMotionEnter();
        }
    }

    private void OffWeaponUI()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            Weapon.SetActive(false);
            TypeUI.SetActive(false);
            ScreenEffect.color = new Color(0f, 0f, 0f, 0f);
            playerMovement.SlowMotionExit();
        }
    }

    private void ChangeWeaponUI()
    {
        float wheelInput = Input.GetAxis("Mouse ScrollWheel");

        if (wheelInput > 0)
        {
            playerMovement.WeaponType = PlayerMovement.E_WeaponType.KATANA;
            playerMovement.ChangeWeapon();
        }
        else if (wheelInput < 0)
        {
            playerMovement.WeaponType = PlayerMovement.E_WeaponType.BOW;
            playerMovement.ChangeWeapon();
        }
    }

    private void VisualWeaponUI()
    {
        if (playerMovement.WeaponType == PlayerMovement.E_WeaponType.KATANA)
        {
            Katana.color = Color.white;
            Bow.color = Color.black;
        }
        else if (playerMovement.WeaponType == PlayerMovement.E_WeaponType.BOW)
        {
            Katana.color = Color.black;
            Bow.color = Color.white;
        }
    }

    private void VisualTypeUI()
    {
        if (playerMovement.AttackType == PlayerMovement.E_AttackType.TYPE_A)
        {
            TypeA_Select.SetActive(true);
            TypeA_UnSelect.SetActive(false);

            TypeB_Select.SetActive(false);
            TypeB_UnSelect.SetActive(true);

            TypeC_Select.SetActive(false);
            TypeC_UnSelect.SetActive(true);
        }
        else if (playerMovement.AttackType == PlayerMovement.E_AttackType.TYPE_B)
        {
            TypeA_Select.SetActive(false);
            TypeA_UnSelect.SetActive(true);

            TypeB_Select.SetActive(true);
            TypeB_UnSelect.SetActive(false);

            TypeC_Select.SetActive(false);
            TypeC_UnSelect.SetActive(true);
        }
        else if (playerMovement.AttackType == PlayerMovement.E_AttackType.TYPE_C)
        {
            TypeA_Select.SetActive(false);
            TypeA_UnSelect.SetActive(true);

            TypeB_Select.SetActive(false);
            TypeB_UnSelect.SetActive(true);

            TypeC_Select.SetActive(true);
            TypeC_UnSelect.SetActive(false);
        }
    }

    #endregion
}
