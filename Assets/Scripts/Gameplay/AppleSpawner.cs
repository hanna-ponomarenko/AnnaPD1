using System.Collections.Generic;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class AppleSpawner : MonoBehaviour
    {
        private const float AppleSpawnIntervalSeconds = 5f;

        [SerializeField] private ApplePickup applePrefab;
        [SerializeField] private Transform applesRoot;

        private readonly List<ApplePickup> activeApples = new List<ApplePickup>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private Sprite appleSprite;
        private float nextSpawnZ;

        public void SetAppleSprite(Sprite sprite)
        {
            appleSprite = sprite;
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
            nextSpawnZ = config.SegmentLength;

            SpawnApple(0, config.ForwardSpeed);
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

            foreach (ApplePickup apple in activeApples)
            {
                if (!apple.gameObject.activeSelf)
                {
                    continue;
                }

                apple.Move(moveDelta);

                Vector3 applePosition = apple.transform.position;
                if (holeMover.CanAbsorb(applePosition, config.CollectibleSize))
                {
                    apple.Collect();
                    runtime.RegisterCollected();
                    holeMover.Grow();
                    holeMover.ActivateSplit(config.AppleSplitDuration);
                    RespawnApple(apple, forwardSpeed);
                    continue;
                }

                if (applePosition.z < passedThreshold)
                {
                    runtime.RegisterMissed();
                    RespawnApple(apple, forwardSpeed);
                }
            }
        }

        private void SpawnApple(int index, float forwardSpeed)
        {
            ApplePickup apple = applePrefab != null
                ? Instantiate(applePrefab, applesRoot)
                : CreateRuntimeApple(index);

            apple.gameObject.name = $"Apple_{index}";
            activeApples.Add(apple);
            RespawnApple(apple, forwardSpeed);
        }

        private void RespawnApple(ApplePickup apple, float forwardSpeed)
        {
            float laneHalfWidth = Mathf.Max(0f, config.TrackWidth * 0.5f - config.CollectibleSize);
            float laneX = Random.Range(-laneHalfWidth, laneHalfWidth);
            Vector3 spawnPosition = new Vector3(laneX, 0.58f, nextSpawnZ);
            apple.SetPosition(spawnPosition);
            float spawnSpacing = Mathf.Max(config.CollectibleSpawnSpacing, forwardSpeed * AppleSpawnIntervalSeconds);
            nextSpawnZ += spawnSpacing;
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
