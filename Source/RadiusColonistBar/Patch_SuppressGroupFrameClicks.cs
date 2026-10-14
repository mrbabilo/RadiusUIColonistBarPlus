using HarmonyLib;
using RimWorld;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "HandleGroupFrameClicks")]
internal static class Patch_SuppressGroupFrameClicks
{
	private static bool Prefix()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		return settings == null || !settings.masterEnabled;
	}
}
