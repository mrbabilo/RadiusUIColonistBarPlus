using HarmonyLib;
using RimWorld;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawSelectionOverlayOnGUI")]
internal static class Patch_SuppressBarSelectionOverlay
{
	private static bool Prefix()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		return settings == null || !settings.masterEnabled || !settings.liveView;
	}
}
