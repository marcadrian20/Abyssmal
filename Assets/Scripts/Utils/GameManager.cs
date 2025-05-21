using UnityEngine;
using System.Collections;
using System.Linq;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private Vector3 lastCheckpointPosition;
    private string lastZoneName;

    private Checkpoint lastCheckpoint;

    [Header("UI")]
    [SerializeField] private ZoneOverlayUI zoneOverlayUI;
    [Header("BGM")]
    public AudioClip defaultBGM;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Play default BGM if set
            if (defaultBGM != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayMusic(defaultBGM, true, 1.5f);
                else
                    StartCoroutine(PlayMusicWhenReady());
            }
            LoadCheckpoint(); // Load checkpoint on game start
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private IEnumerator PlayMusicWhenReady()
    {
        // Wait until AudioManager.Instance is not null
        while (AudioManager.Instance == null)
            yield return null;
        AudioManager.Instance.PlayMusic(defaultBGM, true, 1.5f);
    }
    // Example: Call this when entering a new zone or checkpoint
    public void PlayZoneBGM(AudioClip bgmClip, float fadeIn = 1.5f)
    {
        if (bgmClip != null)
            AudioManager.Instance.PlayMusic(bgmClip, true, fadeIn);
    }

    // Example: Save/Load stubs
    public void SaveGame()
    {
        // Implement save logic here
    }

    public void LoadGame()
    {
        // Implement load logic here
    }

    // Example: Handle game over
    public void GameOver()
    {
        // Show game over UI, stop player, etc.
        UIManager.Instance.ShowPanel(UIManager.Instance.gameOverPanel);

    }

    public void SetCheckpoint(Checkpoint checkpoint, Vector3 position)
    {
        lastCheckpoint = checkpoint;
        lastCheckpointPosition = position;
        lastZoneName = checkpoint.zoneName;

        // Save checkpoint position (you can use PlayerPrefs for simplicity)
        PlayerPrefs.SetFloat("CheckpointX", position.x);
        PlayerPrefs.SetFloat("CheckpointY", position.y);
        PlayerPrefs.SetFloat("CheckpointZ", position.z);
        PlayerPrefs.SetString("CheckpointZone", lastZoneName);
        PlayerPrefs.Save();
    }
    //Call this on new game to clear checkpoint
    public void ClearCheckpoint()
    {
        PlayerPrefs.DeleteKey("CheckpointX");
        PlayerPrefs.DeleteKey("CheckpointY");
        PlayerPrefs.DeleteKey("CheckpointZ");
        PlayerPrefs.DeleteKey("CheckpointZone");
        PlayerPrefs.Save();
    }
    public void LoadCheckpoint()
    {
        if (PlayerPrefs.HasKey("CheckpointX"))
        {
            float x = PlayerPrefs.GetFloat("CheckpointX");
            float y = PlayerPrefs.GetFloat("CheckpointY");
            float z = PlayerPrefs.GetFloat("CheckpointZ");
            lastCheckpointPosition = new Vector3(x, y, z);
            lastZoneName = PlayerPrefs.GetString("CheckpointZone", "");
        }
        else
        {
            // If no checkpoint saved, use player start position
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                lastCheckpointPosition = player.transform.position;
        }
    }
    public void RespawnPlayer(GameObject player)
    {
        LoadCheckpoint();
        player.transform.position = lastCheckpointPosition;
        // Optionally restore health, reset states, etc.
        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
            health.Heal(health.GetMaxHealth());
        ShowZoneOverlay(lastZoneName);

    }
    public void ShowZoneOverlay(string zoneName)
    {
        Debug.Log("Showing zone overlay: " + zoneName);
        if (zoneOverlayUI != null && !string.IsNullOrEmpty(zoneName))
            zoneOverlayUI.ShowZone(zoneName);
    }
}