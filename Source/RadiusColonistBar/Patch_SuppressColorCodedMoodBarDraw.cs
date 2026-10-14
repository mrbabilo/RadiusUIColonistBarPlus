using System;
using System.Reflection;
using HarmonyLib;

namespace RadiusColonistBar;

[HarmonyPatch]
internal static class Patch_SuppressColorCodedMoodBarDraw
{
	private static bool Prepare()
	{
		return AccessTools.TypeByName("ColoredMoodBar13.VanillaDrawColonist") != null;
	}

	private static MethodBase TargetMethod()
	{
		return AccessTools.Method("ColoredMoodBar13.VanillaDrawColonist:Prefix", (Type[])null, (Type[])null);
	}

	private static bool Prefix(ref bool __result)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings != null && settings.masterEnabled)
		{
			__result = true;
			return false;
		}
		return true;
	}
}
