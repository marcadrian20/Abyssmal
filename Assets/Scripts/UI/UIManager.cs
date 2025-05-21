using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject hudPanel;
    // public GameObject mainMenuPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject settingsPanel;
    // Add more as needed

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void ShowPanel(GameObject panel)
    {
        panel.SetActive(true);
    }
    public void HidePanel(GameObject panel)
    {
        panel.SetActive(false);
    }
    public void HideAllPanels()
    {
        hudPanel.SetActive(false);
        // mainMenuPanel.SetActive(false);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        settingsPanel.SetActive(false);
        // etc.
    }
}