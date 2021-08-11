using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossStat : MonoBehaviour
{
    #region Variables

    private BossAI bossAi;
    private PlayerStat playerStat;
    private AudioSource Audio;

    private int RegenerationCount = 0; // 부활 횟수 카운트

    // 보스 체간 게이지 UI의 길이와 높이
    private const float POSTURE_BAR_WIDTH = 490f;
    private const float POSTURE_BAR_HEIGHT = 20f;

    public GameObject BloodEffect;
    public Image HealthBar;
    public RectTransform PostureRectTransform;
    public AudioClip[] HitSFX;
    public float MaxHealth = 600f;
    public float CurrentHealth;
    public int Damage = 30;

    public bool IsStun = false;
     
    #endregion

    #region Initialization

    void Start()
    {
        bossAi = GetComponent<BossAI>();
        playerStat = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStat>();
        Audio = GetComponent<AudioSource>();
        SetHealth();
        SetPosture(0);
    }

    void Update()
    {
        Invoke("PostureDecrease", 1f);

        Stun();

        if (CurrentHealth <= 0 && bossAi.Phase == BossAI.BossPhase.PHASE_1 && RegenerationCount == 0)
        {
            bossAi.Regeneration();
            SetHealth();
            SetPosture(0);
        }
        else if (CurrentHealth <= 0 && bossAi.Phase == BossAI.BossPhase.PHASE_2 && RegenerationCount == 1)
        {
            bossAi.Regeneration();
            SetHealth();
            SetPosture(0);
        }
        else if (CurrentHealth <= 0 && bossAi.Phase == BossAI.BossPhase.PHASE_3 && RegenerationCount == 2 && bossAi.IsDie == false)
        {
            bossAi.Die();
        }

        if (RegenerationCount == 1 && bossAi.Phase == BossAI.BossPhase.PHASE_1) // 부활을 한번했고 페이즈가 1이면 
        {
            bossAi.Phase = BossAI.BossPhase.PHASE_2;                            // 페이즈 2로 넘어가고 
            bossAi.Equip();                                                     // 페이즈 2 무기로 변경
            SetHealth();
            SetPosture(0);
        }
        else if (RegenerationCount == 2 && bossAi.Phase == BossAI.BossPhase.PHASE_2)
        {
            bossAi.Phase = BossAI.BossPhase.PHASE_3;
            SetHealth();
            SetPosture(0);
        }
    }

    #endregion

    #region Boss Function

    void SetHealth()
    {
        CurrentHealth = MaxHealth;
        HealthBar.fillAmount = 1f;
    }

    void SetPosture(float _postureNormalized)
    {
        PostureRectTransform.sizeDelta = new Vector2(_postureNormalized * POSTURE_BAR_WIDTH, PostureRectTransform.sizeDelta.y);
    }

    private void OnCollisionEnter(Collision coll)
    {
        if (coll.gameObject.layer == LayerMask.NameToLayer("Blade"))
        {
            StartCoroutine(HitDelay(coll));
        }

        if (coll.gameObject.layer == LayerMask.NameToLayer("Arrow"))
        {
            StartCoroutine(ArrowHitDelay(coll));
        }
    }

    IEnumerator HitDelay(Collision coll)
    {
        if (PlayerMovement.IsPerilousAttack == false && PlayerMovement.IsFireWeapon == false && bossAi.IsInvincibility == false)
        {
            TakeDamage(playerStat.Damage);
            PostureIncrease(30);
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            playerStat.Shake.ShakeCamera(4f, 0.1f);

            // 의지(체력회복) 획득량
            playerStat.SetPotionIncrease(Random.Range(0.05f, 0.08f));
        }
        else if (PlayerMovement.IsFireWeapon == true && bossAi.IsInvincibility == false) // 플레이어 무기에 붙 붙은경우 가드불가
        {
            TakeDamage(playerStat.Damage + 20f);
            PostureIncrease(50);
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            playerStat.Shake.ShakeCamera(4f, 0.1f);
            IsStun = false;

            // 의지(체력회복) 획득량
            playerStat.SetPotionIncrease(Random.Range(0.07f, 0.1f));
        }
        else if (PlayerMovement.IsPerilousAttack == true && bossAi.IsInvincibility == false) // 플레이어 PerilousAttack
        {
            TakeDamage(100f);
            PostureIncrease(100);
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            playerStat.Shake.ShakeCamera(4f, 0.1f);
            IsStun = false;

            // 의지(체력회복) 획득량
            playerStat.SetPotionIncrease(Random.Range(0.5f, 0.6f));
        }
        else if (bossAi.IsInvincibility == true) // 페이즈 바뀔때 보스 무적 상태
        {
            TakeDamage(0);
            PostureIncrease(0);
            ShowBloodEffect(coll);
            Audio.PlayOneShot(HitSFX[Random.Range(0, 3)], 1f);
            playerStat.Shake.ShakeCamera(4f, 0.1f);

            // 의지(체력회복) 획득량
            playerStat.SetPotionIncrease(Random.Range(0f, 0f));
        }

        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator ArrowHitDelay(Collision coll)
    {
        TakeDamage(5);
        ShowBloodEffect(coll);

        yield return new WaitForSeconds(0.2f);
    }

    void ShowBloodEffect(Collision coll)
    {
        Vector3 pos = coll.contacts[0].point;
        Vector3 normal = coll.contacts[0].normal;
        Quaternion rot = Quaternion.FromToRotation(-Vector3.forward, normal);

        GameObject blood = Instantiate(BloodEffect, pos, rot);
        Destroy(blood, 2f);
    }

    void TakeDamage(float _damage)
    {
        CurrentHealth -= _damage;
        HealthBar.fillAmount = CurrentHealth / MaxHealth;
    }

    public void PostureIncrease(int _amount)
    {
        if (PostureRectTransform.sizeDelta.x <= POSTURE_BAR_WIDTH)
        {
            PostureRectTransform.sizeDelta += new Vector2(_amount, 0f); // 체간게이지 증가
        }
    }

    void PostureDecrease()
    {
        if (PostureRectTransform.sizeDelta.x > 0)
        {
            PostureRectTransform.sizeDelta -= new Vector2(Random.Range(0.1f, 0.3f), 0); // 체간 게이지 감소
        }
    }

    void Stun()
    {
        if (PostureRectTransform.sizeDelta.x >= POSTURE_BAR_WIDTH)
        {
            PostureRectTransform.sizeDelta = new Vector2(0f, POSTURE_BAR_HEIGHT);
            bossAi.Stun();
            IsStun = true;
        }
    }

    #endregion

    #region Animation Func

    void AddRegenerationCount()
    {
        ++RegenerationCount;
    }

    #endregion
}
