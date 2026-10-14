using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBar), "CheckRecacheEntries")]
internal static class Patch_Recache_SquadsAndHidden
{
	private static readonly FieldInfo entriesF = AccessTools.Field(typeof(ColonistBar), "cachedEntries");

	private static readonly AccessTools.FieldRef<ColonistBar, bool> entriesDirtyRef = CreateDirtyRef();

	private static readonly FieldInfo reorderF = AccessTools.Field(typeof(ColonistBar), "cachedReorderableGroups");

	private static readonly FieldInfo drawLocsF = AccessTools.Field(typeof(ColonistBar), "cachedDrawLocs");

	private static readonly FieldInfo scaleF = AccessTools.Field(typeof(ColonistBar), "cachedScale");

	private static readonly FieldInfo finderF = AccessTools.Field(typeof(ColonistBar), "drawLocsFinder");

	private static readonly FieldInfo drawerF = AccessTools.Field(typeof(ColonistBar), "drawer");

	private static readonly MethodInfo calcM = AccessTools.Method(typeof(ColonistBarDrawLocsFinder), "CalculateDrawLocs", new Type[3]
	{
		typeof(List<Vector2>),
		typeof(float).MakeByRefType(),
		typeof(int)
	}, (Type[])null);

	private static readonly HashSet<int> liveIds = new HashSet<int>();

	private static readonly List<Pawn> animalBuf = new List<Pawn>(8);

	private static readonly List<Pawn> fullRoster = new List<Pawn>(32);

	private static readonly List<ColonistBar.Entry> regroupBuf = new List<ColonistBar.Entry>(32);

	internal static bool Rebuilding;

	internal static bool PendingRebuild;

	private static AccessTools.FieldRef<ColonistBar, bool> CreateDirtyRef()
	{
		try
		{
			return AccessTools.FieldRefAccess<ColonistBar, bool>("entriesDirty");
		}
		catch
		{
			return null;
		}
	}

	private static void PruneTransientCaches(ColonistBar bar)
	{
		try
		{
			if (entriesF == null)
			{
				return;
			}
			List<ColonistBar.Entry> list = (List<ColonistBar.Entry>)entriesF.GetValue(bar);
			if (list == null)
			{
				return;
			}
			liveIds.Clear();
			for (int i = 0; i < list.Count; i++)
			{
				Pawn pawn = list[i].pawn;
				if (pawn != null)
				{
					liveIds.Add(pawn.thingIDNumber);
				}
			}
			Patch_DrawColonist_Overlay.PruneCaches(liveIds);
			PawnUiCache.Prune(liveIds);
			SpeechBubbles.Prune(liveIds);
			RcbAlertBand.PruneTips(liveIds);
			ReorderBounds.Prune(liveIds);
		}
		catch
		{
		}
	}

	private static float SortKey(Pawn p, int mode)
	{
		if (p == null || mode == 0)
		{
			return 0f;
		}
		try
		{
			return mode switch
			{
				1 => RcbCohorts.UrgencyKey(p), 
				2 => (p.needs?.mood)?.CurLevelPercentage ?? 2f, 
				_ => 0f, 
			};
		}
		catch
		{
			return 0f;
		}
	}

