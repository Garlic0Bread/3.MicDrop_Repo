using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Game_Manager : MonoBehaviour
{
    public bool canStartTimer = false;
    public int rosterIndex = 1;
    public int p1_Points;
    public int p2_Points;
    [SerializeField] private TMP_Text p1Points;
    [SerializeField] private TMP_Text p2Points;

    [SerializeField] private float levelTimer;
    [SerializeField] private TMP_Text levelTimer_txt;

    void Update()
    {
        if (levelTimer < 0)
        {
            levelTimer = 0;
        }
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Application.Quit();
        }

        p1Points.text = p1_Points.ToString();
        p2Points.text = p2_Points.ToString();

        if(canStartTimer == true)
        {
            levelTimer -= Time.deltaTime;
            levelTimer_txt.text = levelTimer.ToString();
        }

        if (levelTimer <= 0)
        {
            EndGame();
        }

        if(Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetKey(KeyCode.R))
        {
            EndGame();
        }
    }

    public void Start_GAmeTimer()
    {
        canStartTimer = true;
    }
    public void EndGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void Increase_RosterIndex()
    {
        rosterIndex = 2;
    }
}
