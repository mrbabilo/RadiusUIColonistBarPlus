using HarmonyLib;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): turning fire at will off (squad order or the pawn's own gizmo) ends its
// assault, including the attack job already running, which the vanilla setter leaves alone.
[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.FireAtWill), MethodType.Setter)]
internal static class Patch_SquadAssault_HoldFire
{
	private static void Postfix(Pawn_DraftController __instance, bool value)
	{
		if (!value && Current.Game != null)
		{
			SquadAssault.Get?.End(__instance.pawn);
		}
	}
}
