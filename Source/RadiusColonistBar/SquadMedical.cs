using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Medical orders (Colonist Bar Plus): the able members of a squad rescue the downed, tend the wounded, or go to bed
// until healed. Vanilla jobs only, taken as player orders: nothing is saved. Patients are the colony's colonists and
// slaves on the current map, squad members or not; each patient gets the nearest free member, one patient each.
internal static class SquadMedical
{
	internal enum Kind
	{
		Rescue,
		Tend,
		Bed
	}

	private static int hoverIndex = -1;

	private static int hoverFrame = -1;

	private static List<Pawn> hoverPawns;

	private static readonly List<IntVec3> edges = new List<IntVec3>();

	// Bed slots handed out during one click: a job queued behind an uninterruptible one has not reserved its bed yet.
	private static readonly Dictionary<Building_Bed, int> bedsGiven = new Dictionary<Building_Bed, int>();

	// Cheap enough for every frame: no path or bed search. The click checks everything again.
	internal static List<Pawn> Targets(Map map, int index, Kind kind)
	{
		List<Pawn> list = new List<Pawn>();
		BarSquads get = BarSquads.Get;
		if (map == null || get == null || index < 0)
		{
			return list;
		}
		if (kind == Kind.Bed)
		{
			return ToRest(SquadMembers.Able(map, index));
		}
		AddPatients(list, map.mapPawns.FreeColonistsSpawned, kind);
		AddPatients(list, map.mapPawns.SlavesOfColonySpawned, kind);
		list.Sort((Pawn a, Pawn b) => Compare(a, b, get, index));
		return list;
	}

