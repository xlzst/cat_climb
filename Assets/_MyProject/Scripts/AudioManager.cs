using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyProject
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sound Clips")]
        public AudioClip jumpClip;
        public AudioClip wallKickClip;
        public AudioClip coinClip;
        public AudioClip magnetClip;
        public AudioClip crumbleClip;
        public AudioClip hitClip;
        public AudioClip streakClip;
        public AudioClip clickClip;

        [Header("BGM Clips (Per Biome)")]
        public AudioClip bgmAlley;
        public AudioClip bgmRooftops;
        public AudioClip bgmStratosphere;
        public AudioClip bgmSpace;

        [Header("Audio Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1.0f;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.65f;
        [SerializeField] private int audioChannelCount = 8;
        [SerializeField] private float bgmCrossfadeDuration = 1.1f;

        private List<AudioSource> audioSourcePool = new List<AudioSource>();
        private int currentChannelIndex = 0;
        private bool isAudioEnabled = true;
        private bool isMusicEnabled = true;

        // Dual music sources for smooth crossfading
        private AudioSource musicSourceA;
        private AudioSource musicSourceB;
        private bool usingMusicSourceA = true;
        private int currentBiomeTrackIndex = -1;
        private float crossfadeProgress = 1f;
        private float duckMultiplier = 1f;
        private float targetDuckMultiplier = 1f;
        private float targetMusicPitch = 1f;

        public bool IsAudioEnabled => isAudioEnabled;
        public bool IsMusicEnabled => isMusicEnabled;
        public float SFXVolume => sfxVolume;
        public float MusicVolume => musicVolume;

        public event Action<bool> OnAudioToggled;
        public event Action<bool> OnMusicToggled;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Load saved settings
            isAudioEnabled = PlayerPrefs.GetInt("CatClimb_AudioEnabled", 1) == 1;
            isMusicEnabled = PlayerPrefs.GetInt("CatClimb_MusicEnabled", 1) == 1;
            sfxVolume = PlayerPrefs.GetFloat("CatClimb_SFXVolume", 1.0f);
            musicVolume = PlayerPrefs.GetFloat("CatClimb_MusicVolume", 0.65f);

#if UNITY_EDITOR
            if (jumpClip == null) jumpClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_jump.wav");
            if (wallKickClip == null) wallKickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_wallkick.wav");
            if (coinClip == null) coinClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_coin.wav");
            if (magnetClip == null) magnetClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_magnet.wav");
            if (crumbleClip == null) crumbleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_crumble.wav");
            if (hitClip == null) hitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_hit.wav");
            if (streakClip == null) streakClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_streak.wav");
            if (clickClip == null) clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_click.wav");

            if (bgmAlley == null) bgmAlley = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/bgm_alley.wav");
            if (bgmRooftops == null) bgmRooftops = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/bgm_rooftops.wav");
            if (bgmStratosphere == null) bgmStratosphere = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/bgm_stratosphere.wav");
            if (bgmSpace == null) bgmSpace = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/bgm_space.wav");
#endif

            EnsureRuntimeBGMFallbacks();

            // Pre-allocate pooled SFX AudioSource channels (zero runtime allocations)
            for (int i = 0; i < audioChannelCount; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D flat audio
                audioSourcePool.Add(src);
            }

            // Pre-allocate dual looping BGM channels for crossfading
            musicSourceA = CreateMusicSource();
            musicSourceB = CreateMusicSource();
        }

        private AudioSource CreateMusicSource()
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
            return src;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }

            PlayBiomeMusic(0, immediate: true);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                    targetDuckMultiplier = 0.30f;
                    targetMusicPitch = 0.94f;
                    break;
                case GameState.Revive:
                    targetDuckMultiplier = 0.25f;
                    targetMusicPitch = 0.90f;
                    break;
                case GameState.GameOver:
                    targetDuckMultiplier = 0.42f;
                    targetMusicPitch = 0.96f;
                    break;
                case GameState.MainMenu:
                    targetDuckMultiplier = 0.85f;
                    targetMusicPitch = 1.0f;
                    PlayBiomeMusic(0);
                    break;
                case GameState.Playing:
                default:
                    targetDuckMultiplier = 1.0f;
                    targetMusicPitch = 1.0f;
                    break;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            duckMultiplier = Mathf.MoveTowards(duckMultiplier, targetDuckMultiplier, dt * 3.5f);

            if (crossfadeProgress < 1f)
            {
                crossfadeProgress = Mathf.MoveTowards(crossfadeProgress, 1f, dt / Mathf.Max(0.1f, bgmCrossfadeDuration));
            }

            UpdateMusicSourcesVolumeAndPitch(dt);
        }

        private void UpdateMusicSourcesVolumeAndPitch(float dt)
        {
            AudioSource activeSrc = usingMusicSourceA ? musicSourceA : musicSourceB;
            AudioSource fadingSrc = usingMusicSourceA ? musicSourceB : musicSourceA;

            float baseTargetVol = isMusicEnabled ? (musicVolume * duckMultiplier) : 0f;

            if (activeSrc != null)
            {
                activeSrc.volume = baseTargetVol * crossfadeProgress;
                activeSrc.pitch = Mathf.MoveTowards(activeSrc.pitch, targetMusicPitch, dt * 2.5f);
            }

            if (fadingSrc != null)
            {
                fadingSrc.volume = baseTargetVol * (1f - crossfadeProgress);
                fadingSrc.pitch = Mathf.MoveTowards(fadingSrc.pitch, targetMusicPitch, dt * 2.5f);
                if (crossfadeProgress >= 1f && fadingSrc.isPlaying)
                {
                    fadingSrc.Stop();
                }
            }
        }

        public void PlayBiomeMusic(int biomeIndex, bool immediate = false)
        {
            int clampedIndex = Mathf.Clamp(biomeIndex, 0, 3);
            if (clampedIndex == currentBiomeTrackIndex && !immediate) return;

            AudioClip targetClip = GetBiomeClip(clampedIndex);
            if (targetClip == null) return;

            currentBiomeTrackIndex = clampedIndex;

            if (immediate)
            {
                usingMusicSourceA = true;
                crossfadeProgress = 1f;
                if (musicSourceB != null) musicSourceB.Stop();
                if (musicSourceA != null)
                {
                    musicSourceA.clip = targetClip;
                    musicSourceA.volume = isMusicEnabled ? (musicVolume * duckMultiplier) : 0f;
                    if (isMusicEnabled && !musicSourceA.isPlaying)
                    {
                        musicSourceA.Play();
                    }
                }
                return;
            }

            usingMusicSourceA = !usingMusicSourceA;
            AudioSource nextSrc = usingMusicSourceA ? musicSourceA : musicSourceB;
            if (nextSrc != null)
            {
                nextSrc.clip = targetClip;
                nextSrc.volume = 0f;
                if (isMusicEnabled)
                {
                    nextSrc.Play();
                }
            }
            crossfadeProgress = 0f;
        }

        private AudioClip GetBiomeClip(int biomeIndex)
        {
            switch (biomeIndex)
            {
                case 0: return bgmAlley;
                case 1: return bgmRooftops != null ? bgmRooftops : bgmAlley;
                case 2: return bgmStratosphere != null ? bgmStratosphere : bgmAlley;
                case 3: return bgmSpace != null ? bgmSpace : bgmAlley;
                default: return bgmAlley;
            }
        }

        private void EnsureRuntimeBGMFallbacks()
        {
            if (bgmAlley == null)
            {
                bgmAlley = SynthesizeRuntimeLoop("bgm_alley_runtime", 132f, new float[] { 523.25f, 659.25f, 783.99f, 659.25f, 880.0f, 783.99f, 659.25f, 523.25f });
            }
            if (bgmRooftops == null)
            {
                bgmRooftops = SynthesizeRuntimeLoop("bgm_rooftops_runtime", 140f, new float[] { 440.0f, 523.25f, 659.25f, 880.0f, 783.99f, 659.25f, 523.25f, 440.0f });
            }
            if (bgmStratosphere == null)
            {
                bgmStratosphere = SynthesizeRuntimeLoop("bgm_stratosphere_runtime", 150f, new float[] { 587.33f, 698.46f, 880.0f, 1046.50f, 880.0f, 698.46f, 587.33f, 659.25f });
            }
            if (bgmSpace == null)
            {
                bgmSpace = SynthesizeRuntimeLoop("bgm_space_runtime", 156f, new float[] { 783.99f, 1046.50f, 987.77f, 783.99f, 659.25f, 783.99f, 1046.50f, 783.99f });
            }
        }

        private AudioClip SynthesizeRuntimeLoop(string clipName, float bpm, float[] notes)
        {
            int sampleRate = 22050;
            float stepSec = (60f / bpm) * 0.5f;
            int totalSamples = Mathf.Max(1, Mathf.RoundToInt(notes.Length * stepSec * sampleRate));
            float[] data = new float[totalSamples];
            float phase = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int step = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / stepSec));
                float stepPos = (t - step * stepSec) / stepSec;
                float freq = notes[step];
                float env = Mathf.Max(0f, 1f - stepPos * 0.65f);
                phase += freq / sampleRate;
                float sq = (phase % 1f) < 0.25f ? 1f : -1f;
                data[i] = sq * 0.16f * env;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void SetAudioEnabled(bool enabled)
        {
            isAudioEnabled = enabled;
            PlayerPrefs.SetInt("CatClimb_AudioEnabled", isAudioEnabled ? 1 : 0);
            PlayerPrefs.Save();
            OnAudioToggled?.Invoke(isAudioEnabled);
        }

        public void ToggleAudio()
        {
            SetAudioEnabled(!isAudioEnabled);
        }

        public void SetMusicEnabled(bool enabled)
        {
            isMusicEnabled = enabled;
            PlayerPrefs.SetInt("CatClimb_MusicEnabled", isMusicEnabled ? 1 : 0);
            PlayerPrefs.Save();

            AudioSource activeSrc = usingMusicSourceA ? musicSourceA : musicSourceB;
            if (activeSrc != null)
            {
                if (isMusicEnabled && !activeSrc.isPlaying && activeSrc.clip != null)
                {
                    activeSrc.Play();
                }
                else if (!isMusicEnabled && activeSrc.isPlaying)
                {
                    activeSrc.Pause();
                }
            }

            OnMusicToggled?.Invoke(isMusicEnabled);
        }

        public void ToggleMusic()
        {
            SetMusicEnabled(!isMusicEnabled);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("CatClimb_SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("CatClimb_MusicVolume", musicVolume);
            PlayerPrefs.Save();
        }

        private void PlayClip(AudioClip clip, float volumeScale = 1.0f, float pitchVariation = 0.05f)
        {
            if (!isAudioEnabled || clip == null || audioSourcePool.Count == 0) return;

            // Round-robin selection through pre-allocated channels
            AudioSource channel = audioSourcePool[currentChannelIndex];
            currentChannelIndex = (currentChannelIndex + 1) % audioSourcePool.Count;

            // Apply organic micro-pitch variation to prevent ear fatigue on repetitive actions
            float pitch = 1.0f;
            if (pitchVariation > 0f)
            {
                pitch += UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            }

            channel.pitch = pitch;
            channel.volume = sfxVolume * volumeScale;
            channel.PlayOneShot(clip);
        }

        // Gameplay Audio Triggers
        public void PlayJump()
        {
            PlayClip(jumpClip, 0.9f, 0.08f);
        }

        public void PlayWallKick()
        {
            PlayClip(wallKickClip, 0.95f, 0.06f);
        }

        public void PlayCoin()
        {
            PlayClip(coinClip, 0.85f, 0.04f);
        }

        public void PlayMagnet()
        {
            PlayClip(magnetClip, 1.0f, 0.02f);
        }

        public void PlayCrumble()
        {
            PlayClip(crumbleClip, 0.95f, 0.08f);
        }

        public void PlayHit()
        {
            PlayClip(hitClip, 1.0f, 0.04f);
        }

        public void PlayStreak()
        {
            PlayClip(streakClip, 1.0f, 0f);
        }

        public void PlayClick()
        {
            PlayClip(clickClip, 0.7f, 0.05f);
        }
    }
}
