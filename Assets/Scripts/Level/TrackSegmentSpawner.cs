using System.Collections.Generic;
using Featurehole.Runner.Data;
using UnityEngine;

namespace Featurehole.Runner.Level
{
    public sealed class TrackSegmentSpawner : MonoBehaviour
    {
        [SerializeField] private TrackSegment segmentPrefab;
        [SerializeField] private Transform segmentsRoot;
        [SerializeField] private bool logSegmentGap = true;

        private readonly Queue<TrackSegment> activeSegments = new Queue<TrackSegment>();

        private RunnerGameConfig config;
        private float chunkLength;
        private float nextSpawnZ;
        private Material runtimeRiverBaseMaterial;
        private Material runtimeRiverCurrentMaterial;
        private Material runtimeRiverFoamMaterial;
        private Texture2D runtimeRiverBaseTexture;
        private Texture2D runtimeRiverCurrentTexture;
        private Texture2D runtimeRiverFoamTexture;
        private float waterAnimationTime;

        public void Initialize(RunnerGameConfig runnerConfig)
        {
            config = runnerConfig;

            if (segmentsRoot == null)
            {
                segmentsRoot = transform;
            }

            BuildInitialTrack();
        }

        public void ResetTrack()
        {
            if (config == null)
            {
                return;
            }

            BuildInitialTrack();
        }

        public void Tick(float deltaTime, float forwardSpeed)
        {
            if (config == null || activeSegments.Count == 0)
            {
                return;
            }

            AnimateRiver(deltaTime, forwardSpeed);

            float moveDelta = -forwardSpeed * deltaTime;

            foreach (TrackSegment segment in activeSegments)
            {
                segment.Move(moveDelta);
            }

            TrackSegment firstSegment = activeSegments.Peek();
            if (firstSegment.MaxZ < -config.DespawnOffset)
            {
                RecycleFirstSegment();
            }
        }

        private void BuildInitialTrack()
        {
            ClearSpawnedSegments();
            chunkLength = config.SegmentLength;
            nextSpawnZ = 0f;

            for (int index = 0; index < config.InitialSegmentCount; index++)
            {
                SpawnSegment();
            }
        }

        private void SpawnSegment()
        {
            TrackSegment segment = segmentPrefab != null
                ? Instantiate(segmentPrefab, segmentsRoot)
                : CreateRuntimeSegment(activeSegments.Count);

            segment.gameObject.name = $"{segment.gameObject.name}_{activeSegments.Count}";
            segment.Configure(chunkLength);
            float previousEndZ = nextSpawnZ;
            segment.SetMinZ(nextSpawnZ);
            LogSegmentGap("spawn", segment, previousEndZ);

            activeSegments.Enqueue(segment);
            nextSpawnZ = segment.MaxZ;
        }

        private void RecycleFirstSegment()
        {
            TrackSegment segment = activeSegments.Dequeue();
            float previousEndZ = nextSpawnZ;
            segment.SetMinZ(nextSpawnZ);
            LogSegmentGap("recycle", segment, previousEndZ);
            activeSegments.Enqueue(segment);
            nextSpawnZ = segment.MaxZ;
        }

        private void ClearSpawnedSegments()
        {
            while (activeSegments.Count > 0)
            {
                TrackSegment spawnedSegment = activeSegments.Dequeue();

                if (spawnedSegment != null)
                {
                    Destroy(spawnedSegment.gameObject);
                }
            }
        }

