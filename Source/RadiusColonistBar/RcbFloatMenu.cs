using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public class RcbFloatMenu : FloatMenu
{
	private Window child;

	public RcbFloatMenu(List<FloatMenuOption> options)
		: base(options)
	{
	}

	internal void NotifyChildOpened(Window w)
	{
		child = w;
	}

	public override void DoWindowContents(Rect rect)
	{
		if (child != null && !child.IsOpen)
		{
			child = null;
		}
		vanishIfMouseDistant = child == null;
		base.DoWindowContents(rect);
	}
}
