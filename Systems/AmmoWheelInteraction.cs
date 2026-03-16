using Terraria;
using Terraria.UI;

namespace AMS.Systems
{
    public static class AmmoWheelInteraction
    {
        private const int SlotContext = ItemSlot.Context.InventoryAmmo;

        public static void HandleSlotInteraction(AMSPlayer modPlayer, int slotIndex)
        {
            if (modPlayer == null || modPlayer.ammoSlots == null)
                return;

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

        private static bool IsAmmoOrAir(Item item)
        {
            return item == null || item.IsAir || AMSPlayer.IsAmmoItem(item);
        }
    }
}
