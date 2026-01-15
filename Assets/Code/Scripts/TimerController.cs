using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    public static TimerController Instance { get; private set; }

    [SerializeField] private float startupDuration = 3f;
    private float _startupTime;
    public float RemainingTime;
    [SerializeField] private bool _gameStarted;
    [SerializeField] private float _timeBetweenGameEndEvents = 3.2f;
    private bool _lastSeconds = false;
    private int _currentCountDownValue = -1;

    // UI
    [SerializeField] private TextMeshProUGUI gameTimerText;
    [SerializeField] private TextMeshProUGUI startupTimerText;
    [SerializeField] private GameObject gameOverScreen;
    private TextMeshProUGUI _totalScoreUI;

    // Audio
    [SerializeField] private AudioClip start_beep;
    [SerializeField] private AudioClip ticking;
    [SerializeField] private AudioClip buzzer;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip lose;

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
        _totalScoreUI = gameOverScreen.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    // Show or hide startup and game timers accordingly
    private void Start() => UpdateTimersUI();

    // Update is called once per frame
    void Update()
    {
        if (!_gameStarted)  // Manage startup timer
        {
            _startupTime -= Time.deltaTime;

            int countdown = Mathf.CeilToInt(_startupTime);
            startupTimerText.text = string.Format("{0}", countdown);

            // Bepp only when number changes (3, 2, 1, ...)
            if (_currentCountDownValue != countdown)
            {
                GameManager.Instance.SFXManager.PlayOneShot(start_beep);
                _currentCountDownValue = countdown;
            }

            if (_startupTime <= 0)
            {
                _gameStarted = true;
                _startupTime = 1;
                UpdateTimersUI();
                GameManager.Instance.UpdateGameState(GameManager.GameState.Play);
            }
            return;
        }

        // Manage game timer
        if (RemainingTime > 0)
        {
            RemainingTime -= Time.deltaTime;

            if (RemainingTime <= 5 && !_lastSeconds)
            {
                _lastSeconds = true;
                GameManager.Instance.SFXManager.PlayOneShot(ticking);
            }
        }
        else if (RemainingTime < 0)
        {
            RemainingTime = 0;
            GameManager.Instance.UpdateGameState(GameManager.GameState.GameOver);
            GameManager.Instance.SFXManager.PlayOneShot(buzzer);
            // Reset fireball slider so the particles of the fireball
            FireballController.Instance.OnMissedShot();
            StartCoroutine(SetupGameOver());
        }
        int minutes = Mathf.FloorToInt(RemainingTime / 60);
        int seconds = Mathf.FloorToInt(RemainingTime % 60);
        gameTimerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }

    void UpdateTimersUI()
    {
        ResetStartupTimer();
        gameTimerText.transform.parent.gameObject.SetActive(_gameStarted);
        startupTimerText.enabled = !_gameStarted;
    }

    private void ResetStartupTimer()
    {
        _startupTime = startupDuration;
        _currentCountDownValue = Mathf.CeilToInt(_startupTime);
        _currentCountDownValue = -1;
    }

    // Wait until last ball is shot to show the right score
    IEnumerator SetupGameOver()
    {
        // Wait on computing the scores until last ball shots are over
        yield return new WaitUntil(() => GameManager.Instance.LastBallsAreShot());

        int playerScore = GameManager.Instance.TotalScore;
        int opponentScore = GameManager.Instance.OpponentScore;
        bool isSinglePlayer = GameManager.Instance.IsSinglePlayer;
        bool evenPoints = playerScore == opponentScore;

        GameManager.Instance.GameEnded(evenPoints);

        if (evenPoints)
        {
            yield return new WaitForSeconds(_timeBetweenGameEndEvents);
            GameManager.Instance.UpdateGameState(GameManager.GameState.Startup);
            // Reset total scores and set even points timer mode
            SceneController.Instance.SetScores(playerScore, opponentScore);
            SceneController.Instance.SetRetryTimer(10f);
            StartCoroutine(EvenPoints());
        } else
        {
            yield return new WaitForSeconds(_timeBetweenGameEndEvents);

            bool playerWins = playerScore > opponentScore;
            GameManager.Instance.ThemeManager.Stop();
            GameManager.Instance.SFXManager.PlayOneShot(playerWins ? win : lose);
            GameManager.Instance.Victory(playerWins);

            StartCoroutine(ShowScore(playerWins, isSinglePlayer, playerScore, opponentScore));
        }
    }

    // Setup Game Over UI
    IEnumerator ShowScore(bool playerWins, bool isSinglePlayer, int playerScore, int opponentScore)
    {
        yield return new WaitForSeconds(_timeBetweenGameEndEvents);
        string victoryText = isSinglePlayer ? "" : playerWins ? "You win!\n" : "You lose!\n";
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
        gameOverScreen.transform.GetChild(1).gameObject.SetActive(false);   // Turn off replay button
        yield return new WaitForSeconds(3f);
        SceneController.Instance.StartGame();
    }
}
