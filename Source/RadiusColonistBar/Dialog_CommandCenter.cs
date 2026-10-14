using System;
using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RadiusColonistBar;

public class Dialog_CommandCenter : Window
{
	private static readonly Vector2 FrameSize = new Vector2(900f, 560f);

	private readonly SizeAnim railAnim;

	private readonly List<ICCModule> modules;

	private int active;

	private readonly CCContext ctx = new CCContext();

	private float nextRefresh;

	private bool refreshRequested = true;

	private readonly List<Pawn> roster = new List<Pawn>(32);

	private static Pawn pendingFocus;

	private static int pendingModule = -1;

	private static Type pendingModuleType;

	private static float RailFoldedW => 56f;

	public override Vector2 InitialSize => FrameSize;

	protected override float Margin => 0f;

	public Dialog_CommandCenter()
	{
		modules = new List<ICCModule>
		{
			new Module_Overview(),
			new Module_Health(),
			new Module_Armory(),
			new Module_Loadouts(),
			new Module_Squads()
		};
		doCloseX = false;
		drawShadow = false;
		doWindowBackground = false;
		draggable = true;
		resizeable = false;
		absorbInputAroundWindow = false;
		preventCameraMotion = false;
		closeOnClickedOutside = true;
		closeOnCancel = true;
		focusWhenOpened = true;
		forcePause = false;
		layer = WindowLayer.GameUI;
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		railAnim = new SizeAnim((settings != null && settings.ccRailCollapsed) ? RailFoldedW : 214f, 0f, 0.18f);
		ctx.requestClose = delegate
		{
			Close(doCloseSound: false);
		};
		ctx.requestRefresh = delegate
		{
			refreshRequested = true;
		};
		ctx.roster = roster;
		ctx.selection = new List<Pawn>();
	}

	public static void Open()
	{
		OpenFor(null, -1);
	}

	public static void OpenFor(Pawn p, int module)
	{
		Find.WindowStack.WindowOfType<Dialog_CommandCenter>()?.Close(doCloseSound: false);
		pendingFocus = p;
		pendingModule = module;
		pendingModuleType = null;
		Find.WindowStack.Add(new Dialog_CommandCenter());
	}

	public static void OpenFor(Pawn p, Type module)
	{
		Find.WindowStack.WindowOfType<Dialog_CommandCenter>()?.Close(doCloseSound: false);
		pendingFocus = p;
		pendingModule = -1;
		pendingModuleType = module;
		Find.WindowStack.Add(new Dialog_CommandCenter());
	}

	public override void PreOpen()
	{
		base.PreOpen();
		if (pendingModuleType != null)
		{
			for (int i = 0; i < modules.Count; i++)
			{
				if (modules[i].GetType() == pendingModuleType)
				{
					pendingModule = i;
					break;
				}
			}
			pendingModuleType = null;
		}
		if (pendingModule >= 0 && pendingModule < modules.Count)
		{
			active = pendingModule;
		}
		ctx.focus = pendingFocus;
		pendingFocus = null;
		pendingModule = -1;
		refreshRequested = true;
	}

	protected override void SetInitialSizeAndPosition()
	{
		windowRect = new Rect(Mathf.Round(((float)UI.screenWidth - FrameSize.x) * 0.5f), Mathf.Round(((float)UI.screenHeight - FrameSize.y) * 0.5f), FrameSize.x, FrameSize.y);
	}

	private void RefreshIfDue()
	{
		if (!refreshRequested && !Throttle.Every(ref nextRefresh, 1f))
		{
			return;
		}
		refreshRequested = false;
		roster.Clear();
		try
		{
			roster.AddRange(CCRoster.GatherColonists());
		}
		catch
		{
		}
		ctx.squads = BarSquads.Get;
		try
		{
			modules[active].Refresh(ctx);
		}
		catch (Exception ex)
		{
			Log.WarningOnce("[Radius UI - Colonist Bar] Command Center refresh failed: " + ex, 481522);
		}
	}

