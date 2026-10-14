using System;
using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

internal class LoadoutEditor
{
	private int editKit = -1;

	public bool PreviewOnDoll;

	public Thing HoverPreviewThing;

	public ThingDef HoverPreviewDef;

	private KitSlot? focusSlot;

	private bool mapMode = true;

	private string search = "";

	private Vector2 kitScroll;

	private Vector2 reqScroll;

	private Vector2 addScroll;

	private static List<ThingDef> _allItems;

	public CCKit CurrentKit
	{
		get
		{
			CCKits get = CCKits.Get;
			return (get != null && editKit >= 0 && editKit < get.kits.Count) ? get.kits[editKit] : null;
		}
	}

	public void EditKit(int index)
	{
		editKit = index;
	}

	public void ClearHover()
	{
		HoverPreviewThing = null;
		HoverPreviewDef = null;
	}

	public void Draw(Rect r, CCContext ctx)
	{
		CCKits get = CCKits.Get;
		List<Pawn> actingSet = ctx.ActingSet;
		Pawn focus = ctx.focus;
		CCKit kit = ((get != null && editKit >= 0 && editKit < get.kits.Count) ? get.kits[editKit] : null);
		float width = Mathf.Min(370f, r.width * 0.44f);
		Rect left = new Rect(r.x, r.y, width, r.height);
		Rect r2 = new Rect(left.xMax + 12f, r.y, r.xMax - (left.xMax + 12f), r.height);
		DrawSheet(left, kit, focus);
		DrawManage(r2, get, actingSet, ctx, focus, kit);
	}

	private void DrawSheet(Rect left, CCKit kit, Pawn pawn)
	{
		CCWidgets.Panel(left);
		Rect rect = left.ContractedBy(8f);
		CCWidgets.Label(new Rect(rect.x, rect.y, rect.width - 30f, 18f), (kit != null) ? kit.name : "No kit selected", (kit != null) ? CardPalette.Accent : CCWidgets.DimText, GameFont.Tiny);
		if (kit != null)
		{
			Texture2D texture2D = (PreviewOnDoll ? RCBTextures.EyeOpen : RCBTextures.EyeClosed);
			Rect rect2 = new Rect(rect.xMax - 24f, rect.y - 2f, 22f, 20f);
			if (Mouse.IsOver(rect2))
			{
				TooltipHandler.TipRegion(rect2, PreviewOnDoll ? "Hide kit preview on the paperdoll" : "Preview this kit on the paperdoll");
			}
			if (texture2D != null)
			{
				if (Widgets.ButtonImage(rect2, texture2D, PreviewOnDoll ? CardPalette.Accent : CCWidgets.DimText))
				{
					PreviewOnDoll = !PreviewOnDoll;
				}
			}
			else if (CCWidgets.Button(rect2, PreviewOnDoll ? "on" : "off"))
			{
				PreviewOnDoll = !PreviewOnDoll;
			}
		}
		float num = rect.y + 24f;
		float num2;
		if (kit == null)
		{
			CCWidgets.Label(new Rect(rect.x, num, rect.width, 18f), "Select or create a kit on the right to edit one.", CCWidgets.DimText, GameFont.Tiny);
			num2 = num + 22f;
		}
		else
		{
			float num3 = 44f;
			float num4 = 6f;
			KitSlot[] array = new KitSlot[6]
			{
				KitSlot.Weapon,
				KitSlot.Head,
				KitSlot.Torso,
				KitSlot.Legs,
				KitSlot.Feet,
				KitSlot.Belt
			};
			float num5 = rect.x;
			for (int i = 0; i < array.Length; i++)
			{
				DrawSlot(new Rect(num5, num, num3, num3), kit, array[i]);
				num5 += num3 + num4;
			}
			num2 = num + num3 + 10f;
		}
		DrawGearPanel(new Rect(rect.x, num2, rect.width, rect.yMax - num2), kit, pawn);
	}

