using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace AMS
{
    public enum AmmoWheelThemePreset
    {
        Squares,
        BlueCircle
    }

    public class AmmoWheelClientConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("Interaction")]
        [DefaultValue(false)]
        public bool ToggleWheelOnPress;

        [Header("Appearance")]
        [DefaultValue(AmmoWheelThemePreset.Squares)]
        public AmmoWheelThemePreset ThemePreset;

        [Range(0f, 1f)]
        [Increment(0.05f)]
        [Slider]
        [DefaultValue(1f)]
        public float WheelUiOpacity;

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

        [Header("Audio")]
        [Range(0f, 1f)]
        [Increment(0.05f)]
        [Slider]
        [DefaultValue(0.65f)]
        public float WheelRotateVolume;
    }
}

