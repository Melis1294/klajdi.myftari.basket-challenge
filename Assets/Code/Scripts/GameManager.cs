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
    [SerializeField] int currentPosition = 0;
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

        _characterInstance = Instantiate(mainCharacter, _shootingZones[currentPosition].position, Quaternion.Euler(0, 180f, 0));
        if (_characterInstance)
        {
            _ballInstance = _characterInstance.GetChild(0).GetComponent<BallController>();
        }
        else
        {
            throw new NullReferenceException("Character instance not found!");
        }
    }

    // Called on first shot setup and in next ones
    void UpdatePosition()
    {
        if (currentPosition >= _shootingZones.Length) currentPosition = 0;
        Vector3 newShootingZone = _shootingZones[currentPosition].position;
        _characterInstance.position = new Vector3(newShootingZone.x, 0f, newShootingZone.z);
        Vector3 direction = HoopBasket.position - _characterInstance.position;
        direction.y = 0;
        _characterInstance.rotation = Quaternion.LookRotation(direction);
        //_ballInstance.ResetState();
    }

    // Compute shot based on input strength
    public void OnBallShot(float shootingSpeed) => _ballInstance.Shoot(shootingSpeed);

    // Reset game stats for next shot
    public void ResetGameState()
    {
        scoreText.gameObject.SetActive(false);
        CameraController.Instance.ResetCamera();
        UpdatePosition();
        InputManager.Instance.RestartShot();
    }

    // Called on shot succeeded
    public void Win(int points)
    {
        scoreText.text = string.Format("{0} points!", points);
        scoreText.gameObject.SetActive(true);
        TotalScore += points;
        totalScoreText.text = string.Format("Score: {0}", TotalScore);
        currentPosition++; // Update player position for next shot
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
