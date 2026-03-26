using Featurehole.Runner.Data;
using Featurehole.Runner.Gameplay;
using Featurehole.Runner.Hole;
using Featurehole.Runner.Level;
using Featurehole.Runner.Audio;
using System.IO;
using UnityEngine;

namespace Featurehole.Runner.Core
{
    public sealed class RunnerSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private RunnerGameConfig config;
        [SerializeField] private Sprite pepperSprite;
        [SerializeField] private Sprite appleSprite;
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip rockImpactSfx;
        [SerializeField] private AudioClip loseSfx;
        [SerializeField] private Material boostFireMaterial;
        [SerializeField] private UnityEngine.Object boostFirePrefab;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool configureMainCamera = true;

        private Material runtimeBoostFireMaterial;
        private Material runtimeHoleDecalMaterial;
        private Material runtimeHoleAuraMaterial;
        private Material runtimeSunMaterial;

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
            AppleSpawner appleSpawner = CreateAppleSpawner();
            appleSpawner.SetAppleSprite(appleSprite);
            PepperBoostSpawner pepperSpawner = CreatePepperSpawner();
            pepperSpawner.SetPepperSprite(pepperSprite);
            CoinSpawner coinSpawner = CreateCoinSpawner();
            RockObstacleSpawner rockSpawner = CreateRockSpawner();
            MusicManager musicManager = CreateMusicManager();
            GameSfxManager sfxManager = CreateSfxManager();
            RunnerHud hud = CreateHud();

            controller.Configure(runtimeConfig, holeMover, trackSpawner, appleSpawner, pepperSpawner, coinSpawner, rockSpawner, musicManager, sfxManager, hud, autoStart);

            if (configureMainCamera)
            {
                ConfigureCamera();
            }

