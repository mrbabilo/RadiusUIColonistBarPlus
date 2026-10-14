using HarmonyLib;
using RimWorld;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawIcons")]
internal static class Patch_DrawIcons_Toggle
{
	private static bool Prefix()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return true;
		}
		return settings.showActivityIcons;
	}
}
