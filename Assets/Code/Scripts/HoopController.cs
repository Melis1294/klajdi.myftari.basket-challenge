using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoopController : MonoBehaviour
{
    public static HoopController instance { get; private set; }
    private string _playerTag = "Player";
    private bool _hoopEntered;
    private bool _rimWasTouched { get; set; }
    // Start is called before the first frame update
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

    public void RimWasTouched()
    {
        _rimWasTouched = true;
    }

    public void ResetState()
    {
        _hoopEntered = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_playerTag) || _hoopEntered) return;
        int points = 3;
        if (_rimWasTouched)
        { 
            Debug.LogError("2 Points!!!");
            points = 2;
        }
        else
            Debug.LogError("3 Points!!!");

        _rimWasTouched = false;
        _hoopEntered = true;
        GameManager.instance.Win(points);
    }
}
