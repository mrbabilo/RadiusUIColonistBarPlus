using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbInput
{
	private static int reclaimedFrame = -1;

	public static bool LeftPress(Event e)
	{
		return Press(e, 0);
	}

	public static bool RightPress(Event e)
	{
		return Press(e, 1);
	}

	private static bool Press(Event e, int button)
	{
		if (e == null || e.button != button)
		{
			return false;
		}
		if (e.type == EventType.MouseDown)
		{
			return true;
		}
		if (e.rawType != EventType.MouseDown || e.type != EventType.Used)
		{
			return false;
		}
		if (reclaimedFrame == Time.frameCount)
		{
			return false;
		}
		if (!NothingElseOwnsIt(e))
		{
			return false;
		}
		reclaimedFrame = Time.frameCount;
		return true;
	}

	private static bool NothingElseOwnsIt(Event e)
	{
		try
		{
			return Find.WindowStack != null && Find.WindowStack.GetWindowAt(UI.GUIToScreenPoint(e.mousePosition)) == null;
		}
		catch
		{
			return false;
		}
	}
}
