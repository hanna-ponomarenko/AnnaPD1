using System.Collections.Generic;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class CoinSpawner : MonoBehaviour
    {
        private const int CoinsPerCycle = 4;
        private const float CoinSize = 0.65f;
        private const float CoinSpacingPadding = 1.2f;
        private const float MagnetAttractionStrength = 8f;

        [SerializeField] private CoinPickup coinPrefab;
        [SerializeField] private Transform coinsRoot;

        private readonly List<CoinPickup> activeCoins = new List<CoinPickup>();
        private readonly Dictionary<CoinPickup, int> slotIndexByCoin = new Dictionary<CoinPickup, int>();
        private readonly Dictionary<CoinPickup, float> nextCycleStartByCoin = new Dictionary<CoinPickup, float>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private int lastAttractedCoinCount = -1;

        public void Initialize(RunnerGameConfig runnerConfig, HoleMover runnerHoleMover)
        {
            config = runnerConfig;
            holeMover = runnerHoleMover;

            if (coinsRoot == null)
            {
                coinsRoot = transform;
            }

            ResetField();
        }

        public void ResetField()
        {
            if (config == null)
            {
                return;
            }

            ClearCoins();

            float initialCycleStart = config.SegmentLength;
            for (int index = 0; index < CoinsPerCycle; index++)
            {
                SpawnCoin(index, initialCycleStart);
            }
        }

        public void Tick(float deltaTime, RunnerRuntime runtime, float forwardSpeed)
        {
            if (config == null || holeMover == null)
            {
                return;
            }

            float moveDelta = -forwardSpeed * deltaTime;
            float passedThreshold = holeMover.transform.position.z - CoinSize;
            int attractedCoinCount = 0;

            foreach (CoinPickup coin in activeCoins)
            {
                if (!coin.gameObject.activeSelf)
                {
                    continue;
                }

                coin.Move(moveDelta);

                Vector3 coinPosition = coin.transform.position;
                if (runtime.IsMagnetActive)
                {
                    float distanceToHole = Vector3.Distance(coinPosition, holeMover.transform.position);
                    if (distanceToHole <= runtime.MagnetRadius)
                    {
                        attractedCoinCount++;
                        coin.AttractTowards(holeMover.transform.position, MagnetAttractionStrength, deltaTime);
                        coinPosition = coin.transform.position;
                    }
                }

                if (holeMover.CanAbsorb(coinPosition, CoinSize, 0.28f))
                {
                    coin.Collect();
                    runtime.RegisterCollected();
                    RespawnCoin(coin);
                    continue;
                }

                if (coinPosition.z < passedThreshold)
                {
                    RespawnCoin(coin);
                }
            }

            if (runtime.IsMagnetActive && attractedCoinCount != lastAttractedCoinCount)
            {
                Debug.Log($"[Magnet] coins attracted count={attractedCoinCount}", this);
            }

            lastAttractedCoinCount = runtime.IsMagnetActive ? attractedCoinCount : -1;
        }

        private void SpawnCoin(int slotIndex, float cycleStart)
        {
            CoinPickup coin = coinPrefab != null
                ? Instantiate(coinPrefab, coinsRoot)
                : CreateRuntimeCoin(slotIndex);

            coin.gameObject.name = $"Coin_{slotIndex}";
            activeCoins.Add(coin);
            slotIndexByCoin[coin] = slotIndex;
            nextCycleStartByCoin[coin] = cycleStart;
            RespawnCoin(coin);
        }

        private void RespawnCoin(CoinPickup coin)
        {
            int slotIndex = slotIndexByCoin[coin];
            float cycleStart = nextCycleStartByCoin[coin];

            Vector2 spawnWindow = GetCoinWindow(slotIndex, cycleStart);
            float laneHalfWidth = Mathf.Max(0f, config.TrackWidth * 0.5f - CoinSize * 0.65f);
            float laneX = Random.Range(-laneHalfWidth, laneHalfWidth);
            float laneZ = Random.Range(spawnWindow.x, spawnWindow.y);

            coin.SetPosition(new Vector3(laneX, 0.62f, laneZ));
            nextCycleStartByCoin[coin] = cycleStart + GetCycleDistance();
        }

        private Vector2 GetCoinWindow(int slotIndex, float cycleStart)
        {
            float padding = Mathf.Max(CoinSize * 1.2f, CoinSpacingPadding);
            return SpawnCycleLayout.GetSubWindow(config, cycleStart, slotIndex, 0, 2, padding);
        }

        private void ClearCoins()
        {
            foreach (CoinPickup coin in activeCoins)
            {
                if (coin != null)
                {
                    Destroy(coin.gameObject);
                }
            }

            activeCoins.Clear();
            slotIndexByCoin.Clear();
            nextCycleStartByCoin.Clear();
        }

        private CoinPickup CreateRuntimeCoin(int index)
        {
            GameObject root = new GameObject($"Coin_{index}");
            root.transform.SetParent(coinsRoot, false);

            CoinPickup coin = root.AddComponent<CoinPickup>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(CoinSize, CoinSize * 0.12f, CoinSize);

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.82f, 0.18f, 1f);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            GameObject inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inner.name = "Inner";
            inner.transform.SetParent(visual.transform, false);
            inner.transform.localPosition = new Vector3(0f, -0.03f, 0f);
            inner.transform.localScale = new Vector3(0.58f, 1.15f, 0.58f);

            Collider innerCollider = inner.GetComponent<Collider>();
            if (innerCollider != null)
            {
                Destroy(innerCollider);
            }

            Renderer innerRenderer = inner.GetComponent<Renderer>();
            if (innerRenderer != null)
            {
                innerRenderer.material.color = new Color(0.96f, 0.72f, 0.1f, 1f);
                innerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                innerRenderer.receiveShadows = false;
            }

            return coin;
        }

        private float GetCycleDistance()
        {
            return SpawnCycleLayout.GetCycleDistance(config);
        }
    }
}
