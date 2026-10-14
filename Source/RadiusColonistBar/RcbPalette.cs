using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
internal static class RcbPalette
{
	public static readonly Color BG = Palette.Flat.Panel;

	public static readonly Color BGD = Palette.Flat.Rail;

	public static readonly Color PanelBG = Palette.Flat.Card;

	public static readonly Color RowAlt = Palette.Flat.RowAlt;

	public static readonly Color Lift = Palette.Flat.Lift;

	public static readonly Color Ink = Palette.Flat.Rail;

	public static readonly Color Stat = Palette.Flat.Ink;

	public static readonly Color TextDim = Palette.Flat.InkMid;

	public static readonly Color TextDim2 = Palette.Flat.InkLow;

	public static readonly Color TextBody = Palette.Flat.InkMid;

	public static readonly Color Good = Palette.Good;

	public static readonly Color Warn = Palette.Warn;

	public static readonly Color Bad = Palette.Bad;

	public static readonly Color DamageFlash = Palette.Flat.Alert;

	public static readonly Color Over = Palette.Over;

	public static readonly Color Archo = Palette.Archo;

	public static readonly Color WashFaint = Palette.WashFaint;

	public static readonly Color Wash = Palette.Wash;

	public static readonly Color WashHover = Palette.WashHover;

	public static readonly Color WashStrong = Palette.WashStrong;

	public static readonly Color WashSolid = Palette.WashSolid;

	private const string ModId = "astryl.RadiusUI.ColonistBar";

	private static Color accentCached = Palette.Accents[0];

	private static int accentFrame = -1;

	public static Color Accent
	{
		get
		{
			int frameCount = Time.frameCount;
			if (frameCount == accentFrame)
			{
				return accentCached;
			}
			accentFrame = frameCount;
			try
			{
				accentCached = RadiusTheme.AccentFor("astryl.RadiusUI.ColonistBar");
			}
			catch
			{
				accentCached = Palette.Accents[0];
			}
			return accentCached;
		}
	}

	public static float MicroLineH => RadiusFont.LineHAt(13f);

	public static void DrawPlate(Rect r, Color fill)
	{
		Widgets.DrawBoxSolid(r, fill);
	}

	public static void MicroLabel(Rect r, string text, Color col, TextAnchor anchor = TextAnchor.MiddleCenter, bool shadow = true)
	{
		if (!string.IsNullOrEmpty(text))
		{
			float num = RadiusFont.LineHAt(13f);
			if (r.height < num)
			{
				r = new Rect(r.x, r.y + (r.height - num) * 0.5f, r.width, num);
			}
			RadiusFont.LabelAt(r, text, 13f, col, anchor);
		}
	}

	public static bool RowVisible(float rowY, float rowH, Vector2 scroll, float viewportH, float slack = 24f)
	{
		float num = scroll.y - slack;
		float num2 = scroll.y + viewportH + slack;
		return rowY + rowH >= num && rowY <= num2;
	}

	public static void BeginScroll(Rect outRect, ref Vector2 scroll, Rect viewRect)
	{
		FlatScroll.Begin(outRect, ref scroll, viewRect);
	}

	public static void EndScroll()
	{
		FlatScroll.End();
	}
}
