using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
internal static class RCBTextures
{
	public static readonly Texture2D BloodGauge = ContentFinder<Texture2D>.Get("UI/RCB_BloodGauge", reportFailure: false);

	public static readonly Texture2D CtxZzz = IconSet.Status.Zzz;

	public static readonly Texture2D CtxResearch = IconSet.Status.Research;

	public static readonly Texture2D CtxAim = IconSet.Status.Aim;

	public static readonly Texture2D CtxIdle = IconSet.Status.Idle;

	public static readonly Texture2D CtxBurning = IconSet.Status.Burning;

	public static readonly Texture2D CtxMedical = IconSet.Status.Medical;

	public static readonly Texture2D CtxCaravan = IconSet.Status.Caravan;

	public static readonly Texture2D CtxCross = IconSet.Alert.Info;

	public static readonly Texture2D CtxMount = IconSet.Status.Mount;

	public static readonly Texture2D EyeOpen = IconSet.Action.EyeOpenBar;

	public static readonly Texture2D QAJump = IconSet.Action.Jump;

	public static readonly Texture2D QAGear = IconSet.Action.Apparel;

	public static readonly Texture2D Gear = IconSet.Action.Settings;

	public static readonly Texture2D CommandCenter = IconSet.Action.CommandCenter;

	public static readonly Texture2D Pin = IconSet.Action.Pin;

	public static readonly Texture2D TabArmory = IconSet.Action.TabArmory;

	public static readonly Texture2D ChipSharp = IconSet.Stat.Sharp;

	public static readonly Texture2D ChipBlunt = IconSet.Stat.Blunt;

	public static readonly Texture2D ChipHeat = IconSet.Stat.Heat;

	public static readonly Texture2D EyeClosed = IconSet.Action.EyeClosedBar;

	private static readonly Texture2D SlotWeapon = IconSet.Slot.Weapon;

	private static readonly Texture2D SlotHead = IconSet.Slot.Helm;

	private static readonly Texture2D SlotTorso = IconSet.Slot.Torso;

	private static readonly Texture2D SlotLegs = IconSet.Slot.Legs;

	private static readonly Texture2D SlotFeet = IconSet.Slot.Boots;

	private static readonly Texture2D SlotBelt = IconSet.Slot.Belt;

	public const int SquadIconCount = 8;

	public static Texture2D SlotIcon(KitSlot s)
	{
		return s switch
		{
			KitSlot.Weapon => SlotWeapon, 
			KitSlot.Head => SlotHead, 
			KitSlot.Torso => SlotTorso, 
			KitSlot.Legs => SlotLegs, 
			KitSlot.Feet => SlotFeet, 
			KitSlot.Belt => SlotBelt, 
			_ => null, 
		};
	}

	public static Texture2D SquadIcon(int i)
	{
		return (i >= 0 && i < 8) ? IconSet.SquadEmblems[i].Texture : null;
	}
}
