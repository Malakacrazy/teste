using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles sound effects with pooling for mobile optimization
/// Supports pitch randomization, spatial audio, and volume control
/// </summary>
[System.Serializable]
public class SFXClip
{
    [Header("Basic Settings")]
    public string name;
    public AudioClip clip;
    
    [Header("Volume & Pitch")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    
    [Header("Randomization")]
    public bool randomizePitch = false;
    [Range(0f, 0.5f)] public float pitchVariation = 0.1f;
    public bool randomizeVolume = false;
    [Range(0f, 0.3f)] public float volumeVariation = 0.1f;
    
    [Header("3D Audio")]
    public bool is3D = false;
    [Range(0f, 100f)] public float maxDistance = 20f;
    [Range(0f, 100f)] public float minDistance = 1f;
    
    [Header("Cooldown")]
    public bool useCooldown = false;
    public float cooldownTime = 0.1f;
}

public class SFXPlayer : MonoBehaviour
{
    [Header("SFX Library")]
    public List<SFXClip> sfxClips = new List<SFXClip>();
    
    [Header("Pooling Settings")]
    public int maxSimultaneousSFX = 15;
    public bool enablePooling = true;
    public bool enableDebugLogs = false;
    
    [Header("3D Audio Settings")]
    public AnimationCurve spatialBlendCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve rolloffCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    // Private fields
    private Dictionary<string, SFXClip> sfxDictionary;
    private Queue<AudioSource> availableSources;
    private List<AudioSource> allSources;
    private Dictionary<string, float> lastPlayTimes;
    private AudioSource mainSFXSource;
    private AudioManager audioManager;
    
    // Properties
    public bool IsInitialized { get; private set; }
    public int ActiveSFXCount => enablePooling ? (maxSimultaneousSFX - availableSources.Count) : 0;
    
    public void Initialize(AudioSource audioSource, AudioManager manager)
    {
        mainSFXSource = audioSource;
        audioManager = manager;
        
        BuildSFXDictionary();
        InitializeCooldownTracking();
        
        if (enablePooling)
            InitializeAudioSourcePool();
        
        IsInitialized = true;
        
        if (enableDebugLogs)
            Debug.Log($"SFXPlayer initialized with {sfxClips.Count} clips and pooling {(enablePooling ? "enabled" : "disabled")}");
    }
    
    private void BuildSFXDictionary()
    {
        sfxDictionary = new Dictionary<string, SFXClip>();
        foreach (var sfx in sfxClips)
        {
            if (!string.IsNullOrEmpty(sfx.name))
            {
                if (sfxDictionary.ContainsKey(sfx.name))
                {
                    Debug.LogWarning($"Duplicate SFX name found: {sfx.name}. Only the first one will be used.");
                    continue;
                }
                sfxDictionary[sfx.name] = sfx;
            }
        }
    }
    
    private void InitializeCooldownTracking()
    {
        lastPlayTimes = new Dictionary<string, float>();
    }
    
    private void InitializeAudioSourcePool()
    {
        availableSources = new Queue<AudioSource>();
        allSources = new List<AudioSource>();
        
        // Create pool of audio sources
        for (int i = 0; i < maxSimultaneousSFX; i++)
        {
            GameObject sourceGO = new GameObject($"SFXPoolSource_{i}");
            sourceGO.transform.SetParent(transform);
            
            AudioSource source = sourceGO.AddComponent<AudioSource>();
            ConfigurePooledAudioSource(source);
            
            availableSources.Enqueue(source);
            allSources.Add(source);
        }
        
        if (enableDebugLogs)
            Debug.Log($"Audio source pool created with {maxSimultaneousSFX} sources");
    }
    
    private void ConfigurePooledAudioSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.priority = 128;
        source.spatialBlend = 0f; // Start as 2D, will be changed per clip
        source.rolloffMode = AudioRolloffMode.Custom;
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
        source.dopplerLevel = 0f; // Disable doppler for mobile performance
    }
    
