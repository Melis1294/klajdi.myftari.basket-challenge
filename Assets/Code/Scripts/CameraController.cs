using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform cameraStart;

    private float _elapsed = 2f;   // Current time of flight
    private float _duration = 2f;  // Total time of flight
    private float _arcHeight = 2f;   // Height of the parable vertex
    private Transform _cameraEnd;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private Vector3 _adjustedEnd;
    private float _offset = 2f;
    private float _smoothSpeed = 2.8f;
    public static CameraController Instance { get; private set; }

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
        _cameraEnd = GameManager.Instance.CameraTarget;
    }

    // Start is called before the first frame update
    void Start()
    {
        SetupCameraMove();
        ResetCamera();
    }

    // Update is called once per frame
    void Update()
    {
        // ball in the air
        if (_elapsed < _duration) ComputeCameraMove();
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
        transform.position = Vector3.Slerp(transform.position, targetPos, Time.deltaTime * _smoothSpeed);
    }

    public void StartMoving()
    {
        SetupCameraMove();
        _elapsed = 0f;
    }

    private void SetupCameraMove()
    {
        _startPos = cameraStart.position;
        _endPos = _cameraEnd.position;
        Vector3 dir = (_endPos - _startPos).normalized;
        _adjustedEnd = _endPos - dir * _offset;
    }

    public void ResetCamera()
    {
        transform.position = _startPos;
    }
}
