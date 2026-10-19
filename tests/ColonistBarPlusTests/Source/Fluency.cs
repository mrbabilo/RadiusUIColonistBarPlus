using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using RadiusColonistBar;
using UnityEngine;
using Verse;

namespace RCBPlusTests;

// Smoothness probe ("fluency <tab 0|1|2> [frames]", after squadall): alternates every 600 frames between the order
// card closed and the card open on the given tab, on the same map and at the same moment of the game, so the game's
// own allocations cancel out (separate quick-test games draw different maps and colonists and do not compare).
// Per state it records memory allocated per frame (sum of the rises of GC.GetTotalMemory: the per-thread counter is
// always 0 under Unity's Boehm GC), gen-0 collections and frame times. The frame-rate cap is lifted during the run,
// otherwise every frame lasts 33 ms and frame times say nothing. The first 30 frames after each switch are reported
// apart, the very first opening of the card in the game on its own line: they hold the one-off cost of opening it.
internal class Fluency
{
	private const int Block = 600;

	private const int SwitchFrames = 30;

	private class Bucket
	{
		public readonly List<float> Ms = new List<float>();

		public readonly List<double> BlockKb = new List<double>();

		public readonly List<float> BlockSwitchMax = new List<float>();

		public int Frames;

		public long Bytes;

		public int Collections;

		public float SwitchMax;

		public long SwitchBytes;
	}

	private readonly int squad;

	private readonly int tab;

	private readonly int total;

	private readonly string outPath;

	private readonly Bucket closed = new Bucket();

	private readonly Bucket open = new Bucket();

	private readonly int wasVSync;

	private readonly int wasTargetFps;

	private readonly bool openedBefore;

	private float firstOpenMax = -1f;

	private int frame = -1;

	private long lastMem;

	private int lastGc;

	private long blockBytes;

	private float blockSwitchMax;

	private string seen;

	public bool Done { get; private set; }

	public Fluency(int squad, int tab, int total, string dir)
	{
		this.squad = squad;
		this.tab = tab;
		this.total = Math.Max(Block * 2, total / (Block * 2) * Block * 2);
		outPath = Path.Combine(dir, "fluency-tab" + tab + ".txt");
		if (File.Exists(outPath))
		{
			File.Delete(outPath);
		}
		openedBefore = opened;
		wasVSync = QualitySettings.vSyncCount;
		wasTargetFps = Application.targetFrameRate;
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = -1;
	}

	// Whether the card has been opened by a probe earlier in this game: only the first opening carries the JIT and
	// texture cost.
	private static bool opened;

	public void Update()
	{
		if (Done)
		{
			return;
		}
		if (frame < 0)
		{
			Apply(state: false);
			frame = 0;
			lastMem = GC.GetTotalMemory(forceFullCollection: false);
			lastGc = GC.CollectionCount(0);
			return;
		}
		long mem = GC.GetTotalMemory(forceFullCollection: false);
		int gc = GC.CollectionCount(0);
		float ms = Time.unscaledDeltaTime * 1000f;
		bool state = frame / Block % 2 == 1;
		int inBlock = frame % Block;
		Bucket bucket = state ? open : closed;
		long rise = Math.Max(0L, mem - lastMem);
		bucket.Frames++;
		bucket.Bytes += rise;
		bucket.Collections += gc - lastGc;
		if (inBlock < SwitchFrames)
		{
			blockSwitchMax = Math.Max(blockSwitchMax, ms);
			if (state && frame < Block * 2 && !openedBefore)
			{
				firstOpenMax = Math.Max(firstOpenMax, ms);
			}
			else
			{
				bucket.SwitchMax = Math.Max(bucket.SwitchMax, ms);
			}
			bucket.SwitchBytes += rise;
		}
		else
		{
			bucket.Ms.Add(ms);
		}
		if (state && inBlock == 60 && seen == null)
		{
			SquadOrderCard card = Card();
			seen = card == null ? "card not open" : "card open, tab " + AccessTools.StaticFieldRefAccess<int>(typeof(SquadOrderCard), "tab") + ", " + card.windowRect.width + "x" + card.windowRect.height;
		}
		blockBytes += rise;
		lastMem = mem;
		lastGc = gc;
		frame++;
		if (frame % Block == 0)
		{
			bucket.BlockKb.Add(blockBytes / 1024.0 / Block);
			bucket.BlockSwitchMax.Add(blockSwitchMax);
			blockBytes = 0L;
			blockSwitchMax = 0f;
			if (frame >= total)
			{
				Finish();
				return;
			}
			Apply(!state);
		}
	}