    #region Public API
    public void PlaySFX(string sfxName)
    {
        PlaySFX(sfxName, 1f);
    }
    
    public void PlaySFX(string sfxName, float volumeMultiplier)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("SFXPlayer not initialized. Cannot play SFX.");
            return;
        }
        
        if (!TryGetSFXClip(sfxName, out SFXClip sfx))
            return;
        
        if (!CanPlaySFX(sfx))
            return;
        
        if (enablePooling)
            PlaySFXPooled(sfx, volumeMultiplier);
        else
            PlaySFXOneShot(sfx, volumeMultiplier);
        
        UpdateLastPlayTime(sfx.name);
    }
    
    public void PlaySFXAtPosition(string sfxName, Vector3 position)
    {
        PlaySFXAtPosition(sfxName, position, 1f);
    }
    
    public void PlaySFXAtPosition(string sfxName, Vector3 position, float volumeMultiplier)
    {
        if (!TryGetSFXClip(sfxName, out SFXClip sfx))
            return;
        
        if (!CanPlaySFX(sfx))
            return;
        
        if (enablePooling)
        {
            AudioSource source = GetAvailableAudioSource();
            if (source != null)
            {
                ConfigureAudioSource(source, sfx, volumeMultiplier);
                source.transform.position = position;
                source.spatialBlend = 1f; // Force 3D audio for positional sounds
                source.Play();
                StartCoroutine(ReturnSourceToPool(source, GetClipDuration(sfx)));
            }
        }
        else
        {
            // For one-shot, create a temporary game object at the position
            GameObject tempGO = new GameObject("TempSFX_" + sfxName);
            tempGO.transform.position = position;
            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            ConfigureAudioSource(tempSource, sfx, volumeMultiplier);
            tempSource.spatialBlend = 1f;
            tempSource.Play();
            
            // Destroy the temporary object after the clip finishes
            Destroy(tempGO, GetClipDuration(sfx) + 0.1f);
        }
        
        UpdateLastPlayTime(sfx.name);
    }
    
    public void StopAllSFX()
    {
        if (enablePooling && allSources != null)
        {
            foreach (var source in allSources)
            {
                if (source != null && source.isPlaying)
                {
                    source.Stop();
                    if (!availableSources.Contains(source))
                        availableSources.Enqueue(source);
                }
            }
        }
        
        if (mainSFXSource != null)
        {
            mainSFXSource.Stop();
        }
        
        if (enableDebugLogs)
            Debug.Log("All SFX stopped");
    }
    
    public void StopSFX(string sfxName)
    {
        if (enablePooling && allSources != null)
        {
            foreach (var source in allSources)
            {
                if (source != null && source.isPlaying && 
                    source.clip != null && source.clip.name.Contains(sfxName))
                {
                    source.Stop();
                    if (!availableSources.Contains(source))
                        availableSources.Enqueue(source);
                }
            }
        }
    }
    
    public void UpdateVolume()
    {
        float effectiveVolume = audioManager != null ? audioManager.EffectiveSFXVolume : 1f;
        
        if (enablePooling && allSources != null)
        {
            foreach (var source in allSources)
            {
                if (source != null && source.isPlaying)
                {
                    // Update volume while preserving the original volume multiplier
                    // This is a simplified approach; you might want to store original volumes
                    source.volume = effectiveVolume;
                }
            }
        }
        
        if (mainSFXSource != null)
        {
            mainSFXSource.volume = effectiveVolume;
        }
    }
    #endregion
    
    #region Private Methods
    private bool TryGetSFXClip(string sfxName, out SFXClip sfx)
    {
        if (sfxDictionary == null) 
            BuildSFXDictionary();
        
        if (!sfxDictionary.TryGetValue(sfxName, out sfx))
        {
            if (enableDebugLogs)
                Debug.LogWarning($"SFX '{sfxName}' not found in library!");
            return false;
        }
        
        if (sfx.clip == null)
        {
            Debug.LogError($"SFX '{sfxName}' has no AudioClip assigned!");
            return false;
        }
        
        return true;
    }
    
    private bool CanPlaySFX(SFXClip sfx)
    {
        if (!sfx.useCooldown)
            return true;
        
        if (lastPlayTimes.TryGetValue(sfx.name, out float lastTime))
        {
            return (Time.time - lastTime) >= sfx.cooldownTime;
        }
        
        return true;
    }
    
    private void UpdateLastPlayTime(string sfxName)
    {
        lastPlayTimes[sfxName] = Time.time;
    }
    
    private void PlaySFXPooled(SFXClip sfx, float volumeMultiplier)
    {
        AudioSource source = GetAvailableAudioSource();
        if (source == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"No available audio sources for SFX '{sfx.name}'. Consider increasing pool size.");
            return;
        }
        
        ConfigureAudioSource(source, sfx, volumeMultiplier);
        source.Play();
        
        StartCoroutine(ReturnSourceToPool(source, GetClipDuration(sfx)));
    }
    
    private void PlaySFXOneShot(SFXClip sfx, float volumeMultiplier)
    {
        float pitch = CalculatePitch(sfx);
        float volume = CalculateVolume(sfx, volumeMultiplier);
        
        mainSFXSource.pitch = pitch;
        mainSFXSource.PlayOneShot(sfx.clip, volume);
    }
    
    private void ConfigureAudioSource(AudioSource source, SFXClip sfx, float volumeMultiplier)
    {
        source.clip = sfx.clip;
        source.volume = CalculateVolume(sfx, volumeMultiplier);
        source.pitch = CalculatePitch(sfx);
        
        // Configure 3D settings
        if (sfx.is3D)
        {
            source.spatialBlend = 1f;
            source.minDistance = sfx.minDistance;
            source.maxDistance = sfx.maxDistance;
        }
        else
        {
            source.spatialBlend = 0f;
        }
    }
    
    private float CalculatePitch(SFXClip sfx)
    {
        float pitch = sfx.pitch;
        
        if (sfx.randomizePitch && sfx.pitchVariation > 0f)
        {
            float variation = Random.Range(-sfx.pitchVariation, sfx.pitchVariation);
            pitch += variation;
        }
        
        return Mathf.Clamp(pitch, 0.1f, 3f);
    }
    
    private float CalculateVolume(SFXClip sfx, float volumeMultiplier)
    {
        float volume = sfx.volume * volumeMultiplier;
        
        if (sfx.randomizeVolume && sfx.volumeVariation > 0f)
        {
            float variation = Random.Range(-sfx.volumeVariation, sfx.volumeVariation);
            volume += variation;
        }
        
        // Apply master volume
        if (audioManager != null)
        {
            volume *= audioManager.EffectiveSFXVolume;
        }
        
        return Mathf.Clamp01(volume);
    }
    
    private float GetClipDuration(SFXClip sfx)
    {
        if (sfx.clip == null) return 0f;
        return sfx.clip.length / Mathf.Abs(sfx.pitch);
    }
    
    private AudioSource GetAvailableAudioSource()
    {
        // Check if any sources are available in the queue
        if (availableSources.Count > 0)
            return availableSources.Dequeue();
        
        // Check if any sources have finished playing
        foreach (var source in allSources)
        {
            if (source != null && !source.isPlaying)
                return source;
        }
        
        return null; // All sources are busy
    }
    
    private IEnumerator ReturnSourceToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f); // Small buffer
        
        if (source != null)
        {
            source.Stop();
            source.clip = null;
            source.transform.position = transform.position; // Reset position
            source.spatialBlend = 0f; // Reset to 2D
            
            if (!availableSources.Contains(source))
                availableSources.Enqueue(source);
        }
    }
    #endregion
    
    #region Runtime Management
    public void AddSFXClip(string name, AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        var newSFX = new SFXClip
        {
            name = name,
            clip = clip,
            volume = volume,
            pitch = pitch
        };
        
        sfxClips.Add(newSFX);
        if (sfxDictionary == null) sfxDictionary = new Dictionary<string, SFXClip>();
        sfxDictionary[name] = newSFX;
        
        if (enableDebugLogs)
            Debug.Log($"Added SFX clip: {name}");
    }
    
    public void RemoveSFXClip(string name)
    {
        if (sfxDictionary != null && sfxDictionary.ContainsKey(name))
        {
            sfxDictionary.Remove(name);
            sfxClips.RemoveAll(sfx => sfx.name == name);
            
            if (enableDebugLogs)
                Debug.Log($"Removed SFX clip: {name}");
        }
    }
    
    public bool HasSFXClip(string name)
    {
        if (sfxDictionary == null) BuildSFXDictionary();
        return sfxDictionary.ContainsKey(name);
    }
    
    public SFXClip GetSFXClip(string name)
    {
        if (sfxDictionary == null) BuildSFXDictionary();
        return sfxDictionary.TryGetValue(name, out SFXClip sfx) ? sfx : null;
    }
    #endregion
    
    #region Common UI SFX Methods
    public void PlayUIClick() => PlaySFX("ui_click");
    public void PlayUIHover() => PlaySFX("ui_hover");
    public void PlayUIError() => PlaySFX("ui_error");
    public void PlayUISuccess() => PlaySFX("ui_success");
    public void PlayUICancel() => PlaySFX("ui_cancel");
    public void PlayUIConfirm() => PlaySFX("ui_confirm");
    public void PlayUIOpen() => PlaySFX("ui_open");
    public void PlayUIClose() => PlaySFX("ui_close");
    public void PlayUINotification() => PlaySFX("ui_notification");
    public void PlayUIWarning() => PlaySFX("ui_warning");
    #endregion
    
    #region Debug and Utility
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogSFXStatus()
    {
        Debug.Log($"SFXPlayer Status:\n" +
                 $"Total SFX Clips: {sfxClips.Count}\n" +
                 $"Pooling Enabled: {enablePooling}\n" +
                 $"Max Simultaneous SFX: {maxSimultaneousSFX}\n" +
                 $"Active SFX Count: {ActiveSFXCount}\n" +
                 $"Available Sources: {(availableSources?.Count ?? 0)}");
    }
    
    public void SetPoolSize(int newSize)
    {
        if (!enablePooling) return;
        
        newSize = Mathf.Clamp(newSize, 1, 50);
        
        if (newSize > maxSimultaneousSFX)
        {
            // Add more sources
            int toAdd = newSize - maxSimultaneousSFX;
            for (int i = 0; i < toAdd; i++)
            {
                GameObject sourceGO = new GameObject($"SFXPoolSource_{maxSimultaneousSFX + i}");
                sourceGO.transform.SetParent(transform);
                
                AudioSource source = sourceGO.AddComponent<AudioSource>();
                ConfigurePooledAudioSource(source);
                
                availableSources.Enqueue(source);
                allSources.Add(source);
            }
        }
        else if (newSize < maxSimultaneousSFX)
        {
            // Remove excess sources
            int toRemove = maxSimultaneousSFX - newSize;
            for (int i = 0; i < toRemove && availableSources.Count > 0; i++)
            {
                AudioSource source = availableSources.Dequeue();
                allSources.Remove(source);
                if (source != null)
                    DestroyImmediate(source.gameObject);
            }
        }
        
        maxSimultaneousSFX = newSize;
        
        if (enableDebugLogs)
            Debug.Log($"SFX pool size changed to: {newSize}");
    }
    
    public void ClearCooldowns()
    {
        lastPlayTimes?.Clear();
        if (enableDebugLogs)
            Debug.Log("SFX cooldowns cleared");
    }
    #endregion
    
    private void OnDestroy()
    {
        StopAllSFX();
    }
}