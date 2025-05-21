using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene"); // Replace with your gameplay scene name
    }

    public void OpenSettings()
    {
        // Show settings panel if you have one
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}