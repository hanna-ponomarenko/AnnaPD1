using Featurehole.Runner.Data;
using UnityEngine;

namespace Featurehole.Runner.Hole
{
    public sealed class HoleMover : MonoBehaviour
    {
        [SerializeField] private string horizontalAxis = "Horizontal";

        private RunnerGameConfig config;
        private Vector3 startPosition;

        public void Initialize(RunnerGameConfig runnerConfig)
        {
            config = runnerConfig;
            startPosition = transform.position;
            transform.position = startPosition;
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
    }
}
