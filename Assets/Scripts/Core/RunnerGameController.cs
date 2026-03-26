using Featurehole.Runner.Data;
using Featurehole.Runner.Gameplay;
using Featurehole.Runner.Hole;
using Featurehole.Runner.Level;
using Featurehole.Runner.Audio;
using UnityEngine;

namespace Featurehole.Runner.Core
{
    public sealed class RunnerGameController : MonoBehaviour
    {
        [SerializeField] private RunnerGameConfig config;
        [SerializeField] private HoleMover holeMover;
        [SerializeField] private TrackSegmentSpawner trackSpawner;
        [SerializeField] private AppleSpawner appleSpawner;
        [SerializeField] private PepperBoostSpawner pepperSpawner;
        [SerializeField] private MagnetSpawner magnetSpawner;
        [SerializeField] private CoinSpawner coinSpawner;
        [SerializeField] private RockObstacleSpawner rockSpawner;
        [SerializeField] private MusicManager musicManager;
        [SerializeField] private GameSfxManager sfxManager;
        [SerializeField] private RunnerHud hud;
        [SerializeField] private bool autoStart = true;

        private readonly RunnerRuntime runtime = new RunnerRuntime();
        private float restartDelayRemaining = -1f;

        public RunnerRuntime Runtime => runtime;

        public void Configure(
            RunnerGameConfig runnerConfig,
            HoleMover runnerHoleMover,
            TrackSegmentSpawner runnerTrackSpawner,
            AppleSpawner runnerAppleSpawner,
            PepperBoostSpawner runnerPepperSpawner,
            MagnetSpawner runnerMagnetSpawner,
            CoinSpawner runnerCoinSpawner,
            RockObstacleSpawner runnerRockSpawner,
            MusicManager runnerMusicManager,
            GameSfxManager runnerSfxManager,
            RunnerHud runnerHud,
            bool shouldAutoStart = true)
        {
            config = runnerConfig;
            holeMover = runnerHoleMover;
            trackSpawner = runnerTrackSpawner;
            appleSpawner = runnerAppleSpawner;
            pepperSpawner = runnerPepperSpawner;
            magnetSpawner = runnerMagnetSpawner;
            coinSpawner = runnerCoinSpawner;
            rockSpawner = runnerRockSpawner;
            musicManager = runnerMusicManager;
            sfxManager = runnerSfxManager;
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
            appleSpawner.Initialize(config, holeMover);
            pepperSpawner.Initialize(config, holeMover);
            coinSpawner.Initialize(config, holeMover);
            magnetSpawner.Initialize(config, holeMover);
            rockSpawner.Initialize(config, holeMover);
            hud.Initialize(runtime, holeMover);

            if (autoStart)
            {
                StartRun();
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            runtime.Tick(deltaTime, config.ForwardSpeed);
            holeMover.SetBoostActive(runtime.IsBoostActive);

            if (!runtime.IsRunning)
            {
                if (runtime.IsGameOver && restartDelayRemaining > 0f)
                {
                    restartDelayRemaining -= deltaTime;
                    if (restartDelayRemaining <= 0f)
                    {
                        ResetRun();
                        StartRun();
                    }
                }
                else if (runtime.IsGameOver && Input.GetKeyDown(KeyCode.R))
                {
                    ResetRun();
                    StartRun();
                }

                return;
            }

            float currentForwardSpeed = config.ForwardSpeed * runtime.SpeedMultiplier;

            holeMover.Tick(deltaTime);
            trackSpawner.Tick(deltaTime, currentForwardSpeed);
            appleSpawner.Tick(deltaTime, runtime, currentForwardSpeed);
            pepperSpawner.Tick(deltaTime, runtime, currentForwardSpeed);
            magnetSpawner.Tick(deltaTime, runtime, currentForwardSpeed);
            coinSpawner.Tick(deltaTime, runtime, currentForwardSpeed);
            if (rockSpawner.Tick(deltaTime, currentForwardSpeed))
            {
                if (musicManager != null)
                {
                    musicManager.Stop();
                }
                if (sfxManager != null)
                {
                    sfxManager.PlayRockImpact();
                    sfxManager.PlayLose();
                }
                TriggerGameOver();
                return;
            }

            if (runtime.IsGameOver)
            {
                TriggerGameOver();
            }
        }

        public void StartRun()
        {
            trackSpawner.ResetTrack();
            holeMover.ResetPosition();
            holeMover.ResetSize();
            holeMover.SetBoostActive(false);
            appleSpawner.ResetField();
            pepperSpawner.ResetField();
            coinSpawner.ResetField();
            magnetSpawner.ResetField();
            rockSpawner.ResetField();
            restartDelayRemaining = -1f;
            runtime.StartRun(config.MaxMissedCollectibles, true);
            if (musicManager != null)
            {
                musicManager.PlayFromStart();
            }
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
            appleSpawner.ResetField();
            pepperSpawner.ResetField();
            coinSpawner.ResetField();
            magnetSpawner.ResetField();
            rockSpawner.ResetField();
            restartDelayRemaining = -1f;
        }

        private void TriggerGameOver()
        {
            if (!runtime.IsGameOver)
            {
                runtime.TriggerGameOver("Проигрыш");
            }
            else if (string.IsNullOrEmpty(runtime.GameOverMessage))
            {
                runtime.TriggerGameOver("Проигрыш");
            }

            restartDelayRemaining = 1.2f;
            StopRun();
        }

        private bool ValidateScene()
        {
            if (config != null
                && holeMover != null
                && trackSpawner != null
                && appleSpawner != null
                && pepperSpawner != null
                && magnetSpawner != null
                && coinSpawner != null
                && rockSpawner != null
                && musicManager != null
                && sfxManager != null
                && hud != null)
            {
                return true;
            }

            Debug.LogWarning("RunnerGameController is missing scene references and was disabled.", this);
            return false;
        }
    }
}
