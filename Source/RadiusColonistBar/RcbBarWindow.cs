using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbBarWindow
{
	private static int offset;

	private static int anchorId = -1;

	private static bool useAnchor;

	private static int hiddenLeft;

	private static int hiddenRight;

	private static int sevLeft = -1;

	private static int sevRight = -1;

	private static int restCount;

	private static int slotCount;

	private static readonly List<int> restIdx = new List<int>(32);

	private static readonly List<int> restIds = new List<int>(32);

	public static int HiddenLeft => hiddenLeft;

	public static int HiddenRight => hiddenRight;

	public static int SeverityLeft => sevLeft;

	public static int SeverityRight => sevRight;

	public static bool Active => Cap > 0 && (hiddenLeft > 0 || hiddenRight > 0);

	private static int Cap => RadiusColonistBarMod.Settings?.barMaxVisible ?? 0;

	public static int PageCount
	{
		get
		{
			if (slotCount <= 0 || restCount <= 0)
			{
				return 1;
			}
			return Mathf.Max(1, Mathf.CeilToInt((float)restCount / (float)slotCount));
		}
	}

	public static int CurrentPage
	{
		get
		{
			if (slotCount <= 0)
			{
				return 0;
			}
			if (restCount > slotCount && offset >= restCount - slotCount)
			{
				return PageCount - 1;
			}
			return Mathf.Clamp(Mathf.RoundToInt((float)offset / (float)slotCount), 0, PageCount - 1);
		}
	}

	public static void Step(int dir)
	{
		int max = Mathf.Max(0, restCount - slotCount);
		int num = Mathf.Clamp(offset + dir, 0, max);
		if (num != offset)
		{
			offset = num;
			useAnchor = false;
			Dirty();
		}
	}

	public static void GoToPage(int page)
	{
		int max = Mathf.Max(0, restCount - slotCount);
		int num = Mathf.Clamp(page * Mathf.Max(1, slotCount), 0, max);
		if (num != offset)
		{
			offset = num;
			useAnchor = false;
			Dirty();
		}
	}

	private static void Dirty()
	{
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

	public static void EnsureVisible(Pawn p)
	{
		if (Cap > 0 && p != null && slotCount > 0)
		{
			int num = restIds.IndexOf(p.thingIDNumber);
			if (num >= 0 && (num < offset || num >= offset + slotCount))
			{
				int max = Mathf.Max(0, restCount - slotCount);
				offset = Mathf.Clamp(num - slotCount / 2, 0, max);
				useAnchor = false;
				Dirty();
			}
		}
	}

	public static bool Apply(List<ColonistBar.Entry> entries)
	{
		hiddenLeft = (hiddenRight = 0);
		sevLeft = (sevRight = -1);
		restCount = (slotCount = 0);
		int cap = Cap;
		if (cap <= 0 || entries == null || entries.Count == 0)
		{
			return false;
		}
		BarSquads get = BarSquads.Get;
		restIdx.Clear();
		restIds.Clear();
		int num = 0;
		for (int i = 0; i < entries.Count; i++)
		{
			Pawn pawn = entries[i].pawn;
			if (pawn != null)
			{
				if (get != null && get.ccPinned.Contains(pawn.thingIDNumber))
				{
					num++;
					continue;
				}
				restIdx.Add(i);
				restIds.Add(pawn.thingIDNumber);
			}
		}
		restCount = restIdx.Count;
		slotCount = Mathf.Max(1, cap - num);
		if (restCount <= slotCount)
		{
			offset = 0;
			anchorId = -1;
			useAnchor = false;
			return false;
		}
		int max = restCount - slotCount;
		if (useAnchor && anchorId >= 0)
		{
			int num2 = -1;
			for (int j = 0; j < restIdx.Count; j++)
			{
				Pawn pawn2 = entries[restIdx[j]].pawn;
				if (pawn2 != null && pawn2.thingIDNumber == anchorId)
				{
					num2 = j;
					break;
				}
			}
			if (num2 >= 0)
			{
				offset = num2;
			}
		}
		offset = Mathf.Clamp(offset, 0, max);
		for (int k = 0; k < restIdx.Count; k++)
		{
			if (k >= offset && k < offset + slotCount)
			{
				continue;
			}
			Pawn pawn3 = entries[restIdx[k]].pawn;
			int num3 = ((pawn3 != null) ? RcbCohorts.UrgencyKey(pawn3) : int.MaxValue);
			if (k < offset)
			{
				hiddenLeft++;
				if (num3 != int.MaxValue && (sevLeft < 0 || num3 < sevLeft))
				{
					sevLeft = num3;
				}
			}
			else
			{
				hiddenRight++;
				if (num3 != int.MaxValue && (sevRight < 0 || num3 < sevRight))
				{
					sevRight = num3;
				}
			}
		}
		anchorId = entries[restIdx[offset]].pawn?.thingIDNumber ?? (-1);
		useAnchor = true;
		bool result = false;
		for (int num4 = restIdx.Count - 1; num4 >= 0; num4--)
		{
			if (num4 < offset || num4 >= offset + slotCount)
			{
				entries.RemoveAt(restIdx[num4]);
				result = true;
			}
		}
		return result;
	}
}
