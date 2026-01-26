using UnityEngine;

public class FireballControllerAdapter : MonoBehaviour, IFireballService
{
    public int FireballMultiplier => FireballController.Instance != null ? FireballController.Instance.FireballMultiplier : 1;

    public void AddScore(float amount)
    {
        if (FireballController.Instance != null) FireballController.Instance.AddScore(amount);
    }

    public void OnMissedShot()
    {
        if (FireballController.Instance != null) FireballController.Instance.OnMissedShot();
    }
}
