using System;
using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class CCPattern
{
	internal struct Row
	{
		public Pawn pawn;

		public Thing thing;

		public string name;

		public string chip;

		public Color chipFill;

		public Color chipInk;

		public float bar;

		public string barLabel;

		public string chip2;

		public Color chip2Fill;

		public Color chip2Ink;

		public string link;

		public string linkTip;
	}

	internal struct ChipEntry
	{
		public string label;

		public Color fill;

		public Color ink;

		public object tag;

		public bool isLink;
	}

	public static float Primary(Rect r, RadiusIcon icon, string label, out bool clicked, ref float y, string secondaryLink = null, Action onSecondary = null)
	{
		float primaryH = UIKit.Flat.PrimaryH;
		float num = UIKit.Flat.PrimaryWidth(label, icon.Exists);
		clicked = UIKit.Flat.Primary(new Rect(r.x, y, num, primaryH), icon, label);
		if (!string.IsNullOrEmpty(secondaryLink))
		{
			float width = UIKit.Flat.LinkWidth(secondaryLink);
			if (UIKit.Flat.Link(new Rect(r.x + num + 10f, y, width, primaryH), secondaryLink))
			{
				onSecondary?.Invoke();
			}
		}
		y += primaryH + 18f;
		return y;
	}

	public static void Header(Rect r, string label, ref float y)
	{
		float sectionHeaderH = UIKit.Flat.SectionHeaderH;
		UIKit.Flat.SectionHeader(new Rect(r.x, y, r.width, sectionHeaderH), label);
		y += sectionHeaderH + 12f;
	}

	public static int ChipRow(Rect r, List<ChipEntry> chips, ref float y)
	{
		int result = -1;
		if (chips == null || chips.Count == 0)
		{
			return result;
		}
		float chipHBody = UIKit.Flat.ChipHBody;
		float num = r.x;
		for (int i = 0; i < chips.Count; i++)
		{
			ChipEntry chipEntry = chips[i];
			float num2 = (chipEntry.isLink ? (UIKit.Flat.LinkWidth(chipEntry.label) + 12f) : UIKit.Flat.ChipWidthBody(chipEntry.label));
			if (num + num2 > r.xMax && num > r.x)
			{
				num = r.x;
				y += chipHBody + 10f;
			}
			Rect rect = new Rect(num, y, num2, chipHBody);
			if (chipEntry.isLink)
			{
				if (UIKit.Flat.Link(new Rect(rect.x + 6f, rect.y, rect.width, rect.height), chipEntry.label))
				{
					result = i;
				}
			}
			else
			{
				UIKit.Flat.ChipBody(rect, chipEntry.label, Mouse.IsOver(rect) ? (chipEntry.fill * 1.08f) : chipEntry.fill, chipEntry.ink);
				if (Widgets.ButtonInvisible(rect))
				{
					result = i;
				}
			}
			num += num2 + 10f;
		}
		y += chipHBody + 18f;
		return result;
	}

	public static int Table(Rect r, List<Row> rows, ref Vector2 scroll, float y, string emptyText)
	{
		int result = -1;
		Rect rect = new Rect(r.x, y, r.width, r.yMax - y);
		if (rect.height < 54f)
		{
			return result;
		}
		UIKit.Flat.TableCard(rect);
		if (rows == null || rows.Count == 0)
		{
			if (!string.IsNullOrEmpty(emptyText))
			{
				RadiusFont.LabelAt(new Rect(rect.x + 14f, rect.y, rect.width - 28f, 54f), emptyText, 15f, Palette.Flat.InkMid);
			}
			return result;
		}
		float num = 54f;
		float num2 = (float)rows.Count * num;
		bool flag = num2 > rect.height;
		Rect viewRect = new Rect(0f, 0f, rect.width - (flag ? 16f : 0f), Mathf.Max(num2, rect.height));
		FlatScroll.Begin(rect, ref scroll, viewRect);
		try
		{
			for (int i = 0; i < rows.Count; i++)
			{
				float num3 = (float)i * num;
				if (RcbPalette.RowVisible(num3, num, scroll, rect.height, num))
				{
					Rect row = new Rect(0f, num3, viewRect.width, num);
					if (DrawRow(row, rows[i], i))
					{
						result = i;
					}
				}
			}
		}
		finally
		{
			FlatScroll.End();
		}
		return result;
	}

	private static bool DrawRow(Rect row, Row d, int index)
	{
		bool hovered = Mouse.IsOver(row);
		UIKit.Flat.TableRow(row, index, hovered);
		float num = row.x + 14f;
		float y = row.center.y;
		Rect r = new Rect(num, row.y + 8f, 30f, 38f);
		Avatar(r, d.pawn, d.thing);
		num += 44f;
		string name = d.name;
		if (string.IsNullOrEmpty(name) && d.pawn != null)
		{
			name = PawnUiCache.Get(d.pawn).name;
		}
		RadiusFont.LabelAt(new Rect(num, row.y, 130f, row.height), RadiusFont.FitAt(name, 130f, 15f), 15f, Palette.Flat.Ink);
		num += 144f;
		float num2 = row.xMax - 14f;
		bool result = false;
		if (!string.IsNullOrEmpty(d.link))
		{
			float num3 = UIKit.Flat.LinkWidth(d.link);
			Rect rect = new Rect(num2 - num3, row.y, num3, row.height);
			result = UIKit.Flat.Link(rect, d.link);
			if (!string.IsNullOrEmpty(d.linkTip) && Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, d.linkTip);
			}
			num2 -= num3 + 14f;
		}
		if (!string.IsNullOrEmpty(d.chip))
		{
			float a = Mathf.Max(200f, UIKit.Flat.ChipWidthBody(d.chip));
			a = Mathf.Min(a, Mathf.Max(40f, num2 - num));
			float chipHBody = UIKit.Flat.ChipHBody;
			Rect rect2 = new Rect(num, y - chipHBody * 0.5f, a, chipHBody);
			UIKit.Flat.ChipBody(rect2, RadiusFont.FitAt(d.chip, a - 24f, 15f), d.chipFill, d.chipInk);
			num += a + 14f;
		}
		if (d.bar >= 0f && num2 - num >= 60f)
		{
			float num4 = Mathf.Min(150f, num2 - num);
			Rect r2 = new Rect(num, y - 11f, num4, 22f);
			UIKit.Flat.Bar(r2, d.bar, d.barLabel);
			num += num4 + 14f;
		}
		if (!string.IsNullOrEmpty(d.chip2) && num2 - num >= 40f)
		{
			float num5 = Mathf.Min(UIKit.Flat.ChipWidthBody(d.chip2), num2 - num);
			float chipHBody2 = UIKit.Flat.ChipHBody;
			Rect rect3 = new Rect(num, y - chipHBody2 * 0.5f, num5, chipHBody2);
			UIKit.Flat.ChipBody(rect3, RadiusFont.FitAt(d.chip2, num5 - 24f, 15f), d.chip2Fill, d.chip2Ink);
		}
		return result;
	}

	private static void Avatar(Rect r, Pawn p, Thing t)
	{
		if (Event.current.type != EventType.Repaint)
		{
			return;
		}
		if (p != null)
		{
			Texture texture = null;
			try
			{
				texture = PortraitsCache.Get(p, new Vector2(30f, 38f), Rot4.South, default(Vector3), 1.2f);
			}
			catch
			{
			}
			if (texture != null)
			{
				GUI.DrawTexture(r, texture);
			}
			return;
		}
		ThingDef thingDef = t?.def;
		if (thingDef?.uiIcon != null)
		{
			float num = Mathf.Min(r.width, r.height);
			Color color = GUI.color;
			GUI.color = thingDef.uiIconColor;
			GUI.DrawTexture(new Rect(r.center.x - num * 0.5f, r.center.y - num * 0.5f, num, num), thingDef.uiIcon);
			GUI.color = color;
		}
	}

	public static void AlertChip(ref Row row, string text)
	{
		row.chip = text;
		row.chipFill = Palette.Flat.Alert;
		row.chipInk = Palette.Flat.AlertInk;
	}

	public static void ItemChip(ref Row row, string text)
	{
		row.chip = text;
		row.chipFill = Palette.Flat.Item;
		row.chipInk = Palette.Flat.ItemInk;
	}

	public static void FactionChip(ref Row row, string text)
	{
		row.chip = text;
		row.chipFill = Palette.Flat.Faction;
		row.chipInk = Palette.Flat.FactionInk;
	}

	public static void StateChip(ref Row row, string text)
	{
		row.chip = text;
		row.chipFill = Palette.Flat.State;
		row.chipInk = Palette.Flat.StateInk;
	}

	public static void SkillChip2(ref Row row, string text)
	{
		row.chip2 = text;
		row.chip2Fill = Palette.Flat.Skill;
		row.chip2Ink = Palette.Flat.SkillInk;
	}

	public static void FactionChip2(ref Row row, string text)
	{
		row.chip2 = text;
		row.chip2Fill = Palette.Flat.Faction;
		row.chip2Ink = Palette.Flat.FactionInk;
	}

	public static void ItemChip2(ref Row row, string text)
	{
		row.chip2 = text;
		row.chip2Fill = Palette.Flat.Item;
		row.chip2Ink = Palette.Flat.ItemInk;
	}

	public static ChipEntry Faction(string label, object tag)
	{
		return new ChipEntry
		{
			label = label,
			fill = Palette.Flat.Faction,
			ink = Palette.Flat.FactionInk,
			tag = tag
		};
	}

	public static ChipEntry Item(string label, object tag)
	{
		return new ChipEntry
		{
			label = label,
			fill = Palette.Flat.Item,
			ink = Palette.Flat.ItemInk,
			tag = tag
		};
	}

	public static ChipEntry Link(string label, object tag)
	{
		return new ChipEntry
		{
			label = label,
			isLink = true,
			tag = tag
		};
	}
}
