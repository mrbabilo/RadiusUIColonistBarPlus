using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbCell
{
	public static float StripW(float scale)
	{
		return Mathf.Max(2f, 3f * scale);
	}

	public static void Chrome(Rect r, Pawn p, float scale, bool selected, bool drafted, float dmgFlash, Color draftedCol, bool plate, bool hover)
	{
		if (selected)
		{
			RcbPalette.DrawPlate(r, RcbPalette.Lift);
		}
		else if (hover)
		{
			RcbPalette.DrawPlate(r, RcbPalette.PanelBG);
		}
		else if (plate)
		{
			RcbPalette.DrawPlate(r, RcbPalette.BG);
		}
	}

	public static void Strip(Rect r, Pawn p, float scale, bool selected, bool drafted, float dmgFlash, Color draftedCol, float extraPulse = 0f)
	{
	}
}
