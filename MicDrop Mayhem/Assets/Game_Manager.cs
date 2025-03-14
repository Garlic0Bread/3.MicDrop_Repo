using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Game_Manager : MonoBehaviour
{
    public bool canStartTimer = false;
    public int rosterIndex = 1;//roster of songs
    public int p1_Points;
    public int p2_Points;
    [SerializeField] private TMP_Text p1Points;
    [SerializeField] private TMP_Text p2Points;

    [SerializeField] private float levelTimer;
    [SerializeField] private TMP_Text levelTimer_txt;

    void Update()
    {
        GameTimer();
        if (levelTimer < 0)
        {
            levelTimer = 0;
            MenuManager.Instance.EndGame();
        }

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Application.Quit();
        }

        p1Points.text = p1_Points.ToString();
        p2Points.text = p2_Points.ToString();
    }

    
    private void GameTimer()
    {
        if (canStartTimer == true)
        {
            if (levelTimer >= 0)
            {
                levelTimer -= Time.deltaTime;
            }
            if (levelTimer < 0)
            {
                levelTimer = 0;
                levelTimer_txt.color = Color.red;
            }
            int minutes = Mathf.FloorToInt(levelTimer / 60);
            int seconds = Mathf.FloorToInt(levelTimer % 60);
            levelTimer_txt.text = string.Format("{0:0}:{1:00}", minutes, seconds);
        }
    }
    public void Start_GAmeTimer()
    {
        canStartTimer = true;
    }

    public void Increase_RosterIndex()
    {
        rosterIndex = 2;
    }
}
