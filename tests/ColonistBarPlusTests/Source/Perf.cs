using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using RadiusColonistBar;
using RimWorld;
using UnityEngine;
using Verse;

namespace RCBPlusTests;

// Performance run (-rcbperf=<file>): 20-member squad, order card open on the Work tab, game running at normal speed.
// Measures the per-call cost of the mod's hot paths over 1200 frames and writes mean / p95 / max, then quits.
public class PerfRunner : GameComponent
{
	private static string path;

	// Short window: the instance runs occluded, so macOS throttles it to a few fps; 400 frames give the card probe
	// hundreds of samples and the per-call probes thousands. MaxSeconds caps the run when throttling is extreme.
	private const int Warmup = 60;

	private const int Frames = 400;

	private const float MaxSeconds = 120f;

	private bool ready;

	private int frames;

	private int measured;

	public PerfRunner(Game game)
	{
	}

	public static void Bind(Harmony harmony)
	{
		if (!GenCommandLine.TryGetCommandLineArg("rcbperf", out path))
		{
			return;
		}
		Probe.Add(harmony, AccessTools.Method(typeof(SquadOrderCard), "DoWindowContents"), "SquadOrderCard.DoWindowContents (per frame)");
		Probe.Add(harmony, AccessTools.Method(AccessTools.TypeByName("RadiusColonistBar.SquadStatus"), "Of"), "SquadStatus.Of (per call)");
		Probe.Add(harmony, AccessTools.Method(typeof(BarSquads), "GameComponentUpdate"), "BarSquads.GameComponentUpdate (per frame)");
		Probe.Add(harmony, AccessTools.Method(typeof(ColonistBar), "ColonistBarOnGUI"), "ColonistBar.ColonistBarOnGUI, bar + patches (per event)");
	}

	public override void GameComponentUpdate()
	{
		if (path == null || Find.CurrentMap == null || LongEventHandler.ShouldWaitForEvent)
		{
			return;
		}
		if (!ready)
		{
			if (++frames < 120)
			{
				return;
			}
			ready = true;
			try
			{
				Setup();
			}
			catch (Exception ex)
			{
				File.WriteAllText(path, "CRASH setup " + ex + "\n");
				Root.Shutdown();
			}
			return;
		}
		if (++measured == Warmup)
		{
			Probe.ResetAll();
			resetAt = Time.realtimeSinceStartup;
		}
		if (measured < Warmup + Frames && (measured < Warmup || Time.realtimeSinceStartup - resetAt < MaxSeconds))
		{
			return;
		}
		WriteReport();
	}

	private float resetAt;

	private void Setup()
	{
		Fixture.Bind();
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < 20; i++)
		{
			list.Add(Fixture.NewColonist(WorkTypeDefOf.PlantCutting));
		}
		for (int j = 0; j < 10; j++)
		{
			Fixture.Tree(Fixture.FreeCell(12));
		}
		int index = Fixture.Squad(list.ToArray());
		AccessTools.StaticFieldRefAccess<int>(typeof(SquadOrderCard), "tab") = 2;
		AccessTools.Method(typeof(SquadOrderCard), "Toggle").Invoke(null, new object[2]
		{
			index,
			new Rect(150f, 120f, 0f, 0f)
		});
		Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
	}

	private void WriteReport()
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		sb.Append("# Colonist Bar Plus perf, 20-member squad, card open on Work tab, " + Frames + " frames\n");
		sb.Append("# mean fps over the window: " + (Frames / (Time.realtimeSinceStartup - resetAt)).ToString("F0") + "\n");
		foreach (Probe probe in Probe.All)
		{
			sb.Append(probe.Report() + "\n");
		}
		File.WriteAllText(path, sb.ToString());
		Root.Shutdown();
	}
}

// One measured method: a Harmony prefix/postfix pair around it, microseconds per call kept in full.
internal class Probe
{
	internal static readonly List<Probe> All = new List<Probe>();

	private readonly string label;

	private readonly List<double> micros = new List<double>(100000);

	// One stopwatch per probe: SquadStatus.Of runs inside the card and bar draws, so a shared watch would nest.
	private static readonly Stopwatch w0 = new Stopwatch();

	private static readonly Stopwatch w1 = new Stopwatch();

	private static readonly Stopwatch w2 = new Stopwatch();

	private static readonly Stopwatch w3 = new Stopwatch();

	internal static void Add(Harmony harmony, System.Reflection.MethodBase target, string label)
	{
		Probe probe = new Probe(label);
		All.Add(probe);
		int index = All.Count - 1;
		harmony.Patch(target, new HarmonyMethod(typeof(Probe), "Before" + index), new HarmonyMethod(typeof(Probe), "After" + index));
	}

	private Probe(string label)
	{
		this.label = label;
	}

	internal static void ResetAll()
	{
		foreach (Probe probe in All)
		{
			probe.micros.Clear();
		}
	}

	// One static pair and one stopwatch per probe: Harmony needs distinct methods.
	public static void Before0()
	{
		w0.Restart();
	}

	public static void After0()
	{
		All[0].micros.Add(w0.Elapsed.TotalMilliseconds * 1000.0);
	}

	public static void Before1()
	{
		w1.Restart();
	}

	public static void After1()
	{
		All[1].micros.Add(w1.Elapsed.TotalMilliseconds * 1000.0);
	}

	public static void Before2()
	{
		w2.Restart();
	}

	public static void After2()
	{
		All[2].micros.Add(w2.Elapsed.TotalMilliseconds * 1000.0);
	}

	public static void Before3()
	{
		w3.Restart();
	}

	public static void After3()
	{
		All[3].micros.Add(w3.Elapsed.TotalMilliseconds * 1000.0);
	}

	internal string Report()
	{
		if (micros.Count == 0)
		{
			return label + ": no call";
		}
		List<double> sorted = micros.OrderBy((double m) => m).ToList();
		double mean = sorted.Average();
		double p95 = sorted[(int)(sorted.Count * 0.95)];
		double max = sorted[sorted.Count - 1];
		return label + ": calls " + sorted.Count + ", mean " + mean.ToString("F1") + " us, p95 " + p95.ToString("F1") + " us, max " + max.ToString("F0") + " us, total " + (sorted.Sum() / 1000.0).ToString("F0") + " ms";
	}
}
