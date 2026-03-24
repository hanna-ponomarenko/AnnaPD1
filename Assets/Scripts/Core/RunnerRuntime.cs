namespace Featurehole.Runner.Core
{
    public sealed class RunnerRuntime
    {
        public bool IsRunning { get; private set; }
        public float DistanceTravelled { get; private set; }
        public int CollectedCount { get; private set; }
        public int MissedCount { get; private set; }
        public int MaxMissedCount { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public float BoostTimeRemaining { get; private set; }

        public bool IsBoostActive => BoostTimeRemaining > 0f;

        public bool IsGameOver => MissedCount >= MaxMissedCount && MaxMissedCount > 0;

        public void StartRun(int maxMissedCount)
        {
            DistanceTravelled = 0f;
            CollectedCount = 0;
            MissedCount = 0;
            MaxMissedCount = maxMissedCount;
            SpeedMultiplier = 1f;
            BoostTimeRemaining = 0f;
            IsRunning = true;
        }

        public void StopRun()
        {
            IsRunning = false;
        }

        public void RegisterCollected()
        {
            CollectedCount++;
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

        public void Tick(float deltaTime, float speed)
        {
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

            DistanceTravelled += speed * SpeedMultiplier * deltaTime;
        }
    }
}
