using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

internal static class RcbCohorts
{
	public const int SquadBase = 100;

	private static float nextSample = -1f;

	private static bool dirty = true;

	private static int colonists;

	private static int prisoners;

	private static int slaves;

	private static int pins;

	private static int animals;

	private static readonly int[] sev = new int[6];

	private static readonly List<int> squadCount = new List<int>();

	private static readonly List<int> squadSev = new List<int>();

	private static readonly List<Pawn> animalTmp = new List<Pawn>(8);

	public static RcbCohort Tab
	{
		get
		{
			BarSquads get = BarSquads.Get;
			if (get == null)
			{
				return RcbCohort.All;
			}
			int cohortTab = get.cohortTab;
			return (cohortTab >= 0 && cohortTab <= 5) ? ((RcbCohort)cohortTab) : RcbCohort.All;
		}
	}

	public static int Token
	{
		get
		{
			BarSquads get = BarSquads.Get;
			if (get == null)
			{
				return 0;
			}
			int cohortTab = get.cohortTab;
			if (cohortTab >= 100)
			{
				return (cohortTab - 100 < get.names.Count) ? cohortTab : 0;
			}
			return (cohortTab >= 0 && cohortTab <= 5) ? cohortTab : 0;
		}
	}

	public static bool Active
	{
		get
		{
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			if (settings == null || !settings.barCohortTabs)
			{
				return false;
			}
			Refresh();
			if (prisoners > 0 || slaves > 0 || pins > 0 || animals > 0)
			{
				return true;
			}
			for (int i = 0; i < squadCount.Count; i++)
			{
				if (squadCount[i] > 0)
				{
					return true;
				}
			}
			return false;
		}
	}

	public static void SetTab(RcbCohort c)
	{
		SetToken((int)c);
	}

	public static bool TokenIsSquad(int t, out int si)
	{
		si = t - 100;
		return t >= 100;
	}

	public static void SetToken(int t)
	{
		BarSquads get = BarSquads.Get;
		if (get == null || get.cohortTab == t)
		{
			return;
		}
		get.cohortTab = t;
		if (Patch_Recache_SquadsAndHidden.Rebuilding)
		{
			Patch_Recache_SquadsAndHidden.PendingRebuild = true;
			return;
		}
		try
		{
			Find.ColonistBar?.MarkColonistsDirty();
		}
		catch
		{
		}
	}

	public static bool PassesToken(Pawn p, int t)
	{
		if (t < 100)
		{
			return Passes(p, (RcbCohort)t);
		}
		BarSquads get = BarSquads.Get;
		return p != null && get != null && get.SquadIndexOf(p) == t - 100;
	}

	public static bool Passes(Pawn p, RcbCohort c)
	{
		if (p == null)
		{
			return true;
		}
		try
		{
			return c switch
			{
				RcbCohort.Colonists => !p.IsPrisonerOfColony && !p.IsSlaveOfColony && p.RaceProps.Humanlike, 
				RcbCohort.Animals => !p.RaceProps.Humanlike, 
				RcbCohort.Prisoners => p.IsPrisonerOfColony, 
				RcbCohort.Slaves => p.IsSlaveOfColony, 
				RcbCohort.Pinned => IsPinned(p), 
				_ => true, 
			};
		}
		catch
		{
			return true;
		}
	}

	public static bool IsPinned(Pawn p)
	{
		BarSquads get = BarSquads.Get;
		return p != null && get != null && get.ccPinned.Contains(p.thingIDNumber);
	}

	public static void TogglePin(Pawn p)
	{
		BarSquads get = BarSquads.Get;
		if (p == null || get == null)
		{
			return;
		}
		int thingIDNumber = p.thingIDNumber;
		if (!get.ccPinned.Remove(thingIDNumber))
		{
			get.ccPinned.Add(thingIDNumber);
		}
		dirty = true;
		if (Patch_Recache_SquadsAndHidden.Rebuilding)
		{
			Patch_Recache_SquadsAndHidden.PendingRebuild = true;
			return;
		}
		try
		{
			Find.ColonistBar?.MarkColonistsDirty();
		}
		catch
		{
		}
	}

