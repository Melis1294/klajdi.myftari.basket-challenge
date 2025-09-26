using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public Transform HoopBasket;
    public Transform CameraTarget;
    public Transform Backboard;
    public Transform ShootingZone;
    [SerializeField] private Transform mainCharacter;
    [SerializeField] int currentPositionPlayer = 0;
    [SerializeField] private GameObject[] balls;
    private Transform _characterInstance;
    private Transform[] _shootingZones;
    private BallController _ballInstance;
    private BallController _opponentBallInstance;

    public int TotalScore { get;  private set; }

    // UI
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshPro scoreText;
    [SerializeField] private Button endGameButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    // Game State
    public GameState State;
    public static event Action<GameState> OnGameStateChanged;

    // Game mode
    public bool IsSinglePlayer;
    private Vector3 _opponentPositionOffset = new Vector3(-0.5f, 0, 0);
    [SerializeField] int currentPositionOpponent = 0;
    [SerializeField] private TextMeshProUGUI opponentScoreText;
    public int OpponentScore { get; private set; }
    private Transform _opponentInstance;
    

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Prevent class instance duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            Instance = this;
        }
        UpdateGameState(GameState.Startup);

        // Get shooting zones
        int childCount = ShootingZone.childCount;
        _shootingZones = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            _shootingZones[i] = ShootingZone.GetChild(i);
        }

        // Setup Buttons
        endGameButton.onClick.AddListener(() => SceneController.Instance.BackToMainMenu());
        retryButton.onClick.AddListener(() => SceneController.Instance.StartGame());
        menuButton.onClick.AddListener(() => SceneController.Instance.BackToMainMenu());
    }

    private void Start()
    {
        SpawnCharacter();
        int[] totalScores = SceneController.Instance.GetScores();
        TotalScore = totalScores[0];
        totalScoreText.text = string.Format("Score: {0}", TotalScore);
        if (!IsSinglePlayer) {
            OpponentScore = totalScores[1];
            opponentScoreText.text = string.Format("AI Score: {0}", OpponentScore);
            SpawnOpponent(); 
        }
        opponentScoreText.gameObject.SetActive(!IsSinglePlayer);
    }

    // Spawn main player
    void SpawnCharacter()
    {
        _characterInstance = Instantiate(mainCharacter, _shootingZones[currentPositionPlayer].position, Quaternion.Euler(0, 180f, 0));
        if (_characterInstance)
        {
            CameraController.Instance.SetupPlayerCamera(_characterInstance.GetChild(1).transform);
            Transform ballStart = _characterInstance.transform.GetChild(0).transform;
            _ballInstance = Instantiate(balls[0], ballStart.position, Quaternion.identity).GetComponent<BallController>();
            _ballInstance.BallStart = ballStart;
            _ballInstance.ResetState();
        }
        else
        {
            throw new NullReferenceException("Character instance not found!");
        }
    }

    // TODO: Fix initial rotation towards hoop
    void SpawnOpponent()
    {
        _opponentInstance = Instantiate(mainCharacter, _shootingZones[currentPositionOpponent].position + _opponentPositionOffset, Quaternion.Euler(0, 180f, 0));
        if (_opponentInstance)
        {
            //_opponentInstance.gameObject.GetComponent<PlayerController>().enabled = false;
            Transform ballStart = _opponentInstance.transform.GetChild(0).transform;
            _opponentBallInstance = Instantiate(balls[1], ballStart.position, Quaternion.identity).GetComponent<BallController>();

            _opponentInstance.gameObject.AddComponent<AIController>().BallInstance = _opponentBallInstance;  // Attach AI script to opponent
            _opponentBallInstance.transform.SetParent(_opponentInstance); // Set AI ball to AI character
            _opponentBallInstance.AIBall = true; // Set AI ball
            _opponentBallInstance.BallStart = ballStart;
            _opponentBallInstance.ResetState();
        }
        else
        {
            throw new NullReferenceException("Opponent instance not found!");
        }
    }

    // Called on first shot setup and in next ones, from both player and opponent
    void UpdatePosition(ref int currentPosition, Transform playerInstance, Vector3 offset)
    {
        if (currentPosition >= _shootingZones.Length) currentPosition = 0;
        Vector3 newShootingZone = _shootingZones[currentPosition].position;
        playerInstance.position = new Vector3(newShootingZone.x, 0f, newShootingZone.z) + offset;
        Vector3 direction = HoopBasket.position - playerInstance.position;
        direction.y = 0;
        playerInstance.rotation = Quaternion.LookRotation(direction);
    }

    // Compute shot based on input strength
    public void OnBallShot(float shootingSpeed) => _ballInstance.Shoot(shootingSpeed);

    // Reset game stats for next shot, for AI or player
    public void ResetGameState(bool aiState = false)
    {
        if (!aiState)
        {
            scoreText.gameObject.SetActive(false);
            UpdatePosition(ref currentPositionPlayer, _characterInstance, Vector3.zero);
            CameraController.Instance.SetupPlayerCamera(_characterInstance.GetChild(1).transform);
            InputManager.Instance.RestartShot();
        } else
        {
            UpdatePosition(ref currentPositionOpponent, _opponentInstance, _opponentPositionOffset);
        }
    }

    // Called on shot succeeded, for AI and player
    public void Win(int points, bool aiWon)
    {
        if (aiWon)
        {
            OpponentScore += points;
            opponentScoreText.text = string.Format("AI Score: {0}", OpponentScore);
            currentPositionOpponent++;  // Update opponent position for next shot
        } else
        {
            points *= FireballController.Instance.FireballMultiplier;
            scoreText.text = string.Format("{0} points!", points);  // Show single score UI (only player)
            scoreText.gameObject.SetActive(true);
            TotalScore += points;
            totalScoreText.text = string.Format("Score: {0}", TotalScore);
            currentPositionPlayer++; // Update player position for next shot

            // Manage fireball mode
            FireballController.Instance.AddScore((float)points / 8);
        }
    }

    public void Lose(bool aiLost)
    {
        if (aiLost) return;
        FireballController.Instance.OnMissedShot();   // Set Fireball counter to zero if 1 shot missed
        ResetGameState();   // Reset player state for next shot
    }

    

    // Manage game states
    public void UpdateGameState(GameState newState)
    {
        switch (newState)
        {
            case GameState.Startup:
                break;
            case GameState.Play:
                break;
            case GameState.Pause:
                break;
            case GameState.GameOver:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        State = newState;
        InputManager.Instance.enabled = State == GameState.Play;
        OnGameStateChanged?.Invoke(newState);
    }

    public enum GameState
    {
        Startup,
        Play,
        Pause,
        GameOver
    }
}
