using System.Collections.Generic;
using Featurehole.Runner.Audio;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class PepperBoostSpawner : MonoBehaviour
    {
        private const float SpawnPadding = 0.85f;
        private const int CyclesAhead = 2;
        private const float EdgeMargin = 0.55f;

        [SerializeField] private PepperPickup pepperPrefab;
        [SerializeField] private Transform peppersRoot;

        private readonly List<PepperPickup> activePeppers = new List<PepperPickup>();
        private readonly Dictionary<PepperPickup, int> cycleByPepper = new Dictionary<PepperPickup, int>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private GameSfxManager sfxManager;
        private Sprite pepperSprite;
        private int nextCycleIndexToSpawn;

        public void SetPepperSprite(Sprite sprite)
        {
            pepperSprite = sprite;
        }

        public void SetSfxManager(GameSfxManager manager)
        {
            sfxManager = manager;
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
            float passedThreshold = holeTransform.position.z - config.PepperSize;

            for (int index = activePeppers.Count - 1; index >= 0; index--)
            {
                PepperPickup pepper = activePeppers[index];
                if (!pepper.gameObject.activeSelf)
                {
                    continue;
                }

                pepper.Move(moveDelta);

                Vector3 pepperPosition = pepper.transform.position;
                if (holeMover.CanAbsorb(pepperPosition, config.PepperSize, 0.3f))
                {
                    pepper.Collect();
                    sfxManager?.PlayPepperPickup();
                    runtime.RegisterPepperCollected();
                    runtime.ActivateBoost(config.BoostDuration, config.BoostSpeedMultiplier);
                    holeMover.Grow();
                    RemovePepper(pepper, index);
                    continue;
                }

                if (pepperPosition.z < passedThreshold)
                {
                    RemovePepper(pepper, index);
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
            int pepperCount = CollectibleProgressionLayout.GetPepperCount(cycleIndex);
            for (int slotIndex = 0; slotIndex < pepperCount; slotIndex++)
            {
                SpawnPepper(cycleIndex, slotIndex);
            }
        }

        private void SpawnPepper(int cycleIndex, int slotIndex)
        {
            PepperPickup pepper = pepperPrefab != null
                ? Instantiate(pepperPrefab, peppersRoot)
                : CreateRuntimePepper(activePeppers.Count);

            pepper.gameObject.name = $"Pepper_{cycleIndex}_{slotIndex}";
            activePeppers.Add(pepper);
            cycleByPepper[pepper] = cycleIndex;

            Vector2 spawnWindow = CollectibleProgressionLayout.GetPepperWindow(config, cycleIndex, slotIndex, SpawnPadding);
            float laneHalfWidth = Mathf.Max(0f, config.TrackWidth * 0.5f - config.PepperSize - EdgeMargin);
            float laneX = Random.Range(-laneHalfWidth, laneHalfWidth);
            float laneZ = Random.Range(spawnWindow.x, spawnWindow.y);
            pepper.SetPosition(new Vector3(laneX, 0.65f, laneZ));
        }

        private void RemovePepper(PepperPickup pepper, int index)
        {
            cycleByPepper.Remove(pepper);
            activePeppers.RemoveAt(index);
            Destroy(pepper.gameObject);
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
            cycleByPepper.Clear();
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
    }
}
