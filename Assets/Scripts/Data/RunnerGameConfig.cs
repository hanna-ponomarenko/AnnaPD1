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
        [SerializeField] private int initialSegmentCount = 1200;

        [Min(0f)]
        [SerializeField] private float despawnOffset = 14f;

        [Header("Visuals")]
        [Min(1f)]
        [SerializeField] private float trackWidth = 8f;

        [Min(0.1f)]
        [SerializeField] private float holeDiameter = 0.9f;

        [Header("Collectibles")]
        [Min(0.1f)]
        [SerializeField] private float collectibleSize = 0.9f;

        [Min(0.1f)]
        [SerializeField] private float collectibleSpawnSpacing = 4f;

        [Min(1)]
        [SerializeField] private int maxMissedCollectibles = 3;

        [Header("Hole Growth")]
        [Min(0f)]
        [SerializeField] private float growthPerCollectible = 0.1f;

        [Min(0.1f)]
        [SerializeField] private float maxHoleDiameter = 4.5f;

        [Header("Pepper Boost")]
        [Min(0.1f)]
        [SerializeField] private float pepperSize = 1f;

        [Min(1f)]
        [SerializeField] private float pepperSpawnSpacing = 18f;

        [Min(0f)]
        [SerializeField] private float boostDuration = 2f;

        [Min(1f)]
        [SerializeField] private float boostSpeedMultiplier = 1.8f;

        [Header("Apple Split")]
        [Min(0f)]
        [SerializeField] private float appleSplitDuration = 10f;

        [Min(0.1f)]
        [SerializeField] private float splitHoleSpacingMultiplier = 0.65f;

        public float ForwardSpeed => forwardSpeed;
        public float LateralSpeed => lateralSpeed;
        public float LateralLimit => lateralLimit;
        public float SegmentLength => segmentLength;
        public int InitialSegmentCount => initialSegmentCount;
        public float DespawnOffset => despawnOffset;
        public float TrackWidth => trackWidth;
        public float HoleDiameter => holeDiameter;
        public float CollectibleSize => collectibleSize;
        public float CollectibleSpawnSpacing => collectibleSpawnSpacing;
        public int MaxMissedCollectibles => maxMissedCollectibles;
        public float GrowthPerCollectible => growthPerCollectible;
        public float MaxHoleDiameter => maxHoleDiameter;
        public float PepperSize => pepperSize;
        public float PepperSpawnSpacing => pepperSpawnSpacing;
        public float BoostDuration => boostDuration;
        public float BoostSpeedMultiplier => boostSpeedMultiplier;
        public float AppleSplitDuration => appleSplitDuration;
        public float SplitHoleSpacingMultiplier => splitHoleSpacingMultiplier;

        public static RunnerGameConfig CreateRuntimeDefault()
        {
            RunnerGameConfig config = CreateInstance<RunnerGameConfig>();
            config.forwardSpeed = 6f;
            config.lateralSpeed = 8f;
            config.lateralLimit = 3.5f;
            config.segmentLength = 12f;
            config.initialSegmentCount = 1200;
            config.despawnOffset = 14f;
            config.trackWidth = 8f;
            config.holeDiameter = 0.9f;
            config.collectibleSize = 0.9f;
            config.collectibleSpawnSpacing = 4f;
            config.maxMissedCollectibles = 3;
            config.growthPerCollectible = 0.1f;
            config.maxHoleDiameter = 4.5f;
            config.pepperSize = 1f;
            config.pepperSpawnSpacing = 18f;
            config.boostDuration = 2f;
            config.boostSpeedMultiplier = 1.8f;
            config.appleSplitDuration = 10f;
            config.splitHoleSpacingMultiplier = 0.65f;
            return config;
        }
    }
}
