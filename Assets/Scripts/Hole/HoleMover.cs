using Featurehole.Runner.Data;
using UnityEngine;

namespace Featurehole.Runner.Hole
{
    public sealed class HoleMover : MonoBehaviour
    {
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private bool usePointerInput = true;

        private RunnerGameConfig config;
        private Vector3 startPosition;
        private Transform visualTransform;
        private Transform splitVisualTransform;
        private GameObject boostFlameObject;
        private Renderer[] boostFlameVisualRenderers;
        private ParticleSystem[] boostFlames;
        private ParticleSystemRenderer[] boostFlameRenderers;
        private float currentDiameter;
        private float visualHeightScale = 1f;
        private float splitTimeRemaining;

        public float CurrentDiameter => currentDiameter;
        public bool IsSplitActive { get; private set; }
        public float SplitTimeRemaining => splitTimeRemaining;

        public void Initialize(RunnerGameConfig runnerConfig)
        {
            config = runnerConfig;
            startPosition = transform.position;
            visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                visualHeightScale = visualTransform.localScale.y;
            }

            splitVisualTransform = transform.Find("VisualTwin");
            if (splitVisualTransform == null && visualTransform != null)
            {
                GameObject splitVisualObject = Instantiate(visualTransform.gameObject, transform);
                splitVisualObject.name = "VisualTwin";
                splitVisualTransform = splitVisualObject.transform;
            }

            Transform boostFlameTransform = transform.Find("BoostFlame");
            boostFlameObject = boostFlameTransform != null ? boostFlameTransform.gameObject : null;
            boostFlameVisualRenderers = boostFlameTransform != null
                ? boostFlameTransform.GetComponentsInChildren<Renderer>(true)
                : null;
            boostFlames = boostFlameTransform != null
                ? boostFlameTransform.GetComponentsInChildren<ParticleSystem>(true)
                : null;
            boostFlameRenderers = boostFlameTransform != null
                ? boostFlameTransform.GetComponentsInChildren<ParticleSystemRenderer>(true)
                : null;
            transform.position = startPosition;
            ResetSize();
            SetBoostActive(false);
        }

        public void ResetPosition()
        {
            transform.position = startPosition;
        }

        public void ResetSize()
        {
            if (config == null)
            {
                return;
            }

            currentDiameter = config.HoleDiameter;
            splitTimeRemaining = 0f;
            IsSplitActive = false;
            ApplyVisualScale();
        }

        public void Grow()
        {
            if (config == null)
            {
                return;
            }

            currentDiameter = Mathf.Min(currentDiameter + config.GrowthPerCollectible, config.MaxHoleDiameter);
            ApplyVisualScale();
        }

        public void ActivateSplit(float duration)
        {
            if (config == null || duration <= 0f)
            {
                return;
            }

            splitTimeRemaining = Mathf.Max(splitTimeRemaining, duration);
            IsSplitActive = true;
            ApplyVisualScale();
        }

        public bool CanAbsorb(Vector3 worldPosition, float itemSize, float radiusFactor = 0.35f)
        {
            float absorbRadius = currentDiameter * 0.5f + itemSize * radiusFactor;
            float forwardDistance = Mathf.Abs(worldPosition.z - transform.position.z);

            if (forwardDistance > itemSize)
            {
                return false;
            }

            if (!IsSplitActive && Mathf.Abs(worldPosition.x - transform.position.x) <= absorbRadius)
            {
                return true;
            }

            if (!IsSplitActive)
            {
                return false;
            }

            float splitOffset = GetSplitOffset();
            if (Mathf.Abs(worldPosition.x - (transform.position.x - splitOffset)) <= absorbRadius)
            {
                return true;
            }

            return Mathf.Abs(worldPosition.x - (transform.position.x + splitOffset)) <= absorbRadius;
        }

        public void SetBoostActive(bool isActive)
        {
            if (boostFlameObject == null)
            {
                return;
            }

            boostFlameObject.SetActive(isActive);

            if (boostFlameVisualRenderers != null)
            {
                foreach (Renderer renderer in boostFlameVisualRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = isActive;
                    }
                }
            }

            if (boostFlames == null || boostFlames.Length == 0)
            {
                return;
            }

            if (boostFlameRenderers != null)
            {
                foreach (ParticleSystemRenderer renderer in boostFlameRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = isActive;
                    }
                }
            }

            foreach (ParticleSystem boostFlame in boostFlames)
            {
                if (boostFlame == null)
                {
                    continue;
                }

                if (isActive)
                {
                    boostFlame.Clear(true);
                    boostFlame.Play(true);
                }
                else
                {
                    boostFlame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            if (config == null)
            {
                return;
            }

            Vector3 position = transform.position;
            float targetX;

            if (TryGetPointerTargetX(out float pointerTargetX))
            {
                targetX = pointerTargetX;
                position.x = Mathf.MoveTowards(position.x, targetX, config.LateralSpeed * deltaTime);
            }
            else
            {
                float horizontalInput = Input.GetAxisRaw(horizontalAxis);
                position.x += horizontalInput * config.LateralSpeed * deltaTime;
            }

            position.x = Mathf.Clamp(position.x, -config.LateralLimit, config.LateralLimit);
            if (IsSplitActive)
            {
                float splitOffset = GetSplitOffset();
                float splitLimit = Mathf.Max(0f, config.LateralLimit - splitOffset);
                position.x = Mathf.Clamp(position.x, -splitLimit, splitLimit);
            }

            position.y = startPosition.y;

            transform.position = position;

            if (IsSplitActive && splitTimeRemaining > 0f)
            {
                splitTimeRemaining = Mathf.Max(0f, splitTimeRemaining - deltaTime);
                if (splitTimeRemaining <= 0f)
                {
                    IsSplitActive = false;
                    ApplyVisualScale();
                }
            }
        }

        private bool TryGetPointerTargetX(out float targetX)
        {
            targetX = 0f;

            if (!usePointerInput)
            {
                return false;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                targetX = ScreenToLaneX(touch.position.x);
                return true;
            }

            if (Input.GetMouseButton(0))
            {
                targetX = ScreenToLaneX(Input.mousePosition.x);
                return true;
            }

            return false;
        }

        private float ScreenToLaneX(float screenX)
        {
            if (Screen.width <= 0)
            {
                return 0f;
            }

            float normalizedX = Mathf.Clamp01(screenX / Screen.width);
            return Mathf.Lerp(-config.LateralLimit, config.LateralLimit, normalizedX);
        }

        private void ApplyVisualScale()
        {
            if (visualTransform == null)
            {
                return;
            }

            float splitOffset = IsSplitActive ? GetSplitOffset() : 0f;

            visualTransform.localPosition = new Vector3(-splitOffset, 0f, 0f);
            visualTransform.localScale = new Vector3(currentDiameter, visualHeightScale, currentDiameter);

            if (splitVisualTransform == null)
            {
                return;
            }

            splitVisualTransform.localScale = new Vector3(currentDiameter, visualHeightScale, currentDiameter);
            splitVisualTransform.localPosition = new Vector3(splitOffset, 0f, 0f);
            splitVisualTransform.gameObject.SetActive(IsSplitActive);
        }

        private float GetSplitOffset()
        {
            return currentDiameter * config.SplitHoleSpacingMultiplier;
        }
    }
}
