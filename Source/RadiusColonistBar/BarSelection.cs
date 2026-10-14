using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class BarSelection
{
	public static bool IsSelected(Pawn p)
	{
		Selector selector = Find.Selector;
		if (selector == null || p == null)
		{
			return false;
		}
		try
		{
			if (p.Dead)
			{
				return p.Corpse != null && selector.SelectedObjects.Contains(p.Corpse);
			}
			return selector.SelectedObjects.Contains(p);
		}
		catch
		{
			return false;
		}
	}

	public static float BarBottom()
	{
		Rect lastSlab = RcbIsland.LastSlab;
		if (RcbGuard.Finite(lastSlab) && lastSlab.height > 1f)
		{
			return lastSlab.yMax;
		}
		return 96f;
	}
}
