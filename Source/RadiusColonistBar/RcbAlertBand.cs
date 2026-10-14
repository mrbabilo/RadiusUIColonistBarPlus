using System;
using System.Collections.Generic;
using System.Text;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
internal static class RcbAlertBand
{
	internal struct Alert
	{
		public Pawn pawn;

		public RcbAlertKind kind;

		public int sev;
	}

	public const float H = 38f;

	public const float StripH = 4f;

	private const float ContentMinH = 22f;

	private const float PadX = 14f;

	private const float Gap = 10f;

	private const float SwapSecs = 0.25f;

	private static readonly RadiusIcon IcCrit = IconSet.Alert.Crit;

	private static readonly RadiusIcon IcWarn = IconSet.Alert.Warn;

	private static readonly RadiusIcon IcCold = IconSet.Alert.Cold;

	private static readonly RadiusIcon IcInfo = IconSet.Alert.Info;

	private static readonly RadiusIcon IcInspired = IconSet.Status.Inspired;

	private static readonly List<Alert> alerts = new List<Alert>(8);

	private static int collectFrame = -1;

	private static int nextCollect = int.MinValue;

	private static int idx;

	private static float elapsed;

	private static float swapT = 1f;

	private static float lastTime = -1f;

	private static bool hovered;

	private static Rect lastBand;

	private static Rect lastStepChip;

	private static Rect lastInspChip;

	private static bool visible;

	private static string curMsg;

	private static string curMeta;

	private static string prevMsg;

	private static string prevMeta;

	private static RadiusIcon curIcon;

	private static RadiusIcon prevIcon;

	private static bool curState;

	private static bool prevState;

	private static int inspIdx = -1;

	private static string inspLabel;

	private static readonly List<Alert> scratch = new List<Alert>(8);

	private static readonly Comparison<Alert> byRank = delegate(Alert a, Alert b)
	{
		int num = a.sev.CompareTo(b.sev);
		return (num != 0) ? num : a.pawn.thingIDNumber.CompareTo(b.pawn.thingIDNumber);
	};

	private static readonly List<Pawn> roster = new List<Pawn>(32);

	private static bool rosterValid;

	private static readonly Dictionary<int, int> condCount = new Dictionary<int, int>();

	private static readonly Dictionary<int, int> condWorst = new Dictionary<int, int>();

	private static readonly Dictionary<int, int> condMask = new Dictionary<int, int>();

	private static readonly Dictionary<int, string> condTip = new Dictionary<int, string>();

	private static readonly Dictionary<int, int> condTipMask = new Dictionary<int, int>();

	private static readonly StringBuilder tipSb = new StringBuilder(96);

	private const int CondBleed = 1;

	private const int CondBreak = 2;

	private const int CondDowned = 4;

	private const int CondBreakRisk = 8;

	private const int CondStarving = 16;

	private const int CondHypo = 32;

	private const int CondHeat = 64;

	private const int CondTend = 128;

	private static readonly string[] CondKeys = new string[8] { "RCB_Cond_BleedingOut", "RCB_Cond_Break", "RCB_Cond_Downed", "RCB_Cond_BreakRisk", "RCB_Cond_Starving", "RCB_Cond_Hypothermia", "RCB_Cond_Heatstroke", "RCB_Cond_NeedsTend" };

	public const float PulseSecs = 1.5f;

	private static int counterCur = -1;

	private static int counterN = -1;

	private static string counterStr;

	private static int version;

	private static string cachedTip;

	private static int cachedTipVersion = -1;

	public static bool Visible => visible;

	public static bool Active
	{
		get
		{
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			return settings != null && settings.masterEnabled && settings.alertBand && settings.bandStandsIn;
		}
	}

	public static float PulsePhase => Time.realtimeSinceStartup % 1.5f / 1.5f;

