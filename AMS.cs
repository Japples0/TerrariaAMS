using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AMS
{
    public class AMS : Mod
    {
        public static ModKeybind AMSKeybind;

        public override void Load()
        {
            AMSKeybind = KeybindLoader.RegisterKeybind(this, "Ammo Wheel", "LeftAlt");
        }
    }
}