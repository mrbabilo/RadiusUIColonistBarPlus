using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBar), "DrawColonistMouseAttachment")]
internal static class Patch_DragVisual_Card
{
	private static bool Prefix(ColonistBar __instance, int index, Vector2 dragStartPos, int entryGroup)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled || !settings.partyPanel)
		{
			return true;
		}
		Pawn pawn = null;
		int num = 0;
		List<ColonistBar.Entry> entries = __instance.Entries;
		for (int i = 0; i < entries.Count; i++)
		{
			ColonistBar.Entry entry = entries[i];
			if (entry.group == entryGroup && entry.pawn != null)
			{
				if (num == index)
				{
					pawn = entry.pawn;
					break;
				}
				num++;
			}
		}
		if (pawn == null || !ReorderBounds.TryGet(pawn, out var bounds))
		{
			return true;
		}
		Vector2 vector = Event.current.mousePosition - dragStartPos;
		Rect card = new Rect(bounds.x + vector.x, bounds.y + vector.y, bounds.width, bounds.height);
		DrawDragCard(card, pawn, settings);
		return false;
	}

	private static void DrawDragCard(Rect card, Pawn p, RadiusColonistBarSettings s)
	{
		float num = 1f;
		try
		{
			num = ((Find.ColonistBar != null) ? Find.ColonistBar.Scale : 1f);
		}
		catch
		{
		}
		float num2 = Mathf.Max(3f, 4f * num);
		float num3 = Mathf.Max(8f, card.height - num2);
		Rect position = new Rect(card.x + num2, card.y, num3, num3);
		Color color = GUI.color;
		GUI.color = new Color(0f, 0f, 0f, 0.35f);
		GUI.DrawTexture(new Rect(card.x + 3f, card.y + 4f, card.width, card.height), BaseContent.WhiteTex);
		GUI.color = CardPalette.DragBg(s);
		GUI.DrawTexture(card, BaseContent.WhiteTex);
		BarSquads get = BarSquads.Get;
		int num4 = get?.SquadIndexOf(p) ?? (-1);
		GUI.color = ((num4 >= 0) ? get.SquadColor(num4) : CardPalette.DragBorder(s));
		Widgets.DrawBox(card, Mathf.Max(2, Mathf.RoundToInt(2f * num)));
		GUI.color = color;
		try
		{
			Texture portraitTexture = LiveViewManager.GetPortraitTexture(p, new Vector2(num3 * LiveViewManager.PortraitK, num3 * LiveViewManager.PortraitK));
			Texture texture = (s.useLiveCameras ? LiveViewManager.MaybeReplaceWithLiveView(portraitTexture, p) : portraitTexture);
			if (texture != null)
			{
				GUI.DrawTexture(position, texture);
			}
		}
		catch
		{
		}
		float num5 = position.xMax + 5f;
		Rect r = new Rect(num5, card.y, Mathf.Max(10f, card.xMax - num5 - 5f), card.height);
		RcbPalette.MicroLabel(r, p.LabelShortCap, RcbPalette.Stat, TextAnchor.MiddleLeft, shadow: false);
		GUI.color = color;
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Small;
	}
}
