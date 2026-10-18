using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RadiusColonistBar;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RCBPlusTests;

// Automated checks for the squad Work tab (spec 2026-10-18-travail-escouade-design.md, section 9). Inert unless the
// game is started with -rcbtest=<results file> (and -quicktest): it then runs every scenario once on the quick test
// map, writes PASS/FAIL lines to the file and quits. The UI is not driven: the order card's handlers are called
// directly, so the wiring of clicks to these calls is out of scope.
[StaticConstructorOnStartup]
public static class Boot
{
	internal static string ResultsPath;

	internal static readonly List<string> Messages = new List<string>();

	internal static readonly List<string> Errors = new List<string>();

	static Boot()
	{
		GenCommandLine.TryGetCommandLineArg("rcbtest", out ResultsPath);
		UiServer.Bind();
		// CommandLineArgPassed only matches a bare flag, never the -rcbperf=<path> form.
		bool perf = GenCommandLine.TryGetCommandLineArg("rcbperf", out _);
		if (ResultsPath == null && !UiServer.On && !perf)
		{
			return;
		}
		Application.runInBackground = true;
		Harmony harmony = new Harmony("mrbabilo.rcbplus.tests");
		PerfRunner.Bind(harmony);
		if (UiServer.Interactive)
		{
			harmony.PatchAll(typeof(Boot).Assembly);
		}
		if (ResultsPath == null)
		{
			return;
		}
		File.WriteAllText(ResultsPath, "# Colonist Bar Plus squad work checks\n");
		harmony.Patch(AccessTools.Method(typeof(Verse.Messages), "Message", new Type[2]
		{
			typeof(Message),
			typeof(bool)
		}), postfix: new HarmonyMethod(typeof(Boot), "MessagePostfix"));
		harmony.Patch(AccessTools.Method(typeof(Log), "Error", new Type[1] { typeof(string) }), postfix: new HarmonyMethod(typeof(Boot), "ErrorPostfix"));
	}

	public static void MessagePostfix(Message msg)
	{
		Messages.Add(msg.text);
	}

	public static void ErrorPostfix(string text)
	{
		Errors.Add(text);
	}
}

public class TestRunner : GameComponent
{
	// The runner outlives one game: the save round trip loads a new game, with a new TestRunner. Direct checks and the
	// round trip set-up run in Update, the hotkeys and the load in OnGUI (the hotkeys read Event.current).
	internal enum Phase
	{
		Direct,
		Hotkeys,
		SaveSetup,
		Load,
		Verify,
		Done
	}

	internal static Phase phase;

	internal static string saveName;

	private int frames;

	public TestRunner(Game game)
	{
	}

	private static bool Ready => Boot.ResultsPath != null && Find.CurrentMap != null && !LongEventHandler.ShouldWaitForEvent;

	public override void GameComponentUpdate()
	{
		if (!Ready || ++frames < 120)
		{
			return;
		}
		switch (phase)
		{
		case Phase.Direct:
			Guard(delegate
			{
				new Checks(Boot.ResultsPath).RunDirect();
			});
			phase = Phase.Hotkeys;
			break;
		case Phase.SaveSetup:
			phase = Phase.Done;
			Guard(delegate
			{
				saveName = new Checks(Boot.ResultsPath).PrepareRoundTrip();
			});
			if (saveName != null)
			{
				phase = Phase.Load;
			}
			else
			{
				Finish();
			}
			break;
		case Phase.Verify:
			phase = Phase.Done;
			Guard(delegate
			{
				new Checks(Boot.ResultsPath).VerifyRoundTrip();
			});
			Finish();
			break;
		}
	}