	public static bool Covers(int kind)
	{
		if (!Active)
		{
			return false;
		}
		if (kind <= 0 || kind > 9)
		{
			return false;
		}
		if (kind == 9)
		{
			return RadiusColonistBarMod.Settings?.alertInspired ?? false;
		}
		return true;
	}

	public static float Height()
	{
		if (!visible)
		{
			return 0f;
		}
		return Mathf.Round(Mathf.Lerp(4f, 38f, Mathf.Clamp01(RcbIslandAnim.ContentA)));
	}

	public static void AdvanceOnce()
	{
		if (collectFrame == Time.frameCount)
		{
			return;
		}
		collectFrame = Time.frameCount;
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.alertBand)
		{
			alerts.Clear();
			visible = false;
			lastBand = default(Rect);
			return;
		}
		if (Time.frameCount >= nextCollect)
		{
			nextCollect = Time.frameCount + 15;
			Collect();
		}
		visible = alerts.Count > 0;
		if (!visible)
		{
			lastBand = default(Rect);
			elapsed = 0f;
			swapT = 1f;
			return;
		}
		if (RcbIslandAnim.ContentA < 0.01f)
		{
			if (idx != 0)
			{
				idx = 0;
				BuildStrings();
				swapT = 1f;
			}
			elapsed = 0f;
			return;
		}
		float num = (float)Mathf.Max(0, settings.alertCycleMs) / 1000f;
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (lastTime < 0f)
		{
			lastTime = realtimeSinceStartup;
		}
		float num2 = Mathf.Clamp(realtimeSinceStartup - lastTime, 0f, 0.25f);
		lastTime = realtimeSinceStartup;
		if (swapT < 1f)
		{
			swapT = Mathf.Min(1f, swapT + num2 / 0.25f);
		}
		if (!hovered && alerts.Count >= 2 && !(num <= 0.001f))
		{
			elapsed += num2;
			if (elapsed >= num)
			{
				elapsed = 0f;
				Step(1);
			}
		}
	}

	private static void Step(int dir)
	{
		if (alerts.Count != 0)
		{
			prevMsg = curMsg;
			prevMeta = curMeta;
			prevIcon = curIcon;
			prevState = curState;
			idx = ((idx + dir) % alerts.Count + alerts.Count) % alerts.Count;
			BuildStrings();
			swapT = 0f;
			version++;
		}
	}

	public static void HandleInput()
	{
		if (!visible || !RcbGuard.Finite(lastBand) || lastBand.height < 2f)
		{
			hovered = false;
		}
		else
		{
			if (!(hovered = Mouse.IsOver(lastBand)) || lastBand.height < 22f)
			{
				return;
			}
			Event current = Event.current;
			if (RcbInput.LeftPress(current))
			{
				int num = -1;
				if (RcbGuard.Finite(lastStepChip) && lastStepChip.width > 1f && lastStepChip.Contains(current.mousePosition))
				{
					num = Mathf.Clamp(idx, 0, alerts.Count - 1);
				}
				else if (RcbGuard.Finite(lastInspChip) && lastInspChip.width > 1f && lastInspChip.Contains(current.mousePosition))
				{
					num = inspIdx;
				}
				if (num >= 0 && num < alerts.Count)
				{
					Pawn pawn = alerts[num].pawn;
					if (pawn != null)
					{
						try
						{
							CameraJumper.TryJumpAndSelect(pawn);
						}
						catch
						{
						}
						try
						{
							RcbBarWindow.EnsureVisible(pawn);
						}
						catch
						{
						}
					}
				}
				current.Use();
			}
			else if (current.type == EventType.ScrollWheel && alerts.Count > 1)
			{
				Step((current.delta.y > 0f) ? 1 : (-1));
				elapsed = 0f;
				current.Use();
			}
		}
	}

	public static void SetRoster(List<ColonistBar.Entry> entries)
	{
		roster.Clear();
		if (entries != null)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				Pawn pawn = entries[i].pawn;
				if (pawn != null)
				{
					roster.Add(pawn);
				}
			}
		}
		rosterValid = true;
	}

	private static void Collect()
	{
		scratch.Clear();
		if (!rosterValid || roster.Count == 0)
		{
			List<ColonistBar.Entry> list = Find.ColonistBar?.Entries;
			roster.Clear();
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].pawn != null)
					{
						roster.Add(list[i].pawn);
					}
				}
			}
		}
		condCount.Clear();
		condWorst.Clear();
		condMask.Clear();
		for (int j = 0; j < roster.Count; j++)
		{
			Pawn pawn = roster[j];
			if (pawn != null && !pawn.Dead)
			{
				int worst;
				int mask;
				int num = CountConditions(pawn, out worst, out mask);
				if (num > 0)
				{
					condCount[pawn.thingIDNumber] = num;
					condWorst[pawn.thingIDNumber] = worst;
					condMask[pawn.thingIDNumber] = mask;
				}
				RcbAlertKind rcbAlertKind = Worst(pawn);
				if (rcbAlertKind != RcbAlertKind.None)
				{
					scratch.Add(new Alert
					{
						pawn = pawn,
						kind = rcbAlertKind,
						sev = Sev(rcbAlertKind)
					});
				}
			}
		}
		if (scratch.Count > 1)
		{
			scratch.Sort(byRank);
		}
		Pawn pawn2 = ((idx >= 0 && idx < alerts.Count) ? alerts[idx].pawn : null);
		RcbAlertKind rcbAlertKind2 = ((idx >= 0 && idx < alerts.Count) ? alerts[idx].kind : RcbAlertKind.None);
		alerts.Clear();
		for (int k = 0; k < scratch.Count; k++)
		{
			alerts.Add(scratch[k]);
		}
		int num2 = -1;
		if (pawn2 != null)
		{
			for (int l = 0; l < alerts.Count; l++)
			{
				if (alerts[l].pawn == pawn2 && alerts[l].kind == rcbAlertKind2)
				{
					num2 = l;
					break;
				}
			}
		}
		if (num2 >= 0)
		{
			idx = num2;
		}
		else
		{
			idx = 0;
			elapsed = 0f;
			swapT = 1f;
		}
		if (alerts.Count > 0)
		{
			BuildStrings();
		}
		else
		{
			curMsg = (curMeta = null);
		}
		version++;
	}

	public static void PruneTips(HashSet<int> liveIds)
	{
		RcbUtil.PruneById(condTip, liveIds);
		RcbUtil.PruneById(condTipMask, liveIds);
	}

	public static string ConditionTip(Pawn p)
	{
		if (p == null)
		{
			return null;
		}
		int thingIDNumber = p.thingIDNumber;
		if (!condMask.TryGetValue(thingIDNumber, out var value) || value == 0)
		{
			return null;
		}
		if (condTipMask.TryGetValue(thingIDNumber, out var value2) && value2 == value && condTip.TryGetValue(thingIDNumber, out var value3))
		{
			return value3;
		}
		tipSb.Length = 0;
		for (int i = 0; i < CondKeys.Length; i++)
		{
			if ((value & (1 << i)) != 0)
			{
				if (tipSb.Length > 0)
				{
					tipSb.Append('\n');
				}
				tipSb.Append(CondKeys[i].Translate().ToString());
			}
		}
		string text = tipSb.ToString();
		condTip[thingIDNumber] = text;
		condTipMask[thingIDNumber] = value;
		return text;
	}

	public static bool TryGetConditions(Pawn p, out int count, out int worstSev)
	{
		count = 0;
		worstSev = -1;
		if (p == null)
		{
			return false;
		}
		int thingIDNumber = p.thingIDNumber;
		if (!condCount.TryGetValue(thingIDNumber, out count) || count <= 0)
		{
			return false;
		}
		condWorst.TryGetValue(thingIDNumber, out worstSev);
		return worstSev >= 0;
	}

	private static int CountConditions(Pawn p, out int worst, out int mask)
	{
		worst = -1;
		mask = 0;
		int num = 0;
		try
		{
			PawnUiCache.Snap snap = PawnUiCache.Get(p);
			if (p.health != null && p.health.hediffSet.BleedRateTotal > 0.01f && HealthUtility.TicksUntilDeathDueToBloodLoss(p) < 60000)
			{
				num++;
				worst = 0;
				mask |= 1;
			}
			if (p.InMentalState)
			{
				num++;
				if (worst < 0)
				{
					worst = 0;
				}
				mask |= 2;
			}
			if (p.Downed && !InBed(p) && !PawnStatusUtil.DownedIsIncidental(p))
			{
				num++;
				if (worst < 0)
				{
					worst = 0;
				}
				mask |= 4;
			}
			Need_Mood need_Mood = p.needs?.mood;
			if (need_Mood != null && snap.thrMajor > 0f && need_Mood.CurLevelPercentage < snap.thrMajor)
			{
				num++;
				if (worst < 0 || worst > 1)
				{
					worst = 1;
				}
				mask |= 8;
			}
			Need_Food need_Food = p.needs?.food;
			if (need_Food != null && need_Food.CurCategory == HungerCategory.Starving)
			{
				num++;
				if (worst < 0 || worst > 1)
				{
					worst = 1;
				}
				mask |= 16;
			}
			if (snap.hypoSev > 0.25f)
			{
				num++;
				if (worst < 0 || worst > 2)
				{
					worst = 2;
				}
				mask |= 32;
			}
			if (snap.heatSev > 0.25f)
			{
				num++;
				if (worst < 0 || worst > 2)
				{
					worst = 2;
				}
				mask |= 64;
			}
			if (snap.needsTend)
			{
				num++;
				if (worst < 0 || worst > 3)
				{
					worst = 3;
				}
				mask |= 128;
			}
		}
		catch
		{
		}
		return num;
	}

	private static RcbAlertKind Worst(Pawn p)
	{
		try
		{
			PawnUiCache.Snap snap = PawnUiCache.Get(p);
			if (p.health != null && p.health.hediffSet.BleedRateTotal > 0.01f && HealthUtility.TicksUntilDeathDueToBloodLoss(p) < 60000)
			{
				return RcbAlertKind.BleedingOut;
			}
			if (p.InMentalState)
			{
				return RcbAlertKind.Break;
			}
			if (p.Downed && !InBed(p) && !PawnStatusUtil.DownedIsIncidental(p))
			{
				return RcbAlertKind.Downed;
			}
			Need_Mood need_Mood = p.needs?.mood;
			if (need_Mood != null && snap.thrMajor > 0f && need_Mood.CurLevelPercentage < snap.thrMajor)
			{
				return RcbAlertKind.BreakRisk;
			}
			Need_Food need_Food = p.needs?.food;
			if (need_Food != null && need_Food.CurCategory == HungerCategory.Starving)
			{
				return RcbAlertKind.Starving;
			}
			if (snap.hypoSev > 0.25f)
			{
				return RcbAlertKind.Hypothermia;
			}
			if (snap.heatSev > 0.25f)
			{
				return RcbAlertKind.Heatstroke;
			}
			if (snap.needsTend)
			{
				return RcbAlertKind.NeedsTend;
			}
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			if (settings != null && settings.alertInspired)
			{
				InspirationHandler inspirationHandler = p.mindState?.inspirationHandler;
				if (inspirationHandler != null && inspirationHandler.Inspired)
				{
					return RcbAlertKind.Inspired;
				}
			}
		}
		catch
		{
		}
		return RcbAlertKind.None;
	}

	private static bool InBed(Pawn p)
	{
		try
		{
			return p.CurrentBed() != null;
		}
		catch
		{
			return false;
		}
	}

	private static int Sev(RcbAlertKind k)
	{
		switch (k)
		{
		case RcbAlertKind.BleedingOut:
		case RcbAlertKind.Downed:
		case RcbAlertKind.Break:
			return 0;
		case RcbAlertKind.BreakRisk:
		case RcbAlertKind.Starving:
			return 1;
		case RcbAlertKind.Hypothermia:
		case RcbAlertKind.Heatstroke:
			return 2;
		case RcbAlertKind.Inspired:
			return 4;
		default:
			return 3;
		}
	}

	public static Color SeverityColor(int sev)
	{
		return (sev >= 4) ? Palette.Flat.State : Palette.Flat.Alert;
	}

	private static RadiusIcon IconOf(int sev)
	{
		return sev switch
		{
			3 => IcInfo, 
			2 => IcCold, 
			1 => IcWarn, 
			0 => IcCrit, 
			_ => IcInspired, 
		};
	}

	private static string KeyOf(RcbAlertKind k)
	{
		return k switch
		{
			RcbAlertKind.BleedingOut => "RCB_Alert_BleedingOut", 
			RcbAlertKind.Downed => "RCB_Alert_Downed", 
			RcbAlertKind.Break => "RCB_Alert_Break", 
			RcbAlertKind.BreakRisk => "RCB_Alert_BreakRisk", 
			RcbAlertKind.Starving => "RCB_Alert_Starving", 
			RcbAlertKind.Hypothermia => "RCB_Alert_Hypothermia", 
			RcbAlertKind.Heatstroke => "RCB_Alert_Heatstroke", 
			RcbAlertKind.Inspired => "RCB_Alert_Inspired", 
			_ => "RCB_Alert_NeedsTend", 
		};
	}

	private static void BuildStrings()
	{
		if (idx < 0 || idx >= alerts.Count)
		{
			curMsg = (curMeta = null);
			return;
		}
		Alert alert = alerts[idx];
		Pawn pawn = alert.pawn;
		curIcon = IconOf(alert.sev);
		curState = alert.kind == RcbAlertKind.Inspired;
		inspIdx = -1;
		inspLabel = null;
		if (!curState)
		{
			for (int i = 0; i < alerts.Count; i++)
			{
				if (alerts[i].kind != RcbAlertKind.Inspired || alerts[i].pawn == null)
				{
					continue;
				}
				inspIdx = i;
				try
				{
					string text = PawnUiCache.Get(alerts[i].pawn).name;
					if (string.IsNullOrEmpty(text))
					{
						text = alerts[i].pawn.LabelShortCap;
					}
					TaggedString? taggedString = alerts[i].pawn.mindState?.inspirationHandler?.CurState?.def?.LabelCap;
					string text2 = (taggedString.HasValue ? ((string)taggedString.GetValueOrDefault()) : null);
					inspLabel = (string.IsNullOrEmpty(text2) ? "RCB_Alert_Inspired".Translate(text.Named("PAWN")).ToString() : "RCB_Alert_InspiredNamed".Translate(text.Named("PAWN"), text2.Named("WHAT")).ToString());
				}
				catch
				{
					inspLabel = null;
				}
				break;
			}
		}
		string text3 = PawnUiCache.Get(pawn).name;
		if (string.IsNullOrEmpty(text3))
		{
			try
			{
				text3 = pawn.LabelShortCap;
			}
			catch
			{
				text3 = "?";
			}
		}
		try
		{
			if (alert.kind == RcbAlertKind.Inspired)
			{
				string text4 = null;
				try
				{
					TaggedString? taggedString = pawn.mindState?.inspirationHandler?.CurState?.def?.LabelCap;
					text4 = (taggedString.HasValue ? ((string)taggedString.GetValueOrDefault()) : null);
				}
				catch
				{
				}
				curMsg = (string.IsNullOrEmpty(text4) ? "RCB_Alert_Inspired".Translate(text3.Named("PAWN")).ToString() : "RCB_Alert_InspiredNamed".Translate(text3.Named("PAWN"), text4.Named("WHAT")).ToString());
			}
			else
			{
				curMsg = KeyOf(alert.kind).Translate(text3.Named("PAWN")).ToString();
			}
		}
		catch
		{
			curMsg = text3;
		}
		curMeta = Meta(pawn, alert.kind);
	}

	private static string Meta(Pawn p, RcbAlertKind k)
	{
		try
		{
			switch (k)
			{
			case RcbAlertKind.BleedingOut:
			{
				int numTicks = HealthUtility.TicksUntilDeathDueToBloodLoss(p);
				return numTicks.ToStringTicksToPeriod(allowSeconds: true, shortForm: true);
			}
			case RcbAlertKind.BreakRisk:
				return (p.needs?.mood)?.CurLevelPercentage.ToStringPercent("F0");
			case RcbAlertKind.Hypothermia:
				return PawnUiCache.Get(p).hypoSev.ToStringPercent("F0");
			case RcbAlertKind.Heatstroke:
				return PawnUiCache.Get(p).heatSev.ToStringPercent("F0");
			case RcbAlertKind.Break:
				return (p.MentalStateDef != null) ? p.MentalStateDef.LabelCap.ToString() : null;
			case RcbAlertKind.NeedsTend:
				return "RCB_Alert_MetaNow".Translate().ToString();
			case RcbAlertKind.Inspired:
			{
				Inspiration inspiration = p.mindState?.inspirationHandler?.CurState;
				if (inspiration?.def == null)
				{
					return null;
				}
				int num = Mathf.RoundToInt((inspiration.def.baseDurationDays - inspiration.AgeDays) * 60000f);
				return (num > 0) ? num.ToStringTicksToPeriod(allowSeconds: true, shortForm: true) : null;
			}
			default:
				return null;
			}
		}
		catch
		{
			return null;
		}
	}

	public static void Draw(Rect band)
	{
		if (!visible || !RcbGuard.Finite(band) || band.height < 2f || band.width < 40f)
		{
			return;
		}
		lastBand = band;
		lastStepChip = default(Rect);
		lastInspChip = default(Rect);
		if (idx < 0 || idx >= alerts.Count)
		{
			return;
		}
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		float num = Mathf.Clamp01(RcbIslandAnim.ContentA);
		float a = settings?.EffectiveBandOpacity ?? 1f;
		bool flag = band.height < 22f;
		Color c = (flag ? Palette.Flat.Alert : RcbPalette.Ink);
		c.a = a;
		RcbSpatial.BottomBand(band, c);
		if (flag)
		{
			return;
		}
		Color color = GUI.color;
		GUI.color = new Color(1f, 1f, 1f, num);
		bool flag2 = swapT < 0.5f && !string.IsNullOrEmpty(prevMsg);
		float num2 = ((swapT >= 1f) ? 1f : Mathf.Abs(swapT * 2f - 1f));
		string text = (flag2 ? prevMsg : curMsg);
		string text2 = (flag2 ? prevMeta : curMeta);
		RadiusIcon icon = (flag2 ? prevIcon : curIcon);
		bool state = (flag2 ? prevState : curState);
		float y = band.center.y;
		float num3 = 22f;
		float num4 = band.x + 14f;
		float num5 = band.xMax - 14f;
		int count = alerts.Count;
		if (count > 1)
		{
			string text3 = CounterFor(idx + 1, count);
			float num6 = RadiusFont.WidthAt(text3, 13f);
			RadiusFont.LabelAt(new Rect(num5 - num6, y - num3 * 0.5f, num6, num3), text3, 13f, Palette.WithAlpha(Palette.Flat.InkLow, num), TextAnchor.MiddleRight);
			num5 -= num6 + 10f;
		}
		float num7 = num5 - num4;
		if (num7 > 40f && !string.IsNullOrEmpty(text))
		{
			string label = (string.IsNullOrEmpty(text2) ? text : (text + " · " + text2));
			float num8 = DrawChip(num4, y, num7, label, icon, state, num * num2, num);
			if (num8 > 1f)
			{
				lastStepChip = new Rect(num4, y - num3 * 0.5f, num8, num3);
			}
			num4 += num8 + 10f;
		}
		if (inspIdx >= 0 && !string.IsNullOrEmpty(inspLabel))
		{
			float num9 = ChipWidth(inspLabel, withIcon: true);
			if (num5 - num4 >= num9)
			{
				DrawChip(num4, y, num9, inspLabel, IcInspired, state: true, num, num);
				lastInspChip = new Rect(num4, y - num3 * 0.5f, num9, num3);
			}
		}
		GUI.color = color;
		if (Mouse.IsOver(band))
		{
			TooltipHandler.TipRegion(band, TipFor());
		}
	}

	private static string CounterFor(int cur, int n)
	{
		if (cur != counterCur || n != counterN || counterStr == null)
		{
			counterCur = cur;
			counterN = n;
			counterStr = "RCB_Band_Counter".Translate(cur.Named("CUR"), n.Named("N")).ToString();
		}
		return counterStr;
	}

	private static float ChipWidth(string label, bool withIcon)
	{
		float num = RadiusFont.WidthAt(label, 13f, bold: true) + 18f;
		if (withIcon)
		{
			num += 26f;
		}
		return num;
	}

	private static float DrawChip(float x, float cy, float maxW, string label, RadiusIcon icon, bool state, float textA, float plateA)
	{
		bool exists = icon.Exists;
		float num = (exists ? 26f : 0f);
		float num2 = maxW - 18f - num;
		if (num2 < 12f)
		{
			return 0f;
		}
		string text = RadiusFont.FitAt(label, num2, 13f, bold: true);
		float num3 = RadiusFont.WidthAt(text, 13f, bold: true);
		float num4 = num3 + 18f + num;
		float num5 = 22f;
		Rect r = new Rect(x, cy - num5 * 0.5f, num4, num5);
		Color c = (state ? Palette.Flat.State : Palette.Flat.Alert);
		Color c2 = (state ? Palette.Flat.StateInk : Palette.Flat.AlertInk);
		Sdf.RoundedRect(r, Palette.WithAlpha(c, plateA), 6f);
		float num6 = r.x + 9f;
		if (exists)
		{
			float num7 = 17f;
			icon.Draw(new Rect(num6, cy - num7 * 0.5f, num7, num7), new Color(1f, 1f, 1f, textA));
			num6 += num7 + 9f;
		}
		RadiusFont.LabelAt(new Rect(num6, r.y, num3, num5), text, 13f, Palette.WithAlpha(c2, textA), TextAnchor.MiddleLeft, bold: true);
		return num4;
	}

	private static string TipFor()
	{
		if (cachedTipVersion == version && cachedTip != null)
		{
			return cachedTip;
		}
		cachedTipVersion = version;
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < alerts.Count; i++)
		{
			Pawn pawn = alerts[i].pawn;
			if (pawn == null)
			{
				continue;
			}
			string text = PawnUiCache.Get(pawn).name;
			if (string.IsNullOrEmpty(text))
			{
				try
				{
					text = pawn.LabelShortCap;
				}
				catch
				{
					text = "?";
				}
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('\n');
			}
			if (i == idx)
			{
				stringBuilder.Append("> ");
			}
			try
			{
				stringBuilder.Append(KeyOf(alerts[i].kind).Translate(text.Named("PAWN")).ToString());
			}
			catch
			{
				stringBuilder.Append(text);
			}
		}
		if (stringBuilder.Length > 0)
		{
			stringBuilder.Append("\n\n");
		}
		stringBuilder.Append("RCB_Alert_ClickToJump".Translate().ToString());
		cachedTip = stringBuilder.ToString();
		return cachedTip;
	}
}
