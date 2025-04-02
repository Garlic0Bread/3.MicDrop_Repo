using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class _PlayerHealth : MonoBehaviour, IDamageable
{
    private Game_Manager gm;
    public BoxCollider2D feet_Collider;
    public BoxCollider2D body_Collider;

    public float health;
    public float maxhealth = 100;
    [SerializeField] private Image healthBar;
    [SerializeField] private AudioSource hurt;
    [SerializeField] private AudioSource death;
    [SerializeField] private GameObject deathVFX;

    private void Start()
    {
        gm = FindFirstObjectByType<Game_Manager>();
    }
    private void Update()
    {
        healthBar.fillAmount = health / 100f;
    }

    public void Damage(float amount)
    {
        if (amount < 0)
        {
            throw new System.ArgumentOutOfRangeException("Cannot have negative Damage");
        }
        hurt.Play();
        this.health -= amount;
        StartCoroutine(visualIndicator(Color.white));

        if (health <= 0)
        {
            death.Play();
            Die();
        }
    }
    public void Heal(float amount)
    {
        if (amount < 0)
        {
            throw new System.ArgumentOutOfRangeException("Cannot have negative Healing");
        }

        bool wouldBeOverMaHealth = health + amount > maxhealth;
        StartCoroutine(visualIndicator(Color.green));
        if (wouldBeOverMaHealth)
        {
            this.health = maxhealth;
        }
        else
        {
            this.health += amount;
        }
    }
    private void Die()
    {
        Instantiate(deathVFX, transform.position, Quaternion.identity);
        Invoke("RespawnPlayer", 2f);
        #region Death KnockBack

        body_Collider.enabled = false;
        feet_Collider.enabled = false;

        _PlayerMovement playerMovement = GetComponent<_PlayerMovement>();
        playerMovement.KBTotalTime = 2f;
        #endregion
    }


    private IEnumerator visualIndicator(Color color)
    {
        Color currentColor = GetComponent<SpriteRenderer>().color;
        GetComponent<SpriteRenderer>().color = color;
        yield return new WaitForSeconds(0.15f);
        GetComponent<SpriteRenderer>().color = currentColor;
    }
    public void RespawnPlayer()
    {
        if (this.gameObject.CompareTag("Player1"))
        {
            _PlayerMovement playerMovement = GetComponent<_PlayerMovement>();
            playerMovement.KBTotalTime = 0.2f;

            body_Collider.enabled = true;
            feet_Collider.enabled = true;

            transform.localPosition = new Vector3(-10, 2, 0);
            gameObject.GetComponent<_PlayerHealth>().health = gameObject.GetComponent<_PlayerHealth>().maxhealth;
        }
        else if (this.gameObject.CompareTag("Player2"))
        {
            _PlayerMovement playerMovement = GetComponent<_PlayerMovement>();
            playerMovement.KBTotalTime = 0.2f;

            body_Collider.enabled = true;
            feet_Collider.enabled = true;

            transform.localPosition = new Vector3(10, 2, 0);
            gameObject.GetComponent<_PlayerHealth>().health = gameObject.GetComponent<_PlayerHealth>().maxhealth;
        }
    }
}
