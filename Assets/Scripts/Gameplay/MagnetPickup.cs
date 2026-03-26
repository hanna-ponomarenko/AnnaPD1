using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class MagnetPickup : MonoBehaviour
    {
        private const float BounceAmplitude = 0.28f;
        private const float BounceFrequency = 3.4f;
        private const float GlowPulseFrequency = 4.6f;

        private Vector3 basePosition;
        private float bounceOffset;
        private SpriteRenderer[] renderers;
        private Color[] baseColors;

        private void Awake()
        {
            bounceOffset = Random.Range(0f, Mathf.PI * 2f);
            basePosition = transform.position;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseColors = new Color[renderers.Length];

            for (int index = 0; index < renderers.Length; index++)
            {
                baseColors[index] = renderers[index].color;
            }
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            float bounceY = Mathf.Sin(Time.time * BounceFrequency + bounceOffset) * BounceAmplitude;
            transform.position = new Vector3(basePosition.x, basePosition.y + bounceY, basePosition.z);

            float glowPulse = 0.78f + 0.22f * Mathf.Sin(Time.time * GlowPulseFrequency + bounceOffset);
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].color = baseColors[index] * glowPulse;
            }
        }

        public void Move(float deltaZ)
        {
            basePosition += new Vector3(0f, 0f, deltaZ);
        }

        public void SetPosition(Vector3 worldPosition)
        {
            basePosition = worldPosition;
            transform.position = worldPosition;
            gameObject.SetActive(true);
        }

        public void Collect()
        {
            gameObject.SetActive(false);
        }
    }
}
