using Featurehole.Runner.Data;
using UnityEngine;

namespace Featurehole.Runner.Hole
{
    public sealed class HoleMover : MonoBehaviour
    {
        private const float SplitAnimationDuration = 0.45f;
        private const float SplitJumpHeight = 0.42f;
        private const float SplitBounceHeight = 0.12f;
        private const float MergeAnimationDuration = 0.38f;
        private const float MergeSinkDepth = 0.08f;
        private const float MergeStretchBoost = 0.16f;

        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private bool usePointerInput = true;

        private RunnerGameConfig config;
        private Vector3 startPosition;
        private Transform visualTransform;
        private Transform splitVisualTransform;
        private Transform boostHornsTransform;
        private Transform splitBoostHornsTransform;
        private GameObject boostHornsObject;
        private GameObject splitBoostHornsObject;
        private GameObject boostFlameObject;
        private Renderer[] boostFlameVisualRenderers;
        private ParticleSystem[] boostFlames;
        private ParticleSystemRenderer[] boostFlameRenderers;
        private float currentDiameter;
        private float visualHeightScale = 1f;
        private float splitTimeRemaining;
        private float splitAnimationElapsed;
        private float mergeAnimationElapsed;
        private float boostVisualElapsed;
        private bool isBoostVisualActive;
        private bool isMergeAnimating;

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

            boostHornsTransform = transform.Find("Visual/BoostHorns");
            boostHornsObject = boostHornsTransform != null ? boostHornsTransform.gameObject : null;
            splitBoostHornsTransform = transform.Find("VisualTwin/BoostHorns");
            splitBoostHornsObject = splitBoostHornsTransform != null ? splitBoostHornsTransform.gameObject : null;

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
            splitAnimationElapsed = 0f;
            mergeAnimationElapsed = 0f;
            IsSplitActive = false;
            isMergeAnimating = false;
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

            if (!IsSplitActive)
            {
                splitAnimationElapsed = 0f;
            }

            splitTimeRemaining = Mathf.Max(splitTimeRemaining, duration);
            IsSplitActive = true;
            isMergeAnimating = false;
            mergeAnimationElapsed = 0f;
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

            float splitOffset = GetCurrentSplitOffset();
            if (Mathf.Abs(worldPosition.x - (transform.position.x - splitOffset)) <= absorbRadius)
            {
                return true;
            }

            return Mathf.Abs(worldPosition.x - (transform.position.x + splitOffset)) <= absorbRadius;
        }

        public void SetBoostActive(bool isActive)
        {
            if (boostHornsObject != null)
            {
                boostHornsObject.SetActive(false);
            }

            if (splitBoostHornsObject != null)
            {
                splitBoostHornsObject.SetActive(false);
            }

            isBoostVisualActive = isActive;
            if (isActive)
            {
                boostVisualElapsed = 0f;
            }

            UpdateBoostVisualScale();

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

            if (isBoostVisualActive)
            {
                boostVisualElapsed += deltaTime;
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
                splitAnimationElapsed = Mathf.Min(SplitAnimationDuration, splitAnimationElapsed + deltaTime);
                splitTimeRemaining = Mathf.Max(0f, splitTimeRemaining - deltaTime);
                if (splitTimeRemaining <= 0f)
                {
                    IsSplitActive = false;
                    isMergeAnimating = true;
                    mergeAnimationElapsed = 0f;
                }
            }

            if (isMergeAnimating)
            {
                mergeAnimationElapsed = Mathf.Min(MergeAnimationDuration, mergeAnimationElapsed + deltaTime);
                if (mergeAnimationElapsed >= MergeAnimationDuration)
                {
                    isMergeAnimating = false;
                    ApplyVisualScale();
                }
            }

            UpdateBoostVisualScale();
            UpdateVisualLayout();
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

            visualTransform.localScale = new Vector3(currentDiameter, visualHeightScale, currentDiameter);

            if (splitVisualTransform == null)
            {
                return;
            }

            splitVisualTransform.localScale = new Vector3(currentDiameter, visualHeightScale, currentDiameter);
            UpdateBoostVisualScale();
            UpdateVisualLayout();
        }

        private float GetSplitOffset()
        {
            return currentDiameter * config.SplitHoleSpacingMultiplier;
        }

