using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [SerializeField] private bool hasBall;
    public BallController BallInstance;

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
            BallInstance.Shoot(shootingSpeed);
        }
    }

    public void HasBall()
    {
        hasBall = true;
    }
}
