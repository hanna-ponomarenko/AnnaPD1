using Featurehole.Runner.Core;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class RunnerHud : MonoBehaviour
    {
        private RunnerRuntime runtime;
        private HoleMover holeMover;

        public void Initialize(RunnerRuntime runnerRuntime, HoleMover runnerHoleMover)
        {
            runtime = runnerRuntime;
            holeMover = runnerHoleMover;
        }

        private void OnGUI()
        {
            if (runtime == null)
            {
                return;
            }

            const int boxWidth = 240;
            const int boxHeight = 176;

            GUI.Box(new Rect(16f, 16f, boxWidth, boxHeight), "Runner");
            GUI.Label(new Rect(28f, 44f, boxWidth - 24, 22f), $"Distance: {runtime.DistanceTravelled:0}");
            GUI.Label(new Rect(28f, 66f, boxWidth - 24, 22f), $"Collected: {runtime.CollectedCount}");
            GUI.Label(new Rect(28f, 88f, boxWidth - 24, 22f), $"Missed: {runtime.MissedCount}/{runtime.MaxMissedCount}");
            if (holeMover != null)
            {
                GUI.Label(new Rect(28f, 110f, boxWidth - 24, 22f), $"Hole Size: {holeMover.CurrentDiameter:0.0}");
            }
            if (runtime.IsBoostActive)
            {
                GUI.Label(new Rect(28f, 132f, boxWidth - 24, 22f), $"Boost: {runtime.BoostTimeRemaining:0.0}s");
            }
            if (holeMover != null && holeMover.IsSplitActive)
            {
                GUI.Label(new Rect(28f, 154f, boxWidth - 24, 22f), $"Split: {holeMover.SplitTimeRemaining:0.0}s");
            }

            if (runtime.IsGameOver)
            {
                GUI.Box(new Rect(16f, 198f, 260f, 72f), "Game Over");
                GUI.Label(new Rect(28f, 226f, 230f, 22f), "Press R to restart");
            }
        }
    }
}
