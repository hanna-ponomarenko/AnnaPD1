using UnityEngine;

namespace Featurehole.Runner.Level
{
    public sealed class TrackSegment : MonoBehaviour
    {
        private const float SeamOverlap = 0.45f;

        private Transform visualTransform;
        private float length;
        private float localMinZ;
        private float localMaxZ;

        public float MinZ => transform.position.z + localMinZ;
        public float MaxZ => transform.position.z + localMaxZ;
        public float Length => length;

        public void Configure(float segmentLength)
        {
            length = segmentLength;

            ConfigureScaledChild("Visual");
            ConfigureScaledChild("RiverCurrent");
            ConfigureScaledChild("RiverGlow");
            ConfigureScaledChild("RiverSheen");
            ConfigureScaledChild("RiverFoamLeft");
            ConfigureScaledChild("RiverFoamRight");
            ConfigureScaledChild("BankLeft");
            ConfigureScaledChild("BankRight");
            ConfigureScaledChild("UnderwaterSand");

            RecalculateBounds();
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
            SetZ(worldMinZ - localMinZ);
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

        private void RecalculateBounds()
        {
            if (visualTransform == null)
            {
                localMinZ = -length * 0.5f;
                localMaxZ = length * 0.5f;
                return;
            }

            float halfLogicalLength = length * 0.5f;
            localMinZ = visualTransform.localPosition.z - halfLogicalLength;
            localMaxZ = visualTransform.localPosition.z + halfLogicalLength;
        }
    }
}
