using HarmonyLib;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(DynamicDrawManager), "DrawDynamicThings")]
internal static class Patch_DynamicDraw_LiveView
{
	private static void Postfix()
	{
		LiveViewManager.OnDynamicThingsDrawn();
	}
}