	public override void GameComponentOnGUI()
	{
		if (!Ready || frames < 120)
		{
			return;
		}
		if (phase == Phase.Hotkeys)
		{
			phase = Phase.SaveSetup;
			Guard(delegate
			{
				new Checks(Boot.ResultsPath).RunHotkeys();
			});
		}
		else if (phase == Phase.Load)
		{
			phase = Phase.Verify;
			// Out of the component loop: LoadGame disposes the game's maps at once.
			string name = saveName;
			LongEventHandler.QueueLongEvent(delegate
			{
				GameDataSaveLoader.LoadGame(name);
			}, "LoadingLongEvent", doAsynchronously: false, null);
		}
	}

	private static void Guard(Action a)
	{
		try
		{
			a();
		}
		catch (Exception ex)
		{
			File.AppendAllText(Boot.ResultsPath, "CRASH " + ex + "\n");
		}
	}

	private static void Finish()
	{
		File.AppendAllText(Boot.ResultsPath, "# TOTAL pass " + Checks.pass + ", fail " + Checks.fail + "\n# END\n");
		Root.Shutdown();
	}
}

internal partial class Checks
{
	private readonly string path;

	private readonly Map map;

	private readonly IntVec3 home;

	private readonly Type workType = AccessTools.TypeByName("RadiusColonistBar.SquadWork");

	private readonly Type pauseType;

	private readonly Type ordersType = AccessTools.TypeByName("RadiusColonistBar.SquadOrders");

	private readonly Type statusType = AccessTools.TypeByName("RadiusColonistBar.SquadStatus");

	internal static int pass;

	internal static int fail;

	private static WorkTypeDef PlantCutting => WorkTypeDefOf.PlantCutting;

	private static WorkTypeDef Warden => WorkTypeDefOf.Warden;

	internal Checks(string path)
	{
		this.path = path;
		map = Find.CurrentMap;
		home = map.mapPawns.FreeColonistsSpawned.FirstOrDefault()?.Position ?? map.Center;
		pauseType = AccessTools.Inner(workType, "Pause");
	}

	internal void RunDirect()
	{
		// Paused: the checks are synchronous, and the hotkeys run a frame later on the same scene.
		Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
		Line("map " + map.Size + ", biome " + map.Biome.defName + ", manual priorities " + Find.PlaySettings.useWorkPriorities);
		Run("2 Now/Cut: three members on three different trees", Check2);
		Run("3 Incapable and priority 0 members skipped and counted", Check3);
		Run("3bis Allowed area: task inside the area", Check3bis);
		Run("4 Nothing of this type: message, nobody moves", Check4);
		Run("5 Drafted members undrafted, assault ended", Check5);
		Run("7 Manual off: chip toggles for all", Check7);
		Run("8 Manual toggle: vanilla setting follows", Check8);
		Run("6 Manual on: left/right shift, wrap, mixed", Check6);
		Run("11 Copy priorities and timetable from leader, caravan member included", Check11);
		Run("9 Eat/Sleep/Relax: only members in need, sent", Check9);
		Run("10 Resume: relaxing member back to work, eating member goes on", Check10);
		Run("9bis Resume leaves a patient waiting (Wait_MaintainPosture) alone", Check9bis);
		Run("9quater Drafted member, no food: stays drafted, counted as no food", Check9quater);
		RunSquadChecks();
	}

	private void Run(string name, Action<List<string>> check, bool keepErrors = false)
	{
		List<string> notes = new List<string>();
		if (!keepErrors)
		{
			Boot.Errors.Clear();
		}
		bool ok;
		try
		{
			check(notes);
			ok = !notes.Any((string n) => n.StartsWith("FAIL"));
		}
		catch (Exception ex)
		{
			notes.Add("FAIL exception " + ex);
			ok = false;
		}
		List<string> list = Boot.Errors.Where((string e) => e.Contains("Radius UI")).ToList();
		foreach (string item in list)
		{
			notes.Add("FAIL logged error: " + item.Split('\n')[0]);
			ok = false;
		}
		foreach (string item2 in Boot.Errors.Except(list))
		{
			notes.Add("note: other error logged: " + item2.Split('\n')[0]);
		}
		if (ok)
		{
			pass++;
		}
		else
		{
			fail++;
		}
		Line((ok ? "PASS " : "FAIL ") + name);
		foreach (string item3 in notes)
		{
			Line("    " + item3);
		}
	}

