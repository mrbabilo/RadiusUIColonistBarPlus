using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBar))]
internal static class Patch_BarMetrics
{
	internal static bool IslandActive(RadiusColonistBarSettings s)
	{
		return s != null && s.masterEnabled && s.liveView && !s.warbandMode && !s.partyPanel;
	}

	[HarmonyPatch("Size", MethodType.Getter)]
	[HarmonyPostfix]
	private static void SizePostfix(ColonistBar __instance, ref Vector2 __result)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (IslandActive(settings))
		{
			Vector2 vector = RcbIsland.SegSize(__instance.Scale);
			if (RcbGuard.Finite(vector.x) && RcbGuard.Finite(vector.y) && vector.x > 1f && vector.y > 1f)
			{
				__result = vector;
			}
		}
	}

	[HarmonyPatch("SpaceBetweenColonistsHorizontal", MethodType.Getter)]
	[HarmonyPostfix]
	private static void SpacePostfix(ColonistBar __instance, ref float __result)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (IslandActive(settings))
		{
			__result = Mathf.Max(1f, 2f * __instance.Scale);
		}
	}
}
