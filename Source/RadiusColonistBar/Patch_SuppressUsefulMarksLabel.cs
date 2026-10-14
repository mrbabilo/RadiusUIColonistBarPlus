using System;
using System.Reflection;
using HarmonyLib;

namespace RadiusColonistBar;

[HarmonyPatch]
internal static class Patch_SuppressUsefulMarksLabel
{
	private static bool Prepare()
	{
		return AccessTools.TypeByName("UsefulMarks.LabelDrawer") != null;
	}

	private static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("UsefulMarks.LabelDrawer");
		return (type != null) ? AccessTools.Method(type, "DrawMarkLabel", (Type[])null, (Type[])null) : null;
	}

	private static bool Prefix()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return true;
		}
		return !Patch_DrawColonist_Overlay.DrawingBarColonist;
	}
}
