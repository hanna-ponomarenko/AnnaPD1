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
        private Material runtimeHoleDecalMaterial;
        private Material runtimeBoostFireCoreMaterial;

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
                GameObject visualObject = new GameObject("Visual");
                visualObject.transform.SetParent(holeRoot, false);
                visual = visualObject.transform;
            }

            ConfigureHoleDecal(visual, runtimeConfig);

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
            visualRoot.localPosition = new Vector3(0f, 0.01f, 0f);
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
                decalRenderer.material = GetRuntimeHoleDecalMaterial();
                decalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                decalRenderer.receiveShadows = false;
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

        private GameObject CreateBoostFire(Transform parent)
        {
            return CreateRuntimeBoostFire(parent);
        }

        private GameObject CreateRuntimeBoostFire(Transform parent)
        {
            GameObject flameObject = new GameObject("BoostFlame");
            flameObject.transform.SetParent(parent, false);
            CreateTrailQuad(
                flameObject.transform,
                "TailGlow",
                new Vector3(0f, 0f, 0f),
                new Vector3(1.45f, 1f, 2.9f),
                GetRuntimeBoostFireMaterial());
            CreateTrailQuad(
                flameObject.transform,
                "TailCore",
                new Vector3(0f, 0.01f, 0.24f),
                new Vector3(0.7f, 1f, 1.7f),
                GetRuntimeBoostFireCoreMaterial());
            return flameObject;
        }

        private void CreateTrailQuad(
            Transform parent,
            string layerName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject layerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            layerObject.name = layerName;
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.localPosition = localPosition;
            layerObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            layerObject.transform.localScale = localScale;

            Collider quadCollider = layerObject.GetComponent<Collider>();
            if (quadCollider != null)
            {
                Destroy(quadCollider);
            }

            Renderer renderer = layerObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
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

        private Material GetRuntimeBoostFireCoreMaterial()
        {
            if (runtimeBoostFireCoreMaterial != null)
            {
                return runtimeBoostFireCoreMaterial;
            }

            runtimeBoostFireCoreMaterial = new Material(GetRuntimeBoostFireMaterial())
            {
                name = "RuntimeBoostFireCoreMaterial",
                color = new Color(1f, 0.96f, 0.72f, 0.98f)
            };

            return runtimeBoostFireCoreMaterial;
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
            boostFlame.localPosition = new Vector3(0f, 0.05f, 0.8f);
            boostFlame.localRotation = Quaternion.identity;
            boostFlame.localScale = Vector3.one;

            foreach (Renderer renderer in boostFlame.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.enabled = false;
            }

            boostFlame.gameObject.SetActive(false);
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
