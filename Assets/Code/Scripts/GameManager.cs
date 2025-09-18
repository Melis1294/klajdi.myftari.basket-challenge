using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform HoopBasket;
    public Transform Backboard;
    public Transform ShootingZone;
    [SerializeField] private Transform mainCharacter;
    [SerializeField] int currentPosition = 0;
    [SerializeField] private GameObject ball;
    [SerializeField] private float _fallSpeed = 1.8f; //0.2f;
    private Transform _characterInstance;
    private Transform[] _shootingZones;
    private GameObject _ballInstance;
    private float elapsed = 1.5f;
    private float duration = 1.5f; // total time of flight
    private float arcHeight = 2f;  // height of the parabola
    private Vector3 startPos;
    private Vector3 endPos;
    private Rigidbody ballRb;
    public static GameManager instance { get; private set; }
    private bool _canStartGame;

    [SerializeField] float minHoopSpeed = 40;
    [SerializeField] float maxHoopSpeed = 50;
    [SerializeField] float minBackboardSpeed = 70;
    [SerializeField] float maxBackboardSpeed = 75;
    private float _diversion = 0;
    private float _shootingSpeed;
    //private int _backboardScore = 8;

    [SerializeField] private float totalScore;

    // Start is called before the first frame update
    private void Awake()
    {
        // Prevent class instance duplicates
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            instance = this;
        }

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
        //HoopBasket = Backboard;
        //arcHeight = 1.5f;
        SpawnBall();
    }

    void SpawnBall()
    {
        var ballPosition = _shootingZones[currentPosition].position;
        ballPosition.y = ballPosition.y + 1f;
        if (!_ballInstance)
        {
            _ballInstance = Instantiate(ball, ballPosition, Quaternion.identity);
        } else
        {
            _ballInstance.transform.position = ballPosition;
        }

        // Initialize positions
        endPos = HoopBasket.position;
        startPos = ballPosition;

        // Midpoint raised in Y for arc
        ballRb = _ballInstance.GetComponent<Rigidbody>();
        ballRb.useGravity = false;
        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        _ballInstance.transform.LookAt(HoopBasket.transform);
    }

    // Update is called once per frame
    void Update()
    {
        // Manage player spawn among shooting zones
        //if (Input.GetKeyUp(KeyCode.Space)) elapsed = 0;

        //if (Input.GetKeyUp(KeyCode.LeftControl)) UpdatePosition();

        // ball in the air
        if (elapsed < duration) ComputeFlight();
    }

    void UpdatePosition()
    {
        currentPosition++;
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
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // Linear interpolation in XZ
        Vector3 horizontalPos = Vector3.Lerp(
            new Vector3(startPos.x, 0, startPos.z),
            new Vector3(endPos.x, 0, endPos.z),
            t
        );

        // Parabolic interpolation in Y
        float y = Mathf.Lerp(startPos.y, endPos.y, t) + arcHeight * 4 * t * (1 - t);

        Vector3 newPos = new Vector3(horizontalPos.x, y, horizontalPos.z);

        // Update rigidbody velocity
        Vector3 displacement = newPos - ballRb.position;
        Vector3 velocity = displacement / Time.deltaTime;
        ballRb.velocity = velocity;

        // Rolling rotation
        Vector3 moveDir = displacement.normalized;
        Vector3 rollAxis = Vector3.Cross(moveDir, Vector3.up);

        float radius = _ballInstance.transform.localScale.x * 0.5f;
        float speed = displacement.magnitude / Time.deltaTime;

        ballRb.angularVelocity = -rollAxis * (speed / radius);

        ballRb.MovePosition(newPos); // Rigid body position update

        // Ball reached the hoop
        if (elapsed >= duration)
        {
            ballRb.velocity = (_shootingSpeed >= minBackboardSpeed) ? (Vector3.forward * _fallSpeed) : (Vector3.down * _fallSpeed); // Vector3.forward * _fallSpeed; //Vector3.one * _fallSpeed;
            ballRb.useGravity = true; // hand control back to physics
        }
    }

    public void OnBallShot(float shootingSpeed)
    {
        // Compute shot logic to the score
        Debug.Log("GameManager: Ball was shot at the speed: " + shootingSpeed);
        _shootingSpeed = shootingSpeed;
        UpdateTarget(shootingSpeed);
        elapsed = 0;
    }

    private void UpdateTarget(float shootingSpeed)
    {
        _diversion = 0;
        bool isHoopSpeed = (shootingSpeed >= minHoopSpeed && shootingSpeed <= maxHoopSpeed);
        bool isBackboardSpeed = (shootingSpeed >= minBackboardSpeed && shootingSpeed <= maxBackboardSpeed);
        
        if (isBackboardSpeed) 
        { 
            endPos = Backboard.position;
        } else if (isHoopSpeed)
        {
            endPos = HoopBasket.position;
        }
        else if (shootingSpeed > maxBackboardSpeed)
        {
            endPos = Backboard.position;
            _diversion = 0.8f;
        } else
        {
            bool isAlmostHoopSpeed = (shootingSpeed > maxHoopSpeed && (shootingSpeed - maxHoopSpeed) <= 5f)
                || (shootingSpeed < minHoopSpeed && (minHoopSpeed - shootingSpeed) <= 5f);
            _diversion = isAlmostHoopSpeed ? 0.2f : 0.8f;
        }

        if (_diversion == 0) return;

        int sign = Random.value < 0.5f ? -1 : 1;
        int axis = Random.value < 0.5f ? -1 : 1;
        if (axis == -1) endPos.x += (_diversion * sign);
        if (axis == 1) endPos.z += (_diversion * sign);
    }

    public void ResetGameState()
    {
        Debug.LogWarning("Reset!!");
        CameraController.instance.ResetCamera();
        UpdatePosition();
        InputManager.instance.RestartShot();
        BallController.instance.ResetState();
    }

    public void Win(int points)
    {
        Debug.LogError("Payer scored " + points + " points!");
        totalScore += points;
    }

    public void GamOver()
    {
        // TODO: manage last shot score
        _canStartGame = false;
        Debug.LogError("Game Over!");
        Debug.LogError("Total score: " + totalScore);
    }

    public void StartGame()
    {
        _canStartGame = true;
        Debug.LogError("Start!!!");
    }

    public bool CanStartGame()
    {
        return _canStartGame;
    }

    //public int GetBackboardScore()
    //{
    //    return _backboardScore;
    //}
}
