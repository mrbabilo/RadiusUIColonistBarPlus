using HarmonyLib;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBar), "ColonistBarOnGUI")]
internal static class Patch_BarGUI_ReorderScope
{
	private static void Prefix()
	{
		LeftDragReorder.inBarGUI = RadiusColonistBarMod.Settings?.masterEnabled ?? false;
	}

	[HarmonyPriority(0)]
	private static void Postfix()
	{
		LeftDragReorder.inBarGUI = false;
		try
		{
			if (ReorderableWidget.Dragging && Find.Selector?.dragBox != null)
			{
				Find.Selector.dragBox.active = false;
			}
		}
		catch
		{
		}
	}
}