	private static string SortKeyName(Pawn p, int mode)
	{
		if (mode != 3 || p == null)
		{
			return string.Empty;
		}
		try
		{
			return p.Name?.ToStringShort ?? p.LabelShortCap;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static bool AddPrisoners(List<ColonistBar.Entry> entries, int cohort)
	{
		int num = 0;
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].group + 1 > num)
			{
				num = entries[i].group + 1;
			}
		}
		bool result = false;
		List<Map> maps = Find.Maps;
		for (int j = 0; j < maps.Count; j++)
		{
			List<Pawn> prisonersOfColonySpawned = maps[j].mapPawns.PrisonersOfColonySpawned;
			for (int k = 0; k < prisonersOfColonySpawned.Count; k++)
			{
				if (RcbCohorts.PassesToken(prisonersOfColonySpawned[k], cohort))
				{
					entries.Add(new ColonistBar.Entry(prisonersOfColonySpawned[k], maps[j], num));
					result = true;
				}
			}
		}
		return result;
	}

	private static bool AddBondedAnimals(List<ColonistBar.Entry> entries, int cohort)
	{
		int num = 0;
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].group + 1 > num)
			{
				num = entries[i].group + 1;
			}
		}
		bool result = false;
		List<Map> maps = Find.Maps;
		for (int j = 0; j < maps.Count; j++)
		{
			animalBuf.Clear();
			RcbCohorts.CollectBondedAnimals(maps[j], animalBuf);
			for (int k = 0; k < animalBuf.Count; k++)
			{
				if (RcbCohorts.PassesToken(animalBuf[k], cohort))
				{
					entries.Add(new ColonistBar.Entry(animalBuf[k], maps[j], num));
					result = true;
				}
			}
		}
		return result;
	}

	private static bool RenumberSquadGroups(List<ColonistBar.Entry> entries, BarSquads sq)
	{
		int num = int.MinValue;
		int num2 = int.MinValue;
		int num3 = -1;
		bool flag = false;
		regroupBuf.Clear();
		for (int i = 0; i < entries.Count; i++)
		{
			ColonistBar.Entry item = entries[i];
			int num4 = ((item.pawn != null) ? sq.SquadIndexOf(item.pawn) : (-1));
			if (item.group != num || num4 != num2)
			{
				num3++;
				num = item.group;
				num2 = num4;
			}
			if (item.group == num3)
			{
				regroupBuf.Add(item);
				continue;
			}
			regroupBuf.Add(new ColonistBar.Entry(item.pawn, item.map, num3));
			flag = true;
		}
		if (flag)
		{
			entries.Clear();
			entries.AddRange(regroupBuf);
		}
		regroupBuf.Clear();
		return flag;
	}

	private static void Prefix(ColonistBar __instance, out bool __state)
	{
		__state = entriesDirtyRef == null || entriesDirtyRef.Invoke(__instance);
	}

	private static void Postfix(ColonistBar __instance, bool __state)
	{
		if (!__state)
		{
			return;
		}
		Rebuilding = true;
		try
		{
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			if (settings == null || !settings.masterEnabled)
			{
				RcbIsland.ClearMeasureRoster();
				return;
			}
			BarSquads sq = BarSquads.Get;
			if (sq == null)
			{
				RcbIsland.ClearMeasureRoster();
				return;
			}
			PruneTransientCaches(__instance);
			bool flag = settings.barHideSlaves || settings.barHideMechs || settings.barHideCorpses;
			bool flag2 = settings.warbandMode && settings.liveView && settings.warbandHideSelected;
			Pawn pawn = ((settings.warbandMode && settings.liveView && settings.warbandHidePerspective && PerspectiveShiftCompat.Installed) ? PerspectiveShiftCompat.PerspectivePawn() : null);
			int num = (RcbCohorts.Active ? RcbCohorts.Token : 0);
			bool flag3 = num != 0;
			bool flag4 = sq.revealPrisoners || num == 2;
			bool flag5 = sq.revealSlaves || num == 3;
			int sortMode = settings.barSortMode;
			bool flag6 = sortMode != 0 || sq.ccPinned.Count > 0;
			bool barShowBondedAnimals = settings.barShowBondedAnimals;
			bool flag7 = settings.barMaxVisible > 0;
			if (sq.hidden.Count == 0 && sq.squadOf.Count == 0 && !flag && !flag4 && !flag2 && pawn == null && !flag3 && !flag6 && !barShowBondedAnimals && !flag7)
			{
				RcbIsland.ClearMeasureRoster();
			}
			else
			{
				if (entriesF == null || reorderF == null || drawLocsF == null || scaleF == null || finderF == null || calcM == null)
				{
					return;
				}
				List<ColonistBar.Entry> list = (List<ColonistBar.Entry>)entriesF.GetValue(__instance);
				bool flag8 = false;
				int num2 = 0;
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].pawn != null)
					{
						num2++;
					}
				}
				fullRoster.Clear();
				for (int num3 = list.Count - 1; num3 >= 0; num3--)
				{
					Pawn pawn2 = list[num3].pawn;
					if (pawn2 != null)
					{
						bool flag9 = sq.IsHidden(pawn2);
						if (!flag9 && flag2)
						{
							flag9 = BarSelection.IsSelected(pawn2);
						}
						if (!flag9 && pawn != null)
						{
							flag9 = pawn2 == pawn;
						}
						if (!flag9 && settings.barHideSlaves && !flag5)
						{
							try
							{
								flag9 = pawn2.IsSlave;
							}
							catch
							{
							}
						}
						if (!flag9 && settings.barHideMechs)
						{
							try
							{
								flag9 = pawn2.RaceProps.IsMechanoid || pawn2.IsMutant;
							}
							catch
							{
							}
						}
						if (!flag9 && settings.barHideCorpses)
						{
							flag9 = pawn2.Dead;
						}
						if (!flag9)
						{
							fullRoster.Add(pawn2);
						}
						if (flag9 || (flag3 && !RcbCohorts.PassesToken(pawn2, num)))
						{
							list.RemoveAt(num3);
							flag8 = true;
						}
					}
				}
				if ((sq.squadOf.Count > 0 || flag6) && list.Count > 1)
				{
					List<ColonistBar.Entry> list2 = list.OrderBy((ColonistBar.Entry e) => e.group).ThenBy(delegate(ColonistBar.Entry e)
					{
						int num10 = ((e.pawn != null) ? sq.SquadIndexOf(e.pawn) : (-1));
						return (num10 < 0) ? int.MaxValue : num10;
					}).ThenBy((ColonistBar.Entry e) => (e.pawn == null || !sq.ccPinned.Contains(e.pawn.thingIDNumber)) ? 1 : 0)
						.ThenBy((ColonistBar.Entry e) => SortKey(e.pawn, sortMode))
						.ThenBy((ColonistBar.Entry e) => SortKeyName(e.pawn, sortMode))
						.ToList();
					for (int num4 = 0; num4 < list.Count; num4++)
					{
						if (list2[num4].pawn != list[num4].pawn)
						{
							list.Clear();
							list.AddRange(list2);
							flag8 = true;
							break;
						}
					}
				}
				if (flag4 && AddPrisoners(list, num))
				{
					flag8 = true;
				}
				if (settings.barShowBondedAnimals && AddBondedAnimals(list, flag3 ? num : 0))
				{
					flag8 = true;
				}
				if (num2 > 0 && flag3)
				{
					int num5 = 0;
					for (int num6 = 0; num6 < list.Count; num6++)
					{
						if (list[num6].pawn != null)
						{
							num5++;
						}
					}
					if (num5 == 0)
					{
						RcbCohorts.FallBackToAll();
					}
				}
				if (!RcbCohorts.Active && sq.cohortTab != 0)
				{
					sq.cohortTab = 0;
				}
				RcbAlertBand.SetRoster(list);
				RcbIsland.SetMeasureRoster(list);
				for (int num7 = 0; num7 < list.Count; num7++)
				{
					Pawn pawn3 = list[num7].pawn;
					if (pawn3 != null && !fullRoster.Contains(pawn3))
					{
						fullRoster.Add(pawn3);
					}
				}
				RcbIsland.SetSummaryRoster(fullRoster);
				if (RcbBarWindow.Apply(list))
				{
					flag8 = true;
				}
				if (sq.squadOf.Count > 0 && RenumberSquadGroups(list, sq))
				{
					flag8 = true;
				}
				if (!flag8)
				{
					return;
				}
				List<int> list3 = (List<int>)reorderF.GetValue(__instance);
				list3.Clear();
				int num8 = 0;
				for (int num9 = 0; num9 < list.Count; num9++)
				{
					list3.Add(-1);
					if (list[num9].group + 1 > num8)
					{
						num8 = list[num9].group + 1;
					}
				}
				List<Vector2> list4 = (List<Vector2>)drawLocsF.GetValue(__instance);
				object value = finderF.GetValue(__instance);
				object[] array = new object[3]
				{
					list4,
					scaleF.GetValue(__instance),
					num8
				};
				calcM.Invoke(value, array);
				scaleF.SetValue(__instance, array[1]);
				(drawerF?.GetValue(__instance) as ColonistBarColonistDrawer)?.Notify_RecachedEntries();
			}
		}
		catch (Exception ex)
		{
			Log.WarningOnce("[Radius UI - Colonist Bar] squad/hide recache failed: " + ex, 81231746);
		}
		finally
		{
			Rebuilding = false;
		}
	}
}
