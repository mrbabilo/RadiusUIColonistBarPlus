using HarmonyLib;
using RimWorld;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawCaravanSelectionOverlayOnGUI")]
internal static class Patch_SuppressBarCaravanSelectionOverlay
{
	private static bool Prefix()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		return settings == null || !settings.masterEnabled || !settings.liveView;
	}
}
