using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBow : MonoBehaviour
{
    #region Variables

    private EnemyAI_Mongol Enemy;
    private AudioSource Audio;
    
    public AudioClip FireSFX;
    public AudioClip AttackSFX;
    public GameObject ArrowPrefab;
    public Transform FirePos;

    #endregion

    #region Initialization

    void Start()
    {
        Enemy = GetComponent<EnemyAI_Mongol>();
        Audio = GetComponent<AudioSource>();
    }

    private void LateUpdate()
    {
        Aim();
    }

    #endregion

    #region Functions

    void Aim()
    {
        FirePos.transform.LookAt(new Vector3(Enemy.PlayerTransform.position.x, Enemy.PlayerTransform.position.y + 1.4f, Enemy.PlayerTransform.position.z));
    }

    #endregion

    #region Animation Func

    void Fire()
    {
        Audio.PlayOneShot(AttackSFX, 1f);
        Audio.PlayOneShot(FireSFX, 0.5f);

        GameObject _Arrow = Instantiate(ArrowPrefab, FirePos.position, FirePos.rotation);

        Destroy(_Arrow, 10f);
    }

    #endregion
}
