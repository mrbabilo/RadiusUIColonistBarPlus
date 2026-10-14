using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RadiusColonistBar;

public class CCHistory(Game game) : GameComponent
{
	public const int SampleIntervalTicks = 2500;

	public const int HoursKept = 192;

	private Dictionary<int, CCPawnHistory> histories = new Dictionary<int, CCPawnHistory>();

	private int lastSampledHour = -1;

	private static readonly List<Pawn> tmpPawns = new List<Pawn>();

	private static CCHistory cachedComp;

	private static Game cachedGame;

	public static CCHistory Instance
	{
		get
		{
			Game game = Current.Game;
			if (game == null)
			{
				cachedGame = null;
				cachedComp = null;
				return null;
			}
			if (game != cachedGame)
			{
				cachedGame = game;
				cachedComp = game.GetComponent<CCHistory>();
			}
			return cachedComp;
		}
	}

	public CCSeries GetSeries(Pawn pawn, string key)
	{
		if (pawn == null || string.IsNullOrEmpty(key))
		{
			return null;
		}
		if (!histories.TryGetValue(pawn.thingIDNumber, out var value))
		{
			return null;
		}
		value.series.TryGetValue(key, out var value2);
		return value2;
	}

	public override void FinalizeInit()
	{
		base.FinalizeInit();
		TrySample(force: true);
	}

	public override void GameComponentTick()
	{
		if (Find.TickManager.TicksGame % 500 == 0)
		{
			TrySample(force: false);
		}
	}

	private static List<Pawn> TrackedPawns()
	{
		tmpPawns.Clear();
		try
		{
			if (Find.Maps != null)
			{
				foreach (Map map in Find.Maps)
				{
					foreach (Pawn freeColonistsAndPrisoner in map.mapPawns.FreeColonistsAndPrisoners)
					{
						tmpPawns.Add(freeColonistsAndPrisoner);
					}
					foreach (Pawn item in map.mapPawns.SlavesOfColonySpawned)
					{
						if (!tmpPawns.Contains(item))
						{
							tmpPawns.Add(item);
						}
					}
				}
			}
			if (Find.WorldObjects?.Caravans != null)
			{
				foreach (Caravan caravan in Find.WorldObjects.Caravans)
				{
					if (!caravan.IsPlayerControlled)
					{
						continue;
					}
					foreach (Pawn item2 in caravan.PawnsListForReading)
					{
						if ((item2.IsColonist || item2.IsPrisoner || item2.IsSlave) && !tmpPawns.Contains(item2))
						{
							tmpPawns.Add(item2);
						}
					}
				}
			}
		}
		catch
		{
		}
		return tmpPawns;
	}

	private void TrySample(bool force)
	{
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref histories, "mcbCcHistories", LookMode.Value, LookMode.Deep);
		Scribe_Values.Look(ref lastSampledHour, "mcbCcLastHour", -1);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && histories == null)
		{
			histories = new Dictionary<int, CCPawnHistory>();
		}
	}
}
