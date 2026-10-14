using System;
using System.Collections.Generic;
using Verse;

namespace RadiusColonistBar;

internal class CCContext
{
	public List<Pawn> roster;

	public BarSquads squads;

	public Action requestClose;

	public Action requestRefresh;

	public List<Pawn> selection;

	public Pawn focus;

	public Action<Pawn, bool> select;

	public List<Pawn> ActingSet
	{
		get
		{
			if (selection != null && selection.Count > 0)
			{
				return selection;
			}
			List<Pawn> list = new List<Pawn>();
			if (focus != null)
			{
				list.Add(focus);
			}
			else if (roster != null)
			{
				list.AddRange(roster);
			}
			return list;
		}
	}
}
