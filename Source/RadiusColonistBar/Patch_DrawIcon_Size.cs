using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawIcon")]
internal static class Patch_DrawIcon_Size
{
	private const float BaseIconMaxSize = 20f;

	private static void Prefix(ref Vector2 pos, ref float iconSize, out float __state)
	{
		__state = pos.y;
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings != null && settings.masterEnabled)
		{
			float num = ((Find.ColonistBar != null) ? Find.ColonistBar.Scale : 1f);
			float num2 = 20f * num * Mathf.Clamp(settings.activityIconScale, 0.2f, 1.5f);
			float num3 = pos.y + iconSize;
			iconSize = num2;
			pos.y = num3 - num2;
		}
	}

	private static void Postfix(ref Vector2 pos, float __state)
	{
		pos.y = __state;
	}
}
