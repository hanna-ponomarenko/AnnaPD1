using UnityEngine;

namespace Featurehole.Runner.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameSfxManager : MonoBehaviour
    {
        [SerializeField] private AudioClip rockImpactClip;
        [SerializeField] private AudioClip loseClip;
        [SerializeField] [Range(0f, 1f)] private float impactVolume = 0.9f;
        [SerializeField] [Range(0f, 1f)] private float loseVolume = 1f;

        private AudioSource audioSource;

        public void Configure(AudioClip impact, AudioClip lose, float impactClipVolume = 0.9f, float loseClipVolume = 1f)
        {
            rockImpactClip = impact;
            loseClip = lose;
            impactVolume = Mathf.Clamp01(impactClipVolume);
            loseVolume = Mathf.Clamp01(loseClipVolume);
            EnsureAudioSource();
        }

        private void Awake()
        {
            EnsureAudioSource();
        }

        public void PlayRockImpact()
        {
            if (rockImpactClip == null)
            {
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(rockImpactClip, impactVolume);
        }

        public void PlayLose()
        {
            if (loseClip == null)
            {
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(loseClip, loseVolume);
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

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
    }
}
