using System.Collections.Generic;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class CollectibleLaneSpawner : MonoBehaviour
    {
        [SerializeField] private CollectibleItem collectiblePrefab;
        [SerializeField] private Transform collectiblesRoot;

        private readonly List<CollectibleItem> activeItems = new List<CollectibleItem>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private float nextSpawnZ;

        public void Initialize(RunnerGameConfig runnerConfig, HoleMover runnerHoleMover)
        {
            config = runnerConfig;
            holeMover = runnerHoleMover;

            if (collectiblesRoot == null)
            {
                collectiblesRoot = transform;
            }

            ResetField();
        }

        public void ResetField()
        {
            if (config == null)
            {
                return;
            }

            ClearItems();
        }

        public void Tick(float deltaTime, RunnerRuntime runtime)
        {
            if (config == null || holeMover == null)
            {
                return;
            }

            float moveDelta = -config.ForwardSpeed * deltaTime;
            Transform holeTransform = holeMover.transform;
            float absorbRadius = holeMover.CurrentDiameter * 0.5f;
            float passedThreshold = holeTransform.position.z - config.CollectibleSize;

            foreach (CollectibleItem item in activeItems)
            {
                if (!item.gameObject.activeSelf)
                {
                    continue;
                }

                item.Move(moveDelta);

                Vector3 itemPosition = item.transform.position;
                float lateralDistance = Mathf.Abs(itemPosition.x - holeTransform.position.x);
                float forwardDistance = Mathf.Abs(itemPosition.z - holeTransform.position.z);

                if (lateralDistance <= absorbRadius && forwardDistance <= config.CollectibleSize)
                {
                    item.MarkCollected();
                    runtime.RegisterCollected();
                    holeMover.Grow();
                    RespawnItem(item);
                    continue;
                }

                if (itemPosition.z < passedThreshold)
                {
                    item.MarkMissed();
                    runtime.RegisterMissed();
                    RespawnItem(item);
                }
            }
        }

        private void SpawnItem(int index)
        {
            CollectibleItem item = collectiblePrefab != null
                ? Instantiate(collectiblePrefab, collectiblesRoot)
                : CreateRuntimeCollectible(index);

            item.gameObject.name = $"Collectible_{index}";
            activeItems.Add(item);
            RespawnItem(item);
        }

        private void RespawnItem(CollectibleItem item)
        {
            float laneHalfWidth = Mathf.Max(0f, config.TrackWidth * 0.5f - config.CollectibleSize);
            float laneX = Random.Range(-laneHalfWidth, laneHalfWidth);
            Vector3 spawnPosition = new Vector3(laneX, 0.5f, nextSpawnZ);
            item.SetPosition(spawnPosition);
            nextSpawnZ += config.CollectibleSpawnSpacing;
        }

        private void ClearItems()
        {
            foreach (CollectibleItem item in activeItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            activeItems.Clear();
        }

        private CollectibleItem CreateRuntimeCollectible(int index)
        {
            GameObject root = new GameObject($"Collectible_{index}");
            root.transform.SetParent(collectiblesRoot, false);

            CollectibleItem item = root.AddComponent<CollectibleItem>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * config.CollectibleSize;

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.97f, 0.74f, 0.22f);
            }

            return item;
        }
    }
}
