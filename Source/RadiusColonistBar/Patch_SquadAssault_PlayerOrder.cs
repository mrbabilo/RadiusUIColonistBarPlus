using HarmonyLib;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): an order the player gives a pawn ends its assault. The assault node runs
// before the job queue, so without this the queued order would never be taken while a target exists.
[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
internal static class Patch_SquadAssault_PlayerOrder
{
	private static void Postfix(bool __result, Pawn ___pawn)
	{
		if (__result)
		{
			SquadAssault.Get?.Clear(___pawn);
		}
	}
}