	public static int Count(RcbCohort c)
	{
		Refresh();
		return c switch
		{
			RcbCohort.Colonists => colonists, 
			RcbCohort.Prisoners => prisoners, 
			RcbCohort.Slaves => slaves, 
			RcbCohort.Pinned => pins, 
			RcbCohort.Animals => animals, 
			_ => colonists + prisoners + slaves + animals, 
		};
	}

	public static int Severity(RcbCohort c)
	{
		Refresh();
		int num = sev[(uint)c];
		return (num == int.MaxValue) ? (-1) : num;
	}

	public static int TokenCount(int t)
	{
		if (t < 100)
		{
			return Count((RcbCohort)t);
		}
		Refresh();
		int num = t - 100;
		return (num >= 0 && num < squadCount.Count) ? squadCount[num] : 0;
	}

	public static int TokenSeverity(int t)
	{
		if (t < 100)
		{
			return Severity((RcbCohort)t);
		}
		Refresh();
		int num = t - 100;
		int num2 = ((num >= 0 && num < squadSev.Count) ? squadSev[num] : int.MaxValue);
		return (num2 == int.MaxValue) ? (-1) : num2;
	}

	public static string TokenLabel(int t)
	{
		if (t < 100)
		{
			return Label((RcbCohort)t);
		}
		return BarSquads.Get?.SquadName(t - 100) ?? "Squad";
	}

