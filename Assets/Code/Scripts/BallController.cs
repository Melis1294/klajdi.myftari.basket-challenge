using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BallController : MonoBehaviour
{
    public Transform BallStart;

    private bool _hoopEntered;
    private bool _rimWasTouched;
    private bool _backboardWasTouched;
    private bool _groundWasTouched;
    private bool _isFlying;
    private string _hoopTag = "Hoop";
    private string _rimTag = "Rim";
    private string _backboardTag = "Backboard";
    private string _groundTag = "Ground";

    [SerializeField] private float fallSpeed = 1.8f;
    private float _strengthMultiplier = 1f;

    [Header("Launch tuning (input -> physical)")]
    [SerializeField] private float launchSpeedScale = 0.12f; // legacy fallback
    [SerializeField] private float minLaunchSpeed = 3f;
    [SerializeField] private float maxLaunchSpeed = 20f;

    [Header("Hoop tuning")]
    [Range(0f, 1f)]
    [SerializeField] private float hoopArcPreference = 0.9f;
    [SerializeField] private float hoopLaunchScale = 0.15f;
    [SerializeField] private float hoopMinLaunch = 3f;
    [SerializeField] private float hoopMaxLaunch = 10f;

    [Header("Backboard tuning")]
    [Range(0f, 1f)]
    [SerializeField] private float backboardArcPreference = 0.65f;
    [SerializeField] private float backboardLaunchScale = 0.11f;
    [SerializeField] private float backboardMinLaunch = 3f;
    [SerializeField] private float backboardMaxLaunch = 7f;

    [Header("Debug")]
    [SerializeField] private bool debugLaunch = true;
    [SerializeField, Tooltip("Computed apex (vertex) of the last computed trajectory; updated at each shot")]
    private Vector3 computedApex = Vector3.zero;

    private float _elapsed = 1.5f;
    private readonly float _duration = 1.5f; // total time of flight (unused for physics-driven shot)
    private readonly float _arcHeight = 2f;  // height of the parabola (unused for physics-driven shot)
    private Vector3 _startPos;
    private Vector3 _endPos;
    private Rigidbody _ballRb;
    private Transform _playerHand;

    [SerializeField] float minHoopSpeed = 40;
    [SerializeField] float maxHoopSpeed = 50;
    [SerializeField] float minBackboardSpeed = 70;
    [SerializeField] float maxBackboardSpeed = 75;
    private float _diversion = 0;
    private float _shootingSpeed;
    private GameObject _fireTrails; // Particle System for the fireball

    // Event to notify AI that he has the ball again
    public bool AIBall;
    private Collider _ballCollider;
    public AIController Opponent;

    // Audio
    [SerializeField] private AudioClip bounce;
    [SerializeField] private AudioClip hoop;
    [SerializeField] private AudioClip woosh;
    [SerializeField] private AudioClip rim;
    [SerializeField] private AudioClip backboard;
    [SerializeField] private AudioClip dribble;

    // Dribble
    [SerializeField] private float dribbleHeight = 0.8f;
    [SerializeField] private float dribbleSpeed = 5f;
    private float _prevYOffset;
    private bool _goingDown;
    private float _dribbleTime;

    // Camera shake
    [SerializeField] private float cameraShakeDuration = .3f;
    [SerializeField] private float cameraShakeMagintude = .45f;

    // Events
    public event Action LastBallShot;

    // Shooting zones
    private ShootingZoneConfig _zoneConfig;
    private Transform _zoneBackboardTarget;

    private enum BallState
    {
        Dribbling,
        Held,
        Shooting,
        GameOver
    }

    private BallState _state;


    enum ShotType
    {
        Backboard,
        Rim,
        Hoop
    }

    // Which target chosen by UpdateTarget (used for tuning)
    private bool _targetIsBackboard = false;

    private void Awake()
    {
        _ballRb = GetComponent<Rigidbody>();
        _ballCollider = GetComponent<Collider>();

        // Subscribe method to state change events
        GameManager.OnGameStateChanged += EndGame;
    }

    private void OnDestroy()
    {
        // Subscribe method to state change events
        GameManager.OnGameStateChanged -= EndGame;
    }

    private void LateUpdate()
    {
        if (_state == BallState.Held) transform.position = _playerHand.position;
    }

    /// <summary>
    /// Call after the ball instance to apply the config of the shooting zone
    /// </summary>
    public void ApplyZoneConfig(ShootingZoneConfig config, Transform backboardTarget)
    {
        _zoneConfig = config;
        _zoneBackboardTarget = backboardTarget;
    }

    void FixedUpdate()
    {
        //if (_state == BallState.Shooting)
        //{
        //    if (_elapsed < _duration)
        //        ComputeFlight();
        //    return;
        //} else 
        //if (_state == BallState.Dribbling)
        //{
        //    Dribble();
        //}

        // For physics-driven flight we don't move the ball manually while shooting.
        if (_state == BallState.Dribbling)
        {
            Dribble();
        }

        if (!AIBall && _fireTrails != null)
            _fireTrails.SetActive(FireballController.Instance.FireballMultiplier == 2);
    }

    private void Start()
    {
        if (!AIBall) _fireTrails = transform.GetChild(0).gameObject;
        //PhysicsMethods
        //Predict();
    }

    void ComputeFlight()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        // Linear interpolation in XZ
        Vector3 horizontalPos = Vector3.Lerp(
            new Vector3(_startPos.x, 0, _startPos.z),
            new Vector3(_endPos.x, 0, _endPos.z),
            t
        );

        // Parabolic interpolation in Y
        float y = Mathf.Lerp(_startPos.y, _endPos.y, t) + _arcHeight * 4 * t * (1 - t);

        Vector3 newPos = new Vector3(horizontalPos.x, y, horizontalPos.z);

        // Update rigidbody velocity
        Vector3 displacement = newPos - _ballRb.position;
        Vector3 velocity = displacement / Time.deltaTime;
        _ballRb.velocity = velocity;

        // Rolling rotation
        Vector3 moveDir = displacement.normalized;
        Vector3 rollAxis = Vector3.Cross(moveDir, Vector3.up);

        float radius = transform.localScale.x * 0.5f;
        float speed = displacement.magnitude / Time.deltaTime;

        _ballRb.angularVelocity = -rollAxis * (speed / radius);

        _ballRb.MovePosition(newPos); // Rigid body position update

        // Ball reached the hoop
        if (_elapsed >= _duration)
        {
            // Update physics according to if player is aiming for the hoop or for the backboard
            _ballRb.isKinematic = false;
            _ballRb.velocity = (_shootingSpeed >= minBackboardSpeed) ? (Vector3.forward * fallSpeed * 0.2f) : (Vector3.down * fallSpeed);
            _ballRb.useGravity = true; // Hand control back to physics
        }
    }

    // Compute shot logic in relation to right values for hoop and backboard
    private void UpdateTarget(float shootingSpeed)
    {
        _diversion = 0;
        _targetIsBackboard = false;

        bool isHoopSpeed = (shootingSpeed >= minHoopSpeed && shootingSpeed <= maxHoopSpeed);
        bool isBackboardSpeed = (shootingSpeed >= minBackboardSpeed && shootingSpeed <= maxBackboardSpeed);

        if (isBackboardSpeed)
        {
            _endPos = _zoneBackboardTarget != null ? _zoneBackboardTarget.position : GameManager.Instance.Backboard.position;
            _targetIsBackboard = true;
        }
        else if (isHoopSpeed)
        {
            _endPos = GameManager.Instance.HoopBasket.position;
            _targetIsBackboard = false;
        }
        else if (shootingSpeed > maxBackboardSpeed)
        {
            _endPos = GameManager.Instance.Backboard.position;
            _diversion = 0.8f;
            _targetIsBackboard = true;
        }
        else
        {
            bool isAlmostHoopSpeed = (shootingSpeed > maxHoopSpeed && (shootingSpeed - maxHoopSpeed) <= 5f)
                || (shootingSpeed < minHoopSpeed && (minHoopSpeed - shootingSpeed) <= 5f);
            _diversion = isAlmostHoopSpeed ? 0.2f : 0.8f;
            // default prefer hoop
            _endPos = GameManager.Instance.HoopBasket.position;
            _targetIsBackboard = false;
        }

        if (_diversion == 0) return;

        int sign = UnityEngine.Random.value < 0.5f ? -1 : 1;
        int axis = UnityEngine.Random.value < 0.5f ? -1 : 1;
        if (axis == -1) _endPos.x += (_diversion * sign);
        if (axis == 1) _endPos.z += (_diversion * sign);
    }
    //private void UpdateTarget(float shootingSpeed)
    //{
    //    _diversion = 0;
    //    _targetIsBackboard = false;

    //    bool isHoopSpeed = (shootingSpeed >= minHoopSpeed && shootingSpeed <= maxHoopSpeed);
    //    bool isBackboardSpeed = (shootingSpeed >= minBackboardSpeed && shootingSpeed <= maxBackboardSpeed);

    //    if (isBackboardSpeed)
    //    {
    //        // Use zone-specific backboard target if available, otherwise fallback to global
    //        _endPos = _zoneBackboardTarget != null ? _zoneBackboardTarget.position : GameManager.Instance.Backboard.position;
    //        _targetIsBackboard = true;

    //        // If this is an AI ball, compensate the backboard target with the opponent spawn offset
    //        if (AIBall && GameManager.Instance != null)
    //        {
    //            _endPos += GameManager.Instance.OpponentPositionOffset;
    //            if (debugLaunch)
    //                Debug.Log($"[AI Compensation] Adjusted backboard target by opponentPositionOffset: {_endPos}");
    //        }
    //    }
    //    else if (isHoopSpeed)
    //    {
    //        _endPos = GameManager.Instance.HoopBasket.position;
    //        _targetIsBackboard = false;
    //    }
    //    else if (shootingSpeed > maxBackboardSpeed)
    //    {
    //        _endPos = GameManager.Instance.Backboard.position;
    //        _diversion = 0.8f;
    //        _targetIsBackboard = true;
    //    }
    //    else
    //    {
    //        bool isAlmostHoopSpeed = (shootingSpeed > maxHoopSpeed && (shootingSpeed - maxHoopSpeed) <= 5f)
    //            || (shootingSpeed < minHoopSpeed && (minHoopSpeed - shootingSpeed) <= 5f);
    //        _diversion = isAlmostHoopSpeed ? 0.2f : 0.8f;
    //        // default prefer hoop
    //        _endPos = GameManager.Instance.HoopBasket.position;
    //        _targetIsBackboard = false;
    //    }

    //    if (_diversion == 0) return;

    //    int sign = UnityEngine.Random.value < 0.5f ? -1 : 1;
    //    int axis = UnityEngine.Random.value < 0.5f ? -1 : 1;
    //    if (axis == -1) _endPos.x += (_diversion * sign);
    //    if (axis == 1) _endPos.z += (_diversion * sign);
    //}

    public void Shoot(float shootingSpeed)
    {
        shootingSpeed = 72f;

        _isFlying = true; // To check on endgame
        _state = BallState.Shooting;
        transform.SetParent(null);
        transform.localScale = Vector3.one;
        _startPos = transform.position;
        GameManager.Instance.SFXManager.PlayOneShot(woosh);

        _shootingSpeed = shootingSpeed;
        UpdateTarget(shootingSpeed);

        // Ensure collider active
        _ballCollider.enabled = true;

        // Make sure Rigidbody is simulated by physics
        _ballRb.isKinematic = false;
        _ballRb.useGravity = true;

        if (_zoneConfig != null)
        {
            backboardMaxLaunch = AIBall ? _zoneConfig.OpponentBackBoardMaxLaunch : _zoneConfig.BackBoardMaxLaunch;
        }

        // Select tuning values depending on target (use zone config if present)
        float selectedScale = _zoneConfig != null ? (_targetIsBackboard ? _zoneConfig.BackboardLaunchScale : _zoneConfig.HoopLaunchScale) : (_targetIsBackboard ? backboardLaunchScale : hoopLaunchScale);
        float selectedArcPref = _zoneConfig != null ? (_targetIsBackboard ? _zoneConfig.BackboardArcPreference : _zoneConfig.HoopArcPreference) : (_targetIsBackboard ? backboardArcPreference : hoopArcPreference);
        float selectedMin = _targetIsBackboard ? backboardMinLaunch : hoopMinLaunch;
        float selectedMax = _targetIsBackboard ? backboardMaxLaunch : hoopMaxLaunch;

        if (AIBall && _zoneConfig != null)
        {
            if (_targetIsBackboard)
            {
                selectedScale += +_zoneConfig.OpponentBackBoardLaunchScaleOffset;
                selectedArcPref += +_zoneConfig.OpponentBackBoardArcPreferenceOffset;
            } else
            {
                selectedScale += +_zoneConfig.OpponentHoopLaunchScaleOffset;
                selectedArcPref += +_zoneConfig.OpponentHoopArcPreferenceOffset;
            }

            if (debugLaunch)
                Debug.Log($"[AI launch scale Compensation] Adjusted target by opponentPositionOffset: {selectedScale}");
        }

        // Strength multiplier (now interpreted as a multiplier, default fallback field)
        float selectedStrengthMult = _zoneConfig != null ? Mathf.Max(0.001f, _zoneConfig.StrengthMultiplier) : Mathf.Max(0.001f, _strengthMultiplier);

        // compute launch speed from input (clamped)
        float launchSpeedUnclamped = shootingSpeed * selectedScale;
        float launchSpeed = Mathf.Clamp(launchSpeedUnclamped, selectedMin, selectedMax);

        if (debugLaunch)
            Debug.Log($"[Ball] targetIsBackboard={_targetIsBackboard} input={shootingSpeed:F1} -> launchSpeed={launchSpeed:F2}, arcPref={selectedArcPref:F2}, strengthMult={selectedStrengthMult:F2}");

        // Try compute ballistic initial velocity given computed launchSpeed and arc preference
        Vector3 launchVelocity;
        bool ok = TryCalculateLaunchVelocity(_startPos, _endPos, launchSpeed, selectedArcPref, selectedStrengthMult, out launchVelocity);

        if (ok)
        {
            _ballRb.velocity = launchVelocity;
            if (debugLaunch) Debug.Log($"[Ball] launchVelocity={launchVelocity} mag={launchVelocity.magnitude:F2}");
        }
        else
        {
            // fallback: aim towards target with scaled force
            Vector3 fallbackDir = (_endPos - _startPos).normalized;
            _ballRb.velocity = fallbackDir * Mathf.Max(3f, launchSpeed * 0.6f) * selectedStrengthMult;
            if (debugLaunch) Debug.LogWarning("[Ball] fallback launch used");
        }
    }

    // Called before the shot, when the caracter is animating the shot
    public void PrepareShot(Transform hand)
    {
        _state = BallState.Held;
        _playerHand = hand;

        _ballRb.velocity = Vector3.zero;
        _ballRb.angularVelocity = Vector3.zero;
        _ballRb.isKinematic = true;
        _ballRb.useGravity = false;

        transform.SetParent(hand);
        transform.position = hand.position;
    }

    private void SetupBallShoot()
    {
        _hoopEntered = false;
        _rimWasTouched = false;
        _backboardWasTouched = false;

        _startPos = BallStart.position;
        _endPos = GameManager.Instance.HoopBasket.position;
    }

    public void ResetState()
    {
        _ballCollider.enabled = false;
        SetupBallShoot();

        // Reset ball position w.r.t. player, and physics
        transform.position = _startPos;
        //transform.LookAt(GameManager.Instance.HoopBasket.transform);
        ResetPhysics();
        StartDribble();

        // Notify AI that he owns the ball again
        if (AIBall) Opponent.HasBall();

        if (GameManager.Instance.State == GameManager.GameState.GameOver && _groundWasTouched == true)
        {
            LastBallShot?.Invoke();
            return;
        }

        _groundWasTouched = false;
    }

    private void StartDribble()
    {
        _state = BallState.Dribbling;

        _dribbleTime = 0f;
        _prevYOffset = 0f;
        _goingDown = true;

        _ballRb.isKinematic = true;
        _ballRb.useGravity = false;
    }

    public void StopDribble()
    {
        _dribbleTime = 0f;

        transform.localScale = Vector3.one;
    }

    private void Dribble()
    {
        _dribbleTime += Time.deltaTime * dribbleSpeed;

        float yOffset = Mathf.Abs(Mathf.Sin(_dribbleTime)) * dribbleHeight;
        transform.position = BallStart.position - new Vector3(0, yOffset, 0);
        bool nowGoingDown = _prevYOffset > yOffset;

        if (_goingDown && !nowGoingDown)
            GameManager.Instance.SFXManager.PlayOneShot(dribble);

        //float scaleSize = yOffset + 0.4f - ((1 + Mathf.Abs(Mathf.Sin(_dribbleTime))) * 1.1f);
        //transform.localScale = new Vector3(scaleSize, 1f, scaleSize);

        _goingDown = nowGoingDown;
        _prevYOffset = yOffset;
    }

    public void ResetPhysics()
    {
        _ballRb.useGravity = false;
        _ballRb.velocity = Vector3.zero;
        _ballRb.angularVelocity = Vector3.zero;
        _ballRb.isKinematic = true;
    }

    // Manage collisions with ground, rim and backboard
    private void OnCollisionEnter(Collision collision)
    {
        GameManager.Instance.SFXManager.PlayOneShot(bounce);

        if (collision.collider.GetComponent<BallController>())
        {
            Physics.IgnoreCollision(collision.collider, _ballCollider);
            return;
        } else
        {
            if (AIBall)
            {
                Opponent.Drible();
            }
        }

        if (collision.collider.CompareTag(_groundTag))
        {
            // Prepare next shot if game still playing
            if (_hoopEntered)
                GameManager.Instance.ResetGameState(AIBall);
            else
                GameManager.Instance.Lose(AIBall);  // To manage fireball shut off

            _isFlying = false; // To check on endgame
            _groundWasTouched = true;

            ResetState();

            return;
        }

        // Register backboard touched and prevent accidental touch if rim was touched first
        if (collision.collider.CompareTag(_backboardTag) && !_backboardWasTouched && !_rimWasTouched)
        {
            _backboardWasTouched = true;
            GameManager.Instance.SFXManager.PlayOneShot(backboard);
            StartCoroutine(GameManager.Instance.SpawnShotParticles(transform.position, (int)ShotType.Backboard));
            return;
        }

        if (!collision.collider.transform.parent.CompareTag(_rimTag) || _rimWasTouched) return;
        _rimWasTouched = true;

        GameManager.Instance.SFXManager.PlayOneShot(rim);
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
        else if (!_rimWasTouched && CameraController.Instance && !AIBall) StartCoroutine(CameraController.Instance.Shake(cameraShakeDuration, cameraShakeMagintude));

        _hoopEntered = true;
        GameManager.Instance.SFXManager.PlayOneShot(hoop);
        // Show different effects for 2 points or 3 points shot
        StartCoroutine(GameManager.Instance.SpawnShotParticles(transform.position, _rimWasTouched ? (int)ShotType.Rim : (int)ShotType.Hoop));
        GameManager.Instance.Win(points, AIBall);
    }

    public void GameOver()
    {
        // Put balls away from map if game is over
        if (GameManager.Instance.State == GameManager.GameState.GameOver)
        {
            transform.position = Vector3.one * -5;
            _state = BallState.GameOver;
            return;
        }
    }

    public void EndGame(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver && !_isFlying)
        {
            LastBallShot?.Invoke();
        }
    }

    // Try to compute a ballistic launch velocity that sends the projectile from start to target
    // with a given speed magnitude and arc preference. Returns false if no real solution exists (speed too low).
    // Try to compute a ballistic launch velocity that sends the projectile from start to target
    // with a given speed magnitude and arc preference. Returns false if no real solution exists (speed too low).
    private bool TryCalculateLaunchVelocity(Vector3 start, Vector3 target, float speed, float arcPreferenceParam, float strengthMultiplierParam, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        Vector3 toTarget = target - start;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float distance = toTargetXZ.magnitude;
        float yOffset = toTarget.y;

        if (distance < 0.001f)
        {
            // almost vertical shot
            if (Mathf.Approximately(speed, 0f)) return false;
            float vy = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * Mathf.Abs(yOffset));
            Vector3 resultVert = Vector3.up * Mathf.Sign(yOffset) * vy;
            velocity = resultVert * strengthMultiplierParam;
            // apex is simply start + v^2/(2g) upward
            float tVertex = velocity.y / Mathf.Abs(Physics.gravity.y);
            computedApex = start + velocity * tVertex + 0.5f * Physics.gravity * tVertex * tVertex;
            return true;
        }

        float g = Mathf.Abs(Physics.gravity.y);
        float v2 = speed * speed;
        float insideSqrt = v2 * v2 - g * (g * distance * distance + 2f * yOffset * v2);

        if (insideSqrt < 0f)
        {
            // no solution with given speed
            return false;
        }

        float sqrt = Mathf.Sqrt(insideSqrt);

        // two possible tan(theta) solutions (low arc and high arc)
        float tanThetaLow = (v2 - sqrt) / (g * distance);
        float tanThetaHigh = (v2 + sqrt) / (g * distance);

        // compute angles (handle small/negative tan safely)
        float angleLow = Mathf.Atan(tanThetaLow);
        float angleHigh = Mathf.Atan(tanThetaHigh);

        // choose angle according to arcPreferenceParam (0 low -> 1 high)
        float chosenAngle = Mathf.Lerp(angleLow, angleHigh, Mathf.Clamp01(arcPreferenceParam));

        float cos = Mathf.Cos(chosenAngle);
        float sin = Mathf.Sin(chosenAngle);

        Vector3 dirXZ = toTargetXZ.normalized;

        // Compose initial velocity and apply strength multiplier
        Vector3 result = dirXZ * (speed * cos) + Vector3.up * (speed * sin);
        velocity = result * strengthMultiplierParam;

        // compute apex (vertex) position of this parabolic trajectory
        float tVertexFinal = velocity.y / g; // time to reach vertex (v_y / g)
        if (tVertexFinal < 0f) tVertexFinal = 0f;
        computedApex = start + velocity * tVertexFinal + 0.5f * Physics.gravity * tVertexFinal * tVertexFinal;

        if (debugLaunch)
            Debug.Log($"[TryCalculateLaunchVelocity] chosenAngle={chosenAngle * Mathf.Rad2Deg:F1} deg, apex={computedApex}, vel={velocity}");

        return true;
    }
}
