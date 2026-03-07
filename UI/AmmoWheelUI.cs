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

        private const int SlotCount = AMSPlayer.MaxAmmoSlots;
        private const float MaxRadius = 120f;
        private const float SlotInteractionRadius = 30f;
        private const float HoverScale = 1.2f;
        private const int SlotContext = ItemSlot.Context.InventoryAmmo;

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
            AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
            AmmoWheelClientConfig config = ModContent.GetInstance<AmmoWheelClientConfig>();

            Vector2 center = new Vector2(
                Main.screenWidth * 0.5f + config.WheelOffsetX,
                Main.screenHeight * 0.5f + config.WheelOffsetY
            );

            Texture2D slotTex = TextureAssets.InventoryBack.Value;

            float radius = MaxRadius * EaseOut(animationProgress);
            float slice = MathHelper.TwoPi / SlotCount;

            int hoveredByMouse = -1;

            Main.LocalPlayer.mouseInterface = true;

            float baseInventoryScale = Main.inventoryScale;

            for (int i = 0; i < SlotCount; i++)
            {
                bool unlocked = i < modPlayer.unlockedAmmoSlots;

                float angle = slice * i - MathHelper.PiOver2;
                Vector2 radialDirection = angle.ToRotationVector2();
                Vector2 slotCenter = center + radialDirection * radius;

                if (unlocked && IsMouseOverSlot(slotCenter))
                    hoveredByMouse = i;

                bool selected = unlocked && i == hoveredByMouse;
                float scaleMultiplier = selected ? HoverScale : 1f;
                float slotScale = baseInventoryScale * scaleMultiplier;

                Vector2 normalSlotSize = slotTex.Size() * baseInventoryScale;
                Vector2 scaledSlotSize = slotTex.Size() * slotScale;

                // Keep inner boundary fixed and grow outward from wheel center.
                Vector2 shiftedCenter = slotCenter + radialDirection * ((scaledSlotSize.X - normalSlotSize.X) * 0.5f);
                Vector2 slotDrawPos = shiftedCenter - scaledSlotSize * 0.5f;

                Main.inventoryScale = slotScale;

                ItemSlot.Draw(
                    spriteBatch,
                    ref modPlayer.ammoSlots[i],
                    SlotContext,
                    slotDrawPos,
                    (selected ? Color.Gold : (unlocked ? Color.White : Color.Gray)) * animationProgress
                );

                if (!unlocked)
                {
                    Rectangle lockOverlayBounds = new Rectangle(
                        (int)slotDrawPos.X,
                        (int)slotDrawPos.Y,
                        (int)scaledSlotSize.X,
                        (int)scaledSlotSize.Y
                    );

                    spriteBatch.Draw(
                        TextureAssets.MagicPixel.Value,
                        lockOverlayBounds,
                        Color.Black * 0.55f * animationProgress
                    );
                }
            }

            Main.inventoryScale = baseInventoryScale;

            if (hoveredByMouse >= 0)
                HandleSlotInteraction(modPlayer, hoveredByMouse);
        }

        private static bool IsMouseOverSlot(Vector2 slotCenter)
        {
            return Vector2.DistanceSquared(Main.MouseScreen, slotCenter)
                <= SlotInteractionRadius * SlotInteractionRadius;
        }

        private static bool IsAmmoOrAir(Item item)
        {
            return item.IsAir || AMSPlayer.IsAmmoItem(item);
        }

        private static void HandleSlotInteraction(AMSPlayer modPlayer, int slotIndex)
        {
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
            return 1f - (float)System.Math.Pow(1f - x, 3);
        }
    }
}

