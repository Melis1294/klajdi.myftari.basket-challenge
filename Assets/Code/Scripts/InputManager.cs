using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputManager : MonoBehaviour
{
    [SerializeField] private float maxStrength = 90f;
    [SerializeField] private float minStrength = 5f;
    [SerializeField] private float initialTime = 5.7f; // Define countdown params
    private float _strength;           // Define strength params
    private float _touchStregth;
    public bool IsShooting;           // Define shot params - if "true" prevents shooting until new shot
    private float _remainingTime;
    private bool _firstShot = true;
    #region Mouse Input
    [SerializeField] private float mosueSpeedMultiply = 2.3f;
    #endregion
    #region Touch Input
    private bool _isSwiping;
    [SerializeField] private float touchSpeedMultiply = 0.5f;
    private Vector2 _startTouchPos;
    private Vector2 _endTouchPos;
    #endregion
    #region UI
    [SerializeField] Slider slider;
    #endregion
    public static InputManager Instance { get; private set; }

    // Istantiate Singleton class
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
        // Subscribe method to state change events
        GameManager.OnGameStateChanged += GameManagerOnGameStateChanged;

        slider.value = 0;
#if UNITY_IOS || UNITY_ANDROID                      // Touch controls
        _touchStregth = 0;
#endif
    }

    // Start is called before the first frame update
    void Start()
    {
        _remainingTime = initialTime;                  // Setup countdown
        UpdateShotUI();
    }

    void Update()
    {
        if (IsShooting) return;

#if UNITY_IOS || UNITY_ANDROID                      // Touch controls
        if (Input.touchCount > 0)
        {
            if (ManageTouchInput() <= minStrength) return;
            // Start each shot countdown when player applies min srength
            CountDown();
            if (_remainingTime > 0) return;
            // Shoot the ball when shot countdown ends
            ShootAndResetParams();
            return;
        } else if (_strength > minStrength)         // Shoot the ball on touch release
            ShootAndResetParams();
#else                                               // MOuse controls
        if (Input.GetMouseButton(0))                // When LMB down init shooting strength computation
        {
            if (ManageMouseInput() <= minStrength) return;
            CountDown();
            if (_remainingTime > 0) return;
            ShootAndResetParams();
            return;
        } else if (_strength > minStrength)
            ShootAndResetParams();
#endif
    }
    // Unsubscribe method to state change events on object destroy
    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= GameManagerOnGameStateChanged;
    }

    float ManageTouchInput()
    {
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                _startTouchPos = touch.position;
                _isSwiping = true;
                break;

            case TouchPhase.Moved:
                if (_isSwiping)
                {
                    float deltaY = (touch.position.y - _startTouchPos.y) * touchSpeedMultiply;
                    _touchStregth = ComputeStrength(deltaY);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                _endTouchPos = touch.position;
                float totalDeltaY = (_endTouchPos.y - _startTouchPos.y) * touchSpeedMultiply;
                _touchStregth = ComputeStrength(totalDeltaY);
                _isSwiping = false;
                break;
        }

        return _touchStregth;
    }

    float ManageMouseInput()
    {
        float newY = Input.GetAxis("Mouse Y") * mosueSpeedMultiply;
        return ComputeStrength(newY);
    }

    float ComputeStrength(float value)
    {
        if (value > 0)      // Only positive values are taken
        {
            _strength += value;
            if (_strength > maxStrength) _strength = maxStrength;
        }

        UpdateShotUI(Mathf.Round(_strength * 100) / 100.0);

        return _strength;
    }

    // Enable input params for next shot
    public void RestartShot()
    {
        _remainingTime = initialTime;
#if UNITY_IOS || UNITY_ANDROID                      // Touch controls
        _touchStregth = 0;
#endif
        UpdateShotUI();
        IsShooting = false;
    }

    void CountDown()
    {
        _remainingTime -= Time.deltaTime;
        if (_remainingTime < 0) _remainingTime = 0;
    }

    void ShootAndResetParams()
    {
#if UNITY_WEBGL
        if (_firstShot)
        {
            GameManager.Instance.EndTutorial();
            _firstShot = false;
        }
#endif

        GameManager.Instance.OnBallShot(_strength);
        ResetParams();
    }

    // Set input disabled after shooting until next shot
    void ResetParams()
    {
        IsShooting = true;
        _strength = 0;
        _remainingTime = 0;
    }

    // Manage strength UI
    void UpdateShotUI(double strength = 0)
    {
        slider.value = (float)strength;
        Debug.LogWarning("Slider value: " + slider.value);
    }

    // Manage states change
    void GameManagerOnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver)
        {
            // Shoot the ball if player is still holdin touch/mouse down but game is over
            if (Input.GetMouseButton(0) || Input.touchCount > 0)
                ShootAndResetParams();

            slider.enabled = false;
        }
    }
}
