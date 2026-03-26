using UnityEngine;

namespace Featurehole.Runner.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicManager : MonoBehaviour
    {
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.65f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop = true;

        private AudioSource audioSource;

        public void Configure(AudioClip musicClip, float musicVolume = 0.65f, bool shouldLoop = true, bool shouldPlayOnStart = true)
        {
            backgroundMusic = musicClip;
            volume = Mathf.Clamp01(musicVolume);
            loop = shouldLoop;
            playOnStart = shouldPlayOnStart;
            EnsureAudioSource();
            ApplySettings();
        }

        private void Awake()
        {
            EnsureAudioSource();
            ApplySettings();
        }

        private void Start()
        {
            if (!playOnStart || backgroundMusic == null)
            {
                return;
            }

            Play();
        }

        public void Play()
        {
            EnsureAudioSource();
            ApplySettings();

            if (backgroundMusic == null || audioSource.isPlaying)
            {
                return;
            }

            audioSource.Play();
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void ApplySettings()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.clip = backgroundMusic;
            audioSource.playOnAwake = false;
            audioSource.loop = loop;
            audioSource.volume = volume;
            audioSource.spatialBlend = 0f;
        }
    }
}
