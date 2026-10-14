using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): the members of a squad who can take an order on one map. BarSquads keeps no
// ordered member list, so the order comes from the colonist bar, then thingIDNumber; the first one leads.
internal static class SquadMembers
{
	private static readonly Dictionary<Pawn, int> barRank = new Dictionary<Pawn, int>();

	internal static List<Pawn> Able(Map map, int index)
	{
		List<Pawn> list = new List<Pawn>();
		BarSquads get = BarSquads.Get;
		if (map == null || get == null || index < 0)
		{
			return list;
		}
		Add(list, map.mapPawns.FreeColonistsSpawned, get, index);
		Add(list, map.mapPawns.SlavesOfColonySpawned, get, index);
		RankByBar();
		list.Sort(Compare);
		barRank.Clear();
		return list;
	}

	private static void Add(List<Pawn> into, List<Pawn> from, BarSquads squads, int index)
	{
		for (int i = 0; i < from.Count; i++)
		{
			Pawn pawn = from[i];
			// Mechanoids and subhumans run other think trees: the assault node never reaches them.
			if (squads.SquadIndexOf(pawn) == index && !pawn.IsColonyMech && !pawn.IsSubhuman && !pawn.Downed && !pawn.Deathresting && !pawn.InMentalState && !pawn.IsPrisoner && pawn.drafter != null && !into.Contains(pawn))
			{
				into.Add(pawn);
			}
		}
	}

	private static void RankByBar()
	{
		barRank.Clear();
		List<ColonistBar.Entry> list = Find.ColonistBar?.Entries;
		if (list == null)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn = list[i].pawn;
			if (pawn != null && !barRank.ContainsKey(pawn))
			{
				barRank[pawn] = i;
			}
		}
	}

	private static int Compare(Pawn a, Pawn b)
	{
		int value;
		int num = (barRank.TryGetValue(a, out value) ? value : int.MaxValue);
		int value2;
		int num2 = (barRank.TryGetValue(b, out value2) ? value2 : int.MaxValue);
		if (num != num2)
		{
			return num.CompareTo(num2);
		}
		return a.thingIDNumber.CompareTo(b.thingIDNumber);
	}
}
