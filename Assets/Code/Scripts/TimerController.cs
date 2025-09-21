using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerController : MonoBehaviour
{
    public static TimerController instance { get; private set; }

    [SerializeField] private float startTime;
    [SerializeField] private float remainingTime;
    [SerializeField] private bool _gameStarted;
    [SerializeField] private string formattedTime;

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

    // Update is called once per frame
    void Update()
    {
        float time = 0f;
        if (!_gameStarted)
        {
            if (startTime > 0)
            {
                startTime -= Time.deltaTime;
                time = startTime;
            }
            else if (startTime < 0)
            {
                startTime = 0;
                _gameStarted = true;
                GameManager.instance.StartGame();
            }
            return;
        }

        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            time = remainingTime;
        }
        else if (remainingTime < 0)
        {
            remainingTime = 0;
            GameManager.instance.GamOver();
        }
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