	public override void DoWindowContents(Rect inRect)
	{
		RefreshIfDue();
		Rect panel = new Rect(inRect.x, inRect.y, inRect.width - 14f, inRect.height);
		float radius = 10f;
		Rect rect = new Rect(panel.x, panel.y, RailWidthNow(), panel.height);
		Rect rect2 = new Rect(rect.xMax, panel.y, panel.xMax - rect.xMax, panel.height);
		CardChrome.RoundedCorners(rect, Palette.Flat.Rail, radius, Corner.LeftOnly);
		CardChrome.RoundedCorners(rect2, Palette.Flat.Panel, radius, Corner.RightOnly);
		if (UIKit.Flat.CloseX(panel))
		{
			Close();
			return;
		}
		DrawRail(rect);
		Rect rect3 = new Rect(rect2.x + 26f, rect2.y + 22f, rect2.width - 52f, rect2.height - 44f);
		try
		{
			modules[active].Draw(rect3, ctx);
		}
		catch (Exception ex)
		{
			Log.ErrorOnce("[Radius UI - Colonist Bar] Command Center module " + modules[active].Label + " threw: " + ex, 481523);
		}
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		Text.WordWrap = true;
	}

	private float RailWidthNow()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		railAnim.Target((settings != null && settings.ccRailCollapsed) ? RailFoldedW : 214f, 0f);
		return Mathf.Round(railAnim.Width);
	}

	private void DrawRail(Rect rail)
	{
		float num = Mathf.Min(12f, Mathf.Max(0f, (rail.width - 22f) * 0.5f));
		float x = rail.x + num;
		float num2 = rail.width - num * 2f;
		float y = rail.y + 22f;
		float railEntryH = UIKit.Flat.RailEntryH;
		bool flag = rail.width >= 213f;
		SectionLabel(x, ref y, num2, "RCB_CC_Title".Translate(), flag);
		string text = null;
		for (int i = 0; i < modules.Count; i++)
		{
			ICCModule iCCModule = modules[i];
			if (!string.IsNullOrEmpty(iCCModule.Section) && iCCModule.Section != text)
			{
				y += 18f;
				SectionLabel(x, ref y, num2, iCCModule.Section, flag);
				text = iCCModule.Section;
			}
			Rect rect = new Rect(x, y, num2, railEntryH);
			if (!flag && Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, iCCModule.Label);
			}
			if (UIKit.Flat.RailEntry(rect, iCCModule.Icon, flag ? iCCModule.Label : null, i == active) && i != active)
			{
				active = i;
				refreshRequested = true;
			}
			y += railEntryH + 2f;
		}
		DrawRailGrip(rail, num);
	}

	private void DrawRailGrip(Rect rail, float pad)
	{
		float railEntryH = UIKit.Flat.RailEntryH;
		Rect rect = new Rect(rail.x + pad, rail.yMax - railEntryH - 22f, rail.width - pad * 2f, railEntryH);
		if (rect.width < 8f || rect.height < 8f)
		{
			return;
		}
		bool flag = Mouse.IsOver(rect);
		float num = 10f;
		float num2 = Mathf.Round(rect.center.x - num * 0.5f);
		float num3 = Mathf.Round(rect.center.y - num * 0.5f);
		Color color = (flag ? Palette.Flat.Ink : Palette.Flat.InkMid);
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				Sdf.Disc(new Rect(num2 + (float)j * 6f, num3 + (float)i * 6f, 4f, 4f), color);
			}
		}
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		bool flag2 = settings?.ccRailCollapsed ?? false;
		if (flag)
		{
			TooltipHandler.TipRegion(rect, (flag2 ? "RCB_CC_RailExpand" : "RCB_CC_RailCollapse").Translate());
		}
		if (Widgets.ButtonInvisible(rect) && settings != null)
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			settings.ccRailCollapsed = !settings.ccRailCollapsed;
			try
			{
				RadiusColonistBarMod.Instance?.WriteSettings();
			}
			catch
			{
			}
			refreshRequested = true;
		}
	}

	private static void SectionLabel(float x, ref float y, float w, string label, bool draw)
	{
		float sectionHeaderH = UIKit.Flat.SectionHeaderH;
		if (draw)
		{
			UIKit.Flat.SectionHeader(new Rect(x + 12f, y, w - 12f, sectionHeaderH), label);
		}
		y += sectionHeaderH + 12f;
	}
}
