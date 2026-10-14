using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbFont
{
	public static bool BoldAvailable => RadiusFont.RealBoldAvailable;

	public static float Px(GameFont font, bool bold)
	{
		if (font == GameFont.Medium)
		{
			return 17f;
		}
		return bold ? 15f : 13f;
	}

	public static GUIStyle Style(GameFont font, bool bold)
	{
		return RadiusFont.StyleAt(Px(font, bold), bold);
	}

	public static float LineH(GameFont font, bool bold)
	{
		return RadiusFont.LineHAt(Px(font, bold), bold);
	}

	public static void Label(Rect r, string text, bool bold, GameFont font, Color col, TextAnchor anchor = TextAnchor.MiddleLeft)
	{
		if (!string.IsNullOrEmpty(text))
		{
			float px = Px(font, bold);
			float num = RadiusFont.LineHAt(px, bold);
			if (r.height < num)
			{
				r = new Rect(r.x, r.y + (r.height - num) * 0.5f, r.width, num);
			}
			RadiusFont.LabelAt(r, text, px, col, anchor, bold);
		}
	}

	public static void Wrapped(Rect r, string text, bool bold, GameFont font, Color col, TextAnchor anchor = TextAnchor.UpperLeft)
	{
		if (!string.IsNullOrEmpty(text))
		{
			Color color = GUI.color;
			RadiusFont.LabelAt(r, text, Px(font, bold), col, anchor, bold, wrap: true);
			GUI.color = color;
		}
	}

	public static string Fit(string s, float maxW, bool bold, GameFont font, float minScale = 0.82f)
	{
		if (string.IsNullOrEmpty(s) || maxW < 4f)
		{
			return s;
		}
		return RadiusFont.FitAt(s, maxW, Px(font, bold), bold);
	}

	public static float Width(string s, bool bold, GameFont font)
	{
		if (string.IsNullOrEmpty(s))
		{
			return 0f;
		}
		return RadiusFont.WidthAt(s, Px(font, bold), bold);
	}
}
