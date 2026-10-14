using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(PortraitsCache), "IsAnimated")]
internal static class Patch_PortraitsCache_IsAnimated
{
	private struct GateState
	{
		public int tick;

		public int frame;
	}

	private static readonly Dictionary<int, GateState> gates = new Dictionary<int, GateState>();

	private static bool GateOpen(Pawn pawn, int interval)
	{
		TickManager tickManager = Find.TickManager;
		if (tickManager == null)
		{
			return false;
		}
		int ticksGame = tickManager.TicksGame;
		int frameCount = Time.frameCount;
		int thingIDNumber = pawn.thingIDNumber;
		if (gates.TryGetValue(thingIDNumber, out var value) && value.tick == ticksGame)
		{
			return value.frame == frameCount;
		}
		bool flag;
		if (interval <= 1)
		{
			flag = true;
		}
		else
		{
			int num = ticksGame;
			int num2 = (int)tickManager.TickRateMultiplier;
			if (num2 > 0)
			{
				num /= num2;
			}
			flag = (num + thingIDNumber.HashOffset()) % interval == 0;
		}
		if (gates.Count > 512)
		{
			gates.Clear();
		}
		gates[thingIDNumber] = new GateState
		{
			tick = ticksGame,
			frame = (flag ? frameCount : (-1))
		};
		return flag;
	}

	private static void Postfix(Pawn pawn, ref bool __result)
	{
		if (!__result && Current.ProgramState == ProgramState.Playing && pawn != null)
		{
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			if (settings != null && settings.masterEnabled && settings.faPortraitAnimation && !FacialAnimationBridge.FaDrivesPortraits && GateOpen(pawn, settings.faPortraitInterval) && FacialAnimationBridge.IsFaceAnimating(pawn))
			{
				__result = true;
			}
		}
	}
}