            ConfigureBackdrop(runtimeConfig);
        }

        private MusicManager CreateMusicManager()
        {
            Transform musicRoot = transform.Find("MusicManager");
            if (musicRoot == null)
            {
                GameObject musicObject = new GameObject("MusicManager");
                musicObject.transform.SetParent(transform, false);
                musicRoot = musicObject.transform;
            }

            MusicManager musicManager = musicRoot.GetComponent<MusicManager>();
            if (musicManager == null)
            {
                musicManager = musicRoot.gameObject.AddComponent<MusicManager>();
            }

            musicManager.Configure(backgroundMusic);
            return musicManager;
        }

        private GameSfxManager CreateSfxManager()
        {
            Transform sfxRoot = transform.Find("SfxManager");
            if (sfxRoot == null)
            {
                GameObject sfxObject = new GameObject("SfxManager");
                sfxObject.transform.SetParent(transform, false);
                sfxRoot = sfxObject.transform;
            }

            GameSfxManager sfxManager = sfxRoot.GetComponent<GameSfxManager>();
            if (sfxManager == null)
            {
                sfxManager = sfxRoot.gameObject.AddComponent<GameSfxManager>();
            }

            sfxManager.Configure(rockImpactSfx, loseSfx);
            return sfxManager;
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
                GameObject visualObject = new GameObject("Visual");
                visualObject.transform.SetParent(holeRoot, false);
                visual = visualObject.transform;
            }

            ConfigureHoleDecal(visual, runtimeConfig);
            ConfigureBoostAura(visual);
            ConfigureBoostHorns(visual);

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

        private void ConfigureHoleDecal(Transform visualRoot, RunnerGameConfig runtimeConfig)
        {
            visualRoot.localPosition = new Vector3(0f, 0.08f, 0f);
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = new Vector3(runtimeConfig.HoleDiameter, 1f, runtimeConfig.HoleDiameter);

            Transform decalTransform = visualRoot.Find("Decal");
            if (decalTransform == null)
            {
                GameObject decalObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decalObject.name = "Decal";
                decalObject.transform.SetParent(visualRoot, false);

                Collider decalCollider = decalObject.GetComponent<Collider>();
                if (decalCollider != null)
                {
                    Destroy(decalCollider);
                }

                decalTransform = decalObject.transform;
            }

            decalTransform.localPosition = Vector3.zero;
            decalTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            decalTransform.localScale = Vector3.one;

            Renderer decalRenderer = decalTransform.GetComponent<Renderer>();
            if (decalRenderer != null)
            {
                Material decalMaterial = GetRuntimeHoleDecalMaterial();
                decalMaterial.renderQueue = 4000;
                decalRenderer.material = decalMaterial;
                decalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                decalRenderer.receiveShadows = false;
                decalRenderer.sortingOrder = 250;
            }
        }

        private Material GetRuntimeHoleDecalMaterial()
        {
            if (runtimeHoleDecalMaterial != null)
            {
                return runtimeHoleDecalMaterial;
            }

            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            }

            runtimeHoleDecalMaterial = new Material(shader)
            {
                name = "RuntimeHoleDecalMaterial",
                mainTexture = CreateHoleDecalTexture()
            };

            return runtimeHoleDecalMaterial;
        }

        private void ConfigureBoostAura(Transform visualRoot)
        {
            Transform auraTransform = visualRoot.Find("BoostAura");
            if (auraTransform == null)
            {
                GameObject auraObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                auraObject.name = "BoostAura";
                auraObject.transform.SetParent(visualRoot, false);

                Collider auraCollider = auraObject.GetComponent<Collider>();
                if (auraCollider != null)
                {
                    Destroy(auraCollider);
                }

                auraTransform = auraObject.transform;
            }

            auraTransform.localPosition = new Vector3(0f, -0.005f, 0f);
            auraTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            auraTransform.localScale = Vector3.one * 1.8f;
            auraTransform.gameObject.SetActive(false);

            Renderer auraRenderer = auraTransform.GetComponent<Renderer>();
            if (auraRenderer != null)
            {
                Material auraMaterial = GetRuntimeHoleAuraMaterial();
                auraMaterial.renderQueue = 3990;
                auraRenderer.material = auraMaterial;
                auraRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                auraRenderer.receiveShadows = false;
                auraRenderer.sortingOrder = 240;
            }
        }

        private Material GetRuntimeHoleAuraMaterial()
        {
            if (runtimeHoleAuraMaterial != null)
            {
                return runtimeHoleAuraMaterial;
            }

            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            }

            runtimeHoleAuraMaterial = new Material(shader)
            {
                name = "RuntimeHoleAuraMaterial",
                mainTexture = CreateHoleAuraTexture(),
                color = Color.white
            };

            return runtimeHoleAuraMaterial;
        }

        private Texture2D CreateHoleDecalTexture()
        {
            const int textureSize = 256;

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = "HoleDecalTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
            float radius = textureSize * 0.48f;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float distance = Vector2.Distance(point, center) / radius;
                    Color color = Color.clear;

                    if (distance <= 1f)
                    {
                        if (distance > 0.88f)
                        {
                            color = new Color(1f, 0.9f, 0.45f, 1f);
                        }
                        else if (distance > 0.76f)
                        {
                            float t = Mathf.InverseLerp(0.88f, 0.76f, distance);
                            color = Color.Lerp(
                                new Color(0.96f, 0.77f, 0.2f, 1f),
                                new Color(0.55f, 0.4f, 0.14f, 1f),
                                t);
                        }
                        else
                        {
                            float t = Mathf.InverseLerp(0.76f, 0.08f, distance);
                            color = Color.Lerp(
                                new Color(0.18f, 0.14f, 0.06f, 1f),
                                new Color(0.01f, 0.01f, 0.01f, 1f),
                                t);
                        }
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private Texture2D CreateHoleAuraTexture()
        {
            const int textureSize = 256;

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = "HoleAuraTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
            float radius = textureSize * 0.48f;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    Color color = Color.clear;

                    if (distance <= 1f)
                    {
                        float outerGlow = Mathf.SmoothStep(1f, 0.62f, distance);
                        float innerCut = Mathf.SmoothStep(0.28f, 0.12f, distance);
                        float alpha = Mathf.Clamp01(outerGlow - innerCut) * 0.95f;
                        color = new Color(1f, 1f, 1f, alpha);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private void ConfigureBoostHorns(Transform visualRoot)
        {
            Transform hornsRoot = visualRoot.Find("BoostHorns");
            if (hornsRoot == null)
            {
                GameObject hornsObject = new GameObject("BoostHorns");
                hornsObject.transform.SetParent(visualRoot, false);
                hornsRoot = hornsObject.transform;
            }

            hornsRoot.localPosition = new Vector3(0f, 0.42f, -0.9f);
            hornsRoot.localRotation = Quaternion.identity;
            hornsRoot.localScale = Vector3.one;
            hornsRoot.gameObject.SetActive(false);

            CreateHorn(hornsRoot, "LeftHorn", new Vector3(-1.2f, 0.45f, 0f), -28f);
            CreateHorn(hornsRoot, "RightHorn", new Vector3(1.2f, 0.45f, 0f), 28f);
        }

        private void CreateHorn(Transform parent, string hornName, Vector3 localPosition, float zRotation)
        {
            Transform hornTransform = parent.Find(hornName);
            if (hornTransform == null)
            {
                GameObject hornObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hornObject.name = hornName;
                hornObject.transform.SetParent(parent, false);

                Collider hornCollider = hornObject.GetComponent<Collider>();
                if (hornCollider != null)
                {
                    Destroy(hornCollider);
                }

                hornTransform = hornObject.transform;
            }

            hornTransform.localPosition = localPosition;
            hornTransform.localRotation = Quaternion.Euler(68f, 0f, zRotation);
            hornTransform.localScale = new Vector3(0.6f, 1.8f, 0.6f);

            Renderer hornRenderer = hornTransform.GetComponent<Renderer>();
            if (hornRenderer != null)
            {
                hornRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                hornRenderer.receiveShadows = false;
                hornRenderer.material.color = new Color(0.86f, 0.12f, 0.12f, 1f);
            }
        }

        private GameObject CreateBoostFire(Transform parent)
        {
            return CreateRuntimeBoostFire(parent);
        }

        private GameObject CreateRuntimeBoostFire(Transform parent)
        {
            GameObject flameObject = new GameObject("BoostFlame");
            flameObject.transform.SetParent(parent, false);
            CreateBoostFireLayer(flameObject.transform, "Core", 64f, 0.45f, 0.72f, 0.25f, 0.42f);
            CreateBoostFireLayer(flameObject.transform, "Outer", 42f, 0.72f, 1.1f, 0.3f, 0.52f);
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
            main.duration = 0.45f;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.15f);
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
            shape.angle = 14f;
            shape.radius = 0.09f;

            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);

            var limitVelocity = particleSystem.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.limit = 2f;
            limitVelocity.dampen = 0.25f;

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
                    new GradientColorKey(new Color(1f, 0.82f, 0.82f), 0f),
                    new GradientColorKey(new Color(1f, 0.22f, 0.18f), 0.18f),
                    new GradientColorKey(new Color(0.86f, 0.04f, 0.04f), 0.65f),
                    new GradientColorKey(new Color(0.22f, 0.02f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.95f, 0.08f),
                    new GradientAlphaKey(0.9f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var noise = particleSystem.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
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

            if (particleShader == null)
            {
                return runtimeBoostFireMaterial;
            }

            runtimeBoostFireMaterial = new Material(particleShader)
            {
                name = "RuntimeBoostFireMaterial"
            };

            runtimeBoostFireMaterial.mainTexture = CreateRuntimeFireTexture();

            runtimeBoostFireMaterial.color = new Color(1f, 0.92f, 0.8f, 0.95f);
            return runtimeBoostFireMaterial;
        }

        private Texture2D CreateRuntimeFireTexture()
        {
            const int textureWidth = 128;
            const int textureHeight = 256;

            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                name = "RuntimeFireTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(textureWidth * 0.5f, textureHeight * 0.12f);
            Vector2 ellipse = new Vector2(textureWidth * 0.26f, textureHeight * 0.76f);

            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    float normalizedX = (x - center.x) / ellipse.x;
                    float normalizedY = (y - center.y) / ellipse.y;
                    float distance = normalizedX * normalizedX + normalizedY * normalizedY;

                    if (distance >= 1f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float edgeFade = Mathf.Pow(1f - distance, 1.8f);
                    float verticalT = y / (float)(textureHeight - 1);
                    float lengthFade = Mathf.Clamp01(1f - verticalT);
                    float alpha = edgeFade * Mathf.Pow(lengthFade, 0.55f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private void ConfigureBoostFlame(Transform boostFlame)
        {
            boostFlame.localPosition = new Vector3(0f, 0.2f, -1.4f);
            boostFlame.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            boostFlame.localScale = new Vector3(800f, 800f, 800f);

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

        private AppleSpawner CreateAppleSpawner()
        {
            Transform spawnerRoot = transform.Find("AppleSpawner");
            if (spawnerRoot == null)
            {
                GameObject spawnerObject = new GameObject("AppleSpawner");
                spawnerObject.transform.SetParent(transform, false);
                spawnerRoot = spawnerObject.transform;
            }

            AppleSpawner appleSpawner = spawnerRoot.GetComponent<AppleSpawner>();
            if (appleSpawner == null)
            {
                appleSpawner = spawnerRoot.gameObject.AddComponent<AppleSpawner>();
            }

            return appleSpawner;
        }

        private CoinSpawner CreateCoinSpawner()
        {
            Transform spawnerRoot = transform.Find("CoinSpawner");
            if (spawnerRoot == null)
            {
                GameObject spawnerObject = new GameObject("CoinSpawner");
                spawnerObject.transform.SetParent(transform, false);
                spawnerRoot = spawnerObject.transform;
            }

            CoinSpawner coinSpawner = spawnerRoot.GetComponent<CoinSpawner>();
            if (coinSpawner == null)
            {
                coinSpawner = spawnerRoot.gameObject.AddComponent<CoinSpawner>();
            }

            return coinSpawner;
        }

        private RockObstacleSpawner CreateRockSpawner()
        {
            Transform spawnerRoot = transform.Find("RockSpawner");
            if (spawnerRoot == null)
            {
                GameObject spawnerObject = new GameObject("RockSpawner");
                spawnerObject.transform.SetParent(transform, false);
                spawnerRoot = spawnerObject.transform;
            }

            RockObstacleSpawner rockSpawner = spawnerRoot.GetComponent<RockObstacleSpawner>();
            if (rockSpawner == null)
            {
                rockSpawner = spawnerRoot.gameObject.AddComponent<RockObstacleSpawner>();
            }

            return rockSpawner;
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
            cameraTransform.position = new Vector3(0f, 6.8f, -7.2f);
            cameraTransform.rotation = Quaternion.Euler(38f, 0f, 0f);

            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 50f;
            mainCamera.backgroundColor = new Color(0.94f, 0.78f, 0.52f);
        }

        private void ConfigureBackdrop(RunnerGameConfig runtimeConfig)
        {
            CreateBackdropPlane(
                "DesertBackdrop",
                new Vector3(0f, 11f, 62f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(120f, 42f, 1f),
                new Color(0.89f, 0.66f, 0.39f));

            CreateBackdropPlane(
                "DuneBand",
                new Vector3(0f, 2.5f, 48f),
                Quaternion.Euler(18f, 180f, 0f),
                new Vector3(100f, 10f, 1f),
                new Color(0.82f, 0.62f, 0.34f));

            CreateBackdropPlane(
                "DesertSideLeft",
                new Vector3(-18f, 2.5f, 18f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(90f, 18f, 1f),
                new Color(0.8f, 0.61f, 0.34f));

            CreateBackdropPlane(
                "DesertSideRight",
                new Vector3(18f, 2.5f, 18f),
                Quaternion.Euler(0f, -90f, 0f),
                new Vector3(90f, 18f, 1f),
                new Color(0.8f, 0.61f, 0.34f));

            CreateBackdropPlane(
                "DesertFloor",
                new Vector3(0f, -2f, 24f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(110f, 80f, 1f),
                new Color(0.77f, 0.58f, 0.31f));
            RemoveSun();
        }

        private void CreateBackdropPlane(string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color color)
        {
            Transform existing = transform.Find(name);
            GameObject plane = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = name;
            plane.transform.SetParent(transform, false);
            plane.transform.localPosition = localPosition;
            plane.transform.localRotation = localRotation;
            plane.transform.localScale = localScale;

            Collider collider = plane.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = plane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.material.color = color;
            }
        }

        private void CreateSun(Vector3 localPosition, float size)
        {
            Transform sunTransform = transform.Find("Sun");
            GameObject sunObject = sunTransform != null ? sunTransform.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
            sunObject.name = "Sun";
            sunObject.transform.SetParent(transform, false);
            sunObject.transform.localPosition = localPosition;
            sunObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            sunObject.transform.localScale = new Vector3(size, size, 1f);

            Collider sunCollider = sunObject.GetComponent<Collider>();
            if (sunCollider != null)
            {
                Destroy(sunCollider);
            }

            Renderer sunRenderer = sunObject.GetComponent<Renderer>();
            if (sunRenderer != null)
            {
                sunRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sunRenderer.receiveShadows = false;
                sunRenderer.material = GetRuntimeSunMaterial();
            }

            CreateSunRay(sunObject.transform, "RayVertical", Vector3.zero, Vector3.zero, new Vector3(0.45f, 6.5f, 0.08f));
            CreateSunRay(sunObject.transform, "RayHorizontal", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.45f, 6.5f, 0.08f));
            CreateSunRay(sunObject.transform, "RayDiagA", Vector3.zero, new Vector3(0f, 0f, 45f), new Vector3(0.35f, 5.4f, 0.08f));
            CreateSunRay(sunObject.transform, "RayDiagB", Vector3.zero, new Vector3(0f, 0f, -45f), new Vector3(0.35f, 5.4f, 0.08f));
        }

        private void RemoveSun()
        {
            Transform sunTransform = transform.Find("Sun");
            if (sunTransform != null)
            {
                Destroy(sunTransform.gameObject);
            }
        }

        private void CreateSunRay(Transform parent, string name, Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
        {
            Transform rayTransform = parent.Find(name);
            GameObject rayObject = rayTransform != null ? rayTransform.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            rayObject.name = name;
            rayObject.transform.SetParent(parent, false);
            rayObject.transform.localPosition = localPosition;
            rayObject.transform.localRotation = Quaternion.Euler(localEuler);
            rayObject.transform.localScale = localScale;

            Collider rayCollider = rayObject.GetComponent<Collider>();
            if (rayCollider != null)
            {
                Destroy(rayCollider);
            }

            Renderer rayRenderer = rayObject.GetComponent<Renderer>();
            if (rayRenderer != null)
            {
                rayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rayRenderer.receiveShadows = false;
                rayRenderer.material.color = new Color(1f, 0.82f, 0.36f, 1f);
            }
        }

        private Material GetRuntimeSunMaterial()
        {
            if (runtimeSunMaterial != null)
            {
                return runtimeSunMaterial;
            }

            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            }

            runtimeSunMaterial = new Material(shader)
            {
                name = "RuntimeSunMaterial",
                color = Color.white
            };

            Texture2D texture = LoadSunTextureFromProject();
            if (texture != null)
            {
                runtimeSunMaterial.mainTexture = texture;
            }
            else
            {
                runtimeSunMaterial.color = new Color(1f, 0.87f, 0.44f);
            }

            return runtimeSunMaterial;
        }

        private Texture2D LoadSunTextureFromProject()
        {
            string sunTexturePath = Path.Combine(
                Application.dataPath,
                "Sun_v2_L3.123cbc92ee65-5f03-4298-b1e6-b236b6b8b4aa",
                "13913_Sun_diff.jpg");

            if (!File.Exists(sunTexturePath))
            {
                return null;
            }

            byte[] imageBytes = File.ReadAllBytes(sunTexturePath);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "RuntimeSunTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            return texture.LoadImage(imageBytes) ? texture : null;
        }
    }
}
