using UnityEngine;

namespace Featurehole.Runner.Level
{
    public sealed class TrackSegment : MonoBehaviour
    {
        public float TailZ => transform.position.z;

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
