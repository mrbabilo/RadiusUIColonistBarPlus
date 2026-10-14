using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RadiusColonistBar;

internal static class RcbIsland
{
	public const float PortW = 46f;

	public const float PortH = 56f;

	public const float PadX = 11f;

	public const float PadY = 7f;

	public const float Gap = 9f;

	public const float ColMin = 78f;

	public const float SegW = 155f;

	public const float ColMax = 150f;

	public const float ColMinBare = 46f;

	public const float MeterH = 5f;

	public const float WepH = 15f;

	public const float RowGap = 3f;

	public const float StackGap = 5f;

	private static int colHFrame = -1;

	private static int colHNext = int.MinValue;

	private static float colHCache;

	private static float portColCache;

	private static float colHU = -1f;

	private static float colTextCache;

	private static float colMinCache = 78f;

	private static readonly List<Pawn> measureRoster = new List<Pawn>(32);

	public const float PillLaneH = 14f;

	public const float MarksLaneH = 15f;

	public const float LaneGap = 2f;

	public const float ChipGap = 4f;

	private const int RowRoyal = 0;

	private const int RowRole = 1;

	private const int RowJob = 2;

	private static readonly string[] rowText = new string[3];

	private static readonly int[] rowKind = new int[3];

	public const float NameH = 19f;

	public const float LineH = 14f;

	public const float RowTight = 1f;

	public const float DevStripH = 31f;

	public const float GearStripH = 26f;

	public const float GearSize = 18f;

	public const float RailBaseH = 24f;

	private static int tokFrame = -1;

	private static int railHFrame = -1;

	private static float railHCache;

	private static FieldInfo devRowW;

	private static bool devRowLooked;

	private static readonly Color InkRGB = RcbPalette.Ink;

	private static readonly Color SegFill = RcbPalette.BG;

	private static readonly Color SegHover = RcbPalette.PanelBG;

	private static readonly Color SegSel = Palette.Flat.Invert;

	private static readonly Color InkInv = Palette.Flat.InkInvert;

	private static readonly Color InkInvDim = Palette.WithAlpha(Palette.Flat.InkInvert, 0.62f);

	private static readonly Color ChipBg = RcbPalette.BGD;

	private static readonly Color Dim2 = RcbPalette.TextDim2;

	private static readonly Color PinInk = RcbPalette.Stat;

	private static int slabFrame = -1;

	private static Rect lastSlab;

	private static float slabOffsetX;

	private static int offsetFrame = -1;

	private static int sepFrame = -1;

	private static float prevXMax;

	private static int prevGroup;

	private static readonly Dictionary<int, int> groupCounts = new Dictionary<int, int>();

	private static readonly Dictionary<int, int> groupOfPawn = new Dictionary<int, int>();

	private static readonly Color DevChip = RcbPalette.BG;

	private static Rect lastGrip;

	private static readonly List<int> tabList = new List<int>();

	private static readonly List<Rect> tabRects = new List<Rect>();

	private static readonly List<int> railTok = new List<int>();

	private static readonly List<int> railSpill = new List<int>();

	private static Rect lastSpill;

	private static Rect lastRailBand;

	private static bool chromeClaimedDown;

	private const float SpillW = 46f;

	private const float SqNameMaxW = 62f;

	private static readonly Color TabOn = RcbPalette.Lift;

	private static int spillCapN = -1;

	private static string spillCap = "+0";

	private static readonly string[] tabCapCache = new string[6];

	private static readonly int[] tabCapCount = new int[6];

	private static readonly float[] tabCapWidth = new float[6];

	private static readonly List<string> sqCap = new List<string>();

	private static readonly List<int> sqCapCount = new List<int>();

	private static readonly List<float> sqCapW = new List<float>();

	private static readonly List<string> sqCapName = new List<string>();

	private static readonly List<string> sqCapS = new List<string>();

	private static readonly List<float> sqCapSW = new List<float>();

	public const float ArrowW = 26f;

	public const float ArrowGap = 5f;

	public const float PipsH = 13f;

	private static Rect lastArrowL;

	private static Rect lastArrowR;

	private static readonly List<Rect> lastPips = new List<Rect>(8);

	private static readonly List<Pawn> cardPawns = new List<Pawn>(32);

	private static readonly List<Rect> cardRects = new List<Rect>(32);

	private static bool marqueeOn;

	private static Vector2 marqueeA;

	private static Vector2 marqueeB;

	private static bool marqueeAdditive;

	private static Rect lastGear;

	private static Rect lastCC;

	private static readonly List<Pawn> summaryRoster = new List<Pawn>(32);

	private static int summaryNext = int.MinValue;

	private static bool summaryValid;

	private static int sumCrit;

	private static int sumWarn;

	private static int sumDrafted;

	private static int sumAway;

	private const float TabPadX = 14f;

	private const float TabCountCell = 40f;

	private const float TabGap = 11f;

	private const float TabDot = 9f;

	private const float TabDotGap = 5f;

	internal static bool Stacked
	{
		get
		{
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			return settings != null && settings.cardLayout == 1;
		}
	}

	public static float StripH => Prefs.DevMode ? 31f : (26f * Mathf.Clamp01(RcbIslandAnim.ContentA));

	internal static float RailH
	{
		get
		{
			if (railHFrame == Time.frameCount)
			{
				return railHCache;
			}
			railHFrame = Time.frameCount;
			float num = 0f;
			if (RcbCohorts.Active)
			{
				EnsureTokens();
				if (tabList.Count >= 2)
				{
					num = 24f;
				}
			}
			return railHCache = num;
		}
	}

	internal static float RailNow => RailH * Mathf.Clamp01(RcbIslandAnim.ContentA);

	private static Color Ink
	{
		get
		{
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			Color inkRGB = InkRGB;
			inkRGB.a = ((settings != null) ? Mathf.Clamp01(RcbGuard.Sane(settings.barOpacityPct, 92f) / 100f) : 0.92f);
			return inkRGB;
		}
	}

	public static Rect LastSlab => lastSlab;

	internal static bool SlabDrawnThisFrame => slabFrame == Time.frameCount;

	internal static Rect MarqueeRect
	{
		get
		{
			if (!marqueeOn)
			{
				return default(Rect);
			}
			return new Rect(Mathf.Min(marqueeA.x, marqueeB.x), Mathf.Min(marqueeA.y, marqueeB.y), Mathf.Abs(marqueeB.x - marqueeA.x), Mathf.Abs(marqueeB.y - marqueeA.y));
		}
	}

