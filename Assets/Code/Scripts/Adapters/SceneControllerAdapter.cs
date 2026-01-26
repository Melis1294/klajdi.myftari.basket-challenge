using UnityEngine;

public class SceneControllerAdapter : MonoBehaviour, ISceneService
{
    public void StartGame()
    {
        if (SceneController.Instance != null) SceneController.Instance.StartGame();
    }

    public void BackToMainMenu()
    {
        if (SceneController.Instance != null) SceneController.Instance.BackToMainMenu();
    }

    public int[] GetScores()
    {
        return SceneController.Instance != null ? SceneController.Instance.GetScores() : new int[] { 0, 0 };
    }
}
