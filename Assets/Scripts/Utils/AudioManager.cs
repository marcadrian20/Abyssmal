using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    // [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Audio Clips")]
    public AudioClip jumpClip;
    public AudioClip attackClip;
    public AudioClip hitClip;
    public AudioClip healClip;
    public AudioClip deathClip;
    // Add more as needed

    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.3f;

    [Header("Pooling")]
    [SerializeField] private int poolSize = 10;
    [SerializeField] private AudioSource pooledSfxPrefab;
    private List<AudioSource> sfxPool;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        // Initialize SFX pool
        sfxPool = new List<AudioSource>();
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource sfx = Instantiate(pooledSfxPrefab, transform);
            sfx.playOnAwake = false;
            sfxPool.Add(sfx);
        }
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource source = GetAvailableSfxSource();
        if (source != null)
        {
            source.clip = clip;
            source.Play();
        }
    }

    public void PlayMusic(AudioClip clip, bool loop = true, float fadeInDuration = 1f)
    {
        if (musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = 0f; // Start volume at 0 for fade-in
        musicSource.Play();
        StartCoroutine(FadeInMusic(fadeInDuration));

    }
    private IEnumerator FadeInMusic(float duration)
    {
        float targetVolume = musicVolume;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
            yield return null;
        }
        musicSource.volume = targetVolume; // Set volume to desired level

    }
    public void PlaySFXAt(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;
        AudioSource source = GetAvailableSfxSource();
        if (source != null)
        {
            source.transform.position = position;
            source.clip = clip;
            source.spatialBlend = 1f; // 3D sound
            source.Play();
        }
    }

    private AudioSource GetAvailableSfxSource()
    {
        foreach (var sfx in sfxPool)
        {
            if (!sfx.isPlaying)
                return sfx;
        }
        // Optionally, expand pool if all are busy
        return null;
    }
}