        private TrackSegment CreateRuntimeSegment(int index)
        {
            GameObject segmentObject = new GameObject($"TrackSegment_{index}");
            segmentObject.transform.SetParent(segmentsRoot, false);
            segmentObject.transform.position = new Vector3(0f, -0.5f, 0f);

            TrackSegment segment = segmentObject.AddComponent<TrackSegment>();

            float riverWidth = config.TrackWidth;
            float bankWidth = 7.5f;
            float bankOffset = riverWidth * 0.5f + bankWidth * 0.5f;

            CreateBlock(
                segmentObject.transform,
                "BankLeft",
                new Vector3(-bankOffset, -0.06f, 0f),
                new Vector3(bankWidth, 0.7f, 1f),
                new Color(0.84f, 0.71f, 0.45f));
            CreateBlock(
                segmentObject.transform,
                "BankRight",
                new Vector3(bankOffset, -0.06f, 0f),
                new Vector3(bankWidth, 0.7f, 1f),
                new Color(0.84f, 0.71f, 0.45f));

            CreateRiverLayer(
                segmentObject.transform,
                "Visual",
                Vector3.zero,
                new Vector3(riverWidth, 0.8f, 1f),
                GetRuntimeRiverBaseMaterial());
            CreateRiverLayer(
                segmentObject.transform,
                "RiverCurrent",
                new Vector3(0f, 0.21f, 0f),
                new Vector3(riverWidth * 0.96f, 0.025f, 1f),
                GetRuntimeRiverCurrentMaterial());
            CreateRiverLayer(
                segmentObject.transform,
                "RiverGlow",
                new Vector3(0f, 0.18f, 0f),
                new Vector3(riverWidth * 0.92f, 0.06f, 1f),
                GetRuntimeRiverBaseMaterial());
            CreateRiverLayer(
                segmentObject.transform,
                "RiverFoamLeft",
                new Vector3(-riverWidth * 0.38f, 0.22f, 0f),
                new Vector3(riverWidth * 0.1f, 0.02f, 1f),
                GetRuntimeRiverFoamMaterial());
            CreateRiverLayer(
                segmentObject.transform,
                "RiverFoamRight",
                new Vector3(riverWidth * 0.38f, 0.22f, 0f),
                new Vector3(riverWidth * 0.1f, 0.02f, 1f),
                GetRuntimeRiverFoamMaterial());
            CreateBlock(segmentObject.transform, "UnderwaterSand", new Vector3(0f, -0.18f, 0f), new Vector3(riverWidth * 0.78f, 0.08f, 1f), new Color(0.71f, 0.63f, 0.39f));
            CreateFish(segmentObject.transform, new Vector3(-riverWidth * 0.28f, -0.08f, -3.2f), new Color(0.99f, 0.72f, 0.34f), 0.22f);
            CreateFish(segmentObject.transform, new Vector3(riverWidth * 0.22f, -0.05f, 0.8f), new Color(0.96f, 0.54f, 0.18f), 0.18f);
            CreateFish(segmentObject.transform, new Vector3(-riverWidth * 0.06f, -0.09f, 3.5f), new Color(0.96f, 0.84f, 0.4f), 0.2f);
            CreateShell(segmentObject.transform, new Vector3(-riverWidth * 0.18f, -0.12f, 2.4f), new Color(0.95f, 0.86f, 0.68f));
            CreateShell(segmentObject.transform, new Vector3(riverWidth * 0.24f, -0.12f, -2.6f), new Color(0.84f, 0.8f, 0.69f));
            CreateCrocodile(segmentObject.transform, new Vector3(0f, -0.03f, 1.8f), new Color(0.34f, 0.47f, 0.26f));

            CreateReeds(segmentObject.transform, new Vector3(-riverWidth * 0.42f, 0.06f, -3.2f), 5, -1f);
            CreateReeds(segmentObject.transform, new Vector3(riverWidth * 0.4f, 0.06f, 2.2f), 4, 1f);

            float sideOffsetX = riverWidth * 0.5f + 2.35f;
            CreatePyramid(segmentObject.transform, "PyramidLeft", new Vector3(-sideOffsetX - 1.6f, 1.15f, -2.4f), new Vector3(1.7f, 1.6f, 1.7f), new Color(0.82f, 0.69f, 0.42f));
            CreatePyramid(segmentObject.transform, "PyramidRight", new Vector3(sideOffsetX + 1.8f, 1.05f, 2.8f), new Vector3(1.45f, 1.35f, 1.45f), new Color(0.76f, 0.63f, 0.36f));
            CreateRock(segmentObject.transform, "RockLeft", new Vector3(-sideOffsetX - 0.5f, 0.28f, 2.6f), new Vector3(1.8f, 0.7f, 1.4f), new Color(0.58f, 0.49f, 0.39f));
            CreateRock(segmentObject.transform, "RockRight", new Vector3(sideOffsetX + 0.4f, 0.24f, -2.8f), new Vector3(1.7f, 0.65f, 1.2f), new Color(0.64f, 0.56f, 0.43f));
            CreateStatue(segmentObject.transform, "AnubisLeft", new Vector3(-sideOffsetX + 0.15f, 0.58f, 1.1f), new Color(0.29f, 0.25f, 0.18f), 0.9f);
            CreateStatue(segmentObject.transform, "HorusRight", new Vector3(sideOffsetX - 0.12f, 0.58f, -1.3f), new Color(0.23f, 0.2f, 0.17f), 0.9f);
            CreatePalm(segmentObject.transform, "PalmLeft", new Vector3(-sideOffsetX - 1.1f, 0.45f, 3.2f), -1f);
            CreatePalm(segmentObject.transform, "PalmRight", new Vector3(sideOffsetX + 1.05f, 0.45f, -3.3f), 1f);
            CreateObelisk(segmentObject.transform, "ObeliskLeft", new Vector3(-sideOffsetX - 2.2f, 0.9f, -0.8f), 0.75f);
            CreateObelisk(segmentObject.transform, "ObeliskRight", new Vector3(sideOffsetX + 2.2f, 0.9f, 0.95f), 0.75f);

            return segment;
        }

