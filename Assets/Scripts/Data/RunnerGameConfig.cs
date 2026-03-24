using UnityEngine;

namespace Featurehole.Runner.Data
{
    [CreateAssetMenu(fileName = "RunnerGameConfig", menuName = "Runner/Game Config")]
    public sealed class RunnerGameConfig : ScriptableObject
    {
        [Header("Run Speed")]
        [Min(0f)]
        [SerializeField] private float forwardSpeed = 6f;

        [Header("Hole Movement")]
        [Min(0f)]
        [SerializeField] private float lateralSpeed = 8f;

        [Min(0f)]
        [SerializeField] private float lateralLimit = 3.5f;

        [Header("Track")]
        [Min(1f)]
        [SerializeField] private float segmentLength = 12f;

        [Min(1)]
        [SerializeField] private int initialSegmentCount = 6;

        [Min(0f)]
        [SerializeField] private float despawnOffset = 14f;

        [Header("Visuals")]
        [Min(1f)]
        [SerializeField] private float trackWidth = 8f;

        [Min(0.1f)]
        [SerializeField] private float holeDiameter = 1.8f;

        public float ForwardSpeed => forwardSpeed;
        public float LateralSpeed => lateralSpeed;
        public float LateralLimit => lateralLimit;
        public float SegmentLength => segmentLength;
        public int InitialSegmentCount => initialSegmentCount;
        public float DespawnOffset => despawnOffset;
        public float TrackWidth => trackWidth;
        public float HoleDiameter => holeDiameter;

        public static RunnerGameConfig CreateRuntimeDefault()
        {
            RunnerGameConfig config = CreateInstance<RunnerGameConfig>();
            config.forwardSpeed = 6f;
            config.lateralSpeed = 8f;
            config.lateralLimit = 3.5f;
            config.segmentLength = 12f;
            config.initialSegmentCount = 6;
            config.despawnOffset = 14f;
            config.trackWidth = 8f;
            config.holeDiameter = 1.8f;
            return config;
        }
    }
}
