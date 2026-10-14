using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class CardPalette
{
	private static readonly Dictionary<int, Texture2D> moodTexCache = new Dictionary<int, Texture2D>();

	public static Color PanelBG => RcbPalette.PanelBG;

	public static Color Track
	{
		get
		{
			Color bGD = RcbPalette.BGD;
			bGD.a = 0.92f;
			return bGD;
		}
	}

	public static Color Accent => RcbPalette.Accent;

	public static Color StatText => RcbPalette.Stat;

	public static Color Good => RcbPalette.Good;

	public static Color Warn => RcbPalette.Warn;

	public static Color Bad => RcbPalette.Bad;

	public static Color Over => RcbPalette.Over;

	public static Color TextDim => RcbPalette.TextDim;

	public static Color RowAlt => RcbPalette.RowAlt;

	public static Color Bg(RadiusColonistBarSettings s)
	{
		Color panelBG = RcbPalette.PanelBG;
		panelBG.a = 0.92f;
		return panelBG;
	}

	public static Color Border(RadiusColonistBarSettings s)
	{
		return Palette.Flat.PanelBorder;
	}

	public static Color DragBg(RadiusColonistBarSettings s)
	{
		Color panelBG = RcbPalette.PanelBG;
		panelBG.a = 0.96f;
		return panelBG;
	}

	public static Color DragBorder(RadiusColonistBarSettings s)
	{
		Color accent = RcbPalette.Accent;
		accent.a = 0.9f;
		return accent;
	}

	public static Color BubbleBg(RadiusColonistBarSettings s)
	{
		Color ink = RcbPalette.Ink;
		ink.a = ((s != null) ? Mathf.Clamp01(RcbGuard.Sane(s.barOpacityPct, 92f) / 100f) : 0.92f);
		return ink;
	}

	public static Color BubbleText(RadiusColonistBarSettings s)
	{
		return RcbPalette.Stat;
	}

	public static Color StateColor(Pawn p)
	{
		return State(p) switch
		{
			RcbState.Bad => Bad, 
			RcbState.Warn => Warn, 
			_ => Good, 
		};
	}

	public static RcbState State(Pawn p)
	{
		try
		{
			if (p == null)
			{
				return RcbState.Good;
			}
			if (p.Dead || (p.Downed && !PawnStatusUtil.DownedIsIncidental(p)) || p.InMentalState)
			{
				return RcbState.Bad;
			}
			PawnUiCache.Snap snap = PawnUiCache.Get(p);
			if (p.health != null && (p.health.hediffSet.BleedRateTotal > 0.01f || snap.needsTend))
			{
				return RcbState.Warn;
			}
			Need_Mood need_Mood = p.needs?.mood;
			if (need_Mood != null && snap.thrMinor > 0f && need_Mood.CurLevelPercentage < snap.thrMinor)
			{
				return RcbState.Warn;
			}
		}
		catch
		{
		}
		return RcbState.Good;
	}

	public static Color CellStripColor(Pawn p, bool selected, bool drafted, float dmgFlash, Color draftedCol, Color selectedCol)
	{
		RcbState rcbState = State(p);
		Color color = ((rcbState != RcbState.Good) ? StateColor(p) : (drafted ? draftedCol : ((!selected) ? Good : selectedCol)));
		if (dmgFlash > 0f)
		{
			float t = dmgFlash * (Mathf.Sin(Time.realtimeSinceStartup * 9f) * 0.5f + 0.5f);
			color = Color.Lerp(color, RcbPalette.DamageFlash, t);
		}
		return color;
	}

	public static Color ZoneColor(float f)
	{
		return (f > 0.5f) ? Color.Lerp(Warn, Good, (f - 0.5f) / 0.5f) : Color.Lerp(Bad, Warn, f / 0.5f);
	}

	public static Color MoodColor(Pawn p, float frac)
	{
		try
		{
			if (p != null)
			{
				PawnUiCache.Snap snap = PawnUiCache.Get(p);
				RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
				if (settings != null && settings.useColorCodedMoodBar && ColorCodedMoodBarBridge.TryGetColor(frac, snap.thrExtreme, snap.thrMajor, snap.thrMinor, out var col))
				{
					return col;
				}
				if (snap.thrMinor > 0f)
				{
					if (frac <= snap.thrExtreme + 0.003f)
					{
						return Bad;
					}
					if (frac <= snap.thrMajor + 0.003f)
					{
						return Warn;
					}
					if (frac <= snap.thrMinor + 0.003f)
					{
						return Warn;
					}
				}
			}
		}
		catch
		{
		}
		return Color.Lerp(Good, Over, Mathf.InverseLerp(0.55f, 1f, frac));
	}

	public static bool MoodFillOn(RadiusColonistBarSettings s)
	{
		return s != null && s.masterEnabled && s.useColorCodedMoodBar && ColorCodedMoodBarBridge.Active;
	}

	public static bool TryMoodBacking(Pawn p, RadiusColonistBarSettings s, out Color col)
	{
		col = default(Color);
		if (!MoodFillOn(s) || p == null)
		{
			return false;
		}
		try
		{
			Need_Mood need_Mood = p.needs?.mood;
			if (need_Mood == null)
			{
				return false;
			}
			col = MoodColor(p, Mathf.Clamp01(need_Mood.CurLevelPercentage));
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static Texture2D MoodTex(Color c)
	{
		int key = (Mathf.RoundToInt(Mathf.Clamp01(c.r) * 24f) << 16) | (Mathf.RoundToInt(Mathf.Clamp01(c.g) * 24f) << 8) | Mathf.RoundToInt(Mathf.Clamp01(c.b) * 24f);
		if (!moodTexCache.TryGetValue(key, out var value) || value == null)
		{
			value = SolidColorMaterials.NewSolidColorTexture(new Color(c.r, c.g, c.b, 1f));
			moodTexCache[key] = value;
		}
		return value;
	}
}
