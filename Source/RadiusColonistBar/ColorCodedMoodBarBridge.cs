using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RadiusColonistBar;

public static class ColorCodedMoodBarBridge
{
	private static bool init;

	private static object settings;

	private static FieldInfo extremeF;

	private static FieldInfo majorF;

	private static FieldInfo minorF;

	private static FieldInfo neutralF;

	private static FieldInfo contentF;

	private static FieldInfo happyF;

	private static Color cExtreme;

	private static Color cMajor;

	private static Color cMinor;

	private static Color cNeutral;

	private static Color cContent;

	private static Color cHappy;

	private static bool paletteOk;

	private static float nextPaletteAt = -1f;

	private const float PaletteRefreshSeconds = 2f;

	public static bool Active
	{
		get
		{
			Ensure();
			return settings != null && extremeF != null && happyF != null;
		}
	}

	private static void Ensure()
	{
		if (init)
		{
			return;
		}
		init = true;
		try
		{
			Type type = AccessTools.TypeByName("ColoredMoodBar13.Main");
			if (!(type == null))
			{
				settings = AccessTools.Field(type, "Settings")?.GetValue(null);
				if (settings != null)
				{
					Type type2 = settings.GetType();
					extremeF = AccessTools.Field(type2, "Extreme");
					majorF = AccessTools.Field(type2, "Major");
					minorF = AccessTools.Field(type2, "Minor");
					neutralF = AccessTools.Field(type2, "Neutral");
					contentF = AccessTools.Field(type2, "Content");
					happyF = AccessTools.Field(type2, "Happy");
				}
			}
		}
		catch
		{
			settings = null;
		}
	}

	private static void EnsurePalette()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (nextPaletteAt >= 0f && realtimeSinceStartup < nextPaletteAt)
		{
			return;
		}
		nextPaletteAt = realtimeSinceStartup + 2f;
		try
		{
			cExtreme = Opaque((Color)extremeF.GetValue(settings));
			cMajor = Opaque((Color)majorF.GetValue(settings));
			cMinor = Opaque((Color)minorF.GetValue(settings));
			cNeutral = Opaque((Color)neutralF.GetValue(settings));
			cContent = Opaque((Color)contentF.GetValue(settings));
			cHappy = Opaque((Color)happyF.GetValue(settings));
			paletteOk = true;
		}
		catch
		{
			paletteOk = false;
		}
	}

	private static Color Opaque(Color c)
	{
		return new Color(c.r, c.g, c.b, 1f);
	}

	public static bool TryGetColor(float frac, float thrExtreme, float thrMajor, float thrMinor, out Color col)
	{
		col = Color.white;
		if (!Active)
		{
			return false;
		}
		EnsurePalette();
		if (!paletteOk)
		{
			return false;
		}
		if (thrMinor > 0f && frac <= thrExtreme)
		{
			col = cExtreme;
		}
		else if (thrMinor > 0f && frac <= thrMajor)
		{
			col = cMajor;
		}
		else if (thrMinor > 0f && frac <= thrMinor)
		{
			col = cMinor;
		}
		else if (frac <= 0.65f)
		{
			col = cNeutral;
		}
		else if (frac <= 0.9f)
		{
			col = cContent;
		}
		else
		{
			col = cHappy;
		}
		return true;
	}
}
