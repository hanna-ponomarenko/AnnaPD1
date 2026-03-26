using System.Collections.Generic;
using Featurehole.Runner.Core;
using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using UnityEngine;
using UnityEngine.Rendering;

namespace Featurehole.Runner.Gameplay
{
    public sealed class RockObstacleSpawner : MonoBehaviour
    {
        private const int RocksPerCycle = 3;
        private const float RockPadding = 0.7f;
        private const float TargetRockHeight = 0.95f;

        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private Transform rocksRoot;

        private readonly List<RockObstacle> activeRocks = new List<RockObstacle>();
        private readonly Dictionary<RockObstacle, int> intervalByRock = new Dictionary<RockObstacle, int>();
        private readonly Dictionary<RockObstacle, float> cycleStartByRock = new Dictionary<RockObstacle, float>();

        private RunnerGameConfig config;
        private HoleMover holeMover;
        private Material runtimeRockMaterial;

        public void SetRockPrefab(GameObject prefab)
        {
            rockPrefab = prefab;
        }

        public void Initialize(RunnerGameConfig runnerConfig, HoleMover runnerHoleMover)
        {
            config = runnerConfig;
            holeMover = runnerHoleMover;

            if (rocksRoot == null)
            {
                rocksRoot = transform;
            }

            ResetField();
        }

        public void ResetField()
        {
            if (config == null)
            {
                return;
            }

            ClearRocks();
            float initialCycleStart = config.SegmentLength;

            for (int index = 0; index < RocksPerCycle; index++)
            {
                SpawnRock(index, initialCycleStart);
            }
        }

        public bool Tick(float deltaTime, float forwardSpeed)
        {
            if (config == null || holeMover == null)
            {
                return false;
            }

            float moveDelta = -forwardSpeed * deltaTime;
            float passedThreshold = holeMover.transform.position.z - 1.5f;

            foreach (RockObstacle rock in activeRocks)
            {
                if (!rock.gameObject.activeSelf)
                {
                    continue;
                }

                rock.Move(moveDelta);

                if (holeMover.IntersectsObstacle(rock.transform.position, rock.CollisionRadius))
                {
                    return true;
                }

                if (rock.transform.position.z < passedThreshold)
                {
                    RespawnRock(rock);
                }
            }

            return false;
        }

        private void SpawnRock(int intervalIndex, float cycleStart)
        {
            Debug.Log(
                $"[RockObstacleSpawner] spawn attempt intervalIndex={intervalIndex} cycleStart={cycleStart:F2} prefab='{(rockPrefab != null ? rockPrefab.name : "null")}'",
                this);

            RockObstacle rock = rockPrefab != null
                ? CreateRockFromPrefab(intervalIndex)
                : CreateRuntimeRock(intervalIndex);

            Debug.Log(
                $"[RockObstacleSpawner] spawn success activeSpawner='{name}' prefab='{(rockPrefab != null ? rockPrefab.name : "null")}' createdObstacle='{rock.gameObject.name}' visualMode='{(rockPrefab != null ? "prefab" : "runtime")}' activeObstacleCount={activeRocks.Count + 1}",
                this);

            rock.gameObject.name = $"Rock_{intervalIndex}";
            activeRocks.Add(rock);
            intervalByRock[rock] = intervalIndex;
            cycleStartByRock[rock] = cycleStart;
            RespawnRock(rock);
        }

        private void RespawnRock(RockObstacle rock)
        {
            int intervalIndex = intervalByRock[rock];
            float cycleStart = cycleStartByRock[rock];
            Vector2 zWindow = SpawnCycleLayout.GetSubWindow(config, cycleStart, intervalIndex, 1, 2, RockPadding);

            float laneHalfWidth = Mathf.Max(0f, config.TrackWidth * 0.5f - 0.8f);
            float laneX = Random.Range(-laneHalfWidth, laneHalfWidth);
            float laneZ = Random.Range(zWindow.x, zWindow.y);
            float sizeScale = GetRandomSizeScale();

            rock.SetPosition(new Vector3(laneX, 0.55f, laneZ), sizeScale);
            cycleStartByRock[rock] = cycleStart + SpawnCycleLayout.GetCycleDistance(config);
            Debug.Log(
                $"[RockObstacleSpawner] spawned position name='{rock.gameObject.name}' position={rock.transform.position} sizeScale={sizeScale:F2} zWindow=({zWindow.x:F2},{zWindow.y:F2}) activeObstacleCount={activeRocks.Count}",
                this);
        }

