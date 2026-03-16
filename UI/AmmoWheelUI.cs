using System;
using AMS.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace AMS.UI
{
    public class AmmoWheelUI : UIState
    {
        private float animationProgress;
        private bool opening;
        private bool closing;
        private bool loggedThemeFallbackError;

        private const int SlotCount = AMSPlayer.MaxAmmoSlots;

        private const float WheelOuterRadius = 132f;
        private const float WheelInnerRadius = 40f;
        private const float WheelOuterStartRadius = 56f;
        private const float WheelInnerStartRadius = 20f;
        private const float SelectedOuterBoost = 24f;
        private const float HoverOuterBoost = 6f;
        private const float TopSlotAngle = -MathHelper.PiOver2;
        private const float IconDrawSize = 32f;
        private const float WheelSpriteRotationOffset = 0f;

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

            const float speed = 0.12f;

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
            if (player == null || !player.active)
                return;

            Main.LocalPlayer.mouseInterface = true;

            AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
            AmmoWheelClientConfig config = ModContent.GetInstance<AmmoWheelClientConfig>();

            Vector2 center = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f) + new Vector2(config.WheelOffsetX, config.WheelOffsetY);
            float eased = EaseOut(animationProgress);
            float alpha = animationProgress;
            float iconAlpha = alpha;
            float baseAlpha = alpha * MathHelper.Clamp(config.WheelUiOpacity, 0f, 1f);

            try
            {
                switch (config.ThemePreset)
                {
                    case AmmoWheelThemePreset.BlueCircle:
                        DrawBlueCircleTheme(spriteBatch, modPlayer, center, eased, iconAlpha, baseAlpha);
                        break;

                    case AmmoWheelThemePreset.Squares:
                    default:
                        DrawSquaresTheme(spriteBatch, modPlayer, center, eased, iconAlpha, baseAlpha);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (!loggedThemeFallbackError)
                {
                    loggedThemeFallbackError = true;
                    ModContent.GetInstance<AMS>().Logger.Warn($"AMS: Theme draw failed for '{config.ThemePreset}'. Falling back to Squares. {ex}");
                }

                DrawSquaresTheme(spriteBatch, modPlayer, center, eased, iconAlpha, baseAlpha);
            }
        }

        private void DrawSquaresTheme(SpriteBatch spriteBatch, AMSPlayer modPlayer, Vector2 center, float eased, float iconAlpha, float baseAlpha)
        {
            loggedThemeFallbackError = false;
            DrawWheelIcons(spriteBatch, modPlayer, center, eased, iconAlpha, baseAlpha);
        }

        private static void DrawBlueCircleTheme(SpriteBatch spriteBatch, AMSPlayer modPlayer, Vector2 center, float eased, float iconAlpha, float baseAlpha)
        {
            DrawWheelIcons(spriteBatch, modPlayer, center, eased, iconAlpha, baseAlpha);
        }

        private static void DrawWheelIcons(
            SpriteBatch spriteBatch,
            AMSPlayer modPlayer,
            Vector2 center,
            float eased,
            float iconAlpha,
            float baseAlpha)
        {
            int unlockedSlots = Math.Clamp(modPlayer.unlockedAmmoSlots, 0, SlotCount);
            if (unlockedSlots <= 0)
                return;

            int selectedSlot = modPlayer.GetSelectedAmmoSlotIndex();
            float outerRadius = GetOuterRadius(eased);
            float innerRadius = GetInnerRadius(eased);
            float segmentAngle = MathHelper.TwoPi / SlotCount;
            float wheelRotation = modPlayer.GetWheelRotation();

            DrawWheelBase(spriteBatch, center, wheelRotation, baseAlpha);

            GetHoveredSegment(center, eased, unlockedSlots, wheelRotation, out int hoveredVisual, out int hoveredSlot);

            for (int visualIndex = 0; visualIndex < SlotCount; visualIndex++)
            {
                bool unlocked = visualIndex < unlockedSlots;
                bool selectedVisual = unlocked && visualIndex == selectedSlot;
                bool hoveredVisualMatch = unlocked && visualIndex == hoveredVisual;

                float centerAngle = TopSlotAngle + wheelRotation + (segmentAngle * visualIndex);
                float segmentOuter = outerRadius
                    + (selectedVisual ? SelectedOuterBoost * eased : 0f)
                    + (hoveredVisualMatch ? HoverOuterBoost * eased : 0f);

                float iconRadius = MathHelper.Lerp(innerRadius, segmentOuter, selectedVisual ? 0.66f : 0.72f);
                Vector2 iconPos = center + centerAngle.ToRotationVector2() * iconRadius;

                if (unlocked)
                {
                    DrawSlotItem(spriteBatch, modPlayer.ammoSlots[visualIndex], iconPos, selectedVisual, hoveredVisualMatch, iconAlpha);
                }
                else
                {
                    DrawLockedMarker(spriteBatch, iconPos, iconAlpha);
                }
            }

            if (hoveredSlot >= 0)
                AmmoWheelInteraction.HandleSlotInteraction(modPlayer, hoveredSlot);
        }

        private static void DrawWheelBase(SpriteBatch spriteBatch, Vector2 center, float rotation, float alpha)
        {
            Texture2D texture = ModContent.Request<Texture2D>("AMS/Assets/revolver_UI").Value;
            Vector2 origin = texture.Size() * 0.5f;

            spriteBatch.Draw(
                texture,
                center,
                null,
                Color.White * alpha,
                rotation + WheelSpriteRotationOffset,
                origin,
                1f,
                SpriteEffects.None,
                0f
            );
        }

        private static void GetHoveredSegment(
            Vector2 center,
            float eased,
            int unlockedSlots,
            float rotation,
            out int hoveredVisual,
            out int hoveredSlot)
        {
            hoveredVisual = -1;
            hoveredSlot = -1;

            if (unlockedSlots <= 0)
                return;

            Vector2 offset = Main.MouseScreen - center;
            float distanceFromCenter = offset.Length();

            float innerRadius = GetInnerRadius(eased);
            float outerRadius = GetOuterRadius(eased) + (SelectedOuterBoost * eased) + (HoverOuterBoost * eased);

            if (distanceFromCenter < innerRadius || distanceFromCenter > outerRadius + 6f)
                return;

            float mouseAngle = (float)Math.Atan2(offset.Y, offset.X);
            float segmentAngle = MathHelper.TwoPi / SlotCount;
            float relativeAngle = WrapPositiveRadians(mouseAngle - (TopSlotAngle + rotation));

            int visualIndex = (int)Math.Floor((relativeAngle + (segmentAngle * 0.5f)) / segmentAngle);
            visualIndex = WrapIndex(visualIndex, SlotCount);

            if (visualIndex < 0 || visualIndex >= unlockedSlots)
                return;

            hoveredVisual = visualIndex;
            hoveredSlot = visualIndex;
        }

        private static int WrapIndex(int index, int slotCount)
        {
            if (slotCount <= 0)
                return 0;

            int wrapped = index % slotCount;
            if (wrapped < 0)
                wrapped += slotCount;

            return wrapped;
        }

        private static float WrapPositiveRadians(float radians)
        {
            float wrapped = radians % MathHelper.TwoPi;
            if (wrapped < 0f)
                wrapped += MathHelper.TwoPi;

            return wrapped;
        }

        private static float GetOuterRadius(float eased)
        {
            return MathHelper.Lerp(WheelOuterStartRadius, WheelOuterRadius, eased);
        }

        private static float GetInnerRadius(float eased)
        {
            return MathHelper.Lerp(WheelInnerStartRadius, WheelInnerRadius, eased);
        }

        private static void DrawSlotItem(SpriteBatch spriteBatch, Item item, Vector2 position, bool selected, bool hovered, float alpha)
        {
            if (item == null || item.IsAir)
            {
                DrawEmptyMarker(spriteBatch, position, selected, hovered, alpha);
                return;
            }

            Main.instance.LoadItem(item.type);
            Texture2D itemTexture = TextureAssets.Item[item.type].Value;
            Rectangle frame = Main.itemAnimations[item.type]?.GetFrame(itemTexture) ?? itemTexture.Frame();

            float maxDimension = Math.Max(frame.Width, frame.Height);
            float fitScale = IconDrawSize / maxDimension;
            float scaleBoost = selected ? 1.15f : hovered ? 1.07f : 1f;
            float drawScale = fitScale * scaleBoost * alpha;

            spriteBatch.Draw(
                itemTexture,
                position,
                frame,
                item.GetAlpha(Color.White) * alpha,
                0f,
                frame.Size() * 0.5f,
                drawScale,
                SpriteEffects.None,
                0f
            );

            if (item.stack > 1)
            {
                string count = item.stack.ToString();
                Color countColor = selected ? Color.Gold : Color.White;

                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.ItemStack.Value,
                    count,
                    position.X + 8f,
                    position.Y + 7f,
                    countColor * alpha,
                    Color.Black * alpha,
                    Vector2.Zero,
                    0.72f
                );
            }
        }

        private static void DrawEmptyMarker(SpriteBatch spriteBatch, Vector2 position, bool selected, bool hovered, float alpha)
        {
            string marker = "o";
            Color markerColor = (selected ? Color.Gold : hovered ? Color.LightGray : Color.Silver) * alpha;

            Utils.DrawBorderStringFourWay(
                spriteBatch,
                FontAssets.ItemStack.Value,
                marker,
                position.X - 4f,
                position.Y - 6f,
                markerColor,
                Color.Black * alpha,
                Vector2.Zero,
                0.9f
            );
        }

        private static void DrawLockedMarker(SpriteBatch spriteBatch, Vector2 position, float alpha)
        {
            Utils.DrawBorderStringFourWay(
                spriteBatch,
                FontAssets.ItemStack.Value,
                "X",
                position.X - 5f,
                position.Y - 8f,
                Color.DarkGray * alpha,
                Color.Black * alpha,
                Vector2.Zero,
                0.9f
            );
        }

        private static float EaseOut(float x)
        {
            return 1f - (float)Math.Pow(1f - x, 3);
        }
    }
}



