namespace Featurehole.Runner.Core
{
    public sealed class RunnerRuntime
    {
        public bool IsRunning { get; private set; }

        public float DistanceTravelled { get; private set; }

        public void StartRun()
        {
            DistanceTravelled = 0f;
            IsRunning = true;
        }

        public void StopRun()
        {
            IsRunning = false;
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
