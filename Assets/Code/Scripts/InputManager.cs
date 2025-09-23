using UnityEngine;
using TMPro;

public class InputManager : MonoBehaviour
{
    [SerializeField] private float maxStrength = 90f;
    [SerializeField] private float minStrength = 5f;
    [SerializeField] private float initialTime = 5.7f; // Define countdown params
    private float _strength;           // Define strength params
    private bool _shotEnded;           // Define shot params
    private float _remainingTime;
    #region Mouse Input
    [SerializeField] private float mosueSpeedMultiply = 2.3f;
    #endregion
    #region Touch Input
    private bool _isSwiping;
    [SerializeField] private float touchSpeedMultiply = 0.002f;
    private Vector2 _startTouchPos;
    private Vector2 _endTouchPos;
    #endregion
    #region UI
    [SerializeField] TextMeshProUGUI strengthText;
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
    }

    // Start is called before the first frame update
    void Start()
    {
        _remainingTime = initialTime;                  // Setup countdown
        UpdateShotUI();
    }

    void Update()
    {
        if (_shotEnded) return;

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
                    float deltaY = (touch.position.y - _startTouchPos.y) * touchSpeedMultiply;
                    totalStrength = ComputeStrength(deltaY);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                _endTouchPos = touch.position;
                float totalDeltaY = (_endTouchPos.y - _startTouchPos.y) * touchSpeedMultiply;
                totalStrength = ComputeStrength(totalDeltaY);
                _isSwiping = false;
                break;
        }

        return totalStrength;
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
        GameManager.Instance.OnBallShot(_strength);
        CameraController.Instance.StartMoving();
        ResetParams();
    }

    // Set input disabled after shooting until next shot
    void ResetParams()
    {
        _shotEnded = true;
        _strength = 0;
        _remainingTime = 0;
    }

    // Manage strength UI
    void UpdateShotUI(double strength = 0)
    {
        strengthText.text = string.Format("70-75\n40-50\n{0}", strength);
    }

    // Manage states change
    void GameManagerOnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver)
        {
            // Shoot the ball if player is still holdin touch/mouse down but game is over
            if (Input.GetMouseButton(0) || Input.touchCount > 0)
                ShootAndResetParams();

            strengthText.enabled = false;
        }
    }
}
