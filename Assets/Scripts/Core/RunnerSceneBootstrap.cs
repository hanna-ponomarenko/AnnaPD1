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
        [SerializeField] private Sprite pepperSprite;
        [SerializeField] private Material boostFireMaterial;
        [SerializeField] private UnityEngine.Object boostFirePrefab;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool configureMainCamera = true;

        private Material runtimeBoostFireMaterial;

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
            pepperSpawner.SetPepperSprite(pepperSprite);
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
            ConfigureHoleVisual(visual, runtimeConfig);

            Transform boostFlame = holeRoot.Find("BoostFlame");
            if (boostFlame == null)
            {
                GameObject flameObject = CreateBoostFire(holeRoot);
                if (flameObject != null)
                {
                    flameObject.name = "BoostFlame";
                    boostFlame = flameObject.transform;
                }
            }

            if (boostFlame != null)
            {
                ConfigureBoostFlame(boostFlame);
            }

            return holeMover;
        }

        private void ConfigureHoleVisual(Transform visual, RunnerGameConfig runtimeConfig)
        {
            visual.localPosition = Vector3.zero;
            visual.localScale = new Vector3(runtimeConfig.HoleDiameter, 0.14f, runtimeConfig.HoleDiameter);

            Renderer rimRenderer = visual.GetComponent<Renderer>();
            if (rimRenderer != null)
            {
                rimRenderer.material.color = new Color(0.96f, 0.78f, 0.18f);
            }

            ConfigureHoleLayer(
                visual,
                "SlopeOuter",
                new Vector3(0f, -0.03f, 0f),
                new Vector3(0.95f, 0.5f, 0.95f),
                new Color(0.74f, 0.7f, 0.64f));

            ConfigureHoleLayer(
                visual,
                "SlopeMid",
                new Vector3(0f, -0.09f, 0f),
                new Vector3(0.62f, 0.95f, 0.62f),
                new Color(0.42f, 0.42f, 0.42f));

            ConfigureHoleLayer(
                visual,
                "SlopeInner",
                new Vector3(0f, -0.16f, 0f),
                new Vector3(0.38f, 1.2f, 0.38f),
                new Color(0.15f, 0.15f, 0.15f));

            ConfigureHoleLayer(
                visual,
                "CoreDark",
                new Vector3(0f, -0.24f, 0f),
                new Vector3(0.24f, 1.35f, 0.24f),
                new Color(0.01f, 0.01f, 0.01f));
        }

        private void ConfigureHoleLayer(
            Transform parent,
            string layerName,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            Transform layer = parent.Find(layerName);
            if (layer == null)
            {
                GameObject layerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                layerObject.name = layerName;
                layerObject.transform.SetParent(parent, false);

                Collider layerCollider = layerObject.GetComponent<Collider>();
                if (layerCollider != null)
                {
                    Destroy(layerCollider);
                }

                layer = layerObject.transform;
            }

            layer.localPosition = localPosition;
            layer.localRotation = Quaternion.identity;
            layer.localScale = localScale;

            Renderer layerRenderer = layer.GetComponent<Renderer>();
            if (layerRenderer != null)
            {
                layerRenderer.material.color = color;
            }
        }

        private GameObject CreateBoostFire(Transform parent)
        {
            if (boostFirePrefab == null)
            {
                if (boostFireMaterial != null)
                {
                    return CreateRuntimeBoostFire(parent);
                }

                return null;
            }

            Object instance = null;

            if (boostFirePrefab is GameObject firePrefabGameObject)
            {
                instance = Instantiate(firePrefabGameObject, parent);
            }
            else if (boostFirePrefab is Component firePrefabComponent)
            {
                instance = Instantiate(firePrefabComponent, parent);
            }

            if (instance is GameObject fireObject)
            {
                return fireObject;
            }

            if (instance is Component fireComponent)
            {
                return fireComponent.gameObject;
            }

            Debug.LogWarning("Boost fire reference is not a prefab GameObject or Component.", this);
            return null;
        }

        private GameObject CreateRuntimeBoostFire(Transform parent)
        {
            GameObject flameObject = new GameObject("BoostFlame");
            flameObject.transform.SetParent(parent, false);
            CreateBoostFireLayer(flameObject.transform, "Core", 90, 0.7f, 1.1f, 0.35f, 0.55f);
            CreateBoostFireLayer(flameObject.transform, "Outer", 65, 1.1f, 1.8f, 0.45f, 0.8f);
            return flameObject;
        }

        private void CreateBoostFireLayer(
            Transform parent,
            string layerName,
            float emissionRate,
            float minSize,
            float maxSize,
            float minLifetime,
            float maxLifetime)
        {
            GameObject layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(parent, false);

            ParticleSystem particleSystem = layerObject.AddComponent<ParticleSystem>();
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystemRenderer renderer = layerObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetRuntimeBoostFireMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 75;

            var main = particleSystem.main;
            main.duration = 0.6f;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 96;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.18f;

            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.9f, 2.1f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);

            var limitVelocity = particleSystem.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.limit = 2.5f;
            limitVelocity.dampen = 0.35f;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.15f);
            sizeCurve.AddKey(0.2f, 0.85f);
            sizeCurve.AddKey(0.7f, 1f);
            sizeCurve.AddKey(1f, 0.05f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 0.8f), 0f),
                    new GradientColorKey(new Color(1f, 0.72f, 0.18f), 0.25f),
                    new GradientColorKey(new Color(1f, 0.25f, 0.04f), 0.7f),
                    new GradientColorKey(new Color(0.16f, 0.16f, 0.16f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.95f, 0.12f),
                    new GradientAlphaKey(0.8f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var noise = particleSystem.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.6f;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private Material GetRuntimeBoostFireMaterial()
        {
            if (runtimeBoostFireMaterial != null)
            {
                return runtimeBoostFireMaterial;
            }

            Shader particleShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (particleShader == null)
            {
                particleShader = Shader.Find("Particles/Standard Unlit");
            }

            if (particleShader == null || boostFireMaterial == null)
            {
                return runtimeBoostFireMaterial;
            }

            runtimeBoostFireMaterial = new Material(particleShader)
            {
                name = "RuntimeBoostFireMaterial"
            };

            Texture mainTexture = null;
            if (boostFireMaterial.HasProperty("_MainTexture"))
            {
                mainTexture = boostFireMaterial.GetTexture("_MainTexture");
            }

            if (mainTexture == null && boostFireMaterial.HasProperty("_MainTex"))
            {
                mainTexture = boostFireMaterial.GetTexture("_MainTex");
            }

            if (mainTexture != null)
            {
                runtimeBoostFireMaterial.mainTexture = mainTexture;
            }

            runtimeBoostFireMaterial.color = new Color(1f, 0.92f, 0.8f, 0.95f);
            return runtimeBoostFireMaterial;
        }

        private void ConfigureBoostFlame(Transform boostFlame)
        {
            boostFlame.localPosition = new Vector3(0f, 1.1f, -0.15f);
            boostFlame.localRotation = Quaternion.identity;
            boostFlame.localScale = new Vector3(0.28f, 0.28f, 0.28f);

            foreach (ParticleSystemRenderer renderer in boostFlame.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                renderer.sortingOrder = 75;
                renderer.enabled = false;
            }

            foreach (ParticleSystem particleSystem in boostFlame.GetComponentsInChildren<ParticleSystem>(true))
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
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
