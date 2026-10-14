using HarmonyLib;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): an order the player gives a pawn ends its assault. The mode is cleared before
// the order is taken: TryTakeOrderedJob picks the next job inside the call, and the assault node, which runs before
// the job queue, would otherwise start again and leave the order waiting in the queue.
[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
internal static class Patch_SquadAssault_PlayerOrder
{
	private static void Prefix(Pawn ___pawn, out AssaultMode? __state)
	{
		__state = null;
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault != null && squadAssault.TryGetMode(___pawn, out var mode))
		{
			__state = mode;
			squadAssault.Clear(___pawn);
		}
	}

	private static void Postfix(bool __result, Pawn ___pawn, AssaultMode? __state)
	{
		if (!__state.HasValue)
		{
			return;
		}
		if (!__result)
		{
			// Order refused: the pawn keeps its assault.
			SquadAssault.Get?.Set(___pawn, __state.Value);
			return;
		}
		// A queued order (shift) waits behind the running assault job, which would otherwise hand over to an endless
		// drafted wait once it expires; end it so the queue is taken now.
		Job curJob = ___pawn.CurJob;
		if (___pawn.jobs.jobQueue.Count > 0 && curJob != null && !curJob.playerForced && ___pawn.jobs.IsCurrentJobPlayerInterruptible())
		{
			___pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
		}
	}
}
