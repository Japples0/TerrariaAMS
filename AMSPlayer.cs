using AMS.Systems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AMS
{
    public class AMSPlayer : ModPlayer
    {
        public const int MaxAmmoSlots = 6;

        public int unlockedAmmoSlots = MaxAmmoSlots;
        public Item[] ammoSlots;

        public override void Initialize()
        {
            ammoSlots = new Item[MaxAmmoSlots];

            for (int i = 0; i < ammoSlots.Length; i++)
            {
                ammoSlots[i] = new Item();
                ammoSlots[i].TurnToAir();
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["ammoSlotsUnlocked"] = unlockedAmmoSlots;

            List<TagCompound> savedSlots = new List<TagCompound>(ammoSlots.Length);
            for (int i = 0; i < ammoSlots.Length; i++)
                savedSlots.Add(ItemIO.Save(ammoSlots[i]));

            tag["ammoSlots"] = savedSlots;
        }

        public override void LoadData(TagCompound tag)
        {
            unlockedAmmoSlots = MaxAmmoSlots;

            if (tag.ContainsKey("ammoSlotsUnlocked"))
                unlockedAmmoSlots = Math.Clamp(tag.GetInt("ammoSlotsUnlocked"), 1, MaxAmmoSlots);

            // Migrate early saves (single unlocked slot) while unlock progression isn't implemented.
            if (unlockedAmmoSlots == 1)
                unlockedAmmoSlots = MaxAmmoSlots;

            for (int i = 0; i < ammoSlots.Length; i++)
            {
                ammoSlots[i] = new Item();
                ammoSlots[i].TurnToAir();
            }

            if (!tag.ContainsKey("ammoSlots"))
                return;

            IList<TagCompound> savedSlots = tag.GetList<TagCompound>("ammoSlots");
            for (int i = 0; i < ammoSlots.Length && i < savedSlots.Count; i++)
            {
                Item loaded = ItemIO.Load(savedSlots[i]);

                ammoSlots[i] = loaded;

                if (!IsAmmoItem(ammoSlots[i]))
                    ammoSlots[i].TurnToAir();
            }
        }

        public override void PostUpdate()
        {
            NormalizeAmmoSlots();
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (UISystems.ammoWheel == null)
                return;

            AmmoWheelClientConfig config = ModContent.GetInstance<AmmoWheelClientConfig>();

            if (config.ToggleWheelOnPress)
            {
                if (AMS.AMSKeybind.JustPressed)
                {
                    if (UISystems.ammoWheel.Visible)
                        UISystems.ammoWheel.Close();
                    else
                        UISystems.ammoWheel.Open();
                }

                return;
            }

            if (AMS.AMSKeybind.JustPressed)
                UISystems.ammoWheel.Open();

            if (AMS.AMSKeybind.JustReleased)
                UISystems.ammoWheel.Close();
        }

        public bool TryGetAmmoFromWheel(Item weapon, out Item ammo)
        {
            ammo = null;

            if (weapon == null || weapon.IsAir || weapon.useAmmo <= 0 || ammoSlots == null)
                return false;

            int maxSlotsToCheck = Math.Min(unlockedAmmoSlots, ammoSlots.Length);
            for (int i = 0; i < maxSlotsToCheck; i++)
            {
                Item candidate = ammoSlots[i];
                if (!IsValidAmmoForWeapon(Player, weapon, candidate))
                    continue;

                ammo = candidate;
                return true;
            }

            return false;
        }

        public void NormalizeAmmoSlots()
        {
            if (ammoSlots == null || ammoSlots.Length != MaxAmmoSlots)
            {
                Initialize();
                return;
            }

            for (int i = 0; i < ammoSlots.Length; i++)
            {
                if (ammoSlots[i] == null)
                {
                    ammoSlots[i] = new Item();
                    ammoSlots[i].TurnToAir();
                }

                if (ammoSlots[i].stack <= 0 || !IsAmmoItem(ammoSlots[i]))
                    ammoSlots[i].TurnToAir();
            }
        }

        public static bool IsAmmoItem(Item item)
        {
            return item != null
                && !item.IsAir
                && item.stack > 0
                && item.ammo > 0
                && !item.notAmmo;
        }

        private static bool IsValidAmmoForWeapon(Player player, Item weapon, Item ammo)
        {
            if (!IsAmmoItem(ammo))
                return false;

            return ItemLoader.CanChooseAmmo(weapon, ammo, player);
        }
    }
}
