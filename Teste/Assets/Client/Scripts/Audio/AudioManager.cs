using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main controller for the entire audio system
/// Manages volume settings, audio players, and provides a unified API
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    
    [Header("Audio Players")]
    public MusicPlayer musicPlayer;
    public SFXPlayer sfxPlayer;
    
    [Header("Audio Sources")]
    public AudioSource musicAudioSource;
    public AudioSource sfxAudioSource;
    
    [Header("Audio Settings")]
    public bool musicEnabled = true;
    public bool sfxEnabled = true;
    
    // Events for volume changes
    public System.Action<float> OnMasterVolumeChanged;
    public System.Action<float> OnMusicVolumeChanged;
    public System.Action<float> OnSFXVolumeChanged;
    public System.Action<bool> OnMusicEnabledChanged;
    public System.Action<bool> OnSFXEnabledChanged;
    
    // Properties for easy access
    public bool IsInitialized { get; private set; }
    public float EffectiveMusicVolume => masterVolume * musicVolume * (musicEnabled ? 1f : 0f);
    public float EffectiveSFXVolume => masterVolume * sfxVolume * (sfxEnabled ? 1f : 0f);
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSystem();
        }
        else
        {
            Debug.Log("AudioManager instance already exists. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Ensure initialization happens after all components are ready
        if (Instance == this && !IsInitialized)
        {
            InitializeAudioSystem();
        }
    }
    
    private void InitializeAudioSystem()
    {
        Debug.Log("Initializing Audio System...");
        
        CreateAudioSources();
        InitializePlayers();
        LoadSettings();
        UpdateAllVolumes();
        
        IsInitialized = true;
        Debug.Log("Audio System initialized successfully.");
    }
    
    private void CreateAudioSources()
    {
        // Create music audio source if not assigned
        if (musicAudioSource == null)
        {
            GameObject musicGO = new GameObject("MusicAudioSource");
            musicGO.transform.SetParent(transform);
            musicAudioSource = musicGO.AddComponent<AudioSource>();
            musicAudioSource.loop = true;
            musicAudioSource.playOnAwake = false;
            musicAudioSource.priority = 64; // Lower priority for music
        }
        
        // Create SFX audio source if not assigned
        if (sfxAudioSource == null)
        {
            GameObject sfxGO = new GameObject("SFXAudioSource");
            sfxGO.transform.SetParent(transform);
            sfxAudioSource = sfxGO.AddComponent<AudioSource>();
            sfxAudioSource.loop = false;
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.priority = 128; // Higher priority for SFX
        }
    }
    
    private void InitializePlayers()
    {
        // Initialize music player
        if (musicPlayer == null)
            musicPlayer = GetComponent<MusicPlayer>() ?? gameObject.AddComponent<MusicPlayer>();
        
        // Initialize SFX player
        if (sfxPlayer == null)
            sfxPlayer = GetComponent<SFXPlayer>() ?? gameObject.AddComponent<SFXPlayer>();
        
        // Initialize players with audio sources
        musicPlayer.Initialize(musicAudioSource, this);
        sfxPlayer.Initialize(sfxAudioSource, this);
    }
    
    #region Volume Control
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        OnMasterVolumeChanged?.Invoke(masterVolume);
        SaveSettings();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateMusicVolume();
        OnMusicVolumeChanged?.Invoke(musicVolume);
        SaveSettings();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateSFXVolume();
        OnSFXVolumeChanged?.Invoke(sfxVolume);
        SaveSettings();
    }
    
    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        UpdateMusicVolume();
        OnMusicEnabledChanged?.Invoke(musicEnabled);
        SaveSettings();
        
        if (!enabled && musicPlayer != null)
        {
            musicPlayer.PauseMusic();
        }
        else if (enabled && musicPlayer != null && musicPlayer.IsPaused)
        {
            musicPlayer.ResumeMusic();
        }
    }
    
    public void SetSFXEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        UpdateSFXVolume();
        OnSFXEnabledChanged?.Invoke(sfxEnabled);
        SaveSettings();
        
        if (!enabled && sfxPlayer != null)
        {
            sfxPlayer.StopAllSFX();
        }
    }
    
    private void UpdateAllVolumes()
    {
        UpdateMusicVolume();
        UpdateSFXVolume();
    }
    
    public void UpdateMusicVolume()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = EffectiveMusicVolume;
        }
        
        if (musicPlayer != null)
        {
            musicPlayer.UpdateVolume();
        }
    }
    
    public void UpdateSFXVolume()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = EffectiveSFXVolume;
        }
        
        if (sfxPlayer != null)
        {
            sfxPlayer.UpdateVolume();
        }
    }
    #endregion
    
    #region Save/Load Settings
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("AudioManager_MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("AudioManager_MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("AudioManager_SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("AudioManager_MusicEnabled", musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("AudioManager_SFXEnabled", sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("AudioManager_MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("AudioManager_MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("AudioManager_SFXVolume", 0.8f);
        musicEnabled = PlayerPrefs.GetInt("AudioManager_MusicEnabled", 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt("AudioManager_SFXEnabled", 1) == 1;
    }
    #endregion
    
    #region Public API - Music Control
    public void PlayMusic(string musicName, bool useCrossfade = true)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("AudioManager not initialized yet. Cannot play music.");
            return;
        }
        
        if (!musicEnabled)
        {
            Debug.Log("Music is disabled. Skipping music playback.");
            return;
        }
        
        musicPlayer?.PlayMusic(musicName, useCrossfade);
    }
    
    public void StopMusic(bool useFadeOut = true)
    {
        musicPlayer?.StopMusic(useFadeOut);
    }
    
    public void PauseMusic()
    {
        musicPlayer?.PauseMusic();
    }
    
    public void ResumeMusic()
    {
        if (musicEnabled)
        {
            musicPlayer?.ResumeMusic();
        }
    }
    
    public void SetMusicPitch(float pitch)
    {
        musicPlayer?.SetPitch(pitch);
    }
    
    public bool IsMusicPlaying()
    {
        return musicPlayer != null && musicPlayer.IsPlaying;
    }
    
    public string GetCurrentMusicTrack()
    {
        return musicPlayer?.CurrentTrackName ?? "";
    }
    #endregion
    
    #region Public API - SFX Control
    public void PlaySFX(string sfxName)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("AudioManager not initialized yet. Cannot play SFX.");
            return;
        }
        
        if (!sfxEnabled)
        {
            return; // Silently skip SFX when disabled
        }
        
        sfxPlayer?.PlaySFX(sfxName);
    }
    
    public void PlaySFX(string sfxName, float volumeMultiplier)
    {
        if (!IsInitialized || !sfxEnabled) return;
        
        sfxPlayer?.PlaySFX(sfxName, volumeMultiplier);
    }
    
    public void PlaySFX(string sfxName, Vector3 position)
    {
        if (!IsInitialized || !sfxEnabled) return;
        
        sfxPlayer?.PlaySFXAtPosition(sfxName, position);
    }
    
    public void StopAllSFX()
    {
        sfxPlayer?.StopAllSFX();
    }
    
    // Common UI SFX shortcuts
    public void PlayUIClick() => PlaySFX("ui_click");
    public void PlayUIHover() => PlaySFX("ui_hover");
    public void PlayUIError() => PlaySFX("ui_error");
    public void PlayUISuccess() => PlaySFX("ui_success");
    public void PlayUICancel() => PlaySFX("ui_cancel");
    #endregion
    
    #region Utility Methods
    public void MuteAll()
    {
        SetMasterVolume(0f);
    }
    
    public void UnmuteAll()
    {
        SetMasterVolume(1f);
    }
    
    public void MuteMusic()
    {
        SetMusicEnabled(false);
    }
    
    public void UnmuteMusic()
    {
        SetMusicEnabled(true);
    }
    
    public void MuteSFX()
    {
        SetSFXEnabled(false);
    }
    
    public void UnmuteSFX()
    {
        SetSFXEnabled(true);
    }
    
    public void ResetToDefaults()
    {
        SetMasterVolume(1f);
        SetMusicVolume(0.7f);
        SetSFXVolume(0.8f);
        SetMusicEnabled(true);
        SetSFXEnabled(true);
    }
    
    public AudioSettings GetCurrentSettings()
    {
        return new AudioSettings
        {
            masterVolume = this.masterVolume,
            musicVolume = this.musicVolume,
            sfxVolume = this.sfxVolume,
            musicEnabled = this.musicEnabled,
            sfxEnabled = this.sfxEnabled
        };
    }
    
    public void ApplySettings(AudioSettings settings)
    {
        SetMasterVolume(settings.masterVolume);
        SetMusicVolume(settings.musicVolume);
        SetSFXVolume(settings.sfxVolume);
        SetMusicEnabled(settings.musicEnabled);
        SetSFXEnabled(settings.sfxEnabled);
    }
    #endregion
    
    #region Debug Methods
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogAudioStatus()
    {
        Debug.Log($"AudioManager Status:\n" +
                 $"Master Volume: {masterVolume}\n" +
                 $"Music Volume: {musicVolume} (Enabled: {musicEnabled})\n" +
                 $"SFX Volume: {sfxVolume} (Enabled: {sfxEnabled})\n" +
                 $"Effective Music Volume: {EffectiveMusicVolume}\n" +
                 $"Effective SFX Volume: {EffectiveSFXVolume}\n" +
                 $"Current Music: {GetCurrentMusicTrack()}\n" +
                 $"Music Playing: {IsMusicPlaying()}");
    }
    #endregion
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Pause music when app goes to background
            PauseMusic();
        }
        else
        {
            // Resume music when app comes back to foreground
            ResumeMusic();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            PauseMusic();
        }
        else
        {
            ResumeMusic();
        }
    }
}

/// <summary>
/// Data container for audio settings that can be saved/loaded
/// </summary>
[System.Serializable]
public class AudioSettings
{
    public float masterVolume = 1f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.8f;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;
    
    public void ApplyToAudioManager()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplySettings(this);
        }
    }
    
    public void LoadFromPlayerPrefs()
    {
        masterVolume = PlayerPrefs.GetFloat("AudioSettings_MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("AudioSettings_MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("AudioSettings_SFXVolume", 0.8f);
        musicEnabled = PlayerPrefs.GetInt("AudioSettings_MusicEnabled", 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt("AudioSettings_SFXEnabled", 1) == 1;
    }
    
    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetFloat("AudioSettings_MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("AudioSettings_MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("AudioSettings_SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("AudioSettings_MusicEnabled", musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("AudioSettings_SFXEnabled", sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}