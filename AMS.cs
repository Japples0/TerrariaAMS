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
        public static ModKeybind AmmoCycleUpKeybind;
        public static ModKeybind AmmoCycleDownKeybind;

        public override void Load()
        {
            AMSKeybind = KeybindLoader.RegisterKeybind(this, "Ammo Wheel", "LeftAlt");
            AmmoCycleUpKeybind = KeybindLoader.RegisterKeybind(this, "Ammo Cycle Up", "MouseScrollUp");
            AmmoCycleDownKeybind = KeybindLoader.RegisterKeybind(this, "Ammo Cycle Down", "MouseScrollDown");
        }

        public override void Unload()
        {
            AMSKeybind = null;
            AmmoCycleUpKeybind = null;
            AmmoCycleDownKeybind = null;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            AMSMessageType messageType = (AMSMessageType)reader.ReadByte();

            switch (messageType)
            {
                case AMSMessageType.SyncUnlockedAmmoSlots:
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
