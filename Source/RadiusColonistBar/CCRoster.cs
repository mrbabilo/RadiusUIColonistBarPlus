using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RadiusColonistBar;

internal static class CCRoster
{
	public static List<Pawn> GatherColonists()
	{
		List<Pawn> list = new List<Pawn>();
		try
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				List<Pawn> freeColonists = maps[i].mapPawns.FreeColonists;
				for (int j = 0; j < freeColonists.Count; j++)
				{
					if (freeColonists[j] != null && !freeColonists[j].Dead)
					{
						list.Add(freeColonists[j]);
					}
				}
			}
			List<Caravan> list2 = Find.WorldObjects?.Caravans;
			if (list2 != null)
			{
				for (int k = 0; k < list2.Count; k++)
				{
					List<Pawn> pawnsListForReading = list2[k].PawnsListForReading;
					for (int l = 0; l < pawnsListForReading.Count; l++)
					{
						if (pawnsListForReading[l] != null && pawnsListForReading[l].IsColonist && !pawnsListForReading[l].Dead && !list.Contains(pawnsListForReading[l]))
						{
							list.Add(pawnsListForReading[l]);
						}
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	public static List<Pawn> GatherPrisoners()
	{
		List<Pawn> list = new List<Pawn>();
		try
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				List<Pawn> prisonersOfColony = maps[i].mapPawns.PrisonersOfColony;
				for (int j = 0; j < prisonersOfColony.Count; j++)
				{
					if (prisonersOfColony[j] != null && !prisonersOfColony[j].Dead)
					{
						list.Add(prisonersOfColony[j]);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	public static List<Pawn> GatherSlaves()
	{
		List<Pawn> list = new List<Pawn>();
		try
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				List<Pawn> slavesOfColonySpawned = maps[i].mapPawns.SlavesOfColonySpawned;
				for (int j = 0; j < slavesOfColonySpawned.Count; j++)
				{
					if (slavesOfColonySpawned[j] != null && !slavesOfColonySpawned[j].Dead)
					{
						list.Add(slavesOfColonySpawned[j]);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	public static List<Pawn> Gather(CCCategory cat)
	{
		return cat switch
		{
			CCCategory.Prisoners => GatherPrisoners(), 
			CCCategory.Slaves => GatherSlaves(), 
			_ => GatherColonists(), 
		};
	}

	public static List<Pawn> GatherAll()
	{
		List<Pawn> list = GatherColonists();
		foreach (Pawn item in GatherPrisoners())
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		foreach (Pawn item2 in GatherSlaves())
		{
			if (!list.Contains(item2))
			{
				list.Add(item2);
			}
		}
		return list;
	}

	public static List<Pawn> BySquad(List<Pawn> pawns, BarSquads sq)
	{
		List<Pawn> list = new List<Pawn>(pawns);
		list.Sort(delegate(Pawn a, Pawn b)
		{
			int num = ((sq != null) ? sq.SquadIndexOf(a) : (-1));
			int num2 = ((sq != null) ? sq.SquadIndexOf(b) : (-1));
			if (num < 0)
			{
				num = int.MaxValue;
			}
			if (num2 < 0)
			{
				num2 = int.MaxValue;
			}
			return (num != num2) ? num.CompareTo(num2) : string.Compare(a.LabelShortCap, b.LabelShortCap, StringComparison.OrdinalIgnoreCase);
		});
		return list;
	}
}
