using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    public static TimerController instance { get; private set; }

    [SerializeField] private float startupTime;
    public float RemainingTime;
    [SerializeField] private bool _gameStarted;
    [SerializeField] private TextMeshProUGUI gameTimerText;
    [SerializeField] private TextMeshProUGUI startupTimerText;
    [SerializeField] private GameObject gameOverScreen;

    private void Awake()
    {
        // Prevent class instance duplicates
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
        gameOverScreen.SetActive(false);
    }


    private void Start() => UpdateTimersUI();

    // Update is called once per frame
    void Update()
    {
        if (!_gameStarted)
        {
            int countdown = Mathf.FloorToInt(startupTime % 60);
            startupTimerText.text = string.Format("{0}", countdown);
            startupTime -= Time.deltaTime;
            if (startupTime <= 1)
            {
                _gameStarted = true;
                startupTime = 1;
                UpdateTimersUI();
                GameManager.instance.UpdateGameState(GameManager.GameState.Play);
            }
            return;
        }

        if (RemainingTime > 0)
        {
            RemainingTime -= Time.deltaTime;
        }
        else if (RemainingTime < 0)
        {
            RemainingTime = 0;
            GameManager.instance.UpdateGameState(GameManager.GameState.GameOver);
            StartCoroutine(SetupGameOver());
        }
        int minutes = Mathf.FloorToInt(RemainingTime / 60);
        int seconds = Mathf.FloorToInt(RemainingTime % 60);
        gameTimerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }

    void UpdateTimersUI()
    {
        gameTimerText.transform.parent.gameObject.SetActive(_gameStarted);
        startupTimerText.enabled = !_gameStarted;
    }

    IEnumerator SetupGameOver()
    {
        yield return new WaitForSeconds(3f);
        GameOver();
    }

    public void GameOver()
    {
        TextMeshProUGUI totalScoreUI = gameOverScreen.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        totalScoreUI.text = string.Format("Game Over\nTotal Score: {0}", GameManager.instance.TotalScore);
        BackboardController.instance.enabled = false;
        gameTimerText.transform.parent.gameObject.SetActive(false);
        gameOverScreen.SetActive(true);
    }
}
