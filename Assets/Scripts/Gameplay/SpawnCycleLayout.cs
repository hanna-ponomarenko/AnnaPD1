using Featurehole.Runner.Data;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public static class SpawnCycleLayout
    {
        private const float SpawnCycleDurationSeconds = 15f;
        private const int PepperSlotsPerCycle = 3;

        public static float GetCycleDistance(RunnerGameConfig config)
        {
            return Mathf.Max(config.PepperSpawnSpacing * PepperSlotsPerCycle, config.ForwardSpeed * SpawnCycleDurationSeconds);
        }

        public static float GetSlotDistance(RunnerGameConfig config)
        {
            return GetCycleDistance(config) / (PepperSlotsPerCycle + 1f);
        }

        public static Vector2 GetSubWindow(
            RunnerGameConfig config,
            float cycleStart,
            int intervalIndex,
            int sectionIndex,
            int sectionCount,
            float padding)
        {
            float slotDistance = GetSlotDistance(config);
            float intervalStart = cycleStart + slotDistance * intervalIndex;
            float intervalEnd = intervalStart + slotDistance;
            float sectionLength = (intervalEnd - intervalStart) / sectionCount;
            float start = intervalStart + sectionLength * sectionIndex + padding;
            float end = intervalStart + sectionLength * (sectionIndex + 1) - padding;

            if (end <= start)
            {
                float center = intervalStart + sectionLength * (sectionIndex + 0.5f);
                start = center - 0.35f;
                end = center + 0.35f;
            }

            return new Vector2(start, end);
        }
    }
}
