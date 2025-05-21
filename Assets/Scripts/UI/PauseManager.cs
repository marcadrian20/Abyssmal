using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Pause Settings")]
    [SerializeField] private PlayerInput playerInput;
    private InputAction pauseAction;
    private bool isPaused = false;

    void Awake()
    {
        // playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            pauseAction = playerInput.actions["Pause"];
            pauseAction.performed += OnPausePerformed;
        }
    }

    void OnDestroy()
    {
        if (pauseAction != null)
            pauseAction.performed -= OnPausePerformed;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        UIManager.Instance.ShowPanel(UIManager.Instance.pausePanel);
        UIManager.Instance.HidePanel(UIManager.Instance.hudPanel);
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("UI");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        UIManager.Instance.HidePanel(UIManager.Instance.pausePanel);
        UIManager.Instance.ShowPanel(UIManager.Instance.hudPanel);
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("Player");
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // Replace with your main menu scene name
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}