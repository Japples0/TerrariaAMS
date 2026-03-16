using System;
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
        private const int SlotContext = ItemSlot.Context.InventoryAmmo;

        private const float WheelOuterRadius = 132f;
        private const float WheelInnerRadius = 40f;
        private const float WheelOuterStartRadius = 56f;
        private const float WheelInnerStartRadius = 20f;
        private const float SelectedOuterBoost = 24f;
        private const float HoverOuterBoost = 6f;
        private const float SegmentGapRadians = 0.04f;
        private const float TopSlotAngle = -MathHelper.PiOver2;
        private const float IconDrawSize = 32f;

        private static readonly Color WheelBaseColor = new Color(95, 149, 230);
        private static readonly Color WheelSelectedColor = new Color(50, 92, 176);
        private static readonly Color WheelHoverColor = new Color(124, 177, 250);
        private static readonly Color WheelLockedColor = new Color(66, 74, 94);
        private static readonly Color WheelDividerColor = new Color(24, 27, 36);
        private static readonly Color WheelCenterColor = new Color(50, 92, 176);

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

            try
            {
                switch (config.ThemePreset)
                {
                    case AmmoWheelThemePreset.BlueCircle:
                        DrawBlueCircleTheme(spriteBatch, center, eased, alpha);
                        break;

                    case AmmoWheelThemePreset.Squares:
                    default:
                        DrawSquaresTheme(spriteBatch, modPlayer, center, eased, alpha);
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

                DrawSquaresTheme(spriteBatch, modPlayer, center, eased, alpha);
            }
        }

        private void DrawSquaresTheme(SpriteBatch spriteBatch, AMSPlayer modPlayer, Vector2 center, float eased, float alpha)
        {
            loggedThemeFallbackError = false;

            int unlockedSlots = Math.Clamp(modPlayer.unlockedAmmoSlots, 0, SlotCount);
            if (unlockedSlots <= 0)
                return;

            int selectedSlot = modPlayer.GetSelectedAmmoSlotIndex();
            float outerRadius = GetOuterRadius(eased);
            float innerRadius = GetInnerRadius(eased);
            GetHoveredSegment(center, eased, unlockedSlots, selectedSlot, out int hoveredVisual, out int hoveredSlot);

            DrawWheelBackdrop(spriteBatch, center, outerRadius, alpha);
            DrawDynamicWheelTheme(
                spriteBatch,
                modPlayer,
                center,
                outerRadius,
                innerRadius,
                eased,
                alpha,
                unlockedSlots,
                selectedSlot,
                hoveredVisual
            );

            if (hoveredSlot >= 0)
                HandleSlotInteraction(modPlayer, hoveredSlot);
        }

        private static void DrawBlueCircleTheme(SpriteBatch spriteBatch, Vector2 center, float eased, float alpha)
        {
            float circleRadius = Math.Max(24f, GetOuterRadius(eased) * 0.72f);
            Color fillColor = new Color(42, 84, 210) * (0.8f * alpha);

            DrawFilledCircle(spriteBatch, center, circleRadius, fillColor);
        }

        private static void DrawDynamicWheelTheme(
            SpriteBatch spriteBatch,
            AMSPlayer modPlayer,
            Vector2 center,
            float outerRadius,
            float innerRadius,
            float eased,
            float alpha,
            int unlockedSlots,
            int selectedSlot,
            int hoveredVisual)
        {
            float segmentAngle = MathHelper.TwoPi / SlotCount;

            for (int visualIndex = 0; visualIndex < SlotCount; visualIndex++)
            {
                bool unlocked = visualIndex < unlockedSlots;
                bool selectedVisual = unlocked && visualIndex == 0;
                bool hoveredVisualMatch = unlocked && visualIndex == hoveredVisual;

                float centerAngle = TopSlotAngle + (segmentAngle * visualIndex);
                float startAngle = centerAngle - (segmentAngle * 0.5f) + (SegmentGapRadians * 0.5f);
                float endAngle = centerAngle + (segmentAngle * 0.5f) - (SegmentGapRadians * 0.5f);
                float segmentOuter = outerRadius
                    + (selectedVisual ? SelectedOuterBoost * eased : 0f)
                    + (hoveredVisualMatch ? HoverOuterBoost * eased : 0f);

                Color fillColor = unlocked
                    ? selectedVisual
                        ? WheelSelectedColor
                        : hoveredVisualMatch
                            ? WheelHoverColor
                            : WheelBaseColor
                    : WheelLockedColor;

                DrawRingSegment(spriteBatch, center, innerRadius, segmentOuter, startAngle, endAngle, fillColor * alpha);

                float iconRadius = MathHelper.Lerp(innerRadius, segmentOuter, selectedVisual ? 0.66f : 0.62f);
                Vector2 iconPos = center + centerAngle.ToRotationVector2() * iconRadius;

                if (unlocked)
                {
                    int slotIndex = MapVisualToSlotIndex(visualIndex, selectedSlot, unlockedSlots);
                    DrawSlotItem(spriteBatch, modPlayer.ammoSlots[slotIndex], iconPos, selectedVisual, alpha);
                }
                else
                {
                    DrawLockedMarker(spriteBatch, iconPos, alpha);
                }
            }

            DrawSegmentDividers(spriteBatch, center, outerRadius, innerRadius, eased, alpha, segmentAngle);
            DrawCenterCap(spriteBatch, center, innerRadius, alpha);
        }

        private static void DrawWheelBackdrop(SpriteBatch spriteBatch, Vector2 center, float outerRadius, float alpha)
        {
            DrawFilledCircle(spriteBatch, center, outerRadius + 34f, new Color(24, 32, 58) * (0.35f * alpha));
        }

        private static void DrawSegmentDividers(
            SpriteBatch spriteBatch,
            Vector2 center,
            float outerRadius,
            float innerRadius,
            float eased,
            float alpha,
            float segmentAngle)
        {
            float startBoundary = TopSlotAngle - (segmentAngle * 0.5f);
            for (int divider = 0; divider < SlotCount; divider++)
            {
                float angle = startBoundary + (segmentAngle * divider);
                bool selectedBoundary = divider == 0 || divider == 1;
                float dividerOuterRadius = outerRadius + (selectedBoundary ? SelectedOuterBoost * eased : 0f);

                Vector2 start = center + angle.ToRotationVector2() * innerRadius;
                Vector2 end = center + angle.ToRotationVector2() * dividerOuterRadius;
                DrawLine(spriteBatch, start, end, WheelDividerColor * alpha, 4f);
            }

            DrawCircleOutline(spriteBatch, center, outerRadius, WheelDividerColor * (0.9f * alpha), 3f);
        }

        private static void DrawCenterCap(SpriteBatch spriteBatch, Vector2 center, float innerRadius, float alpha)
        {
            DrawFilledCircle(spriteBatch, center, Math.Max(4f, innerRadius - 4f), WheelCenterColor * alpha);
            DrawCircleOutline(spriteBatch, center, innerRadius, WheelDividerColor * alpha, 4f);
        }

        private static void DrawRingSegment(
            SpriteBatch spriteBatch,
            Vector2 center,
            float innerRadius,
            float outerRadius,
            float startAngle,
            float endAngle,
            Color color)
        {
            if (outerRadius <= innerRadius || endAngle <= startAngle)
                return;

            float angleSpan = endAngle - startAngle;
            int lineCount = Math.Max(12, (int)(outerRadius * angleSpan / 4f));
            float stripThickness = Math.Max(2f, (outerRadius * angleSpan / lineCount) + 1f);

            for (int i = 0; i <= lineCount; i++)
            {
                float t = i / (float)lineCount;
                float angle = MathHelper.Lerp(startAngle, endAngle, t);
                Vector2 dir = angle.ToRotationVector2();

                Vector2 start = center + dir * innerRadius;
                Vector2 end = center + dir * outerRadius;
                DrawLine(spriteBatch, start, end, color, stripThickness);
            }
        }

        private static void DrawFilledCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
        {
            if (radius <= 0f)
                return;

            float radiusSquared = radius * radius;
            int yMin = (int)Math.Floor(-radius);
            int yMax = (int)Math.Ceiling(radius);

            for (int y = yMin; y <= yMax; y++)
            {
                float x = (float)Math.Sqrt(Math.Max(0f, radiusSquared - (y * y)));
                Vector2 start = center + new Vector2(-x, y);
                Vector2 end = center + new Vector2(x, y);
                DrawLine(spriteBatch, start, end, color, 1f);
            }
        }

        private static void DrawCircleOutline(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, float thickness)
        {
            if (radius <= 0f || thickness <= 0f)
                return;

            int segments = Math.Max(36, (int)radius);
            Vector2 previous = center + new Vector2(radius, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                Vector2 current = center + angle.ToRotationVector2() * radius;
                DrawLine(spriteBatch, previous, current, color, thickness);
                previous = current;
            }
        }

        private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length <= 0f)
                return;

            float rotation = (float)Math.Atan2(edge.Y, edge.X);
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            spriteBatch.Draw(
                pixel,
                start,
                null,
                color,
                rotation,
                new Vector2(0f, 0.5f),
                new Vector2(length, thickness),
                SpriteEffects.None,
                0f
            );
        }

        private static int MapVisualToSlotIndex(int visualIndex, int selectedSlot, int unlockedSlots)
        {
            if (unlockedSlots <= 0)
                return 0;

            return WrapIndex(selectedSlot + visualIndex, unlockedSlots);
        }

        private static void GetHoveredSegment(
            Vector2 center,
            float eased,
            int unlockedSlots,
            int selectedSlot,
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
            float relativeAngle = WrapPositiveRadians(mouseAngle - TopSlotAngle);

            int visualIndex = (int)Math.Floor((relativeAngle + (segmentAngle * 0.5f)) / segmentAngle);
            visualIndex = WrapIndex(visualIndex, SlotCount);

            if (visualIndex < 0 || visualIndex >= unlockedSlots)
                return;

            hoveredVisual = visualIndex;
            hoveredSlot = MapVisualToSlotIndex(visualIndex, selectedSlot, unlockedSlots);
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

        private static void DrawSlotItem(SpriteBatch spriteBatch, Item item, Vector2 position, bool selected, float alpha)
        {
            if (item == null || item.IsAir)
            {
                DrawEmptyMarker(spriteBatch, position, selected, alpha);
                return;
            }

            Main.instance.LoadItem(item.type);
            Texture2D itemTexture = TextureAssets.Item[item.type].Value;
            Rectangle frame = Main.itemAnimations[item.type]?.GetFrame(itemTexture) ?? itemTexture.Frame();

            float maxDimension = Math.Max(frame.Width, frame.Height);
            float fitScale = IconDrawSize / maxDimension;
            float drawScale = fitScale * (selected ? 1.15f : 1f) * alpha;

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

        private static void DrawEmptyMarker(SpriteBatch spriteBatch, Vector2 position, bool selected, float alpha)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Color markerColor = (selected ? Color.Gold : Color.Silver) * alpha;

            spriteBatch.Draw(pixel, position + new Vector2(-6f, 0f), null, markerColor, 0f, Vector2.Zero, new Vector2(12f, 2f), SpriteEffects.None, 0f);
            spriteBatch.Draw(pixel, position + new Vector2(0f, -6f), null, markerColor, 0f, Vector2.Zero, new Vector2(2f, 12f), SpriteEffects.None, 0f);
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

        private static bool IsAmmoOrAir(Item item)
        {
            return item == null || item.IsAir || AMSPlayer.IsAmmoItem(item);
        }

        private static void HandleSlotInteraction(AMSPlayer modPlayer, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= modPlayer.ammoSlots.Length)
                return;

            ItemSlot.MouseHover(ref modPlayer.ammoSlots[slotIndex], SlotContext);

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Item slotBefore = modPlayer.ammoSlots[slotIndex].Clone();
                Item mouseBefore = Main.mouseItem.Clone();

                ItemSlot.LeftClick(ref modPlayer.ammoSlots[slotIndex], SlotContext);
                Main.mouseLeftRelease = false;

                if (!IsAmmoOrAir(modPlayer.ammoSlots[slotIndex]))
                {
                    modPlayer.ammoSlots[slotIndex] = slotBefore;
                    Main.mouseItem = mouseBefore;
                }
            }

            if (Main.mouseRight && Main.mouseRightRelease)
            {
                Item slotBefore = modPlayer.ammoSlots[slotIndex].Clone();
                Item mouseBefore = Main.mouseItem.Clone();

                ItemSlot.RightClick(ref modPlayer.ammoSlots[slotIndex], SlotContext);
                Main.mouseRightRelease = false;

                if (!IsAmmoOrAir(modPlayer.ammoSlots[slotIndex]))
                {
                    modPlayer.ammoSlots[slotIndex] = slotBefore;
                    Main.mouseItem = mouseBefore;
                }
            }
        }

        private static float EaseOut(float x)
        {
            return 1f - (float)Math.Pow(1f - x, 3);
        }
    }
}
