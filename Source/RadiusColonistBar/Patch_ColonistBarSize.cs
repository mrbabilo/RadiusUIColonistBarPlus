using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarDrawLocsFinder))]
internal static class Patch_ColonistBarSize
{
	private static int[] rowOf = new int[16];

	private static float[] xOf = new float[16];

	private static readonly List<float> rowWidth = new List<float>(4);

	private static int lastCount = -1;

	private static float lastSig = float.NaN;

	private static Vector2 lastFirst = new Vector2(float.NaN, float.NaN);

	private static float Factor()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return 1f;
		}
		return Mathf.Clamp(settings.portraitScale, 0.3f, 3f);
	}

	[HarmonyPatch("MaxColonistBarWidth", MethodType.Getter)]
	[HarmonyPostfix]
	private static void MaxWidthPostfix(ref float __result)
	{
		float num = Factor();
		if (num != 1f)
		{
			__result /= num;
		}
	}

	[HarmonyPatch("FindBestScale")]
	[HarmonyPostfix]
	private static void FindBestScalePostfix(ref float __result)
	{
		float num = Factor();
		if (num != 1f)
		{
			__result *= num;
		}
	}

	[HarmonyPatch("CalculateDrawLocs", new Type[]
	{
		typeof(List<Vector2>),
		typeof(float),
		typeof(bool),
		typeof(int),
		typeof(int)
	})]
	[HarmonyPostfix]
	private static void OffsetDrawLocsPostfix(List<Vector2> outDrawLocs, float scale)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled || outDrawLocs == null)
		{
			return;
		}
		if (Patch_BarMetrics.IslandActive(settings))
		{
			RelayoutIsland(outDrawLocs, scale);
		}
		float barTopOffset = settings.barTopOffset;
		if (!Patch_BarMetrics.IslandActive(settings) && barTopOffset != 0f)
		{
			for (int i = 0; i < outDrawLocs.Count; i++)
			{
				Vector2 value = outDrawLocs[i];
				value.y += barTopOffset;
				outDrawLocs[i] = value;
			}
		}
	}

	internal static void RelayoutIsland(List<Vector2> locs, float scale)
	{
		if (locs == null)
		{
			return;
		}
		List<ColonistBar.Entry> list = Find.ColonistBar?.Entries;
		Vector2 vector = RcbIsland.SegSize(scale);
		int count = locs.Count;
		if (count == 0 || !RcbGuard.Finite(vector.x) || !RcbGuard.Finite(vector.y) || vector.x < 2f)
		{
			return;
		}
		float num = Mathf.Max(1f, 2f * scale);
		float num2 = 17f * scale;
		float num3 = 8f * scale;
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		float num4 = ((settings != null) ? Mathf.Clamp(settings.islandMaxWidthPct, 30f, 100f) : 86f);
		float num5 = Mathf.Max(vector.x, (float)UI.screenWidth * (num4 / 100f));
		float num6 = RcbIsland.ContentTop(scale);
		int num7 = 17;
		float num8 = 0f;
		int num9 = int.MinValue;
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				int num10 = list[i].group;
				num7 = num7 * 31 + num10;
				num8 += ((num8 <= 0f) ? 0f : ((num10 != num9) ? num2 : num)) + vector.x;
				num9 = num10;
			}
		}
		else
		{
			num8 = (float)count * (vector.x + num);
		}
		float num11 = settings?.openMaxHpx ?? 0f;
		if (num11 > 0f && num8 > 0f)
		{
			float b = vector.y + num3;
			float num12 = num6 + RcbAlertBand.Height() + 14f;
			int num13 = Mathf.Max(1, Mathf.FloorToInt((num11 - num12) / Mathf.Max(1f, b)));
			if (num8 > num5 * (float)num13)
			{
				num5 = num8 / (float)num13 + vector.x;
			}
		}
		float num14 = RcbIsland.SlabOffsetX();
		float a = (float)count * 7919f + vector.x * 31f + vector.y * 17f + num6 * 13f + num5 * 3f + num * 5f + num2 * 11f + num3 * 23f + num14 * 29f + (float)(num7 & 0xFFFF);
		Vector2 vector2 = locs[0];
		if (count == lastCount && Mathf.Approximately(a, lastSig) && Mathf.Approximately(vector2.x, lastFirst.x) && Mathf.Approximately(vector2.y, lastFirst.y))
		{
			return;
		}
		if (rowOf.Length < count)
		{
			rowOf = new int[Mathf.NextPowerOfTwo(count)];
			xOf = new float[rowOf.Length];
		}
		rowWidth.Clear();
		int num15 = 0;
		float num16 = 0f;
		int num17 = int.MinValue;
		for (int j = 0; j < count; j++)
		{
			int num18 = ((list != null && j < list.Count) ? list[j].group : 0);
			float num19 = ((j == 0) ? 0f : ((num18 != num17) ? num2 : num));
			if (j > 0 && num16 + num19 + vector.x > num5)
			{
				rowWidth.Add(num16);
				num15++;
				num16 = 0f;
				num19 = 0f;
			}
			num16 += num19;
			rowOf[j] = num15;
			xOf[j] = num16;
			num16 += vector.x;
			num17 = num18;
		}
		rowWidth.Add(num16);
		for (int k = 0; k < count && k < locs.Count; k++)
		{
			float num20 = rowWidth[rowOf[k]];
			float num21 = ((float)UI.screenWidth - num20) * 0.5f + num14;
			locs[k] = new Vector2(num21 + xOf[k], num6 + (float)rowOf[k] * (vector.y + num3));
		}
		lastCount = count;
		lastSig = a;
		lastFirst = ((locs.Count > 0) ? locs[0] : Vector2.zero);
	}

	internal static void InvalidateLayout()
	{
		lastCount = -1;
		lastSig = float.NaN;
	}
}
