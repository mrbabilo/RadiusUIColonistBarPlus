using HarmonyLib;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ReorderableWidget), "Reorderable")]
internal static class Patch_Reorderable_LeftButton
{
	private static void Prefix(ref bool useRightButton)
	{
		if (LeftDragReorder.inBarGUI & useRightButton)
		{
			useRightButton = false;
		}
	}
}
