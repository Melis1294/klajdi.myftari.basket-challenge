using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInput : MonoBehaviour
{
    [SerializeField] private float _strength;
    [SerializeField] private float _maxStrength = 90f;
    [SerializeField] private float _remainingTime = 3f; // internal countdown to measure shot time limit
    [SerializeField] private bool _shotEnded;
    [SerializeField] private bool _shotStarted;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space) && _remainingTime == 0) RestartCountdown();

        if (_shotEnded) return;

        if (Input.GetMouseButton(0))
        {
            _shotStarted = true;
            CountDown();
            if (_remainingTime > 0)
            {
                _strength += Input.GetAxis("Mouse Y");

                if (_strength < 0) _strength = 0;
                if (_strength > _maxStrength) _strength = _maxStrength;
                return;
            } else
            {
                Debug.LogWarning("SHOOOTING AT " + _strength + " SPEED!!!");
                _shotEnded = true;
                _shotStarted = false;
                _strength = 0;
                return;
            }
        } 
        else
        {
            if (_strength == 0)
            {
                if (_shotStarted)
                {
                    RestartCountdown();
                }
            }
            else
            {
                Debug.LogWarning("SHOOOTING AT " + _strength + " SPEED!!!");
                _shotEnded = true;
                _shotStarted = false;
                _strength = 0;
                _remainingTime = 0;
            }
        }
    }

    void RestartCountdown()
    {
        _remainingTime = 3f;
        _shotEnded = false;
        _shotStarted = false;
    }

    void CountDown()
    {
        _remainingTime -= Time.deltaTime;
        if (_remainingTime < 0) _remainingTime = 0;
    }
}
