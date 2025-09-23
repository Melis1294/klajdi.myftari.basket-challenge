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
    [SerializeField] private float _fallSpeed = 1.8f;
    private Transform _characterInstance;
    private Transform[] _shootingZones;
    private GameObject _ballInstance;
    private Vector3 _initialBallLocalPos;
    private float _elapsed = 1.5f;
    private readonly float _duration = 1.5f; // total time of flight
    private readonly float _arcHeight = 2f;  // height of the parabola
    private Vector3 _startPos;
    private Vector3 _endPos;
    private Rigidbody _ballRb;
    public static GameManager Instance { get; private set; }

    [SerializeField] float minHoopSpeed = 40;
    [SerializeField] float maxHoopSpeed = 50;
    [SerializeField] float minBackboardSpeed = 70;
    [SerializeField] float maxBackboardSpeed = 75;
    private float _diversion = 0;
    private float _shootingSpeed;

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
    }

    void Start()
    {
        _characterInstance = Instantiate(mainCharacter, _shootingZones[currentPosition].position, Quaternion.Euler(0, 180f, 0));
        SpawnBall();
    }

    void SpawnBall()
    {
        //var ballPosition = _shootingZones[currentPosition].position;
        //ballPosition.y = ballPosition.y + 1f;
        if (!_ballInstance)
        {
            _ballInstance = _characterInstance.GetChild(0).gameObject; //Instantiate(ball, ballPosition, Quaternion.identity);
            _initialBallLocalPos = _ballInstance.transform.localPosition;

        }
        else
        {
            _ballInstance.transform.localPosition = _initialBallLocalPos;
            //_ballInstance.transform.position = ballPosition;
        }

        // Initialize positions
        _endPos = HoopBasket.position;
        _startPos = _ballInstance.transform.position; //ballPosition;

        // Setup ball physics
        _ballRb = _ballInstance.GetComponent<Rigidbody>();
        _ballRb.useGravity = false;
        _ballRb.velocity = Vector3.zero;
        _ballRb.angularVelocity = Vector3.zero;
        _ballInstance.transform.LookAt(HoopBasket.transform);
    }

    // Update is called once per frame
    void Update()
    {
        // ball in the air
        if (_elapsed < _duration) ComputeFlight();
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
        SpawnBall();
    }

    void ComputeFlight()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        // Linear interpolation in XZ
        Vector3 horizontalPos = Vector3.Lerp(
            new Vector3(_startPos.x, 0, _startPos.z),
            new Vector3(_endPos.x, 0, _endPos.z),
            t
        );

        // Parabolic interpolation in Y
        float y = Mathf.Lerp(_startPos.y, _endPos.y, t) + _arcHeight * 4 * t * (1 - t);

        Vector3 newPos = new Vector3(horizontalPos.x, y, horizontalPos.z);

        // Update rigidbody velocity
        Vector3 displacement = newPos - _ballRb.position;
        Vector3 velocity = displacement / Time.deltaTime;
        _ballRb.velocity = velocity;

        // Rolling rotation
        Vector3 moveDir = displacement.normalized;
        Vector3 rollAxis = Vector3.Cross(moveDir, Vector3.up);

        float radius = _ballInstance.transform.localScale.x * 0.5f;
        float speed = displacement.magnitude / Time.deltaTime;

        _ballRb.angularVelocity = -rollAxis * (speed / radius);

        _ballRb.MovePosition(newPos); // Rigid body position update

        // Ball reached the hoop
        if (_elapsed >= _duration)
        {
            // Update physics according to if player is aiming for the hoop or for the backboard
            _ballRb.velocity = (_shootingSpeed >= minBackboardSpeed) ? (Vector3.forward * _fallSpeed * 0.2f) : (Vector3.down * _fallSpeed);
            _ballRb.useGravity = true; // Hand control back to physics
        }
    }

    // Compute shot based on input strength
    public void OnBallShot(float shootingSpeed)
    {
        _shootingSpeed = shootingSpeed;
        UpdateTarget(shootingSpeed);
        _elapsed = 0;
    }

    // Compute shot logic in relation to right values for hoop and backboard
    private void UpdateTarget(float shootingSpeed)
    {
        _diversion = 0;
        bool isHoopSpeed = (shootingSpeed >= minHoopSpeed && shootingSpeed <= maxHoopSpeed);
        bool isBackboardSpeed = (shootingSpeed >= minBackboardSpeed && shootingSpeed <= maxBackboardSpeed);
        
        if (isBackboardSpeed) 
        { 
            _endPos = Backboard.position;
        } else if (isHoopSpeed)
        {
            _endPos = HoopBasket.position;
        }
        else if (shootingSpeed > maxBackboardSpeed)
        {
            _endPos = Backboard.position;
            _diversion = 0.8f;
        } else
        {
            bool isAlmostHoopSpeed = (shootingSpeed > maxHoopSpeed && (shootingSpeed - maxHoopSpeed) <= 5f)
                || (shootingSpeed < minHoopSpeed && (minHoopSpeed - shootingSpeed) <= 5f);
            _diversion = isAlmostHoopSpeed ? 0.2f : 0.8f;
        }

        if (_diversion == 0) return;

        int sign = UnityEngine.Random.value < 0.5f ? -1 : 1;
        int axis = UnityEngine.Random.value < 0.5f ? -1 : 1;
        if (axis == -1) _endPos.x += (_diversion * sign);
        if (axis == 1) _endPos.z += (_diversion * sign);
    }

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
