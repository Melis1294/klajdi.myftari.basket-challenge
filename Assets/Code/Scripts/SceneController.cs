using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    private void Awake()
    {
        // Prevent class instance duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // Start or restart the game
    public void StartGame() => SceneManager.LoadScene(1);

    public void BackToMainMenu() => SceneManager.LoadScene(0);
}
