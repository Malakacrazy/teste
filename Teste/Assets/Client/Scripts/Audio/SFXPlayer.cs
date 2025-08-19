using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles background music with crossfading and smooth transitions
/// Optimized for mobile with memory-efficient track management
/// </summary>
[System.Serializable]
public class MusicTrack
{
    [Header("Basic Settings")]
    public string name;
    public AudioClip clip;
    
    [Header("Playback Settings")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = true;
    
    [Header("Transition Settings")]
    public float fadeInTime = 1f;
    public float fadeOutTime = 1f;
    public bool allowCrossfade = true;
    
    [Header("Loop Settings")]
    public bool hasIntro = false;
    public float introLength = 0f; // Time after which to start looping
    public float loopStartTime = 0f; // Where to jump back to when looping
    
    [Header("Metadata")]
    [TextArea(2, 4)] public string description;
    public string composer;
    public string mood; // e.g., "calm", "intense", "victory"
}

public class MusicPlayer : MonoBehaviour
{
    [Header("Music Library")]
    public List<MusicTrack> musicTracks = new List<MusicTrack>();
    
    [Header("Crossfade Settings")]
    public float defaultCrossfadeDuration = 2f;
    public AnimationCurve crossfadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool enableSmoothing = true;
    
    [Header("Advanced Settings")]
    public bool enableDebugLogs = false;
    public bool persistAcrossScenes = true;
    public float volumeChangeSpeed = 2f;
    
    // Private fields
    private AudioSource primarySource;
    private AudioSource secondarySource;
    private MusicTrack currentTrack;
    private MusicTrack queuedTrack;
    private Dictionary<string, MusicTrack> trackDictionary;
    private AudioManager audioManager;
    
    // Coroutines
    private Coroutine crossfadeCoroutine;
    private Coroutine fadeCoroutine;
    private Coroutine introLoopCoroutine;
    
    // State tracking
    private bool isPaused = false;
    private bool isFading = false;
    private float pausedTime = 0f;
    private float targetVolume = 1f;
    
    // Properties
    public bool IsInitialized { get; private set; }
    public bool IsPlaying => primarySource != null && primarySource.isPlaying && !isPaused;
    public bool IsPaused => isPaused;
    public bool IsCrossfading => crossfadeCoroutine != null;
    public string CurrentTrackName => currentTrack?.name ?? "";
    public string QueuedTrackName => queuedTrack?.name ?? "";
    public float CurrentTime => primarySource != null ? primarySource.time : 0f;
    public float CurrentLength => currentTrack?.clip != null ? currentTrack.clip.length : 0f;
    public float NormalizedTime => CurrentLength > 0 ? CurrentTime / CurrentLength : 0f;
    
    public void Initialize(AudioSource audioSource, AudioManager manager)
    {
        primarySource = audioSource;
        audioManager = manager;
        
        CreateSecondarySource();
        BuildTrackDictionary();
        
        IsInitialized = true;
        
        if (enableDebugLogs)
            Debug.Log($"MusicPlayer initialized with {musicTracks.Count} tracks");
    }
    
    private void CreateSecondarySource()
    {
        // Create secondary source for crossfading
        GameObject secondaryGO = new GameObject("SecondaryMusicSource");
        secondaryGO.transform.SetParent(transform);
        secondarySource = secondaryGO.AddComponent<AudioSource>();
        
        // Copy settings from primary source
        secondarySource.loop = primarySource.loop;
        secondarySource.playOnAwake = false;
        secondarySource.volume = 0f;
        secondarySource.priority = primarySource.priority;
        secondarySource.spatialBlend = primarySource.spatialBlend;
    }
    
    private void BuildTrackDictionary()
    {
        trackDictionary = new Dictionary<string, MusicTrack>();
        foreach (var track in musicTracks)
        {
            if (!string.IsNullOrEmpty(track.name))
            {
                if (trackDictionary.ContainsKey(track.name))
                {
                    Debug.LogWarning($"Duplicate music track name found: {track.name}. Only the first one will be used.");
                    continue;
                }
                trackDictionary[track.name] = track;
            }
        }
    }
    
    #region Public API - Playback Control
    public void PlayMusic(string trackName, bool useCrossfade = true)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("MusicPlayer not initialized. Cannot play music.");
            return;
        }
        
        if (!TryGetMusicTrack(trackName, out MusicTrack track))
            return;
        
