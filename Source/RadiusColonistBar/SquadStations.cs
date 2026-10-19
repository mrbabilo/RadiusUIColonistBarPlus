using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Battle stations (Colonist Bar Plus): each member of a squad saves a cell per station slot, then runs back to it on
// order. Stations are kept per pawn in BarSquads; a squad only keeps its active slot.
internal static class SquadStations
{
	private static int hoverIndex = -1;

	private static int hoverSlot = -1;

	private static int hoverFrame = -1;

	// The card's member list for the hovered squad: re-sorting it in Update would also make the bar recache there.
	private static List<Pawn> hoverMembers;

	private static readonly List<IntVec3> edges = new List<IntVec3>();

	internal static int CountOnMap(List<Pawn> members, BarSquads squads, int slot, Map map)
	{
		int num = 0;
		for (int i = 0; i < members.Count; i++)
		{
			if (squads.StationOn(members[i], slot, map, out var _))
			{
				num++;
			}
		}
		return num;
	}

	// Every able member takes its current cell, whatever is selected. Absent or downed members keep their old one.
	internal static void Save(int index)
	{
		BarSquads get = BarSquads.Get;
		Map currentMap = Find.CurrentMap;
		if (get == null || currentMap == null)
		{
			return;
		}
		List<Pawn> list = SquadMembers.Able(currentMap, index);
		if (list.Count == 0)
		{
			return;
		}
		int num = get.ActiveStation(index);
		for (int i = 0; i < list.Count; i++)
		{
			get.SetStation(list[i], num, currentMap, list[i].Position);
		}
		SquadStatus.Invalidate();
		Messages.Message("RCB_Stations_Saved".Translate(num + 1, list.Count), MessageTypeDefOf.SilentInput, historical: false);
	}

	internal static void Go(int index)
	{
		BarSquads get = BarSquads.Get;
		Map map = Find.CurrentMap;
		if (get == null || map == null)
		{
			return;
		}
		int slot = get.ActiveStation(index);
		List<Pawn> list = SquadMembers.Able(map, index);
		// The card greys the button in this case; the hotkey must not draft the squad and end its assault for nothing.
		if (CountOnMap(list, get, slot, map) == 0)
		{
			Messages.Message("RCB_Stations_None".Translate(slot + 1, get.SquadName(index)), MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		foreach (Pawn pawn in list)
		{
			SquadOrders.Try(pawn, delegate
			{
				pawn.drafter.Drafted = true;
				SquadAssault.Get?.End(pawn);
				if (get.StationOn(pawn, slot, map, out var cell) && cell.InBounds(map))
				{
					// A wall built on the cell, or a cell walled off since: the nearest free cell instead. Standable first,
					// not BestOrderedGotoDestNear alone: it refuses a cell a squadmate about to leave still stands on.
					IntVec3 c = ((cell.Standable(map) && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly)) ? cell : RCellFinder.BestOrderedGotoDestNear(cell, pawn));
					Job job = JobMaker.MakeJob(JobDefOf.Goto, c);
					job.locomotionUrgency = LocomotionUrgency.Sprint;
					pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
				}
				else
				{
					// A member without a station here stays put: an earlier order of the player (a move) would keep running,
					// since End only stops the assault's own jobs.
					Job curJob = pawn.CurJob;
					if (curJob != null && curJob.playerForced && pawn.jobs.IsCurrentJobPlayerInterruptible())
					{
						pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
					}
				}
			});
		}
		SquadStatus.Invalidate();
	}

	// No undo: clearing wipes the slot for every member on every map, so it asks first. The slot is read now, the
	// dialog is modal, so the squad index cannot shift before the answer.
	internal static void AskClear(int index)
	{
		BarSquads get = BarSquads.Get;
		if (get == null)
		{
			return;
		}
		int slot = get.ActiveStation(index);
		if ((SquadStatus.FreshRoster(index).stationMask & (1 << slot)) == 0)
		{
			return;
		}
		Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("RCB_Stations_ClearConfirm".Translate(slot + 1, get.SquadName(index)), delegate
		{
			BarSquads get2 = BarSquads.Get;
			if (get2 != null)
			{
				get2.ClearSquadStation(index, slot);
				SquadStatus.Invalidate();
				Messages.Message("RCB_Stations_Cleared".Translate(slot + 1), MessageTypeDefOf.SilentInput, historical: false);
			}
		}, destructive: true));
	}

	internal static void Hover(int index, int slot, List<Pawn> members)
	{
		hoverIndex = index;
		hoverSlot = slot;
		hoverMembers = members;
		hoverFrame = Time.frameCount;
	}

	// Called from BarSquads.GameComponentUpdate. The card sets the hover in OnGUI, which runs after Update in a frame:
	// a hover from the previous frame still counts, an older one means the card is gone or the mouse left.
	internal static void DrawHover()
	{
		if (hoverFrame < Time.frameCount - 1 || hoverMembers == null || WorldRendererUtility.WorldSelected)
		{
			return;
		}
		BarSquads get = BarSquads.Get;
		Map currentMap = Find.CurrentMap;
		if (get == null || currentMap == null)
		{
			return;
		}
		// One edge pass for every cell: adjacent stations share an outline, the lines still tell whose cell is whose.
		edges.Clear();
		for (int i = 0; i < hoverMembers.Count; i++)
		{
			Pawn pawn = hoverMembers[i];
			if (pawn.Spawned && pawn.Map == currentMap && get.StationOn(pawn, hoverSlot, currentMap, out var cell) && cell.InBounds(currentMap))
			{
				edges.Add(cell);
				GenDraw.DrawLineBetween(pawn.DrawPos, cell.ToVector3Shifted(), AltitudeLayer.MetaOverlays.AltitudeFor());
			}
		}
		if (edges.Count > 0)
		{
			GenDraw.DrawFieldEdges(edges, get.SquadColor(hoverIndex));
		}
	}
}
