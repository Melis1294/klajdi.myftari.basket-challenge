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
    private string _hoopTag = "Hoop";
    private string _rimTag = "Rim";
    private string _backboardTag = "Backboard";
    private string _groundTag = "Ground";

    [SerializeField] private float _fallSpeed = 1.8f;
    private float _elapsed = 1.5f;
    private readonly float _duration = 1.5f; // total time of flight
    private readonly float _arcHeight = 2f;  // height of the parabola
    private Vector3 _startPos;
    private Vector3 _endPos;
    private Rigidbody _ballRb;
    private Transform _playerHand;
    private Vector3 _playerHandOffset;

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
    public Transform Opponent;

    // Audio
    private AudioSource _sfxManager;
    [SerializeField] private AudioClip bounce;
    [SerializeField] private AudioClip hoop;
    [SerializeField] private AudioClip woosh;
    [SerializeField] private AudioClip rim;
    [SerializeField] private AudioClip backboard;
    [SerializeField] private AudioClip dribble;

    // PhysicsMethods
    public float power = 10.0f;

    // Dribble
    [SerializeField] private float dribbleHeight = 0.8f;
    [SerializeField] private float dribbleSpeed = 5f;
    private float _prevYOffset;
    private bool _goingDown;

    private float _dribbleTime;
    private bool _isDribbling;

    private enum BallState
    {
        Dribbling,
        Held,
        Shooting
    }

    private BallState _state;


    enum ShotType
    {
        Backboard,
        Rim,
        Hoop
    }

    private void Awake()
    {
        _ballRb = GetComponent<Rigidbody>();
        _ballCollider = GetComponent<Collider>();
    }

    private void LateUpdate()
    {
        if (_state == BallState.Held)
        {
            transform.position = _playerHand.position;
            //transform.position = _playerHand.TransformPoint(_playerHandOffset);
        }
    }

    void FixedUpdate()
    {
        if (_state == BallState.Shooting)
        {
            if (_elapsed < _duration)
                ComputeFlight();
            return;
        }

        if (_state == BallState.Dribbling)
        {
            _dribbleTime += Time.deltaTime * dribbleSpeed;

            float yOffset = Mathf.Abs(Mathf.Sin(_dribbleTime)) * dribbleHeight;
            try
            {
                transform.position = BallStart.position - new Vector3(0, yOffset, 0);
            } catch (NullReferenceException e)
            {
                if (AIBall)
                    Debug.LogWarning("Ai ball start not assigned");
                else
                    Debug.LogWarning("Character ball start not assigned");
            }

            bool nowGoingDown = _prevYOffset > yOffset;

            if (_goingDown && !nowGoingDown)
                _sfxManager.PlayOneShot(dribble);

            float scaleSize = yOffset + 0.1f - ((1 + Mathf.Abs(Mathf.Sin(_dribbleTime))) * 1.1f);
            transform.localScale = new Vector3(scaleSize, 1f, scaleSize);

            _goingDown = nowGoingDown;
            _prevYOffset = yOffset;
        }

        if (!AIBall)
            _fireTrails.SetActive(FireballController.Instance.FireballMultiplier == 2);
    }

    // Update is called once per frame
    //void FixedUpdate()
    //{
    //    // ball in the air
    //    if (_elapsed < _duration) ComputeFlight();
    //    else if (_isDribbling)
    //    {
    //        _dribbleTime += Time.deltaTime * dribbleSpeed;

    //        float yOffset = Mathf.Abs(Mathf.Sin(_dribbleTime)) * dribbleHeight;
    //        transform.position = BallStart.position - new Vector3(0, yOffset, 0);

    //        // Direction detection
    //        bool nowGoingDown = _prevYOffset > yOffset;

    //        // Bottom reached → bounce sound
    //        if (_goingDown && !nowGoingDown)
    //        {
    //            _sfxManager.PlayOneShot(dribble);
    //        }

    //        float scaleSize = yOffset + 0.1f - ((1 + Mathf.Abs(Mathf.Sin(_dribbleTime))) * 1.1f);
    //        transform.localScale = new Vector3(scaleSize, 1f, scaleSize);

    //        _goingDown = nowGoingDown;
    //        _prevYOffset = yOffset;
    //    }
    //    if (!AIBall) _fireTrails.SetActive(FireballController.Instance.FireballMultiplier == 2);
    //}

    private void Start()
    {
        if (!AIBall) _fireTrails = transform.GetChild(0).gameObject;
        _sfxManager = GameManager.Instance.SFXManager;
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
            _ballRb.velocity = (_shootingSpeed >= minBackboardSpeed) ? (Vector3.forward * _fallSpeed * 0.2f) : (Vector3.down * _fallSpeed);
            _ballRb.useGravity = true; // Hand control back to physics
        }
    }

    // Compute shot logic in relation to right values for hoop and backboard
    private void UpdateTarget(float shootingSpeed)
    {
        _diversion = 0;
        bool isHoopSpeed = (shootingSpeed >= minHoopSpeed && shootingSpeed <= maxHoopSpeed);
        bool isBackboardSpeed = (shootingSpeed >= minBackboardSpeed && shootingSpeed <= maxBackboardSpeed);

        if (isBackboardSpeed)
        {
            _endPos = GameManager.Instance.Backboard.position;
        }
        else if (isHoopSpeed)
        {
            _endPos = GameManager.Instance.HoopBasket.position;
        }
        else if (shootingSpeed > maxBackboardSpeed)
        {
            _endPos = GameManager.Instance.Backboard.position;
            _diversion = 0.8f;
        }
        else
        {
            bool isAlmostHoopSpeed = (shootingSpeed > maxHoopSpeed && (shootingSpeed - maxHoopSpeed) <= 5f)
                || (shootingSpeed < minHoopSpeed && (minHoopSpeed - shootingSpeed) <= 5f);
            _diversion = isAlmostHoopSpeed ? 0.2f : 0.8f;
        }

        if (_diversion == 0) return;

        int sign = UnityEngine.Random.value < 0.5f ? -1 : 1;
        int axis = UnityEngine.Random.value < 0.5f ? -1 : 1;
        if (axis == -1) _endPos.x += (_diversion * sign);
        if (axis == 1) _endPos.z += (_diversion * sign);
    }

    public void Shoot(float shootingSpeed)
    {
        _sfxManager.PlayOneShot(woosh);

        _state = BallState.Shooting;

        _ballRb.isKinematic = true; // still script-driven


        if (AIBall) transform.SetParent(Opponent);
        else transform.SetParent(null);

        _shootingSpeed = shootingSpeed;
        UpdateTarget(shootingSpeed);
        _elapsed = 0f;
        _ballCollider.enabled = true;
    }

    //public void Shoot(float shootingSpeed)
    //{
    //    // Ball shot sfx
    //    _sfxManager.PlayOneShot(woosh);

    //    _shootingSpeed = shootingSpeed;
    //    UpdateTarget(shootingSpeed);
    //    _elapsed = 0;
    //}

    // Called before the shot, when the caracter is animating the shot
    public void PrepareShot(Transform hand)
    {
        _state = BallState.Held;
        _playerHand = hand;

        _isDribbling = false;

        _ballRb.velocity = Vector3.zero;
        _ballRb.angularVelocity = Vector3.zero;
        _ballRb.isKinematic = true;

        transform.SetParent(hand);
        transform.position = hand.position;
    }

    private void SetupBallShoot()
    {
        _hoopEntered = false;
        _rimWasTouched = false;
        _backboardWasTouched = false;

        _startPos = BallStart?.position ?? transform.position;
        _endPos = GameManager.Instance.HoopBasket.position;
    }

    public void ResetState()
    {
        _ballCollider.enabled = false;
        SetupBallShoot();

        _state = BallState.Dribbling;

        // Reset ball position w.r.t. player, and physics
        transform.position = _startPos;
        //transform.LookAt(GameManager.Instance.HoopBasket.transform);
        ResetPhysics();
        //StartDribble();

        // Notify AI that he owns the ball again
        AIController aiParent = transform.GetComponentInParent<AIController>();
        if (aiParent) aiParent.HasBall();
    }

    private void StartDribble()
    {
        _state = BallState.Dribbling;

        _isDribbling = true;
        _dribbleTime = 0f;
        _prevYOffset = 0f;
        _goingDown = true;

        _ballRb.isKinematic = true;
    }

    public void StopDribble()
    {
        _isDribbling = false;
        _dribbleTime = 0f;

        transform.localScale = Vector3.one;

        // Snap cleanly to hand position
        //transform.position = BallStart.position;

        //// Reset physics completely
        //_ballRb.velocity = Vector3.zero;
        //_ballRb.angularVelocity = Vector3.zero;
        //_ballRb.isKinematic = false;
        //// Get original ball shape
        //transform.localScale = Vector3.one;
    }

    public void ResetPhysics()
    {
        _ballRb.useGravity = false;
        _ballRb.velocity = Vector3.zero;
        _ballRb.angularVelocity = Vector3.zero;
    }

    // Manage collisions with ground, rim and backboard
    private void OnCollisionEnter(Collision collision)
    {
        _sfxManager.PlayOneShot(bounce);

        if (collision.collider.GetComponent<BallController>())
        {
            Physics.IgnoreCollision(collision.collider, _ballCollider);
            return;
        }

        if (collision.collider.CompareTag(_groundTag))
        {
            // Prepare next shot if game still playing
            if (_hoopEntered)
                GameManager.Instance.ResetGameState(AIBall);
            else
                GameManager.Instance.Lose(AIBall);  // To manage fireOn counter
            ResetState();
            return;
        }

        // Register backboard touched and prevent accidental touch if rim was touched first
        if (collision.collider.CompareTag(_backboardTag) && !_backboardWasTouched && !_rimWasTouched)
        {
            _backboardWasTouched = true;
            _sfxManager.PlayOneShot(backboard);
            StartCoroutine(GameManager.Instance.SpawnShotParticles(transform.position, (int)ShotType.Backboard));
            return;
        }

        if (!collision.collider.transform.parent.CompareTag(_rimTag) || _rimWasTouched) return;
        _rimWasTouched = true;

        _sfxManager.PlayOneShot(rim);
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
        _sfxManager.PlayOneShot(hoop);
        StartCoroutine(GameManager.Instance.SpawnShotParticles(transform.position, _rimWasTouched ? (int)ShotType.Rim : (int)ShotType.Hoop));
        GameManager.Instance.Win(points, AIBall);
    }

    //PhysicsMethods
    public Vector3 CalculateForce()
    {
        return transform.forward * power;
    }

    private void Predict()
    {
        GameManager.Instance.predict(gameObject, transform.position, CalculateForce());
    }
}
