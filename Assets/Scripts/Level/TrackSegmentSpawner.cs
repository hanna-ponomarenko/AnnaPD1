using System.Collections.Generic;
using Featurehole.Runner.Data;
using UnityEngine;

namespace Featurehole.Runner.Level
{
    public sealed class TrackSegmentSpawner : MonoBehaviour
    {
        [SerializeField] private TrackSegment segmentPrefab;
        [SerializeField] private Transform segmentsRoot;

        private readonly Queue<TrackSegment> activeSegments = new Queue<TrackSegment>();

        private RunnerGameConfig config;
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

        public void Tick(float deltaTime)
        {
            if (config == null || activeSegments.Count == 0)
            {
                return;
            }

            float moveDelta = -config.ForwardSpeed * deltaTime;

            foreach (TrackSegment segment in activeSegments)
            {
                segment.Move(moveDelta);
            }

            TrackSegment firstSegment = activeSegments.Peek();
            if (firstSegment.TailZ < -config.DespawnOffset)
            {
                RecycleFirstSegment();
            }
        }

        private void BuildInitialTrack()
        {
            ClearSpawnedSegments();

            if (segmentPrefab == null)
            {
                Debug.LogWarning("TrackSegmentSpawner needs a segment prefab to build the track.", this);
                return;
            }

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
            segment.SetZ(nextSpawnZ);

            activeSegments.Enqueue(segment);
            nextSpawnZ += config.SegmentLength;
        }

        private void RecycleFirstSegment()
        {
            TrackSegment segment = activeSegments.Dequeue();
            segment.SetZ(nextSpawnZ);
            activeSegments.Enqueue(segment);
            nextSpawnZ += config.SegmentLength;
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

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(segmentObject.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(config.TrackWidth, 1f, config.SegmentLength);

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = index % 2 == 0
                    ? new Color(0.49f, 0.73f, 0.39f)
                    : new Color(0.44f, 0.67f, 0.34f);
            }

            return segment;
        }
    }
}
