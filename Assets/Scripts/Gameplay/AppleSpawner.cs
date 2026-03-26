using System.Collections.Generic;
using Featurehole.Runner.Audio;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class AppleSpawner : MonoBehaviour
    {
        private const float SpawnPadding = 0.9f;
        private const int CyclesAhead = 2;
        private const float EdgeMargin = 0.55f;

        [SerializeField] private ApplePickup applePrefab;
        [SerializeField] private Transform applesRoot;

        private readonly List<ApplePickup> activeApples = new List<ApplePickup>();
        private readonly Dictionary<ApplePickup, int> cycleByApple = new Dictionary<ApplePickup, int>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private GameSfxManager sfxManager;
        private Sprite appleSprite;
        private int nextCycleIndexToSpawn;

        public void SetAppleSprite(Sprite sprite)
        {
            appleSprite = sprite;
        }

        public void SetSfxManager(GameSfxManager manager)
        {
            sfxManager = manager;
        }

        public void Initialize(RunnerGameConfig runnerConfig, HoleMover runnerHoleMover)
        {
            config = runnerConfig;
            holeMover = runnerHoleMover;

            if (applesRoot == null)
            {
                applesRoot = transform;
            }

            ResetField();
        }

        public void ResetField()
        {
            if (config == null)
            {
                return;
            }

            ClearApples();
            nextCycleIndexToSpawn = 0;
            EnsureUpcomingCycles(0);
        }

        public void Tick(float deltaTime, RunnerRuntime runtime, float forwardSpeed)
        {
            if (config == null || holeMover == null)
            {
                return;
            }

            float moveDelta = -forwardSpeed * deltaTime;
            Transform holeTransform = holeMover.transform;
            float passedThreshold = holeTransform.position.z - config.CollectibleSize;

            for (int index = activeApples.Count - 1; index >= 0; index--)
            {
                ApplePickup apple = activeApples[index];
                if (!apple.gameObject.activeSelf)
                {
                    continue;
                }

                apple.Move(moveDelta);

                Vector3 applePosition = apple.transform.position;
                if (holeMover.CanAbsorb(applePosition, config.CollectibleSize))
                {
                    apple.Collect();
                    sfxManager?.PlayApplePickup();
                    runtime.RegisterAppleCollected();
                    holeMover.Grow();
                    holeMover.ActivateSplit(config.AppleSplitDuration);
                    RemoveApple(apple, index);
                    continue;
                }

                if (applePosition.z < passedThreshold)
                {
                    runtime.RegisterMissed();
                    RemoveApple(apple, index);
                }
            }

            EnsureUpcomingCycles(CollectibleProgressionLayout.GetCycleIndex(config, runtime.DistanceTravelled));
        }

        private void EnsureUpcomingCycles(int currentCycleIndex)
        {
            int targetCycleIndex = currentCycleIndex + CyclesAhead;
            while (nextCycleIndexToSpawn <= targetCycleIndex)
            {
                SpawnCycle(nextCycleIndexToSpawn);
                nextCycleIndexToSpawn++;
            }
        }

        private void SpawnCycle(int cycleIndex)
        {
            int appleCount = CollectibleProgressionLayout.GetAppleCount(cycleIndex);
            for (int slotIndex = 0; slotIndex < appleCount; slotIndex++)
            {
                SpawnApple(cycleIndex, slotIndex, appleCount);
            }
        }

        private void SpawnApple(int cycleIndex, int slotIndex, int appleCount)
        {
            ApplePickup apple = applePrefab != null
                ? Instantiate(applePrefab, applesRoot)
                : CreateRuntimeApple(activeApples.Count);

            apple.gameObject.name = $"Apple_{cycleIndex}_{slotIndex}";
            activeApples.Add(apple);
            cycleByApple[apple] = cycleIndex;

            Vector2 spawnWindow = CollectibleProgressionLayout.GetAppleWindow(config, cycleIndex, slotIndex, SpawnPadding);
            float laneHalfWidth = Mathf.Max(0f, config.TrackWidth * 0.5f - config.CollectibleSize - EdgeMargin);
            float laneX = Random.Range(-laneHalfWidth, laneHalfWidth);
            float laneZ = Random.Range(spawnWindow.x, spawnWindow.y);
            apple.SetPosition(new Vector3(laneX, 0.58f, laneZ));
        }

        private void RemoveApple(ApplePickup apple, int index)
        {
            cycleByApple.Remove(apple);
            activeApples.RemoveAt(index);
            Destroy(apple.gameObject);
        }

        private void ClearApples()
        {
            foreach (ApplePickup apple in activeApples)
            {
                if (apple != null)
                {
                    Destroy(apple.gameObject);
                }
            }

            activeApples.Clear();
            cycleByApple.Clear();
        }

        private ApplePickup CreateRuntimeApple(int index)
        {
            GameObject root = new GameObject($"Apple_{index}");
            root.transform.SetParent(applesRoot, false);

            ApplePickup apple = root.AddComponent<ApplePickup>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = appleSprite;
            spriteRenderer.sortingOrder = 24;

            if (appleSprite != null && appleSprite.bounds.size.y > 0f)
            {
                float targetHeight = config.CollectibleSize * 1.2f;
                float uniformScale = targetHeight / appleSprite.bounds.size.y;
                visual.transform.localScale = Vector3.one * uniformScale;
            }
            else
            {
                visual.transform.localScale = Vector3.one * 0.7f;
            }

            return apple;
        }
    }
}
