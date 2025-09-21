using UnityEngine;
using TMPro;

public class InputManager : MonoBehaviour
{
    [SerializeField] private float _strength;           // Define strength params
    [SerializeField] private float _maxStrength = 90f;
    [SerializeField] private float _initialTime = 5.7f; // Define countdown params
    [SerializeField] private float _remainingTime;
    [SerializeField] private bool _shotEnded;           // Define shot params
    #region Touch Input
    [SerializeField] private bool _isSwiping;
    [SerializeField] private float _touchSpeedMultiply = 0.002f;
    [SerializeField] private float _mosueSpeedMultiply = 2.3f;
    private Vector2 _startTouchPos;
    private Vector2 _endTouchPos;
    #endregion
    #region UI
    [SerializeField] TextMeshProUGUI strengthText;
    #endregion
    public static InputManager instance { get; private set; }

    // Istantiate Singleton class
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
        GameManager.OnGameStateChanged += GameManagerOnGameStateChanged;
    }

    // Start is called before the first frame update
    void Start()
    {
        _remainingTime = _initialTime;                  // Setup countdown
        UpdateShotUI();
    }

    void Update()
    {
#if UNITY_IOS || UNITY_ANDROID
        //if (Input.touchCount == 2 && _remainingTime == 0) RestartShot();
#else
        //if (Input.GetKeyUp(KeyCode.Space) && _remainingTime == 0) RestartShot();
#endif

        if (_shotEnded) return;

#if UNITY_IOS || UNITY_ANDROID                      // Touch controls
        if (Input.touchCount > 0)
        {
            if (ManageTouchInput() == 0) return;
            CountDown();
            if (_remainingTime > 0) return;
            ShootAndResetParams();
            return;
        } else if (_strength > 0)
            ShootAndResetParams();
#else                                               // MOuse controls
        if (Input.GetMouseButton(0))                // When LMB down init shooting strength computation
        {
            if (ManageMouseInput() == 0) return;
            CountDown();
            if (_remainingTime > 0) return;
            ShootAndResetParams();
            return;
        } else if (_strength > 0)
            ShootAndResetParams();
#endif
    }
    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= GameManagerOnGameStateChanged;
    }

    float ManageTouchInput()
    {
        Touch touch = Input.GetTouch(0);
        float totalStrength = 0;

        switch (touch.phase)
        {
            case TouchPhase.Began:
                _startTouchPos = touch.position;
                _isSwiping = true;
                break;

            case TouchPhase.Moved:
                if (_isSwiping)
                {
                    float deltaY = (touch.position.y - _startTouchPos.y) * _touchSpeedMultiply;
                    Debug.Log("Delta Y: " + deltaY);
                    ComputeStrength(deltaY);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                _endTouchPos = touch.position;
                float totalDeltaY = (_endTouchPos.y - _startTouchPos.y) * _touchSpeedMultiply;
                Debug.Log("Total swipe Delta Y: " + totalDeltaY);
                totalStrength = ComputeStrength(totalDeltaY);
                _isSwiping = false;
                break;
        }

        return totalStrength;
    }

    float ManageMouseInput()
    {
        float newY = Input.GetAxis("Mouse Y") * _mosueSpeedMultiply;
        return ComputeStrength(newY);
    }

    float ComputeStrength(float value)
    {
        if (value > 0)
        {
            _strength += value;
            if (_strength > _maxStrength) _strength = _maxStrength;
        }

        UpdateShotUI(Mathf.Round(_strength * 100) / 100.0);

        return _strength;
    }

    public void RestartShot()
    {
        _remainingTime = _initialTime;
        UpdateShotUI();
        _shotEnded = false;
    }

    void CountDown()
    {
        _remainingTime -= Time.deltaTime;
        if (_remainingTime < 0) _remainingTime = 0;
    }

    void ShootAndResetParams()
    {
        GameManager.instance.OnBallShot(_strength);
        CameraController.instance.StartMoving();
        ResetParams();
    }

    void ResetParams()
    {
        _shotEnded = true;
        _strength = 0;
        _remainingTime = 0;
    }

    void UpdateShotUI(double strength = 0)
    {
        strengthText.text = string.Format("70-75\n40-50\n{0}", strength);
    }
    void GameManagerOnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver)
        {
            if (Input.GetMouseButton(0) || Input.touchCount > 0)
                ShootAndResetParams();

            strengthText.enabled = false;
        }
    }
}
