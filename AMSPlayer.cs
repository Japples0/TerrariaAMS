using AMS.Systems;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AMS
{
    public class AMSPlayer : ModPlayer
    {
        public const int MaxAmmoSlots = 6;
        public const int StartingAmmoSlots = 1;

        public int unlockedAmmoSlots = StartingAmmoSlots;
        public int selectedAmmoSlot;
        public Item[] ammoSlots;

        private const float WheelRotationStep = 0.18f;

        private float wheelRotation;
        private float wheelRotationTarget;

        private static readonly SoundStyle WheelRotateSound = new SoundStyle("AMS/Assets/SoundFX/ammo_cycle")
        {
            Volume = 0.65f,
            PitchVariance = 0.08f
        };

        public bool HasUnlockedAllAmmoSlots => unlockedAmmoSlots >= MaxAmmoSlots;

        public override void Initialize()
        {
            unlockedAmmoSlots = StartingAmmoSlots;
            selectedAmmoSlot = 0;
            ammoSlots = new Item[MaxAmmoSlots];
            wheelRotation = 0f;
            wheelRotationTarget = 0f;

            for (int i = 0; i < ammoSlots.Length; i++)
            {
                ammoSlots[i] = new Item();
                ammoSlots[i].TurnToAir();
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["ammoSlotsUnlocked"] = unlockedAmmoSlots;
            tag["selectedAmmoSlot"] = selectedAmmoSlot;

            List<TagCompound> savedSlots = new List<TagCompound>(ammoSlots.Length);
            for (int i = 0; i < ammoSlots.Length; i++)
                savedSlots.Add(ItemIO.Save(ammoSlots[i]));

            tag["ammoSlots"] = savedSlots;
        }

        public override void LoadData(TagCompound tag)
        {
            unlockedAmmoSlots = StartingAmmoSlots;
            selectedAmmoSlot = 0;

            if (tag.ContainsKey("ammoSlotsUnlocked"))
                unlockedAmmoSlots = Math.Clamp(tag.GetInt("ammoSlotsUnlocked"), StartingAmmoSlots, MaxAmmoSlots);

            if (tag.ContainsKey("selectedAmmoSlot"))
                selectedAmmoSlot = tag.GetInt("selectedAmmoSlot");

            for (int i = 0; i < ammoSlots.Length; i++)
            {
                ammoSlots[i] = new Item();
                ammoSlots[i].TurnToAir();
            }

            if (!tag.ContainsKey("ammoSlots"))
            {
                selectedAmmoSlot = GetSelectedAmmoSlotIndex();
                wheelRotationTarget = GetTargetRotation();
                wheelRotation = wheelRotationTarget;
                return;
            }

            IList<TagCompound> savedSlots = tag.GetList<TagCompound>("ammoSlots");
            for (int i = 0; i < ammoSlots.Length && i < savedSlots.Count; i++)
            {
                Item loaded = ItemIO.Load(savedSlots[i]);
                ammoSlots[i] = loaded;

                if (!IsAmmoItem(ammoSlots[i]))
                    ammoSlots[i].TurnToAir();
            }

            selectedAmmoSlot = GetSelectedAmmoSlotIndex();
            wheelRotationTarget = GetTargetRotation();
            wheelRotation = wheelRotationTarget;
        }

        public override void PostUpdate()
        {
            NormalizeAmmoSlots();
            UpdateWheelRotation();
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            HandleAmmoCycleInput();

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

            int maxSlotsToCheck = GetUnlockedSlotCount();
            if (maxSlotsToCheck <= 0)
                return false;

            int startIndex = GetSelectedAmmoSlotIndex();

            for (int offset = 0; offset < maxSlotsToCheck; offset++)
            {
                int index = WrapSlotIndex(startIndex + offset, maxSlotsToCheck);
                Item candidate = ammoSlots[index];

                if (!IsValidAmmoForWeapon(Player, weapon, candidate))
                    continue;

                ammo = candidate;
                return true;
            }

            return false;
        }

        public int GetSelectedAmmoSlotIndex()
        {
            int unlockedSlotCount = GetUnlockedSlotCount();
            if (unlockedSlotCount <= 0)
            {
                selectedAmmoSlot = 0;
                UpdateWheelRotationTarget();
                return 0;
            }

            selectedAmmoSlot = WrapSlotIndex(selectedAmmoSlot, unlockedSlotCount);
            UpdateWheelRotationTarget();
            return selectedAmmoSlot;
        }

        public void SetSelectedAmmoSlot(int slotIndex)
        {
            int unlockedSlotCount = GetUnlockedSlotCount();
            if (unlockedSlotCount <= 0)
            {
                selectedAmmoSlot = 0;
                UpdateWheelRotationTarget();
                return;
            }

            selectedAmmoSlot = WrapSlotIndex(slotIndex, unlockedSlotCount);
            UpdateWheelRotationTarget();
        }

        public bool TryCycleSelectedAmmoSlot(int direction, Item weapon = null)
        {
            int unlockedSlotCount = GetUnlockedSlotCount();
            if (unlockedSlotCount <= 1)
                return false;

            int step = direction >= 0 ? 1 : -1;
            int startIndex = GetSelectedAmmoSlotIndex();
            bool hasWeaponAmmoType = weapon != null && !weapon.IsAir && weapon.useAmmo > 0;

            for (int offset = 1; offset <= unlockedSlotCount; offset++)
            {
                int candidateIndex = WrapSlotIndex(startIndex + (step * offset), unlockedSlotCount);
                Item candidate = ammoSlots[candidateIndex];

                bool validCandidate = hasWeaponAmmoType
                    ? IsValidAmmoForWeapon(Player, weapon, candidate)
                    : IsAmmoItem(candidate);

                if (!validCandidate)
                    continue;

                if (candidateIndex == startIndex)
                    return false;

                selectedAmmoSlot = candidateIndex;
                UpdateWheelRotationTarget();
                return true;
            }

            int fallbackIndex = WrapSlotIndex(startIndex + step, unlockedSlotCount);
            if (fallbackIndex == startIndex)
                return false;

            selectedAmmoSlot = fallbackIndex;
            UpdateWheelRotationTarget();
            return true;
        }

        public bool TryUnlockAmmoSlotsTo(int targetUnlockedSlots)
        {
            int clampedTarget = Math.Clamp(targetUnlockedSlots, StartingAmmoSlots, MaxAmmoSlots);
            if (clampedTarget <= unlockedAmmoSlots)
                return false;

            unlockedAmmoSlots = clampedTarget;
            selectedAmmoSlot = GetSelectedAmmoSlotIndex();
            return true;
        }

        public void SyncUnlockedAmmoSlots(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = Mod.GetPacket();
            packet.Write(AMS.SyncUnlockedAmmoSlotsMessage);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)unlockedAmmoSlots);
            packet.Send(toWho, fromWho);
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

            selectedAmmoSlot = GetSelectedAmmoSlotIndex();
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

        private int GetUnlockedSlotCount()
        {
            if (ammoSlots == null || ammoSlots.Length == 0)
                return 0;

            return Math.Clamp(unlockedAmmoSlots, 1, ammoSlots.Length);
        }

        private static int WrapSlotIndex(int index, int slotCount)
        {
            if (slotCount <= 0)
                return 0;

            int wrapped = index % slotCount;
            if (wrapped < 0)
                wrapped += slotCount;

            return wrapped;
        }

        private void HandleAmmoCycleInput()
        {
            if (Main.gameMenu || Player == null || !Player.active)
                return;

            if (IsContextMenuOpen())
                return;

            bool wheelVisible = UISystems.ammoWheel != null && UISystems.ammoWheel.Visible;
            int direction = 0;

            if (AMS.AmmoCycleRightKeybind != null && AMS.AmmoCycleRightKeybind.JustPressed)
                direction++;

            if (AMS.AmmoCycleLeftKeybind != null && AMS.AmmoCycleLeftKeybind.JustPressed)
                direction--;

            if (wheelVisible)
            {
                int scrollDelta = PlayerInput.ScrollWheelDelta;
                if (scrollDelta > 0)
                    direction++;

                if (scrollDelta < 0)
                    direction--;
            }

            if (direction == 0)
                return;

            int step = direction > 0 ? 1 : -1;
            if (TryCycleSelectedAmmoSlot(step, Player.HeldItem))
                PlayWheelRotateSound();
        }

        private void PlayWheelRotateSound()
        {
            if (Main.dedServ || Player == null || Player.whoAmI != Main.myPlayer)
                return;

            AmmoWheelClientConfig config = ModContent.GetInstance<AmmoWheelClientConfig>();
            SoundStyle style = WheelRotateSound;
            style.Volume = Math.Clamp(config.WheelRotateVolume, 0f, 1f);
            SoundEngine.PlaySound(style);
        }

        public float GetWheelRotation()
        {
            return wheelRotation;
        }

        private void UpdateWheelRotationTarget()
        {
            wheelRotationTarget = GetTargetRotation();
        }

        private float GetTargetRotation()
        {
            float segmentAngle = MathHelper.TwoPi / MaxAmmoSlots;
            return MathHelper.WrapAngle(-selectedAmmoSlot * segmentAngle);
        }

        private void UpdateWheelRotation()
        {
            float diff = MathHelper.WrapAngle(wheelRotationTarget - wheelRotation);
            if (Math.Abs(diff) <= WheelRotationStep)
            {
                wheelRotation = wheelRotationTarget;
                return;
            }

            wheelRotation += Math.Sign(diff) * WheelRotationStep;
            wheelRotation = MathHelper.WrapAngle(wheelRotation);
        }

        private static bool IsContextMenuOpen()
        {
            return Main.playerInventory || Main.mapFullscreen;
        }
    }
}




