using System.Collections;
using UnityEngine;

public class CameraControllerAdapter : MonoBehaviour, ICameraService
{
    public void SetupPlayerCamera(Transform cameraStart, Transform cameraTarget)
    {
        if (CameraController.Instance != null) CameraController.Instance.SetupPlayerCamera(cameraStart, cameraTarget);
    }

    public void StartMoving()
    {
        if (CameraController.Instance != null) CameraController.Instance.StartMoving();
    }

    public void SetPodiumCamera(Transform podiumTransform)
    {
        if (CameraController.Instance != null) CameraController.Instance.SetPodiumCamera(podiumTransform);
    }

    public IEnumerator Shake(float duration, float magnitude)
    {
        if (CameraController.Instance != null) yield return CameraController.Instance.Shake(duration, magnitude);
        yield break;
    }
}