        // Don't restart the same track unless it's not playing
        if (currentTrack != null && currentTrack.name == trackName && IsPlaying)
        {
            if (enableDebugLogs)
                Debug.Log($"Music track '{trackName}' is already playing.");
            return;
        }
        
        if (useCrossfade && track.allowCrossfade && IsPlaying)
        {
            StartCrossfade(track);
        }
        else
        {
            PlayMusicImmediate(track);
        }
        
        currentTrack = track;
        isPaused = false;
        
        if (enableDebugLogs)
            Debug.Log($"Playing music: {trackName}");
    }
    
    public void PlayMusicWithDelay(string trackName, float delay, bool useCrossfade = true)
    {
        StartCoroutine(PlayMusicDelayed(trackName, delay, useCrossfade));
    }
    
    public void QueueMusic(string trackName)
    {
        if (!TryGetMusicTrack(trackName, out MusicTrack track))
            return;
        
        queuedTrack = track;
        
        if (enableDebugLogs)
            Debug.Log($"Queued music: {trackName}");
    }
    
    public void PlayQueuedMusic(bool useCrossfade = true)
    {
        if (queuedTrack != null)
        {
            PlayMusic(queuedTrack.name, useCrossfade);
            queuedTrack = null;
        }
    }
    
    public void StopMusic(bool useFadeOut = true)
    {
        if (useFadeOut && IsPlaying)
        {
            StartFadeOut();
        }
        else
        {
            StopMusicImmediate();
        }
        
        if (enableDebugLogs)
            Debug.Log("Music stopped");
    }
    
    public void PauseMusic()
    {
        if (IsPlaying)
        {
            pausedTime = primarySource.time;
            primarySource.Pause();
            secondarySource.Pause();
            isPaused = true;
            
            if (enableDebugLogs)
                Debug.Log("Music paused");
        }
    }
    
    public void ResumeMusic()
    {
        if (isPaused && primarySource.clip != null)
        {
            primarySource.UnPause();
            secondarySource.UnPause();
            isPaused = false;
            
            if (enableDebugLogs)
                Debug.Log("Music resumed");
        }
    }
    
    public void RestartCurrentTrack()
    {
        if (currentTrack != null)
        {
            PlayMusicImmediate(currentTrack);
            
            if (enableDebugLogs)
                Debug.Log($"Restarted current track: {currentTrack.name}");
        }
    }
    #endregion
    
    #region Public API - Volume and Settings
    public void SetVolume(float volume, bool immediate = false)
    {
        targetVolume = Mathf.Clamp01(volume);
        
        if (immediate || !enableSmoothing)
        {
            ApplyVolumeImmediate();
        }
        else
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(SmoothVolumeChange());
        }
    }
    
    public void SetPitch(float pitch)
    {
        pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        
        if (primarySource != null)
            primarySource.pitch = pitch;
        if (secondarySource != null)
            secondarySource.pitch = pitch;
        
        if (currentTrack != null)
            currentTrack.pitch = pitch;
    }
    
    public void UpdateVolume()
    {
        ApplyVolumeImmediate();
    }
    
    public void SetLooping(bool loop)
    {
        if (primarySource != null)
            primarySource.loop = loop;
        if (secondarySource != null)
            secondarySource.loop = loop;
        
        if (currentTrack != null)
            currentTrack.loop = loop;
    }
    
    public void SeekToTime(float time)
    {
        if (primarySource != null && primarySource.clip != null)
        {
            time = Mathf.Clamp(time, 0f, primarySource.clip.length);
            primarySource.time = time;
            
            if (enableDebugLogs)
                Debug.Log($"Seeked to time: {time:F2}s");
        }
    }
    
    public void SeekToNormalizedTime(float normalizedTime)
    {
        if (primarySource != null && primarySource.clip != null)
        {
            float time = Mathf.Clamp01(normalizedTime) * primarySource.clip.length;
            SeekToTime(time);
        }
    }
    #endregion
    
    #region Private Methods - Playback
    private bool TryGetMusicTrack(string trackName, out MusicTrack track)
    {
        if (trackDictionary == null) 
            BuildTrackDictionary();
        
        if (!trackDictionary.TryGetValue(trackName, out track))
        {
            Debug.LogWarning($"Music track '{trackName}' not found in library!");
            return false;
        }
        
        if (track.clip == null)
        {
            Debug.LogError($"Music track '{trackName}' has no AudioClip assigned!");
            return false;
        }
        
        return true;
    }
    
    private void PlayMusicImmediate(MusicTrack track)
    {
        StopAllCoroutines();
        
        primarySource.clip = track.clip;
        primarySource.loop = track.loop;
        primarySource.pitch = track.pitch;
        primarySource.time = 0f;
        
        ApplyVolumeImmediate();
        primarySource.Play();
        
        secondarySource.Stop();
        secondarySource.volume = 0f;
        
        // Handle intro loop if specified
        if (track.hasIntro && track.introLength > 0f)
        {
            StartIntroLoop(track);
        }
        
        isFading = false;
    }
    
    private void StopMusicImmediate()
    {
        StopAllCoroutines();
        
        primarySource.Stop();
        secondarySource.Stop();
        
        currentTrack = null;
        queuedTrack = null;
        isPaused = false;
        isFading = false;
    }
    
    private void ApplyVolumeImmediate()
    {
        float effectiveVolume = GetEffectiveVolume();
        
        if (primarySource != null)
            primarySource.volume = effectiveVolume;
    }
    
    private float GetEffectiveVolume()
    {
        float volume = targetVolume;
        
        if (currentTrack != null)
            volume *= currentTrack.volume;
        
        if (audioManager != null)
            volume *= audioManager.EffectiveMusicVolume;
        
        return Mathf.Clamp01(volume);
    }
    #endregion
    
    #region Coroutines
    private void StartCrossfade(MusicTrack newTrack)
    {
        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);
        
        float duration = newTrack.fadeInTime > 0 ? newTrack.fadeInTime : defaultCrossfadeDuration;
        crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(newTrack, duration));
    }
    
    private IEnumerator CrossfadeCoroutine(MusicTrack newTrack, float duration)
    {
        isFading = true;
        
        // Setup secondary source with new track
        secondarySource.clip = newTrack.clip;
        secondarySource.loop = newTrack.loop;
        secondarySource.pitch = newTrack.pitch;
        secondarySource.volume = 0f;
        secondarySource.time = 0f;
        secondarySource.Play();
        
        float timer = 0f;
        float initialPrimaryVolume = primarySource.volume;
        float targetSecondaryVolume = GetEffectiveVolume();
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            float curveValue = crossfadeCurve.Evaluate(progress);
            
            // Fade out primary, fade in secondary
            primarySource.volume = initialPrimaryVolume * (1f - curveValue);
            secondarySource.volume = targetSecondaryVolume * curveValue;
            
            yield return null;
        }
        
        // Complete the crossfade
        primarySource.Stop();
        primarySource.volume = 0f;
        secondarySource.volume = targetSecondaryVolume;
        
        // Swap sources
        (primarySource, secondarySource) = (secondarySource, primarySource);
        
        // Handle intro loop for new track
        if (newTrack.hasIntro && newTrack.introLength > 0f)
        {
            StartIntroLoop(newTrack);
        }
        
        crossfadeCoroutine = null;
        isFading = false;
    }
    
    private void StartFadeOut()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        float duration = currentTrack?.fadeOutTime ?? defaultCrossfadeDuration;
        fadeCoroutine = StartCoroutine(FadeOutCoroutine(duration));
    }
    
    private IEnumerator FadeOutCoroutine(float duration)
    {
        isFading = true;
        float initialVolume = primarySource.volume;
        float timer = 0f;
        
        while (timer < duration && primarySource.isPlaying)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            primarySource.volume = initialVolume * (1f - progress);
            yield return null;
        }
        
        StopMusicImmediate();
        fadeCoroutine = null;
    }
    
    private IEnumerator SmoothVolumeChange()
    {
        float startVolume = primarySource.volume;
        float targetEffectiveVolume = GetEffectiveVolume();
        
        while (Mathf.Abs(primarySource.volume - targetEffectiveVolume) > 0.01f)
        {
            primarySource.volume = Mathf.MoveTowards(
                primarySource.volume, 
                targetEffectiveVolume, 
                volumeChangeSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        primarySource.volume = targetEffectiveVolume;
        fadeCoroutine = null;
    }
    
    private void StartIntroLoop(MusicTrack track)
    {
        if (introLoopCoroutine != null)
            StopCoroutine(introLoopCoroutine);
        
        introLoopCoroutine = StartCoroutine(IntroLoopCoroutine(track));
    }
    
    private IEnumerator IntroLoopCoroutine(MusicTrack track)
    {
        yield return new WaitForSeconds(track.introLength);
        
        // Jump back to loop start time
        if (primarySource.isPlaying && currentTrack == track)
        {
            primarySource.time = track.loopStartTime;
            primarySource.loop = true;
            
            if (enableDebugLogs)
                Debug.Log($"Intro finished for {track.name}, starting loop at {track.loopStartTime}s");
        }
        
        introLoopCoroutine = null;
    }
    
    private IEnumerator PlayMusicDelayed(string trackName, float delay, bool useCrossfade)
    {
        yield return new WaitForSeconds(delay);
        PlayMusic(trackName, useCrossfade);
    }
    #endregion
    
    #region Runtime Management
    public void AddMusicTrack(string name, AudioClip clip, float volume = 1f, bool loop = true)
    {
        var newTrack = new MusicTrack
        {
            name = name,
            clip = clip,
            volume = volume,
            loop = loop,
            pitch = 1f,
            fadeInTime = defaultCrossfadeDuration,
            fadeOutTime = defaultCrossfadeDuration,
            allowCrossfade = true
        };
        
        musicTracks.Add(newTrack);
        if (trackDictionary == null) trackDictionary = new Dictionary<string, MusicTrack>();
        trackDictionary[name] = newTrack;
        
        if (enableDebugLogs)
            Debug.Log($"Added music track: {name}");
    }
    
    public void RemoveMusicTrack(string name)
    {
        if (trackDictionary != null && trackDictionary.ContainsKey(name))
        {
            // Stop current track if it's being removed
            if (currentTrack != null && currentTrack.name == name)
            {
                StopMusic(false);
            }
            
            trackDictionary.Remove(name);
            musicTracks.RemoveAll(track => track.name == name);
            
            if (enableDebugLogs)
                Debug.Log($"Removed music track: {name}");
        }
    }
    
    public bool HasMusicTrack(string name)
    {
        if (trackDictionary == null) BuildTrackDictionary();
        return trackDictionary.ContainsKey(name);
    }
    
    public MusicTrack GetMusicTrack(string name)
    {
        if (trackDictionary == null) BuildTrackDictionary();
        return trackDictionary.TryGetValue(name, out MusicTrack track) ? track : null;
    }
    
    public List<string> GetAllTrackNames()
    {
        var names = new List<string>();
        foreach (var track in musicTracks)
        {
            if (!string.IsNullOrEmpty(track.name))
                names.Add(track.name);
        }
        return names;
    }
    
    public List<string> GetTracksByMood(string mood)
    {
        var names = new List<string>();
        foreach (var track in musicTracks)
        {
            if (!string.IsNullOrEmpty(track.name) && 
                string.Equals(track.mood, mood, System.StringComparison.OrdinalIgnoreCase))
            {
                names.Add(track.name);
            }
        }
        return names;
    }
    #endregion
    
    #region Debug and Utility
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogMusicStatus()
    {
        Debug.Log($"MusicPlayer Status:\n" +
                 $"Current Track: {CurrentTrackName}\n" +
                 $"Queued Track: {QueuedTrackName}\n" +
                 $"Is Playing: {IsPlaying}\n" +
                 $"Is Paused: {IsPaused}\n" +
                 $"Is Crossfading: {IsCrossfading}\n" +
                 $"Current Time: {CurrentTime:F2}s / {CurrentLength:F2}s\n" +
                 $"Volume: {primarySource?.volume ?? 0f}\n" +
                 $"Pitch: {primarySource?.pitch ?? 1f}");
    }
    
    public void PlayRandomTrack(bool useCrossfade = true)
    {
        if (musicTracks.Count > 0)
        {
            var randomTrack = musicTracks[Random.Range(0, musicTracks.Count)];
            PlayMusic(randomTrack.name, useCrossfade);
        }
    }
    
    public void PlayRandomTrackByMood(string mood, bool useCrossfade = true)
    {
        var moodTracks = GetTracksByMood(mood);
        if (moodTracks.Count > 0)
        {
            var randomTrack = moodTracks[Random.Range(0, moodTracks.Count)];
            PlayMusic(randomTrack, useCrossfade);
        }
    }
    #endregion
    
    private void OnDestroy()
    {
        StopAllCoroutines();
        StopMusicImmediate();
    }
    
    private void Update()
    {
        // Check if current track has ended and we have a queued track
        if (currentTrack != null && !currentTrack.loop && 
            primarySource != null && !primarySource.isPlaying && 
            !isPaused && queuedTrack != null)
        {
            PlayQueuedMusic(true);
        }
    }
}