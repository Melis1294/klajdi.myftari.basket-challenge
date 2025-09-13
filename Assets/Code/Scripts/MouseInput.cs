using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInput : MonoBehaviour
{
    [SerializeField] private float _strength;
    [SerializeField] private float _maxStrength = 90f;
    [SerializeField] private float _initialTime = 1.7f; // internal countdown to measure shot time limit
    [SerializeField] private float _remainingTime;      // internal countdown to measure shot time limit
    [SerializeField] private bool _shotEnded;
    [SerializeField] private bool _shotStarted;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _remainingTime = _initialTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space) && _remainingTime == 0) RestartShot();

        if (_shotEnded) return;

        if (Input.GetMouseButton(0))
        {
            _shotStarted = true;
            CountDown();
            if (_remainingTime > 0)
            {
                ComputeStrength();
                return;
            }

            ShootAndResetParams();
            return;
        } 
        
        if (_strength == 0)
        {
            if (_shotStarted) RestartShot();
        }
        else ShootAndResetParams();
    }

    void ComputeStrength()
    {
        _strength += Input.GetAxis("Mouse Y");

        if (_strength < 0) _strength = 0;
        if (_strength > _maxStrength) _strength = _maxStrength;
    }

    void RestartShot()
    {
        _remainingTime = _initialTime;
        _shotEnded = false;
        _shotStarted = false;
    }

    void CountDown()
    {
        _remainingTime -= Time.deltaTime;
        if (_remainingTime < 0) _remainingTime = 0;
    }

    void ShootAndResetParams()
    {
        Debug.LogWarning("SHOOOTING AT " + _strength + " SPEED!!!");
        ResetParams();
    }

    void ResetParams()
    {
        _shotEnded = true;
        _shotStarted = false;
        _strength = 0;
        _remainingTime = 0;
    }
}
