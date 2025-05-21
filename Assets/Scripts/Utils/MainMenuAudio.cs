using UnityEngine;

public class MainMenuBGM : MonoBehaviour
{
    public AudioClip mainMenuBGM;

    void Start()
    {
        if (mainMenuBGM != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(mainMenuBGM, true, 1.5f); // 1.5s fade-in
    }
}