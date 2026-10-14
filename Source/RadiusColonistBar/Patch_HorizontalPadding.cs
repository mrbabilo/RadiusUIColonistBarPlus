using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace RadiusColonistBar;

[HarmonyPatch]
internal static class Patch_HorizontalPadding
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		Type t = typeof(ColonistBarDrawLocsFinder);
		yield return AccessTools.Method(t, "FindBestScale", (Type[])null, (Type[])null);
		yield return AccessTools.Method(t, "GetDrawLoc", (Type[])null, (Type[])null);
		yield return AccessTools.Method(t, "CalculateDrawLocs", new Type[5]
		{
			typeof(List<Vector2>),
			typeof(float),
			typeof(bool),
			typeof(int),
			typeof(int)
		}, (Type[])null);
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo hspace = AccessTools.Method(typeof(Patch_HorizontalPadding), "HSpace", (Type[])null, (Type[])null);
		foreach (CodeInstruction ci in instructions)
		{
			if (ci.opcode == OpCodes.Ldc_R4 && ci.operand is float f && f == 24f)
			{
				yield return new CodeInstruction(OpCodes.Call, (object)hspace);
			}
			else
			{
				yield return ci;
			}
		}
	}

	public static float HSpace()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return 24f;
		}
		return 24f + Mathf.Clamp(settings.horizontalPadding, -16f, 48f);
	}
}
