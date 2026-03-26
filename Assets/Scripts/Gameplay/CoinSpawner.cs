using System.Collections.Generic;
using Featurehole.Runner.Audio;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class CoinSpawner : MonoBehaviour
    {
        private enum CoinFormationType
        {
            CenterLine,
            RightEdgeLine,
            LeftEdgeLine,
            CenterDiamond,
            CenterCircle
        }

        private const int CoinsPerCycle = 4;
        private const int PostMagnetCoinCount = 10;
        private const float CoinSize = 0.65f;
        private const float CoinSpacingPadding = 1.2f;
        private const float MagnetAttractionStrength = 24f;
        private const float PostMagnetSpawnLeadSeconds = 2f;
        private const float PostMagnetRowSpacingZ = 0.55f;
        private const float PostMagnetLateralSpacing = 0.75f;
        private const float EdgeMargin = 0.55f;

        [SerializeField] private CoinPickup coinPrefab;
        [SerializeField] private Transform coinsRoot;

        private readonly List<CoinPickup> activeCoins = new List<CoinPickup>();
        private readonly Dictionary<CoinPickup, int> slotIndexByCoin = new Dictionary<CoinPickup, int>();
        private readonly Dictionary<CoinPickup, float> nextCycleStartByCoin = new Dictionary<CoinPickup, float>();
        private readonly HashSet<CoinPickup> postMagnetCoins = new HashSet<CoinPickup>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private GameSfxManager sfxManager;
        private int lastAttractedCoinCount = -1;

        public void SetSfxManager(GameSfxManager manager)
        {
            sfxManager = manager;
        }

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

            for (int index = activeCoins.Count - 1; index >= 0; index--)
            {
                CoinPickup coin = activeCoins[index];
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
                    runtime.RegisterCoinCollected();
                    sfxManager?.PlayCoinPickup();
                    if (postMagnetCoins.Contains(coin))
                    {
                        RemovePatternCoin(coin, index);
                    }
                    else
                    {
                        RespawnCoin(coin);
                    }
                    continue;
                }

                if (coinPosition.z < passedThreshold)
                {
                    if (postMagnetCoins.Contains(coin))
                    {
                        RemovePatternCoin(coin, index);
                    }
                    else
                    {
                        RespawnCoin(coin);
                    }
                }
            }

            if (runtime.IsMagnetActive && attractedCoinCount != lastAttractedCoinCount)
            {
                Debug.Log($"[Magnet] coins attracted count={attractedCoinCount}", this);
            }

            lastAttractedCoinCount = runtime.IsMagnetActive ? attractedCoinCount : -1;
        }

        public float GetPostMagnetPatternReservedDistance()
        {
            return GetPostMagnetPatternLeadDistance() + GetPostMagnetPatternLength();
        }

        public void SpawnMagnetCoinPattern(int cycleIndex, Vector3 magnetPosition)
        {
            CoinFormationType formationType = (CoinFormationType)Random.Range(0, 5);
            Debug.Log($"[Magnet] selected formation type={formationType} cycle={cycleIndex}", this);

            Vector2[] formationOffsets = GetFormationOffsets(formationType);
            float laneHalfWidth = GetSafeLaneHalfWidth();
            float leadDistance = GetPostMagnetPatternLeadDistance();
            Vector3 formationOrigin = new Vector3(magnetPosition.x, 0.62f, magnetPosition.z + leadDistance);

            for (int index = 0; index < formationOffsets.Length; index++)
            {
                CoinPickup coin = coinPrefab != null
                    ? Instantiate(coinPrefab, coinsRoot)
                    : CreateRuntimeCoin(activeCoins.Count);

                coin.gameObject.name = $"Coin_Magnet_{cycleIndex}_{index}";
                activeCoins.Add(coin);
                postMagnetCoins.Add(coin);

                Vector2 offset = formationOffsets[index];
                Vector3 worldPosition = new Vector3(
                    Mathf.Clamp(formationOrigin.x + offset.x, -laneHalfWidth, laneHalfWidth),
                    formationOrigin.y,
                    formationOrigin.z + offset.y);

                coin.SetPosition(worldPosition);
            }

            Debug.Log($"[Magnet] spawned 10 coins cycle={cycleIndex} formation={formationType} origin={formationOrigin}", this);
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
            float laneHalfWidth = GetSafeLaneHalfWidth();
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
            postMagnetCoins.Clear();
        }

        private void RemovePatternCoin(CoinPickup coin, int index)
        {
            postMagnetCoins.Remove(coin);
            slotIndexByCoin.Remove(coin);
            nextCycleStartByCoin.Remove(coin);
            activeCoins.RemoveAt(index);
            Destroy(coin.gameObject);
        }

        private Vector2[] GetFormationOffsets(CoinFormationType formationType)
        {
            switch (formationType)
            {
                case CoinFormationType.CenterLine:
                    return BuildLineFormation(0f);
                case CoinFormationType.RightEdgeLine:
                    return BuildLineFormation(GetSafeLaneHalfWidth());
                case CoinFormationType.LeftEdgeLine:
                    return BuildLineFormation(-GetSafeLaneHalfWidth());
                case CoinFormationType.CenterDiamond:
                    return new[]
                    {
                        new Vector2(0f, 0f),
                        new Vector2(-PostMagnetLateralSpacing, PostMagnetRowSpacingZ),
                        new Vector2(0f, PostMagnetRowSpacingZ),
                        new Vector2(PostMagnetLateralSpacing, PostMagnetRowSpacingZ),
                        new Vector2(-PostMagnetLateralSpacing * 1.5f, PostMagnetRowSpacingZ * 2f),
                        new Vector2(-PostMagnetLateralSpacing * 0.5f, PostMagnetRowSpacingZ * 2f),
                        new Vector2(PostMagnetLateralSpacing * 0.5f, PostMagnetRowSpacingZ * 2f),
                        new Vector2(PostMagnetLateralSpacing * 1.5f, PostMagnetRowSpacingZ * 2f),
                        new Vector2(-PostMagnetLateralSpacing, PostMagnetRowSpacingZ * 3f),
                        new Vector2(PostMagnetLateralSpacing, PostMagnetRowSpacingZ * 3f)
                    };
                default:
                    return new[]
                    {
                        new Vector2(0f, 0f),
                        new Vector2(0.95f, 0.45f),
                        new Vector2(1.45f, 1.35f),
                        new Vector2(1.2f, 2.35f),
                        new Vector2(0.4f, 3.05f),
                        new Vector2(-0.4f, 3.05f),
                        new Vector2(-1.2f, 2.35f),
                        new Vector2(-1.45f, 1.35f),
                        new Vector2(-0.95f, 0.45f),
                        new Vector2(0f, 1.55f)
                    };
            }
        }

        private Vector2[] BuildLineFormation(float xPosition)
        {
            Vector2[] positions = new Vector2[PostMagnetCoinCount];
            for (int index = 0; index < PostMagnetCoinCount; index++)
            {
                positions[index] = new Vector2(xPosition, PostMagnetRowSpacingZ * index);
            }

            return positions;
        }

        private float GetSafeLaneHalfWidth()
        {
            return Mathf.Max(0f, config.TrackWidth * 0.5f - CoinSize * 0.65f - EdgeMargin);
        }

        private float GetPostMagnetPatternLeadDistance()
        {
            if (config == null)
            {
                Debug.LogError("CoinSpawner.GetPostMagnetPatternLeadDistance was called before CoinSpawner.Initialize assigned config.", this);
                return 4.5f;
            }

            return Mathf.Max(config.ForwardSpeed * PostMagnetSpawnLeadSeconds, 4.5f);
        }

        private float GetPostMagnetPatternLength()
        {
            return (PostMagnetCoinCount - 1) * PostMagnetRowSpacingZ;
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
