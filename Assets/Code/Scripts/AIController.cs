using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [SerializeField] private bool hasBall;
    private BallController _ballInstance;

    // Start is called before the first frame update
    void Start()
    {
        _ballInstance = transform.GetChild(0).GetComponent<BallController>();
        if (!_ballInstance) 
            throw new NullReferenceException("Character instance not found!");
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
        float shootingSpeed = UnityEngine.Random.Range(5f, 90f);
        _ballInstance.Shoot(shootingSpeed);
    }
}
