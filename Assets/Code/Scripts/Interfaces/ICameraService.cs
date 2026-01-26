using System.Collections;
using UnityEngine;

public interface ICameraService
{
    void SetupPlayerCamera(Transform cameraStart, Transform cameraTarget);
    void StartMoving();
    void SetPodiumCamera(Transform podiumTransform);
    IEnumerator Shake(float duration, float magnitude);
}
