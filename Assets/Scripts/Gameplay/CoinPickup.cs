using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class CoinPickup : MonoBehaviour
    {
        private const float BounceAmplitude = 0.22f;
        private const float BounceFrequency = 4.1f;

        private Vector3 basePosition;
        private float bounceOffset;

        private void Awake()
        {
            bounceOffset = Random.Range(0f, Mathf.PI * 2f);
            basePosition = transform.position;
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            float bounceY = Mathf.Sin(Time.time * BounceFrequency + bounceOffset) * BounceAmplitude;
            transform.position = new Vector3(basePosition.x, basePosition.y + bounceY, basePosition.z);
        }

        public void Move(float deltaZ)
        {
            basePosition += new Vector3(0f, 0f, deltaZ);
        }

        public void AttractTowards(Vector3 targetPosition, float attractionStrength, float deltaTime)
        {
            basePosition = Vector3.Lerp(basePosition, targetPosition, 1f - Mathf.Exp(-attractionStrength * deltaTime));
            transform.position = basePosition;
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
