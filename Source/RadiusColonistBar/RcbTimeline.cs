using System.Collections.Generic;
using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbTimeline
{
	public const float SpineX = 13f;

	private const float SpineW = 2f;

	private const float SolidX = 14f;

	private const float HollowX = 9f;

	private const float TextNewX = 30f;

	private const float TextOldX = 26f;

	private static readonly float[] MarkerSize = new float[5] { 9f, 8f, 7f, 6f, 6f };

	private static readonly float[] MarkerAlpha = new float[5] { 1f, 0.85f, 0.62f, 0.44f, 0.3f };

	private static readonly float[] RowHeights = new float[5] { 36f, 21f, 19f, 18f, 17f };

	private static readonly float[] IconSize = new float[5] { 20f, 16f, 14f, 12f, 11f };

	private static readonly float[] ProjFade = new float[3] { 1f, 0.85f, 0.7f };

	private const float ProjRowH = 42f;

	private static int Clamp(int i, int n)
	{
		return (i >= 0) ? ((i >= n) ? (n - 1) : i) : 0;
	}

	public static float PastRowH(int i)
	{
		return RowHeights[Clamp(i, RowHeights.Length)];
	}

	public static float Height(List<TlEvent> past, bool hasNow, int nowChipRows, List<TlEvent> future)
	{
		float num = 0f;
		if (past != null)
		{
			for (int i = 0; i < past.Count; i++)
			{
				num += PastRowH(i);
			}
		}
		if (hasNow)
		{
			num += NowClusterH(nowChipRows) + 10f;
		}
		if (future != null && future.Count > 0)
		{
			num += 20f + (float)future.Count * 42f;
		}
		return num;
	}

	public static float NowClusterH(int chipRows)
	{
		return 32f + (float)Mathf.Max(1, chipRows) * 26f + 8f;
	}

	public static void Spine(Rect area)
	{
		if (Event.current.type == EventType.Repaint)
		{
			Widgets.DrawBoxSolid(new Rect(area.x + 13f, area.y, 2f, area.height), RcbPalette.Lift);
		}
	}

	public static float DrawPast(Rect area, float y, in TlEvent e, int i)
	{
		int num = Clamp(i, MarkerSize.Length);
		float num2 = PastRowH(i);
		float num3 = MarkerAlpha[num];
		float num4 = MarkerSize[num];
		Color color = ((e.col == default(Color)) ? RcbPalette.Stat : e.col);
		Widgets.DrawBoxSolid(new Rect(area.x + 14f - num4 * 0.5f, y + 6f, num4, num4), new Color(color.r, color.g, color.b, num3));
		bool flag = i == 0;
		float num5 = area.x + (flag ? 30f : 26f);
		float num6 = area.width - (num5 - area.x) - 4f;
		if (e.icon != null)
		{
			float num7 = IconSize[num];
			GUI.color = new Color(color.r, color.g, color.b, num3 * 0.92f);
			GUI.DrawTexture(new Rect(num5, y + 3f, num7, num7), e.icon, ScaleMode.ScaleToFit);
			GUI.color = Color.white;
			num5 += num7 + 5f;
			num6 -= num7 + 5f;
		}
		if (flag)
		{
			RcbPalette.MicroLabel(new Rect(num5, y + 2f, num6, 20f), e.title, new Color(color.r, color.g, color.b, num3), TextAnchor.MiddleLeft, shadow: false);
			string text = ((!string.IsNullOrEmpty(e.age) && !string.IsNullOrEmpty(e.sub)) ? (e.age + " - " + e.sub) : (e.sub ?? e.age));
			if (!string.IsNullOrEmpty(text))
			{
				Color textDim = RcbPalette.TextDim;
				textDim.a = 0.8f;
				RcbPalette.MicroLabel(new Rect(num5, y + 19f, num6, 16f), text, textDim, TextAnchor.MiddleLeft, shadow: false);
			}
		}
		else
		{
			float num8 = 0f;
			if (!string.IsNullOrEmpty(e.age))
			{
				Color textDim2 = RcbPalette.TextDim;
				textDim2.a = num3;
				num8 = Mathf.Min(70f, RcbTextWidth(e.age) + 6f);
				RcbPalette.MicroLabel(new Rect(area.xMax - num8 - 2f, y, num8, num2), e.age, textDim2, TextAnchor.MiddleRight, shadow: false);
			}
			RcbPalette.MicroLabel(new Rect(num5, y, Mathf.Max(10f, num6 - num8), num2), e.title, new Color(color.r, color.g, color.b, num3), TextAnchor.MiddleLeft, shadow: false);
		}
		return y + num2;
	}

	private static float RcbTextWidth(string s)
	{
		GameFont font = Text.Font;
		Text.Font = GameFont.Small;
		float result = RcbText.Width(s);
		Text.Font = font;
		return result;
	}

	public static float DrawProjected(Rect area, float y, in TlEvent e, int i)
	{
		float num = ProjFade[Clamp(i, ProjFade.Length)];
		Color color = ((e.col == default(Color)) ? RcbPalette.Accent : e.col);
		Color color2 = new Color(color.r, color.g, color.b, num);
		Widgets.DrawBoxSolid(new Rect(area.x + 9f, y + 8f, 10f, 10f), Palette.WithAlpha(color2, color2.a * 0.35f));
		float num2 = area.x + 30f;
		float num3 = area.width - (num2 - area.x) - 4f;
		float num4 = 0f;
		if (!string.IsNullOrEmpty(e.age))
		{
			Color textDim = RcbPalette.TextDim;
			textDim.a = num;
			num4 = Mathf.Min(80f, RcbTextWidth(e.age) + 6f);
			RcbPalette.MicroLabel(new Rect(area.xMax - num4 - 2f, y + 2f, num4, 20f), e.age, textDim, TextAnchor.MiddleRight, shadow: false);
		}
		RcbPalette.MicroLabel(new Rect(num2, y + 2f, Mathf.Max(10f, num3 - num4), 20f), e.title, color2, TextAnchor.MiddleLeft, shadow: false);
		if (!string.IsNullOrEmpty(e.sub))
		{
			Color textDim2 = RcbPalette.TextDim;
			textDim2.a = num * 0.85f;
			RcbPalette.MicroLabel(new Rect(num2, y + 21f, num3, 16f), e.sub, textDim2, TextAnchor.MiddleLeft, shadow: false);
		}
		return y + 42f;
	}

	public static float DrawRule(Rect area, float y, string label)
	{
		float num = 0f;
		if (!string.IsNullOrEmpty(label))
		{
			Color textDim = RcbPalette.TextDim;
			textDim.a = 0.62f;
			num = RcbTextWidth(label) + 8f;
			RcbPalette.MicroLabel(new Rect(area.x + 26f, y, num, 18f), label, textDim, TextAnchor.MiddleLeft, shadow: false);
		}
		float num2 = area.x + 26f + num;
		if (num2 < area.xMax - 2f)
		{
			Widgets.DrawBoxSolid(new Rect(num2, y + 9f, area.xMax - num2 - 2f, 1f), RcbPalette.Wash);
		}
		return y + 20f;
	}

	public static float DrawNow(Rect area, float y, string summary, List<TlEvent> chips, int chipRows)
	{
		float num = NowClusterH(chipRows);
		Rect r = new Rect(area.x + 22f, y, area.width - 24f, num);
		RcbPalette.DrawPlate(r, RcbPalette.PanelBG);
		Rect rect = new Rect(r.x + 1f, r.y + 1f, r.width - 2f, 30f);
		Widgets.DrawBoxSolid(rect, RcbPalette.BGD);
		RcbPalette.MicroLabel(new Rect(rect.x + 10f, rect.y, 44f, rect.height), "Now", RcbPalette.Accent, TextAnchor.MiddleLeft, shadow: false);
		if (!string.IsNullOrEmpty(summary))
		{
			RcbPalette.MicroLabel(new Rect(rect.x + 48f, rect.y, rect.width - 54f, rect.height), summary, RcbPalette.TextDim, TextAnchor.MiddleLeft, shadow: false);
		}
		float num2 = r.x + 8f;
		float num3 = r.y + 36f;
		float num4 = r.xMax - 8f;
		if (chips != null)
		{
			for (int i = 0; i < chips.Count; i++)
			{
				string title = chips[i].title;
				if (!string.IsNullOrEmpty(title))
				{
					bool flag = chips[i].icon != null;
					float num5 = RcbTextWidth(title) + 18f + (flag ? 16f : 0f);
					if (num2 + num5 > num4 && num2 > r.x + 8f)
					{
						num2 = r.x + 8f;
						num3 += 26f;
					}
					if (num3 + 22f > r.yMax - 2f)
					{
						break;
					}
					Rect r2 = new Rect(num2, num3, num5, 22f);
					Color color = ((chips[i].col == default(Color)) ? RcbPalette.TextDim : chips[i].col);
					Sdf.RoundedRect(r2, Palette.Flat.Item, 6f);
					float num6 = r2.x + 8f;
					if (flag)
					{
						GUI.color = color;
						GUI.DrawTexture(new Rect(num6, r2.y + 5f, 12f, 12f), chips[i].icon, ScaleMode.ScaleToFit);
						GUI.color = Color.white;
						num6 += 15f;
					}
					RcbPalette.MicroLabel(new Rect(num6, r2.y, r2.xMax - num6 - 3f, r2.height), title, Palette.Flat.ItemInk, TextAnchor.MiddleLeft, shadow: false);
					num2 += num5 + 5f;
				}
			}
		}
		return y + num + 10f;
	}

	public static int ChipRows(List<TlEvent> chips, float plateWidth)
	{
		if (chips == null || chips.Count == 0)
		{
			return 1;
		}
		float num = plateWidth - 16f;
		float num2 = 0f;
		int num3 = 1;
		for (int i = 0; i < chips.Count; i++)
		{
			string title = chips[i].title;
			if (!string.IsNullOrEmpty(title))
			{
				float num4 = RcbTextWidth(title) + 18f + ((chips[i].icon != null) ? 16f : 0f) + 5f;
				if (num2 + num4 > num && num2 > 0f)
				{
					num3++;
					num2 = 0f;
				}
				num2 += num4;
			}
		}
		return num3;
	}

	public static float Draw(Rect area, List<TlEvent> past, string nowSummary, List<TlEvent> nowChips, List<TlEvent> future, string emptyPast = "No recorded events yet", string emptyFuture = "Nothing projected")
	{
		Spine(area);
		float num = area.y;
		if (past != null && past.Count > 0)
		{
			for (int i = 0; i < past.Count; i++)
			{
				num = DrawPast(area, num, past[i], i);
			}
		}
		else
		{
			Color textDim = RcbPalette.TextDim;
			textDim.a = 0.62f;
			RcbPalette.MicroLabel(new Rect(area.x + 26f, num, area.width - 26f, 20f), emptyPast, textDim, TextAnchor.MiddleLeft, shadow: false);
			num += 22f;
		}
		if (nowChips != null || !string.IsNullOrEmpty(nowSummary))
		{
			num = DrawNow(area, num, nowSummary, nowChips, ChipRows(nowChips, area.width - 24f));
		}
		num = DrawRule(area, num, "Still to come");
		if (future != null && future.Count > 0)
		{
			for (int j = 0; j < future.Count; j++)
			{
				num = DrawProjected(area, num, future[j], j);
			}
		}
		else
		{
			Color textDim2 = RcbPalette.TextDim;
			textDim2.a = 0.5f;
			RcbPalette.MicroLabel(new Rect(area.x + 30f, num, area.width - 30f, 20f), emptyFuture, textDim2, TextAnchor.MiddleLeft, shadow: false);
			num += 22f;
		}
		return num;
	}
}
