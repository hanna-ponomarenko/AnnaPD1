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

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(segmentObject.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(config.TrackWidth, 1f, 1f);

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.47f, 0.71f, 0.37f);
            }

            return segment;
        }
    }
}