	private void Line(string s)
	{
		File.AppendAllText(path, s + "\n");
	}

	private static void Expect(List<string> notes, bool cond, string what)
	{
		notes.Add((cond ? "ok   " : "FAIL ") + what);
	}

	// ---- calls into the mod (internal types, by reflection) ----

	private object Call(Type t, string method, params object[] args)
	{
		MethodInfo methodInfo = AccessTools.Method(t, method) ?? throw new MissingMethodException(t.Name + "." + method);
		return methodInfo.Invoke(null, args);
	}

	private void WorkNow(int squad, WorkTypeDef w)
	{
		Call(workType, "WorkNow", squad, w);
	}

	private void Shift(int squad, WorkTypeDef w, int dir)
	{
		Call(workType, "ShiftPriority", squad, w, dir);
	}

	private void Pause(int squad, string pause)
	{
		Call(workType, "DoPause", squad, Enum.Parse(pauseType, pause));
	}

	private List<Pawn> PauseTargets(List<Pawn> able, string pause)
	{
		return (List<Pawn>)Call(workType, "PauseTargets", able, Enum.Parse(pauseType, pause));
	}

	private string LastMessage()
	{
		return Boot.Messages.Count > 0 ? Boot.Messages[Boot.Messages.Count - 1] : "(none)";
	}

	// ---- setup helpers ----

