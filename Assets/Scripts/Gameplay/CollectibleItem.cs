using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class CollectibleItem : MonoBehaviour
    {
        public bool IsCollected { get; private set; }
        public bool IsMissed { get; private set; }

        public void Move(float deltaZ)
        {
            transform.position += new Vector3(0f, 0f, deltaZ);
        }

        public void SetPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            IsCollected = false;
            IsMissed = false;
            gameObject.SetActive(true);
        }

        public void MarkCollected()
        {
            IsCollected = true;
            gameObject.SetActive(false);
        }

        public void MarkMissed()
        {
            IsMissed = true;
            gameObject.SetActive(false);
        }
    }
}
