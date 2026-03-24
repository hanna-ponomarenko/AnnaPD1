using Featurehole.Runner.Data;
using Featurehole.Runner.Gameplay;
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
            PepperBoostSpawner pepperSpawner = CreatePepperSpawner();
            RunnerHud hud = CreateHud();

            controller.Configure(runtimeConfig, holeMover, trackSpawner, pepperSpawner, hud, autoStart);

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

            Transform boostFlame = holeRoot.Find("BoostFlame");
            if (boostFlame == null)
            {
                GameObject flameObject = new GameObject("BoostFlame");
                flameObject.transform.SetParent(holeRoot, false);
                flameObject.transform.localPosition = new Vector3(0f, 0.08f, -0.9f);
                flameObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                ParticleSystem flame = flameObject.AddComponent<ParticleSystem>();
                var main = flame.main;
                main.duration = 0.35f;
                main.loop = true;
                main.startLifetime = 0.25f;
                main.startSpeed = 2.8f;
                main.startSize = 0.5f;
                main.startColor = new Color(1f, 0.45f, 0.05f, 0.95f);
                main.simulationSpace = ParticleSystemSimulationSpace.Local;

                var emission = flame.emission;
                emission.rateOverTime = 26f;

                var shape = flame.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 18f;
                shape.radius = 0.2f;

                var colorOverLifetime = flame.colorOverLifetime;
                colorOverLifetime.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0f),
                        new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.5f),
                        new GradientColorKey(new Color(0.35f, 0.35f, 0.35f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.9f, 0f),
                        new GradientAlphaKey(0.4f, 0.6f),
                        new GradientAlphaKey(0f, 1f)
                    });
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

                flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

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

        private PepperBoostSpawner CreatePepperSpawner()
        {
            Transform spawnerRoot = transform.Find("PepperSpawner");
            if (spawnerRoot == null)
            {
                GameObject spawnerObject = new GameObject("PepperSpawner");
                spawnerObject.transform.SetParent(transform, false);
                spawnerRoot = spawnerObject.transform;
            }

            PepperBoostSpawner pepperSpawner = spawnerRoot.GetComponent<PepperBoostSpawner>();
            if (pepperSpawner == null)
            {
                pepperSpawner = spawnerRoot.gameObject.AddComponent<PepperBoostSpawner>();
            }

            return pepperSpawner;
        }

        private RunnerHud CreateHud()
        {
            RunnerHud hud = GetComponent<RunnerHud>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<RunnerHud>();
            }

            return hud;
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
