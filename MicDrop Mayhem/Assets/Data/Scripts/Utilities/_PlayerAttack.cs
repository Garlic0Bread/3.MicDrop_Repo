using OWL;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;

public class _PlayerAttack : MonoBehaviour
{
    private Animator anim;
    public Game_Manager gm;
    private RaycastHit2D[] hits;
    private int currentAttack = 0;
    public bool isAttacking = false;
    public float timeSinceAttack = 0.0f;
    [SerializeField] private float death_KnockBackForce;

    [Header("Audio Handling")]
    [SerializeField] private float currentAudioTime;
    [SerializeField] private float timeWindow = 5f; // 5-second window
    [SerializeField] private List<float> targetTimes; // 00:20 in seconds
    [SerializeField] private AudioSource audioSource; // Your music track

    [Header("Melee Attack")]
    public ScreenShake screenShake;
    public GameObject hitVFX;
    private bool canAttack = true;
    public float damageAmount = 1f;
    public float knockbackForce = 10f;
    public float originalKnockbackForce = 10f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private Transform attackTransform;
    [SerializeField] private LayerMask attackableLayer;
    [SerializeField] private float timeBtwAttacks = 0.25f;

    [Header("Beat-Attack Management")]
    [SerializeField] private float onBeat_KnockBackForce;
    [SerializeField] private float extraDamageAmount;
    [SerializeField] private BeatManager beatManager;
    [SerializeField] private GameObject onBeatAura;
    private bool isOnBeat;

    [Header("Special Attack Management")]
    [SerializeField] private GameObject spMove_Blockers;
    [SerializeField] private GameObject SP_aura;
    [SerializeField] private float SP_damage;

    [Header("Shooting Attack")]
    [SerializeField] private float nextFireTime;
    [SerializeField] private float shieldTimout;
    [SerializeField] private Transform playerGun;
    [SerializeField] private int numBinSpread = 3;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float spreadAngle = 15f;
    public ShootMode currentShootmode = ShootMode.Single;

