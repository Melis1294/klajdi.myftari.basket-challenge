using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    public static TimerController Instance { get; private set; }

    [SerializeField] private float startupTime;
    public float RemainingTime;
    [SerializeField] private bool _gameStarted;
    [SerializeField] private TextMeshProUGUI gameTimerText;
    [SerializeField] private TextMeshProUGUI startupTimerText;
    [SerializeField] private GameObject gameOverScreen;

    private void Awake()
    {
        // Prevent class instance duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        gameOverScreen.SetActive(false);
    }


    // Show or hide startup and game timers accordingly
    private void Start() => UpdateTimersUI();

    // Update is called once per frame
    void Update()
    {
        if (!_gameStarted)  // Manage startup timer
        {
            int countdown = Mathf.FloorToInt(startupTime % 60);
            startupTimerText.text = string.Format("{0}", countdown);
            startupTime -= Time.deltaTime;
            if (startupTime <= 1)
            {
                _gameStarted = true;
                startupTime = 1;
                UpdateTimersUI();
                GameManager.Instance.UpdateGameState(GameManager.GameState.Play);
            }
            return;
        }

        // Manage game timer
        if (RemainingTime > 0)
        {
            RemainingTime -= Time.deltaTime;
        }
        else if (RemainingTime < 0)
        {
            RemainingTime = 0;
            GameManager.Instance.UpdateGameState(GameManager.GameState.GameOver);
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

    // Wait until last ball is shot to show the right score
    IEnumerator SetupGameOver()
    {
        yield return new WaitForSeconds(3f);
        GameOver();
    }

    // Setup Game Over UI
    public void GameOver()
    {
        TextMeshProUGUI totalScoreUI = gameOverScreen.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        int playerScore = GameManager.Instance.TotalScore;
        int opponentScore = GameManager.Instance.OpponentScore;
        // TODO: Manage even points case (+ time to win) and fix UI
        string victoryText = playerScore > opponentScore ? "You win" : "You lose";
        totalScoreUI.text = string.Format("{0}\nTotal Score: {1} - {2}", victoryText, playerScore, opponentScore);
        BackboardController.Instance.ResetValue();
        BackboardController.Instance.enabled = false;
        gameTimerText.transform.parent.gameObject.SetActive(false);
        gameOverScreen.SetActive(true);
    }
}
