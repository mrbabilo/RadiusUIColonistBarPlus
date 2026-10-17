using System;
using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): the order card, opened by right-clicking a squad tab or from the Squads module.
// It re-reads the squad state every frame and stays open after an order, so the new state shows at once. It can be
// dragged by any spot outside its buttons, and closes with the suite's close square.
public class SquadOrderCard : Window
{
	private const float Width = 380f;

	private const float Pad = 12f;

	private const float BtnH = 30f;

	private const float Gap = 4f;

	// Station slot pips (Colonist Bar Plus).
	private const float PipH = 24f;

	// The close square (26 px) and its gap to the header text.
	private const float CloseW = 30f;

	private readonly int index;

	private readonly Rect anchor;

	private readonly bool managed;

	private readonly BarSquads managedSquads;

	private readonly Action refresh;

	// The outside click that closes a card and the right-click on its tab come in the same frame: without this the
	// tab would reopen the card it just closed.
	private static int closedIndex = -1;

	private static int closedFrame = -1;

	private static bool closedManaged;

	private static SquadOrderCard open;

	public override Vector2 InitialSize => new Vector2(Width, ContentHeight());

	protected override float Margin => 0f;

	private SquadOrderCard(int index, Rect anchor, bool managed, BarSquads squads, Action refresh)
	{
		this.index = index;
		this.anchor = anchor;
		this.managed = managed;
		managedSquads = squads;
		this.refresh = refresh;
		doWindowBackground = false;
		drawShadow = false;
		doCloseX = false;
		draggable = true;
		closeOnClickedOutside = true;
		closeOnCancel = true;
		absorbInputAroundWindow = false;
		preventCameraMotion = false;
		onlyOneOfTypeAllowed = true;
		layer = WindowLayer.Super;
		soundAppear = null;
		soundClose = null;
	}

	internal static void Toggle(int index, Rect anchor)
	{
		if (open != null && open.IsOpen && open.index == index)
		{
			open.Close();
			return;
		}
		if (!closedManaged && closedIndex == index && closedFrame >= Time.frameCount - 1)
		{
			return;
		}
		Find.WindowStack.Add(new SquadOrderCard(index, anchor, managed: false, null, null));
	}

	internal static void OpenManaged(int index, BarSquads squads, Action refresh)
	{
		Vector2 mousePositionOnUIInverted = UI.MousePositionOnUIInverted;
		Find.WindowStack.Add(new SquadOrderCard(index, new Rect(mousePositionOnUIInverted.x, mousePositionOnUIInverted.y, 0f, 0f), managed: true, squads, refresh));
	}

	public override void PostOpen()
	{
		base.PostOpen();
		open = this;
	}

	public override void PostClose()
	{
		base.PostClose();
		closedIndex = index;
		closedFrame = Time.frameCount;
		closedManaged = managed;
		if (open == this)
		{
			open = null;
		}
	}

	protected override void SetInitialSizeAndPosition()
	{
		Vector2 initialSize = InitialSize;
		float x = Mathf.Clamp(anchor.x, 4f, (float)UI.screenWidth - initialSize.x - 4f);
		float y = Mathf.Clamp(anchor.yMax + 4f, 4f, (float)UI.screenHeight - initialSize.y - 4f);
		windowRect = new Rect(x, y, initialSize.x, initialSize.y).Rounded();
	}

	private float ContentHeight()
	{
		// Header 24 + state 20 + 2 rows of orders + stations (title 18, pips, buttons) + assault title 18 + assault row
		// + members title 18 + members row, plus the footer when managed.
		float num = Pad + 24f + 20f + 8f + (BtnH * 2f + Gap) + 10f + (18f + PipH + Gap + BtnH) + 10f + 18f + BtnH + 10f + 18f + BtnH + Pad;
		if (managed)
		{
			num += 8f + BtnH;
		}
		return num;
	}