	// The members who need medical rest, from the able members the card already has.
	internal static List<Pawn> ToRest(List<Pawn> members)
	{
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < members.Count; i++)
		{
			if (HealthAIUtility.ShouldSeekMedicalRest(members[i]))
			{
				list.Add(members[i]);
			}
		}
		return list;
	}

	private static void AddPatients(List<Pawn> into, List<Pawn> from, Kind kind)
	{
		for (int i = 0; i < from.Count; i++)
		{
			Pawn pawn = from[i];
			if (!into.Contains(pawn) && ((kind == Kind.Rescue) ? WantsRescue(pawn) : (PawnUiCache.Get(pawn).needsTend && !pawn.InAggroMentalState)))
			{
				into.Add(pawn);
			}
		}
	}

	// Babies are always downed: the alert band leaves them out, and vanilla hands them to childcare, not to rescue.
	internal static bool WantsRescue(Pawn p)
	{
		return HealthAIUtility.WantsToBeRescued(p) && !PawnStatusUtil.DownedIsIncidental(p) && (p.playerSettings == null || p.playerSettings.medCare != MedicalCareCategory.NoCare);
	}

	// The same "bleeding out" as the alert band, so the band's first name is the first patient.
	private static bool BleedingOut(Pawn p)
	{
		return p.health != null && p.health.hediffSet.BleedRateTotal > 0.01f && HealthUtility.TicksUntilDeathDueToBloodLoss(p) < 60000;
	}

	private static int Compare(Pawn a, Pawn b, BarSquads squads, int index)
	{
		int num = (BleedingOut(a) ? 0 : 1).CompareTo(BleedingOut(b) ? 0 : 1);
		if (num != 0)
		{
			return num;
		}
		num = ((squads.SquadIndexOf(a) == index) ? 0 : 1).CompareTo((squads.SquadIndexOf(b) == index) ? 0 : 1);
		if (num != 0)
		{
			return num;
		}
		num = HealthUtility.TicksUntilDeathDueToBloodLoss(a).CompareTo(HealthUtility.TicksUntilDeathDueToBloodLoss(b));
		if (num != 0)
		{
			return num;
		}
		return a.thingIDNumber.CompareTo(b.thingIDNumber);
	}

	// The alert band's words for the patient's state, in the card's tooltips.
	internal static string Condition(Pawn p, Kind kind)
	{
		if (BleedingOut(p))
		{
			return "RCB_Cond_BleedingOut".Translate();
		}
		return ((kind == Kind.Rescue) ? "RCB_Cond_Downed" : "RCB_Cond_NeedsTend").Translate();
	}

	internal static bool CanTend(Pawn p)
	{
		return !p.WorkTypeIsDisabled(WorkTypeDefOf.Doctor) && Hands(p);
	}

	internal static bool AnyDoctor(List<Pawn> members)
	{
		for (int i = 0; i < members.Count; i++)
		{
			if (CanTend(members[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Hands(Pawn p)
	{
		return p.health?.capacities != null && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
	}

	// Members already rescuing or tending keep their patient: a second click does not reshuffle them.
	private static List<Pawn> Free(Map map, int index)
	{
		List<Pawn> list = SquadMembers.Able(map, index);
		list.RemoveAll((Pawn p) => p.CurJobDef == JobDefOf.Rescue || p.CurJobDef == JobDefOf.TendPatient);
		return list;
	}

	private static Pawn Nearest(List<Pawn> free, Pawn patient, System.Predicate<Pawn> can)
	{
		Pawn result = null;
		float num = float.MaxValue;
		for (int i = 0; i < free.Count; i++)
		{
			Pawn pawn = free[i];
			float num2 = pawn.Position.DistanceToSquared(patient.Position);
			if (num2 < num && can(pawn))
			{
				result = pawn;
				num = num2;
			}
		}
		return result;
	}

	internal static void Rescue(int index)
	{
		Map map = Find.CurrentMap;
		List<Pawn> list = Targets(map, index, Kind.Rescue);
		if (Refused(map, index, list, "RCB_Medical_NoDowned"))
		{
			return;
		}
		List<Pawn> free = Free(map, index);
		bedsGiven.Clear();
		int num = 0;
		string noBed = null;
		foreach (Pawn patient in list)
		{
			bool lacked = false;
			Pawn pawn = Nearest(free, patient, delegate(Pawn r)
			{
				// forced skips the enemy and forbidden checks, but also reservations: someone may already be carrying
				// this patient (a colonist's own rescue job), so the reservation is checked apart.
				if (r == patient || !Hands(r) || !HealthAIUtility.CanRescueNow(r, patient, forced: true) || !r.CanReserve(patient))
				{
					return false;
				}
				if (FindBed(patient, r) == null)
				{
					lacked = true;
					return false;
				}
				return true;
			});
			// Searched again for the chosen rescuer: a farther candidate tested later may have found another bed.
			Building_Bed taken = ((pawn != null) ? FindBed(patient, pawn) : null);
			if (taken == null)
			{
				if (lacked)
				{
					noBed = noBed ?? (patient.IsSlaveOfColony ? "NoSlaveBed" : "NoNonPrisonerBed");
				}
				continue;
			}
			free.Remove(pawn);
			Give(taken);
			SquadOrders.Try(pawn, delegate
			{
				SquadAssault.Get?.End(pawn);
				Job job = JobMaker.MakeJob(JobDefOf.Rescue, patient, taken);
				job.count = 1;
				if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
				{
					num++;
				}
			});
		}
		Done(num, "RCB_Medical_RescueDone", noBed, rescue: true);
	}

	internal static void Tend(int index)
	{
		Map map = Find.CurrentMap;
		List<Pawn> list = Targets(map, index, Kind.Tend);
		list.RemoveAll((Pawn p) => !HealthAIUtility.ShouldBeTendedNowByPlayer(p));
		if (Refused(map, index, list, "RCB_Medical_NoWounded"))
		{
			return;
		}
		List<Pawn> free = Free(map, index);
		if (!AnyDoctor(free))
		{
			Messages.Message("RCB_Medical_NoDoctor".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		int num = 0;
		foreach (Pawn patient in list)
		{
			Pawn pawn = Nearest(free, patient, (Pawn d) => CanTend(d) && ((d == patient) ? (d.playerSettings == null || d.playerSettings.selfTend) : d.CanReserveAndReach(patient, PathEndMode.ClosestTouch, Danger.Deadly)));
			if (pawn == null)
			{
				continue;
			}
			free.Remove(pawn);
			SquadOrders.Try(pawn, delegate
			{
				SquadAssault.Get?.End(pawn);
				// Built like WorkGiver_Tend: medicine carried by someone else is fetched from its carrier (target C).
				Thing thing = HealthAIUtility.FindBestMedicine(pawn, patient);
				Job job = ((thing != null && thing.SpawnedParentOrMe != thing) ? JobMaker.MakeJob(JobDefOf.TendPatient, patient, thing, thing.SpawnedParentOrMe) : JobMaker.MakeJob(JobDefOf.TendPatient, patient, thing));
				job.count = 1;
				// As for vanilla's drafted tend: a patient on its feet waits for the doctor instead of walking off. Not for
				// a patient in bed: the forced wait would replace its bed rest.
				job.draftedTend = !patient.InBed();
				if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
				{
					num++;
				}
			});
		}
		Done(num, "RCB_Medical_TendDone", null, rescue: false);
	}

	internal static void ToBed(int index)
	{
		Map map = Find.CurrentMap;
		List<Pawn> list = Targets(map, index, Kind.Bed);
		if (Refused(map, index, list, "RCB_Medical_NoRest"))
		{
			return;
		}
		bedsGiven.Clear();
		int num = 0;
		string noBed = null;
		foreach (Pawn pawn in list)
		{
			Building_Bed bed = FindBed(pawn, pawn);
			if (bed == null)
			{
				noBed = noBed ?? (pawn.IsSlaveOfColony ? "NoSlaveBed" : "NoNonPrisonerBed");
				continue;
			}
			Give(bed);
			SquadOrders.Try(pawn, delegate
			{
				// Vanilla refuses bed rest to a drafted pawn; undrafting also ends the assault.
				pawn.drafter.Drafted = false;
				Job job = JobMaker.MakeJob(JobDefOf.LayDown, bed);
				job.restUntilHealed = true;
				if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
				{
					num++;
				}
			});
		}
		Done(num, "RCB_Medical_BedDone", noBed, rescue: false);
	}

	// The card greys the button in these cases; the hotkey says why instead.
	private static bool Refused(Map map, int index, List<Pawn> targets, string noneKey)
	{
		if (map == null || BarSquads.Get == null)
		{
			return true;
		}
		if (SquadMembers.Able(map, index).Count == 0)
		{
			Messages.Message("RCB_Orders_NoMember".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return true;
		}
		if (targets.Count == 0)
		{
			Messages.Message(noneKey.Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return true;
		}
		return false;
	}

	private static void Done(int count, string doneKey, string noBedKey, bool rescue)
	{
		SquadStatus.Invalidate();
		if (count > 0)
		{
			Messages.Message(doneKey.Translate(count), MessageTypeDefOf.SilentInput, historical: false);
		}
		else if (noBedKey == null)
		{
			// Every member busy or every patient already taken: say so rather than nothing.
			Messages.Message("RCB_Medical_NoneFree".Translate(), MessageTypeDefOf.RejectInput, historical: false);
		}
		if (noBedKey != null)
		{
			string text = noBedKey.Translate().CapitalizeFirst();
			Messages.Message(rescue ? ("CannotRescue".Translate() + ": " + text) : text, MessageTypeDefOf.RejectInput, historical: false);
		}
	}

	// Like vanilla's rescue: free beds first, then beds reserved by someone else. A slave goes to a slave bed, as with
	// vanilla's "return to slave bed". A bed already handed out to all its slots in this click is refused.
	private static Building_Bed FindBed(Pawn sleeper, Pawn carrier)
	{
		Building_Bed building_Bed = FindBedFor(sleeper, carrier);
		if (building_Bed == null || Full(building_Bed))
		{
			GuestStatus? guestStatus = (sleeper.IsSlaveOfColony ? new GuestStatus?(GuestStatus.Slave) : null);
			building_Bed = RestUtility.FindBedFor(sleeper, carrier, checkSocialProperness: false, ignoreOtherReservations: true, guestStatus);
		}
		return (building_Bed == null || Full(building_Bed)) ? null : building_Bed;
	}

	private static Building_Bed FindBedFor(Pawn sleeper, Pawn carrier)
	{
		GuestStatus? guestStatus = (sleeper.IsSlaveOfColony ? new GuestStatus?(GuestStatus.Slave) : null);
		return RestUtility.FindBedFor(sleeper, carrier, checkSocialProperness: false, ignoreOtherReservations: false, guestStatus);
	}

	private static bool Full(Building_Bed bed)
	{
		return bedsGiven.TryGetValue(bed, out var value) && value >= bed.SleepingSlotsCount;
	}

	private static void Give(Building_Bed bed)
	{
		bedsGiven.TryGetValue(bed, out var value);
		bedsGiven[bed] = value + 1;
	}

	internal static void Hover(int index, List<Pawn> pawns)
	{
		hoverIndex = index;
		hoverPawns = pawns;
		hoverFrame = Time.frameCount;
	}

	// Called from BarSquads.GameComponentUpdate, like the battle station preview: the card sets the hover in OnGUI,
	// so a hover from the previous frame still counts and an older one means the mouse left.
	internal static void DrawHover()
	{
		if (hoverFrame < Time.frameCount - 1 || hoverPawns == null || WorldRendererUtility.WorldSelected)
		{
			return;
		}
		BarSquads get = BarSquads.Get;
		Map currentMap = Find.CurrentMap;
		if (get == null || currentMap == null)
		{
			return;
		}
		edges.Clear();
		for (int i = 0; i < hoverPawns.Count; i++)
		{
			Pawn pawn = hoverPawns[i];
			if (pawn.Spawned && pawn.Map == currentMap)
			{
				edges.Add(pawn.Position);
			}
		}
		if (edges.Count > 0)
		{
			GenDraw.DrawFieldEdges(edges, get.SquadColor(hoverIndex));
		}
	}
}
