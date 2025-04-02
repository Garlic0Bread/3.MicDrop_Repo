using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class _PlayerAttack : MonoBehaviour
{
    private Animator anim;
    private RaycastHit2D[] hits;
    private int currentAttack = 0;
    private bool isAttacking = false;
    private float timeSinceAttack;

    [SerializeField] private float timeToResetAttack;
    [SerializeField] private float deathKnockBackForce;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float timeBtwAttacks = 0.25f;
    [SerializeField] private Transform attackTransform;
    [SerializeField] private LayerMask attackableLayer;

    [Header("Audio Handling")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<float> targetTimes;
    [SerializeField] private float timeWindow = 5f;

    [Header("Melee Attack")]
    [SerializeField] private ScreenShake screenShake;
    [SerializeField] private GameObject hitVFX;
    [SerializeField] private float damageAmount = 1f;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float originalKnockbackForce = 10f;
    private bool canAttack = true;

    [Header("Beat-Attack Management")]
    [SerializeField] private float onBeatKnockBackForce;
    [SerializeField] private float extraDamageAmount;
    [SerializeField] private BeatManager beatManager;
    [SerializeField] private GameObject onBeatAura;
    private bool isOnBeat;

    [Header("Mayhem System")]
    [SerializeField] private float mayhem;
    [SerializeField] private float mayhemAmount;
    [SerializeField] private float maxMayhem = 100;
    [SerializeField] private Image mayhemBar;

    private void Start()
    {
        anim = GetComponent<Animator>();
        beatManager = FindObjectOfType<BeatManager>();
        timeSinceAttack = timeBtwAttacks;
    }

    private void Update()
    {
        HandleAttackInput();
        UpdateMayhemBar();
    }

    private void HandleAttackInput()
    {
        if (!canAttack || timeSinceAttack < timeBtwAttacks) return;

        if (gameObject.CompareTag("Player1") && _InputManager.P1_isLightAttacking)
        {
            PerformAttack();
        }
        else if (gameObject.CompareTag("Player2") && _InputManager_P2.isLightAttacking)
        {
            PerformAttack();
        }

        timeSinceAttack += Time.deltaTime;
    }

    private void PerformAttack()
    {
        isAttacking = true;
        StartCoroutine(OnBeatAttack());
        hits = Physics2D.CircleCastAll(attackTransform.position, attackRange, transform.right, 0f, attackableLayer);

        foreach (var hit in hits)
        {
            var damageable = hit.collider.GetComponent<IDamageable>();
            var playerOne = hit.collider.GetComponent<PlayerOne_Controller>(); 
            var playerTwo = hit.collider.GetComponent<_PlayerMovement>();
            var enemyHealth = hit.collider.GetComponent<_PlayerHealth>();

            if (playerOne != null && damageable != null)
            {
                ApplyDamageAndKnockback(playerOne, playerTwo, damageable, enemyHealth);
            }
            else if(playerTwo != null && damageable != null)
            {
                ApplyDamageAndKnockback(playerOne, playerTwo, damageable, enemyHealth);
            }
        }

        currentAttack = (currentAttack % 3) + 1;
        if (timeSinceAttack > timeToResetAttack) currentAttack = 1;
    }

    private void ApplyDamageAndKnockback(PlayerOne_Controller playerOne, _PlayerMovement playerTwo, IDamageable damageable, _PlayerHealth enemyHealth)
    {
        if (playerOne != null && damageable != null)
        {
            Vector3 hitPosition = playerOne.transform.position;
            bool isEnemyLowHealth = enemyHealth.health <= (15f / enemyHealth.maxhealth * 100);

            float appliedKnockback = isOnBeat ? onBeatKnockBackForce : knockbackForce;
            if (isEnemyLowHealth) appliedKnockback = deathKnockBackForce;

            screenShake.ShakeCamera();
            damageable.Damage(isOnBeat ? extraDamageAmount : damageAmount);
            Instantiate(hitVFX, hitPosition, Quaternion.identity);

            playerOne.KBCounter = playerOne.KBTotalTime;
            playerOne.KBForce = appliedKnockback;
            playerOne.KnockFromRight = playerOne.transform.position.x <= transform.position.x;

            if (enemyHealth.health <= 0)
            {
                HandleEnemyDeath();
            }
        }
        else if (playerTwo != null && damageable != null)
        {
            Vector3 hitPosition = playerTwo.transform.position;
            bool isEnemyLowHealth = enemyHealth.health <= (15f / enemyHealth.maxhealth * 100);

            float appliedKnockback = isOnBeat ? onBeatKnockBackForce : knockbackForce;
            if (isEnemyLowHealth) appliedKnockback = deathKnockBackForce;

            screenShake.ShakeCamera();
            damageable.Damage(isOnBeat ? extraDamageAmount : damageAmount);
            Instantiate(hitVFX, hitPosition, Quaternion.identity);

            playerTwo.KBCounter = playerTwo.KBTotalTime;
            playerTwo.KBForce = appliedKnockback;
            playerTwo.KnockFromRight = playerTwo.transform.position.x <= transform.position.x;

            if (enemyHealth.health <= 0)
            {
                HandleEnemyDeath();
            }
        }
    }
    private void HandleEnemyDeath()
    {
        Debug.Log("DEATH");
        if (gameObject.CompareTag("Player1"))
            Game_Manager.Instance.p1_Points++;
        else if (gameObject.CompareTag("Player2"))
            Game_Manager.Instance.p2_Points++;
    }

    private void UpdateMayhemBar()
    {
        mayhemBar.fillAmount = mayhem / maxMayhem;
    }

    IEnumerator OnBeatAttack()
    {
        if (beatManager != null)
        {
            int audioIndex = beatManager.GetCurrentAudioIndex();

            if (audioIndex != -1 && beatManager.IsOnBeat(audioIndex)) // Check if on beat
            {
                onBeatAura.SetActive(true);
                isOnBeat = true;
            }
        }
        yield return new WaitForSeconds(0.2f);
        onBeatAura.SetActive(false);
        isOnBeat = false;
    }
}

