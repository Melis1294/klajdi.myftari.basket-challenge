using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    private bool _hoopEntered;
    private bool _rimWasTouched;
    private bool _backboardWasTouched;
    private string _hoopTag = "Hoop";
    private string _rimTag = "Rim";
    private string _backboardTag = "Backboard";
    private string _groundTag = "Ground";
    public static BallController Instance { get; private set; }

    private void Awake()
    {
        // Prevent class instance duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void ResetState()
    {
        _hoopEntered = false;
        _rimWasTouched = false;
        _backboardWasTouched = false;
    }

    // Manage collisions with ground, rim and backboard
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(_groundTag))
        {
            if (GameManager.Instance.State == GameManager.GameState.GameOver)
            {
                // Last shot completed
                TimerController.Instance.GameOver();
                return;
            }

            // Prepare next shot if game still playing
            GameManager.Instance.ResetGameState();
            return;
        }

        if (collision.collider.CompareTag(_backboardTag) && !_backboardWasTouched)
        {
            _backboardWasTouched = true;
            return;
        }

        if (!collision.collider.transform.parent.CompareTag(_rimTag) || _rimWasTouched) return;
        _rimWasTouched = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_hoopTag) || _hoopEntered) return;
        
        // Manage score cases if shot was scored
        int points = 3;
        if (_backboardWasTouched)
        {
            points = BackboardController.Instance.GetValue();
            BackboardController.Instance.ResetValue(); // Reset backboard bonus after scoring
        }
        else if (_rimWasTouched) points = 2;

        _hoopEntered = true;
        GameManager.Instance.Win(points);
    }
}
