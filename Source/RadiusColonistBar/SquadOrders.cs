using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): run on the able members of a squad on the current map, one pawn at a time;
// a pawn that throws is logged once and the others still get the order.
internal static class SquadOrders
{
	internal static void Draft(int index)
	{
		foreach (Pawn pawn in SquadMembers.Able(Find.CurrentMap, index))
		{
			Try(pawn, delegate
			{
				pawn.drafter.Drafted = true;
			});
		}
	}

	internal static void Undraft(int index)
	{
		foreach (Pawn pawn in SquadMembers.Able(Find.CurrentMap, index))
		{
			Try(pawn, delegate
			{
				pawn.drafter.Drafted = false;
			});
		}
	}

	// All on: switch it off for everyone, which also ends their assault. Otherwise switch it on for everyone.
	internal static void ToggleFireAtWill(int index)
	{
		List<Pawn> list = SquadMembers.Able(Find.CurrentMap, index);
		bool allOn = AllFireAtWill(list);
		foreach (Pawn pawn in list)
		{
			Try(pawn, delegate
			{
				pawn.drafter.FireAtWill = !allOn;
				if (allOn)
				{
					SquadAssault.Get?.Clear(pawn);
				}
			});
		}
	}

	internal static void Regroup(int index)
	{
		List<Pawn> list = SquadMembers.Able(Find.CurrentMap, index);
		if (list.Count == 0)
		{
			return;
		}
		Pawn leader = list[0];
		foreach (Pawn pawn in list)
		{
			Try(pawn, delegate
			{
				pawn.drafter.Drafted = true;
				SquadAssault.Get?.Clear(pawn);
				if (pawn != leader)
				{
					IntVec3 intVec = RCellFinder.BestOrderedGotoDestNear(leader.Position, pawn);
					if (intVec.IsValid)
					{
						Job job = JobMaker.MakeJob(JobDefOf.Goto, intVec);
						job.locomotionUrgency = LocomotionUrgency.Sprint;
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					}
				}
			});
		}
	}

	internal static void Assault(int index, AssaultMode mode)
	{
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault == null)
		{
			return;
		}
		List<Pawn> list = SquadMembers.Able(Find.CurrentMap, index);
		if (list.Count == 0)
		{
			return;
		}
		Find.Selector.ClearSelection();
		foreach (Pawn pawn in list)
		{
			Try(pawn, delegate
			{
				pawn.drafter.Drafted = true;
				pawn.drafter.FireAtWill = true;
				squadAssault.Set(pawn, mode);
				Find.Selector.Select(pawn, playSound: false);
				// End the current job so the think tree picks the assault node right away.
				if (pawn.CurJob != null)
				{
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
				}
			});
		}
	}

	internal static bool AllDrafted(List<Pawn> pawns)
	{
		if (pawns.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < pawns.Count; i++)
		{
			if (!pawns[i].Drafted)
			{
				return false;
			}
		}
		return true;
	}

	internal static bool AllFireAtWill(List<Pawn> pawns)
	{
		if (pawns.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < pawns.Count; i++)
		{
			if (!pawns[i].drafter.FireAtWill)
			{
				return false;
			}
		}
		return true;
	}

	// The mode every member shares, if they all have the same one.
	internal static bool CommonMode(List<Pawn> pawns, out AssaultMode mode)
	{
		mode = AssaultMode.Strongest;
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault == null || pawns.Count == 0 || !squadAssault.TryGetMode(pawns[0], out mode))
		{
			return false;
		}
		for (int i = 1; i < pawns.Count; i++)
		{
			if (!squadAssault.TryGetMode(pawns[i], out var mode2) || mode2 != mode)
			{
				return false;
			}
		}
		return true;
	}

	private static void Try(Pawn pawn, Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Log.ErrorOnce("[Radius UI - Colonist Bar] Squad order failed for " + pawn.LabelShort + ": " + ex, pawn.thingIDNumber ^ 0x52434253);
		}
	}
}
