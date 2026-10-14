using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbText
{
	private static float PxNow(out bool bold)
	{
		bold = false;
		try
		{
			bold = (Text.CurFontStyle.fontStyle & FontStyle.Bold) != 0;
		}
		catch
		{
		}
		return RcbFont.Px(Text.Font, bold);
	}

	public static Vector2 CalcSize(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return Vector2.zero;
		}
		bool bold;
		float px = PxNow(out bold);
		return new Vector2(RadiusFont.WidthAt(text, px, bold), RadiusFont.LineHAt(px, bold));
	}

	public static float Width(string text)
	{
		return CalcSize(text).x;
	}

	public static float CalcHeight(string text, float width)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0f;
		}
		bool bold;
		float px = PxNow(out bold);
		return RadiusFont.HeightAt(text, width, px, bold);
	}

	public static string Fit(string full, string shortForm, float maxWidth)
	{
		if (string.IsNullOrEmpty(full))
		{
			return full;
		}
		if (Width(full) <= maxWidth)
		{
			return full;
		}
		if (!string.IsNullOrEmpty(shortForm) && Width(shortForm) <= maxWidth)
		{
			return shortForm;
		}
		return full;
	}
}
