using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
public static class SpeechBubbles
{
	private class Rec
	{
		public string text;

		public int entryTick;

		public int scannedAt = -999999;

		public int logId = -1;

		public string fit;

		public string fitSrc;

		public float fitW;

		public float fitH;

		public int fitSize;
	}

	private struct Q
	{
		public Rect anchor;

		public Pawn pawn;

		public float scale;

		public bool party;
	}

	private static readonly Texture2D bubbleTex = IconSet.Chrome.Bubble;

	private static readonly Texture2D tailTex = IconSet.Get("Chrome/Bubble_Tail").Texture;

	private static readonly Dictionary<int, Rec> cache = new Dictionary<int, Rec>();

	private static readonly FieldInfo initiatorF = AccessTools.Field(typeof(PlayLogEntry_Interaction), "initiator");

	private const int ShowTicks = 480;

	private const int FadeTicks = 140;

	private const int ScanEvery = 30;

	private const int ScanDepth = 40;

	private static readonly List<Q> queue = new List<Q>(16);

	private static readonly List<Rect> placed = new List<Rect>(8);

	private static int qFrame = -1;

	private const float PadX = 10f;

	private const float PadY = 8f;

	private static readonly GUIContent measure = new GUIContent();

	public static void Prune(HashSet<int> liveIds)
	{
		RcbUtil.PruneById(cache, liveIds);
	}

	public static bool TryGetLine(Pawn p, out string line, out float alpha)
	{
		line = null;
		alpha = 0f;
		if (p == null || Current.ProgramState != ProgramState.Playing)
		{
			return false;
		}
		PlayLog playLog = Find.PlayLog;
		TickManager tickManager = Find.TickManager;
		if (playLog == null || tickManager == null)
		{
			return false;
		}
		int ticksAbs = tickManager.TicksAbs;
		int thingIDNumber = p.thingIDNumber;
		if (!cache.TryGetValue(thingIDNumber, out var value))
		{
			value = new Rec();
			cache[thingIDNumber] = value;
		}
		if (ticksAbs - value.scannedAt >= 30)
		{
			value.scannedAt = ticksAbs;
			Scan(p, value, ticksAbs, playLog);
		}
		int num = ticksAbs - value.entryTick;
		if (value.text == null || num > 480 || num < 0)
		{
			return false;
		}
		line = value.text;
		alpha = ((num > 340) ? Mathf.Clamp01((float)(480 - num) / 140f) : 1f);
		return true;
	}

	private static void Scan(Pawn p, Rec rec, int now, PlayLog log)
	{
		try
		{
			List<LogEntry> allEntries = log.AllEntries;
			int num = Mathf.Min(40, allEntries.Count);
			for (int i = 0; i < num; i++)
			{
				LogEntry logEntry = allEntries[i];
				if (logEntry == null)
				{
					continue;
				}
				if (now - logEntry.Timestamp > 480)
				{
					break;
				}
				if (!(logEntry is PlayLogEntry_Interaction))
				{
					continue;
				}
				if (initiatorF != null)
				{
					if (initiatorF.GetValue(logEntry) != p)
					{
						continue;
					}
				}
				else if (!logEntry.Concerns(p))
				{
					continue;
				}
				if (logEntry.LogID == rec.logId)
				{
					break;
				}
				string text = logEntry.ToGameStringFromPOV(p);
				if (text.NullOrEmpty())
				{
					continue;
				}
				rec.text = text.StripTags();
				rec.entryTick = logEntry.Timestamp;
				rec.logId = logEntry.LogID;
				break;
			}
		}
		catch
		{
		}
	}

	public static void DrawParty(Rect portrait, Pawn p, RadiusColonistBarSettings s, float scale)
	{
		Enqueue(portrait, p, scale, party: true);
	}

	public static void DrawBar(Rect portrait, Pawn p, RadiusColonistBarSettings s, float scale)
	{
		Enqueue(portrait, p, scale, party: false);
	}

	private static void Enqueue(Rect anchor, Pawn p, float scale, bool party)
	{
		if (p != null && Event.current.type == EventType.Repaint)
		{
			if (Time.frameCount != qFrame)
			{
				qFrame = Time.frameCount;
				queue.Clear();
			}
			queue.Add(new Q
			{
				anchor = anchor,
				pawn = p,
				scale = scale,
				party = party
			});
		}
	}

	public static void Flush()
	{
		if (Event.current.type != EventType.Repaint || qFrame != Time.frameCount || queue.Count == 0)
		{
			return;
		}
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		placed.Clear();
		if (settings != null)
		{
			for (int i = 0; i < queue.Count; i++)
			{
				Q q = queue[i];
				try
				{
					Draw(q.anchor, q.pawn, settings, q.scale, q.party);
				}
				catch
				{
				}
			}
		}
		queue.Clear();
		placed.Clear();
	}

