using System.Collections;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    private float _elapsed = 2f;   // Current time of flight
    private float _duration = 2f;  // Total time of flight
    private float _arcHeight = 2f;   // Height of the parable vertex
    private Transform _cameraHolder;
    private Transform _cameraStart;
    private Transform _cameraEnd;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private Vector3 _adjustedEnd;
    private float _offset = 2f;
    private float _smoothSpeed = 2.8f;
    public static CameraController Instance { get; private set; }
    // To compute podium camera move
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

        _cameraHolder = transform.parent;
    }

    public void SetupPlayerCamera(Transform cameraStartTransform, Transform cameraTarget)
    {
        _cameraStart = cameraStartTransform;
        _cameraEnd = cameraTarget;
        SetupCameraMove();
        ResetCamera();
    }

    void Update()
    {
        if (_elapsed < _duration)
        {
            if (_gameEnded)
                ComputePodiumCameraMove(); // take camera to players' podium
            else
                ComputeCameraMove(); // ball in the air
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
        _cameraHolder.position = Vector3.Lerp(_cameraHolder.position, targetPos, Time.deltaTime * _smoothSpeed);
    }

    void ComputePodiumCameraMove()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        // ---- STRAIGHT LINE POSITION ----
        _cameraHolder.position = Vector3.Lerp(_startPos, _endPos, t);

        // ---- ROTATION (Y ONLY, SHORTEST PATH) ----
        float deltaYaw = Mathf.DeltaAngle(_startYaw, _endYaw);
        float currentYaw = _startYaw + deltaYaw * t;

        _cameraHolder.rotation = Quaternion.Euler(
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
        if (GameManager.GameIsOver()) return;

        _startPos = _cameraStart.position;
        _endPos = _cameraEnd.position;
        Vector3 dir = (_endPos - _startPos).normalized;
        _adjustedEnd = _endPos - dir * _offset;
    }

    public void ResetCamera()
    {
        // Prevent it when game ends
        if (GameManager.Instance.State == GameManager.GameState.GameOver) return;

        _cameraHolder.position = _startPos;
        Vector3 direction = _endPos - _startPos;
        direction.y = 0;
        _cameraHolder.rotation = Quaternion.LookRotation(direction);
    }

    public void SetPodiumCamera(Transform podiumTransform)
    {
        _cameraEnd = podiumTransform;

        _startPos = _cameraHolder.position;
        _endPos = _cameraEnd.position;

        _startYaw = _cameraHolder.eulerAngles.y;
        _endYaw = _cameraEnd.eulerAngles.y;

        _startPitch = _cameraHolder.eulerAngles.x;
        _startRoll = _cameraHolder.eulerAngles.z;

        _duration = 0.6f;
        _elapsed = 0f;
        _gameEnded = true;
    }

    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;

        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(x, y, originalPos.z);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