	private void DrawGearPanel(Rect r, CCKit kit, Pawn pawn)
	{
		ModeChip(new Rect(r.x, r.y, 66f, 18f), "On map", map: true, enabled: true);
		ModeChip(new Rect(r.x + 70f, r.y, 66f, 18f), "Catalog", map: false, enabled: true);
		string text = (focusSlot.HasValue ? KitSlots.Label(focusSlot.Value).ToLowerInvariant() : "all slots");
		CCWidgets.Label(new Rect(r.x + 144f, r.y, r.width - 196f, 18f), text, CCWidgets.DimText, GameFont.Tiny, TextAnchor.MiddleRight);
		if (focusSlot.HasValue && CCWidgets.Button(new Rect(r.xMax - 46f, r.y, 46f, 18f), "All"))
		{
			focusSlot = null;
		}
		search = Widgets.TextField(new Rect(r.x, r.y + 24f, r.width, 24f), search);
		Rect rect = new Rect(r.x, r.y + 52f, r.width, r.yMax - (r.y + 52f));
		if (mapMode)
		{
			DrawMapList(rect, pawn);
			return;
		}
		List<ThingDef> list = AllItems();
		string text2 = search?.Trim().ToLowerInvariant() ?? "";
		List<ThingDef> list2 = new List<ThingDef>();
		for (int i = 0; i < list.Count; i++)
		{
			ThingDef thingDef = list[i];
			if ((!focusSlot.HasValue || KitSlots.SlotOf(thingDef) == focusSlot.Value) && (text2.Length <= 0 || (thingDef.label ?? thingDef.defName).ToLowerInvariant().Contains(text2)))
			{
				list2.Add(thingDef);
			}
		}
		Rect viewRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max((float)list2.Count * 24f + 2f, rect.height));
		RcbPalette.BeginScroll(rect, ref addScroll, viewRect);
		try
		{
			float num = 0f;
			for (int j = 0; j < list2.Count; j++)
			{
				if (!RcbPalette.RowVisible(num, 24f, addScroll, rect.height))
				{
					num += 24f;
					continue;
				}
				ThingDef thingDef2 = list2[j];
				bool flag = kit != null && (thingDef2.IsWeapon ? (kit.weapon == thingDef2.defName) : kit.apparel.Contains(thingDef2.defName));
				Rect rect2 = new Rect(0f, num, viewRect.width, 22f);
				if (Mouse.IsOver(rect2) && thingDef2.IsApparel)
				{
					HoverPreviewDef = thingDef2;
				}
				if (flag)
				{
					Widgets.DrawBoxSolid(rect2, CCWidgets.RowSel);
				}
				else if (Mouse.IsOver(rect2))
				{
					Widgets.DrawBoxSolid(rect2, CCWidgets.RowHover);
				}
				if (thingDef2.uiIcon != null)
				{
					GUI.color = thingDef2.uiIconColor;
					GUI.DrawTexture(new Rect(rect2.x + 3f, rect2.y + 2f, 18f, 18f), thingDef2.uiIcon);
					GUI.color = Color.white;
				}
				CCWidgets.Label(new Rect(rect2.x + 24f, rect2.y, rect2.width - 152f, rect2.height), thingDef2.LabelCap, flag ? CardPalette.StatText : RcbPalette.TextBody);
				int num2 = CountOnMaps(thingDef2);
				bool flag2 = false;
				if (num2 > 0)
				{
					CCWidgets.Label(new Rect(rect2.xMax - 126f, rect2.y, 36f, rect2.height), "x" + num2, CCWidgets.DimText, GameFont.Tiny, TextAnchor.MiddleRight);
					if (pawn != null)
					{
						Rect rect3 = new Rect(rect2.xMax - 86f, rect2.y + 2f, 62f, 18f);
						flag2 = Mouse.IsOver(rect3);
						if (flag2)
						{
							TooltipHandler.TipRegion(rect3, "Order " + pawn.LabelShortCap + " to " + (thingDef2.IsWeapon ? "equip" : "wear") + " the nearest available one");
						}
						if (CCWidgets.Button(rect3, thingDef2.IsWeapon ? "Equip" : "Wear"))
						{
							OrderGear(pawn, thingDef2);
						}
					}
				}
				if (flag)
				{
					CCWidgets.Label(new Rect(rect2.xMax - 18f, rect2.y, 16f, rect2.height), "✓", CardPalette.Accent, GameFont.Small, TextAnchor.MiddleCenter);
				}
				if (Widgets.ButtonInvisible(rect2) && !flag2 && kit != null)
				{
					Toggle(kit, thingDef2, flag);
				}
				num += 24f;
			}
		}
		finally
		{
			RcbPalette.EndScroll();
		}
	}

	private void ModeChip(Rect cr, string label, bool map, bool enabled)
	{
		bool flag = mapMode == map;
		Widgets.DrawBoxSolid(cr, flag ? RcbPalette.Lift : RcbPalette.BGD);
		CCWidgets.Label(cr, label, (!enabled) ? CCWidgets.DimText : (flag ? CardPalette.Accent : CCWidgets.DimText), GameFont.Tiny, TextAnchor.MiddleCenter);
		if (enabled && Widgets.ButtonInvisible(cr))
		{
			mapMode = map;
		}
	}

	private void DrawMapList(Rect listRect, Pawn pawn)
	{
		List<Thing> list = new List<Thing>();
		string q = search?.Trim().ToLowerInvariant() ?? "";
		try
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				AddMapThings(list, maps[i].listerThings.ThingsInGroup(ThingRequestGroup.Apparel), q, weaponPass: false);
				AddMapThings(list, maps[i].listerThings.ThingsInGroup(ThingRequestGroup.Weapon), q, weaponPass: true);
			}
		}
		catch
		{
		}
		list.Sort((Thing a, Thing b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase));
		Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, Mathf.Max((float)list.Count * 24f + 2f, listRect.height));
		RcbPalette.BeginScroll(listRect, ref addScroll, viewRect);
		try
		{
			float num = 0f;
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				if (!RcbPalette.RowVisible(num, 24f, addScroll, listRect.height))
				{
					num += 24f;
					continue;
				}
				Thing thing = list[num2];
				bool flag = thing.def.IsWeapon && !thing.def.IsApparel;
				Rect rect = new Rect(0f, num, viewRect.width, 22f);
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawBoxSolid(rect, CCWidgets.RowHover);
					if (!flag)
					{
						HoverPreviewThing = thing;
					}
				}
				Rect rect2 = new Rect(rect.x + 2f, rect.y + 1f, 20f, 20f);
				try
				{
					Widgets.ThingIcon(rect2, thing);
				}
				catch
				{
				}
				bool flag2 = false;
				try
				{
					flag2 = thing.TryGetComp<CompForbiddable>()?.Forbidden ?? false;
				}
				catch
				{
				}
				if (flag2)
				{
					CCWidgets.ForbiddenX(rect2);
				}
				CCWidgets.Label(new Rect(rect.x + 26f, rect.y, rect.width - 96f, rect.height), thing.LabelCap, RcbPalette.TextBody);
				bool flag3 = false;
				if (pawn != null)
				{
					Rect rect3 = new Rect(rect.xMax - 66f, rect.y + 2f, 62f, 18f);
					flag3 = Mouse.IsOver(rect3);
					string text = CCOrders.Blocker(pawn, thing, flag);
					if (flag3)
					{
						TooltipHandler.TipRegion(rect3, (text == null) ? ("Order " + pawn.LabelShortCap + " to " + (flag ? "equip" : "wear") + " this") : text.CapitalizeFirst());
					}
					if (CCWidgets.Button(rect3, flag ? "Equip" : "Wear", text == null) && text == null)
					{
						CCOrders.TryOrder(pawn, flag ? JobDefOf.Equip : JobDefOf.Wear, thing);
					}
				}
				if (Widgets.ButtonInvisible(rect) && !flag3)
				{
					OpenMapRowMenu(thing, pawn, flag);
				}
				num += 24f;
			}
			if (list.Count == 0)
			{
				CCWidgets.Label(new Rect(0f, 2f, viewRect.width, 22f), "No matching gear on the map.", CCWidgets.DimText);
			}
		}
		finally
		{
			RcbPalette.EndScroll();
		}
	}

	private void AddMapThings(List<Thing> res, List<Thing> src, string q, bool weaponPass)
	{
		if (src == null)
		{
			return;
		}
		for (int i = 0; i < src.Count; i++)
		{
			Thing thing = src[i];
			if (thing?.def != null && thing.Spawned && (!weaponPass || !thing.def.IsApparel) && (!weaponPass || thing.def.stackLimit <= 1) && (!focusSlot.HasValue || KitSlots.SlotOf(thing.def) == focusSlot.Value) && (q.Length <= 0 || thing.LabelCap.ToString().ToLowerInvariant().Contains(q)))
			{
				res.Add(thing);
			}
		}
	}

	private void OpenMapRowMenu(Thing t, Pawn pawn, bool weapon)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		string text = ((pawn != null) ? pawn.LabelShortCap : "pawn");
		string text2 = CCOrders.Blocker(pawn, t, weapon);
		string text3 = (weapon ? "Equip on " : "Wear on ");
		list.Add((text2 == null) ? new FloatMenuOption(text3 + text, delegate
		{
			CCOrders.TryOrder(pawn, weapon ? JobDefOf.Equip : JobDefOf.Wear, t);
		}) : new FloatMenuOption(text3 + text + " (" + text2 + ")", null));
		if (weapon)
		{
			JobDef ss = SidearmsBridge.EquipSecondaryJob;
			if (ss != null)
			{
				list.Add((text2 == null) ? new FloatMenuOption("Carry as sidearm - " + text, delegate
				{
					CCOrders.TryOrder(pawn, ss, t);
				}) : new FloatMenuOption("Carry as sidearm - " + text + " (" + text2 + ")", null));
			}
		}
		list.Add(new FloatMenuOption("Jump to item", delegate
		{
			try
			{
				CameraJumper.TryJump(t);
			}
			catch
			{
			}
		}));
		Find.WindowStack.Add(new FloatMenu(list));
	}

	private static void OrderGear(Pawn p, ThingDef d)
	{
		try
		{
			if (p == null || !p.Spawned || p.Dead || p.Downed || p.Map == null)
			{
				Messages.Message("No available pawn for that order.", MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			if (d.IsWeapon && p.WorkTagIsDisabled(WorkTags.Violent))
			{
				Messages.Message(p.LabelShortCap + " is incapable of violence.", MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			Thing thing = FindItem(p, d);
			if (thing == null)
			{
				Messages.Message("No reachable " + d.label + " for " + p.LabelShortCap + ".", MessageTypeDefOf.RejectInput, historical: false);
			}
			else
			{
				p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(d.IsWeapon ? JobDefOf.Equip : JobDefOf.Wear, thing), JobTag.Misc);
			}
		}
		catch
		{
		}
	}

	private static void Toggle(CCKit kit, ThingDef d, bool inKit)
	{
		if (d.IsWeapon)
		{
			kit.weapon = (inKit ? null : d.defName);
		}
		else if (inKit)
		{
			kit.apparel.Remove(d.defName);
		}
		else if (!kit.apparel.Contains(d.defName))
		{
			kit.apparel.Add(d.defName);
		}
	}

	private static List<ThingDef> AllItems()
	{
		if (_allItems != null)
		{
			return _allItems;
		}
		_allItems = new List<ThingDef>();
		foreach (ThingDef item in DefDatabase<ThingDef>.AllDefsListForReading)
		{
			if ((item.IsApparel || item.IsWeapon) && item.PlayerAcquirable && !item.destroyOnDrop)
			{
				_allItems.Add(item);
			}
		}
		_allItems.Sort((ThingDef a, ThingDef b) => string.Compare(a.label ?? a.defName, b.label ?? b.defName, StringComparison.OrdinalIgnoreCase));
		return _allItems;
	}

	private void DrawSlot(Rect r, CCKit kit, KitSlot slot)
	{
		List<string> list = KitSlots.ItemsIn(kit, slot);
		bool flag = Mouse.IsOver(r);
		bool flag2 = focusSlot == slot;
		Widgets.DrawBoxSolid(r, CardPalette.Track);
		if (flag)
		{
			Widgets.DrawBoxSolid(r, CCWidgets.RowHover);
		}
		if (list.Count > 0)
		{
			ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(list[0]);
			if (namedSilentFail?.uiIcon != null)
			{
				GUI.color = namedSilentFail.uiIconColor;
				GUI.DrawTexture(r.ContractedBy(4f), namedSilentFail.uiIcon);
				GUI.color = Color.white;
			}
			if (list.Count > 1)
			{
				CCWidgets.Label(new Rect(r.xMax - 16f, r.yMax - 14f, 15f, 13f), "+" + (list.Count - 1), CardPalette.Accent, GameFont.Tiny, TextAnchor.LowerRight);
			}
		}
		else
		{
			Texture2D texture2D = RCBTextures.SlotIcon(slot);
			if (texture2D != null)
			{
				GUI.color = RcbPalette.TextDim2;
				GUI.DrawTexture(r.ContractedBy(6f), texture2D);
				GUI.color = Color.white;
			}
			else
			{
				CCWidgets.Label(r, KitSlots.Label(slot), CCWidgets.DimText, GameFont.Tiny, TextAnchor.MiddleCenter);
			}
		}
		if (flag2)
		{
			Widgets.DrawBoxSolid(r, Palette.WithAlpha(RcbPalette.Lift, 0.55f));
		}
		if (flag)
		{
			if (list.Count > 0)
			{
				List<string> list2 = new List<string>();
				for (int i = 0; i < list.Count; i++)
				{
					ThingDef namedSilentFail2 = DefDatabase<ThingDef>.GetNamedSilentFail(list[i]);
					list2.Add((namedSilentFail2 != null) ? namedSilentFail2.LabelCap.ToString() : list[i]);
				}
				TooltipHandler.TipRegion(r, KitSlots.Label(slot) + ":\n" + string.Join("\n", list2) + "\n\n(click to filter the add-list below)");
			}
			else
			{
				TooltipHandler.TipRegion(r, KitSlots.Label(slot) + " (empty) - left-click to filter add-list, right-click to clear");
			}
		}
		if (!Widgets.ButtonInvisible(r))
		{
			return;
		}
		if (Event.current.button == 1)
		{
			if (slot != KitSlot.Weapon)
			{
				foreach (string item in KitSlots.ItemsIn(kit, slot))
				{
					kit.apparel.Remove(item);
				}
				return;
			}
			kit.weapon = null;
		}
		else
		{
			focusSlot = (flag2 ? ((KitSlot?)null) : new KitSlot?(slot));
		}
	}

	private void DrawManage(Rect r, CCKits store, List<Pawn> set, CCContext ctx, Pawn first, CCKit kit)
	{
		float height = Mathf.Min(r.height * 0.32f, 150f);
		float y = CCWidgets.SectionHeader(new Rect(r.x, r.y, r.width, 22f), "Kits");
		Rect outRect = new Rect(r.x, y, r.width, height);
		int num = store?.kits.Count ?? 0;
		Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max((float)num * 26f + 2f, outRect.height));
		RcbPalette.BeginScroll(outRect, ref kitScroll, viewRect);
		try
		{
			float num2 = 0f;
			for (int i = 0; i < num; i++)
			{
				if (!RcbPalette.RowVisible(num2, 24f, kitScroll, outRect.height))
				{
					num2 += 26f;
					continue;
				}
				Rect rect = new Rect(0f, num2, viewRect.width, 24f);
				if (i == editKit)
				{
					Widgets.DrawBoxSolid(rect, CCWidgets.RowSel);
				}
				else if (Mouse.IsOver(rect))
				{
					Widgets.DrawBoxSolid(rect, CCWidgets.RowHover);
				}
				CCWidgets.Label(new Rect(rect.x + 6f, rect.y, rect.width - 10f, rect.height), store.kits[i].name, CardPalette.StatText);
				if (Widgets.ButtonInvisible(rect))
				{
					editKit = i;
				}
				num2 += 26f;
			}
		}
		finally
		{
			RcbPalette.EndScroll();
		}
		float num3 = outRect.yMax + 6f;
		if (store != null)
		{
			if (CCWidgets.Button(new Rect(r.x, num3, 120f, 24f), "New empty kit"))
			{
				store.kits.Add(new CCKit
				{
					name = "Kit " + (store.kits.Count + 1)
				});
				editKit = store.kits.Count - 1;
			}
			if (first != null && CCWidgets.Button(new Rect(r.x + 126f, num3, 150f, 24f), "New from " + first.LabelShortCap))
			{
				store.kits.Add(Snapshot(first));
				editKit = store.kits.Count - 1;
			}
		}
		num3 += 30f;
		if (kit == null)
		{
			CCWidgets.Label(new Rect(r.x, num3, r.width, 22f), "Select a kit to edit + see requirements.", CCWidgets.DimText, GameFont.Tiny);
			return;
		}
		if (CCWidgets.Button(new Rect(r.x, num3, 90f, 24f), "Rename"))
		{
			Find.WindowStack.Add(new Dialog_RenameSquad(kit.name, delegate(string nm)
			{
				if (!string.IsNullOrEmpty(nm))
				{
					kit.name = nm;
				}
			}));
		}
		if (CCWidgets.Button(new Rect(r.x + 96f, num3, 160f, 24f), "Apply to " + set.Count + " pawn" + ((set.Count == 1) ? "" : "s")))
		{
			foreach (Pawn item in set)
			{
				ApplyKit(kit, item);
			}
		}
		if (CCWidgets.Button(new Rect(r.xMax - 70f, num3, 70f, 24f), "Delete"))
		{
			store.kits.RemoveAt(editKit);
			editKit = -1;
			return;
		}
		num3 += 30f;
		if (CCWidgets.Button(new Rect(r.x, num3, 200f, 24f), "Set allowed outfit from kit"))
		{
			KitToOutfit(kit, set);
		}
		num3 += 30f;
		float num4 = CCWidgets.SectionHeader(new Rect(r.x, num3, r.width - 104f, 22f), "Gear up - availability + materials");
		if (CCWidgets.Button(new Rect(r.xMax - 100f, num3, 100f, 20f), "Queue bills"))
		{
			QueueBills(kit);
		}
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(kit.weapon))
		{
			list.Add(kit.weapon);
		}
		list.AddRange(kit.apparel);
		Rect outRect2 = new Rect(r.x, num4, r.width, r.yMax - num4);
		Rect viewRect2 = new Rect(0f, 0f, outRect2.width - 16f, Mathf.Max((float)list.Count * 34f + 4f, outRect2.height));
		RcbPalette.BeginScroll(outRect2, ref reqScroll, viewRect2);
		try
		{
			float num5 = 0f;
			for (int num6 = 0; num6 < list.Count; num6++)
			{
				if (RcbPalette.RowVisible(num5, 34f, reqScroll, outRect2.height))
				{
					DrawReqRow(new Rect(0f, num5, viewRect2.width, 34f), list[num6]);
				}
				num5 += 34f;
			}
			if (list.Count == 0)
			{
				CCWidgets.Label(new Rect(0f, 2f, viewRect2.width, 22f), "Empty kit - click the paperdoll slots to add gear.", CCWidgets.DimText);
			}
		}
		finally
		{
			RcbPalette.EndScroll();
		}
	}

	private void DrawReqRow(Rect r, string defName)
	{
		if (Mouse.IsOver(r))
		{
			Widgets.DrawBoxSolid(r, CCWidgets.RowHover);
		}
		ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
		if (namedSilentFail == null)
		{
			CCWidgets.Label(r, defName, CCWidgets.DimText, GameFont.Tiny);
			return;
		}
		if (namedSilentFail.uiIcon != null)
		{
			GUI.color = namedSilentFail.uiIconColor;
			GUI.DrawTexture(new Rect(r.x + 2f, r.y + (r.height - 26f) / 2f, 26f, 26f), namedSilentFail.uiIcon);
			GUI.color = Color.white;
		}
		CCWidgets.Label(new Rect(r.x + 32f, r.y, r.width * 0.4f - 32f, r.height), namedSilentFail.LabelCap, CardPalette.StatText);
		float num = r.x + r.width * 0.42f;
		int num2 = CountOnMaps(namedSilentFail);
		if (num2 > 0)
		{
			CCWidgets.Label(new Rect(num, r.y, r.width - (num - r.x), r.height), "✓ available x" + num2, CardPalette.Good, GameFont.Tiny);
			return;
		}
		float x = num;
		bool flag = false;
		if (namedSilentFail.costList != null)
		{
			for (int i = 0; i < namedSilentFail.costList.Count; i++)
			{
				DrawMat(r, ref x, namedSilentFail.costList[i].thingDef, namedSilentFail.costList[i].count);
				flag = true;
			}
		}
		if (namedSilentFail.MadeFromStuff)
		{
			string text = ((namedSilentFail.stuffCategories != null && namedSilentFail.stuffCategories.Count > 0) ? namedSilentFail.stuffCategories[0].label : "material");
			CCWidgets.Label(new Rect(x, r.y, r.xMax - x, r.height), namedSilentFail.costStuffCount + " " + text, CCWidgets.DimText, GameFont.Tiny);
			flag = true;
		}
		if (!flag)
		{
			CCWidgets.Label(new Rect(num, r.y, r.width - (num - r.x), r.height), "buy / quest only", CardPalette.Warn, GameFont.Tiny);
		}
	}

	private void DrawMat(Rect area, ref float x, ThingDef mat, int need)
	{
		if (mat != null && !(x + 62f > area.xMax))
		{
			if (mat.uiIcon != null)
			{
				GUI.color = mat.uiIconColor;
				GUI.DrawTexture(new Rect(x, area.y + area.height / 2f - 8f, 16f, 16f), mat.uiIcon);
				GUI.color = Color.white;
			}
			int num = CountOnMaps(mat);
			if (Mouse.IsOver(new Rect(x, area.y, 60f, area.height)))
			{
				TooltipHandler.TipRegion(new Rect(x, area.y, 60f, area.height), string.Concat(mat.LabelCap + ": ", num.ToString(), " / ", need.ToString()));
			}
			CCWidgets.Label(new Rect(x + 18f, area.y, 42f, area.height), num + "/" + need, (num >= need) ? CardPalette.Good : CardPalette.Bad, GameFont.Tiny);
			x += 64f;
		}
	}

	private static string CountLabel(int n)
	{
		return n.ToString();
	}

	private static void KitToOutfit(CCKit kit, List<Pawn> targets)
	{
		try
		{
			OutfitDatabase outfitDatabase = Current.Game?.outfitDatabase;
			if (outfitDatabase == null || kit == null)
			{
				return;
			}
			ApparelPolicy apparelPolicy = null;
			foreach (ApparelPolicy allOutfit in outfitDatabase.AllOutfits)
			{
				if (allOutfit.label == kit.name)
				{
					apparelPolicy = allOutfit;
					break;
				}
			}
			if (apparelPolicy == null)
			{
				apparelPolicy = outfitDatabase.MakeNewOutfit();
				apparelPolicy.label = kit.name;
			}
			apparelPolicy.filter.SetDisallowAll();
			int num = 0;
			for (int i = 0; i < kit.apparel.Count; i++)
			{
				ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(kit.apparel[i]);
				if (namedSilentFail != null && namedSilentFail.IsApparel)
				{
					apparelPolicy.filter.SetAllow(namedSilentFail, allow: true);
					num++;
				}
			}
			int num2 = 0;
			if (targets != null)
			{
				foreach (Pawn target in targets)
				{
					try
					{
						if (target.outfits != null && !target.IsPrisonerOfColony && !target.IsSlaveOfColony)
						{
							target.outfits.CurrentApparelPolicy = apparelPolicy;
							num2++;
						}
					}
					catch
					{
					}
				}
			}
			Messages.Message("Outfit policy \"" + apparelPolicy.label + "\" set from kit (" + num + " apparel item" + ((num == 1) ? "" : "s") + ") for " + num2 + " pawn" + ((num2 == 1) ? "" : "s") + ".", MessageTypeDefOf.TaskCompletion, historical: false);
		}
		catch
		{
		}
	}

	private static int CountOnMaps(ThingDef def)
	{
		int num = 0;
		try
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				List<Thing> list = maps[i].listerThings.ThingsOfDef(def);
				for (int j = 0; j < list.Count; j++)
				{
					num += Mathf.Max(1, list[j].stackCount);
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private static void QueueBills(CCKit kit)
	{
		if (kit == null)
		{
			return;
		}
		Map currentMap = Find.CurrentMap;
		if (currentMap == null)
		{
			Messages.Message("No map to add bills to.", MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(kit.weapon))
		{
			list.Add(kit.weapon);
		}
		list.AddRange(kit.apparel);
		int num = 0;
		HashSet<string> hashSet = new HashSet<string>();
		for (int i = 0; i < list.Count; i++)
		{
			if (!hashSet.Add(list[i]))
			{
				continue;
			}
			ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(list[i]);
			if (namedSilentFail == null)
			{
				continue;
			}
			RecipeDef recipeDef = RecipeFor(namedSilentFail);
			if (recipeDef == null)
			{
				continue;
			}
			Thing thing = NearestBench(currentMap, recipeDef);
			if (thing is IBillGiver billGiver)
			{
				try
				{
					billGiver.BillStack.AddBill(recipeDef.MakeNewBill());
					num++;
				}
				catch
				{
				}
			}
		}
		Messages.Message((num > 0) ? ("Queued " + num + " crafting bill(s) for " + kit.name) : ("No craftable items or capable benches found for " + kit.name), (num > 0) ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.RejectInput, historical: false);
	}

	private static RecipeDef RecipeFor(ThingDef def)
	{
		List<RecipeDef> allDefsListForReading = DefDatabase<RecipeDef>.AllDefsListForReading;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			if (allDefsListForReading[i].ProducedThingDef == def)
			{
				return allDefsListForReading[i];
			}
		}
		return null;
	}

	private static Thing NearestBench(Map map, RecipeDef recipe)
	{
		Thing result = null;
		float num = float.MaxValue;
		IntVec3 center = map.Center;
		IEnumerable<ThingDef> allRecipeUsers = recipe.AllRecipeUsers;
		if (allRecipeUsers == null)
		{
			return null;
		}
		foreach (ThingDef item in allRecipeUsers)
		{
			List<Thing> list = map.listerThings.ThingsOfDef(item);
			for (int i = 0; i < list.Count; i++)
			{
				Thing thing = list[i];
				if (thing is IBillGiver && thing.Spawned)
				{
					float num2 = thing.Position.DistanceToSquared(center);
					if (num2 < num)
					{
						num = num2;
						result = thing;
					}
				}
			}
		}
		return result;
	}

	private static CCKit Snapshot(Pawn p)
	{
		CCKit cCKit = new CCKit
		{
			name = "Kit " + ((CCKits.Get?.kits.Count ?? 0) + 1)
		};
		try
		{
			cCKit.weapon = p.equipment?.Primary?.def?.defName;
			List<Apparel> list = p.apparel?.WornApparel;
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					cCKit.apparel.Add(list[i].def.defName);
				}
			}
		}
		catch
		{
		}
		return cCKit;
	}

	public static void ApplyKit(CCKit kit, Pawn p)
	{
		try
		{
			if (p == null || !p.Spawned || p.Map == null || p.Dead || p.Downed)
			{
				return;
			}
			bool first = true;
			if (!string.IsNullOrEmpty(kit.weapon))
			{
				ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(kit.weapon);
				Thing thing = ((namedSilentFail != null) ? FindItem(p, namedSilentFail) : null);
				if (thing != null)
				{
					Queue(p, JobMaker.MakeJob(JobDefOf.Equip, thing), ref first);
				}
			}
			for (int i = 0; i < kit.apparel.Count; i++)
			{
				ThingDef namedSilentFail2 = DefDatabase<ThingDef>.GetNamedSilentFail(kit.apparel[i]);
				Thing thing2 = ((namedSilentFail2 != null) ? FindItem(p, namedSilentFail2) : null);
				if (thing2 != null)
				{
					Queue(p, JobMaker.MakeJob(JobDefOf.Wear, thing2), ref first);
				}
			}
		}
		catch
		{
		}
	}

	private static void Queue(Pawn p, Job job, ref bool first)
	{
		if (first)
		{
			p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			first = false;
		}
		else
		{
			p.jobs.jobQueue.EnqueueLast(job);
		}
	}

	private static Thing FindItem(Pawn p, ThingDef def)
	{
		List<Thing> list = p.Map.listerThings.ThingsOfDef(def);
		Thing result = null;
		float num = float.MaxValue;
		for (int i = 0; i < list.Count; i++)
		{
			Thing thing = list[i];
			if (thing != null && !thing.IsForbidden(p) && p.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
			{
				float num2 = thing.Position.DistanceToSquared(p.Position);
				if (num2 < num)
				{
					num = num2;
					result = thing;
				}
			}
		}
		return result;
	}
}
