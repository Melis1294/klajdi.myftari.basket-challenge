using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 1.3f;
    [SerializeField] private float _lerpTime = 6.0f;

    private Vector2 _targetTurn;                                        // where the mouse *wants* to rotate
    private Vector2 _currentTurn;                                       // what the camera is *actually* using
    private Vector2 _verticalRotationLimit = new Vector2(-90f, 90f);    // prevent camera from flipping

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // hide mouse cursor
    }

    void Update()
    {
        // only rotate if left mouse button is NOT held
        if (!Input.GetMouseButton(0))
        {
            // accumulate target rotation from mouse input
            _targetTurn.x += Input.GetAxis("Mouse X") * _sensitivity;
            _targetTurn.y += Input.GetAxis("Mouse Y") * _sensitivity;

            // clamp vertical axis (avoid flipping)
            _targetTurn.y = Mathf.Clamp(_targetTurn.y, _verticalRotationLimit.x, _verticalRotationLimit.y);
        }

        // smoothly interpolate current rotation towards target
        _currentTurn.x = Mathf.Lerp(_currentTurn.x, _targetTurn.x, _lerpTime * Time.deltaTime);
        _currentTurn.y = Mathf.Lerp(_currentTurn.y, _targetTurn.y, _lerpTime * Time.deltaTime);

        // apply rotation
        transform.localRotation = Quaternion.Euler(-_currentTurn.y, _currentTurn.x, 0);
    }
}