	private Pawn NewColonist(params WorkTypeDef[] capableOf)
	{
		for (int i = 0; i < 40; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer, null, forceGenerateNewPawn: true));
			if (pawn.workSettings != null && !pawn.workSettings.EverWork)
			{
				pawn.workSettings.EnableAndInitialize();
			}
			if (capableOf.Any((WorkTypeDef w) => pawn.WorkTypeIsDisabled(w)) || pawn.InMentalState)
			{
				pawn.Destroy();
				continue;
			}
			GenSpawn.Spawn(pawn, FreeCell(6), map);
			foreach (WorkTypeDef w in capableOf)
			{
				pawn.workSettings.SetPriority(w, 3);
			}
			pawn.needs.food.CurLevelPercentage = 1f;
			pawn.needs.rest.CurLevel = 1f;
			if (pawn.needs.joy != null)
			{
				pawn.needs.joy.CurLevel = 1f;
			}
			return pawn;
		}
		throw new Exception("no suitable colonist generated");
	}

	private int Squad(params Pawn[] pawns)
	{
		BarSquads get = BarSquads.Get;
		int num = get.NewSquad();
		foreach (Pawn p in pawns)
		{
			get.Assign(p, num);
		}
		return num;
	}

	private IntVec3 FreeCell(int radius, IntVec3? near = null)
	{
		IntVec3 center = near ?? home;
		for (int r = radius; r < 60; r += 4)
		{
			if (CellFinder.TryFindRandomCellNear(center, map, r, (IntVec3 c) => c.Standable(map) && !c.Fogged(map) && c.GetFirstItem(map) == null && c.GetFirstPawn(map) == null && c.GetPlant(map) == null && map.reachability.CanReach(home, c, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly), out var result))
			{
				return result;
			}
		}
		throw new Exception("no free cell");
	}

	private Plant Tree(IntVec3 cell)
	{
		Plant plant = (Plant)ThingMaker.MakeThing(ThingDef.Named("Plant_TreeOak"));
		GenSpawn.Spawn(plant, cell, map, WipeMode.Vanish);
		plant.Growth = 1f;
		map.designationManager.AddDesignation(new Designation(plant, DesignationDefOf.CutPlant));
		return plant;
	}

	private void Disable(Pawn p, WorkTypeDef w)
	{
		// The cached list of disabled work types: WorkTypeIsDisabled reads it until the game rebuilds it.
		List<WorkTypeDef> disabledWorkTypes = p.GetDisabledWorkTypes();
		if (!disabledWorkTypes.Contains(w))
		{
			disabledWorkTypes.Add(w);
		}
	}

	private static int DoingCount(IEnumerable<Pawn> pawns, WorkTypeDef w)
	{
		return pawns.Count((Pawn p) => p.CurJob?.workGiverDef?.workType == w);
	}

	private static string JobOf(Pawn p)
	{
		return p.LabelShort + "=" + (p.CurJob?.def.defName ?? "none") + (p.CurJob?.targetA.Thing != null ? "@" + p.CurJob.targetA.Thing.ThingID : "");
	}

	// ---- checks ----

	private void Check2(List<string> notes)
	{
		Pawn[] array = new Pawn[3]
		{
			NewColonist(PlantCutting),
			NewColonist(PlantCutting),
			NewColonist(PlantCutting)
		};
		for (int i = 0; i < 6; i++)
		{
			Tree(FreeCell(10));
		}
		int squad = Squad(array);
		WorkNow(squad, PlantCutting);
		notes.Add("jobs: " + string.Join(", ", array.Select(JobOf)));
		notes.Add("message: " + LastMessage());
		Expect(notes, array.All((Pawn p) => p.CurJob?.workGiverDef?.workType == PlantCutting), "all three on a plant cutting job");
		List<Thing> source = array.Select((Pawn p) => p.CurJob?.targetA.Thing).ToList();
		Expect(notes, source.All((Thing t) => t != null) && source.Distinct().Count() == 3, "three different trees");
		Expect(notes, DoingCount(array, PlantCutting) == 3, "badge count (members on this work type) = 3");
		Expect(notes, LastMessage().Contains(": 3 at work"), "message counts 3 at work");
	}

	private void Check3(List<string> notes)
	{
		Pawn pawn = NewColonist(PlantCutting);
		Pawn pawn2 = NewColonist(PlantCutting);
		Pawn pawn3 = NewColonist(PlantCutting);
		Pawn pawn4 = NewColonist(PlantCutting);
		Disable(pawn3, PlantCutting);
		pawn4.workSettings.SetPriority(PlantCutting, 0);
		for (int i = 0; i < 6; i++)
		{
			Tree(FreeCell(10));
		}
		int squad = Squad(pawn, pawn2, pawn3, pawn4);
		Expect(notes, pawn3.WorkTypeIsDisabled(PlantCutting), "setup: one member incapable");
		WorkNow(squad, PlantCutting);
		notes.Add("jobs: " + string.Join(", ", new Pawn[4] { pawn, pawn2, pawn3, pawn4 }.Select(JobOf)));
		notes.Add("message: " + LastMessage());
		Expect(notes, pawn.CurJob?.workGiverDef?.workType == PlantCutting && pawn2.CurJob?.workGiverDef?.workType == PlantCutting, "the two assigned members sent");
		Expect(notes, pawn3.CurJob?.workGiverDef?.workType != PlantCutting, "incapable member not sent");
		Expect(notes, pawn4.CurJob?.workGiverDef?.workType != PlantCutting, "priority 0 member not sent");
		Expect(notes, LastMessage().Contains(": 2 at work") && LastMessage().Contains("1 not assigned") && LastMessage().Contains("1 incapable"), "message: 2 at work, 1 not assigned, 1 incapable");
	}

	private void Check3bis(List<string> notes)
	{
		Pawn pawn = NewColonist(PlantCutting);
		Plant plant = Tree(FreeCell(2, pawn.Position));
		Plant plant2 = null;
		for (int i = 0; i < 20; i++)
		{
			IntVec3 intVec = FreeCell(12, pawn.Position);
			if (intVec.DistanceTo(pawn.Position) > plant.Position.DistanceTo(pawn.Position) + 4f)
			{
				plant2 = Tree(intVec);
				break;
			}
		}
		if (plant2 == null)
		{
			notes.Add("FAIL setup: no far cell for the tree inside the area");
			return;
		}
		if (!map.areaManager.TryMakeNewAllowed(out var area))
		{
			notes.Add("FAIL setup: no allowed area slot");
			return;
		}
		foreach (IntVec3 item in GenRadial.RadialCellsAround(plant2.Position, 3f, useCenter: true))
		{
			if (item.InBounds(map))
			{
				area[item] = true;
			}
		}
		pawn.playerSettings.AreaRestrictionInPawnCurrentMap = area;
		int squad = Squad(pawn);
		WorkNow(squad, PlantCutting);
		notes.Add("near tree (outside) " + plant.ThingID + " at " + plant.Position.DistanceTo(pawn.Position).ToString("F1") + ", far tree (inside) " + plant2.ThingID + "; " + JobOf(pawn));
		notes.Add("message: " + LastMessage());
		// Trees of earlier checks may also stand in the area: any designated tree inside it is right.
		Thing thing = pawn.CurJob?.targetA.Thing;
		Expect(notes, thing is Plant && area[thing.Position] && thing != plant, "task is a tree inside the allowed area" + ((thing != null && thing != plant2) ? " (an earlier check's tree in the area)" : ""));
		pawn.playerSettings.AreaRestrictionInPawnCurrentMap = null;
	}

	private void Check4(List<string> notes)
	{
		Pawn[] array = new Pawn[2]
		{
			NewColonist(Warden),
			NewColonist(Warden)
		};
		int squad = Squad(array);
		List<Job> before = array.Select((Pawn p) => p.CurJob).ToList();
		WorkNow(squad, Warden);
		notes.Add("message: " + LastMessage());
		Expect(notes, array.Select((Pawn p) => p.CurJob).SequenceEqual(before), "no member's job changed");
		Expect(notes, array.All((Pawn p) => !p.Drafted), "nobody drafted or moved");
		Expect(notes, LastMessage().Contains(": 0 at work") && LastMessage().Contains("2 found no task"), "message: 0 at work, 2 found no task");
	}

	private void Check5(List<string> notes)
	{
		Pawn[] array = new Pawn[2]
		{
			NewColonist(PlantCutting),
			NewColonist(PlantCutting)
		};
		for (int i = 0; i < 4; i++)
		{
			Tree(FreeCell(10));
		}
		int squad = Squad(array);
		Call(ordersType, "Assault", squad, AssaultMode.Strongest);
		SquadAssault get = SquadAssault.Get;
		Expect(notes, array.All((Pawn p) => p.Drafted && get.TryGetMode(p, out var _)), "setup: drafted and assaulting");
		WorkNow(squad, PlantCutting);
		notes.Add("jobs: " + string.Join(", ", array.Select(JobOf)));
		notes.Add("message: " + LastMessage());
		Expect(notes, array.All((Pawn p) => !p.Drafted), "members undrafted");
		Expect(notes, array.All((Pawn p) => !get.TryGetMode(p, out var _)), "assault ended");
		Expect(notes, array.All((Pawn p) => p.CurJob?.workGiverDef?.workType == PlantCutting), "members on plant cutting");
	}

	private void Check7(List<string> notes)
	{
		SetManual(false);
		Pawn[] array = new Pawn[3]
		{
			NewColonist(PlantCutting),
			NewColonist(PlantCutting),
			NewColonist(PlantCutting)
		};
		int squad = Squad(array);
		Shift(squad, PlantCutting, -1);
		notes.Add("after 1st click: " + Prios(array));
		Expect(notes, array.All((Pawn p) => p.workSettings.GetPriority(PlantCutting) == 0), "on for all, click: off for all");
		Shift(squad, PlantCutting, -1);
		notes.Add("after 2nd click: " + Prios(array));
		Expect(notes, array.All((Pawn p) => p.workSettings.WorkIsActive(PlantCutting)), "off for all, click: on for all");
		array[1].workSettings.SetPriority(PlantCutting, 0);
		Shift(squad, PlantCutting, 1);
		notes.Add("mixed, after click: " + Prios(array));
		Expect(notes, array.All((Pawn p) => p.workSettings.WorkIsActive(PlantCutting)), "mixed, click: on for all");
	}

	private void Check8(List<string> notes)
	{
		Pawn pawn = NewColonist(PlantCutting);
		SetManual(true);
		Expect(notes, Find.PlaySettings.useWorkPriorities, "manual on: PlaySettings.useWorkPriorities true");
		pawn.workSettings.SetPriority(PlantCutting, 1);
		SetManual(false);
		Expect(notes, !Find.PlaySettings.useWorkPriorities, "manual off: PlaySettings.useWorkPriorities false");
		notes.Add("priority 1 after manual off reads " + pawn.workSettings.GetPriority(PlantCutting));
		Expect(notes, pawn.workSettings.GetPriority(PlantCutting) == 3, "pawn told: priority 1 becomes 3, as with the vanilla checkbox");
		SetManual(true);
	}

	private void Check6(List<string> notes)
	{
		SetManual(true);
		Pawn[] array = new Pawn[3]
		{
			NewColonist(PlantCutting),
			NewColonist(PlantCutting),
			NewColonist(PlantCutting)
		};
		int squad = Squad(array);
		foreach (Pawn p in array)
		{
			p.workSettings.SetPriority(PlantCutting, 3);
		}
		Shift(squad, PlantCutting, -1);
		Expect(notes, All(array, 2), "3, left click: 2 for all (" + Prios(array) + ")");
		Shift(squad, PlantCutting, 1);
		Expect(notes, All(array, 3), "2, right click: 3 for all (" + Prios(array) + ")");
		Set(array, 1);
		Shift(squad, PlantCutting, -1);
		Expect(notes, All(array, 0), "1, left click: 0 for all, as the Work tab (" + Prios(array) + ")");
		Shift(squad, PlantCutting, -1);
		Expect(notes, All(array, 4), "0, left click: wraps to 4 (" + Prios(array) + ")");
		Shift(squad, PlantCutting, 1);
		Expect(notes, All(array, 0), "4, right click: wraps to 0 (" + Prios(array) + ")");
		Set(array, 2);
		array[2].workSettings.SetPriority(PlantCutting, 4);
		object[] array2 = new object[4]
		{
			array.ToList(),
			PlantCutting,
			false,
			0
		};
		AccessTools.Method(workType, "SharedPriority").Invoke(null, array2);
		Expect(notes, (bool)array2[2], "mixed values detected (shown as Mixed)");
		Shift(squad, PlantCutting, -1);
		Expect(notes, All(array, 1), "mixed from first member's 2, left click: 1 for all (" + Prios(array) + ")");
	}

	private void Check11(List<string> notes)
	{
		SetManual(true);
		WorkTypeDef w = WorkTypeDefOf.Hauling;
		Pawn pawn = NewColonist(PlantCutting, w);
		Pawn pawn2 = NewColonist(PlantCutting, w);
		Pawn pawn3 = NewColonist(PlantCutting, w);
		int num = Squad(pawn, pawn2, pawn3);
		Caravan caravan = CaravanMaker.MakeCaravan(new Pawn[1] { pawn3 }, Faction.OfPlayer, map.Tile, addToWorldPawnsIfNotAlready: true);
		Expect(notes, pawn3.GetCaravan() == caravan && !pawn3.Spawned, "setup: one member in a caravan");
		Pawn leader = (Pawn)Call(workType, "Leader", num);
		Pawn pawn4 = leader == pawn ? pawn2 : pawn;
		notes.Add("leader " + leader.LabelShort);
		foreach (WorkTypeDef allDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
		{
			if (!leader.WorkTypeIsDisabled(allDef))
			{
				leader.workSettings.SetPriority(allDef, allDef.index % 5);
			}
		}
		pawn4.workSettings.SetPriority(w, 2);
		pawn3.workSettings.SetPriority(w, 2);
		leader.workSettings.SetPriority(w, 4);
		Disable(leader, w);
		for (int i = 0; i < 24; i++)
		{
			leader.timetable.SetAssignment(i, (i < 6) ? TimeAssignmentDefOf.Sleep : ((i < 14) ? TimeAssignmentDefOf.Work : ((i < 18) ? TimeAssignmentDefOf.Joy : TimeAssignmentDefOf.Anything)));
		}
		Call(workType, "CopyPriorities", num);
		notes.Add("message: " + LastMessage());
		Call(workType, "CopyTimetable", num);
		notes.Add("message: " + LastMessage());
		foreach (Pawn item in new Pawn[2] { pawn4, pawn3 })
		{
			List<string> list = (from d in DefDatabase<WorkTypeDef>.AllDefsListForReading
				where d != w && !leader.WorkTypeIsDisabled(d) && !item.WorkTypeIsDisabled(d) && item.workSettings.GetPriority(d) != leader.workSettings.GetPriority(d)
				select d.defName).ToList();
			Expect(notes, list.Count == 0, item.LabelShort + (item == pawn3 ? " (caravan)" : "") + ": priorities equal to the leader's" + (list.Count > 0 ? " except " + string.Join(",", list) : ""));
			Expect(notes, item.workSettings.GetPriority(w) == 2, item.LabelShort + ": type the leader cannot do keeps its own priority (2), reads " + item.workSettings.GetPriority(w));
			Expect(notes, Enumerable.Range(0, 24).All((int h) => item.timetable.GetAssignment(h) == leader.timetable.GetAssignment(h)), item.LabelShort + ": timetable equal to the leader's");
		}
		Expect(notes, LastMessage().Contains("2"), "timetable message counts 2 members");
	}

	// Packaged survival meals near home: whether the quick test map has food of its own is not under test.
	private void Meals(List<string> notes)
	{
		notes.Add("food on map before: " + map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree).Count((Thing t) => !(t is Pawn)));
		Thing thing = ThingMaker.MakeThing(ThingDef.Named("MealSurvivalPack"));
		thing.stackCount = 10;
		GenPlace.TryPlaceThing(thing, FreeCell(4), map, ThingPlaceMode.Near);
	}

	private void Check9(List<string> notes)
	{
		Meals(notes);
		// A recreation spot, so Relax has something to send the member to.
		Thing pin = ThingMaker.MakeThing(ThingDef.Named("HorseshoesPin"), ThingDefOf.WoodLog);
		pin.SetFaction(Faction.OfPlayer);
		GenSpawn.Spawn(pin, FreeCell(8), map, Rot4.North, WipeMode.Vanish);
		for (int i = 0; i < 2; i++)
		{
			IntVec3 c = FreeCell(8);
			Building_Bed building_Bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
			building_Bed.SetFaction(Faction.OfPlayer);
			GenSpawn.Spawn(building_Bed, c, map, Rot4.North, WipeMode.Vanish);
		}
		foreach (string text in new string[3] { "Eat", "Sleep", "Joy" })
		{
			Pawn pawn = NewColonist();
			Pawn pawn2 = NewColonist();
			if (pawn.needs.joy == null || pawn2.needs.joy == null)
			{
				notes.Add("FAIL setup: generated colonist without a joy need");
				return;
			}
			switch (text)
			{
			case "Eat":
				pawn.needs.food.CurLevelPercentage = 0.3f;
				break;
			case "Sleep":
				pawn.needs.rest.CurLevel = 0.2f;
				break;
			default:
				pawn.needs.joy.CurLevel = 0.1f;
				break;
			}
			int squad = Squad(pawn, pawn2);
			List<Pawn> list = PauseTargets(new List<Pawn> { pawn, pawn2 }, text);
			Expect(notes, list.Count == 1 && list[0] == pawn, text + ": only the member in need is counted (" + list.Count + ")");
			Pause(squad, text);
			notes.Add(text + " jobs: " + JobOf(pawn) + ", " + JobOf(pawn2) + "; message: " + LastMessage());
			bool flag = text switch
			{
				"Eat" => pawn.CurJobDef == JobDefOf.Ingest,
				"Sleep" => pawn.CurJobDef == JobDefOf.LayDown,
				_ => pawn.CurJob?.def.joyKind != null,
			};
			bool flag2 = LastMessage().Contains("found nothing") || LastMessage().Contains("found no");
			Expect(notes, flag || (text == "Joy" && flag2), text + ": member in need sent" + (text == "Joy" ? " (or counted as found nothing on this bare map)" : ""));
			Expect(notes, pawn2.CurJob == null || pawn2.CurJobDef != pawn.CurJobDef || text == "Joy", text + ": member not in need left alone");
		}
	}

	private void Check10(List<string> notes)
	{
		Meals(notes);
		Pawn pawn = NewColonist();
		Pawn pawn2 = NewColonist();
		pawn2.needs.food.CurLevelPercentage = 0.3f;
		int squad = Squad(pawn, pawn2);
		Pause(squad, "Eat");
		pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_Wander), JobCondition.InterruptForced);
		Expect(notes, pawn2.CurJobDef == JobDefOf.Ingest && pawn.CurJobDef == JobDefOf.Wait_Wander, "setup: one eating, one idling (" + JobOf(pawn2) + ", " + JobOf(pawn) + ")");
		Pause(squad, "Resume");
		notes.Add("after Resume: " + JobOf(pawn) + ", " + JobOf(pawn2) + "; message: " + LastMessage());
		Expect(notes, pawn.CurJobDef != JobDefOf.Wait_Wander, "idling member's wait ended (back to work)");
		Expect(notes, pawn2.CurJobDef == JobDefOf.Ingest, "eating member goes on eating");
	}

	private void Check9bis(List<string> notes)
	{
		Pawn pawn = NewColonist();
		int squad = Squad(pawn);
		pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture), JobCondition.InterruptForced);
		if (pawn.CurJobDef != JobDefOf.Wait_MaintainPosture)
		{
			notes.Add("FAIL setup: Wait_MaintainPosture did not start (" + JobOf(pawn) + ")");
			return;
		}
		Job curJob = pawn.CurJob;
		Pause(squad, "Resume");
		notes.Add("after Resume: " + JobOf(pawn));
		Expect(notes, pawn.CurJob == curJob, "patient's wait not interrupted");
	}

	private void Check9quater(List<string> notes)
	{
		foreach (Thing item in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree).ToList())
		{
			if (!(item is Pawn) && item.Spawned)
			{
				item.Destroy();
			}
		}
		Pawn pawn = NewColonist();
		pawn.needs.food.CurLevelPercentage = 0.3f;
		int squad = Squad(pawn);
		pawn.drafter.Drafted = true;
		Pause(squad, "Eat");
		notes.Add(JobOf(pawn) + "; message: " + LastMessage());
		Expect(notes, pawn.Drafted, "member stays drafted");
		Expect(notes, LastMessage().Contains("1 found no food"), "message counts 1 found no food");
	}

	// ---- small helpers ----

	private void SetManual(bool on)
	{
		Call(workType, "SetManual", on);
	}

	private static string Prios(IEnumerable<Pawn> pawns)
	{
		return string.Join("/", pawns.Select((Pawn p) => p.workSettings.GetPriority(PlantCutting)));
	}

	private static bool All(IEnumerable<Pawn> pawns, int prio)
	{
		return pawns.All((Pawn p) => p.workSettings.GetPriority(PlantCutting) == prio);
	}

	private static void Set(IEnumerable<Pawn> pawns, int prio)
	{
		foreach (Pawn pawn in pawns)
		{
			pawn.workSettings.SetPriority(PlantCutting, prio);
		}
	}
}
