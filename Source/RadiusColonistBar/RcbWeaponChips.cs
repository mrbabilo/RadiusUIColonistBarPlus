using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbWeaponChips
{
	public const float Chip = 18f;

	public const float ChipSolo = 20f;

	public const float Gap = 4f;

	private static readonly Color Ink = RcbPalette.Ink;

	private static readonly Color FillHand = Color.Lerp(Ink, Color.white, 0.3f);

	private static readonly Color FillStow = Color.Lerp(Ink, Color.white, 0.16f);

	private static readonly Color FillMore = Color.Lerp(Ink, Color.white, 0.2f);

	private static readonly Color FillHover = Color.Lerp(Ink, Color.white, 0.42f);

	private static readonly Color GlyphHand = Color.white;

	private static readonly Color GlyphStow = new Color(1f, 1f, 1f, 0.72f);

	private static readonly List<Thing> tmp = new List<Thing>(4);

	private static int collectFrame = -1;

	private static int collectPawn = -1;

	private static int collectMode = -1;

	private static void Collect(Pawn p, RadiusColonistBarSettings s, List<Thing> into)
	{
		int num = p?.thingIDNumber ?? (-1);
		int num2 = s.weaponMode * 2 + (s.weaponPrimaryOnly ? 1 : 0);
		if (into == tmp && collectFrame == Time.frameCount && collectPawn == num && collectMode == num2)
		{
			return;
		}
		if (into == tmp)
		{
			collectFrame = Time.frameCount;
			collectPawn = num;
			collectMode = num2;
		}
		into.Clear();
		if (s.weaponMode == 0 || (s.weaponMode == 1 && !p.Drafted))
		{
			return;
		}
		ThingWithComps thingWithComps = null;
		try
		{
			thingWithComps = p.equipment?.Primary;
		}
		catch
		{
		}
		if (thingWithComps != null && thingWithComps.def != null && thingWithComps.def.IsWeapon && Patch_DrawColonist_Overlay.TryWeaponIcons(thingWithComps.def, out var colour, out var gray))
		{
			into.Add(thingWithComps);
		}
		if (s.weaponPrimaryOnly)
		{
			return;
		}
		List<Thing> weapons = PawnUiCache.Get(p).weapons;
		if (weapons == null)
		{
			return;
		}
		for (int i = 0; i < weapons.Count; i++)
		{
			Thing thing = weapons[i];
			if (thing != null && thing != thingWithComps && thing.def != null && Patch_DrawColonist_Overlay.TryWeaponIcons(thing.def, out gray, out colour))
			{
				into.Add(thing);
			}
		}
	}

	public static float Height(Pawn p, RadiusColonistBarSettings s, float u)
	{
		Collect(p, s, tmp);
		float num;
		switch (tmp.Count)
		{
		case 0:
			return 0f;
		default:
			num = 18f;
			break;
		case 1:
			num = 20f;
			break;
		}
		return num * u;
	}

	public static float Draw(float centreX, float top, Pawn p, RadiusColonistBarSettings s, float u, float maxW = 0f)
	{
		Collect(p, s, tmp);
		int count = tmp.Count;
		float num;
		switch (count)
		{
		case 0:
			return 0f;
		default:
			num = 18f;
			break;
		case 1:
			num = 20f;
			break;
		}
		float num2 = num * u;
		float num3 = 4f * u;
		int num4 = Mathf.Min(count, 2);
		int num5 = count - num4;
		int num6 = num4 + ((num5 > 0) ? 1 : 0);
		float num7 = (float)num6 * num2 + (float)(num6 - 1) * num3;
		if (maxW > 4f && num7 > maxW)
		{
			float num8 = maxW / num7;
			num2 *= num8;
			num3 *= num8;
			num7 = maxW;
		}
		float num9 = centreX - num7 * 0.5f;
		for (int i = 0; i < num4; i++)
		{
			Rect r = new Rect(num9 + (float)i * (num2 + num3), top, num2, num2);
			DrawChip(r, tmp[i], i == 0, u);
		}
		if (num5 > 0)
		{
			Rect r2 = new Rect(num9 + (float)num4 * (num2 + num3), top, num2, num2);
			DrawMore(r2, p, s, num5, u);
		}
		return num2;
	}

	private static void DrawChip(Rect r, Thing t, bool inHand, float u)
	{
		if (!RcbGuard.Finite(r) || t?.def == null)
		{
			return;
		}
		bool flag = Mouse.IsOver(r);
		RcbSpatial.ChipFast(r, flag ? FillHover : (inHand ? FillHand : FillStow));
		if (Patch_DrawColonist_Overlay.TryWeaponIcons(t.def, out var colour, out var gray))
		{
			Texture2D texture2D = (inHand ? colour : gray);
			if (texture2D != null)
			{
				Color color = GUI.color;
				GUI.color = (inHand ? GlyphHand : GlyphStow);
				GUI.DrawTexture(r.ContractedBy(Mathf.Max(1.5f, r.width * 0.14f)), texture2D, ScaleMode.ScaleToFit);
				GUI.color = color;
			}
		}
		if (flag)
		{
			TooltipHandler.TipRegion(r, t.LabelCap + (inHand ? ("\n" + "RCB_Weap_InHand".Translate()) : ("\n" + "RCB_Weap_Stowed".Translate())));
		}
	}

	private static void DrawMore(Rect r, Pawn p, RadiusColonistBarSettings s, int n, float u)
	{
		if (!RcbGuard.Finite(r))
		{
			return;
		}
		bool flag = Mouse.IsOver(r);
		RcbSpatial.ChipFast(r, flag ? FillHover : FillMore);
		RcbPalette.MicroLabel(r, SmallStrings.Plus(n), flag ? RcbPalette.Stat : RcbPalette.TextDim, TextAnchor.MiddleCenter, shadow: false);
		if (!flag)
		{
			return;
		}
		JobDef swap = null;
		try
		{
			swap = SidearmsBridge.EquipSecondaryJob;
		}
		catch
		{
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append((swap != null) ? "RCB_Weap_SwapTo".Translate() : "RCB_Weap_AlsoCarrying".Translate());
		for (int i = 2; i < tmp.Count; i++)
		{
			Thing thing = tmp[i];
			if (thing != null)
			{
				stringBuilder.Append("\n  ").Append(thing.LabelCap);
			}
		}
		TooltipHandler.TipRegion(r, stringBuilder.ToString());
		if (swap == null || !Widgets.ButtonInvisible(r))
		{
			return;
		}
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		for (int j = 2; j < tmp.Count; j++)
		{
			Thing t = tmp[j];
			if (t != null)
			{
				list.Add(new FloatMenuOption(t.LabelCap, delegate
				{
					CCOrders.TryOrder(p, swap, t);
				}));
			}
		}
		if (list.Count > 0)
		{
			Find.WindowStack.Add(new FloatMenu(list));
		}
	}
}
