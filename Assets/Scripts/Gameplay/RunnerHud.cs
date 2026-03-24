using Featurehole.Runner.Core;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class RunnerHud : MonoBehaviour
    {
        private RunnerRuntime runtime;

        public void Initialize(RunnerRuntime runnerRuntime)
        {
            runtime = runnerRuntime;
        }

        private void OnGUI()
        {
            if (runtime == null)
            {
                return;
            }

            const int boxWidth = 240;
            const int boxHeight = 110;

            GUI.Box(new Rect(16f, 16f, boxWidth, boxHeight), "Runner");
            GUI.Label(new Rect(28f, 44f, boxWidth - 24, 22f), $"Distance: {runtime.DistanceTravelled:0}");
            GUI.Label(new Rect(28f, 66f, boxWidth - 24, 22f), $"Collected: {runtime.CollectedCount}");
            GUI.Label(new Rect(28f, 88f, boxWidth - 24, 22f), $"Missed: {runtime.MissedCount}/{runtime.MaxMissedCount}");

            if (runtime.IsGameOver)
            {
                GUI.Box(new Rect(16f, 136f, 260f, 72f), "Game Over");
                GUI.Label(new Rect(28f, 164f, 230f, 22f), "Press R to restart");
            }
        }
    }
}
