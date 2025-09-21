using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance { get; private set; }

    private void Awake()
    {
        // Prevent class instance duplicates
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    public void StartGame() => SceneManager.LoadScene(1);

    public void BackToMainMenu() => SceneManager.LoadScene(0);
}
