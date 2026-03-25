using System.Collections.Generic;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class PepperBoostSpawner : MonoBehaviour
    {
        private const float SpawnCycleDurationSeconds = 15f;
        private const int PeppersPerCycle = 3;

        [SerializeField] private PepperPickup pepperPrefab;
        [SerializeField] private Transform peppersRoot;

        private readonly List<PepperPickup> activePeppers = new List<PepperPickup>();
        private readonly Dictionary<PepperPickup, float> nextSpawnZByPepper = new Dictionary<PepperPickup, float>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private Sprite pepperSprite;

        public void SetPepperSprite(Sprite sprite)
        {
            pepperSprite = sprite;
        }

        public void Initialize(RunnerGameConfig runnerConfig, HoleMover runnerHoleMover)
        {
            config = runnerConfig;
            holeMover = runnerHoleMover;

            if (peppersRoot == null)
            {
                peppersRoot = transform;
            }

            ResetField();
        }

        public void ResetField()
        {
            if (config == null)
            {
                return;
            }

            ClearPeppers();
            float slotDistance = GetSlotDistance();
            float holeZ = holeMover != null ? holeMover.transform.position.z : 0f;

            for (int index = 0; index < 3; index++)
            {
                SpawnPepper(index, holeZ + config.SegmentLength + slotDistance * (index + 1));
            }
        }

        public void Tick(float deltaTime, RunnerRuntime runtime, float forwardSpeed)
        {
            if (config == null || holeMover == null)
            {
                return;
            }

            Transform holeTransform = holeMover.transform;
            float passedThreshold = holeTransform.position.z - config.PepperSize;

            foreach (PepperPickup pepper in activePeppers)
            {
                if (!pepper.gameObject.activeSelf)
                {
                    continue;
                }

                Vector3 pepperPosition = pepper.transform.position;
                if (holeMover.CanAbsorb(pepperPosition, config.PepperSize, 0.3f))
                {
                    pepper.Collect();
                    runtime.RegisterCollected();
                    runtime.ActivateBoost(config.BoostDuration, config.BoostSpeedMultiplier);
                    holeMover.Grow();
                    RespawnPepper(pepper);
                    continue;
                }

                if (pepperPosition.z < passedThreshold)
                {
                    RespawnPepper(pepper);
                }
            }
        }

        private void SpawnPepper(int index, float initialSpawnZ)
        {
            PepperPickup pepper = pepperPrefab != null
                ? Instantiate(pepperPrefab, peppersRoot)
                : CreateRuntimePepper(index);

            pepper.gameObject.name = $"Pepper_{index}";
            activePeppers.Add(pepper);
            nextSpawnZByPepper[pepper] = initialSpawnZ;
            RespawnPepper(pepper);
        }

        private void RespawnPepper(PepperPickup pepper)
        {
            float laneHalfWidth = Mathf.Max(0f, config.TrackWidth * 0.5f - config.PepperSize);
            float laneX = Random.Range(-laneHalfWidth, laneHalfWidth);
            float holeZ = holeMover != null ? holeMover.transform.position.z : 0f;
            float spawnZ = Mathf.Max(nextSpawnZByPepper[pepper], holeZ + config.SegmentLength);
            Vector3 spawnPosition = new Vector3(laneX, 0.65f, spawnZ);
            pepper.SetPosition(spawnPosition);
            nextSpawnZByPepper[pepper] = spawnZ + GetCycleDistance();
        }

        private void ClearPeppers()
        {
            foreach (PepperPickup pepper in activePeppers)
            {
                if (pepper != null)
                {
                    Destroy(pepper.gameObject);
                }
            }

            activePeppers.Clear();
            nextSpawnZByPepper.Clear();
        }

        private PepperPickup CreateRuntimePepper(int index)
        {
            GameObject root = new GameObject($"Pepper_{index}");
            root.transform.SetParent(peppersRoot, false);

            PepperPickup pepper = root.AddComponent<PepperPickup>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = pepperSprite;
            spriteRenderer.sortingOrder = 25;

            if (pepperSprite != null && pepperSprite.bounds.size.y > 0f)
            {
                float targetHeight = config.PepperSize * 1.35f;
                float uniformScale = targetHeight / pepperSprite.bounds.size.y;
                visual.transform.localScale = Vector3.one * uniformScale;
            }
            else
            {
                visual.transform.localScale = Vector3.one * 0.75f;
            }

            return pepper;
        }

        private float GetCycleDistance()
        {
            return Mathf.Max(config.PepperSpawnSpacing * PeppersPerCycle, config.ForwardSpeed * SpawnCycleDurationSeconds);
        }

        private float GetSlotDistance()
        {
            return GetCycleDistance() / (PeppersPerCycle + 1f);
        }
    }
}