        private void AnimateRiver(float deltaTime, float forwardSpeed)
        {
            waterAnimationTime += deltaTime;
            float normalizedSpeed = Mathf.Max(0.2f, forwardSpeed / 6f);

            Material riverBaseMaterial = GetRuntimeRiverBaseMaterial();
            Material riverCurrentMaterial = GetRuntimeRiverCurrentMaterial();
            Material riverFoamMaterial = GetRuntimeRiverFoamMaterial();

            if (riverBaseMaterial != null)
            {
                if (riverBaseMaterial.HasProperty("_FlowSpeedA"))
                {
                    riverBaseMaterial.SetFloat("_FlowSpeedA", 0.05f * normalizedSpeed);
                    riverBaseMaterial.SetFloat("_FlowSpeedB", 0.11f * normalizedSpeed);
                    riverBaseMaterial.SetFloat("_WaveHeight", 0.055f + Mathf.Sin(waterAnimationTime * 0.45f) * 0.008f);
                }
                else
                {
                    Vector2 baseOffset = new Vector2(
                        Mathf.Sin(waterAnimationTime * 0.13f) * 0.03f,
                        -waterAnimationTime * 0.18f * normalizedSpeed);
                    riverBaseMaterial.mainTextureOffset = baseOffset;
                }
            }

            if (riverCurrentMaterial != null)
            {
                Vector2 currentOffset = new Vector2(
                    Mathf.Sin(waterAnimationTime * 0.65f) * 0.06f,
                    -waterAnimationTime * 0.62f * normalizedSpeed);
                riverCurrentMaterial.mainTextureOffset = currentOffset;
                riverCurrentMaterial.color = Color.Lerp(
                    new Color(0.34f, 0.84f, 0.98f, 0.36f),
                    new Color(0.52f, 0.94f, 1f, 0.48f),
                    (Mathf.Sin(waterAnimationTime * 1.7f) + 1f) * 0.5f);
            }

            if (riverFoamMaterial != null)
            {
                Vector2 foamOffset = new Vector2(
                    Mathf.Sin(waterAnimationTime * 0.9f) * 0.05f,
                    -waterAnimationTime * 1.1f * normalizedSpeed);
                riverFoamMaterial.mainTextureOffset = foamOffset;
                riverFoamMaterial.color = Color.Lerp(
                    new Color(0.88f, 0.97f, 1f, 0.42f),
                    new Color(1f, 1f, 1f, 0.7f),
                    (Mathf.Sin(waterAnimationTime * 2.4f) + 1f) * 0.5f);
            }
        }

