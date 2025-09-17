using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform HoopBasket;
    public Transform ShootingZone;
    [SerializeField] private Transform mainCharacter;
    [SerializeField] int currentPosition = 0;
    [SerializeField] private GameObject ball;
    [SerializeField] private float _fallSpeed = 0.2f;
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
        startPos = ballPosition;
        endPos = HoopBasket.position;

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
            ballRb.velocity = Vector3.one * _fallSpeed;
            ballRb.useGravity = true; // hand control back to physics
        }
    }

    public void OnBallShot(float shootingSpeed)
    {
        // Compute shot logic to the score
        Debug.Log("GameManager: Ball was shot!");
        elapsed = 0;
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
    }
}
