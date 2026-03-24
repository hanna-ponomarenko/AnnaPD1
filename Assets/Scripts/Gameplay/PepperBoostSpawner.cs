using System.Collections.Generic;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class PepperBoostSpawner : MonoBehaviour
    {
        [SerializeField] private PepperPickup pepperPrefab;
        [SerializeField] private Transform peppersRoot;

        private readonly List<PepperPickup> activePeppers = new List<PepperPickup>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private float nextSpawnZ;

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
            nextSpawnZ = config.SegmentLength * 1.5f;

            for (int index = 0; index < 3; index++)
            {
                SpawnPepper(index);
            }
        }

        public void Tick(float deltaTime, RunnerRuntime runtime, float forwardSpeed)
        {
            if (config == null || holeMover == null)
            {
                return;
            }

            float moveDelta = -forwardSpeed * deltaTime;
            Transform holeTransform = holeMover.transform;
            float absorbRadius = holeMover.CurrentDiameter * 0.5f + config.PepperSize * 0.3f;
            float passedThreshold = holeTransform.position.z - config.PepperSize;

            foreach (PepperPickup pepper in activePeppers)
            {
                if (!pepper.gameObject.activeSelf)
                {
                    continue;
                }

                pepper.Move(moveDelta);

                Vector3 pepperPosition = pepper.transform.position;
                float lateralDistance = Mathf.Abs(pepperPosition.x - holeTransform.position.x);
                float forwardDistance = Mathf.Abs(pepperPosition.z - holeTransform.position.z);

                if (lateralDistance <= absorbRadius && forwardDistance <= config.PepperSize)
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

        private void SpawnPepper(int index)
        {
            PepperPickup pepper = pepperPrefab != null
                ? Instantiate(pepperPrefab, peppersRoot)
                : CreateRuntimePepper(index);

            pepper.gameObject.name = $"Pepper_{index}";
            activePeppers.Add(pepper);
            RespawnPepper(pepper);
        }

        private void RespawnPepper(PepperPickup pepper)
        {
            float laneHalfWidth = Mathf.Max(0f, config.TrackWidth * 0.5f - config.PepperSize);
            float laneX = Random.Range(-laneHalfWidth, laneHalfWidth);
            Vector3 spawnPosition = new Vector3(laneX, 0.65f, nextSpawnZ);
            pepper.SetPosition(spawnPosition);
            nextSpawnZ += config.PepperSpawnSpacing;
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
        }

        private PepperPickup CreateRuntimePepper(int index)
        {
            GameObject root = new GameObject($"Pepper_{index}");
            root.transform.SetParent(peppersRoot, false);

            PepperPickup pepper = root.AddComponent<PepperPickup>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            body.transform.localScale = new Vector3(config.PepperSize * 0.55f, config.PepperSize * 0.75f, config.PepperSize * 0.55f);
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = new Color(0.92f, 0.14f, 0.08f);
            }

            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(root.transform, false);
            stem.transform.localPosition = new Vector3(0.18f, 0.55f, 0f);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            stem.transform.localScale = new Vector3(config.PepperSize * 0.12f, config.PepperSize * 0.18f, config.PepperSize * 0.12f);
            Renderer stemRenderer = stem.GetComponent<Renderer>();
            if (stemRenderer != null)
            {
                stemRenderer.material.color = new Color(0.14f, 0.48f, 0.11f);
            }

            return pepper;
        }
    }
}
