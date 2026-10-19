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

	// The squad's size and stations across all maps: PawnsFinder walks every pawn alive (most of a Compute), and both
	// change rarely. Kept until the next Invalidate (every order, station save or clear, squad edit) or RosterFrames.
	private struct Roster
	{
		public int total;

		public int stationMask;

		public int frame;

		public int version;
	}

	private const int RosterFrames = 60;

	private static readonly Dictionary<int, Roster> rosters = new Dictionary<int, Roster>();

	// Bumped on every Invalidate: the order card rebuilds its own snapshot when it changes.
	internal static int Version { get; private set; }

	internal static void Invalidate()
	{
		cache.Clear();
		Version++;
	}

	// Recomputed now, for the card and the hotkeys: a draft made by the game would otherwise show up to
	// CacheFrames late. The roster (total, stationMask) may still be up to RosterFrames old, after a death or a lost
	// map: FreshRoster rescans it, for a decision that depends on it.
	internal static State Fresh(int index)
	{
		return Recompute(index, exactRoster: false);
	}

	internal static State FreshRoster(int index)
	{
		return Recompute(index, exactRoster: true);
	}

	private static State Recompute(int index, bool exactRoster)
	{
		// Expire only: Of would compute a second time when its cache has run out.
		Expire();
		State state = Compute(index, Find.CurrentMap, exactRoster);
		cache[index] = state;
		return state;
	}

	internal static State Of(int index)
	{
		Expire();
		if (!cache.TryGetValue(index, out var value))
		{
			value = Compute(index, cacheMap);
			cache[index] = value;
		}
		return value;
	}

	private static void Expire()
	{
		Map currentMap = Find.CurrentMap;
		if (Time.frameCount - cacheFrame > CacheFrames || cacheMap != currentMap)
		{
			cache.Clear();
			cacheFrame = Time.frameCount;
			cacheMap = currentMap;
		}
	}

	private static State Compute(int index, Map map, bool exactRoster = false)
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
		Roster roster = RosterOf(index, get, exactRoster);
		result.stationMask = roster.stationMask;
		result.total = Mathf.Max(roster.total, result.present);
		return result;
	}

	private static Roster RosterOf(int index, BarSquads get, bool exact)
	{
		if (!exact && rosters.TryGetValue(index, out var value) && value.version == Version && Time.frameCount - value.frame <= RosterFrames)
		{
			return value;
		}
		value = new Roster
		{
			frame = Time.frameCount,
			version = Version
		};
		// All maps, caravans and transporters, slaves included: the bar's own list follows the squad tab filter.
		List<Pawn> list = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
		for (int i = 0; i < list.Count; i++)
		{
			if (get.SquadIndexOf(list[i]) == index)
			{
				value.total++;
				for (int j = 0; j < BarSquads.StationSlots; j++)
				{
					if (get.HasLiveStation(list[i], j))
					{
						value.stationMask |= 1 << j;
					}
				}
			}
		}
		rosters[index] = value;
		return value;
	}
}
