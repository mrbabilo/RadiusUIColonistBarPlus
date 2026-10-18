using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RadiusColonistBar;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RCBPlusTests;

// Checks for the other batches of the README: orders, assault, battle stations, care, rules, members, hotkeys and a
// save round trip. Same rules as the Work tab checks: the order card's handlers are called directly, by reflection.
internal partial class Checks
{
	private static readonly Type stationsType = AccessTools.TypeByName("RadiusColonistBar.SquadStations");

	private static readonly Type medicalType = AccessTools.TypeByName("RadiusColonistBar.SquadMedical");

	private static readonly Type rulesType = AccessTools.TypeByName("RadiusColonistBar.SquadRules");

	private static readonly Type membersType = AccessTools.TypeByName("RadiusColonistBar.SquadMembers");

	private static readonly Type hotkeysType = AccessTools.TypeByName("RadiusColonistBar.SquadHotkeys");

	private static readonly Type uiCacheType = AccessTools.TypeByName("RadiusColonistBar.PawnUiCache");

	private static readonly Type medKind = AccessTools.Inner(medicalType, "Kind");

	private static readonly Type ruleEnum = AccessTools.Inner(rulesType, "Rule");

	private const string SaveName = "RCBPlusTests-roundtrip";

	// What the round trip expects back, by thingIDNumber: the Pawn objects do not survive the load.
	private static class Saved
	{
		internal static List<string> names;

		internal static List<int> active;

		internal static Dictionary<int, int> squadOf = new Dictionary<int, int>();

		internal static Dictionary<int, IntVec3> stationCell = new Dictionary<int, IntVec3>();

		internal static int stationSlot;

		internal static int renamed;

		internal static Dictionary<int, AssaultMode> modes = new Dictionary<int, AssaultMode>();

		internal static int mapId;
	}

	internal void RunSquadChecks()
	{
		Run("O1 Draft/Undraft: able members only, other squad untouched", CheckDraft);
		Run("O2 Fire at will: all on toggles off, partial toggles on", CheckFireAtWill);
		Run("O3 Regroup: drafted, assault ended, members sprint to the leader", CheckRegroup);
		Run("O4 Assault, 3 modes: drafted, fire at will, selected, assault giver, target by mode", CheckAssaultModes);
		Run("O5 Stop assault: modes cleared, members stay drafted", CheckStopAssault);
		Run("O6 Assault ends on player order, undraft, hold fire (attack interrupted)", CheckAssaultEnds);
		Run("O7 Assault: a member incapable of violence, armed (by the test), gets no attack job", delegate(List<string> n)
		{
			CheckAssaultPacifist(n, armedPacifist: true);
		});
		Run("O7b Assault: a member incapable of violence, unarmed (reachable in play), gets no attack job", delegate(List<string> n)
		{
			CheckAssaultPacifist(n, armedPacifist: false);
		});
		Run("S1 Stations Save: able members take their cell, downed member keeps its old one", CheckStationSave);
		Run("S2 Stations Go: walled cell replaced, member without station stays drafted, assault ended", CheckStationGo);
		Run("S3 Stations active slot, refusal without station, AskClear with confirmation", CheckStationSlotsAndClear);
		Run("M1 Rescue: bleeding first, then members, nearest free member, one patient each, medical bed", CheckRescue);
		Run("M2 Tend: doctors only, best allowed medicine, standing patient waits", CheckTend);
		Run("M3 To bed: wounded member undrafted, rests in a medical bed until healed", CheckToBed);
		Run("R1 Rules: six rules set for the squad, caravan member included, Mixed detected", CheckRules);
		Run("R2 Rules: area only on the current map, no Attack for the incapable of violence", CheckRulesAreaHostility);
		Run("R3 Rules: release forced apparel", CheckReleaseForced);
		Run("B1 Members: add selection, remove selection, select all", CheckMembers);
	}

	// ---- calls into the mod ----

	private List<Pawn> Able(int squad)
	{
		return (List<Pawn>)Call(membersType, "Able", map, squad);
	}

	private void Order(string method, int squad)
	{
		Call(ordersType, method, squad);
	}

	private int StationMask(int squad)
	{
		object state = Call(statusType, "Fresh", squad);
		return (int)AccessTools.Field(state.GetType(), "stationMask").GetValue(state);
	}

	private List<Pawn> MedTargets(int squad, string kind)
	{
		return (List<Pawn>)Call(medicalType, "Targets", map, squad, Enum.Parse(medKind, kind));
	}

	private static AssaultMode? ModeOf(Pawn p)
	{
		return SquadAssault.Get.TryGetMode(p, out var mode) ? new AssaultMode?(mode) : null;
	}

	private static bool FromAssaultGiver(Pawn p)
	{
		return p.CurJob?.jobGiver is JobGiver_SquadAssault;
	}

	private bool Said(string text)
	{
		return Boot.Messages.Contains(text);
	}

	// ---- scene helpers ----

	private void Move(Pawn p, IntVec3 c)
	{
		p.Position = c;
		p.Notify_Teleported();
	}

