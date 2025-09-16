using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public static BallController instance { get; private set; }
    private bool _rimIsTouched;
    private string _rimTag = "Rim";
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
        _rimIsTouched = false;
    }

    // Start is called before the first frame update
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(_groundTag))
        {
            GameManager.instance.ResetGameState();
            return;
        }

        if (!collision.collider.transform.parent.CompareTag(_rimTag) || _rimIsTouched) return;
        Debug.LogWarning("Rim touched!!!");
        _rimIsTouched = true;
        HoopController.instance.RimWasTouched();
    }
}
