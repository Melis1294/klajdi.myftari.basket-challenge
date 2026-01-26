using UnityEngine;

public class InputManagerAdapter : MonoBehaviour, IInputService
{
    public bool Enabled
    {
        get => InputManager.Instance != null && InputManager.Instance.enabled;
        set { if (InputManager.Instance != null) InputManager.Instance.enabled = value; }
    }

    public void RestartShot()
    {
        if (InputManager.Instance != null) InputManager.Instance.RestartShot();
    }
}