	public override void DoWindowContents(Rect inRect)
	{
		BarSquads get = BarSquads.Get;
		// Orders act on the current map: the card closes when the world view opens.
		if (get == null || index < 0 || index >= get.names.Count || WorldRendererUtility.WorldSelected)
		{
			Close();
			return;
		}
		RcbSpatial.Rounded(inRect, Palette.WithAlpha(RcbPalette.Ink, 0.96f), 14f);
		SquadStatus.State state = SquadStatus.Fresh(index);
		bool flag = state.present > 0;
		float x = inRect.x + Pad;
		float num = inRect.width - Pad * 2f;
		float y = inRect.y + Pad;
		DrawHeader(new Rect(x, y, num - CloseW, 24f), get, state);
		if (UIKit.Flat.CloseXIn(inRect, Pad * 2f + 24f))
		{
			Close();
			return;
		}
		y += 24f;
		RcbFont.Label(new Rect(x, y, num, 20f), flag ? StateLine(state) : "RCB_Orders_NoMember".Translate().ToString(), bold: false, GameFont.Tiny, RcbPalette.TextDim);
		y += 28f;
		float num2 = (num - Gap) / 2f;
		// Shown on every order while nobody can take one.
		string why = "RCB_Orders_NoMember".Translate().ToString();
		bool flag2 = flag && state.drafted == state.present;
		if (Button(new Rect(x, y, num2, BtnH), IconSet.Action.Draft, (flag2 ? "RCB_Card_UndraftShort" : "RCB_Card_DraftShort").Translate(), Tip(SquadHotkeys.WithKey((flag2 ? "RCB_Orders_UndraftTip" : "RCB_Orders_DraftTip").Translate(), SquadHotkeys.Key.Draft), flag, why), flag, flag2, flag && !flag2 && state.drafted > 0))
		{
			if (flag2)
			{
				SquadOrders.Undraft(index);
			}
			else
			{
				SquadOrders.Draft(index);
			}
		}
		bool flag3 = flag && state.fireAtWill == state.present;
		if (Button(new Rect(x + num2 + Gap, y, num2, BtnH), IconSet.Alert.Threat, (flag3 ? "RCB_Card_HoldShort" : "RCB_Card_FireShort").Translate(), Tip(SquadHotkeys.WithKey("RCB_Orders_FireAtWillTip".Translate(), SquadHotkeys.Key.Fire), flag, why), flag, flag3, flag && !flag3 && state.fireAtWill > 0))
		{
			SquadOrders.ToggleFireAtWill(index);
		}
		y += BtnH + Gap;
		if (Button(new Rect(x, y, num2, BtnH), IconSet.Action.DefPos, "RCB_Card_RegroupShort".Translate(), Tip(SquadHotkeys.WithKey("RCB_Orders_RegroupTip".Translate(), SquadHotkeys.Key.Regroup), flag, why), flag, active: false))
		{
			SquadOrders.Regroup(index);
		}
		// Quick assignment (Colonist Bar Plus): downed members count, Able would leave them out.
		int count = SquadMembers.Spawned(Find.CurrentMap, index).Count;
		if (Button(new Rect(x + num2 + Gap, y, num2, BtnH), null, "RCB_Members_Select".Translate(count), Tip("RCB_Members_SelectTip".Translate(), count > 0, "RCB_Orders_NoMember".Translate()), count > 0, active: false))
		{
			SquadMembers.SelectAll(Find.CurrentMap, index);
		}
		y += BtnH + 10f;
		if (DrawStations(x, ref y, num, get, state))
		{
			return;
		}
		y += 10f;
		RcbPalette.MicroLabel(new Rect(x, y, num, 18f), "RCB_Card_AssaultTitle".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
		y += 18f;
		float num3 = (num - Gap * 2f) / 3f;
		AssaultButton(new Rect(x, y, num3, BtnH), AssaultMode.Strongest, "RCB_Card_Strongest", "RCB_Orders_AssaultStrongestTip", SquadHotkeys.Key.Strongest, state, flag);
		AssaultButton(new Rect(x + num3 + Gap, y, num3, BtnH), AssaultMode.Weakest, "RCB_Card_Weakest", "RCB_Orders_AssaultWeakestTip", SquadHotkeys.Key.Weakest, state, flag);
		AssaultButton(new Rect(x + (num3 + Gap) * 2f, y, num3, BtnH), AssaultMode.Fleeing, "RCB_Card_Fleeing", "RCB_Orders_AssaultFleeingTip", SquadHotkeys.Key.Fleeing, state, flag);
		y += BtnH + 10f;
		DrawMembers(x, ref y, num, get);
		if (managed && managedSquads != null)
		{
			y += 8f;
			DrawManageRow(new Rect(x, y, num, BtnH), managedSquads);
		}
	}

	// Battle stations (Colonist Bar Plus): slot pips, then Go / Save / Clear on the active slot. One Able call for the
	// whole section: it sorts and reads the bar, five calls a frame would add up. Returns true when the card closed.
	private bool DrawStations(float x, ref float y, float w, BarSquads squads, SquadStatus.State state)
	{
		Map currentMap = Find.CurrentMap;
		List<Pawn> list = SquadMembers.Able(currentMap, index);
		int num = squads.ActiveStation(index);
		RcbPalette.MicroLabel(new Rect(x, y, w, 18f), "RCB_Stations_Title".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
		y += 18f;
		float num2 = (w - Gap * 3f) / 4f;
		for (int i = 0; i < BarSquads.StationSlots; i++)
		{
			Rect r = new Rect(x + (num2 + Gap) * (float)i, y, num2, PipH);
			if (Mouse.IsOver(r))
			{
				SquadStations.Hover(index, i, list);
			}
			if (Pip(r, i, SquadStations.CountOnMap(list, squads, i, currentMap), list.Count, i == num))
			{
				squads.SetActiveStation(index, i);
			}
		}
		y += PipH + Gap;
		float num3 = (w - Gap * 2f) / 3f;
		Rect rect = new Rect(x, y, num3, BtnH);
		if (Mouse.IsOver(rect))
		{
			SquadStations.Hover(index, num, list);
		}
		bool flag = SquadStations.CountOnMap(list, squads, num, currentMap) > 0;
		string why = ((list.Count == 0) ? "RCB_Orders_NoMember".Translate() : "RCB_Stations_None".Translate(num + 1, squads.SquadName(index))).ToString();
		if (Button(rect, null, "RCB_Stations_Go".Translate(), Tip(SquadHotkeys.WithKey("RCB_Stations_GoTip".Translate(num + 1), SquadHotkeys.Key.Station), flag, why), flag, active: false))
		{
			SquadStations.Go(index);
		}
		if (Button(new Rect(x + num3 + Gap, y, num3, BtnH), null, "RCB_Stations_Save".Translate(), Tip("RCB_Stations_SaveTip".Translate(num + 1), list.Count > 0, "RCB_Orders_NoMember".Translate()), list.Count > 0, active: false))
		{
			SquadStations.Save(index);
		}
		bool flag2 = (state.stationMask & (1 << num)) != 0;
		if (Button(new Rect(x + (num3 + Gap) * 2f, y, num3, BtnH), null, "RCB_Stations_Clear".Translate(), Tip("RCB_Stations_ClearTip".Translate(num + 1), flag2, "RCB_Stations_ClearNone".Translate(num + 1)), flag2, active: false))
		{
			// The card sits above dialogs: left open, it would cover the confirmation and keep its input.
			Close();
			SquadStations.AskClear(index);
			return true;
		}
		y += BtnH;
		return false;
	}

	// Active slot: accent. Slot saved on this map for at least one able member: filled. Otherwise: faint outline.
	private static bool Pip(Rect r, int slot, int saved, int able, bool active)
	{
		bool flag = Mouse.IsOver(r);
		if (active || saved > 0)
		{
			RcbSpatial.Rounded(r, active ? RcbPalette.Accent : Color.Lerp(RcbPalette.Ink, Color.white, flag ? 0.2f : 0.12f));
		}
		else
		{
			RcbSpatial.Ring(r, Color.Lerp(RcbPalette.Ink, Color.white, flag ? 0.22f : 0.12f), RcbPalette.Ink, 1.5f);
		}
		Color col = (active ? Palette.Flat.Rail : ((saved > 0) ? RcbPalette.Stat : RcbPalette.TextDim2));
		RcbFont.Label(r, (slot + 1).ToString(), bold: true, GameFont.Small, col, TextAnchor.MiddleCenter);
		if (flag)
		{
			TooltipHandler.TipRegion(r, "RCB_Stations_PipTip".Translate(slot + 1, saved, able));
		}
		if (Widgets.ButtonInvisible(r))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			return true;
		}
		return false;
	}

	private void DrawHeader(Rect r, BarSquads squads, SquadStatus.State state)
	{
		RcbSpatial.Dot(new Rect(r.x, r.center.y - 5f, 10f, 10f), squads.SquadColor(index));
		string text = "RCB_Card_Present".Translate(state.present, state.total);
		float num = RcbFont.Width(text, bold: false, GameFont.Tiny) + 4f;
		RcbFont.Label(new Rect(r.x + 16f, r.y, r.width - 16f - num, r.height), RcbFont.Fit(squads.SquadName(index) ?? "", r.width - 16f - num, bold: true, GameFont.Small), bold: true, GameFont.Small, RcbPalette.Stat);
		RcbFont.Label(new Rect(r.xMax - num, r.y, num, r.height), text, bold: false, GameFont.Tiny, RcbPalette.TextDim, TextAnchor.MiddleRight);
	}

	private static string StateLine(SquadStatus.State s)
	{
		string text = ((s.drafted == s.present) ? "RCB_Card_Drafted" : ((s.drafted > 0) ? "RCB_Card_DraftedPart" : "RCB_Card_Undrafted")).Translate();
		string text2 = ((s.fireAtWill == s.present) ? "RCB_Card_Fire" : ((s.fireAtWill > 0) ? "RCB_Card_FirePart" : "RCB_Card_FireOff")).Translate();
		string text3 = text + " · " + text2;
		if (s.hasCommonMode)
		{
			text3 = text3 + " · " + "RCB_Card_AssaultMode".Translate(ModeLabel(s.commonMode));
		}
		else if (s.assault > 0)
		{
			text3 = text3 + " · " + "RCB_Card_AssaultPart".Translate();
		}
		return text3;
	}

	private static string ModeLabel(AssaultMode mode)
	{
		return mode switch
		{
			AssaultMode.Weakest => "RCB_Card_Weakest".Translate(), 
			AssaultMode.Fleeing => "RCB_Card_Fleeing".Translate(), 
			_ => "RCB_Card_Strongest".Translate(), 
		};
	}

	private void AssaultButton(Rect r, AssaultMode mode, string labelKey, string tipKey, SquadHotkeys.Key key, SquadStatus.State state, bool enabled)
	{
		bool flag = state.hasCommonMode && state.commonMode == mode;
		string tip = tipKey.Translate().ToString();
		if (flag)
		{
			tip = tip + "\n\n" + "RCB_Card_AssaultStopTip".Translate().ToString();
		}
		tip = Tip(SquadHotkeys.WithKey(tip, key), enabled, "RCB_Orders_NoMember".Translate());
		if (Button(r, IconSet.Common.Sword, labelKey.Translate(), tip, enabled, flag))
		{
			if (flag)
			{
				SquadOrders.StopAssault(index);
			}
			else
			{
				SquadOrders.Assault(index, mode);
			}
		}
	}

	private void DrawManageRow(Rect r, BarSquads squads)
	{
		float num = (r.width - Gap * 3f) / 4f;
		int count = squads.names.Count;
		if (Button(new Rect(r.x, r.y, num, r.height), null, "RCB_CC_SquadRename".Translate(), null, enabled: true, active: false))
		{
			Find.WindowStack.Add(new Dialog_RenameSquad(squads, index));
			// The card sits above dialogs: left open, it would cover the rename box and keep its input.
			Close();
		}
		if (Button(new Rect(r.x + (num + Gap), r.y, num, r.height), null, "RCB_CC_SquadMoveUp".Translate(), null, index > 0, active: false))
		{
			squads.MoveSquad(index, -1);
			Changed(closeCard: true);
		}
		if (Button(new Rect(r.x + (num + Gap) * 2f, r.y, num, r.height), null, "RCB_CC_SquadMoveDown".Translate(), null, index < count - 1, active: false))
		{
			squads.MoveSquad(index, 1);
			Changed(closeCard: true);
		}
		if (Button(new Rect(r.x + (num + Gap) * 3f, r.y, num, r.height), null, "RCB_CC_SquadDisband".Translate(), null, enabled: true, active: false))
		{
			AskDisband(squads);
		}
	}

	// Disbanding loses the squad's name, colour and icon: it asks first, like clearing a station. The card closes so
	// it cannot cover the confirmation; the callback re-checks the squad by name, since indices may have moved.
	private void AskDisband(BarSquads squads)
	{
		int i = index;
		string name = squads.SquadName(i);
		Action action = refresh;
		Close();
		Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("RCB_Squad_DisbandConfirm".Translate(name), delegate
		{
			if (i < squads.names.Count && squads.names[i] == name)
			{
				squads.Disband(i);
				SquadStatus.Invalidate();
				action?.Invoke();
			}
		}, destructive: true));
	}

