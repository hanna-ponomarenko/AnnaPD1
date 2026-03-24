using Featurehole.Runner.Data;
using UnityEngine;

namespace Featurehole.Runner.Hole
{
    public sealed class HoleMover : MonoBehaviour
    {
        [SerializeField] private string horizontalAxis = "Horizontal";

        private RunnerGameConfig config;
        private Vector3 startPosition;
        private Transform visualTransform;
        private float currentDiameter;

        public float CurrentDiameter => currentDiameter;

        public void Initialize(RunnerGameConfig runnerConfig)
        {
            config = runnerConfig;
            startPosition = transform.position;
            visualTransform = transform.Find("Visual");
            transform.position = startPosition;
            ResetSize();
        }

        public void ResetPosition()
        {
            transform.position = startPosition;
        }

        public void ResetSize()
        {
            if (config == null)
            {
                return;
            }

            currentDiameter = config.HoleDiameter;
            ApplyVisualScale();
        }

        public void Grow()
        {
            if (config == null)
            {
                return;
            }

            currentDiameter = Mathf.Min(currentDiameter + config.GrowthPerCollectible, config.MaxHoleDiameter);
            ApplyVisualScale();
        }

        public void Tick(float deltaTime)
        {
            if (config == null)
            {
                return;
            }

            float horizontalInput = Input.GetAxisRaw(horizontalAxis);
            Vector3 position = transform.position;

            position.x += horizontalInput * config.LateralSpeed * deltaTime;
            position.x = Mathf.Clamp(position.x, -config.LateralLimit, config.LateralLimit);
            position.y = startPosition.y;

            transform.position = position;
        }

        private void ApplyVisualScale()
        {
            if (visualTransform == null)
            {
                return;
            }

            visualTransform.localPosition = Vector3.zero;
            visualTransform.localScale = new Vector3(currentDiameter, 0.08f, currentDiameter);
        }
    }
}