	public static Vector2 SegSize(float scale)
	{
		float num = Mathf.Max(0.5f, scale);
		float num2 = Mathf.Clamp(TextColumnWidth(num) / num, colMinCache, 150f);
		float x;
		float num3;
		if (Stacked)
		{
			x = (11f + num2 + 11f) * num;
			num3 = 7f * num * 2f + PortColumnHeight(num) + 5f * num + ColumnHeight(num);
		}
		else
		{
			x = (66f + num2 + 11f) * num;
			num3 = 7f * num * 2f + Mathf.Max(PortColumnHeight(num), ColumnHeight(num));
		}
		num3 += MarksReserve(num);
		float heightT = RcbIslandAnim.HeightT;
		if (heightT >= 0.999f)
		{
			return new Vector2(x, num3);
		}
		float a = Mathf.Max(26f, 26f * num);
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings != null && settings.collapsedMaxHpx > 0f)
		{
			a = Mathf.Max(16f, Mathf.Min(a, settings.collapsedMaxHpx));
		}
		return new Vector2(x, Mathf.Lerp(a, num3, heightT));
	}

	private static float ColumnHeight(float u)
	{
		EnsureColumns(u);
		return colHCache;
	}

	private static float TextColumnWidth(float u)
	{
		EnsureColumns(u);
		return colTextCache;
	}

	private static float PortColumnHeight(float u)
	{
		EnsureColumns(u);
		return portColCache;
	}

	internal static void SetMeasureRoster(List<ColonistBar.Entry> entries)
	{
		measureRoster.Clear();
		if (entries == null)
		{
			return;
		}
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].pawn != null)
			{
				measureRoster.Add(entries[i].pawn);
			}
		}
	}

	internal static void ClearMeasureRoster()
	{
		measureRoster.Clear();
	}

	private static void EnsureColumns(float u)
	{
		if (colHFrame == Time.frameCount)
		{
			return;
		}
		colHFrame = Time.frameCount;
		if (Time.frameCount < colHNext && Mathf.Approximately(u, colHU) && colHCache > 0f)
		{
			return;
		}
		colHNext = Time.frameCount + 15;
		colHU = u;
		colHCache = 56f * u;
		portColCache = PortColFor(u, 0f);
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null)
		{
			return;
		}
		colMinCache = ((!IsNone(settings.barTopStat) || !IsNone(settings.barBottomStat)) ? 78f : 46f);
		List<ColonistBar.Entry> list = null;
		int count = measureRoster.Count;
		if (count == 0)
		{
			list = Find.ColonistBar?.Entries;
			if (list == null)
			{
				return;
			}
			count = list.Count;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < count; i++)
		{
			Pawn pawn = ((list != null) ? list[i].pawn : measureRoster[i]);
			if (pawn == null)
			{
				continue;
			}
			float num4 = PawnColumnHeight(pawn, settings, u);
			if (num4 > num)
			{
				num = num4;
			}
			float num5 = ChipHeightFor(pawn, settings, u);
			if (num5 > num2)
			{
				num2 = num5;
			}
			if (settings.showName)
			{
				float num6 = RcbFont.Width(GetNameOf(pawn), bold: true, GameFont.Small);
				if (num6 > num3)
				{
					num3 = num6;
				}
			}
			int num7 = IdentityRows(pawn, settings);
			for (int j = 0; j < num7; j++)
			{
				float x = RcbText.CalcSize(rowText[j]).x;
				if (x > num3)
				{
					num3 = x;
				}
			}
		}
		colTextCache = num3 + 4f;
		if (num > 0f)
		{
			colHCache = num;
		}
		portColCache = PortColFor(u, num2);
	}

	private static float PillLane(float u)
	{
		return Mathf.Max(12f, 14f * u);
	}

	private static float PortColFor(float u, float chipH)
	{
		float num = 56f * u + 2f * u + PillLane(u);
		if (chipH > 0f)
		{
			num += 4f * u + chipH;
		}
		return num;
	}

	internal static float MarksReserve(float u)
	{
		return MarksHost.Live ? (17f * u) : 0f;
	}

	private static float ChipHeightFor(Pawn p, RadiusColonistBarSettings s, float u)
	{
		if (s.draftedWeaponCard && p.Drafted)
		{
			return 0f;
		}
		return RcbWeaponChips.Height(p, s, u);
	}

	private static int IdentityRows(Pawn p, RadiusColonistBarSettings s)
	{
		int num = 0;
		string text = (s.showRoyalTitle ? Patch_DrawColonist_Overlay.GetRoyalTitle(p) : null);
		if (!string.IsNullOrEmpty(text))
		{
			rowText[num] = text;
			rowKind[num] = 0;
			num++;
		}
		string text2 = (s.showIdeoRole ? Patch_DrawColonist_Overlay.GetIdeoRole(p) : null);
		if (!string.IsNullOrEmpty(text2))
		{
			rowText[num] = text2;
			rowKind[num] = 1;
			num++;
		}
		if (s.showJobTitle && (!s.jobTitleFallbackOnly || num <= 0))
		{
			string jobTitle = Patch_DrawColonist_Overlay.GetJobTitle(p);
			if (!string.IsNullOrEmpty(jobTitle))
			{
				rowText[num] = jobTitle;
				rowKind[num] = 2;
				num++;
			}
		}
		return num;
	}

	private static Color RowColor(int kind, Pawn p, RadiusColonistBarSettings s, bool sel, bool away)
	{
		if (away)
		{
			return sel ? InkInvDim : Dim2;
		}
		switch (kind switch
		{
			1 => s.titleColorIdeo, 
			0 => s.titleColorRoyal, 
			_ => s.titleColorJob, 
		})
		{
		case 0:
			return Palette.Flat.Accent;
		case 1:
			return Palette.Flat.Faction;
		case 2:
			return sel ? InkInv : RcbPalette.Stat;
		case 4:
		{
			PawnUiCache.Snap snap = PawnUiCache.Get(p);
			if (snap.hasIdeoColor)
			{
				return snap.ideoColor;
			}
			break;
		}
		}
		return sel ? InkInvDim : Dim2;
	}

	internal static float PawnColumnHeight(Pawn p, RadiusColonistBarSettings s, float u)
	{
		float num = 1f * u;
		float num2 = 0f;
		if (s.showName)
		{
			num2 += 19f * u + num;
		}
		num2 += (float)IdentityRows(p, s) * (14f * u + num);
		if (s.showJobLabel)
		{
			num2 += 14f * u + num;
		}
		if (!IsNone(s.barTopStat) || !IsNone(s.barBottomStat))
		{
			num2 += 5f * u + 3f * u;
		}
		return (num2 > 0f) ? (num2 - num) : 0f;
	}

	private static string Headline(Pawn p, RadiusColonistBarSettings s, out Color col)
	{
		col = RcbPalette.Stat;
		switch (CardPalette.State(p))
		{
		case RcbState.Bad:
			col = RcbPalette.Bad;
			break;
		case RcbState.Warn:
			col = RcbPalette.Warn;
			break;
		default:
			if (p.Drafted)
			{
				col = RcbPalette.Warn;
			}
			break;
		}
		if (RcbAlertBand.Visible)
		{
			col = Dim2;
		}
		string jobText = Patch_DrawColonist_Overlay.GetJobText(p);
		if (!string.IsNullOrEmpty(jobText))
		{
			return jobText;
		}
		if (p.Drafted)
		{
			return "RCB_State_Drafted".Translate();
		}
		return "RCB_State_Idle".Translate();
	}

	private static bool IsNone(string key)
	{
		return string.IsNullOrEmpty(key) || key == "__none__";
	}

	private static void EnsureTokens()
	{
		if (tokFrame != Time.frameCount)
		{
			tokFrame = Time.frameCount;
			RcbCohorts.BuildTokens(tabList);
		}
	}

	public static float ContentTop(float scale)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		float b = 7f * Mathf.Max(0.5f, scale);
		float b2 = 27f + Mathf.Max(10f, b);
		float num = (Prefs.DevMode ? (32f + Mathf.Max(10f, b)) : Mathf.Lerp(Mathf.Max(5f, b), b2, Mathf.Clamp01(RcbIslandAnim.ContentA)));
		num += RailNow;
		if (settings != null)
		{
			num += settings.barTopOffset;
		}
		return Mathf.Max(2f, num);
	}

	public static float DevRowWidth()
	{
		if (!Prefs.DevMode)
		{
			return 0f;
		}
		if (!devRowLooked)
		{
			devRowLooked = true;
			try
			{
				devRowW = AccessTools.Field(typeof(DebugWindowsOpener), "widgetRowFinalX");
			}
			catch
			{
				devRowW = null;
			}
		}
		try
		{
			DebugWindowsOpener debugWindowsOpener = Find.UIRoot?.debugWindowOpener;
			if (debugWindowsOpener != null && devRowW != null)
			{
				float num = (float)devRowW.GetValue(debugWindowsOpener);
				if (RcbGuard.Finite(num) && num > 1f)
				{
					return num;
				}
			}
		}
		catch
		{
		}
		return 260f;
	}

	internal static void ResetFrame()
	{
		slabFrame = -1;
		sepFrame = -1;
	}

	internal static float SlabOffsetX()
	{
		if (offsetFrame == Time.frameCount)
		{
			return slabOffsetX;
		}
		offsetFrame = Time.frameCount;
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		float num = ((settings != null) ? Mathf.Clamp(RcbGuard.Sane(settings.barPosXPct, 50f), 0f, 100f) : 50f);
		float num2 = (RcbGuard.Finite(lastSlab) ? lastSlab.width : 0f);
		float num3 = ((num2 > 1f) ? Mathf.Max(0f, ((float)UI.screenWidth - num2) * 0.5f) : 0f);
		slabOffsetX = Mathf.Round((num - 50f) / 50f * num3);
		return slabOffsetX;
	}

	private static void EnsureGroupCounts()
	{
		groupCounts.Clear();
		groupOfPawn.Clear();
		ColonistBar colonistBar = Find.ColonistBar;
		if (colonistBar == null)
		{
			return;
		}
		List<ColonistBar.Entry> entries = colonistBar.Entries;
		if (entries == null)
		{
			return;
		}
		for (int i = 0; i < entries.Count; i++)
		{
			int num = entries[i].group;
			groupCounts.TryGetValue(num, out var value);
			groupCounts[num] = value + 1;
			Pawn pawn = entries[i].pawn;
			if (pawn != null)
			{
				groupOfPawn[pawn.thingIDNumber] = num;
			}
		}
	}

	private static void Separator(Rect seg, Pawn p, float u)
	{
		if (Event.current.type == EventType.Repaint)
		{
			if (sepFrame != Time.frameCount)
			{
				sepFrame = Time.frameCount;
				EnsureGroupCounts();
				prevXMax = float.MinValue;
			}
			int num = GroupOf(p);
			float num2 = seg.x - prevXMax;
			if (!(prevXMax <= -1.7014117E+38f) && !(num2 <= 0f) && !(num2 > 60f * u) && num != prevGroup)
			{
				groupCounts.TryGetValue(num, out var value);
				DrawNotch(prevXMax + num2 * 0.5f, seg.y, seg.height, u, value);
			}
			prevXMax = seg.xMax;
			prevGroup = num;
		}
	}

	private static void DrawNotch(float cx, float top, float height, float u, int count)
	{
		if (count > 0)
		{
			string text = SmallStrings.Int(count);
			Vector2 vector = RcbText.CalcSize(text);
			Rect r = new Rect(cx - (vector.x + 6f * u) * 0.5f, top + height * 0.5f - (vector.y + 4f) * 0.5f, vector.x + 6f * u, vector.y + 4f);
			if (RcbGuard.Finite(r))
			{
				RcbSpatial.Pill(r, ChipBg);
				RcbPalette.MicroLabel(r, text, Dim2, TextAnchor.MiddleCenter, shadow: false);
			}
		}
	}

	private static int GroupOf(Pawn p)
	{
		if (p == null)
		{
			return -1;
		}
		int value;
		return groupOfPawn.TryGetValue(p.thingIDNumber, out value) ? value : (-1);
	}

	internal static void DrawSlabOnce(float scale)
	{
		if (Event.current.type != EventType.Repaint || slabFrame == Time.frameCount)
		{
			return;
		}
		slabFrame = Time.frameCount;
		cardPawns.Clear();
		cardRects.Clear();
		lastGrip = default(Rect);
		ColonistBar colonistBar = Find.ColonistBar;
		if (colonistBar == null)
		{
			return;
		}
		List<Vector2> drawLocs = colonistBar.DrawLocs;
		if (drawLocs == null || drawLocs.Count == 0)
		{
			return;
		}
		Vector2 size = colonistBar.Size;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		for (int i = 0; i < drawLocs.Count; i++)
		{
			Vector2 vector = drawLocs[i];
			if (RcbGuard.Finite(vector.x) && RcbGuard.Finite(vector.y))
			{
				num = Mathf.Min(num, vector.x);
				num2 = Mathf.Min(num2, vector.y);
				num3 = Mathf.Max(num3, vector.x + size.x);
				num4 = Mathf.Max(num4, vector.y + size.y);
			}
		}
		if (num3 <= num || num4 <= num2)
		{
			return;
		}
		float num5 = 7f * scale;
		float num6 = Mathf.Min(0f, num2 - num5);
		float num7 = num3 - num + num5 * 2f;
		float num8 = RcbDevToolbar.RowWidth();
		if (num8 <= 0f)
		{
			num8 = DevRowWidth();
		}
		float num9 = 88f;
		if (num8 > 0f)
		{
			num7 = Mathf.Max(num7, num8 + (num9 + num5) * 2f);
		}
		if (RailH > 0f)
		{
			num7 = Mathf.Max(num7, 230f);
		}
		bool active = RcbBarWindow.Active;
		if (active)
		{
			num7 += 62f;
		}
		float num10 = RcbAlertBand.Height();
		float num11 = ((active && RcbBarWindow.PageCount > 1) ? 13f : 0f);
		float heightT = RcbIslandAnim.HeightT;
		if (heightT < 0.999f)
		{
			float a = TabWidth(scale);
			if (Prefs.DevMode && num8 > 0f)
			{
				a = Mathf.Max(a, num8 + num5 * 4f);
			}
			num7 = Mathf.Lerp(a, num7, heightT);
		}
		Rect r = new Rect((float)UI.screenWidth * 0.5f - num7 * 0.5f + SlabOffsetX(), num6, num7, num4 - num6 + num5 + num11 + num10);
		if (RcbGuard.Finite(r))
		{
			r = (lastSlab = new Rect(Mathf.Round(r.x), Mathf.Round(r.y), Mathf.Round(r.width), Mathf.Round(r.height)));
			float radius = Mathf.Clamp(10f * scale, 6f, 16f);
			RcbSpatial.SurfaceFlush(r, Ink);
			RcbSpatial.NotchFillets(r, Ink, radius);
			float stripH = StripH;
			bool flag = r.height > stripH + 6f;
			if (flag && Prefs.DevMode)
			{
				RcbDevToolbar.NotifyIslandDrew();
				DrawDevRow(r);
			}
			if (flag && RcbIslandAnim.ContentA > 0.5f)
			{
				DrawGear(r);
			}
			DrawCohortRail(r);
			if (active)
			{
				DrawWindowChrome(r, num, num3, num2, num4, num11);
			}
			if (num10 > 0f)
			{
				RcbAlertBand.Draw(new Rect(r.x, r.yMax - num10, r.width, num10));
			}
			Rect marqueeRect = MarqueeRect;
			if (marqueeRect.width > 1f || marqueeRect.height > 1f)
			{
				RcbSpatial.Rounded(marqueeRect, Palette.WithAlpha(RcbPalette.Accent, 0.16f), 4f);
				GUI.color = Palette.WithAlpha(RcbPalette.Accent, 0.85f);
				Widgets.DrawBox(marqueeRect);
				GUI.color = Color.white;
			}
			float summaryA = RcbIslandAnim.SummaryA;
			if (summaryA > 0.01f)
			{
				float num12 = (Prefs.DevMode ? (r.y + 31f) : r.y);
				float num13 = r.yMax - num10;
				Rect band = new Rect(r.x, num12, r.width, Mathf.Max(1f, num13 - num12));
				DrawTab(band, scale, summaryA);
			}
		}
	}

	private static void DrawDevRow(Rect slab)
	{
		float y = slab.y + 3.5f;
		RcbDevToolbar.Draw(slab.center.x, y);
	}

	private static void DrawGear(Rect slab)
	{
		if (RcbIslandAnim.ContentA < 0.5f)
		{
			return;
		}
		float num = 18f;
		Rect rect = new Rect(slab.xMax - num - 10f, slab.y + (StripH - num) * 0.5f, num, num);
		if (RcbGuard.Finite(rect))
		{
			lastGear = rect;
			bool flag = Mouse.IsOver(rect);
			RcbSpatial.ChipFast(rect, flag ? RcbPalette.PanelBG : RcbPalette.BG);
			Texture2D gear = RCBTextures.Gear;
			if (gear != null)
			{
				Color color = GUI.color;
				GUI.color = (flag ? RcbPalette.Stat : RcbPalette.TextDim);
				GUI.DrawTexture(rect.ContractedBy(3f), gear);
				GUI.color = color;
			}
			else
			{
				RcbPalette.MicroLabel(rect, "⚙", flag ? RcbPalette.Stat : Dim2, TextAnchor.MiddleCenter, shadow: false);
			}
			if (flag)
			{
				TooltipHandler.TipRegion(rect, "RCB_Gear_Tip".Translate());
			}
			Rect r = new Rect(slab.xMax - num * 3f - 22f, rect.y, num, num);
			GripHandle(r, 1f, plate: true);
			Rect rect2 = (lastCC = new Rect(rect.x - num - 6f, rect.y, num, num));
			bool flag2 = Mouse.IsOver(rect2);
			RcbSpatial.ChipFast(rect2, flag2 ? RcbPalette.PanelBG : RcbPalette.BG);
			Texture2D commandCenter = RCBTextures.CommandCenter;
			if (commandCenter != null)
			{
				Color color2 = GUI.color;
				GUI.color = (flag2 ? RcbPalette.Stat : RcbPalette.TextDim);
				GUI.DrawTexture(rect2.ContractedBy(2f), commandCenter);
				GUI.color = color2;
			}
			else
			{
				RcbPalette.MicroLabel(rect2, "☷", flag2 ? RcbPalette.Stat : Dim2, TextAnchor.MiddleCenter, shadow: false);
			}
			if (flag2)
			{
				TooltipHandler.TipRegion(rect2, "RCB_CC_Tip".Translate());
			}
		}
	}

	private static void GripHandle(Rect r, float u, bool plate)
	{
		if (!RcbGuard.Finite(r) || r.width < 6f || r.height < 6f)
		{
			return;
		}
		lastGrip = r;
		bool flag = Mouse.IsOver(r);
		if (plate)
		{
			RcbSpatial.ChipFast(r, flag ? RcbPalette.PanelBG : RcbPalette.BG);
		}
		float num = Mathf.Max(3f, Mathf.Round(4f * u));
		float num2 = Mathf.Max(2f, Mathf.Round(2f * u));
		float num3 = num * 2f + num2;
		float num4 = Mathf.Round(r.center.x - num3 * 0.5f);
		float num5 = Mathf.Round(r.center.y - num3 * 0.5f);
		Color color = (flag ? RcbPalette.Stat : RcbPalette.TextDim);
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				Sdf.Disc(new Rect(num4 + (float)j * (num + num2), num5 + (float)i * (num + num2), num, num), color);
			}
		}
		if (flag)
		{
			TooltipHandler.TipRegion(r, ((RcbIslandAnim.OpenT > 0.5f) ? "RCB_Grip_Collapse" : "RCB_Grip_Expand").Translate());
		}
	}

	private static void DrawCohortRail(Rect slab)
	{
		tabRects.Clear();
		railTok.Clear();
		railSpill.Clear();
		lastSpill = default(Rect);
		lastRailBand = default(Rect);
		if (RcbIslandAnim.ContentA < 0.5f || RailH <= 0f)
		{
			return;
		}
		float num = 19f;
		float num2 = slab.y + StripH + 1f;
		lastRailBand = new Rect(slab.x, num2, slab.width, 24f);
		float y = num2 + (24f - num) * 0.5f;
		float num3 = slab.width - 24f;
		GameFont font = Text.Font;
		Text.Font = GameFont.Small;
		bool shortNames = false;
		float num4 = RunWidth(shortNames: false);
		if (num4 > num3)
		{
			shortNames = true;
			num4 = RunWidth(shortNames: true);
		}
		int token = RcbCohorts.Token;
		railTok.AddRange(tabList);
		if (num4 > num3)
		{
			num4 += 46f;
			int count = railTok.Count;
			while (num4 > num3 && count-- > 0)
			{
				int num5 = PickSpill(railTok, token);
				if (num5 < 0)
				{
					break;
				}
				num4 -= TabWidth(railTok[num5], shortNames) + 2f;
				railSpill.Add(railTok[num5]);
				railTok.RemoveAt(num5);
			}
		}
		float num6 = slab.x + Mathf.Max(12f, (slab.width - num4) * 0.5f);
		for (int i = 0; i < railTok.Count; i++)
		{
			int num7 = railTok[i];
			float num8 = TabWidth(num7, shortNames);
			Rect rect = new Rect(num6, y, num8, num);
			tabRects.Add(rect);
			bool flag = num7 == token;
			bool flag2 = Mouse.IsOver(rect);
			if (flag)
			{
				CardChrome.Rounded(rect, TabOn, 6f);
			}
			int num9 = RcbCohorts.TokenSeverity(num7);
			float num10 = ((num9 >= 0) ? 9f : 0f);
			RcbPalette.MicroLabel(new Rect(rect.x + 7f, rect.y, rect.width - 14f - num10, rect.height), TabCaption(num7, shortNames), (flag || flag2) ? RcbPalette.Stat : Dim2, TextAnchor.MiddleLeft, shadow: false);
			if (num9 >= 0)
			{
				RcbSpatial.Dot(new Rect(rect.xMax - 11f, rect.center.y - 2.5f, 5f, 5f), Palette.Flat.Alert);
			}
			if (flag2)
			{
				TooltipHandler.TipRegion(rect, TabTip(num7, num9));
			}
			num6 += num8 + 2f;
		}
		if (railSpill.Count > 0)
		{
			Rect rect2 = (lastSpill = new Rect(num6, y, 44f, num));
			bool flag3 = Mouse.IsOver(rect2);
			int num11 = int.MaxValue;
			for (int j = 0; j < railSpill.Count; j++)
			{
				int num12 = RcbCohorts.TokenSeverity(railSpill[j]);
				if (num12 >= 0 && num12 < num11)
				{
					num11 = num12;
				}
			}
			float num13 = ((num11 != int.MaxValue) ? 9f : 0f);
			RcbPalette.MicroLabel(new Rect(rect2.x + 7f, rect2.y, rect2.width - 14f - num13, rect2.height), SpillCaption(railSpill.Count), flag3 ? RcbPalette.Stat : Dim2, TextAnchor.MiddleLeft, shadow: false);
			if (num11 != int.MaxValue)
			{
				RcbSpatial.Dot(new Rect(rect2.xMax - 11f, rect2.center.y - 2.5f, 5f, 5f), Palette.Flat.Alert);
			}
			if (flag3)
			{
				TooltipHandler.TipRegion(rect2, "RCB_Cohort_More".Translate(railSpill.Count.Named("COUNT")));
			}
		}
		Text.Font = font;
	}

	private static int PickSpill(List<int> vis, int cur)
	{
		int result = -1;
		int num = -1;
		for (int i = 1; i < vis.Count; i++)
		{
			int num2 = vis[i];
			if (num2 != cur)
			{
				bool flag = RcbCohorts.TokenSeverity(num2) >= 0;
				bool flag2 = num2 >= 100;
				int num3 = ((!flag) ? 2 : 0) + (flag2 ? 1 : 0);
				if (num3 >= num)
				{
					num = num3;
					result = i;
				}
			}
		}
		return result;
	}

	private static string SpillCaption(int n)
	{
		if (n != spillCapN)
		{
			spillCapN = n;
			spillCap = "+" + SmallStrings.Int(n);
		}
		return spillCap;
	}

	private static void OpenSpillMenu()
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		for (int i = 0; i < tabList.Count; i++)
		{
			int t = tabList[i];
			if (railSpill.Contains(t))
			{
				string text = RcbCohorts.TokenLabel(t) + "  " + RcbCohorts.TokenCount(t);
				if (RcbCohorts.TokenSeverity(t) >= 0)
				{
					text += " !";
				}
				list.Add(new FloatMenuOption(text, delegate
				{
					RcbCohorts.SetToken(t);
				}));
			}
		}
		if (list.Count > 0)
		{
			Find.WindowStack.Add(new FloatMenu(list));
		}
	}

	private static string TabCaption(int t, bool shortNames)
	{
		if (t < 100)
		{
			RcbCohort c = (RcbCohort)t;
			int num = RcbCohorts.Count(c);
			if (tabCapCache[t] == null || tabCapCount[t] != num)
			{
				tabCapCount[t] = num;
				tabCapCache[t] = RcbCohorts.Label(c) + "  " + SmallStrings.Int(num);
				tabCapWidth[t] = RcbText.CalcSize(tabCapCache[t]).x;
			}
			return tabCapCache[t];
		}
		int num2 = t - 100;
		string text = BarSquads.Get?.SquadName(num2) ?? "Squad";
		int num3 = RcbCohorts.TokenCount(t);
		while (sqCap.Count <= num2)
		{
			sqCap.Add(null);
			sqCapCount.Add(-1);
			sqCapW.Add(0f);
			sqCapName.Add(null);
			sqCapS.Add(null);
			sqCapSW.Add(0f);
		}
		if (sqCap[num2] == null || sqCapCount[num2] != num3 || sqCapName[num2] != text)
		{
			sqCapCount[num2] = num3;
			sqCapName[num2] = text;
			sqCap[num2] = text + "  " + SmallStrings.Int(num3);
			sqCapW[num2] = RcbText.CalcSize(sqCap[num2]).x;
			string text2 = RCBLabel.Truncate(text, 62f, 1, 0);
			if (text2 == text)
			{
				sqCapS[num2] = sqCap[num2];
				sqCapSW[num2] = sqCapW[num2];
			}
			else
			{
				sqCapS[num2] = text2 + "  " + SmallStrings.Int(num3);
				sqCapSW[num2] = RcbText.CalcSize(sqCapS[num2]).x;
			}
		}
		return shortNames ? sqCapS[num2] : sqCap[num2];
	}

	private static float TabWidth(int t, bool shortNames)
	{
		TabCaption(t, shortNames);
		float num = ((t >= 100) ? (shortNames ? sqCapSW[t - 100] : sqCapW[t - 100]) : tabCapWidth[t]);
		return num + 14f + ((RcbCohorts.TokenSeverity(t) >= 0) ? 9f : 0f);
	}

	private static float RunWidth(bool shortNames)
	{
		float num = 0f;
		for (int i = 0; i < tabList.Count; i++)
		{
			num += TabWidth(tabList[i], shortNames) + 2f;
		}
		return num;
	}

	private static string TabTip(int t, int sev)
	{
		string text = "RCB_Cohort_Tip".Translate(RcbCohorts.TokenLabel(t).Named("COHORT")).ToString();
		if (sev >= 0)
		{
			text = text + "\n" + "RCB_Cohort_TipAlert".Translate().ToString();
		}
		return text + "\n" + "RCB_Cohort_TipAll".Translate().ToString();
	}

	private static void DrawWindowChrome(Rect slab, float x0, float x1, float y0, float y1, float pipsH)
	{
		lastArrowL = (lastArrowR = default(Rect));
		lastPips.Clear();
		if (RcbIslandAnim.ContentA < 0.5f)
		{
			return;
		}
		float num = Mathf.Min(y1 - y0, 46f);
		float num2 = (y0 + y1) * 0.5f;
		Rect r = new Rect(x0 - 5f - 26f, num2 - num * 0.5f, 26f, num);
		Rect r2 = new Rect(x1 + 5f, num2 - num * 0.5f, 26f, num);
		lastArrowL = r;
		lastArrowR = r2;
		Chevron(r, left: true, RcbBarWindow.HiddenLeft, RcbBarWindow.SeverityLeft);
		Chevron(r2, left: false, RcbBarWindow.HiddenRight, RcbBarWindow.SeverityRight);
		if (!(pipsH <= 0f))
		{
			int pageCount = RcbBarWindow.PageCount;
			int currentPage = RcbBarWindow.CurrentPage;
			float num3 = (float)pageCount * 5f + (float)(pageCount - 1) * 5f;
			float num4 = slab.center.x - num3 * 0.5f;
			float y2 = y1 + 4f;
			for (int i = 0; i < pageCount; i++)
			{
				Rect r3 = new Rect(num4, y2, 5f, 5f);
				lastPips.Add(new Rect(r3.x - 3f, r3.y - 4f, 11f, 13f));
				RcbSpatial.Dot(r3, (i == currentPage) ? RcbPalette.Stat : new Color(1f, 1f, 1f, 0.28f));
				num4 += 10f;
			}
		}
	}

	private static void Chevron(Rect r, bool left, int hidden, int sev)
	{
		if (!RcbGuard.Finite(r) || hidden <= 0)
		{
			if (RcbGuard.Finite(r))
			{
				RcbSpatial.Rounded(r, RcbPalette.WashFaint, 7f);
			}
			return;
		}
		bool flag = Mouse.IsOver(r);
		RcbSpatial.Rounded(r, new Color(1f, 1f, 1f, flag ? 0.16f : 0.08f), 7f);
		RcbPalette.MicroLabel(new Rect(r.x, r.y, r.width, r.height - 8f), left ? "‹" : "›", flag ? Color.white : RcbPalette.Stat, TextAnchor.MiddleCenter, shadow: false);
		RcbPalette.MicroLabel(new Rect(r.x, r.yMax - 12f, r.width, 10f), SmallStrings.Int(hidden), Dim2, TextAnchor.MiddleCenter, shadow: false);
		if (sev >= 0)
		{
			RcbSpatial.Dot(new Rect(r.xMax - 8f, r.y + 3f, 5f, 5f), RcbAlertBand.SeverityColor(sev));
		}
		if (flag)
		{
			TooltipHandler.TipRegion(r, "RCB_Window_Tip".Translate(hidden.ToString().Named("COUNT")));
		}
	}

	private static bool OverACard(Vector2 pos)
	{
		for (int i = 0; i < cardRects.Count; i++)
		{
			if (cardRects[i].Contains(pos))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HandleMarquee()
	{
		Event current = Event.current;
		if (current == null)
		{
			return false;
		}
		if (marqueeOn)
		{
			if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
			{
				marqueeOn = false;
				current.Use();
				return true;
			}
			if (current.rawType == EventType.MouseDrag)
			{
				marqueeB = current.mousePosition;
				current.Use();
				return true;
			}
			if (current.rawType == EventType.MouseUp)
			{
				marqueeB = current.mousePosition;
				ApplyMarquee();
				marqueeOn = false;
				current.Use();
				return true;
			}
			return false;
		}
		if (!RcbInput.LeftPress(current))
		{
			return false;
		}
		if (RcbIslandAnim.ContentA < 0.9f)
		{
			return false;
		}
		if (!RcbGuard.Finite(lastSlab) || !lastSlab.Contains(current.mousePosition))
		{
			return false;
		}
		if (cardRects.Count == 0 || OverACard(current.mousePosition))
		{
			return false;
		}
		if (RcbGuard.Finite(lastGear) && lastGear.Contains(current.mousePosition))
		{
			return false;
		}
		if (RcbGuard.Finite(lastCC) && lastCC.Contains(current.mousePosition))
		{
			return false;
		}
		if (RcbGuard.Finite(lastArrowL) && lastArrowL.Contains(current.mousePosition))
		{
			return false;
		}
		if (RcbGuard.Finite(lastArrowR) && lastArrowR.Contains(current.mousePosition))
		{
			return false;
		}
		for (int i = 0; i < tabRects.Count; i++)
		{
			if (tabRects[i].Contains(current.mousePosition))
			{
				return false;
			}
		}
		for (int j = 0; j < lastPips.Count; j++)
		{
			if (lastPips[j].Contains(current.mousePosition))
			{
				return false;
			}
		}
		float num = StripH + RailNow;
		if ((Prefs.DevMode || RailNow > 0f) && current.mousePosition.y < lastSlab.y + num)
		{
			return false;
		}
		marqueeOn = true;
		marqueeA = (marqueeB = current.mousePosition);
		marqueeAdditive = current.shift || current.control;
		current.Use();
		return true;
	}

	private static void ApplyMarquee()
	{
		Rect marqueeRect = MarqueeRect;
		Selector selector = Find.Selector;
		if (selector == null)
		{
			return;
		}
		bool flag = marqueeRect.width < 4f && marqueeRect.height < 4f;
		if (!marqueeAdditive)
		{
			selector.ClearSelection();
		}
		if (flag)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < cardRects.Count && i < cardPawns.Count; i++)
		{
			if (!cardRects[i].Overlaps(marqueeRect))
			{
				continue;
			}
			Pawn pawn = cardPawns[i];
			if (pawn == null)
			{
				continue;
			}
			try
			{
				if (pawn.Dead)
				{
					if (pawn.Corpse != null)
					{
						selector.Select(pawn.Corpse, playSound: false, forceDesignatorDeselect: false);
						num++;
					}
				}
				else
				{
					selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
					num++;
				}
			}
			catch
			{
			}
		}
		if (num > 0)
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
		}
	}

	internal static bool TryChromeClickAt(Vector2 p)
	{
		if (RcbIslandAnim.ContentA < 0.5f && (!RcbGuard.Finite(lastGrip) || !lastGrip.Contains(p)))
		{
			return false;
		}
		if (RcbGuard.Finite(lastGrip) && lastGrip.width > 1f && lastGrip.Contains(p))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			RcbIslandAnim.ToggleFold();
			return true;
		}
		if (RcbGuard.Finite(lastCC) && lastCC.width > 1f && lastCC.Contains(p))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			Dialog_CommandCenter dialog_CommandCenter = Find.WindowStack.WindowOfType<Dialog_CommandCenter>();
			if (dialog_CommandCenter != null)
			{
				dialog_CommandCenter.Close(doCloseSound: false);
			}
			else
			{
				Dialog_CommandCenter.Open();
			}
			return true;
		}
		if (RcbGuard.Finite(lastGear) && lastGear.width > 1f && lastGear.Contains(p))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			if (RadiusColonistBarMod.Instance != null)
			{
				Find.WindowStack.Add(new Dialog_ModSettings(RadiusColonistBarMod.Instance));
			}
			return true;
		}
		return false;
	}

	internal static void HandleChromeInput()
	{
		Event current = Event.current;
		bool flag = RcbInput.LeftPress(current);
		if (flag)
		{
			chromeClaimedDown = false;
		}
		if (RcbDevToolbar.HandleInput())
		{
			if (flag)
			{
				chromeClaimedDown = true;
			}
		}
		else if (!marqueeOn && chromeClaimedDown && current != null && current.rawType == EventType.MouseUp)
		{
			chromeClaimedDown = false;
			current.Use();
		}
		else if (flag && RcbGuard.Finite(lastGrip) && lastGrip.width > 1f && lastGrip.Contains(current.mousePosition))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			RcbIslandAnim.ToggleFold();
			chromeClaimedDown = true;
			current.Use();
		}
		else
		{
			if (HandleMarquee())
			{
				return;
			}
			if (Event.current.type == EventType.ScrollWheel && RcbIslandAnim.ContentA >= 0.5f && RcbBarWindow.Active && RcbGuard.Finite(lastSlab) && lastSlab.Contains(Event.current.mousePosition))
			{
				float y = Event.current.delta.y;
				if (Mathf.Abs(y) > 0.01f)
				{
					RcbBarWindow.Step((y > 0f) ? 1 : (-1));
					Event.current.Use();
					return;
				}
			}
			if (RcbIslandAnim.ContentA < 0.5f)
			{
				return;
			}
			Rect r = lastGear;
			if (!RcbGuard.Finite(r) || r.width < 2f)
			{
				return;
			}
			Event current2 = Event.current;
			if (RcbInput.RightPress(current2) && RcbGuard.Finite(lastSlab) && lastSlab.Contains(current2.mousePosition) && !OverACard(current2.mousePosition))
			{
				if (RcbGuard.Finite(lastRailBand) && lastRailBand.height > 1f && lastRailBand.Contains(current2.mousePosition) && RcbCohorts.Token != 0)
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					RcbCohorts.SetToken(0);
				}
				chromeClaimedDown = true;
				current2.Use();
			}
			else
			{
				if (!RcbInput.LeftPress(current2))
				{
					return;
				}
				if (RcbBarWindow.Active)
				{
					if (RcbGuard.Finite(lastArrowL) && lastArrowL.Contains(current2.mousePosition) && RcbBarWindow.HiddenLeft > 0)
					{
						SoundDefOf.Click.PlayOneShotOnCamera();
						RcbBarWindow.Step(-1);
						chromeClaimedDown = true;
						current2.Use();
						return;
					}
					if (RcbGuard.Finite(lastArrowR) && lastArrowR.Contains(current2.mousePosition) && RcbBarWindow.HiddenRight > 0)
					{
						SoundDefOf.Click.PlayOneShotOnCamera();
						RcbBarWindow.Step(1);
						chromeClaimedDown = true;
						current2.Use();
						return;
					}
					for (int i = 0; i < lastPips.Count; i++)
					{
						if (lastPips[i].Contains(current2.mousePosition))
						{
							SoundDefOf.Click.PlayOneShotOnCamera();
							RcbBarWindow.GoToPage(i);
							chromeClaimedDown = true;
							current2.Use();
							return;
						}
					}
				}
				for (int j = 0; j < tabRects.Count && j < railTok.Count; j++)
				{
					if (tabRects[j].Contains(current2.mousePosition))
					{
						SoundDefOf.Click.PlayOneShotOnCamera();
						RcbCohorts.SetToken(railTok[j]);
						chromeClaimedDown = true;
						current2.Use();
						return;
					}
				}
				if (RcbGuard.Finite(lastSpill) && lastSpill.width > 1f && lastSpill.Contains(current2.mousePosition))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					OpenSpillMenu();
					chromeClaimedDown = true;
					current2.Use();
				}
				else if (RcbGuard.Finite(lastCC) && lastCC.Contains(current2.mousePosition))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					Dialog_CommandCenter dialog_CommandCenter = Find.WindowStack.WindowOfType<Dialog_CommandCenter>();
					if (dialog_CommandCenter != null)
					{
						dialog_CommandCenter.Close(doCloseSound: false);
					}
					else
					{
						Dialog_CommandCenter.Open();
					}
					chromeClaimedDown = true;
					current2.Use();
				}
				else if (r.Contains(current2.mousePosition))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					if (RadiusColonistBarMod.Instance != null)
					{
						Find.WindowStack.Add(new Dialog_ModSettings(RadiusColonistBarMod.Instance));
					}
					chromeClaimedDown = true;
					current2.Use();
				}
			}
		}
	}

	internal static void SetSummaryRoster(List<Pawn> pawns)
	{
		summaryRoster.Clear();
		if (pawns != null)
		{
			summaryRoster.AddRange(pawns);
		}
		summaryValid = true;
		summaryNext = int.MinValue;
	}

	internal static float TabWidth(float scale)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		float num = ((settings == null || settings.collapsedShowCount) ? 51f : 0f) + 51f;
		return Mathf.Round((28f + num) * Mathf.Max(0.5f, scale));
	}

	private static void EnsureSummaryCounts()
	{
		if (Time.frameCount < summaryNext)
		{
			return;
		}
		summaryNext = Time.frameCount + 15;
		sumCrit = (sumWarn = (sumDrafted = (sumAway = 0)));
		for (int i = 0; i < summaryRoster.Count; i++)
		{
			Pawn pawn = summaryRoster[i];
			if (pawn != null)
			{
				bool flag = IsAway(pawn);
				RcbState rcbState = ((!flag) ? CardPalette.State(pawn) : RcbState.Good);
				if (flag)
				{
					sumAway++;
				}
				if (!flag && pawn.Drafted)
				{
					sumDrafted++;
				}
				switch (rcbState)
				{
				case RcbState.Bad:
					sumCrit++;
					break;
				case RcbState.Warn:
					sumWarn++;
					break;
				}
			}
		}
	}

	private static void DrawTab(Rect band, float scale, float alpha)
	{
		if (!summaryValid || summaryRoster.Count == 0)
		{
			List<ColonistBar.Entry> list = Find.ColonistBar?.Entries;
			summaryRoster.Clear();
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].pawn != null)
					{
						summaryRoster.Add(list[i].pawn);
					}
				}
			}
			summaryNext = int.MinValue;
		}
		EnsureSummaryCounts();
		if (summaryRoster.Count != 0)
		{
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			float num = Mathf.Max(0.5f, scale);
			Color color = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, alpha);
			bool flag = settings?.collapsedShowCount ?? true;
			float num2 = Mathf.Round(9f * num);
			float num3 = Mathf.Round(5f * num);
			float num4 = (flag ? Mathf.Round(40f * num) : 0f);
			float num5 = (flag ? Mathf.Round(11f * num) : 0f);
			float num6 = num4 + num5 + num2 * 4f + num3 * 3f;
			float num7 = Mathf.Round(band.center.x - num6 * 0.5f);
			float num8 = Mathf.Round(band.center.y);
			bool flag2 = Mouse.IsOver(band);
			if (flag)
			{
				RcbFont.Label(new Rect(num7, band.y, num4, band.height), SmallStrings.Int(summaryRoster.Count), bold: true, GameFont.Medium, flag2 ? RcbPalette.Stat : RcbPalette.TextDim, TextAnchor.MiddleCenter);
				num7 += num4 + num5;
			}
			float y = num8 - num2 * 0.5f;
			KindDot(new Rect(num7, y, num2, num2), sumCrit, RcbPalette.Bad);
			num7 += num2 + num3;
			KindDot(new Rect(num7, y, num2, num2), sumWarn, RcbPalette.Warn);
			num7 += num2 + num3;
			KindDot(new Rect(num7, y, num2, num2), sumDrafted, RcbPalette.Accent);
			num7 += num2 + num3;
			KindDot(new Rect(num7, y, num2, num2), sumAway, Palette.Flat.SegOff);
			GUI.color = color;
		}
	}

	private static void KindDot(Rect r, int count, Color lit)
	{
		RcbSpatial.Dot(r, (count > 0) ? lit : RcbPalette.Lift);
	}

	internal static void DrawSegment(Rect rect, Rect pr, Pawn p, float scale, RadiusColonistBarSettings s, float dmgFlash)
	{
		float contentA = RcbIslandAnim.ContentA;
		if (contentA < 0.01f)
		{
			return;
		}
		float num = Mathf.Max(0.5f, scale);
		float num2 = 7f * num;
		float num3 = 11f * num;
		float num4 = 9f * num;
		float num5 = 3f * num;
		Rect rect2 = new Rect(rect.x, rect.y, rect.width, rect.height);
		Separator(rect2, p, num);
		Color color = GUI.color;
		if (contentA < 0.999f)
		{
			GUI.color = new Color(1f, 1f, 1f, contentA);
		}
		bool flag = IsSelectedPawn(p);
		bool flag2 = IsAway(p);
		bool flag3 = Mouse.IsOver(rect2);
		Rect rect3 = rect2;
		if (Prefs.DevMode || RailNow > 0f)
		{
			float num6 = lastSlab.y + StripH + RailNow + 2f;
			if (rect3.y < num6)
			{
				rect3.height -= num6 - rect3.y;
				rect3.y = num6;
			}
		}
		if (rect3.height > 2f)
		{
			CardChrome.Rounded(rect3, Palette.WithAlpha(flag ? SegSel : (flag3 ? SegHover : SegFill), s.EffectiveCardOpacity), 12f);
			if (s.frameMode > 0 && !flag)
			{
				PawnUiCache.Snap snap = PawnUiCache.Get(p);
				if (snap.hasFrame)
				{
					UIKit.Flat.Frame(rect3, snap.frameHue, s.frameStrength, snap.frameGlyph);
				}
			}
		}
		float num7 = rect2.y + 4f;
		int count = 0;
		int worstSev;
		bool flag4 = s.showConditionMarkers && RcbAlertBand.TryGetConditions(p, out count, out worstSev);
		if (flag4)
		{
			Color alert = Palette.Flat.Alert;
			int num8 = Mathf.Max(0, s.conditionPulseAt);
			bool flag5 = num8 > 0 && count >= num8;
			float pulsePhase = RcbAlertBand.PulsePhase;
			Color c = alert;
			if (flag5)
			{
				c.a *= Mathf.Lerp(0.42f, 1f, 0.5f + 0.5f * Mathf.Cos(pulsePhase * Mathf.PI * 2f));
			}
			float num9 = Mathf.Max(3f, 4f * num);
			RcbSpatial.Pill(new Rect(rect2.x + 7f, rect2.yMax - num9 - 3f, rect2.width - 14f, num9), c);
			if (count > 1)
			{
				string text = SmallStrings.Int(count);
				float num10 = Mathf.Max(15f, RcbText.CalcSize(text).x + 8f);
				float height = 15f;
				if (RcbCohorts.IsPinned(p))
				{
					num7 += Mathf.Max(9f, 11f * num) + 2f;
				}
				Rect r = new Rect(rect2.xMax - num10 - 5f, num7, num10, height);
				if (flag5)
				{
					float num11 = Mathf.Lerp(0f, 9f, pulsePhase);
					Color c2 = alert;
					c2.a *= (1f - pulsePhase) * 0.55f;
					RcbSpatial.Pill(new Rect(r.x - num11 * 0.5f, r.y - num11 * 0.5f, r.width + num11, r.height + num11), c2);
				}
				RcbSpatial.Pill(r, alert);
				RcbPalette.MicroLabel(r, text, Palette.Flat.AlertInk, TextAnchor.MiddleCenter, shadow: false);
			}
		}
		if (RcbCohorts.IsPinned(p))
		{
			float num12 = Mathf.Max(9f, 11f * num);
			Rect position = new Rect(rect2.xMax - num12 - 4f, rect2.y + 4f, num12, num12);
			Texture2D pin = RCBTextures.Pin;
			if (pin != null)
			{
				Color color2 = GUI.color;
				GUI.color = Palette.WithAlpha(PinInk, color2.a);
				GUI.DrawTexture(position, pin);
				GUI.color = color2;
			}
			else
			{
				RcbSpatial.Dot(new Rect(rect2.xMax - 10f, rect2.y + 5f, 5f, 5f), PinInk);
			}
		}
		Color color3 = GUI.color;
		if (flag2)
		{
			GUI.color = new Color(1f, 1f, 1f, 0.55f * contentA);
		}
		bool flag6 = s.cardLayout == 1;
		float num13 = 46f * num;
		float height2 = 56f * num;
		float num14 = ChipHeightFor(p, s, num);
		float num15 = PortColFor(num, num14);
		float num16 = MarksReserve(num);
		float num17 = rect2.height - num16;
		float x;
		float y;
		if (flag6)
		{
			x = rect2.x + (rect2.width - num13) * 0.5f;
			y = rect2.y + num2;
		}
		else
		{
			x = rect2.x + num3;
			y = rect2.y + (num17 - num15) * 0.5f;
		}
		cardPawns.Add(p);
		cardRects.Add(rect2);
		Rect rect4 = new Rect(x, y, num13, height2);
		try
		{
			DrawPortraitBox(rect4, p, s);
		}
		catch
		{
		}
		if (flag3 && s.portraitHoverInfo)
		{
			HoverPopout.Record(p, rect2);
		}
		string line = null;
		float alpha = 0f;
		if (s.showSpeechBubbles && contentA > 0.9f)
		{
			if (s.speechPlacement == 1)
			{
				if (!SpeechBubbles.TryGetLine(p, out line, out alpha) || alpha <= 0.02f)
				{
					line = null;
				}
			}
			else
			{
				SpeechBubbles.DrawBar(rect4, p, s, scale);
			}
		}
		if (num14 > 0f)
		{
			RcbWeaponChips.Draw(rect4.center.x, rect4.yMax + 2f * num + PillLane(num) + 4f * num, p, s, num, num13);
		}
		float num18 = 15f * num;
		PawnStatusUtil.Status badge = default(PawnStatusUtil.Status);
		if (s.showStatusIndicators && PawnStatusUtil.TryGetBadge(p, out badge) && badge.icon != null)
		{
			BadgeLayer.Icon(new Rect(rect4.x + 2f, rect4.y + 2f, num18, num18), badge.icon, badge.color);
		}
		if (s.showSquadBadges && DefensivePositionsCompat.Active)
		{
			int num19 = DefensivePositionsCompat.SquadNumber(p);
			if (num19 > 0)
			{
				BadgeLayer.NumBox(new Rect(rect4.xMax - num18 - 2f, rect4.y + 2f, num18, num18), Palette.Flat.Faction, SmallStrings.Int(num19));
			}
		}
		if (num16 > 0f)
		{
			float num20 = 15f * num;
			Rect r2 = new Rect(rect2.x + num3, rect2.yMax - num2 - num20, rect2.width - num3 * 2f, num20);
			if (r2.width >= 8f)
			{
				BadgeLayer.Marks(p, r2);
			}
		}
		bool conditionShown = flag4 || (s.statePillMode == 1 && RcbAlertBand.Visible);
		if (s.statePillMode != 2 && StatePill(p, flag2, conditionShown, out var label, out var bg, out var fg, out var isAlert))
		{
			float height3 = PillLane(num);
			Rect rect5 = new Rect(rect4.x, rect4.yMax + 2f * num, rect4.width, height3);
			if (RcbGuard.Finite(rect5) && rect5.width > 8f)
			{
				RcbSpatial.Chip(rect5, bg);
				RcbPalette.MicroLabel(rect5, RcbFont.Fit(label, rect5.width - 4f, bold: false, GameFont.Small), fg, TextAnchor.MiddleCenter, shadow: false);
				if (isAlert && flag3)
				{
					string text2 = RcbAlertBand.ConditionTip(p);
					if (text2 != null)
					{
						TooltipHandler.TipRegion(rect5, text2);
					}
				}
			}
		}
		bool flag7 = false;
		Texture2D colour = null;
		Rect uv = default(Rect);
		ThingWithComps thingWithComps = null;
		if (s.draftedWeaponCard && p.Drafted)
		{
			try
			{
				thingWithComps = p.equipment?.Primary;
			}
			catch
			{
				thingWithComps = null;
			}
			if (thingWithComps != null && thingWithComps.def != null && thingWithComps.def.IsWeapon && Patch_DrawColonist_Overlay.TryWeaponArt(thingWithComps.def, out colour, out uv) && colour != null)
			{
				flag7 = true;
			}
		}
		float num21 = (flag7 ? ColumnHeight(num) : PawnColumnHeight(p, s, num));
		float num22;
		float num23;
		TextAnchor anchor;
		float num24;
		if (flag6)
		{
			num22 = rect2.x + num3;
			num23 = rect2.width - num3 * 2f;
			num24 = rect2.y + num2 + PortColumnHeight(num) + 5f * num;
			anchor = TextAnchor.MiddleCenter;
		}
		else
		{
			num22 = rect4.xMax + num4;
			num23 = rect2.xMax - num3 - num22;
			num24 = rect2.y + Mathf.Max(num2, (num17 - num21) * 0.5f);
			anchor = TextAnchor.MiddleLeft;
		}
		if (num23 < 12f)
		{
			GUI.color = color3;
			GUI.color = color;
			return;
		}
		float num25 = 1f * num;
		Color color4 = (flag ? InkInv : RcbPalette.Stat);
		Color color5 = (flag ? InkInvDim : Dim2);
		bool flag8 = !IsNone(s.barTopStat) || !IsNone(s.barBottomStat);
		if (line != null && !flag7)
		{
			float num26 = ColumnHeight(num);
			float num27 = num26 - (flag8 ? (5f * num + 3f * num) : 0f) - 2f * num;
			if (num27 > 14f * num)
			{
				Rect rect6 = new Rect(num22, num24, num23, num27);
				Color col = Palette.Flat.StateInk;
				if (flag)
				{
					col = InkInv;
				}
				col.a *= alpha * GUI.color.a;
				RcbFont.Wrapped(rect6, SpeechBubbles.FitCard(p, line, num23, num27), bold: false, GameFont.Small, col, flag6 ? TextAnchor.UpperCenter : TextAnchor.UpperLeft);
				if (flag3)
				{
					TooltipHandler.TipRegion(rect6, line);
				}
			}
			num24 += num27 + 2f * num;
			if (flag8)
			{
				float height4 = 5f * num;
				bool flag9 = !IsNone(s.barTopStat);
				bool flag10 = !IsNone(s.barBottomStat);
				int num28 = (flag9 ? 1 : 0) + (flag10 ? 1 : 0);
				float num29 = (num23 - (float)(num28 - 1) * 4f * num) / (float)num28;
				float num30 = num22;
				if (flag9)
				{
					Rail(s.barTopStat, p, s, dmgFlash, new Rect(num30, num24, num29, height4));
					num30 += num29 + 4f * num;
				}
				if (flag10)
				{
					Rail(s.barBottomStat, p, s, dmgFlash, new Rect(num30, num24, num29, height4));
				}
			}
			GUI.color = color3;
			GUI.color = color;
			return;
		}
		if (s.showName)
		{
			RcbFont.Label(new Rect(num22, num24, num23, 19f * num), RcbFont.Fit(GetNameOf(p), num23, bold: true, GameFont.Small), bold: true, GameFont.Small, flag2 ? color5 : color4, anchor);
			num24 += 19f * num + num25;
		}
		bool flag11 = flag8;
		if (flag7)
		{
			float num31 = num21 - (s.showName ? (19f * num + num25) : 0f) - (flag11 ? (5f * num + 3f * num) : 0f) - 2f * num;
			if (num31 > 6f && num23 > 6f && uv.width > 0f && uv.height > 0f)
			{
				float num32 = uv.width * (float)colour.width / (uv.height * (float)colour.height);
				if (!(num32 > 0.01f))
				{
					num32 = 1f;
				}
				float num33 = num23;
				float num34 = num33 / num32;
				if (num34 > num31)
				{
					num34 = num31;
					num33 = num34 * num32;
				}
				float x2 = (flag6 ? (num22 + (num23 - num33) * 0.5f) : num22);
				Rect rect7 = new Rect(x2, num24 + (num31 - num34) * 0.5f, num33, num34);
				GUI.DrawTextureWithTexCoords(rect7, colour, uv);
				if (flag3)
				{
					TooltipHandler.TipRegion(rect7, thingWithComps.LabelCap);
				}
			}
			num24 += num31;
		}
		int num35 = ((!flag7) ? IdentityRows(p, s) : 0);
		for (int i = 0; i < num35; i++)
		{
			RcbPalette.MicroLabel(new Rect(num22, num24, num23, 14f * num), RcbFont.Fit(rowText[i], num23, bold: false, GameFont.Small), RowColor(rowKind[i], p, s, flag, flag2), anchor, shadow: false);
			num24 += 14f * num + num25;
		}
		if (s.showJobLabel && !flag7)
		{
			if (!s.jobLabelHoverOnly || flag3)
			{
				Color col2;
				string s2 = Headline(p, s, out col2);
				if (flag2)
				{
					col2 = Dim2;
				}
				if (flag)
				{
					if (col2 == RcbPalette.Stat)
					{
						col2 = color4;
					}
					else if (col2 == Dim2)
					{
						col2 = color5;
					}
				}
				RcbPalette.MicroLabel(new Rect(num22, num24, num23, 14f * num), RcbFont.Fit(s2, num23, bold: false, GameFont.Small), col2, anchor, shadow: false);
			}
			num24 += 14f * num + num25;
		}
		num24 += 2f * num;
		bool flag12 = !IsNone(s.barTopStat);
		bool flag13 = !IsNone(s.barBottomStat);
		if (flag11)
		{
			float height5 = 5f * num;
			int num36 = (flag12 ? 1 : 0) + (flag13 ? 1 : 0);
			float num37 = (num23 - (float)(num36 - 1) * 4f * num) / (float)num36;
			float num38 = num22;
			if (flag12)
			{
				Rail(s.barTopStat, p, s, dmgFlash, new Rect(num38, num24, num37, height5));
				num38 += num37 + 4f * num;
			}
			if (flag13)
			{
				Rail(s.barBottomStat, p, s, dmgFlash, new Rect(num38, num24, num37, height5));
			}
		}
		GUI.color = color3;
		GUI.color = color;
	}

	private static void Rail(string key, Pawn p, RadiusColonistBarSettings s, float dmgFlash, Rect r)
	{
		if (!Patch_DrawColonist_Overlay.TryResolveStat(p, key, out var frac, out var _, out var isHealth))
		{
			return;
		}
		if (isHealth && s.healthMeterStyle == 1)
		{
			RcbEkg.Draw(r, frac, p);
			return;
		}
		Color color = Palette.Flat.Skill;
		if (s.pulseOnDamage && dmgFlash > 0f && isHealth)
		{
			color = Color.Lerp(color, RcbPalette.DamageFlash, Mathf.Sqrt(Mathf.Clamp01(dmgFlash)) * 0.7f);
		}
		RcbSpatial.Meter(r, frac, color, RcbPalette.PanelBG);
	}

	private static void Line(bool show, string text, Color col, int font, int style, float x, float w, ref float y, float bottom, float rowGap)
	{
		if (show && !string.IsNullOrEmpty(text))
		{
			float num = RCBLabel.LineH(font) + 1f;
			if (!(y + num > bottom))
			{
				RCBLabel.DrawLine(new Rect(x, y, w, num), text, col, font, style, TextAnchor.MiddleLeft);
				y += num + rowGap;
			}
		}
	}

	private static void Meter(string key, Pawn p, RadiusColonistBarSettings s, float dmgFlash, float x, float w, float h, ref float y, float bottom, float rowGap)
	{
		if (!(y + h > bottom) && Patch_DrawColonist_Overlay.TryResolveStat(p, key, out var frac, out var _, out var isHealth))
		{
			Color color = Palette.Flat.Skill;
			if (s.pulseOnDamage && dmgFlash > 0f && isHealth)
			{
				color = Color.Lerp(color, RcbPalette.DamageFlash, Mathf.Sqrt(Mathf.Clamp01(dmgFlash)) * 0.7f);
			}
			RcbSpatial.Meter(new Rect(x, y, w, h), frac, color, RcbPalette.PanelBG);
			y += h + rowGap;
		}
	}

	private static bool StatePill(Pawn p, bool away, bool conditionShown, out string label, out Color bg, out Color fg, out bool isAlert)
	{
		label = null;
		bg = default(Color);
		fg = Color.white;
		isAlert = false;
		if (away)
		{
			label = "RCB_State_Away".Translate();
			bg = Palette.Flat.SegOff;
			fg = Palette.Flat.SegOffInk;
			return true;
		}
		RcbState rcbState = CardPalette.State(p);
		if (rcbState == RcbState.Bad && !conditionShown)
		{
			label = "RCB_State_Critical".Translate();
			bg = Palette.Flat.Alert;
			fg = Palette.Flat.AlertInk;
			isAlert = true;
			return true;
		}
		if (p.Drafted)
		{
			label = "RCB_State_Drafted".Translate();
			bg = Palette.Flat.Faction;
			fg = Palette.Flat.FactionInk;
			return true;
		}
		if (rcbState == RcbState.Warn && !conditionShown)
		{
			label = "RCB_State_Warning".Translate();
			bg = Palette.Flat.Alert;
			fg = Palette.Flat.AlertInk;
			isAlert = true;
			return true;
		}
		return false;
	}

	private static bool IsSelectedPawn(Pawn p)
	{
		return BarSelection.IsSelected(p);
	}

	private static void DrawPortraitBox(Rect box, Pawn p, RadiusColonistBarSettings s)
	{
		Texture texture = LiveViewManager.TryLive(p);
		if (texture != null)
		{
			GUI.DrawTexture(box, texture);
			return;
		}
		Pawn partner = PawnUiCache.Get(p).partner;
		if (partner != null)
		{
			float num = Mathf.Max(1f, 2f * (box.height / 56f));
			float num2 = (box.width - num) * 0.5f;
			Rect r = new Rect(box.x, box.y, num2, box.height);
			Rect r2 = new Rect(box.xMax - num2, box.y, num2, box.height);
			DrawOnePortrait(r, p);
			DrawOnePortrait(r2, partner);
			Color color = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, 0.1f * color.a);
			Widgets.DrawBoxSolid(new Rect(box.x + num2, box.y + box.height * 0.12f, num, box.height * 0.76f), Color.white);
			GUI.color = color;
		}
		else
		{
			DrawOnePortrait(box, p);
		}
	}

	private static void DrawOnePortrait(Rect r, Pawn p)
	{
		if (p != null && !(r.width < 2f) && !(r.height < 2f))
		{
			Texture portraitTexture = LiveViewManager.GetPortraitTexture(p, new Vector2(r.width * LiveViewManager.PortraitK, r.height * LiveViewManager.PortraitK));
			if (portraitTexture != null)
			{
				GUI.DrawTexture(r, portraitTexture);
			}
		}
	}

	private static bool IsAway(Pawn p)
	{
		try
		{
			return p.Map != Find.CurrentMap || !p.Spawned;
		}
		catch
		{
			return false;
		}
	}

	private static string GetNameOf(Pawn p)
	{
		return PawnUiCache.Get(p).name;
	}
}