	private static float AutoWidth(float scale)
	{
		return Mathf.Clamp(300f * scale, 220f, 460f);
	}

	private static void Draw(Rect portrait, Pawn p, RadiusColonistBarSettings s, float scale, bool party)
	{
		if (!TryGetLine(p, out var line, out var alpha))
		{
			return;
		}
		GameFont font = Text.Font;
		bool wordWrap = Text.WordWrap;
		TextAnchor anchor = Text.Anchor;
		Text.Font = GameFont.Small;
		Text.WordWrap = true;
		Text.Anchor = TextAnchor.UpperLeft;
		float px = 15f;
		float num = RadiusFont.LineHAt(px);
		float v = ((s.bubbleMaxWpx > 0f) ? s.bubbleMaxWpx : AutoWidth(scale));
		v = Mathf.Clamp(RcbGuard.Sane(v, 300f), 120f, (float)UI.screenWidth * 0.45f);
		float b = Mathf.Max(30f, v - 20f);
		float a = RadiusFont.WidthAt(line, px) + 1f;
		float num2 = Mathf.Min(Mathf.Max(a, 30f), b);
		float num3 = Mathf.Clamp(RcbGuard.Sane(s.bubbleMaxLines, 6f), 2f, 12f);
		float num4 = num * num3;
		string text = FitFor(p, line, num2, num4, px);
		float num5 = Mathf.Min(RadiusFont.HeightAt(text, num2, px), num4);
		float num6 = num2 + 20f;
		float num7 = num5 + 16f;
		int num9;
		Rect rect;
		if (party)
		{
			float num8 = portrait.xMax + 9f;
			num9 = 2;
			if (num8 + num6 > (float)UI.screenWidth - 3f)
			{
				num8 = portrait.x - 9f - num6;
				num9 = 3;
			}
			float y = Mathf.Clamp(portrait.center.y - num7 * 0.5f, 3f, (float)UI.screenHeight - num7 - 3f);
			rect = new Rect(num8, y, num6, num7);
		}
		else
		{
			num9 = 0;
			float y2 = Mathf.Max(portrait.yMax + 9f, BarSelection.BarBottom() + 4f);
			float x = Mathf.Clamp(portrait.center.x - num6 * 0.5f, 3f, (float)UI.screenWidth - num6 - 3f);
			rect = new Rect(x, y2, num6, num7);
		}
		float num10 = Mathf.Max(rect.yMax, (float)UI.screenHeight * 0.4f);
		for (int i = 0; i <= placed.Count; i++)
		{
			bool flag = false;
			for (int j = 0; j < placed.Count; j++)
			{
				if (rect.Overlaps(placed[j]) && placed[j].yMax + 4f > rect.y)
				{
					rect.y = placed[j].yMax + 4f;
					flag = true;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		if (rect.yMax > num10 || rect.yMax > (float)UI.screenHeight - 3f)
		{
			Text.Font = font;
			Text.WordWrap = wordWrap;
			Text.Anchor = anchor;
			return;
		}
		placed.Add(rect);
		float along = ((num9 >= 2) ? Mathf.Clamp(portrait.center.y, rect.y + 9f, rect.yMax - 9f) : Mathf.Clamp(portrait.center.x, rect.x + 9f, rect.xMax - 9f));
		Color col = CardPalette.BubbleBg(s);
		col.a *= alpha;
		Color value = CardPalette.BubbleText(s);
		value.a *= alpha;
		DrawTail(rect, num9, along, col);
		Draw9(rect, bubbleTex, col);
		RadiusFont.LabelAt(new Rect(rect.x + 10f, rect.y + 8f, num2, rect.height - 16f + 1f), text, px, value, TextAnchor.UpperLeft, bold: false, wrap: true);
		GUI.color = Color.white;
		Text.Font = font;
		Text.WordWrap = wordWrap;
		Text.Anchor = anchor;
	}

	internal static string FitCard(Pawn p, string line, float w, float maxH)
	{
		return FitFor(p, line, w, maxH, RcbFont.Px(GameFont.Small, bold: false));
	}

	private static string FitFor(Pawn p, string line, float w, float maxH, float px)
	{
		int num = Mathf.RoundToInt(px);
		if (!cache.TryGetValue(p.thingIDNumber, out var value))
		{
			return Ellipsize(line, w, maxH, px);
		}
		if (value.fit != null && (object)value.fitSrc == line && value.fitW == w && value.fitH == maxH && value.fitSize == num)
		{
			return value.fit;
		}
		value.fit = Ellipsize(line, w, maxH, px);
		value.fitSrc = line;
		value.fitW = w;
		value.fitH = maxH;
		value.fitSize = num;
		return value.fit;
	}

	private static float RawHeight(string s, float w, float px)
	{
		GUIStyle gUIStyle = RadiusFont.StyleAt(px);
		bool wordWrap = gUIStyle.wordWrap;
		gUIStyle.wordWrap = true;
		measure.text = s;
		float result = gUIStyle.CalcHeight(measure, w);
		gUIStyle.wordWrap = wordWrap;
		return result;
	}

	private static string Ellipsize(string src, float w, float maxH, float px)
	{
		if (string.IsNullOrEmpty(src) || RawHeight(src, w, px) <= maxH)
		{
			return src;
		}
		int num = 1;
		int num2 = src.Length;
		while (num < num2)
		{
			int num3 = (num + num2 + 1) / 2;
			if (RawHeight(src.Substring(0, num3) + "…", w, px) <= maxH)
			{
				num = num3;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		int num4 = num;
		int num5 = src.LastIndexOf(' ', Mathf.Min(num4, src.Length - 1));
		if (num5 > num4 - 16 && num5 > 0)
		{
			num4 = num5;
		}
		return src.Substring(0, num4).TrimEnd(' ', ',', ';', ':', '-') + "…";
	}

	private static void Draw9(Rect rect, Texture2D tex, Color col)
	{
		if (tex == null)
		{
			Widgets.DrawBoxSolid(rect, col);
			return;
		}
		GUI.color = col;
		float num = Mathf.Min((float)tex.width * 0.25f, rect.height * 0.5f, rect.width * 0.5f);
		Widgets.DrawTexturePart(new Rect(rect.x, rect.y, num, num), new Rect(0f, 0f, 0.25f, 0.25f), tex);
		Widgets.DrawTexturePart(new Rect(rect.xMax - num, rect.y, num, num), new Rect(0.75f, 0f, 0.25f, 0.25f), tex);
		Widgets.DrawTexturePart(new Rect(rect.x, rect.yMax - num, num, num), new Rect(0f, 0.75f, 0.25f, 0.25f), tex);
		Widgets.DrawTexturePart(new Rect(rect.xMax - num, rect.yMax - num, num, num), new Rect(0.75f, 0.75f, 0.25f, 0.25f), tex);
		Widgets.DrawTexturePart(new Rect(rect.x + num, rect.y, rect.width - 2f * num, num), new Rect(0.25f, 0f, 0.5f, 0.25f), tex);
		Widgets.DrawTexturePart(new Rect(rect.x + num, rect.yMax - num, rect.width - 2f * num, num), new Rect(0.25f, 0.75f, 0.5f, 0.25f), tex);
		Widgets.DrawTexturePart(new Rect(rect.x, rect.y + num, num, rect.height - 2f * num), new Rect(0f, 0.25f, 0.25f, 0.5f), tex);
		Widgets.DrawTexturePart(new Rect(rect.xMax - num, rect.y + num, num, rect.height - 2f * num), new Rect(0.75f, 0.25f, 0.25f, 0.5f), tex);
		Widgets.DrawTexturePart(new Rect(rect.x + num, rect.y + num, rect.width - 2f * num, rect.height - 2f * num), new Rect(0.25f, 0.25f, 0.5f, 0.5f), tex);
		GUI.color = Color.white;
	}

	private static void DrawTail(Rect box, int dir, float along, Color col)
	{
		if (!(tailTex == null))
		{
			float num;
			float num2;
			float angle;
			switch (dir)
			{
			case 0:
				num = along;
				num2 = box.y;
				angle = 0f;
				break;
			case 1:
				num = along;
				num2 = box.yMax;
				angle = 180f;
				break;
			case 2:
				num = box.x;
				num2 = along;
				angle = 270f;
				break;
			default:
				num = box.xMax;
				num2 = along;
				angle = 90f;
				break;
			}
			if (!RcbGuard.Finite(num) || !RcbGuard.Finite(num2))
			{
				string[] obj = new string[6]
				{
					"bx=",
					num.ToString(),
					" by=",
					num2.ToString(),
					" box=",
					null
				};
				Rect rect = box;
				obj[5] = rect.ToString();
				RcbGuard.WarnOnce("SpeechBubbles.DrawTail", string.Concat(obj));
			}
			else
			{
				Rect position = new Rect(num - 6f, num2 - 8f, 12f, 8f);
				Matrix4x4 matrix = GUI.matrix;
				GUIUtility.RotateAroundPivot(angle, new Vector2(num, num2));
				GUI.color = col;
				GUI.DrawTexture(position, tailTex);
				GUI.color = Color.white;
				GUI.matrix = matrix;
			}
		}
	}
}
