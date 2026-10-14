using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBar), "ColonistBarOnGUI")]
internal static class Patch_BarRelayout
{
	[HarmonyPriority(600)]
	private static void Prefix()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (!Patch_BarMetrics.IslandActive(settings))
		{
			return;
		}
		ColonistBar colonistBar = Find.ColonistBar;
		if (colonistBar != null)
		{
			if (Patch_Recache_SquadsAndHidden.PendingRebuild)
			{
				Patch_Recache_SquadsAndHidden.PendingRebuild = false;
				colonistBar.MarkColonistsDirty();
			}
			RcbIslandAnim.AdvanceOnce(RcbIsland.LastSlab);
			RcbAlertBand.AdvanceOnce();
			RcbAlertBand.HandleInput();
			RcbIsland.HandleChromeInput();
			List<Vector2> drawLocs = colonistBar.DrawLocs;
			if (drawLocs != null && drawLocs.Count != 0)
			{
				Patch_ColonistBarSize.RelayoutIsland(drawLocs, colonistBar.Scale);
			}
		}
	}
}
