using System;
using System.Collections.Generic;
using LudeonTK;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
internal static class RcbSpatial
{
	private readonly struct RKey(int w, int h, int r) : IEquatable<RKey>
	{
		private readonly int w = w;

		private readonly int h = h;

		private readonly int r = r;

		public bool Equals(RKey o)
		{
			return w == o.w && h == o.h && r == o.r;
		}

		public override bool Equals(object o)
		{
			return o is RKey o2 && Equals(o2);
		}

		public override int GetHashCode()
		{
			return (((w * 397) ^ h) * 397) ^ r;
		}
	}

	public const float RLg = 18f;

	public const float RMd = 12f;

	public const float RSm = 7f;

	public const float RChip = 6f;

	private static readonly Texture2D SurfaceTex = MakeRounded(18);

	private static readonly Texture2D CardTex = MakeRounded(12);

	private static readonly Texture2D SmallTex = MakeRounded(7);

	private static readonly Texture2D ChipTex = MakeRounded(6);

	private static readonly Texture2D ShadowTex = MakeShadow(18, 14);

	private static readonly Texture2D DiscTex = MakeDisc(32);

	private static readonly Texture2D NotchL = MakeNotch(48, left: true);

	private static readonly Texture2D NotchR = MakeNotch(48, left: false);

	private const int MaxBake = 768;

	private const int MaxCached = 96;

	private static readonly Dictionary<RKey, Texture2D> roundCache = new Dictionary<RKey, Texture2D>();

	private static readonly List<RKey> roundOrder = new List<RKey>();

	private static readonly Rect LeftHalf = new Rect(0f, 0f, 0.5f, 1f);

	private static readonly Rect RightHalf = new Rect(0.5f, 0f, 0.5f, 1f);

	private static readonly Rect TopHalf = new Rect(0f, 0.5f, 1f, 0.5f);

	private static readonly Rect BottomHalf = new Rect(0f, 0f, 1f, 0.5f);

	private static void Atlas(Rect r, Color c, Texture2D tex)
	{
		if (RcbGuard.Finite(r) && !(r.width < 2f) && !(r.height < 2f) && !(tex == null))
		{
			Color color = GUI.color;
			GUI.color = c;
			Widgets.DrawAtlas(r, tex);
			GUI.color = color;
		}
	}

	public static void Surface(Rect r, Color c)
	{
		Atlas(r, c, SurfaceTex);
	}

