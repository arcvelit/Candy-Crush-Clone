using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class Session : MonoBehaviour
{
    [SerializeField]
    GridManager grid;

    [SerializeField]
    AudioSource winSFX, loseSFX, tickingSFX, backgroundSFX;

    [SerializeField]
    TextMeshProUGUI scoreText;
    int score = 0;

    [SerializeField]
    TextMeshProUGUI finalScoreText;

    [SerializeField]
    TextMeshProUGUI loserScoreText;

    [SerializeField]
    TextMeshProUGUI whyText;


    [SerializeField]
    TextMeshProUGUI movesText;
    [SerializeField]
    int movesLeft = 15;

    [SerializeField]
    TextMeshProUGUI timerText;
    [SerializeField]
    float timeLeft = 90; // seconds


    [SerializeField]
    GameObject winnerScreen;

    [SerializeField]
    GameObject loserScreen;

    
    [SerializeField]
    GameObject star2;
    [SerializeField]
    GameObject star3;


    const int OBJECTIVE_NUMBER = 3;

    [SerializeField]
    TextMeshProUGUI redText;
    int redCount = OBJECTIVE_NUMBER;
    [SerializeField]
    TextMeshProUGUI blueText;
    int blueCount = OBJECTIVE_NUMBER;
    [SerializeField]
    TextMeshProUGUI yellowText;
    int yellowCount = OBJECTIVE_NUMBER;
    [SerializeField]
    TextMeshProUGUI purpleText;
    int purpleCount = OBJECTIVE_NUMBER;
    [SerializeField]
    TextMeshProUGUI greenText;
    int greenCount = OBJECTIVE_NUMBER;

    bool timesUp => timeLeft < 0;
    bool paused = false;

    // Set clock?
    void Start() 
    {
        movesText.text = movesLeft.ToString();
        scoreText.text = score.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (!paused && !timesUp) 
        {
            ClockTick();
        }

    }


    void ClockTick()
    {
        timeLeft -= Time.deltaTime;
        TimeSpan t = TimeSpan.FromSeconds((int)timeLeft);
        timerText.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        if ((int)timeLeft == 0)
        {
            EndGameQuestionMark();
        }
        else if ((int)timeLeft == 10)
        {
            tickingSFX.Play();
        }
    }

    public void DecrementMovesLeft()
    {
        movesLeft--;
        movesText.text = movesLeft.ToString();
    }

    public bool EndGameQuestionMark()
    {
        bool gotObjectives = redCount == 0 && yellowCount == 0 && blueCount == 0 && greenCount == 0 && purpleCount == 0;
        // Win screen
        if (timesUp && !gotObjectives)
        {
            LoserScreen("on time");
            return true;
        }
        else if (movesLeft == 0 && !gotObjectives)
        {
            LoserScreen("on moves");
            return true;
        }
        if (gotObjectives)
        {
            WinnerScreen();
            return true;
        }
        return false;
    }

    void WinnerScreen()
    {
        Time.timeScale = 0;
        winnerScreen.SetActive(true);
        finalScoreText.text = scoreText.text;

        if (score >= 2000) star2.SetActive(true);
        if (score >= 3000) star3.SetActive(true);

        backgroundSFX.Pause();
        winSFX.Play();
    }

    void LoserScreen(string why)
    {
        Time.timeScale = 0;
        loserScreen.SetActive(true);
        loserScoreText.text = scoreText.text;
        whyText.text = why; 

        backgroundSFX.Pause();
        loseSFX.Play();
    }


    public void AddToScore(int points)
    {
        score += points;
        scoreText.text = score.ToString();
    }

    public void PauseGame() 
    {
        paused = true;
        Time.timeScale = 0;
    }
    public void ResumeGame() 
    {
        paused = false;
        Time.timeScale = 1;
    }

    public void QuitSession()
    {
        SceneManager.LoadScene("Main");
    }

    public void DecrementCount(string color)
    {
        switch(color)
        {
            case "Red": if (redCount>0) { redCount--; redText.text = redCount.ToString();} break;
            case "Yellow": if (yellowCount>0) { yellowCount--; yellowText.text = yellowCount.ToString();} break;
            case "Purple": if (purpleCount>0) { purpleCount--; purpleText.text = purpleCount.ToString();} break;
            case "Blue": if (blueCount>0) { blueCount--; blueText.text = blueCount.ToString();} break;
            case "Green": if (greenCount>0) { greenCount--; greenText.text = greenCount.ToString();} break;
        }
    }


}
