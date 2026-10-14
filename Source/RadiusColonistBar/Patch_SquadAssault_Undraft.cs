using HarmonyLib;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): undrafting a pawn ends its assault. ExposeData writes draftedInt directly,
// so loading a save never comes through here.
[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
internal static class Patch_SquadAssault_Undraft
{
	private static void Postfix(Pawn_DraftController __instance)
	{
		if (Current.Game != null && !__instance.Drafted)
		{
			SquadAssault.Get?.Clear(__instance.pawn);
		}
	}
}
