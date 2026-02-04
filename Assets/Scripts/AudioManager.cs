using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using System.IO;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource voiceSource;

    [Header("Audio Lists")]
    public List<AudioEntry> sfxList = new List<AudioEntry>();
    public List<AudioEntry> musicList = new List<AudioEntry>();
    public List<AudioEntry> voiceList = new List<AudioEntry>();

    [Header("Voice Auto-Load Settings")]
    [Tooltip("Enable automatic loading of voice clips from Resources folders")]
    public bool autoLoadVoiceFromResources = true;
    [Tooltip("Resources folder path for voice clips (relative to Assets/Resources/)")]
    public string voiceResourcesPath = "Audio/DialogueAudio";
    [Tooltip("If true, searches character subfolders (Alex, Jessica, Benji, Yari)")]
    public bool includeCharacterSubfolders = true;

    private Dictionary<string, AudioEntry> sfxDict = new Dictionary<string, AudioEntry>();
    private Dictionary<string, AudioEntry> musicDict = new Dictionary<string, AudioEntry>();
    private Dictionary<string, AudioEntry> voiceDict = new Dictionary<string, AudioEntry>();

    public static AudioManager Instance;

    [System.Serializable]
    public class AudioEntry
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    private Coroutine currentMusicFade;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy new duplicate in the SAME scene
            Destroy(gameObject);
            return;
        }

        Instance = this;

        BuildDictionaries();
    }

    void OnDestroy()
    {
        // If this instance is being destroyed, clear the singleton reference
        if (Instance == this)
            Instance = null;
    }

    private void BuildDictionaries()
    {
        sfxDict.Clear();
        musicDict.Clear();
        voiceDict.Clear();

        foreach (var entry in sfxList)
        {
            if (entry != null && entry.clip != null && !string.IsNullOrEmpty(entry.name))
                sfxDict.TryAdd(entry.name.ToLower(), entry);
        }

        foreach (var entry in musicList)
        {
            if (entry != null && entry.clip != null && !string.IsNullOrEmpty(entry.name))
                musicDict.TryAdd(entry.name.ToLower(), entry);
        }

        // Load from manual list first
        foreach (var entry in voiceList)
        {
            if (entry != null && entry.clip != null && !string.IsNullOrEmpty(entry.name))
                voiceDict.TryAdd(entry.name.ToLower(), entry);
        }

        // Auto-load from Resources if enabled
        if (autoLoadVoiceFromResources)
        {
            LoadVoiceClipsFromResources();
        }
    }

    private void LoadVoiceClipsFromResources()
    {
        if (string.IsNullOrEmpty(voiceResourcesPath))
            return;

        // Load clips from the main voice folder (no prefix)
        LoadClipsFromPath(voiceResourcesPath, voiceDict);

        // Load clips from character subfolders if enabled
        if (includeCharacterSubfolders)
        {
            // Load from character-specific folders: Alex, Jessica, Benji, Yari
            string[] characterFolders = { "Alex", "Jessica", "Benji", "Yari" };
            
            foreach (string charFolder in characterFolders)
            {
                string subPath = $"{voiceResourcesPath}/{charFolder}";
                AudioClip[] subClips = Resources.LoadAll<AudioClip>(subPath);

                if (subClips != null && subClips.Length > 0)
                {
                    // Found clips in this character's folder, load them with character prefix
                    LoadClipsFromPath(subPath, voiceDict, $"{charFolder}_");
                }
            }
        }
    }

    private void LoadClipsFromPath(string resourcesPath, Dictionary<string, AudioEntry> targetDict, string namePrefix = "")
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>(resourcesPath);

        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;

            string clipName = namePrefix + clip.name;
            string key = clipName.ToLower();

            // Only add if not already in dictionary (manual entries take precedence)
            if (!targetDict.ContainsKey(key))
            {
                AudioEntry entry = new AudioEntry
                {
                    name = clipName,
                    clip = clip,
                    volume = 1f
                };
                targetDict.Add(key, entry);
            }
        }
    }

    // ========================
    // MUSIC COMMANDS
    // ========================

    [YarnCommand("play_music")]
    public void PlayMusic(string clipName, float volume = -1f, float fadeTime = 1f)
    {
        if (musicSource == null)
        {
            Debug.LogWarning("AudioManager: No music source assigned!");
            return;
        }

        if (!musicDict.TryGetValue(clipName.ToLower(), out var entry))
        {
            Debug.LogWarning($"AudioManager: No music clip found with name '{clipName}'");
            return;
        }

        float targetVolume = (volume >= 0f) ? Mathf.Clamp01(volume) : entry.volume;

        if (currentMusicFade != null)
            StopCoroutine(currentMusicFade);

        currentMusicFade = StartCoroutine(FadeToNewMusic(entry.clip, targetVolume, fadeTime));
    }

    private IEnumerator FadeToNewMusic(AudioClip newClip, float targetVolume, float fadeTime)
    {
        if (musicSource.isPlaying && musicSource.clip != null)
        {
            float startVol = musicSource.volume;
            float t = 0f;

            while (t < fadeTime)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
                yield return null;
            }

            musicSource.Stop();
        }

        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.loop = true;
        musicSource.Play();

        float tIn = 0f;
        while (tIn < fadeTime)
        {
            tIn += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, tIn / fadeTime);
            yield return null;
        }

        musicSource.volume = targetVolume;
        currentMusicFade = null;
    }

    [YarnCommand("stop_music")]
    public void StopMusic(float fadeTime = 1f)
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        if (currentMusicFade != null)
            StopCoroutine(currentMusicFade);

        currentMusicFade = StartCoroutine(FadeOutMusic(fadeTime));
    }

    private IEnumerator FadeOutMusic(float fadeTime)
    {
        float startVol = musicSource.volume;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        musicSource.Stop();
        currentMusicFade = null;
    }

    // ========================
    // SFX COMMANDS
    // ========================

    [YarnCommand("play_sound")]
    public void PlaySFX(string clipName, float volume = -1f)
    {
        if (sfxSource == null)
        {
            Debug.LogWarning("AudioManager: No SFX source assigned!");
            return;
        }

        if (!sfxDict.TryGetValue(clipName.ToLower(), out var entry))
        {
            Debug.LogWarning($"AudioManager: No SFX clip found with name '{clipName}'");
            return;
        }

        float finalVolume = (volume >= 0f) ? Mathf.Clamp01(volume) : entry.volume;
        sfxSource.PlayOneShot(entry.clip, finalVolume);
    }

    public void PlaySFXWithPitch(string clipName, float pitch = 1f, float volume = -1f)
    {
        if (sfxSource == null)
        {
            Debug.LogWarning("AudioManager: No SFX source assigned!");
            return;
        }

        if (!sfxDict.TryGetValue(clipName.ToLower(), out var entry))
        {
            Debug.LogWarning($"AudioManager: No SFX clip found with name '{clipName}'");
            return;
        }

        StartCoroutine(PlaySFXWithPitchCoroutine(entry, pitch, volume));
    }

    private IEnumerator PlaySFXWithPitchCoroutine(AudioEntry entry, float pitch, float volume)
    {
        float originalPitch = sfxSource.pitch;
        float originalVolume = sfxSource.volume;
        AudioClip originalClip = sfxSource.clip;
        
        // Set pitch and volume, then play
        sfxSource.pitch = pitch;
        float finalVolume = (volume >= 0f) ? Mathf.Clamp01(volume) : entry.volume;
        sfxSource.volume = finalVolume;
        sfxSource.clip = entry.clip;
        sfxSource.Play();
        
        // Wait for the clip to finish
        while (sfxSource.isPlaying && sfxSource.clip == entry.clip)
        {
            yield return null;
        }
        
        // Restore original settings
        sfxSource.pitch = originalPitch;
        sfxSource.volume = originalVolume;
        sfxSource.clip = originalClip;
    }

    [YarnCommand("stop_sound")]
    public void StopSFX()
    {
        sfxSource?.Stop();
    }

    // ========================
    // VOICE COMMANDS
    // ========================

    [YarnCommand("play_voice")]
    public void PlayVoice(string clipName, float volume = -1f, bool interrupt = true)
    {
        if (voiceSource == null)
        {
            Debug.LogWarning("AudioManager: No voice source assigned!");
            return;
        }

        if (!voiceDict.TryGetValue(clipName.ToLower(), out var entry))
        {
            Debug.LogWarning($"AudioManager: No voice clip found with name '{clipName}'");
            return;
        }

        float finalVolume = (volume >= 0f) ? Mathf.Clamp01(volume) : entry.volume;

        if (!interrupt && voiceSource.isPlaying)
        {
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = entry.clip;
        voiceSource.volume = finalVolume;
        voiceSource.loop = false;
        voiceSource.Play();
    }

    [YarnCommand("stop_voice")]
    public void StopVoice()
    {
        voiceSource?.Stop();
    }

    // ========================
    // HELPERS
    // ========================

    public void PlayHoverSound()
    {
        PlaySFX("hover");
    }

    public void PlayUISound()
    {
        PlaySFX("ui");
    }

    public void StopAllAudio()
    {
        StopMusic();
        StopSFX();
        StopVoice();
    }
}