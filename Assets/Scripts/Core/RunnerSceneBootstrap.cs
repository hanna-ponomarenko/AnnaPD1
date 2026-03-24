using Featurehole.Runner.Data;
using Featurehole.Runner.Hole;
using Featurehole.Runner.Level;
using UnityEngine;

namespace Featurehole.Runner.Core
{
    public sealed class RunnerSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private RunnerGameConfig config;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool configureMainCamera = true;

        private void Awake()
        {
            RunnerGameConfig runtimeConfig = config != null
                ? config
                : RunnerGameConfig.CreateRuntimeDefault();

            RunnerGameController controller = GetComponent<RunnerGameController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<RunnerGameController>();
            }

            HoleMover holeMover = CreateHole(runtimeConfig);
            TrackSegmentSpawner trackSpawner = CreateTrackSpawner();

            controller.Configure(runtimeConfig, holeMover, trackSpawner, autoStart);

            if (configureMainCamera)
            {
                ConfigureCamera();
            }
        }

        private HoleMover CreateHole(RunnerGameConfig runtimeConfig)
        {
            Transform holeRoot = transform.Find("Hole");
            if (holeRoot == null)
            {
                GameObject holeObject = new GameObject("Hole");
                holeObject.transform.SetParent(transform, false);
                holeRoot = holeObject.transform;
            }

            holeRoot.position = new Vector3(0f, 0.2f, 0f);

            HoleMover holeMover = holeRoot.GetComponent<HoleMover>();
            if (holeMover == null)
            {
                holeMover = holeRoot.gameObject.AddComponent<HoleMover>();
            }

            Transform visual = holeRoot.Find("Visual");
            if (visual == null)
            {
                GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visualObject.name = "Visual";
                visualObject.transform.SetParent(holeRoot, false);

                Collider visualCollider = visualObject.GetComponent<Collider>();
                if (visualCollider != null)
                {
                    Destroy(visualCollider);
                }

                Renderer renderer = visualObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.05f, 0.05f, 0.05f);
                }

                visual = visualObject.transform;
            }

            visual.localPosition = Vector3.zero;
            visual.localScale = new Vector3(runtimeConfig.HoleDiameter, 0.08f, runtimeConfig.HoleDiameter);

            return holeMover;
        }

        private TrackSegmentSpawner CreateTrackSpawner()
        {
            Transform spawnerRoot = transform.Find("TrackSpawner");
            if (spawnerRoot == null)
            {
                GameObject spawnerObject = new GameObject("TrackSpawner");
                spawnerObject.transform.SetParent(transform, false);
                spawnerRoot = spawnerObject.transform;
            }

            TrackSegmentSpawner trackSpawner = spawnerRoot.GetComponent<TrackSegmentSpawner>();
            if (trackSpawner == null)
            {
                trackSpawner = spawnerRoot.gameObject.AddComponent<TrackSegmentSpawner>();
            }

            return trackSpawner;
        }

        private void ConfigureCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            Transform cameraTransform = mainCamera.transform;
            cameraTransform.position = new Vector3(0f, 8f, -8f);
            cameraTransform.rotation = Quaternion.Euler(35f, 0f, 0f);

            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 50f;
            mainCamera.backgroundColor = new Color(0.65f, 0.86f, 0.98f);
        }
    }
}
