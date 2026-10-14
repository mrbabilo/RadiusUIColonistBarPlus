using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public static class ReorderBounds
{
	private struct Rec
	{
		public Rect rect;

		public int frame;
	}

	private static readonly Dictionary<int, Rec> map = new Dictionary<int, Rec>();

	public static void Record(Pawn p, Rect bounds)
	{
		if (p != null)
		{
			map[p.thingIDNumber] = new Rec
			{
				rect = bounds,
				frame = Time.frameCount
			};
		}
	}

	public static void Prune(HashSet<int> liveIds)
	{
		RcbUtil.PruneById(map, liveIds);
	}

	public static bool TryGet(Pawn p, out Rect bounds)
	{
		bounds = default(Rect);
		if (p != null && map.TryGetValue(p.thingIDNumber, out var value) && Time.frameCount - value.frame <= 4)
		{
			bounds = value.rect;
			return true;
		}
		return false;
	}
}
