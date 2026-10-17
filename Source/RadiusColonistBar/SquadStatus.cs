using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): one squad's state on the current map, shared by the order card, the squad tabs
// and the hotkeys. Cached for a few frames: the tabs ask for it every frame.
internal static class SquadStatus
{
	internal struct State
	{
		public int present;

		public int total;

		public int drafted;

		public int fireAtWill;

		public int assault;

		public bool hasCommonMode;

		public AssaultMode commonMode;

		// Battle stations (Colonist Bar Plus): bit n set when a living member, on any map, has station n on a map
		// that still exists. Dead members and destroyed maps would otherwise keep Clear enabled with nothing to clear.
		public int stationMask;

		// Medical orders (Colonist Bar Plus): members on this map who are down (rescue would take them) or need tending.
		public int downed;

		public int wounded;
	}

	private const int CacheFrames = 20;

	private static readonly Dictionary<int, State> cache = new Dictionary<int, State>();

	private static int cacheFrame = -1;

	private static Map cacheMap;

	// Bumped on every Invalidate: the order card rebuilds its own snapshot when it changes.
	internal static int Version { get; private set; }

	internal static void Invalidate()
	{
		cache.Clear();
		Version++;
	}

	// Recomputed now, for the card and the hotkeys: a draft made by the game would otherwise show up to
	// CacheFrames late.
	internal static State Fresh(int index)
	{
		Of(index);
		State state = Compute(index, Find.CurrentMap);
		cache[index] = state;
		return state;
	}

	internal static State Of(int index)
	{
		Map currentMap = Find.CurrentMap;
		if (Time.frameCount - cacheFrame > CacheFrames || cacheMap != currentMap)
		{
			cache.Clear();
			cacheFrame = Time.frameCount;
			cacheMap = currentMap;
		}
		if (!cache.TryGetValue(index, out var value))
		{
			value = Compute(index, currentMap);
			cache[index] = value;
		}
		return value;
	}

	private static State Compute(int index, Map map)
	{
		State result = default(State);
		BarSquads get = BarSquads.Get;
		if (get == null || index < 0)
		{
			return result;
		}
		List<Pawn> list = SquadMembers.Able(map, index);
		result.present = list.Count;
		SquadAssault get2 = SquadAssault.Get;
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn = list[i];
			if (pawn.Drafted)
			{
				result.drafted++;
			}
			if (pawn.drafter != null && pawn.drafter.FireAtWill)
			{
				result.fireAtWill++;
			}
			if (get2 != null && get2.TryGetMode(pawn, out var _))
			{
				result.assault++;
			}
		}
		result.hasCommonMode = SquadOrders.CommonMode(list, out result.commonMode);
		foreach (Pawn item in SquadMembers.Spawned(map, index))
		{
			if (SquadMedical.WantsRescue(item))
			{
				result.downed++;
			}
			else if (PawnUiCache.Get(item).needsTend)
			{
				result.wounded++;
			}
		}
		// All maps, caravans and transporters, slaves included: the bar's own list follows the squad tab filter.
		List<Pawn> list2 = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
		for (int j = 0; j < list2.Count; j++)
		{
			if (get.SquadIndexOf(list2[j]) == index)
			{
				result.total++;
				for (int k = 0; k < BarSquads.StationSlots; k++)
				{
					if (get.HasLiveStation(list2[j], k))
					{
						result.stationMask |= 1 << k;
					}
				}
			}
		}
		result.total = Mathf.Max(result.total, result.present);
		return result;
	}
}
