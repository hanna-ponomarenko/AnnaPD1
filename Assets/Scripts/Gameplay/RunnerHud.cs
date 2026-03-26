using Featurehole.Runner.Core;
using Featurehole.Runner.Hole;
using UnityEngine;

namespace Featurehole.Runner.Gameplay
{
    public sealed class RunnerHud : MonoBehaviour
    {
        private RunnerRuntime runtime;
        private HoleMover holeMover;
        private Sprite appleSprite;
        private Sprite pepperSprite;
        private Texture2D coinIconTexture;
        private GUIStyle counterStyle;
        private GUIStyle centerMessageStyle;
        private GUIStyle countdownStyle;

        public void Initialize(RunnerRuntime runnerRuntime, HoleMover runnerHoleMover)
        {
            runtime = runnerRuntime;
            holeMover = runnerHoleMover;
            EnsureCoinIcon();
        }

        public void SetIcons(Sprite apple, Sprite pepper)
        {
            appleSprite = apple;
            pepperSprite = pepper;
        }

        private void OnGUI()
        {
            if (runtime == null)
            {
                return;
            }

            EnsureStyles();
            DrawCoinCounter();
            DrawGoalCounters();
            DrawCenterMessages();
        }

        private void DrawCoinCounter()
        {
            const float iconSize = 95f;
            const float x = 28f;
            float y = Screen.height - 120f;

            if (coinIconTexture != null)
            {
                GUI.DrawTexture(new Rect(x, y, iconSize, iconSize), coinIconTexture, ScaleMode.StretchToFill, true);
            }

            GUI.Label(new Rect(x + iconSize + 22f, y + 4f, 180f, 88f), runtime.CoinCount.ToString(), counterStyle);
        }

        private void DrawGoalCounters()
        {
            const float iconSize = 95f;
            float baselineY = Screen.height - 120f;
            float pepperBlockX = Screen.width - 215f;
            float appleBlockX = Screen.width - 410f;

            DrawSpriteIcon(appleSprite, new Rect(appleBlockX, baselineY, iconSize, iconSize));
            GUI.Label(new Rect(appleBlockX + iconSize + 22f, baselineY + 4f, 120f, 88f), runtime.AppleCount.ToString(), counterStyle);

            DrawSpriteIcon(pepperSprite, new Rect(pepperBlockX, baselineY, iconSize, iconSize));
            GUI.Label(new Rect(pepperBlockX + iconSize + 22f, baselineY + 4f, 146f, 88f), runtime.PepperCount.ToString(), counterStyle);
        }

        private void DrawCenterMessages()
        {
            if (runtime.IsGameOver)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 80f), "GAME OVER", centerMessageStyle);
                return;
            }

            if (runtime.IsStartScreenVisible)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 100f), runtime.CountdownText, countdownStyle);
            }
        }

        private void DrawSpriteIcon(Sprite sprite, Rect rect)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Rect texRect = sprite.textureRect;
            Rect uv = new Rect(
                texRect.x / sprite.texture.width,
                texRect.y / sprite.texture.height,
                texRect.width / sprite.texture.width,
                texRect.height / sprite.texture.height);

            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
        }

        private void EnsureStyles()
        {
            if (counterStyle == null)
            {
                counterStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 75,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.black }
                };
            }

            if (centerMessageStyle == null)
            {
                centerMessageStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 64,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.black }
                };
            }

            if (countdownStyle == null)
            {
                countdownStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 72,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.black }
                };
            }
        }

        private void EnsureCoinIcon()
        {
            if (coinIconTexture != null)
            {
                return;
            }

            const int size = 64;
            coinIconTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "HudCoinIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    Color color = Color.clear;

                    if (distance <= 1f)
                    {
                        if (distance > 0.8f)
                        {
                            color = new Color(1f, 0.86f, 0.22f, 1f);
                        }
                        else
                        {
                            color = Color.Lerp(
                                new Color(1f, 0.8f, 0.16f, 1f),
                                new Color(0.95f, 0.68f, 0.08f, 1f),
                                distance * 0.8f);
                        }
                    }

                    coinIconTexture.SetPixel(x, y, color);
                }
            }

            coinIconTexture.Apply();
        }
    }
}
