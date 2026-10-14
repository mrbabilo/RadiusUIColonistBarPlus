using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(GenMapUI), "DrawPawnLabel", new Type[]
{
	typeof(Pawn),
	typeof(Rect),
	typeof(float),
	typeof(float),
	typeof(Dictionary<string, string>),
	typeof(GameFont),
	typeof(bool),
	typeof(bool)
})]
[HarmonyPriority(800)]
internal static class Patch_GenMapUI_SuppressBarName
{
	private static bool Prefix()
	{
		if (!Patch_DrawColonist_Overlay.DrawingBarColonist)
		{
			return true;
		}
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return true;
		}
		return false;
	}
}
