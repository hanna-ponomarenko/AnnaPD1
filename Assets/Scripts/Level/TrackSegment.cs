using UnityEngine;

namespace Featurehole.Runner.Level
{
    public sealed class TrackSegment : MonoBehaviour
    {
        private const float SeamOverlap = 0.05f;

        private Transform visualTransform;
        private float length;

        public float MinZ => transform.position.z - length * 0.5f;
        public float MaxZ => transform.position.z + length * 0.5f;
        public float Length => length;

        public void Configure(float segmentLength)
        {
            length = segmentLength;

            ConfigureScaledChild("Visual");
            ConfigureScaledChild("RiverGlow");
            ConfigureScaledChild("BankLeft");
            ConfigureScaledChild("BankRight");
            ConfigureScaledChild("UnderwaterSand");
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

        public void SetMinZ(float worldMinZ)
        {
            SetZ(worldMinZ + length * 0.5f);
        }

        private void ConfigureScaledChild(string childName)
        {
            Transform childTransform = childName == "Visual" && visualTransform != null
                ? visualTransform
                : transform.Find(childName);

            if (childTransform == null)
            {
                return;
            }

            if (childName == "Visual")
            {
                visualTransform = childTransform;
            }

            Vector3 scale = childTransform.localScale;
            scale.z = length + SeamOverlap;
            childTransform.localScale = scale;
        }
    }
}
