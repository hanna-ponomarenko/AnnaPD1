using UnityEngine;

namespace Featurehole.Runner.Core
{
    public sealed class RunnerRuntime
    {
        private const float CountdownDuration = 4f;

        public bool IsRunning { get; private set; }
        public float DistanceTravelled { get; private set; }
        public int CollectedCount { get; private set; }
        public int CoinCount { get; private set; }
        public int AppleCount { get; private set; }
        public int PepperCount { get; private set; }
        public int MissedCount { get; private set; }
        public int MaxMissedCount { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public float BoostTimeRemaining { get; private set; }
        public float MagnetTimeRemaining { get; private set; }
        public float MagnetRadius { get; private set; }
        public float CountdownTimeRemaining { get; private set; }
        public string GameOverMessage { get; private set; }
        private bool forcedGameOver;

        public bool IsBoostActive => BoostTimeRemaining > 0f;
        public bool IsMagnetActive => MagnetTimeRemaining > 0f;
        public bool IsCountdownActive => CountdownTimeRemaining > 0f;
        public bool IsStartScreenVisible => !IsRunning && !IsGameOver && CountdownTimeRemaining > 0f;

        public string CountdownText
        {
            get
            {
                if (!IsCountdownActive)
                {
                    return string.Empty;
                }

                if (CountdownTimeRemaining > 3f)
                {
                    return "START";
                }

                return Mathf.Clamp(Mathf.CeilToInt(CountdownTimeRemaining), 1, 3).ToString();
            }
        }

        public bool IsGameOver => forcedGameOver || (MissedCount >= MaxMissedCount && MaxMissedCount > 0);

        public void StartRun(int maxMissedCount, bool withCountdown = true)
        {
            DistanceTravelled = 0f;
            CollectedCount = 0;
            CoinCount = 0;
            AppleCount = 0;
            PepperCount = 0;
            MissedCount = 0;
            MaxMissedCount = maxMissedCount;
            SpeedMultiplier = 1f;
            BoostTimeRemaining = 0f;
            MagnetTimeRemaining = 0f;
            MagnetRadius = 0f;
            CountdownTimeRemaining = withCountdown ? CountdownDuration : 0f;
            GameOverMessage = string.Empty;
            forcedGameOver = false;
            IsRunning = !withCountdown;
        }

        public void StopRun()
        {
            IsRunning = false;
        }

        public void RegisterCollected()
        {
            CollectedCount++;
        }

        public void RegisterCoinCollected()
        {
            CollectedCount++;
            CoinCount++;
        }

        public void RegisterAppleCollected()
        {
            CollectedCount++;
            AppleCount++;
        }

        public void RegisterPepperCollected()
        {
            CollectedCount++;
            PepperCount++;
        }

        public void RegisterMissed()
        {
            MissedCount++;
        }

        public void ActivateBoost(float duration, float speedMultiplier)
        {
            BoostTimeRemaining = duration;
            SpeedMultiplier = speedMultiplier;
        }

        public void ActivateMagnet(float duration, float radius)
        {
            MagnetTimeRemaining = duration;
            MagnetRadius = radius;
            Debug.Log($"[Magnet] magnet active duration={duration:F1} radius={radius:F1}");
        }

        public void TriggerGameOver(string message)
        {
            forcedGameOver = true;
            GameOverMessage = message;
            BoostTimeRemaining = 0f;
            MagnetTimeRemaining = 0f;
            MagnetRadius = 0f;
            SpeedMultiplier = 1f;
            IsRunning = false;
        }

        public void Tick(float deltaTime, float speed)
        {
            if (IsCountdownActive)
            {
                CountdownTimeRemaining = System.Math.Max(0f, CountdownTimeRemaining - deltaTime);
                if (CountdownTimeRemaining <= 0f)
                {
                    IsRunning = true;
                }
            }

            if (!IsRunning)
            {
                return;
            }

            if (BoostTimeRemaining > 0f)
            {
                BoostTimeRemaining = System.Math.Max(0f, BoostTimeRemaining - deltaTime);

                if (BoostTimeRemaining <= 0f)
                {
                    SpeedMultiplier = 1f;
                }
            }

            if (MagnetTimeRemaining > 0f)
            {
                MagnetTimeRemaining = System.Math.Max(0f, MagnetTimeRemaining - deltaTime);
                if (MagnetTimeRemaining <= 0f)
                {
                    MagnetRadius = 0f;
                    Debug.Log("[Magnet] magnet expired");
                }
            }

            DistanceTravelled += speed * SpeedMultiplier * deltaTime;
        }
    }
}
