using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class RockObstacle : MonoBehaviour
    {
        private Transform visualTransform;

        public float CollisionRadius { get; private set; } = 0.6f;

        public void Move(float deltaZ)
        {
            transform.position += new Vector3(0f, 0f, deltaZ);
        }

        public void SetPosition(Vector3 worldPosition, float sizeScale)
        {
            transform.position = worldPosition;
            transform.localScale = Vector3.one * sizeScale;
            CollisionRadius = 0.55f * sizeScale;
            gameObject.SetActive(true);
        }

        public void SetVisual(Transform visual)
        {
            visualTransform = visual;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
