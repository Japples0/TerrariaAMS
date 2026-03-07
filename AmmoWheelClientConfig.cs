using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace AMS
{
    public class AmmoWheelClientConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("Interaction")]
        [DefaultValue(false)]
        public bool ToggleWheelOnPress;

        [Header("WheelPosition")]
        [Range(-900f, 900f)]
        [Increment(5f)]
        [Slider]
        [DefaultValue(0f)]
        public float WheelOffsetX;

        [Range(-900f, 900f)]
        [Increment(5f)]
        [Slider]
        [DefaultValue(0f)]
        public float WheelOffsetY;
    }
}