	public static void CollectBondedAnimals(Map map, List<Pawn> into)
	{
		int num = RadiusColonistBarMod.Settings?.barAnimalMode ?? 0;
		if (num == 0)
		{
			return;
		}
		try
		{
			List<Pawn> list = map?.mapPawns?.SpawnedColonyAnimals;
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				Pawn pawn = list[i];
				if (pawn != null && !pawn.Dead && (num != 1 || (pawn.relations != null && pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond) != null)))
				{
					into.Add(pawn);
				}
			}
		}
		catch
		{
		}
	}

	private static void Refresh()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (!dirty && realtimeSinceStartup < nextSample)
		{
			return;
		}
		dirty = false;
		nextSample = realtimeSinceStartup + 0.25f;
		colonists = (prisoners = (slaves = (pins = (animals = 0))));
		for (int i = 0; i < sev.Length; i++)
		{
			sev[i] = int.MaxValue;
		}
		BarSquads get = BarSquads.Get;
		squadCount.Clear();
		squadSev.Clear();
		int num = get?.names.Count ?? 0;
		for (int j = 0; j < num; j++)
		{
			squadCount.Add(0);
			squadSev.Add(int.MaxValue);
		}
		List<Map> maps = Find.Maps;
		if (maps == null)
		{
			return;
		}
		for (int k = 0; k < maps.Count; k++)
		{
			MapPawns mapPawns = maps[k]?.mapPawns;
			if (mapPawns != null)
			{
				Tally(mapPawns.FreeColonistsSpawned, RcbCohort.Colonists, get);
				Tally(mapPawns.PrisonersOfColonySpawned, RcbCohort.Prisoners, get);
				Tally(mapPawns.SlavesOfColonySpawned, RcbCohort.Slaves, get);
				if (settings != null && settings.barAnimalMode != 0)
				{
					animalTmp.Clear();
					CollectBondedAnimals(maps[k], animalTmp);
					Tally(animalTmp, RcbCohort.Animals, get);
				}
			}
		}
	}

	private static void Tally(List<Pawn> list, RcbCohort c, BarSquads sq)
	{
		if (list == null)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Pawn pawn = list[i];
			if (pawn == null)
			{
				continue;
			}
			if (c == RcbCohort.Colonists)
			{
				try
				{
					if (pawn.IsSlaveOfColony || pawn.IsPrisonerOfColony)
					{
						continue;
					}
					goto IL_005e;
				}
				catch
				{
					goto IL_005e;
				}
			}
			goto IL_005e;
			IL_005e:
			switch (c)
			{
			case RcbCohort.Colonists:
				colonists++;
				break;
			case RcbCohort.Prisoners:
				prisoners++;
				break;
			case RcbCohort.Slaves:
				slaves++;
				break;
			case RcbCohort.Animals:
				animals++;
				break;
			}
			bool flag = sq?.ccPinned.Contains(pawn.thingIDNumber) ?? false;
			if (flag)
			{
				pins++;
			}
			int num = sq?.SquadIndexOf(pawn) ?? (-1);
			if (num >= 0 && num < squadCount.Count)
			{
				squadCount[num]++;
			}
			int num2 = WorstSev(pawn);
			if (num2 >= 0)
			{
				if (num2 < sev[(uint)c])
				{
					sev[(uint)c] = num2;
				}
				if (num2 < sev[0])
				{
					sev[0] = num2;
				}
				if (flag && num2 < sev[4])
				{
					sev[4] = num2;
				}
				if (num >= 0 && num < squadSev.Count && num2 < squadSev[num])
				{
					squadSev[num] = num2;
				}
			}
		}
	}

	private static int WorstSev(Pawn p)
	{
		try
		{
			if (p.Dead)
			{
				return -1;
			}
			if (p.health != null && p.health.hediffSet.BleedRateTotal > 0.01f && HealthUtility.TicksUntilDeathDueToBloodLoss(p) < 60000)
			{
				return 0;
			}
			if (p.InMentalState)
			{
				return 0;
			}
			if (p.Downed && p.CurrentBed() == null && !PawnStatusUtil.DownedIsIncidental(p))
			{
				return 0;
			}
			Need_Mood need_Mood = p.needs?.mood;
			MentalBreaker mentalBreaker = p.mindState?.mentalBreaker;
			if (need_Mood != null && mentalBreaker != null && need_Mood.CurLevelPercentage < mentalBreaker.BreakThresholdMajor)
			{
				return 1;
			}
			if (HealthAIUtility.ShouldBeTendedNowByPlayer(p))
			{
				return 3;
			}
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			if (settings != null && settings.alertInspired)
			{
				InspirationHandler inspirationHandler = p.mindState?.inspirationHandler;
				if (inspirationHandler != null && inspirationHandler.Inspired)
				{
					return 4;
				}
			}
		}
		catch
		{
		}
		return -1;
	}

	public static int UrgencyKey(Pawn p)
	{
		int num = WorstSev(p);
		return (num < 0) ? int.MaxValue : num;
	}

	public static string Label(RcbCohort c)
	{
		return c switch
		{
			RcbCohort.Colonists => "RCB_Cohort_Colonists".Translate(), 
			RcbCohort.Prisoners => "RCB_Cohort_Prisoners".Translate(), 
			RcbCohort.Slaves => "RCB_Cohort_Slaves".Translate(), 
			RcbCohort.Pinned => "RCB_Cohort_Pinned".Translate(), 
			RcbCohort.Animals => "RCB_Cohort_Animals".Translate(), 
			_ => "RCB_Cohort_All".Translate(), 
		};
	}

	public static void BuildTokens(List<int> into)
	{
		into.Clear();
		Refresh();
		int token = Token;
		into.Add(0);
		for (int i = 0; i < squadCount.Count; i++)
		{
			if (squadCount[i] > 0 || token == 100 + i)
			{
				into.Add(100 + i);
			}
		}
		if (colonists > 0 || token == 1)
		{
			into.Add(1);
		}
		if (prisoners > 0 || token == 2)
		{
			into.Add(2);
		}
		if (slaves > 0 || token == 3)
		{
			into.Add(3);
		}
		if (animals > 0 || token == 5)
		{
			into.Add(5);
		}
		if (pins > 0 || token == 4)
		{
			into.Add(4);
		}
	}

	public static void FallBackToAll()
	{
		SetTab(RcbCohort.All);
	}

	public static void MarkDirty()
	{
		dirty = true;
	}
}
