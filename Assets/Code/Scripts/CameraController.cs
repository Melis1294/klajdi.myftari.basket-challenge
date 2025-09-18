using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    private float elapsed = 1.5f;
    private float duration = 1.5f; // total time of flight
    private float arcHeight = 2f;  // height of the parabola
    [SerializeField] private Transform cameraStart;
    [SerializeField] private Transform cameraEnd;
    private Vector3 startPos;
    private Vector3 endPos;
    public static CameraController instance { get; private set; }

    private void Awake()
    {
        // Prevent class instance duplicates
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        startPos = cameraStart.position;
        endPos = cameraEnd.position;
        ResetCamera();
    }

    // Update is called once per frame
    void Update()
    {
        // ball in the air
        if (elapsed < duration) ComputeCameraMove();
    }

    void ComputeCameraMove()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // Linear interpolation in XZ
        Vector3 horizontalPos = Vector3.Lerp(
            new Vector3(startPos.x, 0, startPos.z),
            new Vector3(endPos.x, 0, endPos.z),
            t
        );

        // Parabolic interpolation in Y
        float y = Mathf.Lerp(startPos.y, endPos.y, t) + arcHeight * 4 * t * (1 - t);

        Vector3 targetPos = new Vector3(horizontalPos.x, y, horizontalPos.z);

        // Add smooth movement transition
        float smoothSpeed = 5f; // tweak this value (higher = snappier)
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
    }

    public void StartMoving()
    {
        startPos = cameraStart.position;
        endPos = cameraEnd.position;
        elapsed = 0f;
    }

    public void ResetCamera()
    {
        transform.position = startPos;
    }
}
