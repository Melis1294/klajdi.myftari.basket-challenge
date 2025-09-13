using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInput : MonoBehaviour
{
    [SerializeField] private float _strength;           // Define strength params
    [SerializeField] private float _maxStrength = 90f;
    [SerializeField] private float _initialTime = 5.7f; // Define countdown params
    [SerializeField] private float _remainingTime;
    [SerializeField] private bool _shotEnded;           // Define shot params

    void Start()
    {
        _remainingTime = _initialTime;                  // Setup countdown
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space) && _remainingTime == 0) RestartShot();

        if (_shotEnded) return;

        if (Input.GetMouseButton(0))                // When LMB down init shooting strength computation
        {
            if (ComputeStrength() == 0) return;
            /* If player puts strength init internal countdown to let him choose the strength.
             Only Y axis positive values are taken */
            CountDown();
            if (_remainingTime > 0) return;         // If player takes too much time shot is taken automatically

            ShootAndResetParams();
        } else if (_strength > 0)
            ShootAndResetParams();                  // When player releases LMB shot is taken
    }

    float ComputeStrength()
    {
        float newY = Input.GetAxis("Mouse Y");
        if (newY > 0)
        {
            _strength += newY;
            if (_strength > _maxStrength) _strength = _maxStrength;
        }
        return _strength;
    }

    void RestartShot()
    {
        _remainingTime = _initialTime;
        _shotEnded = false;
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
        _strength = 0;
        _remainingTime = 0;
    }
}