    [Header("Mayhem")]
    private float mayhem = 0;
    public float mayhemAmount;
    public float maxmayhem = 100;
    [SerializeField] private Image mayhemBar;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attackTransform.position, attackRange);
    }
    public enum ShootMode
    {
        Single,
        Spread,
    }

    private void Start()
    {
        extraDamageAmount = damageAmount + extraDamageAmount;
        beatManager = FindObjectOfType<BeatManager>();
        SetShootMode(currentShootmode);
        anim = GetComponent<Animator>();
        timeSinceAttack = timeBtwAttacks;
    }
    private void Update()
    {
        Attacking_Inputs();//inputs for light attack, special attack for both plaer 1 and player 2
        mayhemBar.fillAmount = mayhem / 100f;

    }

    private void Attacking_Inputs()
    {
        if (this.gameObject.CompareTag("Player1"))
        {
            #region Code for Specific Timestamp Within Song E.g. Hype part of a song. Timestamp can be set in the inspector
            currentAudioTime = audioSource.time;
            foreach (float targetTime in targetTimes)
            {
                /*if (currentAudioTime >= targetTime - timeWindow && currentAudioTime <= targetTime + timeWindow)
                {
                   
                }

                else if (currentAudioTime > targetTime + timeWindow)
                {
                    SP_NOW.SetActive(false); // Disable SP_NOW after the time window has passed
                }*/
            }
            #endregion

            if (_InputManager.P1_isLightAttacking && timeSinceAttack >= timeBtwAttacks)
            {
                isAttacking = true;
                Light_Attack();
            }
            timeSinceAttack += Time.deltaTime;

            #region SHOOTING
            /*else if (Input.GetMouseButtonDown(1))
            {
                Projectile_Attack();
                print("shooting");
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) //changing shooting modes
            {
                SetShootMode(ShootMode.Single);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetShootMode(ShootMode.Spread);
            }*/
            #endregion
        }


        //PLAYER TWO
        if (this.gameObject.CompareTag("Player2"))
        {
            #region Code for Specific Timestamp Within Song E.g. Hype part of a song. Timestamp can be set in the inspector
            currentAudioTime = audioSource.time;
            /*foreach (float targetTime in targetTimes)
            {
                if (currentAudioTime >= targetTime - timeWindow && currentAudioTime <= targetTime + timeWindow)
                {
                    
                }
                else
                {

                }
            }*/
            #endregion

            if (_InputManager_P2.isLightAttacking && timeSinceAttack >= timeBtwAttacks)
            {
                isAttacking = true;
                Light_Attack();
            }
            timeSinceAttack += Time.deltaTime;

            #region SHOOTING
            /*else if (Input.GetMouseButtonDown(1))
            {
                Projectile_Attack();
                print("shooting");
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) //changing shooting modes
            {
                SetShootMode(ShootMode.Single);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetShootMode(ShootMode.Spread);
            }*/
            #endregion
        }
    }

    private void SpecialMove()
    {
        StartCoroutine(SpecialMove_Activated());
        hits = Physics2D.CircleCastAll(attackTransform.position, attackRange, transform.right, 0f, attackableLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable i_Damageable = hits[i].collider.gameObject.GetComponent<IDamageable>();

            if (i_Damageable != null)
            {
                i_Damageable.Damage(SP_damage);
            }
        }
    }
    private void Light_Attack()
    {
        if (canAttack)
        {
            StartCoroutine(OnBeatAttack());

            hits = Physics2D.CircleCastAll(attackTransform.position, attackRange, transform.right, 0f, attackableLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable i_Damageable = hits[i].collider.gameObject.GetComponent<IDamageable>();
                _PlayerMovement playerMovement = hits[i].collider.gameObject.GetComponent<_PlayerMovement>();
                _PlayerHealth enemyHealth = hits[i].collider.gameObject.GetComponent<_PlayerHealth>();

                if (playerMovement != null)
                {
                    Vector3 hitPosition = hits[i].collider.transform.position;

                    if (isOnBeat == true)
                    {
                        screenShake.ShakeCamera();
                        IncreaseMayhem(mayhemAmount);
                        i_Damageable.Damage(extraDamageAmount);
                        Instantiate(hitVFX, hitPosition, Quaternion.identity);
                        #region KNOCKBACK
                        playerMovement.KBCounter = playerMovement.KBTotalTime;
                        playerMovement.KBForce = onBeat_KnockBackForce;

                        if (playerMovement.transform.position.x <= transform.position.x)
                        {
                            playerMovement.KnockFromRight = true;
                        }
                        if (playerMovement.transform.position.x >= transform.position.x)
                        {
                            playerMovement.KnockFromRight = false;
                        }
                        #endregion
                    }
                    else if (isOnBeat == false)
                    {
                        screenShake.ShakeCamera();
                        i_Damageable.Damage(damageAmount);
                        Instantiate(hitVFX, hitPosition, Quaternion.identity);
                        #region KNOCKBACK
                        playerMovement.KBCounter = playerMovement.KBTotalTime;
                        playerMovement.KBForce = knockbackForce;

                        if (playerMovement.transform.position.x <= transform.position.x)
                        {
                            playerMovement.KnockFromRight = true;
                        }
                        if (playerMovement.transform.position.x >= transform.position.x)
                        {
                            playerMovement.KnockFromRight = false;
                        }
                        #endregion
                    }

                    #region DEATH KNOCKBACK
                    if (enemyHealth.health <= 15 / enemyHealth.maxhealth * 100)
                    {
                        
                        playerMovement.KBCounter = playerMovement.KBTotalTime;
                        playerMovement.KBForce = death_KnockBackForce;

                        if (playerMovement.transform.position.x <= transform.position.x)
                        {
                            playerMovement.KnockFromRight = true;
                        }
                        if (playerMovement.transform.position.x >= transform.position.x)
                        {
                            playerMovement.KnockFromRight = false;
                        }
                        
                    }
                    #endregion

                    if (enemyHealth.health <= 0 && this.gameObject.CompareTag("Player1"))
                    {
                        print("DEATH");
                        gm.p1_Points++;
                    }
                    else if (enemyHealth.health <= 0 && this.gameObject.CompareTag("Player2"))
                    {
                        print("DEATH");
                        gm.p2_Points++;
                    }
                }
            }

            currentAttack++;
            if (currentAttack > 3)
                currentAttack = 1;// Loop back to one after third attack

            if (timeSinceAttack > 1.0f)//if time since last attack is too large reset Light_Attack combo 
                currentAttack = 1;

            // Call one of three attack animations "Attack1", "Attack2", "Attack3"
            anim.SetTrigger("Light_Attack" + currentAttack);
            timeSinceAttack = 0.0f;
        }
        else
            return;
    }
    public void IncreaseMayhem(float amount)
    {
        this.mayhem += amount;
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
    IEnumerator SpecialMove_Activated()
    {
        if (this.gameObject.CompareTag("Player1"))
        {
            _PlayerMovement my_Movement = gameObject.GetComponent<_PlayerMovement>();
            anim.SetBool("SpecialAttack", true);
            spMove_Blockers.SetActive(true);
            my_Movement.player1_CanMove = false;
            SP_aura.SetActive(true);
            canAttack = false;
        }
        else if (this.gameObject.CompareTag("Player2"))
        {
            _PlayerMovement my_Movement = gameObject.GetComponent<_PlayerMovement>();
            anim.SetBool("SpecialAttack", true);
            spMove_Blockers.SetActive(true);
            my_Movement.player2_CanMove = false;
            SP_aura.SetActive(true);
            canAttack = false;
        }

        yield return new WaitForSeconds(3f);

        if (this.gameObject.CompareTag("Player1"))
        {
            _PlayerMovement my_Movement = gameObject.GetComponent<_PlayerMovement>();
            anim.SetBool("SpecialAttack", false);
            spMove_Blockers.SetActive(false);
            my_Movement.player1_CanMove = true;
            SP_aura.SetActive(false);
            canAttack = true;
        }
        else if (this.gameObject.CompareTag("Player2"))
        {
            _PlayerMovement my_Movement = gameObject.GetComponent<_PlayerMovement>();
            anim.SetBool("SpecialAttack", false);
            spMove_Blockers.SetActive(false);
            my_Movement.player2_CanMove = true;
            SP_aura.SetActive(false);
            canAttack = true;
        }
    }

    #region SHOOTING LOGIC
    void Projectile_Attack()
    {
        switch (currentShootmode)
        {
            case ShootMode.Single:
                ShootSingle();
                break;
            case ShootMode.Spread:
                SpreadShooting();
                break;
        }
    }
    void ShootSingle()
    {
        // Instantiate a bullet prefab at the player's gun position and rotation
        GameObject bullet = Instantiate(bulletPrefab, playerGun.position, Quaternion.identity);

        // Apply velocity to the bullet in the forward direction of the gun
        Rigidbody2D bulletRigidbody = bullet.GetComponent<Rigidbody2D>();
        bulletRigidbody.velocity = playerGun.right * bulletSpeed;
    }
    void SpreadShooting()
    {
        for (int i = 0; i < numBinSpread; i++)
        {
            float angle = playerGun.rotation.eulerAngles.z - spreadAngle / 2f + i * (spreadAngle / (numBinSpread - 1));

            // Calculate direction based on the angle
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
            GameObject bullet = Instantiate(bulletPrefab, playerGun.position, Quaternion.identity);

            // Apply velocity to the bullet in the forward direction of the gun
            Rigidbody2D bulletRigidbody = bullet.GetComponent<Rigidbody2D>();
            bulletRigidbody.velocity = direction * bulletSpeed;
        }
    }
    void SetShootMode(ShootMode newMode)
    {
        currentShootmode = newMode;
    }
    #endregion
}
