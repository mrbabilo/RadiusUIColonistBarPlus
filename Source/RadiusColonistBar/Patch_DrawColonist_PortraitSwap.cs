using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawColonist")]
internal static class Patch_DrawColonist_PortraitSwap
{
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo hook = AccessTools.Method(typeof(LiveViewManager), "MaybeReplaceWithLiveView", (Type[])null, (Type[])null);
		MethodInfo barPortrait = AccessTools.Method(typeof(LiveViewManager), "BarPortrait", (Type[])null, (Type[])null);
		MethodInfo filter = AccessTools.Method(typeof(LiveViewManager), "FilterCellTex", (Type[])null, (Type[])null);
		MethodInfo bgFilter = AccessTools.Method(typeof(LiveViewManager), "FilterBgTex", (Type[])null, (Type[])null);
		FieldInfo bgTexField = AccessTools.Field(typeof(ColonistBar), "BGTex");
		HashSet<FieldInfo> cellTexFields = new HashSet<FieldInfo>
		{
			AccessTools.Field(typeof(ColonistBarColonistDrawer), "MoodBGTex"),
			AccessTools.Field(typeof(ColonistBarColonistDrawer), "MoodAtlas"),
			AccessTools.Field(typeof(ColonistBarColonistDrawer), "MoodGradient")
		};
		cellTexFields.RemoveWhere((FieldInfo f) => f == null);
		bool portraitPatched = false;
		foreach (CodeInstruction ci in instructions)
		{
			int num;
			if (!portraitPatched && ci.opcode == OpCodes.Call)
			{
				object operand = ci.operand;
				if (operand is MethodInfo { Name: "Get" } mi)
				{
					num = ((mi.DeclaringType == typeof(PortraitsCache)) ? 1 : 0);
					goto IL_022c;
				}
			}
			num = 0;
			goto IL_022c;
			IL_0402:
			int num2;
			if (num2 != 0)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_2, (object)null);
				yield return new CodeInstruction(OpCodes.Call, (object)bgFilter);
			}
			continue;
			IL_0347:
			int num3;
			if (num3 != 0)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_2, (object)null);
				yield return new CodeInstruction(OpCodes.Call, (object)filter);
				continue;
			}
			if (ci.opcode == OpCodes.Ldsfld && bgTexField != null)
			{
				object operand = ci.operand;
				if (operand is FieldInfo bf)
				{
					num2 = ((bf == bgTexField) ? 1 : 0);
					goto IL_0402;
				}
			}
			num2 = 0;
			goto IL_0402;
			IL_022c:
			if (num != 0)
			{
				if (barPortrait != null)
				{
					ci.operand = barPortrait;
				}
				yield return ci;
				yield return new CodeInstruction(OpCodes.Ldarg_2, (object)null);
				yield return new CodeInstruction(OpCodes.Call, (object)hook);
				portraitPatched = true;
				continue;
			}
			yield return ci;
			if (ci.opcode == OpCodes.Ldsfld)
			{
				object operand = ci.operand;
				if (operand is FieldInfo fi)
				{
					num3 = (cellTexFields.Contains(fi) ? 1 : 0);
					goto IL_0347;
				}
			}
			num3 = 0;
			goto IL_0347;
		}
		if (!portraitPatched)
		{
			Log.Warning("[Radius UI - Colonist Bar] DrawColonist transpiler did not find PortraitsCache.Get; live view inactive (compat-safe fallback to vanilla draw).");
		}
	}
}
