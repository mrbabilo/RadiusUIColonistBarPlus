using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RadiusColonistBar;

// Squad work (Colonist Bar Plus): breaks, a task of one work type for every member, and work priorities, for a whole
// squad. One click is one assignment: nothing is saved and nothing is reapplied later. The task search follows the
// game's own work search (JobGiver_Work): same work giver checks, normal (not forced) jobs, nearest target.
internal static class SquadWork
{
	internal enum Pause
	{
		Eat,
		Sleep,
		Joy,
		Resume
	}

	private static readonly JobGiver_GetFood food = new JobGiver_GetFood();

	private static JobGiver_GetJoy joy;

	private static WorkTypeDef patient;

	private static WorkTypeDef patientBedRest;

	private static bool bound;

	// Thresholds of the vanilla job givers: a pawn above them would be sent for nothing.
	internal static List<Pawn> PauseTargets(List<Pawn> able, Pause pause)
	{
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < able.Count; i++)
		{
			Pawn pawn = able[i];
			bool flag = pause switch
			{
				Pause.Eat => pawn.needs?.food != null && pawn.needs.food.CurLevelPercentage < 0.9f,
				Pause.Sleep => pawn.needs?.rest != null && RestUtility.CanFallAsleep(pawn),
				Pause.Joy => pawn.needs?.joy != null && pawn.needs.joy.CurLevel < 0.99f,
				_ => true,
			};
			if (flag)
			{
				list.Add(pawn);
			}
		}
		return list;
	}

	internal static void DoPause(int index, Pause pause)
	{
		Map currentMap = Find.CurrentMap;
		List<Pawn> list = SquadMembers.Able(currentMap, index);
		if (list.Count == 0)
		{
			Messages.Message("RCB_Orders_NoMember".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		List<Pawn> list2 = PauseTargets(list, pause);
		if (list2.Count == 0)
		{
			Messages.Message(("RCB_Work_" + pause + "None").Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		Bind();
		int sent = 0;
		int none = 0;
		foreach (Pawn pawn in list2)
		{
			SquadOrders.Try(pawn, delegate
			{
				if (TakeBreak(pawn, pause))
				{
					sent++;
				}
				else
				{
					none++;
				}
			});
		}
		SquadStatus.Invalidate();
		string text = ("RCB_Work_" + pause + "Done").Translate(sent).ToString();
		if (none > 0)
		{
			text = text + ", " + ("RCB_Work_" + pause + "Failed").Translate(none).ToString();
		}
		Messages.Message(text, (sent > 0) ? MessageTypeDefOf.SilentInput : MessageTypeDefOf.RejectInput, historical: false);
	}

	// The job is looked for first: a member who finds no food, bed or recreation stays drafted (a raid may be on).
	// Breaks need an undrafted pawn; undrafting also ends the assault.
	private static bool TakeBreak(Pawn pawn, Pause pause)
	{
		Job job = null;
		switch (pause)
		{
		case Pause.Eat:
			job = food.TryIssueJobPackage(pawn, default(JobIssueParams)).Job;
			break;
		case Pause.Sleep:
		{
			Building_Bed building_Bed = RestUtility.FindBedFor(pawn);
			job = ((building_Bed != null) ? JobMaker.MakeJob(JobDefOf.LayDown, building_Bed) : null);
			break;
		}
		case Pause.Joy:
			job = joy.TryIssueJobPackage(pawn, default(JobIssueParams)).Job;
			if (job != null && job.def == JobDefOf.GotoWander)
			{
				job = null;
			}
			break;
		}
		if (pause != Pause.Resume)
		{
			if (job == null)
			{
				return false;
			}
			Undraft(pawn);
			return pawn.jobs.TryTakeOrderedJob(job, JobTag.SatisfyingNeeds);
		}
		Undraft(pawn);
		// Wandering, waiting or having fun stops so the think tree hands out work; eating and sleeping go on.
		// Wait_MaintainPosture is left alone: it is a patient waiting for its doctor.
		JobDef curJobDef = pawn.CurJobDef;
		if (curJobDef != null && (curJobDef == JobDefOf.GotoWander || curJobDef == JobDefOf.Wait_Wander || curJobDef == JobDefOf.Wait || curJobDef.joyKind != null) && pawn.jobs.IsCurrentJobPlayerInterruptible())
		{
			pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
		}
		return true;
	}

	private static void Undraft(Pawn pawn)
	{
		if (pawn.drafter != null && pawn.Drafted)
		{
			pawn.drafter.Drafted = false;
		}
	}

	private static void Bind()
	{
		if (!bound)
		{
			bound = true;
			joy = new JobGiver_GetJoy();
			joy.ResolveReferences();
			patient = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Patient");
			patientBedRest = DefDatabase<WorkTypeDef>.GetNamedSilentFail("PatientBedRest");
		}
	}

	// The Work tab's columns, in its order. "Patient" and "Bed rest" are not tasks to hand out: only for priorities.
	internal static List<WorkTypeDef> Types(bool now)
	{
		Bind();
		List<WorkTypeDef> list = new List<WorkTypeDef>();
		foreach (WorkTypeDef item in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
		{
			if (item.visible && (!now || (item != patient && item != patientBedRest)))
			{
				list.Add(item);
			}
		}
		return list;
	}

	internal static bool Capable(Pawn p, WorkTypeDef work)
	{
		return p.workSettings != null && p.workSettings.EverWork && !p.WorkTypeIsDisabled(work);
	}

	internal static bool Assigned(Pawn p, WorkTypeDef work)
	{
		return Capable(p, work) && p.workSettings.GetPriority(work) > 0;
	}

	// Like vanilla's "Prioritize" menu, a member not assigned to the work type (priority 0) is not sent.
	internal static void WorkNow(int index, WorkTypeDef work)
	{
		List<Pawn> list = SquadMembers.Able(Find.CurrentMap, index);
		if (list.Count == 0)
		{
			Messages.Message("RCB_Orders_NoMember".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		int sent = 0;
		int nothing = 0;
		int unassigned = 0;
		int incapable = 0;
		foreach (Pawn pawn in list)
		{
			if (!Capable(pawn, work))
			{
				incapable++;
				continue;
			}
			if (!Assigned(pawn, work))
			{
				unassigned++;
				continue;
			}
			if (pawn.CurJob?.workGiverDef?.workType == work)
			{
				sent++;
				continue;
			}
			SquadOrders.Try(pawn, delegate
			{
				Job job = FindJob(pawn, work, out var giver, out var cell);
				if (job == null)
				{
					nothing++;
					return;
				}
				Undraft(pawn);
				job.workGiverDef = giver.def;
				if (pawn.jobs.TryTakeOrderedJobPrioritizedWork(job, giver, cell))
				{
					sent++;
				}
				else
				{
					nothing++;
				}
			});
		}
		SquadStatus.Invalidate();
		string text = "RCB_Work_NowDone".Translate(work.labelShort.CapitalizeFirst(), sent).ToString();
		if (nothing > 0)
		{
			text = text + ", " + "RCB_Work_NowNothing".Translate(nothing).ToString();
		}
		if (unassigned > 0)
		{
			text = text + ", " + "RCB_Work_NowUnassigned".Translate(unassigned).ToString();
		}
		if (incapable > 0)
		{
			text = text + ", " + "RCB_Work_NowIncapable".Translate(incapable).ToString();
		}
		Messages.Message(text, (sent > 0) ? MessageTypeDefOf.SilentInput : MessageTypeDefOf.RejectInput, historical: false);
	}

	// The game's work search narrowed to one work type: work givers by priorityInType, the first level with a target
	// wins, and inside a level the nearest target. Normal jobs (forced: false), so allowed areas and limits hold.
	private static Job FindJob(Pawn pawn, WorkTypeDef work, out WorkGiver giver, out IntVec3 cell)
	{
		giver = null;
		cell = pawn.Position;
		List<WorkGiverDef> workGiversByPriority = work.workGiversByPriority;
		int num = int.MinValue;
		TargetInfo targetInfo = TargetInfo.Invalid;
		WorkGiver_Scanner workGiver_Scanner = null;
		float num2 = float.MaxValue;
		for (int i = 0; i < workGiversByPriority.Count; i++)
		{
			WorkGiverDef workGiverDef = workGiversByPriority[i];
			if (targetInfo.IsValid && workGiverDef.priorityInType != num)
			{
				break;
			}
			num = workGiverDef.priorityInType;
			WorkGiver worker = workGiverDef.Worker;
			// One work giver at a time, as JobGiver_Work does: a modded giver that throws only loses its own targets.
			try
			{
				if (!CanUse(pawn, worker))
				{
					continue;
				}
				Job job = worker.NonScanJob(pawn);
				if (job != null)
				{
					giver = worker;
					return job;
				}
				if (!(worker is WorkGiver_Scanner workGiver_Scanner2))
				{
					continue;
				}
				if (workGiver_Scanner2.def.scanThings)
				{
					Thing thing = BestThing(pawn, workGiver_Scanner2);
					if (thing != null)
					{
						float num3 = (thing.Position - pawn.Position).LengthHorizontalSquared;
						if (num3 < num2)
						{
							targetInfo = thing;
							workGiver_Scanner = workGiver_Scanner2;
							num2 = num3;
						}
					}
				}
				if (workGiver_Scanner2.def.scanCells)
				{
					IntVec3 intVec = BestCell(pawn, workGiver_Scanner2);
					if (intVec.IsValid)
					{
						float num4 = (intVec - pawn.Position).LengthHorizontalSquared;
						if (num4 < num2)
						{
							targetInfo = new TargetInfo(intVec, pawn.Map);
							workGiver_Scanner = workGiver_Scanner2;
							num2 = num4;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.ErrorOnce("[Radius UI - Colonist Bar] Squad work search failed in " + workGiverDef.defName + ": " + ex, workGiverDef.shortHash ^ 0x52435757);
			}
		}
		if (!targetInfo.IsValid)
		{
			return null;
		}
		giver = workGiver_Scanner;
		cell = targetInfo.Cell;
		return targetInfo.HasThing ? workGiver_Scanner.JobOnThing(pawn, targetInfo.Thing) : workGiver_Scanner.JobOnCell(pawn, targetInfo.Cell);
	}

	// JobGiver_Work.PawnCanUseWorkGiver, every test.
	private static bool CanUse(Pawn pawn, WorkGiver giver)
	{
		if (!giver.def.nonColonistsCanDo && !pawn.IsColonist && !pawn.IsColonyMech && !pawn.IsColonySubhuman)
		{
			return false;
		}
		if (pawn.WorkTagIsDisabled(giver.def.workTags))
		{
			return false;
		}
		if (giver.def.workType != null && pawn.WorkTypeIsDisabled(giver.def.workType))
		{
			return false;
		}
		if (giver.ShouldSkip(pawn))
		{
			return false;
		}
		if (giver.MissingRequiredCapacity(pawn) != null)
		{
			return false;
		}
		if (pawn.RaceProps.IsMechanoid && !giver.def.canBeDoneByMechs)
		{
			return false;
		}
		return true;
	}

	private static Thing BestThing(Pawn pawn, WorkGiver_Scanner scanner)
	{
		Predicate<Thing> validator = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
		IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);
		TraverseParms traverseParams = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn));
		if (scanner.Prioritized)
		{
			IEnumerable<Thing> enumerable2 = enumerable ?? pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
			return scanner.AllowUnreachable ? GenClosest.ClosestThing_Global(pawn.Position, enumerable2, 99999f, validator, (Thing x) => scanner.GetPriority(pawn, x)) : GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, enumerable2, scanner.PathEndMode, traverseParams, 9999f, validator, (Thing x) => scanner.GetPriority(pawn, x));
		}
		if (scanner.AllowUnreachable)
		{
			IEnumerable<Thing> searchSet = enumerable ?? pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
			return GenClosest.ClosestThing_Global(pawn.Position, searchSet, 99999f, validator);
		}
		return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, traverseParams, 9999f, validator, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null);
	}

	private static IntVec3 BestCell(Pawn pawn, WorkGiver_Scanner scanner)
	{
		IntVec3 result = IntVec3.Invalid;
		IntVec3 position = pawn.Position;
		float num = float.MaxValue;
		float num2 = float.MinValue;
		bool prioritized = scanner.Prioritized;
		bool allowUnreachable = scanner.AllowUnreachable;
		Danger maxDanger = scanner.MaxPathDanger(pawn);
		foreach (IntVec3 item in scanner.PotentialWorkCellsGlobal(pawn))
		{
			float num3 = (item - position).LengthHorizontalSquared;
			if (!prioritized && num3 >= num)
			{
				continue;
			}
			if (item.IsForbidden(pawn) || !scanner.HasJobOnCell(pawn, item) || (!allowUnreachable && !pawn.CanReach(item, scanner.PathEndMode, maxDanger)))
			{
				continue;
			}
			float num4 = (prioritized ? scanner.GetPriority(pawn, item) : 0f);
			if (!prioritized || num4 > num2 || (num4 == num2 && num3 < num))
			{
				result = item;
				num = num3;
				num2 = num4;
			}
		}
		return result;
	}

	// The real priorities of the members, shared or mixed; -1 when nobody can do this work.
	internal static int SharedPriority(List<Pawn> members, WorkTypeDef work, out bool mixed, out int capable)
	{
		mixed = false;
		capable = 0;
		int num = -1;
		for (int i = 0; i < members.Count; i++)
		{
			if (!Capable(members[i], work))
			{
				continue;
			}
			capable++;
			int priority = members[i].workSettings.GetPriority(work);
			if (num < 0)
			{
				num = priority;
			}
			else if (priority != num)
			{
				mixed = true;
			}
		}
		return num;
	}

	// The vanilla work box for the whole squad: left click (dir -1) lowers the number, right click raises it, wrapping
	// like the Work tab; without manual priorities it toggles. A mixed value starts from the first capable member's.
	internal static void ShiftPriority(int index, WorkTypeDef work, int dir)
	{
		List<Pawn> list = SquadRules.Members(index);
		bool mixed;
		int capable;
		int num = SharedPriority(list, work, out mixed, out capable);
		if (num < 0)
		{
			return;
		}
		bool useWorkPriorities = Find.PlaySettings.useWorkPriorities;
		int num2;
		if (useWorkPriorities)
		{
			num2 = num + dir;
			if (num2 < 0)
			{
				num2 = 4;
			}
			if (num2 > 4)
			{
				num2 = 0;
			}
		}
		else
		{
			num2 = ((num > 0 && !mixed) ? 0 : 3);
		}
		Pawn pawn = null;
		foreach (Pawn item in list)
		{
			if (!Capable(item, work))
			{
				continue;
			}
			Pawn p = item;
			SquadOrders.Try(p, delegate
			{
				bool flag = p.workSettings.GetPriority(work) == 0;
				p.workSettings.SetPriority(work, num2);
				if (flag && num2 > 0 && pawn == null && p.Ideo != null && p.Ideo.IsWorkTypeConsideredDangerous(work))
				{
					pawn = p;
				}
			});
		}
		if (pawn != null)
		{
			Messages.Message("MessageIdeoOpposedWorkTypeSelected".Translate(pawn, work.gerundLabel), pawn, MessageTypeDefOf.CautionInput, historical: false);
		}
		(useWorkPriorities ? SoundDefOf.DragSlider : ((num2 > 0) ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff)).PlayOneShotOnCamera();
		SquadStatus.Invalidate();
	}

	// A setting of the whole colony, as the Work tab's checkbox: every player pawn is told.
	internal static void SetManual(bool on)
	{
		Find.PlaySettings.useWorkPriorities = on;
		foreach (Pawn item in PawnsFinder.AllMapsWorldAndTemporary_Alive)
		{
			if (item.Faction == Faction.OfPlayer && item.workSettings != null)
			{
				item.workSettings.Notify_UseWorkPrioritiesChanged();
			}
		}
		SquadStatus.Invalidate();
	}

	// The leader of Regroup (first able member in bar order), else the first living member.
	internal static Pawn Leader(int index)
	{
		List<Pawn> list = SquadMembers.Able(Find.CurrentMap, index);
		if (list.Count > 0)
		{
			return list[0];
		}
		List<Pawn> list2 = SquadRules.Members(index);
		return (list2.Count > 0) ? list2[0] : null;
	}

	// Only the types the leader can do: a disabled type reads 0 and would switch that work off for everyone else.
	internal static void CopyPriorities(int index)
	{
		Pawn leader = Leader(index);
		if (leader?.workSettings == null || !leader.workSettings.EverWork)
		{
			return;
		}
		int num = 0;
		foreach (Pawn item in SquadRules.Members(index))
		{
			if (item == leader || item.workSettings == null || !item.workSettings.EverWork)
			{
				continue;
			}
			Pawn p = item;
			SquadOrders.Try(p, delegate
			{
				foreach (WorkTypeDef allDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
				{
					if (Capable(leader, allDef) && Capable(p, allDef))
					{
						p.workSettings.SetPriority(allDef, leader.workSettings.GetPriority(allDef));
					}
				}
			});
			num++;
		}
		SquadStatus.Invalidate();
		Messages.Message("RCB_Work_CopiedPriorities".Translate(leader.LabelShortCap, num), MessageTypeDefOf.SilentInput, historical: false);
	}

	internal static void CopyTimetable(int index)
	{
		Pawn leader = Leader(index);
		if (leader?.timetable == null)
		{
			return;
		}
		int num = 0;
		foreach (Pawn item in SquadRules.Members(index))
		{
			if (item == leader || item.timetable == null)
			{
				continue;
			}
			Pawn p = item;
			SquadOrders.Try(p, delegate
			{
				for (int i = 0; i < 24; i++)
				{
					p.timetable.SetAssignment(i, leader.timetable.GetAssignment(i));
				}
			});
			num++;
		}
		SquadStatus.Invalidate();
		Messages.Message("RCB_Work_CopiedTimetable".Translate(leader.LabelShortCap, num), MessageTypeDefOf.SilentInput, historical: false);
	}
}
