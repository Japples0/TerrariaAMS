using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AMS
{
    public class AMS : Mod
    {
        public static ModKeybind AMSKeybind;
        public static ModKeybind AmmoCycleLeftKeybind;
        public static ModKeybind AmmoCycleRightKeybind;

        public const byte SyncUnlockedAmmoSlotsMessage = 0;

        public override void Load()
        {
            AMSKeybind = KeybindLoader.RegisterKeybind(this, "Ammo Wheel", "LeftAlt");
            AmmoCycleLeftKeybind = KeybindLoader.RegisterKeybind(this, "Ammo Cycle Left", "Q");
            AmmoCycleRightKeybind = KeybindLoader.RegisterKeybind(this, "Ammo Cycle Right", "E");
        }

        public override void Unload()
        {
            AMSKeybind = null;
            AmmoCycleLeftKeybind = null;
            AmmoCycleRightKeybind = null;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte messageType = reader.ReadByte();

            switch (messageType)
            {
                case SyncUnlockedAmmoSlotsMessage:
                {
                    int playerIndex = reader.ReadByte();
                    int unlockedSlots = reader.ReadByte();

                    if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                        return;

                    Player player = Main.player[playerIndex];
                    if (player == null || !player.active)
                        return;

                    AMSPlayer modPlayer = player.GetModPlayer<AMSPlayer>();
                    modPlayer.unlockedAmmoSlots = Math.Clamp(unlockedSlots, AMSPlayer.StartingAmmoSlots, AMSPlayer.MaxAmmoSlots);

                    if (Main.netMode == NetmodeID.Server)
                        modPlayer.SyncUnlockedAmmoSlots(-1, whoAmI);

                    break;
                }
            }
        }
    }
}
