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
    private TextMeshProUGUI _totalScoreUI;

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
        RemainingTime = SceneController.Instance.GetRetryTimer();
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
        _totalScoreUI = gameOverScreen.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        int playerScore = GameManager.Instance.TotalScore;
        int opponentScore = GameManager.Instance.OpponentScore;
        if (playerScore == opponentScore)
        {
            Debug.Log("Even points");
            GameManager.Instance.UpdateGameState(GameManager.GameState.Startup);
            // Reset total scores
            SceneController.Instance.SetScores(playerScore, opponentScore);
            SceneController.Instance.SetRetryTimer(10f);
            StartCoroutine(EvenPoints());
            return;
        }

        bool isSinglePlayer = GameManager.Instance.IsSinglePlayer;
        string victoryText = isSinglePlayer ? "" : playerScore > opponentScore ? "You win!\n" : "You lose!\n";
        string scoreRecapText = isSinglePlayer ? playerScore.ToString() : string.Format("{0} - {1}", playerScore, opponentScore);
        _totalScoreUI.text = string.Format("{0}Total Score\n{1}", victoryText, scoreRecapText);
        BackboardController.Instance.ResetValue();
        BackboardController.Instance.enabled = false;
        gameTimerText.transform.parent.gameObject.SetActive(false);
        gameOverScreen.SetActive(true);

        // Reset total scores
        SceneController.Instance.SetScores(0);
        SceneController.Instance.ResetRetryTimer();
    }

    IEnumerator EvenPoints()
    {
        _totalScoreUI.text = string.Format("The score is even\nRetry for {0} seconds", 10f);
        gameOverScreen.SetActive(true);
        // Turn off replay button
        gameOverScreen.transform.GetChild(1).gameObject.SetActive(false);
        yield return new WaitForSeconds(3f);
        SceneController.Instance.StartGame();
        //_totalScoreUI.text = "";
        //startupTime = 3f;
        //_gameStarted = false;
        //UpdateTimersUI();
        //GameManager.Instance.UpdateGameState(GameManager.GameState.Play);
        //RemainingTime = 10f;
    }
}
