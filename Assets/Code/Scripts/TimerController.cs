using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    public static TimerController instance { get; private set; }

    [SerializeField] private float startupTime;
    [SerializeField] private float remainingTime;
    [SerializeField] private bool _gameStarted;
    [SerializeField] private TextMeshProUGUI gameTimerText;
    [SerializeField] private TextMeshProUGUI startupTimerText;

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

    private void Start() => UpdateTimersUI();

    // Update is called once per frame
    void Update()
    {
        if (!_gameStarted)
        {
            int countdown = Mathf.FloorToInt(startupTime % 60);
            startupTimerText.text = string.Format("{0}", countdown);
            startupTime -= Time.deltaTime;
            if (startupTime <= 1)
            {
                _gameStarted = true;
                startupTime = 1;
                UpdateTimersUI();
                GameManager.instance.StartGame();
            }
            return;
        }

        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
        }
        else if (remainingTime < 0)
        {
            remainingTime = 0;
            GameManager.instance.GamOver();
        }
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        gameTimerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }

    void UpdateTimersUI()
    {
        gameTimerText.transform.parent.gameObject.SetActive(_gameStarted);
        startupTimerText.enabled = !_gameStarted;
    }
}
