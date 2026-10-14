using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class CCWidgets
{
	public static readonly Color HeaderText = RcbPalette.Stat;

	public static readonly Color DimText = RcbPalette.TextDim;

	public static readonly Color RowHover = RcbPalette.Wash;

	public static readonly Color RowSel = RcbPalette.Lift;

	public static readonly Color Divider = RcbPalette.RowAlt;

	public static readonly Color WindowBg = RcbPalette.BG;

	public static readonly Color GrayBtn = RcbPalette.Lift;

	public static readonly Color Ink = RcbPalette.Ink;

	private static int _tinyFrame = -1;

	private static float _tinyH;

	public static float TinyH
	{
		get
		{
			if (Time.frameCount != _tinyFrame)
			{
				GameFont font = Text.Font;
				Text.Font = GameFont.Small;
				_tinyH = Mathf.Ceil(Text.LineHeight) + 2f;
				Text.Font = font;
				_tinyFrame = Time.frameCount;
			}
			return _tinyH;
		}
	}

	public static void ForbiddenX(Rect r)
	{
		Color alert = Palette.Flat.Alert;
		Widgets.DrawLine(new Vector2(r.x + 2f, r.y + 2f), new Vector2(r.xMax - 2f, r.yMax - 2f), alert, 2f);
		Widgets.DrawLine(new Vector2(r.x + 2f, r.yMax - 2f), new Vector2(r.xMax - 2f, r.y + 2f), alert, 2f);
	}

	public static void Plate(Rect r, float lift, float alpha = 1f)
	{
		Color c = Color.Lerp(Ink, Color.white, Mathf.Clamp01(lift));
		c.a = alpha;
		RcbSpatial.Card(r, c);
	}

	public static void Panel(Rect r)
	{
		Plate(r, 0.045f);
	}

	public static void Well(Rect r)
	{
		RcbSpatial.Card(r, new Color(0f, 0f, 0f, 0.3f));
	}

	public static float SectionHeader(Rect r, string label, string countSuffix = null)
	{
		GameFont font = Text.Font;
		Text.Font = GameFont.Small;
		float num = Mathf.Ceil(Text.LineHeight);
		Text.Font = font;
		UIKit.Flat.SectionHeader(new Rect(r.x, r.y, r.width, num + 2f), string.IsNullOrEmpty(countSuffix) ? label : (label + " (" + countSuffix + ")"));
		return r.y + num + 8f;
	}

	public static void StatBar(Rect r, string label, float frac, Color col)
	{
		frac = Mathf.Clamp01(frac);
		Widgets.DrawBoxSolid(r, CardPalette.Track);
		if (frac > 0f)
		{
			Rect rect = new Rect(r.x, r.y, r.width * frac, r.height);
			Widgets.DrawBoxSolid(rect, new Color(col.r, col.g, col.b, 0.55f));
			if (rect.width >= 2f)
			{
				Widgets.DrawBoxSolid(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), col);
			}
		}
		GUI.color = CardPalette.Border(null);
		Widgets.DrawBox(r);
		GUI.color = Color.white;
		if (!string.IsNullOrEmpty(label) && Mouse.IsOver(r))
		{
			TooltipHandler.TipRegion(r, label + ": " + Mathf.RoundToInt(frac * 100f) + "%");
		}
	}

	public static void Label(Rect r, string text, Color col, GameFont font = GameFont.Small, TextAnchor anchor = TextAnchor.MiddleLeft)
	{
		GameFont font2 = Text.Font;
		TextAnchor anchor2 = Text.Anchor;
		bool wordWrap = Text.WordWrap;
		if (font == GameFont.Tiny)
		{
			font = GameFont.Small;
		}
		Text.Font = font;
		Text.Anchor = anchor;
		Text.WordWrap = false;
		float num = Mathf.Ceil(Text.LineHeight);
		if (r.height < num)
		{
			r = new Rect(r.x, r.y + (r.height - num) * 0.5f, r.width, num);
		}
		GUI.color = col;
		Widgets.Label(r, text ?? "");
		GUI.color = Color.white;
		Text.Font = font2;
		Text.Anchor = anchor2;
		Text.WordWrap = wordWrap;
	}

	public static bool Tab(Rect r, Texture2D icon, string label, bool active)
	{
		Rect rect = new Rect(r.x, r.y, r.width - 2f, r.height);
		bool flag = Mouse.IsOver(rect);
		if (active)
		{
			Plate(rect, 0.15f);
		}
		else if (flag)
		{
			Plate(rect, 0.07f);
		}
		if (icon != null)
		{
			GUI.color = (active ? CardPalette.StatText : DimText);
			GUI.DrawTexture(new Rect(rect.x + 10f, rect.y + (rect.height - 16f) / 2f, 16f, 16f), icon);
			GUI.color = Color.white;
		}
		Label(rect, label, active ? CardPalette.StatText : DimText, GameFont.Small, TextAnchor.MiddleCenter);
		return Widgets.ButtonInvisible(r);
	}

	public static bool TabIcon(Rect r, Texture2D icon, string tooltip, bool active)
	{
		Rect rect = new Rect(r.x, r.y, r.width - 2f, r.height);
		bool flag = Mouse.IsOver(rect);
		if (active)
		{
			Plate(rect, 0.15f);
		}
		else if (flag)
		{
			Plate(rect, 0.07f);
		}
		if (icon != null)
		{
			GUI.color = (active ? CardPalette.StatText : DimText);
			float num = Mathf.Min(18f, rect.height - 8f);
			GUI.DrawTexture(new Rect(rect.center.x - num / 2f, rect.center.y - num / 2f, num, num), icon);
			GUI.color = Color.white;
		}
		else
		{
			Label(rect, tooltip, active ? CardPalette.StatText : DimText, GameFont.Small, TextAnchor.MiddleCenter);
		}
		if (!string.IsNullOrEmpty(tooltip) && Mouse.IsOver(rect))
		{
			TooltipHandler.TipRegion(rect, tooltip);
		}
		return Widgets.ButtonInvisible(r);
	}

	public static bool IconButton(Rect r, Texture2D icon, string tooltip, string fallback = "►")
	{
		bool flag = Mouse.IsOver(r);
		RcbPalette.DrawPlate(r, flag ? Color.Lerp(GrayBtn, CardPalette.Accent, 0.14f) : GrayBtn);
		if (icon != null)
		{
			float num = Mathf.Min(16f, r.height - 6f);
			GUI.color = (flag ? CardPalette.Accent : RcbPalette.TextBody);
			GUI.DrawTexture(new Rect(r.center.x - num / 2f, r.center.y - num / 2f, num, num), icon);
			GUI.color = Color.white;
		}
		else
		{
			Label(r, fallback, CardPalette.StatText, GameFont.Tiny, TextAnchor.MiddleCenter);
		}
		if (!string.IsNullOrEmpty(tooltip) && flag)
		{
			TooltipHandler.TipRegion(r, tooltip);
		}
		return Widgets.ButtonInvisible(r);
	}

	public static bool Button(Rect r, string label, bool enabled = true)
	{
		bool flag = enabled && Mouse.IsOver(r);
		Plate(r, (!enabled) ? 0.04f : (flag ? 0.16f : 0.08f));
		Label(r, label, enabled ? CardPalette.StatText : DimText, GameFont.Small, TextAnchor.MiddleCenter);
		return enabled && Widgets.ButtonInvisible(r);
	}
}
