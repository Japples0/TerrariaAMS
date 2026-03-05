using AMS.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AMS
{
    public class AMSPlayer : ModPlayer
    {
        public int unlockedAmmoSlots = 1;

        public Item[] ammoSlots;

        public override void Initialize()
        {
            ammoSlots = new Item[6];

            for (int i = 0; i < ammoSlots.Length; i++)
            {
                ammoSlots[i] = new Item();
                ammoSlots[i].TurnToAir();
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["ammoSlotsUnlocked"] = unlockedAmmoSlots;
        }

        public override void LoadData(TagCompound tag)
        {
            unlockedAmmoSlots = tag.GetInt("ammoSlotsUnlocked");
        }
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (AMS.AMSKeybind.JustPressed)
            {
                UISystems.ammoWheel.Open();
            }

            if (AMS.AMSKeybind.JustReleased)
            {
                UISystems.ammoWheel.Close();
            }
        }
    }
}