        private void CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void CreateRiverLayer(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            layer.name = name;
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = localPosition;
            layer.transform.localScale = localScale;

            Renderer renderer = layer.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private Material GetRuntimeRiverBaseMaterial()
        {
            if (runtimeRiverBaseMaterial != null)
            {
                return runtimeRiverBaseMaterial;
            }

            runtimeRiverBaseTexture = CreateRiverBaseTexture();
            runtimeRiverCurrentTexture = runtimeRiverCurrentTexture != null ? runtimeRiverCurrentTexture : CreateRiverCurrentTexture();
            runtimeRiverFoamTexture = runtimeRiverFoamTexture != null ? runtimeRiverFoamTexture : CreateRiverFoamTexture();

            Shader waterShader = Shader.Find("Featurehole/Runner/Water");
            if (waterShader != null)
            {
                runtimeRiverBaseMaterial = new Material(waterShader)
                {
                    name = "RuntimeRiverBaseMaterial"
                };
                runtimeRiverBaseMaterial.SetTexture("_WaveTex", runtimeRiverBaseTexture);
                runtimeRiverBaseMaterial.SetTexture("_DetailTex", runtimeRiverCurrentTexture);
                runtimeRiverBaseMaterial.SetTexture("_FoamTex", runtimeRiverFoamTexture);
                runtimeRiverBaseMaterial.SetColor("_ShallowColor", new Color(0.32f, 0.68f, 0.96f, 0.84f));
                runtimeRiverBaseMaterial.SetColor("_DeepColor", new Color(0.06f, 0.18f, 0.58f, 0.94f));
                runtimeRiverBaseMaterial.SetColor("_FoamColor", Color.white);
                runtimeRiverBaseMaterial.SetFloat("_WaveScale", 2.1f);
                runtimeRiverBaseMaterial.SetFloat("_DetailScale", 4.2f);
                runtimeRiverBaseMaterial.SetFloat("_FlowSpeedA", 0.05f);
                runtimeRiverBaseMaterial.SetFloat("_FlowSpeedB", 0.11f);
                runtimeRiverBaseMaterial.SetFloat("_WaveHeight", 0.06f);
                runtimeRiverBaseMaterial.SetFloat("_NormalStrength", 1.4f);
                runtimeRiverBaseMaterial.SetFloat("_FoamStrength", 0.42f);
                runtimeRiverBaseMaterial.SetFloat("_FresnelStrength", 1.55f);
                runtimeRiverBaseMaterial.SetFloat("_Smoothness", 0.94f);
                runtimeRiverBaseMaterial.SetFloat("_Metallic", 0.02f);
                runtimeRiverBaseMaterial.SetFloat("_Alpha", 0.78f);
                runtimeRiverBaseMaterial.renderQueue = 3000;
            }
            else
            {
                runtimeRiverBaseMaterial = CreateTransparentUnlitMaterial("RuntimeRiverBaseMaterial", runtimeRiverBaseTexture);
                runtimeRiverBaseMaterial.color = new Color(0.62f, 0.9f, 1f, 0.96f);
                runtimeRiverBaseMaterial.mainTextureScale = new Vector2(1.45f, chunkLength * 0.16f);
            }

            return runtimeRiverBaseMaterial;
        }

        private Material GetRuntimeRiverCurrentMaterial()
        {
            if (runtimeRiverCurrentMaterial != null)
            {
                return runtimeRiverCurrentMaterial;
            }

            runtimeRiverCurrentTexture = CreateRiverCurrentTexture();
            runtimeRiverCurrentMaterial = CreateTransparentUnlitMaterial("RuntimeRiverCurrentMaterial", runtimeRiverCurrentTexture);
            runtimeRiverCurrentMaterial.color = new Color(0.4f, 0.88f, 1f, 0.42f);
            runtimeRiverCurrentMaterial.mainTextureScale = new Vector2(1.8f, chunkLength * 0.28f);
            return runtimeRiverCurrentMaterial;
        }

        private Material GetRuntimeRiverFoamMaterial()
        {
            if (runtimeRiverFoamMaterial != null)
            {
                return runtimeRiverFoamMaterial;
            }

            runtimeRiverFoamTexture = CreateRiverFoamTexture();
            runtimeRiverFoamMaterial = CreateTransparentUnlitMaterial("RuntimeRiverFoamMaterial", runtimeRiverFoamTexture);
            runtimeRiverFoamMaterial.color = new Color(0.96f, 0.99f, 1f, 0.58f);
            runtimeRiverFoamMaterial.mainTextureScale = new Vector2(1f, chunkLength * 0.42f);
            return runtimeRiverFoamMaterial;
        }

        private Material CreateTransparentUnlitMaterial(string materialName, Texture2D texture)
        {
            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            }

            Material material = new Material(shader)
            {
                name = materialName,
                mainTexture = texture
            };

            return material;
        }

