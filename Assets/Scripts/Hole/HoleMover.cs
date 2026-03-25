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
        private GameObject boostFlameObject;
        private Renderer[] boostFlameVisualRenderers;
        private ParticleSystem[] boostFlames;
        private ParticleSystemRenderer[] boostFlameRenderers;
        private float currentDiameter;
        private float visualHeightScale = 1f;

        public float CurrentDiameter => currentDiameter;

        public void Initialize(RunnerGameConfig runnerConfig)
        {
            config = runnerConfig;
            startPosition = transform.position;
            visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                visualHeightScale = visualTransform.localScale.y;
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
            position.y = startPosition.y;

            transform.position = position;
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

            visualTransform.localPosition = Vector3.zero;
            visualTransform.localScale = new Vector3(currentDiameter, visualHeightScale, currentDiameter);
        }
    }
}
