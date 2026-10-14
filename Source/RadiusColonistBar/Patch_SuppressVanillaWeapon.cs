using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBar), "ColonistBarOnGUI")]
internal static class Patch_SuppressVanillaWeapon
{
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo repl = AccessTools.Method(typeof(Patch_SuppressVanillaWeapon), "Mode", (Type[])null, (Type[])null);
		foreach (CodeInstruction ci in instructions)
		{
			int num;
			if (ci.opcode == OpCodes.Call || ci.opcode == OpCodes.Callvirt)
			{
				object operand = ci.operand;
				if (operand is MethodInfo { Name: "get_ShowWeaponsUnderPortraitMode" } mi)
				{
					num = ((mi.DeclaringType == typeof(Prefs)) ? 1 : 0);
					goto IL_0105;
				}
			}
			num = 0;
			goto IL_0105;
			IL_0105:
			if (num != 0)
			{
				yield return new CodeInstruction(OpCodes.Call, (object)repl);
			}
			else
			{
				yield return ci;
			}
		}
	}

	private static ShowWeaponsUnderPortraitMode Mode()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings != null && settings.masterEnabled)
		{
			return ShowWeaponsUnderPortraitMode.Never;
		}
		return Prefs.ShowWeaponsUnderPortraitMode;
	}
}
