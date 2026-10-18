using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RCBPlusScene;

// Measurement scenes (-rcbscene=<dir>): grows the quick test colony to N colonists, sends raiders, sets the game speed,
// so the same scene can be profiled with Colonist Bar Plus and with the vanilla bar. No Harmony patch, no reference to
// the mod under test: whatever a profiler shows is the game and the mods loaded, not this helper. Commands come from
// <dir>/scene.txt ("<id> <verb> [args]"), acks go to <dir>/scene-ack.txt, state to <dir>/scene-state.json.
public class SceneServer : GameComponent
{
	private static string dir;

	private bool ready;

	private int frames;

	private long lastCmd = -1;

	private int ackFrame = -1;

	private string ackText;

	private float fpsAccum;

	private int fpsFrames;

	private float fps;

	public SceneServer(Game game)
	{
		if (dir == null && GenCommandLine.TryGetCommandLineArg("rcbscene", out dir))
		{
			Application.runInBackground = true;
		}
	}

	public override void GameComponentUpdate()
	{
		if (dir == null || Find.CurrentMap == null || LongEventHandler.ShouldWaitForEvent)
		{
			return;
		}
		fpsAccum += Time.unscaledDeltaTime;
		fpsFrames++;
		if (fpsAccum >= 1f)
		{
			fps = fpsFrames / fpsAccum;
			fpsAccum = 0f;
			fpsFrames = 0;
		}
		if (!ready)
		{
			if (++frames < 120)
			{
				return;
			}
			ready = true;
			Find.TickManager.Pause();
			WriteState();
			File.WriteAllText(Path.Combine(dir, "scene-ack.txt"), "0 ready colonists=" + Find.CurrentMap.mapPawns.FreeColonistsSpawnedCount);
			return;
		}
		if (ackFrame >= 0 && Time.frameCount >= ackFrame)
		{
			File.WriteAllText(Path.Combine(dir, "scene-ack.txt"), ackText);
			ackFrame = -1;
		}
		if (Time.frameCount % 30 == 0)
		{
			WriteState();
		}
		if (Time.frameCount % 10 != 0)
		{
			return;
		}
		string path = Path.Combine(dir, "scene.txt");
		if (!File.Exists(path))
		{
			return;
		}
		string[] array;
		try
		{
			array = File.ReadAllText(path).Trim().Split(new char[1] { ' ' }, 3);
		}
		catch (IOException)
		{
			return;
		}
		if (array.Length < 2 || !long.TryParse(array[0], out var id) || id <= lastCmd)
		{
			return;
		}
		lastCmd = id;
		string note = "ok";
		int delay = 1;
		try
		{
			switch (array[1])
			{
			case "colonists":
				note = "colonists=" + Colonists(int.Parse(array[2]));
				break;
			case "raid":
				note = "raiders=" + Raid(int.Parse(array[2]));
				break;
			case "speed":
				Find.TickManager.CurTimeSpeed = (TimeSpeed)int.Parse(array[2]);
				break;
			case "shot":
				ScreenCapture.CaptureScreenshot(Path.Combine(dir, array[2]));
				delay = 5;
				break;
			case "quit":
				File.WriteAllText(Path.Combine(dir, "scene-ack.txt"), id + " bye");
				Root.Shutdown();
				return;
			default:
				note = "unknown verb";
				break;
			}
		}
		catch (Exception ex)
		{
			note = "failed: " + ex.Message;
		}
		ackText = id + " " + note;
		ackFrame = Time.frameCount + delay;
	}

	private static int Colonists(int target)
	{
		Map map = Find.CurrentMap;
		IntVec3 home = map.mapPawns.FreeColonistsSpawned.FirstOrDefault()?.Position ?? map.Center;
		int guard = 0;
		while (map.mapPawns.FreeColonistsSpawnedCount < target && guard++ < target * 4)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer, null, forceGenerateNewPawn: true));
			if (pawn.InMentalState)
			{
				pawn.Destroy();
				continue;
			}
			if (pawn.workSettings != null && !pawn.workSettings.EverWork)
			{
				pawn.workSettings.EnableAndInitialize();
			}
			IntVec3 cell = CellFinder.RandomClosewalkCellNear(home, map, 8);
			GenSpawn.Spawn(pawn, cell, map);
		}
		return map.mapPawns.FreeColonistsSpawnedCount;
	}

	// A small pirate band that assaults the colony, so drafted squads and their think tree nodes have work.
	private static int Raid(int count)
	{
		Map map = Find.CurrentMap;
		Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Pirate) ?? Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: false);
		PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Pirate") ?? faction.def.basicMemberKind;
		IntVec3 edge = CellFinder.RandomEdgeCell(map);
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < count; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, null, forceGenerateNewPawn: true));
			GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(edge, map, 6), map);
			list.Add(pawn);
		}
		LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), map, list);
		return map.mapPawns.AllPawnsSpawned.Count((Pawn p) => p.HostileTo(Faction.OfPlayer) && !p.Dead && !p.Downed);
	}

	private void WriteState()
	{
		Map map = Find.CurrentMap;
		string text = "{\"frame\":" + Time.frameCount
			+ ",\"fps\":" + fps.ToString("F1", CultureInfo.InvariantCulture)
			+ ",\"colonists\":" + map.mapPawns.FreeColonistsSpawnedCount
			+ ",\"drafted\":" + map.mapPawns.FreeColonistsSpawned.Count((Pawn p) => p.Drafted)
			+ ",\"hostiles\":" + map.mapPawns.AllPawnsSpawned.Count((Pawn p) => p.HostileTo(Faction.OfPlayer) && !p.Dead && !p.Downed)
			+ ",\"speed\":" + (int)Find.TickManager.CurTimeSpeed
			+ ",\"paused\":" + (Find.TickManager.Paused ? "true" : "false")
			+ ",\"ticks\":" + Find.TickManager.TicksGame
			+ ",\"windows\":" + Find.WindowStack.Count + "}";
		File.WriteAllText(Path.Combine(dir, "scene-state.json"), text);
	}
}
