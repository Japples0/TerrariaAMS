using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AMS.Systems
{
    public class AmmoWheelAmmoSystem : ModSystem
    {
        public override void Load()
        {
            On_Player.ChooseAmmo += HookChooseAmmo;
            On_Player.HasAmmo_Item += HookHasAmmoItem;
            On_Player.HasAmmo_Item_bool += HookHasAmmoItemBool;
            On_Player.CountItem += HookCountItem;
            On_Player.PickAmmo_Item_refInt32_refSingle_refInt32_refSingle_refInt32_bool += HookPickAmmoOut;
            On_Player.PickAmmo_Item_refInt32_refSingle_refBoolean_refInt32_refSingle_refInt32_bool += HookPickAmmoRef;

            On_Main.TryGetAmmo += HookMainTryGetAmmo;
        }

        public override void Unload()
        {
            On_Player.ChooseAmmo -= HookChooseAmmo;
            On_Player.HasAmmo_Item -= HookHasAmmoItem;
            On_Player.HasAmmo_Item_bool -= HookHasAmmoItemBool;
            On_Player.CountItem -= HookCountItem;
            On_Player.PickAmmo_Item_refInt32_refSingle_refInt32_refSingle_refInt32_bool -= HookPickAmmoOut;
            On_Player.PickAmmo_Item_refInt32_refSingle_refBoolean_refInt32_refSingle_refInt32_bool -= HookPickAmmoRef;

            On_Main.TryGetAmmo -= HookMainTryGetAmmo;
        }

        private static Item HookChooseAmmo(On_Player.orig_ChooseAmmo orig, Player player, Item weapon)
        {
            Item vanillaAmmo = orig(player, weapon);
            if (AMSPlayer.IsAmmoItem(vanillaAmmo))
                return vanillaAmmo;

            AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
            return modPlayer.TryGetAmmoFromWheel(weapon, out Item wheelAmmo)
                ? wheelAmmo
                : vanillaAmmo;
        }

        private static bool HookHasAmmoItem(On_Player.orig_HasAmmo_Item orig, Player player, Item weapon)
        {
            if (orig(player, weapon))
                return true;

            AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
            return modPlayer.TryGetAmmoFromWheel(weapon, out _);
        }

        private static bool HookHasAmmoItemBool(On_Player.orig_HasAmmo_Item_bool orig, Player player, Item weapon, bool canUse)
        {
            if (orig(player, weapon, canUse))
                return true;

            AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
            return modPlayer.TryGetAmmoFromWheel(weapon, out _);
        }

        private static int HookCountItem(On_Player.orig_CountItem orig, Player player, int type, int stopAt)
        {
            int count = orig(player, type, stopAt);

            if (count >= stopAt && stopAt > 0)
                return count;

            AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
            int maxSlotsToCheck = System.Math.Min(modPlayer.unlockedAmmoSlots, modPlayer.ammoSlots.Length);
            bool queryingHeldWeaponAmmoType = player.HeldItem != null && !player.HeldItem.IsAir && player.HeldItem.useAmmo == type;

            for (int i = 0; i < maxSlotsToCheck; i++)
            {
                Item slotItem = modPlayer.ammoSlots[i];
                if (!AMSPlayer.IsAmmoItem(slotItem))
                    continue;

                bool matches = slotItem.type == type
                    || (queryingHeldWeaponAmmoType && slotItem.ammo == type);

                if (!matches)
                    continue;

                count += slotItem.stack;

                if (stopAt > 0 && count >= stopAt)
                    return stopAt;
            }

            return count;
        }

        private static bool HookPickAmmoOut(
            On_Player.orig_PickAmmo_Item_refInt32_refSingle_refInt32_refSingle_refInt32_bool orig,
            Player player,
            Item weapon,
            out int projToShoot,
            out float speed,
            out int damage,
            out float knockBack,
            out int usedAmmoItemId,
            bool dontConsume)
        {
            bool foundAmmo = orig(
                player,
                weapon,
                out projToShoot,
                out speed,
                out damage,
                out knockBack,
                out usedAmmoItemId,
                dontConsume
            );

            if (foundAmmo || !dontConsume)
                return foundAmmo;

            return TryBuildWheelAmmoPreview(
                player,
                weapon,
                out projToShoot,
                out speed,
                out damage,
                out knockBack,
                out usedAmmoItemId
            );
        }

        private static void HookPickAmmoRef(
            On_Player.orig_PickAmmo_Item_refInt32_refSingle_refBoolean_refInt32_refSingle_refInt32_bool orig,
            Player player,
            Item weapon,
            ref int projToShoot,
            ref float speed,
            ref bool canShoot,
            ref int totalDamage,
            ref float knockBack,
            out int usedAmmoItemId,
            bool dontConsume)
        {
            orig(
                player,
                weapon,
                ref projToShoot,
                ref speed,
                ref canShoot,
                ref totalDamage,
                ref knockBack,
                out usedAmmoItemId,
                dontConsume
            );

            if (canShoot || !dontConsume)
                return;

            canShoot = TryBuildWheelAmmoPreview(
                player,
                weapon,
                out projToShoot,
                out speed,
                out totalDamage,
                out knockBack,
                out usedAmmoItemId
            );
        }

        private static bool HookMainTryGetAmmo(
            On_Main.orig_TryGetAmmo orig,
            Main self,
            Item sourceItem,
            out Item ammoItem,
            out Color ammoColor,
            out float ammoScale,
            out Vector2 ammoOffset)
        {
            bool foundAmmo = orig(self, sourceItem, out ammoItem, out ammoColor, out ammoScale, out ammoOffset);
            if (foundAmmo)
                return true;

            Player player = Main.LocalPlayer;
            if (player == null)
                return false;

            AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
            if (!modPlayer.TryGetAmmoFromWheel(sourceItem, out Item wheelAmmo))
                return false;

            ammoItem = wheelAmmo;
            ammoColor = Color.White;
            ammoScale = 1f;
            ammoOffset = Vector2.Zero;
            return true;
        }

        private static bool TryBuildWheelAmmoPreview(
            Player player,
            Item weapon,
            out int projToShoot,
            out float speed,
            out int damage,
            out float knockBack,
            out int usedAmmoItemId)
        {
            projToShoot = 0;
            speed = 0f;
            damage = 0;
            knockBack = 0f;
            usedAmmoItemId = 0;

            AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
            if (!modPlayer.TryGetAmmoFromWheel(weapon, out Item ammo))
                return false;

            projToShoot = weapon.shoot;
            speed = weapon.shootSpeed;
            knockBack = weapon.knockBack;

            StatModifier damageModifier = StatModifier.Default;
            ItemLoader.PickAmmo(weapon, ammo, player, ref projToShoot, ref speed, ref damageModifier, ref knockBack);

            damage = (int)damageModifier.ApplyTo(player.GetWeaponDamage(weapon));
            usedAmmoItemId = ammo.type;

            return true;
        }
    }
}
