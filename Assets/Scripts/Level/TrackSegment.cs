using UnityEngine;

namespace Featurehole.Runner.Level
{
    public sealed class TrackSegment : MonoBehaviour
    {
        private const float SeamOverlap = 0.05f;

        private Transform visualTransform;
        private float length;

        public float MaxZ => transform.position.z + length * 0.5f;

        public void Configure(float segmentLength)
        {
            length = segmentLength;

            if (visualTransform == null)
            {
                visualTransform = transform.Find("Visual");
            }

            if (visualTransform != null)
            {
                Vector3 scale = visualTransform.localScale;
                scale.z = length + SeamOverlap;
                visualTransform.localScale = scale;
            }
        }

        public void Move(float deltaZ)
        {
            transform.position += new Vector3(0f, 0f, deltaZ);
        }

        public void SetZ(float worldZ)
        {
            Vector3 position = transform.position;
            position.z = worldZ;
            transform.position = position;
        }
    }
}
