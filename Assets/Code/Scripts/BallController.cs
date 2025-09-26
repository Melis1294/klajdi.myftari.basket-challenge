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

    [SerializeField] float minHoopSpeed = 40;
    [SerializeField] float maxHoopSpeed = 50;
    [SerializeField] float minBackboardSpeed = 70;
    [SerializeField] float maxBackboardSpeed = 75;
    private float _diversion = 0;
    private float _shootingSpeed;

    // Event to notify AI that he has the ball again
    public bool AIBall;
    private Collider _ballCollider;

    private void Awake()
    {
        _ballRb = GetComponent<Rigidbody>();
        _ballCollider = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        // ball in the air
        if (_elapsed < _duration) ComputeFlight();
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
        _shootingSpeed = shootingSpeed;
        UpdateTarget(shootingSpeed);
        _elapsed = 0;
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
        SetupBallShoot();
        
        // Reset ball position w.r.t. player, and physics
        transform.position = _startPos;
        transform.LookAt(GameManager.Instance.HoopBasket.transform);
        ResetPhysics();

        // Notify AI that he owns the ball again
        AIController aiParent = transform.GetComponentInParent<AIController>();
        if (aiParent) aiParent.HasBall();
    }

    void ResetPhysics()
    {
        _ballRb.useGravity = false;
        _ballRb.velocity = Vector3.zero;
        _ballRb.angularVelocity = Vector3.zero;
    }

    // Manage collisions with ground, rim and backboard
    private void OnCollisionEnter(Collision collision)
    {
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
                GameManager.Instance.Lose(AIBall);  // To manage fireball counter
            ResetState();
            return;
        }

        if (collision.collider.CompareTag(_backboardTag) && !_backboardWasTouched)
        {
            _backboardWasTouched = true;
            return;
        }

        if (!collision.collider.transform.parent.CompareTag(_rimTag) || _rimWasTouched) return;
        _rimWasTouched = true;
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
        GameManager.Instance.Win(points, AIBall);
    }
}
