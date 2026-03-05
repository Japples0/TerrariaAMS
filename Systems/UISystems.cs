using AMS.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using System.Collections.Generic;

namespace AMS.Systems
{
    public class UISystems : ModSystem
    {
        internal static UserInterface ammoWheelInterface;
        internal static AmmoWheelUI ammoWheel;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                ammoWheel = new AmmoWheelUI();
                ammoWheel.Activate();

                ammoWheelInterface = new UserInterface();
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (ammoWheel.Visible)
            {
                ammoWheelInterface?.SetState(ammoWheel);
            }

            if (ammoWheelInterface?.CurrentState != null)
            {
                ammoWheelInterface.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "AMS: Ammo Wheel",
                    delegate
                    {
                        if (ammoWheel.Visible)
                        {
                            ammoWheelInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}