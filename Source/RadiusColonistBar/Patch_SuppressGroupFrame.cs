using HarmonyLib;
using RimWorld;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawGroupFrame")]
internal static class Patch_SuppressGroupFrame
{
	private static bool Prefix()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		return settings == null || !settings.masterEnabled;
	}
}
