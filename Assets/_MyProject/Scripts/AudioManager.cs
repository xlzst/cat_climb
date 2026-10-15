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

        [Header("Audio Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1.0f;
        [SerializeField] private int audioChannelCount = 8;

        private List<AudioSource> audioSourcePool = new List<AudioSource>();
        private int currentChannelIndex = 0;
        private bool isAudioEnabled = true;

        public bool IsAudioEnabled => isAudioEnabled;
        public float SFXVolume => sfxVolume;

        public event Action<bool> OnAudioToggled;

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
            sfxVolume = PlayerPrefs.GetFloat("CatClimb_SFXVolume", 1.0f);

#if UNITY_EDITOR
            if (jumpClip == null) jumpClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_jump.wav");
            if (wallKickClip == null) wallKickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_wallkick.wav");
            if (coinClip == null) coinClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_coin.wav");
            if (magnetClip == null) magnetClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_magnet.wav");
            if (crumbleClip == null) crumbleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_crumble.wav");
            if (hitClip == null) hitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_hit.wav");
            if (streakClip == null) streakClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_streak.wav");
            if (clickClip == null) clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_MyProject/Audio/sfx_click.wav");
#endif

            // Pre-allocate pooled AudioSource channels (zero runtime allocations)
            for (int i = 0; i < audioChannelCount; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D flat audio
                audioSourcePool.Add(src);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
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

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("CatClimb_SFXVolume", sfxVolume);
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