        private Texture2D CreateRiverBaseTexture()
        {
            const int width = 128;
            const int height = 128;

            Texture2D texture = CreateRuntimeTexture("RiverBaseTexture", width, height);

            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float band = Mathf.Sin((u * 3.5f + v * 7.2f) * Mathf.PI) * 0.5f + 0.5f;
                    float ripple = Mathf.Sin((u * 11f - v * 4.5f) * Mathf.PI) * 0.5f + 0.5f;
                    Color deep = new Color(0.06f, 0.34f, 0.65f, 1f);
                    Color shallow = new Color(0.16f, 0.58f, 0.84f, 1f);
                    Color highlight = new Color(0.34f, 0.82f, 0.97f, 1f);
                    Color color = Color.Lerp(deep, shallow, band * 0.65f);
                    color = Color.Lerp(color, highlight, ripple * 0.2f);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private Texture2D CreateRiverCurrentTexture()
        {
            const int width = 128;
            const int height = 256;

            Texture2D texture = CreateRuntimeTexture("RiverCurrentTexture", width, height);

            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float veinA = Mathf.Abs(Mathf.Sin((u * 7f + v * 10f) * Mathf.PI));
                    float veinB = Mathf.Abs(Mathf.Sin((u * 13f - v * 16f) * Mathf.PI));
                    float streak = Mathf.SmoothStep(0.68f, 1f, Mathf.Max(veinA, veinB));
                    float alpha = streak * 0.85f;
                    texture.SetPixel(x, y, new Color(0.9f, 0.99f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private Texture2D CreateRiverFoamTexture()
        {
            const int width = 64;
            const int height = 256;

            Texture2D texture = CreateRuntimeTexture("RiverFoamTexture", width, height);

            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float edgeNoise = Mathf.Sin((v * 18f + u * 5f) * Mathf.PI) * 0.5f + 0.5f;
                    float bubbles = Mathf.Sin((v * 33f - u * 3f) * Mathf.PI) * 0.5f + 0.5f;
                    float alpha = Mathf.SmoothStep(0.62f, 1f, edgeNoise * 0.7f + bubbles * 0.3f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private Texture2D CreateRuntimeTexture(string textureName, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            return texture;
        }

        private void CreateFish(Transform parent, Vector3 localPosition, Color color, float size)
        {
            GameObject fish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fish.name = "Fish";
            fish.transform.SetParent(parent, false);
            fish.transform.localPosition = localPosition;
            fish.transform.localScale = new Vector3(size * 1.6f, size * 0.7f, size);

            Renderer renderer = fish.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "Tail";
            tail.transform.SetParent(fish.transform, false);
            tail.transform.localPosition = new Vector3(-0.65f, 0f, 0f);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            tail.transform.localScale = new Vector3(0.38f, 0.2f, 0.08f);

            Renderer tailRenderer = tail.GetComponent<Renderer>();
            if (tailRenderer != null)
            {
                tailRenderer.material.color = color * 0.9f;
            }
        }

        private void CreateShell(Transform parent, Vector3 localPosition, Color color)
        {
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "Shell";
            shell.transform.SetParent(parent, false);
            shell.transform.localPosition = localPosition;
            shell.transform.localScale = new Vector3(0.22f, 0.1f, 0.22f);

            Renderer renderer = shell.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void CreateCrocodile(Transform parent, Vector3 localPosition, Color color)
        {
            GameObject crocodile = new GameObject("Crocodile");
            crocodile.transform.SetParent(parent, false);
            crocodile.transform.localPosition = localPosition;

            CreateBlock(crocodile.transform, "Body", Vector3.zero, new Vector3(0.46f, 0.12f, 1.1f), color);
            CreateBlock(crocodile.transform, "Head", new Vector3(0f, 0.01f, 0.64f), new Vector3(0.36f, 0.1f, 0.38f), color * 1.08f);
            CreateBlock(crocodile.transform, "Tail", new Vector3(0f, 0f, -0.72f), new Vector3(0.18f, 0.08f, 0.5f), color * 0.92f);
        }

        private void CreateReeds(Transform parent, Vector3 localPosition, int count, float direction)
        {
            GameObject reeds = new GameObject("Reeds");
            reeds.transform.SetParent(parent, false);
            reeds.transform.localPosition = localPosition;

            for (int index = 0; index < count; index++)
            {
                GameObject reed = GameObject.CreatePrimitive(PrimitiveType.Cube);
                reed.name = $"Reed_{index}";
                reed.transform.SetParent(reeds.transform, false);
                reed.transform.localPosition = new Vector3(index * 0.08f * direction, index * 0.03f, index * 0.12f);
                reed.transform.localRotation = Quaternion.Euler(0f, 0f, -10f * direction);
                reed.transform.localScale = new Vector3(0.04f, 0.45f + index * 0.05f, 0.04f);

                Renderer renderer = reed.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.36f, 0.57f, 0.24f);
                }
            }
        }

        private void CreateRock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = name;
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = localPosition;
            rock.transform.localRotation = Quaternion.Euler(12f, 25f, 8f);
            rock.transform.localScale = localScale;

            Renderer renderer = rock.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void CreatePalm(Transform parent, string name, Vector3 localPosition, float direction)
        {
            GameObject palm = new GameObject(name);
            palm.transform.SetParent(parent, false);
            palm.transform.localPosition = localPosition;

            CreateBlock(palm.transform, "Trunk", new Vector3(0f, 0.55f, 0f), new Vector3(0.16f, 1.15f, 0.16f), new Color(0.52f, 0.34f, 0.16f));

            for (int index = 0; index < 4; index++)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = $"Leaf_{index}";
                leaf.transform.SetParent(palm.transform, false);
                leaf.transform.localPosition = new Vector3(0.08f * direction, 1.1f, 0f);
                leaf.transform.localRotation = Quaternion.Euler(22f, index * 90f, -28f * direction);
                leaf.transform.localScale = new Vector3(0.12f, 0.04f, 0.82f);

                Renderer leafRenderer = leaf.GetComponent<Renderer>();
                if (leafRenderer != null)
                {
                    leafRenderer.material.color = new Color(0.22f, 0.49f, 0.18f);
                }
            }
        }

        private void CreateObelisk(Transform parent, string name, Vector3 localPosition, float height)
        {
            GameObject obelisk = new GameObject(name);
            obelisk.transform.SetParent(parent, false);
            obelisk.transform.localPosition = localPosition;

            CreateBlock(obelisk.transform, "Body", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.28f, height, 0.28f), new Color(0.78f, 0.67f, 0.45f));
            CreatePyramid(obelisk.transform, "Tip", new Vector3(0f, height + 0.06f, 0f), new Vector3(0.24f, 0.18f, 0.24f), new Color(0.9f, 0.78f, 0.48f));
        }

        private void CreateStatue(Transform parent, string name, Vector3 localPosition, Color color, float scaleMultiplier)
        {
            GameObject statue = new GameObject(name);
            statue.transform.SetParent(parent, false);
            statue.transform.localPosition = localPosition;
            statue.transform.localScale = Vector3.one * scaleMultiplier;

            CreateBlock(statue.transform, "Base", new Vector3(0f, -0.3f, 0f), new Vector3(0.9f, 0.3f, 0.9f), new Color(0.71f, 0.58f, 0.34f));
            CreateBlock(statue.transform, "Body", new Vector3(0f, 0.2f, 0f), new Vector3(0.42f, 1.2f, 0.32f), color);
            CreateBlock(statue.transform, "Arms", new Vector3(0f, 0.26f, 0f), new Vector3(0.75f, 0.16f, 0.2f), color * 1.05f);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            head.name = "Head";
            head.transform.SetParent(statue.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            head.transform.localScale = new Vector3(0.34f, 0.26f, 0.3f);

            Renderer headRenderer = head.GetComponent<Renderer>();
            if (headRenderer != null)
            {
                headRenderer.material.color = color * 1.08f;
            }
        }

        private void CreatePyramid(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject pyramid = new GameObject(name);
            pyramid.transform.SetParent(parent, false);
            pyramid.transform.localPosition = localPosition;
            pyramid.transform.localScale = localScale;

            MeshFilter meshFilter = pyramid.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreatePyramidMesh();

            MeshRenderer meshRenderer = pyramid.AddComponent<MeshRenderer>();
            meshRenderer.material.color = color;
        }

        private Mesh CreatePyramidMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "RuntimePyramid"
            };

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0f, 1f, 0f)
            };

            mesh.triangles = new[]
            {
                0, 4, 1,
                1, 4, 2,
                2, 4, 3,
                3, 4, 0,
                0, 1, 2,
                0, 2, 3
            };

            mesh.RecalculateNormals();
            return mesh;
        }

        private void LogSegmentGap(string phase, TrackSegment segment, float previousEndZ)
        {
            if (!logSegmentGap || segment == null)
            {
                return;
            }

            float newStartZ = segment.MinZ;
            float gapDistance = newStartZ - previousEndZ;
            Debug.Log(
                $"[TrackSegmentSpawner] {phase} segment='{segment.gameObject.name}' prevEnd={previousEndZ:0.###} newStart={newStartZ:0.###} gap={gapDistance:0.#####}",
                this);
        }
    }
}
