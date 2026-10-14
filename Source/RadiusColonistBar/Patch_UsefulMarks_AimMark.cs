using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RadiusColonistBar;

[HarmonyPatch]
internal static class Patch_UsefulMarks_AimMark
{
	private static bool Prepare()
	{
		return MarksHost.Available && MarksHost.DrawMarkTarget != null;
	}

	private static MethodBase TargetMethod()
	{
		return MarksHost.DrawMarkTarget;
	}

	private static bool Prefix(Texture2D icon, Texture2D ascentIcon, Color baseColor, Color ascentColor, ref Vector2 point, ref float? limitScalePix)
	{
		if (MarksHost.Hosting)
		{
			return MarksHost.Aim(icon, ascentIcon, baseColor, ascentColor, ref point, ref limitScalePix);
		}
		if (!MarksHost.Live)
		{
			return true;
		}
		return !Patch_DrawColonist_Overlay.DrawingBarColonist;
	}
}
