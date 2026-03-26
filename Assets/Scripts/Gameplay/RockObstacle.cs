using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class RockObstacle : MonoBehaviour
    {
        private Transform visualTransform;
        private float baseCollisionRadius = 0.6f;

        public float CollisionRadius { get; private set; } = 0.6f;

        public void Move(float deltaZ)
        {
            transform.position += new Vector3(0f, 0f, deltaZ);
        }

        public void SetPosition(Vector3 worldPosition, float sizeScale)
        {
            transform.position = worldPosition;
            transform.localScale = Vector3.one * sizeScale;
            CollisionRadius = baseCollisionRadius * sizeScale;
            gameObject.SetActive(true);
        }

        public void SetVisual(Transform visual)
        {
            visualTransform = visual;
            FitVisualToGround();
            RecalculateCollisionRadius();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void FitVisualToGround()
        {
            if (visualTransform == null)
            {
                return;
            }

            Renderer[] renderers = visualTransform.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            float minY = float.MaxValue;
            foreach (Renderer renderer in renderers)
            {
                minY = Mathf.Min(minY, renderer.bounds.min.y);
            }

            visualTransform.position += Vector3.up * -minY;
        }

        private void RecalculateCollisionRadius()
        {
            if (visualTransform == null)
            {
                baseCollisionRadius = 0.6f;
                return;
            }

            Renderer[] renderers = visualTransform.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                baseCollisionRadius = 0.6f;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            baseCollisionRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            CollisionRadius = baseCollisionRadius;
        }
    }
}
