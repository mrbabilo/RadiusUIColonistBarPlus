using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "HandleClicks")]
internal static class Patch_HubClick
{
	private static void Prefix(Pawn colonist)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled || colonist == null)
		{
			return;
		}
		Event current = Event.current;
		if (current != null && current.type == EventType.MouseDown && current.button == 0)
		{
			HubTarget t;
			if (current.alt)
			{
				Dialog_CommandCenter.OpenFor(colonist, -1);
				current.Use();
			}
			else if (HubLinks.TryHit(current.mousePosition, colonist.thingIDNumber, out t))
			{
				HubRouter.OpenTab(colonist, t);
				current.Use();
			}
		}
	}
}