	public static void SurfaceSeamless(Rect r, Color c, float radius = 18f)
	{
		if (RcbGuard.Finite(r) && !(r.width < 4f) && !(r.height < 4f) && !(DiscTex == null))
		{
			float num = Mathf.Round(r.x);
			float num2 = Mathf.Round(r.y);
			float num3 = Mathf.Round(r.width);
			float num4 = Mathf.Round(r.height);
			float num5 = Mathf.Max(1f, Mathf.Min(Mathf.Round(radius), Mathf.Floor(Mathf.Min(num3, num4) * 0.5f)));
			Widgets.DrawBoxSolid(new Rect(num, num2 + num5, num3, num4 - num5 * 2f), c);
			Widgets.DrawBoxSolid(new Rect(num + num5, num2, num3 - num5 * 2f, num5), c);
			Widgets.DrawBoxSolid(new Rect(num + num5, num2 + num4 - num5, num3 - num5 * 2f, num5), c);
			Color color = GUI.color;
			GUI.color = c;
			GUI.DrawTextureWithTexCoords(new Rect(num, num2, num5, num5), DiscTex, new Rect(0f, 0.5f, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(num + num3 - num5, num2, num5, num5), DiscTex, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(num, num2 + num4 - num5, num5, num5), DiscTex, new Rect(0f, 0f, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(num + num3 - num5, num2 + num4 - num5, num5, num5), DiscTex, new Rect(0.5f, 0f, 0.5f, 0.5f));
			GUI.color = color;
		}
	}

	public static void SurfaceFlush(Rect r, Color c)
	{
		if (RcbGuard.Finite(r) && !(r.width < 2f) && !(r.height < 2f) && !(SurfaceTex == null))
		{
			Color color = GUI.color;
			GUI.color = c;
			Widgets.DrawAtlas(r, SurfaceTex, drawTop: false);
			GUI.color = color;
		}
	}

	public static void NotchFillets(Rect slab, Color fill, float radius = 12f)
	{
		if (RcbGuard.Finite(slab) && !(radius < 2f) && !(NotchL == null) && !(NotchR == null))
		{
			float num = Mathf.Round(slab.x);
			float num2 = Mathf.Round(slab.width);
			float num3 = UIScaling.AdjustCoordToUIScalingFloor(num);
			float num4 = UIScaling.AdjustCoordToUIScalingCeil(num + num2);
			float y = UIScaling.AdjustCoordToUIScalingFloor(Mathf.Round(slab.y));
			float num5 = Mathf.Max(2f, Mathf.Round(radius));
			Color color = GUI.color;
			GUI.color = fill;
			GUI.DrawTexture(new Rect(num3 - num5, y, num5 + 1f, num5), NotchL);
			GUI.DrawTexture(new Rect(num4 - 1f, y, num5 + 1f, num5), NotchR);
			GUI.color = color;
		}
	}

	public static void BottomBand(Rect band, Color c, float radius = 18f)
	{
		if (!RcbGuard.Finite(band) || band.width < 2f || band.height < 1f || DiscTex == null)
		{
			return;
		}
		Color color = GUI.color;
		GUI.BeginClip(band);
		try
		{
			float num = Mathf.Round(band.width);
			float num2 = Mathf.Max(1f, Mathf.Min(Mathf.Round(radius), Mathf.Floor(num * 0.5f)));
			float num3 = Mathf.Max(Mathf.Round(band.height), num2);
			float num4 = Mathf.Round(band.height) - num3;
			Widgets.DrawBoxSolid(new Rect(0f, num4, num, num3 - num2), c);
			Widgets.DrawBoxSolid(new Rect(num2, num4 + num3 - num2, num - num2 * 2f, num2), c);
			GUI.color = c;
			GUI.DrawTextureWithTexCoords(new Rect(0f, num4 + num3 - num2, num2, num2), DiscTex, new Rect(0f, 0f, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(num - num2, num4 + num3 - num2, num2, num2), DiscTex, new Rect(0.5f, 0f, 0.5f, 0.5f));
		}
		finally
		{
			GUI.EndClip();
			GUI.color = color;
		}
	}

	public static void Rounded(Rect r, Color c, float radius = 12f, bool exact = false)
	{
		if (!RcbGuard.Finite(r) || r.width < 2f || r.height < 2f)
		{
			return;
		}
		int num = Mathf.RoundToInt(r.width);
		int num2 = Mathf.RoundToInt(r.height);
		if (num > 768 || num2 > 768)
		{
			Atlas(r, c, (radius >= 17f) ? SurfaceTex : CardTex);
			return;
		}
		int num3 = Mathf.Clamp(Mathf.RoundToInt(radius), 1, Mathf.Min(num, num2) / 2);
		int w = (exact ? num : ((num + 7) / 8 * 8));
		int h = (exact ? num2 : ((num2 + 7) / 8 * 8));
		RKey rKey = new RKey(w, h, num3);
		if (!roundCache.TryGetValue(rKey, out var value))
		{
			value = MakeRoundedRect(w, h, num3);
			if (roundOrder.Count >= 96)
			{
				RKey key = roundOrder[0];
				roundOrder.RemoveAt(0);
				if (roundCache.TryGetValue(key, out var value2))
				{
					roundCache.Remove(key);
					if (value2 != null)
					{
						UnityEngine.Object.Destroy(value2);
					}
				}
			}
			roundCache[rKey] = value;
			roundOrder.Add(rKey);
		}
		Color color = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(new Rect(Mathf.Round(r.x), Mathf.Round(r.y), num, num2), value);
		GUI.color = color;
	}

	private static Texture2D MakeRoundedRect(int w, int h, int rad)
	{
		Texture2D texture2D = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false);
		texture2D.filterMode = FilterMode.Bilinear;
		texture2D.wrapMode = TextureWrapMode.Clamp;
		texture2D.hideFlags = HideFlags.HideAndDontSave;
		texture2D.name = "RCB_Round_" + w + "x" + h + "r" + rad;
		Texture2D texture2D2 = texture2D;
		Color32[] array = new Color32[w * h];
		for (int i = 0; i < h; i++)
		{
			float num = Mathf.Max((float)rad - ((float)i + 0.5f), (float)i + 0.5f - (float)(h - rad));
			for (int j = 0; j < w; j++)
			{
				float num2 = Mathf.Max((float)rad - ((float)j + 0.5f), (float)j + 0.5f - (float)(w - rad));
				float num3 = ((num2 > 0f && num > 0f) ? (Mathf.Sqrt(num2 * num2 + num * num) - (float)rad) : (Mathf.Max(num2, num) - (float)rad));
				array[i * w + j] = White(0.5f - num3);
			}
		}
		texture2D2.SetPixels32(array);
		texture2D2.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		return texture2D2;
	}

	public static void Card(Rect r, Color c)
	{
		Atlas(r, c, CardTex);
	}

	public static void Small(Rect r, Color c)
	{
		Atlas(r, c, SmallTex);
	}

	public static void Chip(Rect r, Color c)
	{
		Atlas(r, c, ChipTex);
	}

	public static void ChipFast(Rect r, Color c)
	{
		if (RcbGuard.Finite(r) && !(r.width < 1f) && !(r.height < 1f) && !(ChipTex == null))
		{
			if (r.width > 26f || r.height > 26f || Mathf.Abs(r.width - r.height) > 8f)
			{
				Chip(r, c);
				return;
			}
			Color color = GUI.color;
			GUI.color = c;
			GUI.DrawTexture(r, ChipTex);
			GUI.color = color;
		}
	}

	public static void Pill(Rect r, Color c)
	{
		if (!RcbGuard.Finite(r) || r.width < 1f || r.height < 1f)
		{
			return;
		}
		if (Mathf.Abs(r.width - r.height) < 0.5f)
		{
			Dot(r, c);
		}
		else
		{
			if (DiscTex == null)
			{
				return;
			}
			int num = Mathf.RoundToInt(r.width);
			int num2 = Mathf.RoundToInt(r.height);
			if (num <= 768 && num2 <= 768)
			{
				Rounded(r, c, (float)Mathf.Min(num, num2) * 0.5f, exact: true);
				return;
			}
			Color color = GUI.color;
			GUI.color = c;
			if (r.width > r.height)
			{
				float num3 = r.height * 0.5f;
				GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, num3, r.height), DiscTex, LeftHalf);
				GUI.DrawTextureWithTexCoords(new Rect(r.xMax - num3, r.y, num3, r.height), DiscTex, RightHalf);
				GUI.color = color;
				Widgets.DrawBoxSolid(new Rect(r.x + num3, r.y, r.width - r.height, r.height), c);
			}
			else
			{
				float num4 = r.width * 0.5f;
				GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, r.width, num4), DiscTex, TopHalf);
				GUI.DrawTextureWithTexCoords(new Rect(r.x, r.yMax - num4, r.width, num4), DiscTex, BottomHalf);
				GUI.color = color;
				Widgets.DrawBoxSolid(new Rect(r.x, r.y + num4, r.width, r.height - r.width), c);
			}
		}
	}

	public static void Dot(Rect r, Color c)
	{
		if (RcbGuard.Finite(r) && !(DiscTex == null))
		{
			Color color = GUI.color;
			GUI.color = c;
			GUI.DrawTexture(r, DiscTex);
			GUI.color = color;
		}
	}

	public static void LeftBand(Rect band, Color c, float radius = 12f)
	{
		if (!RcbGuard.Finite(band) || band.width < 1f || band.height < 2f)
		{
			return;
		}
		GUI.BeginClip(band);
		try
		{
			Atlas(new Rect(0f, 0f, radius * 2f, band.height), c, CardTex);
		}
		finally
		{
			GUI.EndClip();
		}
	}

	public static void Meter(Rect r, float frac, Color fill, Color track)
	{
		if (!RcbGuard.Finite(r) || r.height < 1f)
		{
			return;
		}
		frac = (RcbGuard.Finite(frac) ? Mathf.Clamp01(frac) : 0f);
		float num = Mathf.Round(r.width * frac);
		if (r.height <= 6f)
		{
			Widgets.DrawBoxSolid(r, track);
			if (num >= 2f)
			{
				Widgets.DrawBoxSolid(new Rect(r.x, r.y, num, r.height), fill);
			}
		}
		else
		{
			Pill(r, track);
			if (num >= 2f)
			{
				Pill(new Rect(r.x, r.y, num, r.height), fill);
			}
		}
	}

	public static void Elevate(Rect r, Rect clamp, float spread = 16f, float alpha = 0.5f)
	{
		if (Event.current.type == EventType.Repaint && RcbGuard.Finite(r))
		{
			Rect rect = new Rect(r.x - spread, r.y - spread + spread * 0.42f, r.width + spread * 2f, r.height + spread * 2f);
			float num = Mathf.Max(rect.x, clamp.x);
			float num2 = Mathf.Min(rect.xMax, clamp.xMax);
			float num3 = Mathf.Max(rect.y, clamp.y);
			float num4 = Mathf.Min(rect.yMax, clamp.yMax);
			if (!(num2 - num < 2f) && !(num4 - num3 < 2f) && !(num > rect.x + 0.5f) && !(num3 > rect.y + 0.5f) && !(num2 < rect.xMax - 0.5f) && !(num4 < rect.yMax - 0.5f))
			{
				Atlas(new Rect(num, num3, num2 - num, num4 - num3), new Color(0f, 0f, 0f, alpha), ShadowTex);
			}
		}
	}

	public static void TopHighlight(Rect r, float radius = 18f, float alpha = 0.06f)
	{
		if (!(r.width <= radius * 2f))
		{
			Widgets.DrawBoxSolid(new Rect(r.x + radius, r.y, r.width - radius * 2f, 1f), new Color(1f, 1f, 1f, alpha));
		}
	}

	public static void InnerShade(Rect r, float radius = 14f, float alpha = 0.28f)
	{
		if (!(r.width <= radius * 2f))
		{
			Widgets.DrawBoxSolid(new Rect(r.x + radius, r.y, r.width - radius * 2f, 2f), new Color(0f, 0f, 0f, alpha));
		}
	}

	public static void Well(Rect r, float alpha = 0.3f)
	{
		Small(r, new Color(0f, 0f, 0f, alpha));
	}

	public static void Ring(Rect r, Color c, Color innerFill, float w = 2f)
	{
		if (RcbGuard.Finite(r) && !(r.width < w * 3f) && !(r.height < w * 3f))
		{
			Card(r, c);
			Card(r.ContractedBy(w), innerFill);
		}
	}

	private static Texture2D NewTex(int n)
	{
		Texture2D texture2D = new Texture2D(n, n, TextureFormat.RGBA32, mipChain: false);
		texture2D.filterMode = FilterMode.Bilinear;
		texture2D.wrapMode = TextureWrapMode.Clamp;
		texture2D.hideFlags = HideFlags.HideAndDontSave;
		texture2D.name = "RCB_Spatial_" + n;
		return texture2D;
	}

	private static Color32 White(float a)
	{
		return new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
	}

	private static Texture2D MakeRounded(int r)
	{
		int num = r * 4;
		Texture2D texture2D = NewTex(num);
		Color32[] array = new Color32[num * num];
		float num2 = (float)num * 0.5f;
		float num3 = num2 - (float)r;
		for (int i = 0; i < num; i++)
		{
			float num4 = Mathf.Max(Mathf.Abs((float)i + 0.5f - num2) - num3, 0f);
			for (int j = 0; j < num; j++)
			{
				float num5 = Mathf.Max(Mathf.Abs((float)j + 0.5f - num2) - num3, 0f);
				array[i * num + j] = White(0.5f - (Mathf.Sqrt(num5 * num5 + num4 * num4) - (float)r));
			}
		}
		texture2D.SetPixels32(array);
		texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		return texture2D;
	}

	private static Texture2D MakeShadow(int r, int blur)
	{
		int num = r + blur;
		int num2 = num * 4;
		Texture2D texture2D = NewTex(num2);
		Color32[] array = new Color32[num2 * num2];
		float num3 = (float)num2 * 0.5f;
		float num4 = num3 - (float)blur - (float)r;
		for (int i = 0; i < num2; i++)
		{
			float num5 = Mathf.Max(Mathf.Abs((float)i + 0.5f - num3) - num4, 0f);
			for (int j = 0; j < num2; j++)
			{
				float num6 = Mathf.Max(Mathf.Abs((float)j + 0.5f - num3) - num4, 0f);
				float num7 = Mathf.Sqrt(num6 * num6 + num5 * num5) - (float)r;
				float num8 = ((num7 <= 0f) ? 1f : (1f - Mathf.Clamp01(num7 / (float)blur)));
				array[i * num2 + j] = White(num8 * num8);
			}
		}
		texture2D.SetPixels32(array);
		texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		return texture2D;
	}

	private static Texture2D MakeNotch(int n, bool left)
	{
		Texture2D texture2D = NewTex(n);
		Color32[] array = new Color32[n * n];
		for (int i = 0; i < n; i++)
		{
			for (int j = 0; j < n; j++)
			{
				float num = (left ? ((float)j + 0.5f) : ((float)n - ((float)j + 0.5f)));
				float num2 = (float)i + 0.5f;
				array[i * n + j] = White(Mathf.Sqrt(num * num + num2 * num2) - (float)n + 0.5f);
			}
		}
		texture2D.SetPixels32(array);
		texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		return texture2D;
	}

	private static Texture2D MakeDisc(int n)
	{
		Texture2D texture2D = NewTex(n);
		Color32[] array = new Color32[n * n];
		float num = (float)n * 0.5f;
		for (int i = 0; i < n; i++)
		{
			for (int j = 0; j < n; j++)
			{
				float num2 = (float)j + 0.5f - num;
				float num3 = (float)i + 0.5f - num;
				array[i * n + j] = White(0.5f - (Mathf.Sqrt(num2 * num2 + num3 * num3) - (num - 0.5f)));
			}
		}
		texture2D.SetPixels32(array);
		texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		return texture2D;
	}
}