	private static void Down(Pawn p)
	{
		Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.Anesthetic, p);
		hediff.Severity = 1f;
		p.health.AddHediff(hediff);
	}

	private static void Cut(Pawn p, BodyPartRecord part, float severity)
	{
		Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.Cut, p, part);
		hediff.Severity = severity;
		p.health.AddHediff(hediff, part);
	}

	// One small cut on a leg: needs tending, does not down the pawn.
	private static void Wound(Pawn p)
	{
		Cut(p, p.health.hediffSet.GetNotMissingParts().First((BodyPartRecord x) => x.def == BodyPartDefOf.Leg), 3f);
	}

	private bool BleedingOut(Pawn p)
	{
		return (bool)AccessTools.Method(medicalType, "BleedingOut").Invoke(null, new object[1] { p });
	}

	// Cuts on the torso and the limbs until the mod's own "bleeding out" test says so.
	private void Bleed(Pawn p)
	{
		List<BodyPartRecord> parts = p.health.hediffSet.GetNotMissingParts().Where((BodyPartRecord x) => x.def == BodyPartDefOf.Torso || x.def == BodyPartDefOf.Leg || x.def == BodyPartDefOf.Arm).ToList();
		for (int i = 0; i < parts.Count * 2 && !BleedingOut(p); i++)
		{
			Cut(p, parts[i % parts.Count], 7f);
		}
	}

	// The bar's per-pawn snapshot (needsTend) refreshes every few frames; drop it so the next read is fresh.
	private void FreshUi(Pawn p)
	{
		((IDictionary)AccessTools.Field(uiCacheType, "snaps").GetValue(null)).Remove(p.thingIDNumber);
	}

	private Building_Bed Bed(bool medical)
	{
		Building_Bed building_Bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
		building_Bed.SetFaction(Faction.OfPlayer);
		GenSpawn.Spawn(building_Bed, FreeCell(8), map, Rot4.North, WipeMode.Vanish);
		building_Bed.Medical = medical;
		return building_Bed;
	}

	private static void Gun(Pawn p)
	{
		if (p.equipment.Primary == null)
		{
			p.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(ThingDef.Named("Gun_AssaultRifle")));
		}
	}

	private static void Gone(IEnumerable<Thing> things)
	{
		foreach (Thing thing in things)
		{
			if (thing != null && !thing.Destroyed)
			{
				thing.Destroy();
			}
		}
	}

	private Caravan ToCaravan(Pawn p)
	{
		return CaravanMaker.MakeCaravan(new Pawn[1] { p }, Faction.OfPlayer, map.Tile, addToWorldPawnsIfNotAlready: true);
	}

	// A pawn just spawned has no job until its first tick, and the checks run paused: give it the idle job a real
	// colonist would have, so orders that end the current job start the next one at once.
	private static void Idle(IEnumerable<Pawn> pawns)
	{
		foreach (Pawn pawn in pawns)
		{
			pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_Wander), JobCondition.InterruptForced);
		}
	}

	// Colonists of earlier checks or of the start may carry tendable conditions of their own (generated with them):
	// named in a note, then tended, so the care checks only see their own patients.
	private void TendLeftovers(List<string> notes)
	{
		foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned.ToList())
		{
			List<Hediff> list = pawn.health.hediffSet.hediffs.Where((Hediff h) => h.TendableNow()).ToList();
			if (list.Count > 0 || pawn.Downed)
			{
				notes.Add("note: leftover " + pawn.LabelShort + (pawn.Downed ? " downed" : "") + " tendable: " + string.Join(",", list.Select((Hediff h) => h.def.defName)));
				foreach (Hediff item in list)
				{
					item.Tended(1f, 1f);
				}
				FreshUi(pawn);
			}
		}
	}

	// A colonist able to fight: generated colonists are now and then incapable of violence, which the assault and
	// hostility checks must not get by chance.
	private Pawn Fighter()
	{
		for (int i = 0; i < 40; i++)
		{
			Pawn pawn = NewColonist();
			if (!pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				return pawn;
			}
			pawn.Destroy();
		}
		throw new Exception("no colonist able to fight generated");
	}

	private static string Ids(IEnumerable<Pawn> pawns)
	{
		return string.Join(",", pawns.Select((Pawn p) => p.LabelShort));
	}

	// ---- orders ----

	private void CheckDraft(List<string> notes)
	{
		Pawn a = NewColonist();
		Pawn b = NewColonist();
		Pawn downed = NewColonist();
		Pawn other = NewColonist();
		Down(downed);
		int squad = Squad(a, b, downed);
		Squad(other);
		Expect(notes, downed.Downed, "setup: one member downed");
		Order("Draft", squad);
		Expect(notes, a.Drafted && b.Drafted, "able members drafted");
		Expect(notes, !downed.Drafted, "downed member left alone");
		Expect(notes, !other.Drafted, "member of another squad left alone");
		other.drafter.Drafted = true;
		Order("Undraft", squad);
		Expect(notes, !a.Drafted && !b.Drafted, "Undraft: members undrafted");
		Expect(notes, other.Drafted, "Undraft: other squad stays drafted");
		other.drafter.Drafted = false;
		Gone(new Thing[1] { downed });
	}

	private void CheckFireAtWill(List<string> notes)
	{
		Pawn[] array = new Pawn[3]
		{
			NewColonist(),
			NewColonist(),
			NewColonist()
		};
		int squad = Squad(array);
		Order("Draft", squad);
		Expect(notes, array.All((Pawn p) => p.drafter.FireAtWill), "setup: fire at will on for all after drafting");
		Order("ToggleFireAtWill", squad);
		Expect(notes, array.All((Pawn p) => !p.drafter.FireAtWill), "all on, toggle: off for all");
		Order("ToggleFireAtWill", squad);
		Expect(notes, array.All((Pawn p) => p.drafter.FireAtWill), "all off, toggle: on for all");
		array[1].drafter.FireAtWill = false;
		Order("ToggleFireAtWill", squad);
		Expect(notes, array.All((Pawn p) => p.drafter.FireAtWill), "partial, toggle: on for all");
		Order("Undraft", squad);
	}

	private void CheckRegroup(List<string> notes)
	{
		Pawn[] array = new Pawn[3]
		{
			NewColonist(),
			NewColonist(),
			NewColonist()
		};
		int squad = Squad(array);
		Call(ordersType, "Assault", squad, AssaultMode.Strongest);
		Pawn leader = Able(squad)[0];
		foreach (Pawn p in array.Where((Pawn x) => x != leader))
		{
			Move(p, FreeCell(14));
		}
		Order("Regroup", squad);
		notes.Add("leader " + leader.LabelShort + "; jobs: " + string.Join(", ", array.Select(JobOf)));
		Expect(notes, array.All((Pawn p) => p.Drafted), "all drafted");
		Expect(notes, array.All((Pawn p) => ModeOf(p) == null), "assault ended for all, leader included");
		Expect(notes, !FromAssaultGiver(leader), "leader's assault job stopped");
		foreach (Pawn p2 in array.Where((Pawn x) => x != leader))
		{
			Job curJob = p2.CurJob;
			bool flag = curJob != null && curJob.def == JobDefOf.Goto && curJob.locomotionUrgency == LocomotionUrgency.Sprint;
			Expect(notes, flag && curJob.targetA.Cell.DistanceTo(leader.Position) <= 6f, p2.LabelShort + ": sprints to a cell near the leader" + (flag ? " (" + curJob.targetA.Cell.DistanceTo(leader.Position).ToString("F1") + " cells)" : ""));
		}
		Order("Undraft", squad);
	}

	private Faction Enemy()
	{
		return Find.FactionManager.FirstFactionOfDef(FactionDefOf.Pirate) ?? Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: false);
	}

	// A cell between minD and maxD from center, seen from it.
	private IntVec3 CellAt(IntVec3 center, float minD, float maxD)
	{
		IntVec3 result;
		if (CellFinder.TryFindRandomCellNear(center, map, (int)maxD, (IntVec3 c) => c.Standable(map) && !c.Fogged(map) && c.GetFirstPawn(map) == null && c.DistanceTo(center) >= minD && c.DistanceTo(center) <= maxD && GenSight.LineOfSight(center, c, map) && map.reachability.CanReach(home, c, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly), out result))
		{
			return result;
		}
		throw new Exception("no cell at " + minD + "-" + maxD);
	}

	private Pawn NewEnemy(PawnKindDef kind, Faction faction, IntVec3 cell)
	{
		Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, null, forceGenerateNewPawn: true));
		GenSpawn.Spawn(pawn, cell, map);
		return pawn;
	}

	private void CheckAssaultModes(List<string> notes)
	{
		Faction faction = Enemy();
		List<PawnKindDef> list = (from k in DefDatabase<PawnKindDef>.AllDefs
			where k.RaceProps.Humanlike && k.defaultFactionDef == faction.def && k.combatPower > 0f && !k.factionLeader
			orderby k.combatPower
			select k).ToList();
		if (list.Count < 2 || list[0].combatPower == list[list.Count - 1].combatPower)
		{
			notes.Add("FAIL setup: no two kinds of " + faction.def.defName + " with different combat power");
			return;
		}
		PawnKindDef weakKind = list[0];
		PawnKindDef strongKind = list[list.Count - 1];
		Pawn[] array = new Pawn[3]
		{
			Fighter(),
			Fighter(),
			Fighter()
		};
		// Grouped around one cell: the map is crowded near home by now, and the assault only looks 20 cells out.
		Move(array[0], FreeCell(2));
		IntVec3 anchor = array[0].Position;
		foreach (Pawn p in array)
		{
			if (p != array[0])
			{
				Move(p, FreeCell(2, anchor));
			}
			Gun(p);
		}
		// The weak one nearer (what vanilla would take first), the strong one farther and fleeing.
		Pawn weak = NewEnemy(weakKind, faction, CellAt(anchor, 6f, 9f));
		Pawn strong = NewEnemy(strongKind, faction, CellAt(anchor, 10f, 13f));
		strong.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest);
		notes.Add("enemies (" + faction.def.defName + "): weak " + weakKind.defName + " " + weakKind.combatPower + " at " + weak.Position.DistanceTo(home).ToString("F1") + ", strong and fleeing " + strongKind.defName + " " + strongKind.combatPower + " at " + strong.Position.DistanceTo(home).ToString("F1"));
		Expect(notes, weak.HostileTo(Faction.OfPlayer) && strong.HostileTo(Faction.OfPlayer), "setup: both enemies hostile");
		Expect(notes, array.All((Pawn p) => GenSight.LineOfSight(p.Position, weak.Position, map) && GenSight.LineOfSight(p.Position, strong.Position, map)), "setup: every member sees both enemies");
		Expect(notes, array.All((Pawn p) => p.Position.DistanceTo(strong.Position) <= 19f && p.Position.DistanceTo(weak.Position) <= 19f), "setup: both enemies within the assault's 20 cells of every member (" + string.Join(", ", array.Select((Pawn p) => p.Position.DistanceTo(weak.Position).ToString("F0") + "/" + p.Position.DistanceTo(strong.Position).ToString("F0"))) + ")");
		int squad = Squad(array);
		Idle(array);
		Find.Selector.ClearSelection();
		foreach (var item in new (AssaultMode, Pawn)[3]
		{
			(AssaultMode.Strongest, strong),
			(AssaultMode.Weakest, weak),
			(AssaultMode.Fleeing, strong)
		})
		{
			AssaultMode mode = item.Item1;
			Pawn want = item.Item2;
			// Vanilla keeps an acquired target until it loses it: start each mode from no target.
			foreach (Pawn p2 in array)
			{
				p2.mindState.enemyTarget = null;
			}
			Idle(array);
			Call(ordersType, "Assault", squad, mode);
			notes.Add(mode.ToString() + ": " + string.Join(", ", array.Select((Pawn p) => JobOf(p) + " target " + ((p.mindState.enemyTarget as Pawn)?.kindDef.defName ?? "none"))));
			Expect(notes, array.All((Pawn p) => p.Drafted && p.drafter.FireAtWill && ModeOf(p) == mode), mode.ToString() + ": drafted, fire at will, mode set");
			Expect(notes, new HashSet<Pawn>(Find.Selector.SelectedPawns).SetEquals(array), mode.ToString() + ": the members are the selection");
			Expect(notes, array.All(FromAssaultGiver), mode.ToString() + ": current job from JobGiver_SquadAssault");
			Expect(notes, array.All((Pawn p) => p.mindState.enemyTarget == want), mode.ToString() + ": target is the " + ((want == weak) ? "weak" : "strong") + " enemy");
		}
		// Switching mode while a target is held: vanilla's target keeping decides, reported only.
		Call(ordersType, "Assault", squad, AssaultMode.Weakest);
		notes.Add("note: Fleeing then Weakest without clearing the target: " + string.Join(",", array.Select((Pawn p) => (p.mindState.enemyTarget == weak) ? "weak" : ((p.mindState.enemyTarget == strong) ? "strong" : "none"))));
		Call(ordersType, "StopAssault", squad);
		Order("Undraft", squad);
		Gone(new Thing[2] { weak, strong });
		foreach (Pawn p3 in array)
		{
			p3.mindState.enemyTarget = null;
		}
	}

	// Vanilla keeps the incapable of violence out of JobGiver_AIFightEnemy by their hostility response, never Attack
	// for them; JobGiver_SquadAssault sets Attack for the call.
	private void CheckAssaultPacifist(List<string> notes, bool armedPacifist)
	{
		BackstoryDef backstoryDef = DefDatabase<BackstoryDef>.AllDefs.FirstOrDefault((BackstoryDef x) => x.slot == BackstorySlot.Adulthood && (x.workDisables & WorkTags.Violent) != 0);
		Pawn pacifist = NewColonist();
		Pawn armed = Fighter();
		pacifist.story.Adulthood = backstoryDef;
		pacifist.Notify_DisabledWorkTypesChanged();
		Pawn[] array = new Pawn[2] { pacifist, armed };
		Move(pacifist, FreeCell(2));
		Move(armed, FreeCell(2, pacifist.Position));
		// Vanilla never lets a pawn incapable of violence equip a weapon: the armed case needs a mod or a change of
		// backstory after equipping; the unarmed one is the state play reaches.
		Gun(armed);
		if (armedPacifist)
		{
			Gun(pacifist);
		}
		Expect(notes, pacifist.WorkTagIsDisabled(WorkTags.Violent) && (pacifist.equipment.Primary != null) == armedPacifist, "setup: member incapable of violence, " + (armedPacifist ? "armed" : "unarmed"));
		Faction faction = Enemy();
		PawnKindDef kind = DefDatabase<PawnKindDef>.AllDefs.First((PawnKindDef k) => k.RaceProps.Humanlike && k.defaultFactionDef == faction.def && k.combatPower > 0f && !k.factionLeader);
		Pawn enemy = NewEnemy(kind, faction, CellAt(pacifist.Position, 5f, 8f));
		int squad = Squad(array);
		Idle(array);
		Boot.Errors.Clear();
		Call(ordersType, "Assault", squad, AssaultMode.Strongest);
		notes.Add("jobs: " + string.Join(", ", array.Select((Pawn p) => JobOf(p) + " giver " + (p.CurJob?.jobGiver?.GetType().Name ?? "none") + " target " + (p.mindState.enemyTarget?.LabelShort ?? "none"))) + "; hostility of the pacifist " + pacifist.playerSettings.hostilityResponse);
		Expect(notes, FromAssaultGiver(armed) && armed.mindState.enemyTarget == enemy, "setup: the other member attacks");
		bool flag = pacifist.CurJobDef == JobDefOf.AttackStatic || pacifist.CurJobDef == JobDefOf.AttackMelee || (FromAssaultGiver(pacifist) && pacifist.mindState.enemyTarget != null);
		Expect(notes, !flag, "member incapable of violence: no attack job, no enemy target from the assault");
		string text = Boot.Errors.FirstOrDefault((string e) => e.Contains("disabled stat"));
		if (text != null)
		{
			notes.Add("vanilla consistency error: " + text.Split('\n')[0]);
		}
		Call(ordersType, "StopAssault", squad);
		Order("Undraft", squad);
		Gone(new Thing[1] { enemy });
	}

	private void CheckStopAssault(List<string> notes)
	{
		Pawn[] array = new Pawn[3]
		{
			Fighter(),
			Fighter(),
			Fighter()
		};
		int squad = Squad(array);
		Pawn other = Fighter();
		int squad2 = Squad(other);
		Idle(array.Concat(new Pawn[1] { other }));
		Call(ordersType, "Assault", squad, AssaultMode.Weakest);
		Call(ordersType, "Assault", squad2, AssaultMode.Weakest);
		Expect(notes, array.All((Pawn p) => ModeOf(p) == AssaultMode.Weakest), "setup: squad on assault");
		Down(array[2]);
		// Vanilla undrafts a pawn that goes down, and the undraft patch clears its mode on the spot.
		Expect(notes, array[2].Downed && !array[2].Drafted && ModeOf(array[2]) == null, "member downed during the assault: undrafted by the game, mode cleared at once");
		Call(ordersType, "StopAssault", squad);
		notes.Add("jobs: " + string.Join(", ", array.Select(JobOf)));
		Expect(notes, ModeOf(array[0]) == null && ModeOf(array[1]) == null, "modes cleared");
		Expect(notes, array[0].Drafted && array[1].Drafted, "members stay drafted");
		Expect(notes, !FromAssaultGiver(array[0]) && !FromAssaultGiver(array[1]), "assault jobs stopped");
		Expect(notes, ModeOf(other) == AssaultMode.Weakest, "other squad still on assault");
		Call(ordersType, "StopAssault", squad2);
		Order("Undraft", squad);
		Order("Undraft", squad2);
		Gone(new Thing[1] { array[2] });
	}

	private void CheckAssaultEnds(List<string> notes)
	{
		Pawn[] array = new Pawn[3]
		{
			Fighter(),
			Fighter(),
			Fighter()
		};
		int squad = Squad(array);
		Idle(array);
		Call(ordersType, "Assault", squad, AssaultMode.Strongest);
		Expect(notes, array.All((Pawn p) => ModeOf(p) == AssaultMode.Strongest && FromAssaultGiver(p)), "setup: all on assault, assault job running (" + string.Join(", ", array.Select(JobOf)) + ")");
		IntVec3 c = FreeCell(10);
		Job job = JobMaker.MakeJob(JobDefOf.Goto, c);
		bool flag = array[0].jobs.TryTakeOrderedJob(job, JobTag.Misc);
		Expect(notes, flag && ModeOf(array[0]) == null && array[0].CurJobDef == JobDefOf.Goto && array[0].CurJob.targetA.Cell == c, "player order: assault ended, order taken now (" + JobOf(array[0]) + ")");
		array[1].drafter.Drafted = false;
		Expect(notes, ModeOf(array[1]) == null, "undraft: assault ended");
		array[2].drafter.FireAtWill = false;
		Expect(notes, ModeOf(array[2]) == null, "hold fire: assault ended");
		Expect(notes, !FromAssaultGiver(array[2]), "hold fire: running assault job interrupted (" + JobOf(array[2]) + ")");
		Expect(notes, array[2].Drafted, "hold fire: stays drafted");
		Order("Undraft", squad);
	}

	// ---- battle stations ----

	private void CheckStationSave(List<string> notes)
	{
		BarSquads get = BarSquads.Get;
		Pawn a = NewColonist();
		Pawn b = NewColonist();
		Pawn downed = NewColonist();
		int squad = Squad(a, b, downed);
		IntVec3 old = FreeCell(12);
		get.SetStation(downed, 0, map, old);
		Down(downed);
		Boot.Messages.Clear();
		Call(stationsType, "Save", squad);
		Expect(notes, get.StationOn(a, 0, map, out var cell) && cell == a.Position, "member a: station = its cell");
		Expect(notes, get.StationOn(b, 0, map, out var cell2) && cell2 == b.Position, "member b: station = its cell");
		Expect(notes, get.StationOn(downed, 0, map, out var cell3) && cell3 == old, "downed member keeps its previous station");
		Expect(notes, (StationMask(squad) & 1) != 0, "station mask: slot 1 set");
		string text = "RCB_Stations_Saved".Translate(1, 2).ToString();
		Expect(notes, Said(text), "message: " + text + " (got " + LastMessage() + ")");
		Gone(new Thing[1] { downed });
	}

	private void CheckStationGo(List<string> notes)
	{
		BarSquads get = BarSquads.Get;
		Pawn a = NewColonist();
		Pawn b = NewColonist();
		Pawn none = NewColonist();
		int squad = Squad(a, b);
		Call(stationsType, "Save", squad);
		get.StationOn(a, 0, map, out var cellA);
		get.StationOn(b, 0, map, out var cellB);
		get.Assign(none, squad);
		Move(a, FreeCell(14));
		Move(b, FreeCell(14));
		Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.WoodLog);
		wall.SetFaction(Faction.OfPlayer);
		GenSpawn.Spawn(wall, cellB, map, WipeMode.Vanish);
		Expect(notes, !cellB.Standable(map), "setup: wall built on b's station");
		Call(ordersType, "Assault", squad, AssaultMode.Strongest);
		Job job = JobMaker.MakeJob(JobDefOf.Goto, FreeCell(16));
		none.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		Expect(notes, none.CurJobDef == JobDefOf.Goto, "setup: member without station on a player move");
		Call(stationsType, "Go", squad);
		notes.Add("jobs: " + string.Join(", ", new Pawn[3] { a, b, none }.Select((Pawn p) => JobOf(p) + ((p.CurJobDef == JobDefOf.Goto) ? (" to " + p.CurJob.targetA.Cell) : ""))) + "; a's station " + cellA + ", b's " + cellB);
		Expect(notes, new Pawn[3] { a, b, none }.All((Pawn p) => p.Drafted && ModeOf(p) == null), "all drafted, assault ended");
		Expect(notes, a.CurJobDef == JobDefOf.Goto && a.CurJob.targetA.Cell == cellA && a.CurJob.locomotionUrgency == LocomotionUrgency.Sprint, "a sprints to its station");
		IntVec3 intVec = ((b.CurJobDef == JobDefOf.Goto) ? b.CurJob.targetA.Cell : IntVec3.Invalid);
		Expect(notes, intVec.IsValid && intVec != cellB && intVec.Standable(map) && intVec.DistanceTo(cellB) <= 3f, "b goes to the nearest free cell instead of the walled one");
		// Jobs are pooled: the ended move's Job object may come back as the next job, so only the def is compared.
		Expect(notes, none.CurJobDef != JobDefOf.Goto, "member without station: player move ended, stays put");
		Order("Undraft", squad);
		Gone(new Thing[1] { wall });
	}

	private void CheckStationSlotsAndClear(List<string> notes)
	{
		BarSquads get = BarSquads.Get;
		Pawn a = NewColonist();
		Pawn b = NewColonist();
		Pawn away = NewColonist();
		int squad = Squad(a, b, away);
		Call(stationsType, "Save", squad);
		get.StationOn(a, 0, map, out var cell);
		Move(a, FreeCell(12));
		get.SetActiveStation(squad, 2);
		Call(stationsType, "Save", squad);
		Expect(notes, get.StationOn(a, 2, map, out var cell2) && cell2 == a.Position, "active slot 3: saved there");
		Expect(notes, get.StationOn(a, 0, map, out var cell3) && cell3 == cell, "slot 1 untouched");
		Expect(notes, StationMask(squad) == 5, "station mask: slots 1 and 3 (" + StationMask(squad) + ")");
		Move(a, FreeCell(12));
		Call(stationsType, "Go", squad);
		Expect(notes, a.CurJobDef == JobDefOf.Goto && a.CurJob.targetA.Cell == cell2, "Go uses the active slot");
		Order("Undraft", squad);
		// Refusal: no station on the active slot, nobody drafted.
		get.SetActiveStation(squad, 3);
		Call(stationsType, "Go", squad);
		string text = "RCB_Stations_None".Translate(4, get.SquadName(squad)).ToString();
		Expect(notes, !a.Drafted && !b.Drafted && LastMessage() == text, "slot without station: refused, nobody drafted (" + LastMessage() + ")");
		int count = Find.WindowStack.Windows.Count;
		Call(stationsType, "AskClear", squad);
		Expect(notes, Find.WindowStack.Windows.Count == count, "AskClear on an empty slot: no dialog");
		// A member in a caravan keeps its station on this map: Clear reaches it too.
		Caravan caravan = ToCaravan(away);
		Expect(notes, get.HasLiveStation(away, 0), "setup: member in caravan has station 1");
		get.SetActiveStation(squad, 0);
		Call(stationsType, "AskClear", squad);
		Dialog_MessageBox dialog = Find.WindowStack.Windows.OfType<Dialog_MessageBox>().LastOrDefault();
		Expect(notes, dialog != null, "AskClear: confirmation asked");
		Expect(notes, get.StationOn(a, 0, map, out var _), "before confirming: station still there");
		if (dialog != null)
		{
			dialog.buttonAAction();
			Find.WindowStack.TryRemove(dialog, doCloseSound: false);
		}
		Expect(notes, !get.StationOn(a, 0, map, out var _) && !get.StationOn(b, 0, map, out var _), "confirmed: slot 1 cleared for the members here");
		Expect(notes, !get.HasLiveStation(away, 0), "confirmed: cleared for the member in the caravan");
		Expect(notes, get.StationOn(a, 2, map, out var _), "slot 3 kept");
		string text2 = "RCB_Stations_Cleared".Translate(1).ToString();
		Expect(notes, LastMessage() == text2, "message: " + text2);
		notes.Add("caravan " + caravan.Label);
	}

	// ---- care ----

	private void CheckCareBaseline(List<string> notes, int squad)
	{
		TendLeftovers(notes);
		List<Pawn> list = MedTargets(squad, "Rescue");
		List<Pawn> list2 = MedTargets(squad, "Tend");
		if (list.Count > 0 || list2.Count > 0)
		{
			notes.Add("FAIL setup: patients left from earlier checks: " + Ids(list.Concat(list2)));
		}
	}

	private void CheckRescue(List<string> notes)
	{
		Pawn a = NewColonist();
		Pawn b = NewColonist();
		int squad = Squad(a, b);
		CheckCareBaseline(notes, squad);
		Pawn bleeder = NewColonist();
		Pawn member = NewColonist();
		Pawn other = NewColonist();
		Down(bleeder);
		Bleed(bleeder);
		Down(member);
		Down(other);
		BarSquads.Get.Assign(member, squad);
		Building_Bed medBed = Bed(medical: true);
		Building_Bed bed = Bed(medical: false);
		// a next to the downed member, b next to the bleeder: nearest wins, whatever the bar order.
		Move(a, FreeCell(1, member.Position));
		Move(b, FreeCell(1, bleeder.Position));
		Expect(notes, BleedingOut(bleeder) && !BleedingOut(member) && !BleedingOut(other), "setup: one patient bleeding out");
		List<Pawn> list = MedTargets(squad, "Rescue");
		notes.Add("patients in order: " + Ids(list));
		Expect(notes, list.Count == 3 && list[0] == bleeder && list[1] == member && list[2] == other, "order: bleeding out, then member, then the others");
		Boot.Messages.Clear();
		Call(medicalType, "Rescue", squad);
		notes.Add("jobs: " + JobOf(a) + " bed " + (a.CurJob?.targetB.Thing as Building_Bed)?.Medical + ", " + JobOf(b) + " bed " + (b.CurJob?.targetB.Thing as Building_Bed)?.Medical + "; message " + LastMessage());
		Expect(notes, b.CurJobDef == JobDefOf.Rescue && b.CurJob.targetA.Thing == bleeder, "bleeder carried by the nearest member (b)");
		Expect(notes, a.CurJobDef == JobDefOf.Rescue && a.CurJob.targetA.Thing == member, "downed member carried by a");
		Expect(notes, b.CurJob?.targetB.Thing == medBed, "bleeder goes to the medical bed");
		// Check 9 left beds nearer: any other bed than the bleeder's will do.
		Expect(notes, a.CurJob?.targetB.Thing is Building_Bed building_Bed && building_Bed != medBed, "second patient gets another bed");
		Expect(notes, Said("RCB_Medical_RescueDone".Translate(2).ToString()), "message: 2 on the way");
		Call(medicalType, "Rescue", squad);
		Expect(notes, a.CurJobDef == JobDefOf.Rescue && a.CurJob.targetA.Thing == member && b.CurJobDef == JobDefOf.Rescue && b.CurJob.targetA.Thing == bleeder, "second click: members already rescuing keep their patient");
		Expect(notes, LastMessage() == "RCB_Medical_NoneFree".Translate().ToString(), "second click: none free (" + LastMessage() + ")");
		Gone(new Thing[5] { bleeder, member, other, medBed, bed });
		Order("Undraft", squad);
	}

	private void CheckTend(List<string> notes)
	{
		Pawn doctor = NewColonist(WorkTypeDefOf.Doctor);
		Pawn noDoctor = NewColonist();
		Fixture.Disable(noDoctor, WorkTypeDefOf.Doctor);
		int squad = Squad(doctor, noDoctor);
		CheckCareBaseline(notes, squad);
		Pawn w1 = NewColonist();
		Pawn w2 = NewColonist();
		foreach (Pawn p in new Pawn[2] { w1, w2 })
		{
			Wound(p);
			p.playerSettings.medCare = MedicalCareCategory.HerbalOrWorse;
			FreshUi(p);
		}
		Thing thing = ThingMaker.MakeThing(ThingDefOf.MedicineHerbal);
		thing.stackCount = 5;
		GenPlace.TryPlaceThing(thing, FreeCell(2, doctor.Position), map, ThingPlaceMode.Near);
		Thing thing2 = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
		thing2.stackCount = 5;
		GenPlace.TryPlaceThing(thing2, FreeCell(2, doctor.Position), map, ThingPlaceMode.Near);
		List<Pawn> list = MedTargets(squad, "Tend");
		Expect(notes, list.Contains(w1) && list.Contains(w2) && !w1.Downed, "setup: two standing wounded counted");
		// No doctor among the free members: refused.
		int squad2 = Squad(NewColonist());
		Fixture.Disable(Able(squad2)[0], WorkTypeDefOf.Doctor);
		Call(medicalType, "Tend", squad2);
		Expect(notes, LastMessage() == "RCB_Medical_NoDoctor".Translate().ToString(), "squad without doctor: refused (" + LastMessage() + ")");
		Boot.Messages.Clear();
		Call(medicalType, "Tend", squad);
		Job curJob = doctor.CurJob;
		notes.Add("jobs: " + JobOf(doctor) + " medicine " + (curJob?.targetB.Thing?.def.defName ?? "none") + ", " + JobOf(noDoctor) + "; message " + LastMessage());
		Expect(notes, curJob?.def == JobDefOf.TendPatient && (curJob.targetA.Thing == w1 || curJob.targetA.Thing == w2), "doctor tends a patient");
		Expect(notes, noDoctor.CurJobDef != JobDefOf.TendPatient, "member who cannot doctor not sent");
		Expect(notes, curJob?.targetB.Thing?.def == ThingDefOf.MedicineHerbal, "herbal medicine, the best HerbalOrWorse allows");
		Expect(notes, curJob != null && curJob.draftedTend, "standing patient waits for the doctor (draftedTend)");
		Expect(notes, Said("RCB_Medical_TendDone".Translate(1).ToString()), "message: 1 on the way");
		Gone(new Thing[4] { w1, w2, thing, thing2 });
		Order("Undraft", squad);
	}

	private void CheckToBed(List<string> notes)
	{
		Pawn healthy = NewColonist();
		Pawn wounded = NewColonist();
		int squad = Squad(healthy, wounded);
		// A generated colonist may come with asthma or the like, untended: it would need rest too.
		TendLeftovers(notes);
		Wound(wounded);
		Building_Bed bed = Bed(medical: false);
		Building_Bed medBed = Bed(medical: true);
		Order("Draft", squad);
		List<Pawn> list = MedTargets(squad, "Bed");
		Expect(notes, list.Count == 1 && list[0] == wounded, "only the wounded member needs rest (" + Ids(list) + ")");
		Boot.Messages.Clear();
		Call(medicalType, "ToBed", squad);
		notes.Add("jobs: " + JobOf(healthy) + ", " + JobOf(wounded) + "; message " + LastMessage());
		Expect(notes, !wounded.Drafted, "wounded member undrafted");
		Expect(notes, wounded.CurJobDef == JobDefOf.LayDown && wounded.CurJob.restUntilHealed, "rests until healed");
		Expect(notes, wounded.CurJob?.targetA.Thing == medBed, "in the medical bed");
		Expect(notes, healthy.Drafted && healthy.CurJobDef != JobDefOf.LayDown, "healthy member left drafted");
		Expect(notes, Said("RCB_Medical_BedDone".Translate(1).ToString()), "message: 1");
		Order("Undraft", squad);
		wounded.health.hediffSet.hediffs.RemoveAll((Hediff h) => h is Hediff_Injury);
		wounded.jobs.EndCurrentJob(JobCondition.InterruptForced);
		Gone(new Thing[2] { bed, medBed });
	}

	// ---- rules ----

	private object Rule(string rule)
	{
		return Enum.Parse(ruleEnum, rule);
	}

	private List<Pawn> RuleMembers(int squad)
	{
		return (List<Pawn>)Call(rulesType, "Members", squad);
	}

	private string Shared(int squad, string rule, out bool mixed, out List<Pawn> into)
	{
		into = new List<Pawn>();
		object[] array = new object[4]
		{
			RuleMembers(squad),
			Rule(rule),
			into,
			false
		};
		string result = (string)AccessTools.Method(rulesType, "Shared").Invoke(null, array);
		mixed = (bool)array[3];
		return result;
	}

	// Opens the rule's menu as the card does and clicks the option with this label.
	private void Pick(int squad, string rule, string label)
	{
		string current = Shared(squad, rule, out var _, out var _);
		Call(rulesType, "OpenMenu", squad, Rule(rule), current);
		FloatMenu floatMenu = Find.WindowStack.Windows.OfType<FloatMenu>().LastOrDefault() ?? throw new Exception("no menu for " + rule);
		List<FloatMenuOption> list = (List<FloatMenuOption>)AccessTools.Field(typeof(FloatMenu), "options").GetValue(floatMenu);
		string marked = "RCB_Rules_Current".Translate(label).ToString();
		FloatMenuOption floatMenuOption = list.FirstOrDefault((FloatMenuOption o) => o.Label == label || o.Label == marked);
		Find.WindowStack.TryRemove(floatMenu, doCloseSound: false);
		if (floatMenuOption == null)
		{
			throw new Exception(rule + ": no option " + label + " among " + string.Join("|", list.Select((FloatMenuOption o) => o.Label)));
		}
		floatMenuOption.action();
	}

	private void CheckRules(List<string> notes)
	{
		Pawn a = NewColonist();
		Pawn b = NewColonist();
		Pawn c = NewColonist();
		int squad = Squad(a, b, c);
		ToCaravan(c);
		Pawn[] all = new Pawn[3] { a, b, c };
		Expect(notes, RuleMembers(squad).Count == 3, "members include the caravan member");
		ApparelPolicy apparel = Current.Game.outfitDatabase.AllOutfits.Last((ApparelPolicy x) => x != a.outfits.CurrentApparelPolicy);
		FoodPolicy food = Current.Game.foodRestrictionDatabase.AllFoodRestrictions.Last((FoodPolicy x) => x != a.foodRestriction.CurrentFoodPolicy);
		DrugPolicy drug = Current.Game.drugPolicyDatabase.AllPolicies.Last((DrugPolicy x) => x != a.drugs.CurrentPolicy);
		Pick(squad, "Apparel", apparel.label);
		Expect(notes, all.All((Pawn p) => p.outfits.CurrentApparelPolicy == apparel), "Apparel: " + apparel.label + " for all");
		Pick(squad, "Food", food.label);
		Expect(notes, all.All((Pawn p) => p.foodRestriction.CurrentFoodPolicy == food), "Food: " + food.label + " for all");
		Pick(squad, "Drugs", drug.label);
		Expect(notes, all.All((Pawn p) => p.drugs.CurrentPolicy == drug), "Drugs: " + drug.label + " for all");
		Pick(squad, "Medical", MedicalCareCategory.NoMeds.GetLabel());
		Expect(notes, all.All((Pawn p) => p.playerSettings.medCare == MedicalCareCategory.NoMeds), "Medical: no medicine for all");
		Pick(squad, "Hostility", HostilityResponseMode.Ignore.GetLabel());
		Expect(notes, all.All((Pawn p) => p.playerSettings.hostilityResponse == HostilityResponseMode.Ignore), "Hostility: ignore for all");
		Expect(notes, LastMessage() == "RCB_Rules_Set".Translate(SquadRulesLabel("Hostility"), HostilityResponseMode.Ignore.GetLabel(), 3).ToString(), "message counts 3 (" + LastMessage() + ")");
		string text = Shared(squad, "Medical", out var mixed, out var _);
		Expect(notes, !mixed && text == MedicalCareCategory.NoMeds.GetLabel(), "shared value read back, not mixed");
		b.playerSettings.medCare = MedicalCareCategory.Best;
		Shared(squad, "Medical", out var mixed2, out var _);
		Expect(notes, mixed2, "one member differs: Mixed");
		Pick(squad, "Medical", MedicalCareCategory.Best.GetLabel());
		Shared(squad, "Medical", out var mixed3, out var _);
		Expect(notes, !mixed3 && all.All((Pawn p) => p.playerSettings.medCare == MedicalCareCategory.Best), "Mixed, then a choice: same for all");
	}

	private string SquadRulesLabel(string rule)
	{
		return (string)Call(rulesType, "Label", Rule(rule));
	}

	private void CheckRulesAreaHostility(List<string> notes)
	{
		Pawn a = Fighter();
		Pawn b = Fighter();
		Pawn c = Fighter();
		Pawn pacifist = NewColonist();
		BackstoryDef backstoryDef = DefDatabase<BackstoryDef>.AllDefs.FirstOrDefault((BackstoryDef x) => x.slot == BackstorySlot.Adulthood && (x.workDisables & WorkTags.Violent) != 0);
		if (backstoryDef == null)
		{
			notes.Add("FAIL setup: no adulthood backstory disabling violence");
			return;
		}
		pacifist.story.Adulthood = backstoryDef;
		pacifist.Notify_DisabledWorkTypesChanged();
		Expect(notes, pacifist.WorkTagIsDisabled(WorkTags.Violent), "setup: one member incapable of violence (" + backstoryDef.defName + ")");
		int squad = Squad(a, b, c, pacifist);
		ToCaravan(c);
		if (!map.areaManager.TryMakeNewAllowed(out var area))
		{
			notes.Add("FAIL setup: no allowed area slot");
			return;
		}
		Expect(notes, !(bool)Call(rulesType, "Applies", c, Rule("Area")), "area does not apply to the caravan member");
		Pick(squad, "Area", area.Label);
		Expect(notes, a.playerSettings.AreaRestrictionInPawnCurrentMap == area && b.playerSettings.AreaRestrictionInPawnCurrentMap == area && pacifist.playerSettings.AreaRestrictionInPawnCurrentMap == area, "area set for the members on this map");
		string text = "RCB_Rules_Set".Translate(SquadRulesLabel("Area"), area.Label, 3).ToString();
		Expect(notes, LastMessage() == text, "message counts 3, caravan member left out (" + LastMessage() + ")");
		Pick(squad, "Area", "NoAreaAllowed".Translate().ToString());
		Expect(notes, a.playerSettings.AreaRestrictionInPawnCurrentMap == null, "no area: cleared");
		Pick(squad, "Hostility", HostilityResponseMode.Flee.GetLabel());
		Pick(squad, "Hostility", HostilityResponseMode.Attack.GetLabel());
		Expect(notes, a.playerSettings.hostilityResponse == HostilityResponseMode.Attack && b.playerSettings.hostilityResponse == HostilityResponseMode.Attack && c.playerSettings.hostilityResponse == HostilityResponseMode.Attack, "Attack for the others, caravan member included");
		Expect(notes, pacifist.playerSettings.hostilityResponse == HostilityResponseMode.Flee, "incapable of violence keeps Flee");
		Expect(notes, LastMessage().Contains("RCB_Rules_Skipped".Translate(1).ToString()), "message: 1 left as it was (" + LastMessage() + ")");
		area.Delete();
	}

	private void CheckReleaseForced(List<string> notes)
	{
		Pawn a = NewColonist();
		Pawn b = NewColonist();
		int squad = Squad(a, b);
		Apparel apparel = a.apparel.WornApparel.FirstOrDefault();
		if (apparel == null)
		{
			notes.Add("FAIL setup: generated colonist wears nothing");
			return;
		}
		a.outfits.forcedHandler.SetForced(apparel, forced: true);
		List<Pawn> arg = RuleMembers(squad);
		Expect(notes, (int)Call(rulesType, "ForcedCount", arg) == 1, "setup: one member with forced apparel");
		Call(rulesType, "ReleaseForced", squad);
		Expect(notes, !a.outfits.forcedHandler.SomethingIsForced && (int)Call(rulesType, "ForcedCount", RuleMembers(squad)) == 0, "released");
		Expect(notes, a.apparel.WornApparel.Contains(apparel), "apparel still worn (only the forcing is lifted)");
	}

	// ---- members ----

	private void CheckMembers(List<string> notes)
	{
		BarSquads get = BarSquads.Get;
		Pawn m = NewColonist();
		Pawn downed = NewColonist();
		Down(downed);
		Pawn free = NewColonist();
		Pawn moving = NewColonist();
		Pawn stays = NewColonist();
		int squad = Squad(m, downed);
		int num = Squad(moving, stays);
		Find.Selector.ClearSelection();
		foreach (Pawn p in new Pawn[3] { free, moving, m })
		{
			Find.Selector.Select(p, playSound: false);
		}
		object[] array = new object[4] { squad, 0, 0, 0 };
		AccessTools.Method(membersType, "CountSelected").Invoke(null, array);
		Expect(notes, (int)array[1] == 2 && (int)array[2] == 1 && (int)array[3] == 1, "count: 2 joining, 1 moving, 1 inside (" + array[1] + "/" + array[2] + "/" + array[3] + ")");
		Call(membersType, "AddSelected", squad);
		Expect(notes, get.SquadIndexOf(free) == squad && get.SquadIndexOf(moving) == squad, "added: free pawn and pawn from another squad");
		Expect(notes, get.SquadIndexOf(stays) == num, "unselected member of the other squad stays there");
		Expect(notes, LastMessage() == "RCB_Members_Added".Translate(2, get.SquadName(squad)).ToString(), "message: 2 added (" + LastMessage() + ")");
		Find.Selector.ClearSelection();
		Find.Selector.Select(free, playSound: false);
		Find.Selector.Select(stays, playSound: false);
		Call(membersType, "RemoveSelected", squad);
		Expect(notes, get.SquadIndexOf(free) == -1, "removed: selected member");
		Expect(notes, get.SquadIndexOf(stays) == num, "selected pawn of another squad untouched");
		Expect(notes, LastMessage() == "RCB_Members_Removed".Translate(1, get.SquadName(squad)).ToString(), "message: 1 removed (" + LastMessage() + ")");
		Call(membersType, "SelectAll", map, squad);
		Expect(notes, new HashSet<Pawn>(Find.Selector.SelectedPawns).SetEquals(new Pawn[3] { m, downed, moving }), "select: members on the map, downed one included (" + Ids(Find.Selector.SelectedPawns) + ")");
		Find.Selector.ClearSelection();
		Gone(new Thing[1] { downed });
	}

	// ---- hotkeys (OnGUI) ----

	private static readonly string[] HotkeyDefs = new string[10] { "RCB_SquadDraft", "RCB_SquadFireAtWill", "RCB_SquadRegroup", "RCB_SquadAssaultStrongest", "RCB_SquadAssaultWeakest", "RCB_SquadAssaultFleeing", "RCB_SquadGoToStation", "RCB_SquadRescue", "RCB_SquadTend", "RCB_SquadToBed" };

	private static KeyCode KeyOf(string def)
	{
		return KeyCode.F13 + Array.IndexOf(HotkeyDefs, def);
	}

	private static void Press(string def)
	{
		Event current = Event.current;
		Event.current = new Event
		{
			type = EventType.KeyDown,
			keyCode = KeyOf(def)
		};
		try
		{
			AccessTools.Method(hotkeysType, "OnGUI").Invoke(null, null);
		}
		finally
		{
			Event.current = current;
		}
	}

	private static void Select(params Pawn[] pawns)
	{
		Find.Selector.ClearSelection();
		foreach (Pawn obj in pawns)
		{
			Find.Selector.Select(obj, playSound: false);
		}
	}

	internal void RunHotkeys()
	{
		Line("keyboardControl " + GUIUtility.keyboardControl + " during OnGUI");
		foreach (string defName in HotkeyDefs)
		{
			KeyBindingDef named = DefDatabase<KeyBindingDef>.GetNamed(defName);
			KeyPrefs.KeyPrefsData.SetBinding(named, KeyPrefs.BindingSlot.A, KeyOf(defName));
		}
		try
		{
			Run("K1 Hotkeys: draft, fire at will, assault modes, regroup for each selected squad once", CheckHotkeyOrders);
			Run("K2 Hotkeys: refusals (no squad, no station, nothing to rescue, tend or rest), unbound key", CheckHotkeyRefusals);
		}
		finally
		{
			foreach (string defName2 in HotkeyDefs)
			{
				KeyPrefs.KeyPrefsData.SetBinding(DefDatabase<KeyBindingDef>.GetNamed(defName2), KeyPrefs.BindingSlot.A, KeyCode.None);
			}
		}
	}

	private void CheckHotkeyOrders(List<string> notes)
	{
		Pawn a = NewColonist();
		Pawn a2 = NewColonist();
		Pawn b = NewColonist();
		int squad = Squad(a, a2);
		int squad2 = Squad(b);
		Pawn[] array = new Pawn[3] { a, a2, b };
		Select(a, a2, b);
		Press("RCB_SquadDraft");
		Expect(notes, array.All((Pawn p) => p.Drafted), "draft key: both squads drafted (two selected from the first, each squad once)");
		Press("RCB_SquadFireAtWill");
		Expect(notes, array.All((Pawn p) => !p.drafter.FireAtWill), "fire key: off for all");
		Press("RCB_SquadFireAtWill");
		Expect(notes, array.All((Pawn p) => p.drafter.FireAtWill), "fire key again: on for all");
		Press("RCB_SquadDraft");
		Expect(notes, array.All((Pawn p) => !p.Drafted), "draft key with all drafted: undrafted");
		Select(a, b);
		Press("RCB_SquadAssaultStrongest");
		Expect(notes, array.All((Pawn p) => p.Drafted && ModeOf(p) == AssaultMode.Strongest), "strongest key: both squads on assault (targets read before the assault reselects)");
		Select(a);
		Press("RCB_SquadAssaultWeakest");
		Expect(notes, ModeOf(a) == AssaultMode.Weakest && ModeOf(a2) == AssaultMode.Weakest && ModeOf(b) == AssaultMode.Strongest, "weakest key: only the selected squad");
		Select(b);
		Press("RCB_SquadAssaultFleeing");
		Expect(notes, ModeOf(b) == AssaultMode.Fleeing && ModeOf(a) == AssaultMode.Weakest, "fleeing key: only the selected squad");
		Select(a);
		Press("RCB_SquadRegroup");
		Expect(notes, ModeOf(a) == null && ModeOf(a2) == null && ModeOf(b) == AssaultMode.Fleeing, "regroup key: assault of the selected squad ended");
		Call(ordersType, "StopAssault", squad2);
		Order("Undraft", squad);
		Order("Undraft", squad2);
	}

	private void CheckHotkeyRefusals(List<string> notes)
	{
		Pawn a = NewColonist();
		int squad = Squad(a);
		TendLeftovers(notes);
		Find.Selector.ClearSelection();
		Press("RCB_SquadDraft");
		Expect(notes, LastMessage() == "RCB_Hotkey_NoSquad".Translate().ToString(), "nothing selected: no squad (" + LastMessage() + ")");
		Select(a);
		Press("RCB_SquadGoToStation");
		string text = "RCB_Stations_None".Translate(BarSquads.Get.ActiveStation(squad) + 1, BarSquads.Get.SquadName(squad)).ToString();
		Expect(notes, LastMessage() == text && !a.Drafted, "station key without station: refused, not drafted (" + LastMessage() + ")");
		Press("RCB_SquadRescue");
		Expect(notes, LastMessage() == "RCB_Medical_NoDowned".Translate().ToString(), "rescue key: nobody down (" + LastMessage() + ")");
		Press("RCB_SquadTend");
		Expect(notes, LastMessage() == "RCB_Medical_NoWounded".Translate().ToString(), "tend key: nobody wounded (" + LastMessage() + ")");
		Press("RCB_SquadToBed");
		Expect(notes, LastMessage() == "RCB_Medical_NoRest".Translate().ToString(), "bed key: nobody to rest (" + LastMessage() + ")");
		KeyPrefs.KeyPrefsData.SetBinding(DefDatabase<KeyBindingDef>.GetNamed("RCB_SquadDraft"), KeyPrefs.BindingSlot.A, KeyCode.None);
		Press("RCB_SquadDraft");
		Expect(notes, !a.Drafted, "unbound key: nothing happens");
		Find.Selector.ClearSelection();
	}

	// ---- save round trip ----

	internal string PrepareRoundTrip()
	{
		Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
		bool ok = false;
		Run("T1 Round trip set-up: renamed squad with stations on slot 2, squad on assault, game saved", delegate(List<string> notes)
		{
			BarSquads get = BarSquads.Get;
			Pawn pawn = NewColonist();
			Pawn pawn2 = NewColonist();
			Pawn pawn3 = NewColonist();
			Pawn pawn4 = NewColonist();
			int num = Squad(pawn, pawn2);
			int num2 = Squad(pawn3, pawn4);
			get.Rename(num, "Roundtrip Alpha");
			get.SetActiveStation(num, 1);
			Call(stationsType, "Save", num);
			Call(ordersType, "Assault", num2, AssaultMode.Weakest);
			Saved.names = new List<string>(get.names);
			Saved.active = new List<int>(get.activeStation);
			Saved.renamed = num;
			Saved.stationSlot = 1;
			Saved.mapId = map.uniqueID;
			foreach (KeyValuePair<int, int> item in get.squadOf)
			{
				Saved.squadOf[item.Key] = item.Value;
			}
			foreach (Pawn p in new Pawn[2] { pawn, pawn2 })
			{
				Expect(notes, get.StationOn(p, 1, map, out var cell), "setup: " + p.LabelShort + " has station 2");
				Saved.stationCell[p.thingIDNumber] = cell;
			}
			foreach (Pawn p2 in new Pawn[2] { pawn3, pawn4 })
			{
				Expect(notes, ModeOf(p2) == AssaultMode.Weakest && p2.Drafted, "setup: " + p2.LabelShort + " drafted, on assault");
				Saved.modes[p2.thingIDNumber] = AssaultMode.Weakest;
			}
			notes.Add(Saved.names.Count + " squads, " + Saved.squadOf.Count + " assignments, " + Saved.stationCell.Count + " stations, " + Saved.modes.Count + " on assault");
			File.Delete(GenFilePaths.FilePathForSavedGame(SaveName));
			GameDataSaveLoader.SaveGame(SaveName);
			ok = File.Exists(GenFilePaths.FilePathForSavedGame(SaveName));
			Expect(notes, ok, "save file written");
		});
		// The load is checked from a clean error list: what it logs belongs to the verification.
		Boot.Errors.Clear();
		return ok ? SaveName : null;
	}

	internal void VerifyRoundTrip()
	{
		Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
		Run("T2 After load: squad names, members, stations, active slot, assault in progress", delegate(List<string> notes)
		{
			BarSquads get = BarSquads.Get;
			SquadAssault get2 = SquadAssault.Get;
			Expect(notes, get != null && get2 != null, "components present");
			Expect(notes, map.uniqueID == Saved.mapId, "same map");
			Expect(notes, get.names.SequenceEqual(Saved.names), "squad names equal (" + get.names.Count + "), renamed one reads " + get.SquadName(Saved.renamed));
			Expect(notes, get.activeStation.SequenceEqual(Saved.active), "active stations equal");
			List<int> list = Saved.squadOf.Where((KeyValuePair<int, int> kv) => !get.squadOf.TryGetValue(kv.Key, out var value) || value != kv.Value).Select((KeyValuePair<int, int> kv) => kv.Key).ToList();
			Expect(notes, list.Count == 0 && get.squadOf.Count == Saved.squadOf.Count, "assignments equal (" + get.squadOf.Count + "/" + Saved.squadOf.Count + ")" + ((list.Count > 0) ? (" differ: " + string.Join(",", list)) : ""));
			foreach (KeyValuePair<int, IntVec3> kv2 in Saved.stationCell)
			{
				Pawn pawn = map.mapPawns.AllPawns.FirstOrDefault((Pawn p) => p.thingIDNumber == kv2.Key);
				Expect(notes, pawn != null && get.StationOn(pawn, Saved.stationSlot, map, out var cell) && cell == kv2.Value, "station 2 of pawn " + kv2.Key + " back at " + kv2.Value);
			}
			foreach (KeyValuePair<int, AssaultMode> kv3 in Saved.modes)
			{
				Pawn pawn2 = map.mapPawns.AllPawns.FirstOrDefault((Pawn p) => p.thingIDNumber == kv3.Key);
				Expect(notes, pawn2 != null && pawn2.Drafted && get2.TryGetMode(pawn2, out var mode) && mode == kv3.Value, "pawn " + kv3.Key + " drafted, assault " + kv3.Value + " kept" + ((pawn2 != null) ? (" (" + JobOf(pawn2) + ")") : ""));
			}
		}, keepErrors: true);
	}
}
