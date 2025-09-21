using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public static BallController instance { get; private set; }
    private bool _hoopEntered;
    private bool _rimWasTouched;
    private bool _backboardWasTouched;
    private string _hoopTag = "Hoop";
    private string _rimTag = "Rim";
    private string _backboardTag = "Backboard";
    private string _groundTag = "Ground";

    private void Awake()
    {
        // Prevent class instance duplicates
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }
    // Start is called before the first frame update
    //void Start()
    //{
        
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    //public void ShootBall()
    //{

    //}

    //public void ResetPosition()
    //{

    //}

    public void ResetState()
    {
        _hoopEntered = false;
        _rimWasTouched = false;
        _backboardWasTouched = false;
    }

    // Start is called before the first frame update
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(_groundTag))
        {
            GameManager.instance.ResetGameState();
            return;
        }

        if (collision.collider.CompareTag(_backboardTag) && !_backboardWasTouched)
        {
            _backboardWasTouched = true;
            return;
        }

        if (!collision.collider.transform.parent.CompareTag(_rimTag) || _rimWasTouched) return;
        Debug.LogWarning("Rim touched!!!");
        _rimWasTouched = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_hoopTag) || _hoopEntered) return;
        
        int points = 3;
        if (_backboardWasTouched)
        {
            points = BackboardController.instance.GetValue();
            BackboardController.instance.ResetValue(); // Reset backboard bonus after scoring
        }
        if (_rimWasTouched)
        {
            Debug.LogError("2 Points!!!");
            points = 2;
        }
        else // clean shot!
            Debug.LogError("3 Points!!!");

        _hoopEntered = true;
        GameManager.instance.Win(points);
    }
}
