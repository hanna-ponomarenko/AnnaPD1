using System.Collections.Generic;
using Featurehole.Runner.Audio;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class MagnetSpawner : MonoBehaviour
    {
        private const float MagnetSize = 0.95f;
        private const float SpawnPadding = 1f;
        private const int CyclesAhead = 2;
        private const float EdgeMargin = 0.55f;

        [SerializeField] private MagnetPickup magnetPrefab;
        [SerializeField] private Transform magnetsRoot;

        private readonly List<MagnetPickup> activeMagnets = new List<MagnetPickup>();
        private readonly Dictionary<MagnetPickup, int> cycleByMagnet = new Dictionary<MagnetPickup, int>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private CoinSpawner coinSpawner;
        private GameSfxManager sfxManager;
        private Sprite magnetSprite;
        private int nextCycleIndexToSpawn;

        public void SetMagnetSprite(Sprite sprite)
        {
            magnetSprite = sprite;
        }

        public void SetCoinSpawner(CoinSpawner spawner)
        {
            coinSpawner = spawner;
        }

        public void SetSfxManager(GameSfxManager manager)
        {
            sfxManager = manager;
        }

        public void Initialize(RunnerGameConfig runnerConfig, HoleMover runnerHoleMover)
        {
            config = runnerConfig;
            holeMover = runnerHoleMover;

            if (magnetsRoot == null)
            {
                magnetsRoot = transform;
            }

            ResetField();
        }

        public void ResetField()
        {
            if (config == null)
            {
                return;
            }

            ClearMagnets();
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
            float passedThreshold = holeMover.transform.position.z - MagnetSize;

            for (int index = activeMagnets.Count - 1; index >= 0; index--)
            {
                MagnetPickup magnet = activeMagnets[index];
                if (magnet == null)
                {
                    activeMagnets.RemoveAt(index);
                    continue;
                }

                if (!magnet.gameObject.activeSelf)
                {
                    continue;
                }

                magnet.Move(moveDelta);
                Vector3 magnetPosition = magnet.transform.position;
                if (holeMover.CanAbsorb(magnetPosition, MagnetSize, 0.32f))
                {
                    magnet.Collect();
                    sfxManager?.PlayMagnetPickup();
                    runtime.ActivateMagnet(5f, 3.6f);
                    Debug.Log($"[Magnet] magnet picked position={magnetPosition}", this);
                    RemoveMagnet(magnet, index);
                    continue;
                }

                if (magnetPosition.z < passedThreshold)
                {
                    RemoveMagnet(magnet, index);
                }
            }

            EnsureUpcomingCycles(CollectibleProgressionLayout.GetCycleIndex(config, runtime.DistanceTravelled));
        }

        private void EnsureUpcomingCycles(int currentCycleIndex)
        {
            int targetCycleIndex = currentCycleIndex + CyclesAhead;
            while (nextCycleIndexToSpawn <= targetCycleIndex)
            {
                SpawnMagnet(nextCycleIndexToSpawn);
                nextCycleIndexToSpawn++;
            }
        }

        private void SpawnMagnet(int cycleIndex)
        {
            MagnetPickup magnet = magnetPrefab != null
                ? Instantiate(magnetPrefab, magnetsRoot)
                : CreateRuntimeMagnet(cycleIndex);

            magnet.gameObject.name = $"Magnet_{cycleIndex}";
            activeMagnets.Add(magnet);
            cycleByMagnet[magnet] = cycleIndex;

            Vector2 window = CollectibleProgressionLayout.GetMagnetWindow(config, cycleIndex, SpawnPadding);
            float reservedDistance = coinSpawner != null ? coinSpawner.GetPostMagnetPatternReservedDistance() : 0f;
            float laneHalfWidth = Mathf.Max(0f, config.TrackWidth * 0.5f - MagnetSize - EdgeMargin);
            float laneX = Random.Range(-laneHalfWidth, laneHalfWidth);
            float maxMagnetZ = Mathf.Max(window.x, window.y - reservedDistance);
            float laneZ = Random.Range(window.x, maxMagnetZ);
            Vector3 magnetSpawnPosition = new Vector3(laneX, 0.62f, laneZ);
            magnet.SetPosition(magnetSpawnPosition);
            if (coinSpawner != null)
            {
                coinSpawner.SpawnMagnetCoinPattern(cycleIndex, magnetSpawnPosition);
            }
            Debug.Log($"[Magnet] magnet spawn cycle={cycleIndex} position={magnet.transform.position}", this);
        }

        private void RemoveMagnet(MagnetPickup magnet, int index)
        {
            cycleByMagnet.Remove(magnet);
            activeMagnets.RemoveAt(index);
            Destroy(magnet.gameObject);
        }

        private void ClearMagnets()
        {
            foreach (MagnetPickup magnet in activeMagnets)
            {
                if (magnet != null)
                {
                    Destroy(magnet.gameObject);
                }
            }

            activeMagnets.Clear();
            cycleByMagnet.Clear();
        }

        private MagnetPickup CreateRuntimeMagnet(int cycleIndex)
        {
            GameObject root = new GameObject($"Magnet_{cycleIndex}");
            root.transform.SetParent(magnetsRoot, false);

            MagnetPickup magnet = root.AddComponent<MagnetPickup>();

            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.05f, 0.02f);
            SpriteRenderer glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = magnetSprite;
            glowRenderer.sortingOrder = 23;
            glowRenderer.color = new Color(1f, 0.95f, 0.48f, 0.35f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = magnetSprite;
            spriteRenderer.sortingOrder = 24;
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);

            float uniformScale = 0.9f;
            if (magnetSprite != null && magnetSprite.bounds.size.y > 0f)
            {
                float targetHeight = MagnetSize * 1.25f;
                uniformScale = targetHeight / magnetSprite.bounds.size.y;
            }

            visual.transform.localScale = Vector3.one * uniformScale;
            glow.transform.localScale = Vector3.one * (uniformScale * 1.25f);
            return magnet;
        }
    }
}