	private static SquadOrderCard Card()
	{
		return (SquadOrderCard)AccessTools.StaticFieldRefAccess<object>(typeof(SquadOrderCard), "open");
	}

	private void Apply(bool state)
	{
		Card()?.Close(doCloseSound: false);
		if (state)
		{
			AccessTools.StaticFieldRefAccess<int>(typeof(SquadOrderCard), "tab") = tab;
			AccessTools.Method(typeof(SquadOrderCard), "Toggle").Invoke(null, new object[2]
			{
				squad,
				new Rect(150f, 116f, 0f, 0f)
			});
			opened = true;
		}
	}

	private void Finish()
	{
		Done = true;
		Card()?.Close(doCloseSound: false);
		QualitySettings.vSyncCount = wasVSync;
		Application.targetFrameRate = wasTargetFps;
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("tab " + tab + ", " + total + " frames, " + Block + " per block, speed " + Find.TickManager.CurTimeSpeed + ", squad members " + Find.CurrentMap.mapPawns.FreeColonistsSpawnedCount);
		sb.AppendLine("open showed: " + seen);
		sb.AppendLine(firstOpenMax >= 0f ? string.Format(CultureInfo.InvariantCulture, "first opening of the card in this game: worst frame {0:F1} ms", firstOpenMax) : "first opening: the card was already opened earlier in this game");
		sb.AppendLine("state    KB/frame  per block                 gen0   p50    p95    p99    max  >25ms >40ms | switch: max ms, KB");
		Line(sb, "closed", closed);
		Line(sb, "open", open);
		sb.AppendLine("worst frame after each switch, per block: closed " + string.Join(" ", closed.BlockSwitchMax.Select((float x) => x.ToString("F1", CultureInfo.InvariantCulture)))
			+ ", open " + string.Join(" ", open.BlockSwitchMax.Select((float x) => x.ToString("F1", CultureInfo.InvariantCulture))));
		sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "open - closed: {0:+0.0;-0.0} KB/frame", Kb(open) - Kb(closed)));
		File.WriteAllText(outPath, sb.ToString());
	}

	private static double Kb(Bucket b)
	{
		return b.Bytes / 1024.0 / Math.Max(1, b.Frames);
	}

	private static void Line(StringBuilder sb, string name, Bucket b)
	{
		List<float> ms = b.Ms.OrderBy((float x) => x).ToList();
		string blocks = string.Join(" ", b.BlockKb.Select((double x) => x.ToString("F1", CultureInfo.InvariantCulture)));
		sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-8} {1,8:F1}  {2,-25} {3,4} {4,6:F1} {5,6:F1} {6,6:F1} {7,6:F1} {8,5} {9,5} | {10:F1} ms, {11:F1} KB",
			name, Kb(b), blocks, b.Collections, Pct(ms, 0.5f), Pct(ms, 0.95f), Pct(ms, 0.99f), ms.Count > 0 ? ms[ms.Count - 1] : 0f,
			ms.Count((float x) => x > 25f), ms.Count((float x) => x > 40f), b.SwitchMax, b.SwitchBytes / 1024.0));
	}

	private static float Pct(List<float> sorted, float p)
	{
		return sorted.Count == 0 ? 0f : sorted[Mathf.Clamp(Mathf.CeilToInt(p * sorted.Count) - 1, 0, sorted.Count - 1)];
	}
}
