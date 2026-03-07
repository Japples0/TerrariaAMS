using System;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

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

            IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += PatchItemSlotDrawAmmoCount;
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

            IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color -= PatchItemSlotDrawAmmoCount;
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
            int maxSlotsToCheck = Math.Min(modPlayer.unlockedAmmoSlots, modPlayer.ammoSlots.Length);
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

        private static void PatchItemSlotDrawAmmoCount(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);

                if (!TryFindAmmoCountLocal(c, out int totalCountLocal))
                {
                    ModContent.GetInstance<AMS>().Logger.Warn("AMS: Failed to locate ItemSlot.Draw ammo total local for IL patch.");
                    return;
                }

                if (!c.TryGotoNext(
                    MoveType.Before,
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdfld<Item>(nameof(Item.fishingPole))
                ))
                {
                    ModContent.GetInstance<AMS>().Logger.Warn("AMS: Failed to locate ItemSlot.Draw post-ammo insertion point for IL patch.");
                    return;
                }

                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldarg_3);
                EmitLdloc(c, totalCountLocal);
                c.EmitDelegate<Func<Item[], int, int, int>>(AdjustDisplayedAmmoCount);
                EmitStloc(c, totalCountLocal);
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<AMS>().Logger.Error($"AMS: ItemSlot.Draw IL patch threw exception: {ex}");
            }
        }

        private static bool TryFindAmmoCountLocal(ILCursor c, out int totalCountLocal)
        {
            totalCountLocal = -1;
            int foundLocal = -1;

            if (c.TryGotoNext(
                MoveType.After,
                i => i.MatchLdfld<Item>(nameof(Item.useAmmo)),
                i => i.MatchLdcI4(0),
                i => i.MatchBle(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchLdfld<Item>(nameof(Item.useAmmo)),
                i => i.MatchPop(),
                i => i.MatchLdcI4(0),
                i => i.MatchStloc(out foundLocal)
            ))
            {
                totalCountLocal = foundLocal;
                return true;
            }

            c.Index = 0;
            foundLocal = -1;

            if (c.TryGotoNext(
                MoveType.After,
                i => i.MatchLdfld<Item>(nameof(Item.useAmmo)),
                i => i.MatchLdcI4(0),
                i => i.MatchBle(out _),
                i => i.MatchLdcI4(0),
                i => i.MatchStloc(out foundLocal)
            ))
            {
                totalCountLocal = foundLocal;
                return true;
            }

            return false;
        }

        private static void EmitLdloc(ILCursor c, int index)
        {
            switch (index)
            {
                case 0:
                    c.Emit(OpCodes.Ldloc_0);
                    return;
                case 1:
                    c.Emit(OpCodes.Ldloc_1);
                    return;
                case 2:
                    c.Emit(OpCodes.Ldloc_2);
                    return;
                case 3:
                    c.Emit(OpCodes.Ldloc_3);
                    return;
            }

            if (index <= byte.MaxValue)
                c.Emit(OpCodes.Ldloc_S, (byte)index);
            else
                throw new InvalidOperationException($"AMS: Unsupported local index for ldloc: {index}");
        }

        private static void EmitStloc(ILCursor c, int index)
        {
            switch (index)
            {
                case 0:
                    c.Emit(OpCodes.Stloc_0);
                    return;
                case 1:
                    c.Emit(OpCodes.Stloc_1);
                    return;
                case 2:
                    c.Emit(OpCodes.Stloc_2);
                    return;
                case 3:
                    c.Emit(OpCodes.Stloc_3);
                    return;
            }

            if (index <= byte.MaxValue)
                c.Emit(OpCodes.Stloc_S, (byte)index);
            else
                throw new InvalidOperationException($"AMS: Unsupported local index for stloc: {index}");
        }

        private static int AdjustDisplayedAmmoCount(Item[] inv, int slot, int currentCount)
        {
            if (currentCount < 0 || inv == null)
                return currentCount;

            Player player = Main.LocalPlayer;
            if (player == null)
                return currentCount;

            if (!ReferenceEquals(inv, player.inventory))
                return currentCount;

            if (slot < 0 || slot >= inv.Length)
                return currentCount;

            Item weapon = inv[slot];
            if (weapon == null || weapon.IsAir || weapon.useAmmo <= 0)
                return currentCount;

            AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
            int maxSlotsToCheck = Math.Min(modPlayer.unlockedAmmoSlots, modPlayer.ammoSlots.Length);
            int wheelAmmoTotal = 0;

            for (int i = 0; i < maxSlotsToCheck; i++)
            {
                Item ammo = modPlayer.ammoSlots[i];
                if (!AMSPlayer.IsAmmoItem(ammo))
                    continue;

                if (!ItemLoader.CanChooseAmmo(weapon, ammo, player))
                    continue;

                wheelAmmoTotal += ammo.stack;
            }

            if (wheelAmmoTotal <= 0)
                return currentCount;

            long combined = (long)currentCount + wheelAmmoTotal;
            return combined > int.MaxValue ? int.MaxValue : (int)combined;
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

