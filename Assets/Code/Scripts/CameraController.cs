using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    private float _elapsed = 2f;   // Current time of flight
    private float _duration = 2f;  // Total time of flight
    private float _arcHeight = 2f;   // Height of the parable vertex
    private Transform _cameraStart;
    private Transform _cameraEnd;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private Vector3 _adjustedEnd;
    private float _offset = 2f;
    private float _smoothSpeed = 2.8f;
    public static CameraController Instance { get; private set; }
    // To compute podium camera move
    private Quaternion _startRot;
    private Quaternion _endRot;
    float _startYaw;
    float _endYaw;
    float _startPitch;
    float _startRoll;
    private bool _gameEnded = false;

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
    }

    // Start is called before the first frame update
    public void SetupPlayerCamera(Transform cameraStartTransform)
    {
        _cameraStart = cameraStartTransform;
        _cameraEnd = GameManager.Instance.CameraTarget;
        SetupCameraMove();
        ResetCamera();
    }

    // Update is called once per frame
    void Update()
    {
        if (_elapsed < _duration)
        {
            if (_gameEnded)
                ComputePodiumCameraMove(); // take camera to players' podium
            else ComputeCameraMove(); // ball in the air
        }
    }

    void ComputeCameraMove()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        // Linear interpolation in XZ
        Vector3 horizontalPos = Vector3.Lerp(
            new Vector3(_startPos.x, 0, _startPos.z),
            new Vector3(_adjustedEnd.x, 0, _adjustedEnd.z),
            t
        );

        // Parabolic interpolation in Y
        float y = Mathf.Lerp(_startPos.y, _adjustedEnd.y, t) + _arcHeight * 4 * t * (1 - t);

        Vector3 targetPos = new Vector3(horizontalPos.x, y, horizontalPos.z);

        // Add smooth movement transition
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * _smoothSpeed);
    }

    void ComputePodiumCameraMove()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        // ---- STRAIGHT LINE POSITION ----
        transform.position = Vector3.Lerp(_startPos, _endPos, t);

        // ---- ROTATION (Y ONLY, SHORTEST PATH) ----
        float deltaYaw = Mathf.DeltaAngle(_startYaw, _endYaw);
        float currentYaw = _startYaw + deltaYaw * t;

        transform.rotation = Quaternion.Euler(
            _startPitch,
            currentYaw,
            _startRoll
        );
    }

    public void StartMoving()
    {
        SetupCameraMove();
        _elapsed = 0f;
    }

    private void SetupCameraMove()
    {
        _startPos = _cameraStart.position;
        _endPos = _cameraEnd.position;
        Vector3 dir = (_endPos - _startPos).normalized;
        _adjustedEnd = _endPos - dir * _offset;
    }

    public void ResetCamera()
    {
        // Prevent it when game ends
        if (GameManager.Instance.State == GameManager.GameState.GameOver) return;

        transform.position = _startPos;
        Vector3 direction = _endPos - _startPos;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    public void SetPodiumCamera(Transform podiumTransform)
    {
        _cameraEnd = podiumTransform;

        _startPos = transform.position;
        _endPos = _cameraEnd.position;

        _startYaw = transform.eulerAngles.y;
        _endYaw = _cameraEnd.eulerAngles.y;

        _startPitch = transform.eulerAngles.x;
        _startRoll = transform.eulerAngles.z;

        _duration = 1.5f;
        _elapsed = 0f;
        _gameEnded = true;
    }
}
