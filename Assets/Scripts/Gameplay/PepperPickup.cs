using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class PepperPickup : MonoBehaviour
    {
        public void Move(float deltaZ)
        {
            transform.position += new Vector3(0f, 0f, deltaZ);
        }

        public void SetPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            gameObject.SetActive(true);
        }

        public void Collect()
        {
            gameObject.SetActive(false);
        }
    }
}
