using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class BadgeLayer
{
	private enum Kind
	{
		Icon,
		NumBox,
		Chip,
		Marks
	}

	private struct Entry
	{
		public Kind kind;

		public Rect r;

		public Texture2D tex;

		public Color col;

		public string text;

		public Pawn pawn;
	}

	private static readonly List<Entry> entries = new List<Entry>(32);

	private static int frame = -1;

	private static void Stamp()
	{
		if (Time.frameCount != frame)
		{
			frame = Time.frameCount;
			entries.Clear();
		}
	}

	public static void Icon(Rect r, Texture2D tex, Color col)
	{
		if (!(tex == null))
		{
			Stamp();
			entries.Add(new Entry
			{
				kind = Kind.Icon,
				r = r,
				tex = tex,
				col = col
			});
		}
	}

	public static void NumBox(Rect r, Color bg, string text)
	{
		Stamp();
		entries.Add(new Entry
		{
			kind = Kind.NumBox,
			r = r,
			col = bg,
			text = text
		});
	}

	public static void Marks(Pawn p, Rect r)
	{
		if (p != null)
		{
			Stamp();
			entries.Add(new Entry
			{
				kind = Kind.Marks,
				r = r,
				pawn = p
			});
		}
	}

	public static void Chip(Rect r, Color col, string text)
	{
		Stamp();
		entries.Add(new Entry
		{
			kind = Kind.Chip,
			r = r,
			col = col,
			text = text,
			tex = null
		});
	}

	public static void Flush()
	{
		if (Event.current.type != EventType.Repaint || frame != Time.frameCount || entries.Count == 0)
		{
			return;
		}
		TextAnchor anchor = Text.Anchor;
		GameFont font = Text.Font;
		for (int i = 0; i < entries.Count; i++)
		{
			Entry entry = entries[i];
			switch (entry.kind)
			{
			case Kind.Icon:
			{
				float num3 = Mathf.Max(1f, entry.r.width * 0.08f);
				GUI.color = new Color(0f, 0f, 0f, 0.6f);
				GUI.DrawTexture(new Rect(entry.r.x + num3, entry.r.y + num3, entry.r.width, entry.r.height), entry.tex);
				GUI.color = entry.col;
				GUI.DrawTexture(entry.r, entry.tex);
				GUI.color = Color.white;
				break;
			}
			case Kind.NumBox:
			{
				float microLineH = RcbPalette.MicroLineH;
				Rect rect = entry.r;
				if (rect.height < microLineH || rect.width < microLineH)
				{
					float num = Mathf.Max(rect.width, microLineH);
					float num2 = Mathf.Max(rect.height, microLineH);
					rect = new Rect(rect.center.x - num * 0.5f, rect.center.y - num2 * 0.5f, num, num2);
				}
				Widgets.DrawBoxSolid(rect, entry.col);
				RcbPalette.MicroLabel(rect, entry.text, Color.white, TextAnchor.MiddleCenter, shadow: false);
				break;
			}
			case Kind.Chip:
				RcbPalette.MicroLabel(entry.r, entry.text, entry.col, TextAnchor.LowerLeft);
				break;
			case Kind.Marks:
				MarksHost.DrawFor(entry.pawn, entry.r);
				break;
			}
		}
		Text.Anchor = anchor;
		Text.Font = font;
		GUI.color = Color.white;
		entries.Clear();
	}
}