	// Quick assignment (Colonist Bar Plus): add the selected colonists and slaves to this squad, or take them out.
	private void DrawMembers(float x, ref float y, float w, BarSquads squads)
	{
		RcbPalette.MicroLabel(new Rect(x, y, w, 18f), "RCB_Members_Title".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
		y += 18f;
		SquadMembers.CountSelected(index, out var joining, out var moved, out var inside);
		float num = (w - Gap) / 2f;
		string text = "RCB_Members_AddTip".Translate(squads.SquadName(index)).ToString();
		if (moved > 0)
		{
			text = text + "\n\n" + "RCB_Members_AddMoves".Translate(moved).ToString();
		}
		if (Button(new Rect(x, y, num, BtnH), null, "RCB_Members_Add".Translate(joining), Tip(text, joining > 0, "RCB_Members_AddNone".Translate()), joining > 0, active: false))
		{
			SquadMembers.AddSelected(index);
			Changed(closeCard: false);
		}
		if (Button(new Rect(x + num + Gap, y, num, BtnH), null, "RCB_Members_Remove".Translate(inside), Tip("RCB_Members_RemoveTip".Translate(), inside > 0, "RCB_Members_RemoveNone".Translate()), inside > 0, active: false))
		{
			SquadMembers.RemoveSelected(index);
			Changed(closeCard: false);
		}
		y += BtnH;
	}

	// A greyed button says why after what it does.
	private static string Tip(string tip, bool enabled, string why)
	{
		return enabled ? tip : (tip + "\n\n" + why);
	}

	// Moving or disbanding changes the squad indices: the card would point at another squad, so it closes.
	private void Changed(bool closeCard)
	{
		SquadStatus.Invalidate();
		refresh?.Invoke();
		if (closeCard)
		{
			Close();
		}
	}

	// active: the whole squad is in this state (accent fill). partial: only some members are (accent underline).
	private static bool Button(Rect r, Texture2D icon, string label, string tip, bool enabled, bool active, bool partial = false)
	{
		bool flag = enabled && Mouse.IsOver(r);
		Color c = (active ? RcbPalette.Accent : Color.Lerp(RcbPalette.Ink, Color.white, flag ? 0.16f : 0.08f));
		RcbSpatial.Rounded(r, enabled ? c : Palette.WithAlpha(c, 0.45f));
		if (partial && !active)
		{
			RcbSpatial.Rounded(new Rect(r.x + 8f, r.yMax - 4f, r.width - 16f, 2f), RcbPalette.Accent, 1f);
		}
		Color color = ((!enabled) ? RcbPalette.TextDim2 : (active ? Palette.Flat.Rail : (flag ? RcbPalette.Stat : RcbPalette.TextDim)));
		float num = r.x + 8f;
		if (icon != null)
		{
			Rect position = new Rect(r.x + 6f, r.center.y - 8f, 16f, 16f);
			Color color2 = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(position, icon);
			GUI.color = color2;
			num = position.xMax + 5f;
		}
		float num2 = Mathf.Max(10f, r.xMax - num - 6f);
		RcbFont.Label(new Rect(num, r.y, num2, r.height), RcbFont.Fit(label, num2, bold: false, GameFont.Small), bold: false, GameFont.Small, color);
		if (!tip.NullOrEmpty() && Mouse.IsOver(r))
		{
			TooltipHandler.TipRegion(r, tip);
		}
		if (enabled && Widgets.ButtonInvisible(r))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			return true;
		}
		return false;
	}
}
