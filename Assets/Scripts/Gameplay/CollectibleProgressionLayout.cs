using Featurehole.Runner.Data;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public static class CollectibleProgressionLayout
    {
        private const float ApplesSectionStart = 0f;
        private const float ApplesSectionEnd = 0.3f;
        private const float PeppersSectionStart = 0.3f;
        private const float PeppersSectionEnd = 0.8f;
        private const float MagnetSectionStart = 0.8f;
        private const float MagnetSectionEnd = 1f;

        public static float GetCycleDistance(RunnerGameConfig config)
        {
            return SpawnCycleLayout.GetCycleDistance(config);
        }

        public static float GetCycleStart(RunnerGameConfig config, int cycleIndex)
        {
            return config.SegmentLength + GetCycleDistance(config) * cycleIndex;
        }

        public static int GetCycleIndex(RunnerGameConfig config, float distanceTravelled)
        {
            return Mathf.Max(0, Mathf.FloorToInt(distanceTravelled / GetCycleDistance(config)));
        }

        public static int GetAppleCount(int cycleIndex)
        {
            return Mathf.Max(1, cycleIndex + 1);
        }

        public static int GetPepperCount(int cycleIndex)
        {
            return Mathf.Max(3, cycleIndex + 3);
        }

        public static Vector2 GetAppleWindow(RunnerGameConfig config, int cycleIndex, int slotIndex, float padding)
        {
            return GetWindow(config, cycleIndex, slotIndex, GetAppleCount(cycleIndex), ApplesSectionStart, ApplesSectionEnd, padding);
        }

        public static Vector2 GetPepperWindow(RunnerGameConfig config, int cycleIndex, int slotIndex, float padding)
        {
            return GetWindow(config, cycleIndex, slotIndex, GetPepperCount(cycleIndex), PeppersSectionStart, PeppersSectionEnd, padding);
        }

        public static Vector2 GetMagnetWindow(RunnerGameConfig config, int cycleIndex, float padding)
        {
            return GetWindow(config, cycleIndex, 0, 1, MagnetSectionStart, MagnetSectionEnd, padding);
        }

        private static Vector2 GetWindow(
            RunnerGameConfig config,
            int cycleIndex,
            int slotIndex,
            int slotCount,
            float sectionStart,
            float sectionEnd,
            float padding)
        {
            float cycleStart = GetCycleStart(config, cycleIndex);
            float cycleDistance = GetCycleDistance(config);
            float absoluteStart = cycleStart + cycleDistance * sectionStart;
            float absoluteEnd = cycleStart + cycleDistance * sectionEnd;
            float sectionLength = (absoluteEnd - absoluteStart) / Mathf.Max(1, slotCount);

            float start = absoluteStart + sectionLength * slotIndex + padding;
            float end = absoluteStart + sectionLength * (slotIndex + 1) - padding;

            if (end <= start)
            {
                float center = absoluteStart + sectionLength * (slotIndex + 0.5f);
                start = center - 0.35f;
                end = center + 0.35f;
            }

            return new Vector2(start, end);
        }
    }
}
