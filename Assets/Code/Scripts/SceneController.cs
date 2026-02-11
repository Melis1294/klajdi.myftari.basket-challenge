using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [SerializeField] private float _retryTimer = 60f;
    [SerializeField] private GameObject quitButton;
    public static SceneController Instance { get; private set; }
    private int _playerTotalScore;
    private int _opponentTotalScore;

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(this.gameObject);

        Time.timeScale = 1f;
        AudioListener.pause = false;

#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
            quitButton.SetActive(false);
#endif
    }

    public void SetScores(int playerScore, int opponentScore = 0)
    {
        _playerTotalScore = playerScore;
        _opponentTotalScore = opponentScore;
    }

    public int[] GetScores() => new int[] { _playerTotalScore, _opponentTotalScore };

    public float SetRetryTimer(float retryTimer) => _retryTimer = retryTimer;
    public float GetRetryTimer() => _retryTimer;
    public void ResetRetryTimer() => _retryTimer = 60f;

    // Start or restart the game
    public void StartGame() => SceneManager.LoadScene(1);

    public void BackToMainMenu() => SceneManager.LoadScene(0);


    public void QuitGame()
    {
        Application.Quit();
    }
}
