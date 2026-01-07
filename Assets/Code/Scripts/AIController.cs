using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [SerializeField] private bool hasBall;
    public BallController BallInstance;
    private Animator _animator;
    private Transform _hand;

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
        float shotCountdown = UnityEngine.Random.Range(1f, 3f);
        yield return new WaitForSeconds(shotCountdown);
        // Check if game is still on playing
        if (GameManager.Instance.State == GameManager.GameState.Play)
        {
            float shootingSpeed = UnityEngine.Random.Range(5f, 90f);
            _animator.SetBool("shoot", true);
            BallInstance.PrepareShot(_hand);
            yield return new WaitForSeconds(0.7f);
            BallInstance.Shoot(shootingSpeed);
            yield return new WaitForSeconds(1f);
            _animator.SetBool("shoot", false);
        }
    }

    public void HasBall()
    {
        hasBall = true;
    }

    public void SetAnimator()
    {
        _animator = GetComponent<Animator>();

        _hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (_hand == null)
            Debug.LogError("RightHand bone not found!");
        else Debug.Log(_hand.name); // should print "mixamorig:RightHand"
    }
}
