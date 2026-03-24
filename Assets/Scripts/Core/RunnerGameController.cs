using Featurehole.Runner.Data;
using Featurehole.Runner.Gameplay;
using Featurehole.Runner.Hole;
using Featurehole.Runner.Level;
using UnityEngine;

namespace Featurehole.Runner.Core
{
    public sealed class RunnerGameController : MonoBehaviour
    {
        [SerializeField] private RunnerGameConfig config;
        [SerializeField] private HoleMover holeMover;
        [SerializeField] private TrackSegmentSpawner trackSpawner;
        [SerializeField] private PepperBoostSpawner pepperSpawner;
        [SerializeField] private RunnerHud hud;
        [SerializeField] private bool autoStart = true;

        private readonly RunnerRuntime runtime = new RunnerRuntime();

        public RunnerRuntime Runtime => runtime;

        public void Configure(
            RunnerGameConfig runnerConfig,
            HoleMover runnerHoleMover,
            TrackSegmentSpawner runnerTrackSpawner,
            PepperBoostSpawner runnerPepperSpawner,
            RunnerHud runnerHud,
            bool shouldAutoStart = true)
        {
            config = runnerConfig;
            holeMover = runnerHoleMover;
            trackSpawner = runnerTrackSpawner;
            pepperSpawner = runnerPepperSpawner;
            hud = runnerHud;
            autoStart = shouldAutoStart;
        }

        private void Start()
        {
            if (!ValidateScene())
            {
                enabled = false;
                return;
            }

            holeMover.Initialize(config);
            trackSpawner.Initialize(config);
            pepperSpawner.Initialize(config, holeMover);
            hud.Initialize(runtime, holeMover);

            if (autoStart)
            {
                StartRun();
            }
        }

        private void Update()
        {
            if (!runtime.IsRunning)
            {
                if (runtime.IsGameOver && Input.GetKeyDown(KeyCode.R))
                {
                    ResetRun();
                    StartRun();
                }

                return;
            }

            float deltaTime = Time.deltaTime;
            float currentForwardSpeed = config.ForwardSpeed * runtime.SpeedMultiplier;

            holeMover.Tick(deltaTime);
            trackSpawner.Tick(deltaTime, currentForwardSpeed);
            pepperSpawner.Tick(deltaTime, runtime, currentForwardSpeed);
            runtime.Tick(deltaTime, config.ForwardSpeed);
            holeMover.SetBoostActive(runtime.IsBoostActive);

            if (runtime.IsGameOver)
            {
                StopRun();
            }
        }

        public void StartRun()
        {
            trackSpawner.ResetTrack();
            holeMover.ResetPosition();
            holeMover.ResetSize();
            holeMover.SetBoostActive(false);
            pepperSpawner.ResetField();
            runtime.StartRun(config.MaxMissedCollectibles);
        }

        public void StopRun()
        {
            runtime.StopRun();
        }

        public void ResetRun()
        {
            runtime.StopRun();
            trackSpawner.ResetTrack();
            holeMover.ResetPosition();
            holeMover.ResetSize();
            holeMover.SetBoostActive(false);
            pepperSpawner.ResetField();
        }

        private bool ValidateScene()
        {
            if (config != null
                && holeMover != null
                && trackSpawner != null
                && pepperSpawner != null
                && hud != null)
            {
                return true;
            }

            Debug.LogWarning("RunnerGameController is missing scene references and was disabled.", this);
            return false;
        }
    }
}
