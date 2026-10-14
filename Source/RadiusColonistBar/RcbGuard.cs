using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbGuard
{
	private static readonly HashSet<string> logged = new HashSet<string>();

	public static bool Finite(float v)
	{
		return !float.IsNaN(v) && !float.IsInfinity(v);
	}

	public static bool Finite(Rect r)
	{
		return Finite(r.x) && Finite(r.y) && Finite(r.width) && Finite(r.height);
	}

	public static float Sane(float v, float def)
	{
		return Finite(v) ? v : def;
	}

	public static void WarnOnce(string site, string detail)
	{
		if (logged.Count <= 32 && logged.Add(site))
		{
			Log.Warning("[Radius UI - Colonist Bar] Skipped a GUI.matrix transform at " + site + " - an input was NaN/Infinity (fell back to an unscaled draw). Corrupt data reached the bar (another mod's colonist-bar patch or a corrupt config value). Inputs: " + detail);
		}
	}
}
