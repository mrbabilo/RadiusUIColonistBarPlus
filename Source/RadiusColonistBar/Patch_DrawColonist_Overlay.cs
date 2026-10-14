using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawColonist")]
[StaticConstructorOnStartup]
internal static class Patch_DrawColonist_Overlay
{
	private sealed class WepIconBake
	{
		public Texture2D color;

		public Texture2D gray;

		public Texture2D art;

		public bool blank;

		public Rect bounds = new Rect(0f, 0f, 1f, 1f);
	}

	internal static bool DrawingBarColonist;

	private static readonly DamageFlashTracker barFlash = new DamageFlashTracker();

	private static readonly Dictionary<ushort, WepIconBake> wepIconCache = new Dictionary<ushort, WepIconBake>();

	private static Color SuiteGood => RcbPalette.Good;

	private static Color SuiteWarn => RcbPalette.Warn;

	private static Color SuiteBad => RcbPalette.Bad;

	internal static void PruneCaches(HashSet<int> liveIds)
	{
		barFlash.Prune(liveIds);
		MarksHost.Prune(liveIds);
	}

	private static bool Prefix()
	{
		DrawingBarColonist = true;
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return true;
		}
		if (Event.current.type != EventType.Repaint || RcbIsland.SlabDrawnThisFrame)
		{
			return false;
		}
		float scale = ((Find.ColonistBar != null) ? Find.ColonistBar.Scale : 1f);
		RcbIsland.DrawSlabOnce(scale);
		return false;
	}

	private static void Finalizer()
	{
		DrawingBarColonist = false;
	}

	private static void Postfix(ColonistBarColonistDrawer __instance, Rect rect, Pawn colonist)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings != null && settings.masterEnabled && colonist != null && Event.current.type == EventType.Repaint && !(RcbIslandAnim.ContentA < 0.01f))
		{
			float scale = ((Find.ColonistBar != null) ? Find.ColonistBar.Scale : 1f);
			float dmgFlash = barFlash.Flash(colonist);
			Rect pawnTextureRect = __instance.GetPawnTextureRect(rect.position);
			RcbIsland.DrawSegment(rect, pawnTextureRect, colonist, scale, settings, dmgFlash);
			if (!RcbIslandAnim.SuppressCellInput)
			{
				ReorderBounds.Record(colonist, rect);
			}
		}
	}

	internal static bool TryWeaponIcons(ThingDef def, out Texture2D colour, out Texture2D gray)
	{
		colour = null;
		gray = null;
		WepIconBake wepIconBake = IconBake(def);
		if (wepIconBake == null || wepIconBake.blank)
		{
			return false;
		}
		colour = wepIconBake.color ?? def?.uiIcon;
		gray = wepIconBake.gray ?? colour;
		return colour != null;
	}

	internal static bool TryWeaponArt(ThingDef def, out Texture2D colour, out Rect uv)
	{
		colour = null;
		uv = new Rect(0f, 0f, 1f, 1f);
		WepIconBake wepIconBake = IconBake(def);
		if (wepIconBake == null || wepIconBake.blank)
		{
			return false;
		}
		colour = wepIconBake.color ?? wepIconBake.art;
		uv = wepIconBake.bounds;
		return colour != null;
	}

	private static WepIconBake IconBake(ThingDef def)
	{
		if (def == null)
		{
			return null;
		}
		if (wepIconCache.TryGetValue(def.shortHash, out var value))
		{
			return value;
		}
		WepIconBake wepIconBake = new WepIconBake();
		Texture2D uiIcon = def.uiIcon;
		if (uiIcon == null)
		{
			wepIconBake.blank = true;
		}
		else
		{
			RenderTexture active = RenderTexture.active;
			RenderTexture renderTexture = null;
			try
			{
				float num = Mathf.Min(1f, 256f / (float)Mathf.Max(uiIcon.width, uiIcon.height));
				int num2 = Mathf.Max(8, Mathf.RoundToInt((float)uiIcon.width * num));
				int num3 = Mathf.Max(8, Mathf.RoundToInt((float)uiIcon.height * num));
				renderTexture = RenderTexture.GetTemporary(num2, num3, 0, RenderTextureFormat.ARGB32);
				Graphics.Blit(uiIcon, renderTexture);
				RenderTexture.active = renderTexture;
				Texture2D texture2D = new Texture2D(num2, num3, TextureFormat.ARGB32, mipChain: false)
				{
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp
				};
				texture2D.ReadPixels(new Rect(0f, 0f, num2, num3), 0, 0);
				Color32[] pixels = texture2D.GetPixels32();
				int[] array = new int[8]
				{
					0,
					num2 - 1,
					(num3 - 1) * num2,
					(num3 - 1) * num2 + num2 - 1,
					num2 / 2,
					(num3 - 1) * num2 + num2 / 2,
					num3 / 2 * num2,
					num3 / 2 * num2 + num2 - 1
				};
				int num4 = 0;
				int num5 = 255;
				int num6 = 0;
				int num7 = 255;
				int num8 = 0;
				int num9 = 255;
				int num10 = 0;
				int num11 = 0;
				int num12 = 0;
				int num13 = 0;
				for (int i = 0; i < array.Length; i++)
				{
					Color32 color = pixels[array[i]];
					if (color.a >= 240)
					{
						num4++;
					}
					num5 = Mathf.Min(num5, color.r);
					num6 = Mathf.Max(num6, color.r);
					num11 += color.r;
					num7 = Mathf.Min(num7, color.g);
					num8 = Mathf.Max(num8, color.g);
					num12 += color.g;
					num9 = Mathf.Min(num9, color.b);
					num10 = Mathf.Max(num10, color.b);
					num13 += color.b;
				}
				bool flag = num4 >= 6 && num6 - num5 <= 32 && num8 - num7 <= 32 && num10 - num9 <= 32;
				if (flag)
				{
					int num14 = num11 / 8;
					int num15 = num12 / 8;
					int num16 = num13 / 8;
					for (int j = 0; j < pixels.Length; j++)
					{
						Color32 color2 = pixels[j];
						int num17 = Mathf.Max(Mathf.Abs(color2.r - num14), Mathf.Max(Mathf.Abs(color2.g - num15), Mathf.Abs(color2.b - num16)));
						if (num17 <= 28)
						{
							color2.a = 0;
						}
						else if (num17 < 70)
						{
							color2.a = (byte)(color2.a * (num17 - 28) / 42);
						}
						pixels[j] = color2;
					}
				}
				int num18 = 0;
				int num19 = num2;
				int num20 = -1;
				int num21 = num3;
				int num22 = -1;
				for (int k = 0; k < pixels.Length; k++)
				{
					if (pixels[k].a > 12)
					{
						num18++;
						int num23 = k % num2;
						int num24 = k / num2;
						if (num23 < num19)
						{
							num19 = num23;
						}
						if (num23 > num20)
						{
							num20 = num23;
						}
						if (num24 < num21)
						{
							num21 = num24;
						}
						if (num24 > num22)
						{
							num22 = num24;
						}
					}
				}
				if (num20 >= num19 && num22 >= num21)
				{
					float num25 = Mathf.Max(0, num19 - 1);
					float num26 = Mathf.Min(num2, num20 + 2);
					float num27 = Mathf.Max(0, num21 - 1);
					float num28 = Mathf.Min(num3, num22 + 2);
					wepIconBake.bounds = new Rect(num25 / (float)num2, num27 / (float)num3, (num26 - num25) / (float)num2, (num28 - num27) / (float)num3);
				}
				if (num18 * 50 < pixels.Length)
				{
					wepIconBake.blank = true;
					Object.Destroy(texture2D);
				}
				else
				{
					if (flag)
					{
						texture2D.SetPixels32(pixels);
						texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: false);
						wepIconBake.color = texture2D;
					}
					else
					{
						wepIconBake.art = texture2D;
					}
					Color32[] array2 = new Color32[pixels.Length];
					for (int l = 0; l < pixels.Length; l++)
					{
						Color32 color3 = pixels[l];
						byte b = (byte)Mathf.Min(255, Mathf.RoundToInt((float)(int)color3.r * 0.299f + (float)(int)color3.g * 0.587f + (float)(int)color3.b * 0.114f));
						array2[l] = new Color32(b, b, b, color3.a);
					}
					Texture2D texture2D2 = new Texture2D(num2, num3, TextureFormat.ARGB32, mipChain: false)
					{
						filterMode = FilterMode.Bilinear,
						wrapMode = TextureWrapMode.Clamp
					};
					texture2D2.SetPixels32(array2);
					texture2D2.Apply(updateMipmaps: false, makeNoLongerReadable: false);
					wepIconBake.gray = texture2D2;
				}
			}
			catch
			{
			}
			finally
			{
				RenderTexture.active = active;
				if (renderTexture != null)
				{
					RenderTexture.ReleaseTemporary(renderTexture);
				}
			}
		}
		wepIconCache[def.shortHash] = wepIconBake;
		return wepIconBake;
	}

	internal static bool TryResolveStat(Pawn p, string key, out float frac, out Color col, out bool isHealth)
	{
		frac = 0f;
		col = default(Color);
		isHealth = false;
		if (IsNoneKey(key))
		{
			return false;
		}
		RCBStats.ResolveValue(p, key, out frac, out col, out isHealth);
		return true;
	}

	private static void ResolveStat(Pawn p, string key, string fallback, out string label, out float frac, out Color col, out bool isHealth)
	{
		RCBStats.Resolve(p, key, fallback, out label, out frac, out col, out isHealth);
	}

	private static bool IsNoneKey(string key)
	{
		return string.IsNullOrEmpty(key) || key == "__none__";
	}

	private static bool IsSelected(Pawn p)
	{
		return BarSelection.IsSelected(p);
	}

	internal static string GetRoyalTitle(Pawn p)
	{
		return PawnUiCache.Get(p).royalTitle;
	}

	internal static string GetJobTitle(Pawn p)
	{
		return PawnUiCache.Get(p).jobTitle;
	}

	internal static string GetIdeoRole(Pawn p)
	{
		return PawnUiCache.Get(p).role;
	}

	internal static string GetJobText(Pawn p)
	{
		return PawnUiCache.Get(p).job;
	}
}
