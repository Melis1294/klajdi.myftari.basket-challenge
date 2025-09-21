using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreController : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    private Transform _camera;

    private void OnEnable()
    {
        _camera = CameraController.instance.transform;
        transform.LookAt(_camera);
        transform.Rotate(0, 180f, 0);
        ResetLifetime();
    }

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        lifetime -= Time.deltaTime;

        if (lifetime <= 0) gameObject.SetActive(false);
    }

    void ResetLifetime()
    {
        lifetime = 3f;
    }
}
