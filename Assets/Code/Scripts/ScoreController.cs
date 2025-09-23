using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreController : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    private Transform _camera;
    private RectTransform _rectTransform;

    private void OnEnable()
    {
        _camera = CameraController.Instance.transform;
        // Make score text look at camera at each shot
        _rectTransform.LookAt(_camera);
        // Get the current euler angles
        Vector3 euler = _rectTransform.rotation.eulerAngles;
        // Keep only Y, reset X and Z
        _rectTransform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        ResetLifetime();
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        lifetime -= Time.deltaTime;

        if (lifetime <= 0) gameObject.SetActive(false);
    }

    // Manage score txt lifetime
    void ResetLifetime()
    {
        lifetime = 3f;
    }
}
