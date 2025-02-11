using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathTrigger : MonoBehaviour
{
    private Game_Manager gm;

    private void Start()
    {
        gm = FindObjectOfType<Game_Manager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player1"))
        {
            collision.gameObject.GetComponent<_PlayerHealth>().health = collision.gameObject.GetComponent<_PlayerHealth>().maxhealth;
            collision.transform.localPosition = new Vector3(-10, 2, 0);
            gm.p2_Points++;
        }

        else if (collision.gameObject.CompareTag("Player2"))
        {
            collision.gameObject.GetComponent<_PlayerHealth>().health = collision.gameObject.GetComponent<_PlayerHealth>().maxhealth;
            collision.transform.localPosition = new Vector3(10, 2, 0);
            gm.p1_Points++;
        }
    }
}
