using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
internal static class FrameSources
{
	private static bool vpeProbed;

	private static bool vpeReady;

	private static Type vpeHediffType;

	private static FieldInfo vpePaths;

	private static FieldInfo vpePathColor;

	private static FieldInfo vpePathImage;

	private static Texture2D psylinkEmblem;

	private static bool psylinkProbed;

	private static Texture2D PsylinkEmblem
	{
		get
		{
			if (psylinkProbed)
			{
				return psylinkEmblem;
			}
			psylinkProbed = true;
			try
			{
				psylinkEmblem = DefDatabase<ThingDef>.GetNamedSilentFail("PsychicAmplifier")?.uiIcon;
			}
			catch
			{
			}
			return psylinkEmblem;
		}
	}

	private static void ProbeVpe()
	{
		vpeProbed = true;
		try
		{
			vpeHediffType = AccessTools.TypeByName("VanillaPsycastsExpanded.Hediff_PsycastAbilities");
			Type type = AccessTools.TypeByName("VanillaPsycastsExpanded.PsycasterPathDef");
			if (!(vpeHediffType == null) && !(type == null))
			{
				vpePaths = AccessTools.Field(vpeHediffType, "unlockedPaths");
				vpePathColor = AccessTools.Field(type, "backgroundColor");
				vpePathImage = AccessTools.Field(type, "backgroundImage");
				vpeReady = vpePaths != null && vpePathColor != null;
			}
		}
		catch
		{
			vpeReady = false;
		}
	}

	private static Def FirstPath(Pawn p)
	{
		if (!vpeProbed)
		{
			ProbeVpe();
		}
		if (!vpeReady)
		{
			return null;
		}
		List<Hediff> list = p.health?.hediffSet?.hediffs;
		if (list == null)
		{
			return null;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Hediff hediff = list[i];
			if (hediff != null && vpeHediffType.IsInstanceOfType(hediff))
			{
				if (!(vpePaths.GetValue(hediff) is IList { Count: not 0 } list2))
				{
					return null;
				}
				return list2[0] as Def;
			}
		}
		return null;
	}

	internal static void Resolve(Pawn p, PawnUiCache.Snap s, RadiusColonistBarSettings cfg)
	{
		s.hasFrame = false;
		s.frameGlyph = null;
		s.frameEmblem = null;
		if (cfg == null || cfg.frameMode == 0)
		{
			return;
		}
		List<int> frameSources = cfg.frameSources;
		if (frameSources == null)
		{
			return;
		}
		for (int i = 0; i < frameSources.Count; i++)
		{
			switch (frameSources[i])
			{
			case 0:
			{
				Def def = null;
				try
				{
					def = FirstPath(p);
				}
				catch
				{
				}
				if (def == null)
				{
					break;
				}
				try
				{
					s.frameHue = (Color)vpePathColor.GetValue(def);
					s.frameEmblem = ((vpePathImage != null) ? (vpePathImage.GetValue(def) as Texture2D) : null);
				}
				catch
				{
					break;
				}
				if (s.frameHue.maxColorComponent < 0.05f)
				{
					s.frameHue = Palette.Flat.Faction;
				}
				s.frameHue.a = 1f;
				s.hasFrame = true;
				return;
			}
			case 1:
			{
				int num = 0;
				try
				{
					num = p.GetPsylinkLevel();
				}
				catch
				{
				}
				if (num <= 0)
				{
					break;
				}
				s.frameHue = Palette.Flat.Faction;
				s.frameHue.a = 1f;
				s.frameGlyph = null;
				s.frameEmblem = PsylinkEmblem;
				s.hasFrame = true;
				return;
			}
			case 2:
			{
				if (!ModsConfig.IdeologyActive)
				{
					break;
				}
				Ideo ideo = null;
				Precept_Role precept_Role = null;
				try
				{
					ideo = p.Ideo;
					precept_Role = ideo?.GetRole(p);
				}
				catch
				{
				}
				if (precept_Role == null)
				{
					break;
				}
				s.frameHue = ideo.Color;
				s.frameHue.a = 1f;
				try
				{
					s.frameGlyph = precept_Role.def?.Icon;
					s.frameEmblem = ideo.Icon;
				}
				catch
				{
				}
				s.hasFrame = true;
				return;
			}
			case 3:
			{
				if (!ModsConfig.RoyaltyActive)
				{
					break;
				}
				RoyalTitle royalTitle = null;
				try
				{
					royalTitle = p.royalty?.MostSeniorTitle;
				}
				catch
				{
				}
				if (royalTitle?.def == null)
				{
					break;
				}
				s.frameHue = Palette.Flat.Accent;
				s.frameHue.a = 1f;
				try
				{
					s.frameEmblem = royalTitle.faction?.def?.FactionIcon;
				}
				catch
				{
				}
				s.hasFrame = true;
				return;
			}
			}
		}
	}
}
