using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AIController : MonoBehaviour
{
    [SerializeField] private bool hasBall;
    public BallController BallInstance;
    private Animator _animator;
    public Transform OpponentHand;
    private bool _goodShot;

    private void Awake()
    {
        SetAnimator();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.State != GameManager.GameState.Play) return;

        if (hasBall) ManageShot();
    }

    void ManageShot()
    {
        hasBall = false;
        StartCoroutine(Shoot());
    }

    IEnumerator Shoot()
    {
        _goodShot = Random.value < 0.66f;
        float shotCountdown = UnityEngine.Random.Range(1f, 3f);
        yield return new WaitForSeconds(shotCountdown);
        // Check if game is still on playing
        if (GameManager.Instance.State == GameManager.GameState.Play && BallInstance)
        {
            float shootingSpeed;

            if (_goodShot)
            {
                // Pick which success range
                if (Random.value < 0.5f)
                {
                    shootingSpeed = Random.Range(40f, 50f);
                }
                else
                {
                    shootingSpeed = Random.Range(70f, 75f);
                }
            }
            else
            {
                // Bad shot: pick outside success ranges
                shootingSpeed = GetBadShootingSpeed();
            }
            _animator.SetBool("shoot", true);
            BallInstance.PrepareShot(OpponentHand);
            yield return new WaitForSeconds(0.7f);

            shootingSpeed = 45f;
            BallInstance.Shoot(shootingSpeed);
        }
    }

    public void HasBall()
    {
        hasBall = true;
    }

    public void SetAnimator()
    {
        _animator = GetComponent<Animator>();

        if (OpponentHand == null) { 
            // Empty related to the hand where to place the ball when shooting it
            OpponentHand = _animator.GetBoneTransform(HumanBodyBones.RightHand).GetChild(5);
        }
        
        if (OpponentHand == null)
            Debug.LogError("RightHand bone not found!");
    }

    public void GameOver() => _animator.SetBool("game_ended", true);

    public void Victory(bool victory) => _animator.SetTrigger(victory ? "victory" : "defeat");

    public void Drible() => _animator.SetBool("shoot", false);

    private float GetBadShootingSpeed()
    {
        while (true)
        {
            float value = Random.Range(5f, 90f);

            bool inFirstGoodRange = value >= 40f && value <= 50f;
            bool inSecondGoodRange = value >= 70f && value <= 75f;

            if (!inFirstGoodRange && !inSecondGoodRange)
                return value;
        }
    }

}
