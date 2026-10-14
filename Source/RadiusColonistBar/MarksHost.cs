using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class MarksHost
{
	private sealed class Held
	{
		public readonly Texture2D[] icon = new Texture2D[6];

		public readonly Texture2D[] accent = new Texture2D[6];

		public readonly Color[] col = new Color[6];

		public readonly Color[] acc = new Color[6];

		public int count;

		public float at;
	}

	private static bool bound;

	private static Action<Pawn, Rect, float> drawMarks;

	private static Action<ColonistBarColonistDrawer, Rect, Pawn> busyworkMark;

	private static FieldInfo fiAtBar;

	private static PropertyInfo piMarkSize;

	private static readonly object BoxTrue = true;

	private static readonly object BoxFalse = false;

	internal static MethodBase DrawMarkTarget;

	internal static bool Hosting;

	private static float ox;

	private static float oy;

	private static float step;

	private static float cell;

	private static float capPx;

	private static int placed;

	private static int maxCols;

	private static int cap;

	private static int curId;

	private static int fails;

	private static bool parked;

	private const float HoldSecs = 0.4f;

	private const int HeldCap = 6;

	private static readonly Dictionary<int, Held> held = new Dictionary<int, Held>();

	private static readonly List<int> pruneScratch = new List<int>();

	private static Held cur;

	private static bool replaying;

	private static Action<Texture2D, Texture2D, Color, Color, Vector2, float?> drawMark;

	private static float markPx = 16f;

	private static int markPxFrame = -999;

	internal static bool Available
	{
		get
		{
			Ensure();
			return drawMarks != null || busyworkMark != null;
		}
	}

	internal static bool Live
	{
		get
		{
			if (parked || !Available)
			{
				return false;
			}
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			return settings != null && settings.masterEnabled && settings.hostForeignMarks;
		}
	}

	private static void Ensure()
	{
		if (bound)
		{
			return;
		}
		bound = true;
		try
		{
			Type type = AccessTools.TypeByName("UsefulMarks.NamePlatePatches");
			if (type != null)
			{
				MethodInfo methodInfo = AccessTools.Method(type, "DrawMarks", new Type[3]
				{
					typeof(Pawn),
					typeof(Rect),
					typeof(float)
				}, (Type[])null);
				if (methodInfo != null)
				{
					drawMarks = (Action<Pawn, Rect, float>)Delegate.CreateDelegate(typeof(Action<Pawn, Rect, float>), methodInfo);
				}
				DrawMarkTarget = AccessTools.Method(type, "DrawMark", (Type[])null, (Type[])null);
				if (DrawMarkTarget is MethodInfo method)
				{
					drawMark = (Action<Texture2D, Texture2D, Color, Color, Vector2, float?>)Delegate.CreateDelegate(typeof(Action<Texture2D, Texture2D, Color, Color, Vector2, float?>), method);
				}
				Type type2 = AccessTools.Inner(type, "GenMapUI_DrawPawnLabel_Patch");
				if (type2 != null)
				{
					fiAtBar = AccessTools.Field(type2, "DrawingAtColonistBar");
				}
			}
			Type type3 = AccessTools.TypeByName("UsefulMarks.Assets");
			if (type3 != null)
			{
				piMarkSize = AccessTools.Property(type3, "MarkSize");
			}
			Type type4 = AccessTools.TypeByName("Busywork.Patch_ColonistBarColonistDrawer_DrawColonist");
			MethodInfo methodInfo2 = ((type4 != null) ? AccessTools.Method(type4, "Postfix", (Type[])null, (Type[])null) : null);
			if (methodInfo2 != null)
			{
				busyworkMark = (Action<ColonistBarColonistDrawer, Rect, Pawn>)Delegate.CreateDelegate(typeof(Action<ColonistBarColonistDrawer, Rect, Pawn>), methodInfo2);
			}
		}
		catch (Exception ex)
		{
			Log.Warning("[Radius UI - Colonist Bar] could not bind the Useful Marks host: " + ex.Message);
			drawMarks = null;
			busyworkMark = null;
		}
	}

	internal static void Prune(HashSet<int> liveIds)
	{
		if (held.Count == 0)
		{
			return;
		}
		pruneScratch.Clear();
		foreach (KeyValuePair<int, Held> item in held)
		{
			if (!liveIds.Contains(item.Key))
			{
				pruneScratch.Add(item.Key);
			}
		}
		for (int i = 0; i < pruneScratch.Count; i++)
		{
			held.Remove(pruneScratch[i]);
		}
		pruneScratch.Clear();
	}

	private static float MarkPx()
	{
		if (Time.frameCount - markPxFrame < 60)
		{
			return markPx;
		}
		markPxFrame = Time.frameCount;
		float num = 16f;
		try
		{
			if (piMarkSize != null)
			{
				num = ((Vector2)piMarkSize.GetValue(null)).x;
			}
		}
		catch
		{
		}
		markPx = Mathf.Max(4f, num / Mathf.Max(0.01f, Prefs.UIScale));
		return markPx;
	}

	internal static void DrawFor(Pawn p, Rect strip)
	{
		if (p == null || !Live)
		{
			return;
		}
		cell = Mathf.Min(MarkPx(), strip.height);
		step = cell + 1f;
		maxCols = Mathf.Max(1, Mathf.FloorToInt(strip.width / step));
		cap = Mathf.Min(maxCols * 2, 6);
		capPx = strip.height;
		ox = strip.x;
		oy = strip.y;
		placed = 0;
		curId = p.thingIDNumber;
		held.TryGetValue(curId, out cur);
		Color color = GUI.color;
		TextAnchor anchor = Text.Anchor;
		GameFont font = Text.Font;
		Hosting = true;
		try
		{
			SetAtBar(BoxTrue);
			if (drawMarks != null)
			{
				drawMarks(p, strip, 0f);
			}
			if (busyworkMark != null)
			{
				busyworkMark(null, strip, p);
			}
			if (placed > 0)
			{
				if (cur != null)
				{
					cur.count = placed;
					cur.at = Time.realtimeSinceStartup;
				}
			}
			else if (cur != null && cur.count > 0 && drawMark != null && Time.realtimeSinceStartup - cur.at <= 0.4f)
			{
				replaying = true;
				for (int i = 0; i < cur.count; i++)
				{
					drawMark(cur.icon[i], cur.accent[i], cur.col[i], cur.acc[i], Vector2.zero, null);
				}
				replaying = false;
			}
			fails = 0;
		}
		catch (Exception ex)
		{
			if (++fails >= 10)
			{
				parked = true;
				Log.Warning("[Radius UI - Colonist Bar] parking the Useful Marks host for this session after repeated errors: " + ex.Message);
			}
			else
			{
				Log.WarningOnce("[Radius UI - Colonist Bar] Useful Marks host threw: " + ex, 1296126539);
			}
		}
		finally
		{
			SetAtBar(BoxFalse);
			Hosting = false;
			replaying = false;
			cur = null;
			GUI.color = color;
			Text.Anchor = anchor;
			Text.Font = font;
		}
	}

	private static void SetAtBar(object v)
	{
		try
		{
			if (fiAtBar != null)
			{
				fiAtBar.SetValue(null, v);
			}
		}
		catch
		{
		}
	}

	internal static bool Aim(Texture2D icon, Texture2D accent, Color col, Color acc, ref Vector2 point, ref float? limitScalePix)
	{
		if (placed >= cap)
		{
			return false;
		}
		if (!replaying)
		{
			if (cur == null)
			{
				held[curId] = (cur = new Held());
			}
			cur.icon[placed] = icon;
			cur.accent[placed] = accent;
			cur.col[placed] = col;
			cur.acc[placed] = acc;
		}
		int num = placed % maxCols;
		int num2 = placed / maxCols;
		point = new Vector2(ox + step * (float)num + cell * 0.5f, oy + step * (float)num2 + cell * 0.5f);
		limitScalePix = capPx;
		placed++;
		return true;
	}
}
