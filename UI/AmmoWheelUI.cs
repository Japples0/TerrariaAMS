using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace AMS.UI
{
    public class AmmoWheelUI : UIState
    {
        private float animationProgress = 0f;

        private bool opening = false;
        private bool closing = false;

        private const int SlotCount = 6;

        private const float MaxRadius = 120f;

        public bool Visible => animationProgress > 0f || opening;

        public void Open()
        {
            closing = false;
            opening = true;
        }

        public void Close()
        {
            opening = false;
            closing = true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float speed = 0.12f;

            if (opening)
            {
                animationProgress += speed;

                if (animationProgress >= 1f)
                {
                    animationProgress = 1f;
                    opening = false;
                }
            }

            if (closing)
            {
                animationProgress -= speed;

                if (animationProgress <= 0f)
                {
                    animationProgress = 0f;
                    closing = false;
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;

            Player player = Main.LocalPlayer;

            Vector2 center = player.MountedCenter - Main.screenPosition;

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            float radius = MaxRadius * EaseOut(animationProgress);

            float rotation = 0f;

            Color slotColor = Color.White * animationProgress;

            float backgroundSize = radius * 2f + 80f;

            Rectangle bgRect = new (
                (int)center.X - (int)(backgroundSize / 2),
                (int)center.Y - (int)(backgroundSize / 2),
                (int)backgroundSize,
                (int)backgroundSize
            );

            Color bgColor = new Color(63, 82, 151) * 0.6f * animationProgress;

            spriteBatch.Draw(pixel, bgRect, bgColor);

            for (int i = 0; i < SlotCount; i++)
            {
                float angle = MathHelper.TwoPi / SlotCount * i + rotation;

                Vector2 pos = center + angle.ToRotationVector2() * radius;

                Rectangle rect = new(
                    (int)pos.X - 20,
                    (int)pos.Y - 20,
                    40,
                    40
                );

                spriteBatch.Draw(pixel, rect, new Color(100, 120, 200) * animationProgress);
            }
        }

        private float EaseOut(float x)
        {
            return 1f - (float)System.Math.Pow(1f - x, 3);
        }
    }
}