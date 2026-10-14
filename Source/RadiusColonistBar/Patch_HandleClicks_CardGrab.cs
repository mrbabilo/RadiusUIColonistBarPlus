using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "HandleClicks")]
internal static class Patch_HandleClicks_CardGrab
{
	private static void Prefix(Pawn colonist, ref Rect rect)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings != null && settings.masterEnabled && ReorderBounds.TryGet(colonist, out var bounds))
		{
			rect = bounds;
		}
	}
}
