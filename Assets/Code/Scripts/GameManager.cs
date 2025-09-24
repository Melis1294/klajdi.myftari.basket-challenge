using System;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public Transform HoopBasket;
    public Transform CameraTarget;
    public Transform Backboard;
    public Transform ShootingZone;
    [SerializeField] private Transform mainCharacter;
    [SerializeField] int currentPositionPlayer = 0;
    private Transform _characterInstance;
    private Transform[] _shootingZones;
    private BallController _ballInstance;
    public static GameManager Instance { get; private set; }

    public int TotalScore { get;  private set; }
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshPro scoreText;

    // Game State
    public GameState State;
    public static event Action<GameState> OnGameStateChanged;

    // Game mode
    private bool _isSinglePlayer;
    [SerializeField] private Vector3 opponentPositionOffset = Vector3.left;
    [SerializeField] int currentPositionOpponent = 0;
    [SerializeField] private TextMeshProUGUI opponentScoreText;
    public int OpponentScore { get; private set; }
    private Transform _opponentInstance;

    // Start is called before the first frame update
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

        // TODO: Manage AI palyer spawn (needs fix)
        SpawnCharacter();
        if (!_isSinglePlayer) SpawnOpponent();
    }

    // Spawn main player
    void SpawnCharacter()
    {
        _characterInstance = Instantiate(mainCharacter, _shootingZones[currentPositionPlayer].position, Quaternion.Euler(0, 180f, 0));
        if (_characterInstance)
        {
            _ballInstance = _characterInstance.GetChild(0).GetComponent<BallController>();
        }
        else
        {
            throw new NullReferenceException("Character instance not found!");
        }
    }

    void SpawnOpponent()
    {
        _opponentInstance = Instantiate(mainCharacter, _shootingZones[currentPositionOpponent].position + opponentPositionOffset, Quaternion.Euler(0, 180f, 0));
        if (_opponentInstance)
        {
            _opponentInstance.gameObject.AddComponent<AIController>();  // Attach AI script to opponent
            _opponentInstance.GetChild(0).GetComponent<BallController>().AIBall = true; // Set AI ball
            Camera cameraInstance = _opponentInstance.GetComponentInChildren<Camera>();
            if (cameraInstance)
            {
                // TODO: remove after moving camera outside character prefab
                // Disable opponent camera object and script (not needed)
                cameraInstance.enabled = false;
                cameraInstance.gameObject.SetActive(false);
            }
        }
        else
        {
            throw new NullReferenceException("Opponent instance not found!");
        }
    }

    // Called on first shot setup and in next ones, from both player and opponent
    void UpdatePosition(ref int currentPosition, Transform playerInstance)
    {
        if (currentPosition >= _shootingZones.Length) currentPosition = 0;
        Vector3 newShootingZone = _shootingZones[currentPosition].position;
        playerInstance.position = new Vector3(newShootingZone.x, 0f, newShootingZone.z);
        Vector3 direction = HoopBasket.position - playerInstance.position;
        direction.y = 0;
        playerInstance.rotation = Quaternion.LookRotation(direction);
    }

    // Compute shot based on input strength
    public void OnBallShot(float shootingSpeed) => _ballInstance.Shoot(shootingSpeed);

    // Reset game stats for next shot, for AI or player
    public void ResetGameState(bool aiState)
    {
        if (!aiState)
        {
            scoreText.gameObject.SetActive(false);
            // TODO: Move camera out of prefab
            CameraController.Instance.ResetCamera();
            UpdatePosition(ref currentPositionPlayer, _characterInstance);
            InputManager.Instance.RestartShot();
        } else
        {
            UpdatePosition(ref currentPositionOpponent, _opponentInstance);
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
            scoreText.text = string.Format("{0} points!", points);  // Show single score UI
            scoreText.gameObject.SetActive(true);
            TotalScore += points;
            totalScoreText.text = string.Format("Score: {0}", TotalScore);
            currentPositionPlayer++; // Update player position for next shot
        }
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
