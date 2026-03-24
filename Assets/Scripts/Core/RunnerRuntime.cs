namespace Featurehole.Runner.Core
{
    public sealed class RunnerRuntime
    {
        public bool IsRunning { get; private set; }
        public float DistanceTravelled { get; private set; }
        public int CollectedCount { get; private set; }
        public int MissedCount { get; private set; }
        public int MaxMissedCount { get; private set; }

        public bool IsGameOver => MissedCount >= MaxMissedCount && MaxMissedCount > 0;

        public void StartRun(int maxMissedCount)
        {
            DistanceTravelled = 0f;
            CollectedCount = 0;
            MissedCount = 0;
            MaxMissedCount = maxMissedCount;
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

        public void Tick(float deltaTime, float speed)
        {
            if (!IsRunning)
            {
                return;
            }

            DistanceTravelled += speed * deltaTime;
        }
    }
}
