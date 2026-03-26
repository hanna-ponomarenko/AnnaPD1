using UnityEngine;

namespace Featurehole.Runner.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameSfxManager : MonoBehaviour
    {
        [SerializeField] private AudioClip rockImpactClip;
        [SerializeField] private AudioClip loseClip;
        [SerializeField] private AudioClip coinPickupClip;
        [SerializeField] private AudioClip magnetPickupClip;
        [SerializeField] private AudioClip applePickupClip;
        [SerializeField] private AudioClip pepperPickupClip;
        [SerializeField] [Range(0f, 1f)] private float impactVolume = 0.9f;
        [SerializeField] [Range(0f, 1f)] private float loseVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float coinPickupVolume = 0.75f;
        [SerializeField] [Range(0f, 1f)] private float magnetPickupVolume = 0.9f;
        [SerializeField] [Range(0f, 1f)] private float applePickupVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float pepperPickupVolume = 0.85f;

        private AudioSource audioSource;

        public void Configure(
            AudioClip impact,
            AudioClip lose,
            AudioClip coinPickup,
            AudioClip magnetPickup,
            AudioClip applePickup,
            AudioClip pepperPickup,
            float impactClipVolume = 0.9f,
            float loseClipVolume = 1f,
            float coinVolume = 0.75f,
            float magnetVolume = 0.9f,
            float appleVolume = 0.8f,
            float pepperVolume = 0.85f)
        {
            rockImpactClip = impact;
            loseClip = lose;
            coinPickupClip = coinPickup;
            magnetPickupClip = magnetPickup;
            applePickupClip = applePickup;
            pepperPickupClip = pepperPickup;
            impactVolume = Mathf.Clamp01(impactClipVolume);
            loseVolume = Mathf.Clamp01(loseClipVolume);
            coinPickupVolume = Mathf.Clamp01(coinVolume);
            magnetPickupVolume = Mathf.Clamp01(magnetVolume);
            applePickupVolume = Mathf.Clamp01(appleVolume);
            pepperPickupVolume = Mathf.Clamp01(pepperVolume);
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

        public void PlayCoinPickup()
        {
            if (coinPickupClip == null)
            {
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(coinPickupClip, coinPickupVolume);
        }

        public void PlayMagnetPickup()
        {
            if (magnetPickupClip == null)
            {
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(magnetPickupClip, magnetPickupVolume);
        }

        public void PlayApplePickup()
        {
            if (applePickupClip == null)
            {
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(applePickupClip, applePickupVolume);
        }

        public void PlayPepperPickup()
        {
            if (pepperPickupClip == null)
            {
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(pepperPickupClip, pepperPickupVolume);
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