        private void ClearRocks()
        {
            foreach (RockObstacle rock in activeRocks)
            {
                if (rock != null)
                {
                    Destroy(rock.gameObject);
                }
            }

            activeRocks.Clear();
            intervalByRock.Clear();
            cycleStartByRock.Clear();
        }

        private RockObstacle CreateRuntimeRock(int index)
        {
            GameObject root = new GameObject($"Rock_{index}");
            root.transform.SetParent(rocksRoot, false);

            RockObstacle rock = root.AddComponent<RockObstacle>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            body.transform.localScale = new Vector3(1.2f, 0.85f, 1f);
            rock.SetVisual(body.transform);

            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null)
            {
                Destroy(bodyCollider);
            }

            Renderer bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = new Color(0.46f, 0.4f, 0.34f);
                bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bodyRenderer.receiveShadows = false;
            }

            GameObject lump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lump.name = "Lump";
            lump.transform.SetParent(root.transform, false);
            lump.transform.localPosition = new Vector3(0.18f, 0.34f, -0.08f);
            lump.transform.localScale = new Vector3(0.5f, 0.35f, 0.46f);

            Collider lumpCollider = lump.GetComponent<Collider>();
            if (lumpCollider != null)
            {
                Destroy(lumpCollider);
            }

            Renderer lumpRenderer = lump.GetComponent<Renderer>();
            if (lumpRenderer != null)
            {
                lumpRenderer.material.color = new Color(0.4f, 0.35f, 0.29f);
                lumpRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lumpRenderer.receiveShadows = false;
            }

            return rock;
        }

        private RockObstacle CreateRockFromPrefab(int index)
        {
            GameObject root = new GameObject($"Rock_{index}");
            root.transform.SetParent(rocksRoot, false);

            RockObstacle rock = root.AddComponent<RockObstacle>();

            GameObject visual = Instantiate(rockPrefab, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            NormalizeVisualScale(visual.transform);

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }

            ApplyOpaqueRockLook(visual);

            rock.SetVisual(visual.transform);
            Debug.Log(
                $"[RockObstacleSpawner] instantiated prefabVisual='{visual.name}' sourcePrefab='{rockPrefab.name}' root='{root.name}'",
                this);
            return rock;
        }

        private void NormalizeVisualScale(Transform visualTransform)
        {
            Renderer[] renderers = visualTransform.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            float currentHeight = Mathf.Max(0.001f, bounds.size.y);
            float scaleFactor = TargetRockHeight / currentHeight;
            visualTransform.localScale *= scaleFactor;
        }

        private void ApplyOpaqueRockLook(GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Material rockMaterial = GetRuntimeRockMaterial();
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterial = rockMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private Material GetRuntimeRockMaterial()
        {
            if (runtimeRockMaterial != null)
            {
                return runtimeRockMaterial;
            }

            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            runtimeRockMaterial = new Material(shader)
            {
                name = "RuntimeRockOpaqueMaterial",
                color = new Color(0.43f, 0.39f, 0.35f, 1f)
            };

            if (runtimeRockMaterial.HasProperty("_Mode"))
            {
                runtimeRockMaterial.SetFloat("_Mode", 0f);
            }

            if (runtimeRockMaterial.HasProperty("_Metallic"))
            {
                runtimeRockMaterial.SetFloat("_Metallic", 0.02f);
            }

            if (runtimeRockMaterial.HasProperty("_Glossiness"))
            {
                runtimeRockMaterial.SetFloat("_Glossiness", 0.18f);
            }

            runtimeRockMaterial.renderQueue = (int)RenderQueue.Geometry;
            return runtimeRockMaterial;
        }

        private float GetRandomSizeScale()
        {
            int variant = Random.Range(0, 3);
            switch (variant)
            {
                case 0:
                    return 0.7f;
                case 1:
                    return 1f;
                default:
                    return 1.35f;
            }
        }
    }
}
