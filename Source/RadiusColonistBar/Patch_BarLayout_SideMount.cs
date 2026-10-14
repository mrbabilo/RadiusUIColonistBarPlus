using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch]
internal static class Patch_BarLayout_SideMount
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(ColonistBarDrawLocsFinder), "CalculateDrawLocs", new Type[3]
		{
			typeof(List<Vector2>),
			typeof(float).MakeByRefType(),
			typeof(int)
		}, (Type[])null);
	}

	private static void Postfix(List<Vector2> outDrawLocs, ref float scale)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return;
		}
		int count = outDrawLocs.Count;
		if (count == 0)
		{
			return;
		}
		bool flag = settings.partyPanel && settings.liveView && !settings.warbandMode;
		try
		{
			if (flag)
			{
				float num = 80f + Mathf.Max(0f, settings.barTopOffset);
				float num2 = Mathf.Max(200f, (float)UI.screenHeight - num - 80f);
				float num3 = Mathf.Max(0f, settings.verticalPadding);
				float num4 = ColonistBar.BaseSize.y * 1.35f;
				scale = Mathf.Clamp(num2 / ((float)count * (num4 + num3)), 0.5f, 1.5f);
				float num5 = num4 * scale + num3;
				for (int i = 0; i < count; i++)
				{
					outDrawLocs[i] = new Vector2(8f, num + (float)i * num5);
				}
				return;
			}
			if (settings.warbandMode && settings.liveView)
			{
				for (int j = 0; j < count; j++)
				{
					float y = outDrawLocs[j].y;
					bool flag2 = true;
					for (int k = 0; k < j; k++)
					{
						if (Mathf.Abs(outDrawLocs[k].y - y) < 0.5f)
						{
							flag2 = false;
							break;
						}
					}
					if (!flag2)
					{
						continue;
					}
					float num6 = float.MaxValue;
					for (int l = 0; l < count; l++)
					{
						if (Mathf.Abs(outDrawLocs[l].y - y) < 0.5f)
						{
							num6 = Mathf.Min(num6, outDrawLocs[l].x);
						}
					}
					float num7 = num6 - 56f;
					if (Mathf.Abs(num7) < 0.5f)
					{
						continue;
					}
					for (int m = 0; m < count; m++)
					{
						if (Mathf.Abs(outDrawLocs[m].y - y) < 0.5f)
						{
							Vector2 value = outDrawLocs[m];
							value.x -= num7;
							outDrawLocs[m] = value;
						}
					}
				}
			}
			if (!(settings.verticalPadding > 0.5f) || count <= 1)
			{
				return;
			}
			int num8 = 0;
			float y2 = outDrawLocs[0].y;
			for (int n = 0; n < count; n++)
			{
				if (outDrawLocs[n].y > y2 + 0.5f)
				{
					num8++;
					y2 = outDrawLocs[n].y;
				}
				Vector2 value2 = outDrawLocs[n];
				value2.y += (float)num8 * settings.verticalPadding;
				outDrawLocs[n] = value2;
			}
		}
		catch
		{
		}
	}
}
