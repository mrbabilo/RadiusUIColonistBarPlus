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

	// Squad rules (Colonist Bar Plus): the tab strip, and the tallest tab (Combat: two order rows, stations, assault,
	// care). The Rules tab, six rows plus the forced-apparel row, fits under it.
	private const float TabH = 22f;

	private const float TabAreaH = BtnH * 2f + Gap + 10f + (18f + PipH + Gap + BtnH) + 10f + 18f + BtnH + 10f + 18f + BtnH;

	private const float RuleLabelW = 92f;

	// Wide enough for the French "Modifier" (the English "Edit" fits easily).
	private const float RuleEditW = 78f;

	// The open tab, kept for the session and shared by every squad: the same settings are often made squad after squad.
	private static int tab;

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

	// Where the player last dragged a card, bar card and Command Center card apart: it reopens there.
	private static Vector2? movedBar;

	private static Vector2? movedManaged;

	private Vector2 openedAt;

	// The squad's state and member lists, rebuilt every few frames or after an order instead of every frame: each list
	// sorts or scans the map, and a dozen buttons read them. An order bumps SquadStatus.Version, so its effect shows
	// on the next frame; a change made by the game shows up to SnapFrames late.
	private const int SnapFrames = 6;

	private int snapFrame = -1000;

	private int snapVersion = -1;

	private Map snapMap;

	private SquadStatus.State snapState;

	private List<Pawn> able = new List<Pawn>();

	private int spawned;

	private readonly int[] stationCounts = new int[BarSquads.StationSlots];

	private List<Pawn> rescue = new List<Pawn>();

	private List<Pawn> tend = new List<Pawn>();

	private List<Pawn> rest = new List<Pawn>();

	private bool anyDoctor;

	private int rescuing;

	private int tending;

	// Squad rules (Colonist Bar Plus): per rule, the shared value (or null), whether members differ, and the members
	// it applies to. Read only while the Rules tab is open.
	private readonly string[] ruleValue = new string[6];

	private readonly bool[] ruleMixed = new bool[6];

	private readonly List<Pawn>[] ruleMembers = new List<Pawn>[6]
	{
		new List<Pawn>(),
		new List<Pawn>(),
		new List<Pawn>(),
		new List<Pawn>(),
		new List<Pawn>(),
		new List<Pawn>()
	};

	private int forced;

	// Squad work (Colonist Bar Plus): the Work tab's mode, kept for the session like the tab, and its snapshot, read
	// only while the tab is open.
	private const float ChipH = 26f;

	private const float WorkGridH = TabAreaH - 58f - 26f - 40f;

	private static bool workNow = true;

	private Vector2 workScroll;

	private List<Pawn> pauseEat = new List<Pawn>();

	private List<Pawn> pauseSleep = new List<Pawn>();

	private List<Pawn> pauseJoy = new List<Pawn>();

	private List<WorkTypeDef> workTypes = new List<WorkTypeDef>();

	private int[] workDoing = new int[0];

	// Snapshot too: counting assigned members per chip and fitting labels every pass cost most of the card's draw.
	private int[] workAssigned = new int[0];

	private string[] workLabel = new string[0];

	private float workLabelW = -1f;

	private int[] workPrio = new int[0];

	private bool[] workMixed = new bool[0];

	private List<Pawn> workMembers = new List<Pawn>();

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
		if ((windowRect.position - openedAt).sqrMagnitude > 4f)
		{
			if (managed)
			{
				movedManaged = windowRect.position;
			}
			else
			{
				movedBar = windowRect.position;
			}
		}
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
		Vector2? vector = (managed ? movedManaged : movedBar);
		float x = Mathf.Clamp(vector.HasValue ? vector.Value.x : anchor.x, 4f, (float)UI.screenWidth - initialSize.x - 4f);
		float y = Mathf.Clamp(vector.HasValue ? vector.Value.y : (anchor.yMax + 4f), 4f, (float)UI.screenHeight - initialSize.y - 4f);
		windowRect = new Rect(x, y, initialSize.x, initialSize.y).Rounded();
		openedAt = windowRect.position;
	}

	private float ContentHeight()
	{
		// Header 24 + state 20 + tab strip + tab area + members title 18 + members row, plus the footer when managed.
		float num = Pad + 24f + 20f + 8f + TabH + 8f + TabAreaH + 10f + 18f + BtnH + Pad;
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
		Snapshot(get);
		SquadStatus.State state = snapState;
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
		// Shown on every order while nobody can take one.
		string why = NoMemberWhy();
		// The short form here; the full reason stays in the tooltips. Fit, not wrap: a partial state plus the medical
		// counts can outgrow the line.
		string text = (flag ? StateLine(state) : (((spawned > 0) ? "RCB_Card_NoneAble" : "RCB_Orders_NoMember").Translate() + MedicalLine(state)));
		RcbFont.Label(new Rect(x, y, num, 20f), RcbFont.Fit(text, num, bold: false, GameFont.Tiny), bold: false, GameFont.Tiny, RcbPalette.TextDim);
		y += 28f;
		// Squad rules (Colonist Bar Plus): the card's sections sit in tabs; the tab area keeps the height of the
		// tallest tab, so switching never moves the card or its bottom rows.
		DrawTabs(new Rect(x, y, num, TabH));
		y += TabH + 8f;
		float num4 = y + TabAreaH;
		if (tab == 1)
		{
			if (DrawRules(x, y, num))
			{
				return;
			}
		}
		else if (tab == 2)
		{
			DrawWork(x, y, num);
		}
		else if (DrawCombat(x, ref y, num, get, state, flag, why))
		{
			return;
		}
		y = num4 + 10f;
		DrawMembers(x, ref y, num, get);
		if (managed && managedSquads != null)
		{
			y += 8f;
			DrawManageRow(new Rect(x, y, num, BtnH), managedSquads);
		}
	}

	// The Combat tab: orders, battle stations, assault and care, as before the tabs. Returns true when the card closed.
	private bool DrawCombat(float x, ref float y, float num, BarSquads get, SquadStatus.State state, bool flag, string why)
	{
		float num2 = (num - Gap) / 2f;
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
		int count = spawned;
		if (Button(new Rect(x + num2 + Gap, y, num2, BtnH), null, "RCB_Members_Select".Translate(count), Tip("RCB_Members_SelectTip".Translate(), count > 0, "RCB_Orders_NoMember".Translate()), count > 0, active: false))
		{
			SquadMembers.SelectAll(Find.CurrentMap, index);
		}
		y += BtnH + 10f;
		if (DrawStations(x, ref y, num, get, state))
		{
			return true;
		}
		y += 10f;
		RcbPalette.MicroLabel(new Rect(x, y, num, 18f), "RCB_Card_AssaultTitle".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
		y += 18f;
		float num3 = (num - Gap * 2f) / 3f;
		AssaultButton(new Rect(x, y, num3, BtnH), AssaultMode.Strongest, "RCB_Card_Strongest", "RCB_Orders_AssaultStrongestTip", SquadHotkeys.Key.Strongest, state, flag);
		AssaultButton(new Rect(x + num3 + Gap, y, num3, BtnH), AssaultMode.Weakest, "RCB_Card_Weakest", "RCB_Orders_AssaultWeakestTip", SquadHotkeys.Key.Weakest, state, flag);
		AssaultButton(new Rect(x + (num3 + Gap) * 2f, y, num3, BtnH), AssaultMode.Fleeing, "RCB_Card_Fleeing", "RCB_Orders_AssaultFleeingTip", SquadHotkeys.Key.Fleeing, state, flag);
		y += BtnH + 10f;
		DrawMedical(x, ref y, num, flag, why);
		return false;
	}

	private void Snapshot(BarSquads squads)
	{
		Map currentMap = Find.CurrentMap;
		if (Time.frameCount - snapFrame < SnapFrames && snapVersion == SquadStatus.Version && snapMap == currentMap)
		{
			return;
		}
		snapFrame = Time.frameCount;
		snapVersion = SquadStatus.Version;
		snapMap = currentMap;
		snapState = SquadStatus.Fresh(index);
		able = SquadMembers.Able(currentMap, index);
		spawned = SquadMembers.Spawned(currentMap, index).Count;
		for (int i = 0; i < BarSquads.StationSlots; i++)
		{
			stationCounts[i] = SquadStations.CountOnMap(able, squads, i, currentMap);
		}
		rescue = SquadMedical.Targets(currentMap, index, SquadMedical.Kind.Rescue);
		tend = SquadMedical.Targets(currentMap, index, SquadMedical.Kind.Tend);
		rest = SquadMedical.ToRest(able);
		anyDoctor = SquadMedical.AnyDoctor(able);
		if (tab == 1)
		{
			List<Pawn> members = SquadRules.Members(index);
			for (int k = 0; k < SquadRules.All.Length; k++)
			{
				ruleValue[k] = SquadRules.Shared(members, SquadRules.All[k], ruleMembers[k], out ruleMixed[k]);
			}
			forced = SquadRules.ForcedCount(members);
		}
		else if (tab == 2)
		{
			pauseEat = SquadWork.PauseTargets(able, SquadWork.Pause.Eat);
			pauseSleep = SquadWork.PauseTargets(able, SquadWork.Pause.Sleep);
			pauseJoy = SquadWork.PauseTargets(able, SquadWork.Pause.Joy);
			workTypes = SquadWork.Types(workNow);
			workMembers = SquadRules.Members(index);
			int count = workTypes.Count;
			workDoing = new int[count];
			workAssigned = new int[count];
			workLabel = new string[count];
			workPrio = new int[count];
			workMixed = new bool[count];
			for (int l = 0; l < count; l++)
			{
				WorkTypeDef workTypeDef = workTypes[l];
				for (int m = 0; m < able.Count; m++)
				{
					if (able[m].CurJob?.workGiverDef?.workType == workTypeDef)
					{
						workDoing[l]++;
					}
					if (SquadWork.Assigned(able[m], workTypeDef))
					{
						workAssigned[l]++;
					}
				}
				workPrio[l] = SquadWork.SharedPriority(workMembers, workTypeDef, out workMixed[l], out var _);
			}
		}
		rescuing = (tending = 0);
		for (int j = 0; j < able.Count; j++)
		{
			if (able[j].CurJobDef == JobDefOf.Rescue)
			{
				rescuing++;
			}
			else if (able[j].CurJobDef == JobDefOf.TendPatient)
			{
				tending++;
			}
		}
	}

	private void DrawTabs(Rect r)
	{
		string[] array = new string[3]
		{
			"RCB_Tab_Combat".Translate(),
			"RCB_Tab_Rules".Translate(),
			"RCB_Tab_Work".Translate()
		};
		float num = (r.width - Gap * (float)(array.Length - 1)) / (float)array.Length;
		for (int i = 0; i < array.Length; i++)
		{
			Rect rect = new Rect(r.x + (num + Gap) * (float)i, r.y, num, r.height);
			bool flag = i == tab;
			bool flag2 = Mouse.IsOver(rect);
			if (flag || flag2)
			{
				RcbSpatial.Rounded(rect, Color.Lerp(RcbPalette.Ink, Color.white, flag ? 0.14f : 0.07f), 6f);
			}
			if (flag)
			{
				RcbSpatial.Rounded(new Rect(rect.x + 8f, rect.yMax - 3f, rect.width - 16f, 2f), RcbPalette.Accent, 1f);
			}
			RcbPalette.MicroLabel(rect, array[i], (flag || flag2) ? RcbPalette.Stat : RcbPalette.TextDim2, TextAnchor.MiddleCenter, shadow: false);
			if (!flag && Widgets.ButtonInvisible(rect))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				tab = i;
				// The rules and the work data are only read while their tab is open: read them now, before it draws.
				snapFrame = -1000;
				BarSquads get = BarSquads.Get;
				if (get != null)
				{
					Snapshot(get);
				}
			}
		}
	}

	// Squad rules (Colonist Bar Plus): one row per rule, label, shared value (a menu of choices), and vanilla's editor
	// when it has one. Returns true when the card closed.
	private bool DrawRules(float x, float y, float w)
	{
		for (int i = 0; i < SquadRules.All.Length; i++)
		{
			SquadRules.Rule rule = SquadRules.All[i];
			RcbFont.Label(new Rect(x, y, RuleLabelW, BtnH), RcbFont.Fit(SquadRules.Label(rule), RuleLabelW - 4f, bold: false, GameFont.Tiny), bold: false, GameFont.Tiny, RcbPalette.TextDim);
			bool flag = SquadRules.HasEditor(rule);
			float num = w - RuleLabelW - (flag ? (RuleEditW + Gap) : 0f);
			Rect r = new Rect(x + RuleLabelW, y, num, BtnH);
			List<Pawn> list = ruleMembers[i];
			bool flag2 = list.Count > 0;
			string text = ((!flag2) ? "-" : (ruleMixed[i] ? "RCB_Rules_Mixed".Translate().ToString() : ruleValue[i]));
			string tip = null;
			if (Mouse.IsOver(r))
			{
				tip = RuleTip(rule, list, ruleMixed[i]);
			}
			if (Button(r, null, text, tip, flag2, active: false, ruleMixed[i]))
			{
				SquadRules.OpenMenu(index, rule, ruleMixed[i] ? null : ruleValue[i]);
			}
			// Tiny: the French vanilla label is "Modifier..." and overflows the small button in Small.
			if (flag && Button(new Rect(r.xMax + Gap, y, RuleEditW, BtnH), null, "AssignTabEdit".Translate(), "RCB_Rules_EditTip".Translate(), enabled: true, active: false, partial: false, GameFont.Tiny))
			{
				// The card sits above dialogs: left open, it would cover vanilla's manager and keep its input.
				Close();
				SquadRules.OpenEditor(rule, list);
				return true;
			}
			y += BtnH + Gap;
		}
		if (forced > 0)
		{
			RcbFont.Label(new Rect(x, y, w - RuleEditW - Gap, BtnH), "RCB_Rules_Forced".Translate(forced), bold: false, GameFont.Tiny, RcbPalette.TextDim);
			if (Button(new Rect(x + w - RuleEditW, y, RuleEditW, BtnH), null, "RCB_Rules_Release".Translate(), "RCB_Rules_ReleaseTip".Translate(), enabled: true, active: false))
			{
				SquadRules.ReleaseForced(index);
			}
		}
		return false;
	}

	// Squad work (Colonist Bar Plus): breaks, then the work types (do it now, or priorities), then the leader copies.
	private void DrawWork(float x, float y, float w)
	{
		RcbPalette.MicroLabel(new Rect(x, y, w, 18f), "RCB_Work_PauseTitle".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
		y += 18f;
		float num = (w - Gap * 3f) / 4f;
		string noMember = NoMemberWhy();
		bool flag = able.Count > 0;
		PauseButton(new Rect(x, y, num, BtnH), IconSet.Stat.Food, SquadWork.Pause.Eat, pauseEat, flag, noMember);
		PauseButton(new Rect(x + (num + Gap), y, num, BtnH), IconSet.Status.Zzz, SquadWork.Pause.Sleep, pauseSleep, flag, noMember);
		PauseButton(new Rect(x + (num + Gap) * 2f, y, num, BtnH), IconSet.Stat.Mood, SquadWork.Pause.Joy, pauseJoy, flag, noMember);
		PauseButton(new Rect(x + (num + Gap) * 3f, y, num, BtnH), IconSet.Action.Resume, SquadWork.Pause.Resume, able, flag, noMember);
		y += BtnH + 10f;
		DrawWorkHeader(new Rect(x, y, w, 22f));
		y += 26f;
		DrawWorkGrid(new Rect(x, y, w, WorkGridH));
		y += WorkGridH + 10f;
		float num2 = (w - Gap) / 2f;
		// The leader from the snapshot (SquadWork.Leader's rule), not looked up every frame.
		Pawn pawn = ((able.Count > 0) ? able[0] : ((workMembers.Count > 0) ? workMembers[0] : null));
		bool flag2 = pawn != null && workMembers.Count > 1;
		string why = "RCB_Work_NoOther".Translate();
		if (Button(new Rect(x, y, num2, BtnH), null, "RCB_Work_CopyPriorities".Translate(), Tip("RCB_Work_CopyPrioritiesTip".Translate(pawn?.LabelShortCap ?? "-"), flag2, why), flag2, active: false))
		{
			SquadWork.CopyPriorities(index);
		}
		if (Button(new Rect(x + num2 + Gap, y, num2, BtnH), null, "RCB_Work_CopyTimetable".Translate(), Tip("RCB_Work_CopyTimetableTip".Translate(pawn?.LabelShortCap ?? "-"), flag2, why), flag2, active: false))
		{
			SquadWork.CopyTimetable(index);
		}
	}

	private void PauseButton(Rect r, Texture2D icon, SquadWork.Pause pause, List<Pawn> pawns, bool anyAble, string noMember)
	{
		string key = "RCB_Work_" + pause;
		string tip = null;
		bool flag = anyAble && pawns.Count > 0;
		if (Mouse.IsOver(r))
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder((key + "Tip").Translate().ToString());
			for (int i = 0; i < pawns.Count && i < 6; i++)
			{
				stringBuilder.Append((i == 0) ? "\n\n" : "\n").Append(pawns[i].LabelShortCap);
			}
			if (pawns.Count > 6)
			{
				stringBuilder.Append('\n').Append("RCB_Medical_More".Translate(pawns.Count - 6));
			}
			tip = Tip(stringBuilder.ToString(), flag, anyAble ? (key + "None").Translate().ToString() : noMember);
		}
		// Four buttons share one row: the long French labels ("Reprendre") only fit in Tiny, and the count rides the
		// label ("Manger · 3") instead of a "(3)" that used to push every label past its box. Resume shows no count:
		// it would only repeat the able-member count.
		string label = key.Translate();
		if (pause != SquadWork.Pause.Resume && pawns.Count > 0)
		{
			label = label + " · " + pawns.Count;
		}
		if (Button(r, icon, label, tip, flag, active: false, partial: false, GameFont.Tiny))
		{
			SquadWork.DoPause(index, pause);
		}
	}

	// TASKS title, the Now / Priorities switch on the right, and in Priorities the colony-wide manual priorities switch.
	private void DrawWorkHeader(Rect r)
	{
		RcbPalette.MicroLabel(r, "RCB_Work_TasksTitle".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
		float num = 84f;
		Rect rect = new Rect(r.xMax - num * 2f - 2f, r.y, num, r.height);
		Rect rect2 = new Rect(r.xMax - num, r.y, num, r.height);
		if (Segment(rect, "RCB_Work_ModeNow".Translate(), "RCB_Work_ModeNowTip".Translate(), workNow) && !workNow)
		{
			workNow = true;
			workScroll = Vector2.zero;
			snapFrame = -1000;
		}
		if (Segment(rect2, "RCB_Work_ModePriorities".Translate(), "RCB_Work_ModePrioritiesTip".Translate(), !workNow) && workNow)
		{
			workNow = false;
			workScroll = Vector2.zero;
			snapFrame = -1000;
		}
		if (!workNow)
		{
			bool useWorkPriorities = Find.PlaySettings.useWorkPriorities;
			Rect r2 = new Rect(rect.x - Gap - 96f, r.y, 96f, r.height);
			if (Segment(r2, (useWorkPriorities ? "RCB_Work_ManualOn" : "RCB_Work_ManualOff").Translate(), "RCB_Work_ManualTip".Translate(), useWorkPriorities))
			{
				SquadWork.SetManual(!useWorkPriorities);
			}
		}
		if (snapFrame == -1000)
		{
			BarSquads get = BarSquads.Get;
			if (get != null)
			{
				Snapshot(get);
			}
		}
	}

	private static bool Segment(Rect r, string label, string tip, bool on)
	{
		bool flag = Mouse.IsOver(r);
		RcbSpatial.Rounded(r, on ? Palette.WithAlpha(RcbPalette.Accent, 0.85f) : Color.Lerp(RcbPalette.Ink, Color.white, flag ? 0.14f : 0.07f), 6f);
		RcbFont.Label(r, RcbFont.Fit(label, r.width - 8f, bold: false, GameFont.Tiny), bold: false, GameFont.Tiny, on ? Palette.Flat.Rail : (flag ? RcbPalette.Stat : RcbPalette.TextDim), TextAnchor.MiddleCenter);
		if (flag)
		{
			TooltipHandler.TipRegion(r, tip);
		}
		if (Widgets.ButtonInvisible(r))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			return true;
		}
		return false;
	}

	// Three columns of chips in a scroll view. The card leaves the camera free, so the wheel over the grid is used up
	// here, or it would zoom the map as well. A right click is read apart: the card's buttons only take the left one.
	private void DrawWorkGrid(Rect r)
	{
		int num = 3;
		int num2 = (workTypes.Count + num - 1) / num;
		float num3 = (float)num2 * (ChipH + Gap) - Gap;
		bool flag = num3 > r.height;
		Rect viewRect = new Rect(0f, 0f, r.width - (flag ? 16f : 0f), Mathf.Max(num3, r.height));
		float num4 = (viewRect.width - Gap * (float)(num - 1)) / (float)num;
		bool flag2 = Mouse.IsOver(r);
		int depth = FlatScroll.Depth;
		FlatScroll.Begin(r, ref workScroll, viewRect);
		try
		{
			// Only the visible rows, and no hover or click outside the grid: a chip scrolled out of view would still
			// catch the mouse over the rows above or below.
			for (int i = 0; i < workTypes.Count; i++)
			{
				float num5 = (float)(i / num) * (ChipH + Gap);
				if (RcbPalette.RowVisible(num5, ChipH, workScroll, r.height, 0f))
				{
					WorkChip(new Rect((float)(i % num) * (num4 + Gap), num5, num4, ChipH), i, flag2);
				}
			}
		}
		finally
		{
			FlatScroll.EndOrUnwind(depth);
		}
		if (flag2 && Event.current.type == EventType.ScrollWheel)
		{
			Event.current.Use();
		}
	}

	private void WorkChip(Rect r, int i, bool gridHover)
	{
		WorkTypeDef workTypeDef = workTypes[i];
		int num2 = workAssigned[i];
		bool flag = (workNow ? (num2 > 0) : (workPrio[i] >= 0));
		bool flag2 = !workNow && workMixed[i];
		bool flag4 = gridHover && Mouse.IsOver(r);
		bool flag3 = flag && flag4;
		Color c = Color.Lerp(RcbPalette.Ink, Color.white, flag3 ? 0.16f : 0.08f);
		RcbSpatial.Rounded(r, flag ? c : Palette.WithAlpha(c, 0.45f), 8f);
		if (flag2 || (workNow && workDoing[i] > 0))
		{
			RcbSpatial.Rounded(new Rect(r.x + 6f, r.yMax - 3f, r.width - 12f, 2f), RcbPalette.Accent, 1f);
		}
		float num3 = 22f;
		Rect rect = new Rect(r.xMax - num3 - 2f, r.y, num3, r.height);
		Color col = ((!flag) ? RcbPalette.TextDim2 : (flag3 ? RcbPalette.Stat : RcbPalette.TextDim));
		// Fit measures text, which is costly every pass for every chip: the fitted labels are cached per width.
		float num5 = r.width - num3 - 10f;
		if (workLabel[i] == null || workLabelW != num5)
		{
			if (workLabelW != num5)
			{
				Array.Clear(workLabel, 0, workLabel.Length);
				workLabelW = num5;
			}
			workLabel[i] = RcbFont.Fit(workTypeDef.labelShort.CapitalizeFirst(), num5, bold: false, GameFont.Tiny);
		}
		RcbFont.Label(new Rect(r.x + 6f, r.y, num5, r.height), workLabel[i], bold: false, GameFont.Tiny, col);
		if (workNow)
		{
			if (workDoing[i] > 0)
			{
				RcbFont.Label(rect, workDoing[i].ToString(), bold: true, GameFont.Tiny, RcbPalette.Accent, TextAnchor.MiddleCenter);
			}
		}
		else if (workPrio[i] > 0 && !flag2)
		{
			if (Find.PlaySettings.useWorkPriorities)
			{
				RcbFont.Label(rect, workPrio[i].ToString(), bold: true, GameFont.Small, WidgetsWork.ColorOfPriority(workPrio[i]), TextAnchor.MiddleCenter);
			}
			else
			{
				Color color = GUI.color;
				GUI.color = RcbPalette.Stat;
				GUI.DrawTexture(new Rect(rect.center.x - 7f, rect.center.y - 7f, 14f, 14f), IconSet.Action.Check);
				GUI.color = color;
			}
		}
		if (flag4)
		{
			TooltipHandler.TipRegion(r, WorkTip(workTypeDef, i));
		}
		if (!flag || !gridHover)
		{
			return;
		}
		if (!workNow && Event.current.type == EventType.MouseDown && Event.current.button == 1 && flag4)
		{
			SquadWork.ShiftPriority(index, workTypeDef, 1);
			Event.current.Use();
		}
		else if (Widgets.ButtonInvisible(r) && (workNow ? (Event.current.button == 0) : true))
		{
			if (workNow)
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				SquadWork.WorkNow(index, workTypeDef);
			}
			else
			{
				SquadWork.ShiftPriority(index, workTypeDef, -1);
			}
		}
	}

	// The work type's description, who is assigned, unassigned or incapable here, and, in Priorities, each value.
	private string WorkTip(WorkTypeDef work, int i)
	{
		System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
		stringBuilder.Append(work.gerundLabel.CapitalizeFirst());
		if (!work.description.NullOrEmpty())
		{
			stringBuilder.Append("\n").Append(work.description);
		}
		List<Pawn> list = (workNow ? able : workMembers);
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		int num = 0;
		for (int j = 0; j < list.Count; j++)
		{
			if (!SquadWork.Capable(list[j], work))
			{
				list3.Add(list[j].LabelShortCap);
			}
			else if (!SquadWork.Assigned(list[j], work))
			{
				list2.Add(list[j].LabelShortCap);
			}
			else
			{
				num++;
			}
		}
		stringBuilder.Append("\n\n").Append("RCB_Work_TipAssigned".Translate(num));
		if (list2.Count > 0)
		{
			stringBuilder.Append("\n").Append("RCB_Work_TipUnassigned".Translate(string.Join(", ", list2)));
		}
		if (list3.Count > 0)
		{
			stringBuilder.Append("\n").Append("RCB_Work_TipIncapable".Translate(string.Join(", ", list3)));
		}
		if (!workNow && workMixed[i])
		{
			stringBuilder.Append("\n");
			int num2 = 0;
			for (int k = 0; k < workMembers.Count && num2 < 8; k++)
			{
				if (SquadWork.Capable(workMembers[k], work))
				{
					stringBuilder.Append("\n").Append("RCB_Rules_TipLine".Translate(workMembers[k].LabelShortCap, workMembers[k].workSettings.GetPriority(work)));
					num2++;
				}
			}
		}
		stringBuilder.Append("\n\n").Append((workNow ? "RCB_Work_ChipNowTip" : (Find.PlaySettings.useWorkPriorities ? "RCB_Work_ChipManualTip" : "RCB_Work_ChipToggleTip")).Translate());
		return stringBuilder.ToString();
	}

	// How many members the rule concerns, then each one's value when they differ (eight at most).
	private static string RuleTip(SquadRules.Rule rule, List<Pawn> members, bool mixed)
	{
		if (members.Count == 0)
		{
			return ((rule == SquadRules.Rule.Area) ? "RCB_Rules_NoneOnMap" : "RCB_Rules_None").Translate();
		}
		System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(((rule == SquadRules.Rule.Area) ? "RCB_Rules_TipArea" : "RCB_Rules_Tip").Translate(members.Count).ToString());
		if (mixed)
		{
			stringBuilder.Append('\n');
			for (int i = 0; i < members.Count && i < 8; i++)
			{
				stringBuilder.Append('\n').Append("RCB_Rules_TipLine".Translate(members[i].LabelShortCap, SquadRules.ValueLabel(members[i], rule)));
			}
			if (members.Count > 8)
			{
				stringBuilder.Append('\n').Append("RCB_Medical_More".Translate(members.Count - 8));
			}
		}
		return stringBuilder.ToString();
	}

	// Members on the map but none able (all down, in a break or in deathrest) is not the same as nobody here.
	private string NoMemberWhy()
	{
		return ((spawned > 0) ? "RCB_Orders_NoneAble" : "RCB_Orders_NoMember").Translate().ToString();
	}

	// Battle stations (Colonist Bar Plus): slot pips, then Go / Save / Clear on the active slot, from the card's
	// snapshot. Returns true when the card closed.
	private bool DrawStations(float x, ref float y, float w, BarSquads squads, SquadStatus.State state)
	{
		List<Pawn> list = able;
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
			if (Pip(r, i, stationCounts[i], list.Count, i == num))
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
		bool flag = stationCounts[num] > 0;
		string why = ((list.Count == 0) ? NoMemberWhy() : "RCB_Stations_None".Translate(num + 1, squads.SquadName(index)).ToString());
		if (Button(rect, null, "RCB_Stations_Go".Translate(), Tip(SquadHotkeys.WithKey("RCB_Stations_GoTip".Translate(num + 1), SquadHotkeys.Key.Station), flag, why), flag, active: false))
		{
			SquadStations.Go(index);
		}
		if (Button(new Rect(x + num3 + Gap, y, num3, BtnH), null, "RCB_Stations_Save".Translate(), Tip("RCB_Stations_SaveTip".Translate(num + 1), list.Count > 0, NoMemberWhy()), list.Count > 0, active: false))
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

	internal static string StateLine(SquadStatus.State s)
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
		return text3 + MedicalLine(s);
	}

	// Medical orders (Colonist Bar Plus): " · 2 down · 1 to tend", or nothing.
	internal static string MedicalLine(SquadStatus.State s)
	{
		string text = "";
		if (s.downed > 0)
		{
			text = text + " · " + "RCB_Card_Downed".Translate(s.downed);
		}
		if (s.wounded > 0)
		{
			text = text + " · " + "RCB_Card_Wounded".Translate(s.wounded);
		}
		return text;
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
		tip = Tip(SquadHotkeys.WithKey(tip, key), enabled, NoMemberWhy());
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

	// Medical orders (Colonist Bar Plus): rescue, tend, to bed, from the card's snapshot. The patient lists skip path
	// and bed searches; the click checks everything again. An order still running (members rescuing or tending) gets
	// the partial underline, like a squad partly drafted.
	private void DrawMedical(float x, ref float y, float w, bool anyAble, string noMember)
	{
		RcbPalette.MicroLabel(new Rect(x, y, w, 18f), "RCB_Medical_Title".Translate(), RcbPalette.TextDim2, TextAnchor.MiddleLeft, shadow: false);
		y += 18f;
		float num = (w - Gap * 2f) / 3f;
		string why = (anyAble ? "RCB_Medical_NoDowned".Translate().ToString() : noMember);
		if (MedicalButton(new Rect(x, y, num, BtnH), IconSet.Alert.Crit, "RCB_Medical_Rescue", "RCB_Medical_RescueTip", SquadHotkeys.Key.Rescue, rescue, SquadMedical.Kind.Rescue, anyAble && rescue.Count > 0, why, rescuing))
		{
			SquadMedical.Rescue(index);
		}
		string why2 = ((!anyAble) ? noMember : ((tend.Count == 0) ? "RCB_Medical_NoWounded".Translate().ToString() : "RCB_Medical_NoDoctor".Translate().ToString()));
		if (MedicalButton(new Rect(x + num + Gap, y, num, BtnH), IconSet.Status.Medical, "RCB_Medical_Tend", "RCB_Medical_TendTip", SquadHotkeys.Key.Tend, tend, SquadMedical.Kind.Tend, anyAble && anyDoctor && tend.Count > 0, why2, tending))
		{
			SquadMedical.Tend(index);
		}
		string why3 = (anyAble ? "RCB_Medical_NoRest".Translate().ToString() : noMember);
		if (MedicalButton(new Rect(x + (num + Gap) * 2f, y, num, BtnH), IconSet.Status.Zzz, "RCB_Medical_Bed", "RCB_Medical_BedTip", SquadHotkeys.Key.Bed, rest, SquadMedical.Kind.Bed, rest.Count > 0, why3, 0))
		{
			SquadMedical.ToBed(index);
		}
		y += BtnH;
	}

	// The tooltip names the patients (six at most) with the alert band's words for their state. Built only on hover.
	private bool MedicalButton(Rect r, Texture2D icon, string labelKey, string tipKey, SquadHotkeys.Key key, List<Pawn> pawns, SquadMedical.Kind kind, bool enabled, string why, int busy)
	{
		string tip = null;
		if (Mouse.IsOver(r))
		{
			SquadMedical.Hover(index, pawns);
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(tipKey.Translate().ToString());
			if (busy > 0)
			{
				stringBuilder.Append("\n\n").Append("RCB_Medical_Busy".Translate(busy));
			}
			if (pawns.Count > 0)
			{
				stringBuilder.Append('\n');
			}
			for (int i = 0; i < pawns.Count && i < 6; i++)
			{
				stringBuilder.Append('\n').Append(pawns[i].LabelShortCap);
				if (kind != SquadMedical.Kind.Bed)
				{
					stringBuilder.Append(" · ").Append(SquadMedical.Condition(pawns[i], kind));
				}
			}
			if (pawns.Count > 6)
			{
				stringBuilder.Append('\n').Append("RCB_Medical_More".Translate(pawns.Count - 6));
			}
			tip = Tip(SquadHotkeys.WithKey(stringBuilder.ToString(), key), enabled, why);
		}
		return Button(r, icon, labelKey.Translate(pawns.Count), tip, enabled, active: false, busy > 0);
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
	private static bool Button(Rect r, Texture2D icon, string label, string tip, bool enabled, bool active, bool partial = false, GameFont font = GameFont.Small)
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
		RcbFont.Label(new Rect(num, r.y, num2, r.height), RcbFont.Fit(label, num2, bold: false, font), bold: false, font, color);
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
