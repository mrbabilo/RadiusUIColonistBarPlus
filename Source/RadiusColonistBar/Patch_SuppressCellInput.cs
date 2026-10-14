using HarmonyLib;
using RimWorld;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "HandleClicks")]
internal static class Patch_SuppressCellInput
{
	[HarmonyPriority(800)]
	private static bool Prefix()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return true;
		}
		if (!Patch_BarMetrics.IslandActive(settings))
		{
			return true;
		}
		return !RcbIslandAnim.SuppressCellInput;
	}
}