        private float GetCurrentSplitOffset()
        {
            if (!IsSplitActive)
            {
                return 0f;
            }

            float progress = Mathf.Clamp01(splitAnimationElapsed / SplitAnimationDuration);
            return GetSplitOffset() * EaseOutBack(progress);
        }

        private float GetCurrentSplitHeight()
        {
            if (!IsSplitActive)
            {
                return 0f;
            }

            float progress = Mathf.Clamp01(splitAnimationElapsed / SplitAnimationDuration);
            float jumpArc = Mathf.Sin(progress * Mathf.PI) * SplitJumpHeight;

            if (progress <= 0.72f)
            {
                return jumpArc;
            }

            float bounceProgress = Mathf.InverseLerp(0.72f, 1f, progress);
            float bounceArc = Mathf.Sin(bounceProgress * Mathf.PI) * SplitBounceHeight * (1f - bounceProgress);
            return jumpArc + bounceArc;
        }

        private void UpdateVisualLayout()
        {
            if (visualTransform == null)
            {
                return;
            }

            if (!IsSplitActive && !isMergeAnimating)
            {
                visualTransform.localPosition = Vector3.zero;
                visualTransform.localScale = new Vector3(currentDiameter, visualHeightScale, currentDiameter);
                if (splitVisualTransform != null)
                {
                    splitVisualTransform.localPosition = Vector3.zero;
                    splitVisualTransform.localScale = new Vector3(currentDiameter, visualHeightScale, currentDiameter);
                    splitVisualTransform.gameObject.SetActive(false);
                }

                UpdateBoostVisualScale();
                return;
            }

            if (splitVisualTransform == null)
            {
                return;
            }

            splitVisualTransform.gameObject.SetActive(true);

            if (IsSplitActive)
            {
                float splitOffset = GetCurrentSplitOffset();
                float splitHeight = GetCurrentSplitHeight();

                visualTransform.localPosition = new Vector3(-splitOffset, splitHeight, 0f);
                splitVisualTransform.localPosition = new Vector3(splitOffset, splitHeight, 0f);
                visualTransform.localScale = new Vector3(currentDiameter, visualHeightScale, currentDiameter);
                splitVisualTransform.localScale = new Vector3(currentDiameter, visualHeightScale, currentDiameter);
                return;
            }

            float mergeProgress = Mathf.Clamp01(mergeAnimationElapsed / MergeAnimationDuration);
            float fluidProgress = EaseInOutSine(mergeProgress);
            float mergeOffset = Mathf.Lerp(GetSplitOffset(), 0f, fluidProgress);
            float sink = Mathf.Sin(fluidProgress * Mathf.PI) * MergeSinkDepth;
            float widthScale = 1f + Mathf.Sin(fluidProgress * Mathf.PI) * MergeStretchBoost;
            float depthScale = 1f - Mathf.Sin(fluidProgress * Mathf.PI) * (MergeStretchBoost * 0.55f);

            visualTransform.localPosition = new Vector3(-mergeOffset, -sink, 0f);
            splitVisualTransform.localPosition = new Vector3(mergeOffset, -sink, 0f);
            visualTransform.localScale = new Vector3(currentDiameter * widthScale, visualHeightScale, currentDiameter * depthScale);
            splitVisualTransform.localScale = new Vector3(currentDiameter * widthScale, visualHeightScale, currentDiameter * depthScale);
            UpdateBoostVisualScale();
        }

        private void UpdateBoostVisualScale()
        {
            float pulse = 1f;
            if (isBoostVisualActive)
            {
                pulse = 1.22f + Mathf.Sin(boostVisualElapsed * 14f) * 0.18f;
            }

            if (boostHornsTransform != null)
            {
                boostHornsTransform.localScale = Vector3.one * pulse;
            }

            if (splitBoostHornsTransform != null)
            {
                splitBoostHornsTransform.localScale = Vector3.one * pulse;
            }
        }

        private static float EaseOutBack(float value)
        {
            const float overshoot = 1.70158f;
            float t = value - 1f;
            return 1f + (overshoot + 1f) * t * t * t + overshoot * t * t;
        }

        private static float EaseInOutSine(float value)
        {
            return -(Mathf.Cos(Mathf.PI * value) - 1f) * 0.5f;
        }
    }
}
