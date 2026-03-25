using System.Collections.Generic;
using Featurehole.Runner.Data;
using UnityEngine;

namespace Featurehole.Runner.Level
{
    public sealed class TrackSegmentSpawner : MonoBehaviour
    {
        private const int ContinuousSegmentCount = 2;

        [SerializeField] private TrackSegment segmentPrefab;
        [SerializeField] private Transform segmentsRoot;

        private readonly Queue<TrackSegment> activeSegments = new Queue<TrackSegment>();

        private RunnerGameConfig config;
        private float continuousTrackLength;
        private float nextSpawnZ;

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
            continuousTrackLength = config.InitialSegmentCount * config.SegmentLength;
            nextSpawnZ = 0f;

            for (int index = 0; index < ContinuousSegmentCount; index++)
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
            segment.Configure(continuousTrackLength);
            segment.SetZ(nextSpawnZ);

            activeSegments.Enqueue(segment);
            nextSpawnZ += continuousTrackLength;
        }

        private void RecycleFirstSegment()
        {
            TrackSegment segment = activeSegments.Dequeue();
            segment.SetZ(nextSpawnZ);
            activeSegments.Enqueue(segment);
            nextSpawnZ += continuousTrackLength;
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

            float riverWidth = config.TrackWidth * 0.55f;
            float bankWidth = (config.TrackWidth - riverWidth) * 0.5f + 0.15f;

            CreateBlock(segmentObject.transform, "Visual", Vector3.zero, new Vector3(riverWidth, 0.8f, 1f), new Color(0.09f, 0.46f, 0.76f));
            CreateBlock(segmentObject.transform, "RiverGlow", new Vector3(0f, 0.18f, 0f), new Vector3(riverWidth * 0.92f, 0.06f, 1f), new Color(0.3f, 0.73f, 0.95f));
            CreateBlock(segmentObject.transform, "BankLeft", new Vector3(-(riverWidth + bankWidth) * 0.5f, 0.02f, 0f), new Vector3(bankWidth, 0.86f, 1f), new Color(0.86f, 0.73f, 0.45f));
            CreateBlock(segmentObject.transform, "BankRight", new Vector3((riverWidth + bankWidth) * 0.5f, 0.02f, 0f), new Vector3(bankWidth, 0.86f, 1f), new Color(0.86f, 0.73f, 0.45f));

            CreateBlock(segmentObject.transform, "UnderwaterSand", new Vector3(0f, -0.18f, 0f), new Vector3(riverWidth * 0.78f, 0.08f, 1f), new Color(0.71f, 0.63f, 0.39f));
            CreateFish(segmentObject.transform, new Vector3(-riverWidth * 0.18f, -0.08f, -2.2f), new Color(0.99f, 0.72f, 0.34f), 0.22f);
            CreateFish(segmentObject.transform, new Vector3(riverWidth * 0.14f, -0.05f, 1.4f), new Color(0.96f, 0.54f, 0.18f), 0.18f);
            CreateShell(segmentObject.transform, new Vector3(-riverWidth * 0.08f, -0.12f, 3.6f), new Color(0.95f, 0.86f, 0.68f));
            CreateShell(segmentObject.transform, new Vector3(riverWidth * 0.2f, -0.12f, -4.1f), new Color(0.84f, 0.8f, 0.69f));
            CreateCrocodile(segmentObject.transform, new Vector3(0f, -0.03f, 5.2f), new Color(0.34f, 0.47f, 0.26f));

            CreateReeds(segmentObject.transform, new Vector3(-riverWidth * 0.5f - 0.08f, 0.15f, -3.5f), 5, -1f);
            CreateReeds(segmentObject.transform, new Vector3(riverWidth * 0.5f + 0.08f, 0.15f, 2.8f), 4, 1f);

            float sideOffsetX = config.TrackWidth * 0.5f + 2.6f;
            CreatePyramid(segmentObject.transform, "PyramidLeft", new Vector3(-sideOffsetX, 1.4f, -2f), new Vector3(2.4f, 2.1f, 2.4f), new Color(0.82f, 0.69f, 0.42f));
            CreatePyramid(segmentObject.transform, "PyramidRight", new Vector3(sideOffsetX + 0.6f, 1.2f, 4.2f), new Vector3(1.8f, 1.7f, 1.8f), new Color(0.76f, 0.63f, 0.36f));
            CreateRock(segmentObject.transform, "RockLeft", new Vector3(-sideOffsetX - 0.8f, 0.35f, 5.4f), new Vector3(1.6f, 0.9f, 1.2f), new Color(0.58f, 0.49f, 0.39f));
            CreateRock(segmentObject.transform, "RockRight", new Vector3(sideOffsetX + 1.4f, 0.3f, -5.2f), new Vector3(1.4f, 0.8f, 1f), new Color(0.64f, 0.56f, 0.43f));
            CreateStatue(segmentObject.transform, "AnubisLeft", new Vector3(-sideOffsetX + 0.3f, 0.6f, 6.6f), new Color(0.29f, 0.25f, 0.18f));
            CreateStatue(segmentObject.transform, "HorusRight", new Vector3(sideOffsetX - 0.2f, 0.6f, -6.8f), new Color(0.23f, 0.2f, 0.17f));

            return segment;
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

        private void CreateStatue(Transform parent, string name, Vector3 localPosition, Color color)
        {
            GameObject statue = new GameObject(name);
            statue.transform.SetParent(parent, false);
            statue.transform.localPosition = localPosition;

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
    }
}
