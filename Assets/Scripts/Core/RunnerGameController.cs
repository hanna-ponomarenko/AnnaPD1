using Featurehole.Runner.Data;
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
        [SerializeField] private bool autoStart = true;

        private readonly RunnerRuntime runtime = new RunnerRuntime();

        public RunnerRuntime Runtime => runtime;

        public void Configure(
            RunnerGameConfig runnerConfig,
            HoleMover runnerHoleMover,
            TrackSegmentSpawner runnerTrackSpawner,
            bool shouldAutoStart = true)
        {
            config = runnerConfig;
            holeMover = runnerHoleMover;
            trackSpawner = runnerTrackSpawner;
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

            if (autoStart)
            {
                StartRun();
            }
        }

        private void Update()
        {
            if (!runtime.IsRunning)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            holeMover.Tick(deltaTime);
            trackSpawner.Tick(deltaTime);
            runtime.Tick(deltaTime, config.ForwardSpeed);
        }

        public void StartRun()
        {
            runtime.StartRun();
        }

        public void StopRun()
        {
            runtime.StopRun();
        }

        private bool ValidateScene()
        {
            if (config != null && holeMover != null && trackSpawner != null)
            {
                return true;
            }

            Debug.LogWarning("RunnerGameController is missing scene references and was disabled.", this);
            return false;
        }
    }
}
