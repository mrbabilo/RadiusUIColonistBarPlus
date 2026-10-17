using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class SettingsUI
{
	private static int tab;

	private static Vector2 scrollPos;

	private static float[] measuredH;

	private static readonly string[] TabKeys = new string[7] { "RCB_Tab_Bar", "RCB_Tab_Cards", "RCB_Tab_Who", "RCB_Tab_Alerts", "RCB_Tab_Portraits", "RCB_Tab_Hover", "RCB_Tab_Extras" };

	private static readonly Color TabActive = RcbPalette.Lift;

	private static readonly Color TabHover = RcbPalette.PanelBG;

	private static readonly Color TabIdle = RcbPalette.BG;

	private static readonly Color Dim = RcbPalette.TextDim;

	private const string I1 = "   ";

	private const string I2 = "      ";

	private static readonly List<int> hiddenScratch = new List<int>(8);

	private static readonly string[] TaskModeKeys = new string[3] { "RCB_TaskMode_Always", "RCB_TaskMode_Hover", "RCB_TaskMode_Off" };

	private static readonly string[] FrameModeKeys = new string[3] { "RCB_FrameMode_Off", "RCB_FrameMode_Cards", "RCB_FrameMode_CardsPopout" };

	private static readonly string[] SpeechPlaceKeys = new string[2] { "RCB_SpeechPlace_Bubble", "RCB_SpeechPlace_Card" };

	private static readonly string[] HealthStyleKeys = new string[2] { "RCB_HealthStyle_Rail", "RCB_HealthStyle_Ekg" };

	private static readonly string[] PillModeKeys = new string[3] { "RCB_PillMode_Show", "RCB_PillMode_QuietWithBand", "RCB_PillMode_Off" };

	private static readonly string[] QualityKeys = new string[4] { "RCB_Quality_Low", "RCB_Quality_Medium", "RCB_Quality_High", "RCB_Quality_Ultra" };

	private static readonly int[] QualityPx = new int[4] { 128, 192, 256, 384 };

	private static float[] MeasuredH => measuredH ?? (measuredH = new float[TabKeys.Length]);

	private static Color Accent => RcbPalette.Accent;

	private static string L(string key)
	{
		return key.Translate().ToString();
	}

	private static string L(string key, params NamedArgument[] args)
	{
		return key.Translate(args).ToString();
	}

	private static void DrawRaceFraming(Listing_Standard list, RadiusColonistBarSettings s)
	{
		if (s.raceFraming == null)
		{
			s.raceFraming = new List<RaceFraming>();
		}
		for (int i = 0; i < s.raceFraming.Count; i++)
		{
			RaceFraming raceFraming = s.raceFraming[i];
			if (raceFraming != null)
			{
				ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(raceFraming.race);
				string text = ((namedSilentFail != null) ? namedSilentFail.LabelCap.ToString() : L("RCB_Setting_RaceMissing", raceFraming.race ?? "?"));
				Rect rect = list.GetRect(24f);
				RcbFont.Label(new Rect(rect.x, rect.y, rect.width - 26f, rect.height), text, bold: true, GameFont.Small, (namedSilentFail != null) ? RcbPalette.Stat : RcbPalette.TextDim);
				if (Widgets.ButtonText(new Rect(rect.xMax - 24f, rect.y, 24f, 22f), "×"))
				{
					s.raceFraming.RemoveAt(i);
					RaceFramingTable.Invalidate();
					break;
				}
				float zoom = raceFraming.zoom;
				float panX = raceFraming.panX;
				float panY = raceFraming.panY;
				list.Label(L("RCB_Setting_RaceZoom", zoom.ToString("0.00")));
				raceFraming.zoom = Mathf.Round(list.Slider(zoom, 0.5f, 2.5f) * 20f) / 20f;
				list.Label(L("RCB_Setting_RacePan", panX.ToString("0.00"), panY.ToString("0.00")));
				raceFraming.panX = Mathf.Round(list.Slider(panX, -1f, 1f) * 20f) / 20f;
				raceFraming.panY = Mathf.Round(list.Slider(panY, -1f, 1f) * 20f) / 20f;
				if (raceFraming.zoom != zoom || raceFraming.panX != panX || raceFraming.panY != panY)
				{
					RaceFramingTable.Invalidate();
				}
				list.Gap(4f);
			}
		}
		if (list.ButtonText(L("RCB_Setting_RaceAdd")))
		{
			List<FloatMenuOption> list2 = new List<FloatMenuOption>();
			List<ThingDef> list3 = RaceFramingTable.PickableRaces();
			for (int j = 0; j < list3.Count; j++)
			{
				ThingDef d = list3[j];
				bool flag = false;
				for (int k = 0; k < s.raceFraming.Count; k++)
				{
					if (s.raceFraming[k]?.race == d.defName)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list2.Add(new FloatMenuOption(d.LabelCap, delegate
					{
						s.raceFraming.Add(new RaceFraming(d.defName));
						RaceFramingTable.Invalidate();
					}));
				}
			}
			if (list2.Count == 0)
			{
				list2.Add(new FloatMenuOption(L("RCB_Setting_RaceNoneLeft"), null));
			}
			Find.WindowStack.Add(new FloatMenu(list2));
		}
		Note(list, L("RCB_Setting_RaceFramingNote"));
	}

	private static string PxOrAuto(float v)
	{
		return (v > 0f) ? (Mathf.RoundToInt(v) + " px") : L("RCB_Value_Auto");
	}

	public static void Draw(Rect inRect, RadiusColonistBarSettings s)
	{
		Rect rect = new Rect(inRect.x, inRect.y, inRect.width, 26f);
		bool masterEnabled = s.masterEnabled;
		Widgets.CheckboxLabeled(rect, L("RCB_Setting_MasterEnabled"), ref s.masterEnabled);
		if (masterEnabled != s.masterEnabled)
		{
			MarkBarDirty();
		}
		float num = inRect.y + 32f;
		if (!s.masterEnabled)
		{
			GUI.color = Dim;
			Widgets.Label(new Rect(inRect.x, num + 8f, inRect.width, 24f), L("RCB_Setting_MasterDisabled"));
			GUI.color = Color.white;
			return;
		}
		float num2 = 74f;
		Widgets.Label(new Rect(inRect.x, num + 6f, 58f, 24f), L("RCB_Setting_Preview"));
		for (int i = 0; i < 3; i++)
		{
			Rect rect2 = new Rect(inRect.x + 60f + (float)i * (num2 + 4f), num, num2, 30f);
			bool flag = s.previewState == i;
			Widgets.DrawBoxSolid(rect2, flag ? TabActive : (Mouse.IsOver(rect2) ? TabHover : TabIdle));
			RcbFont.Label(rect2, L(i switch
			{
				1 => "RCB_Setting_PreviewOpen", 
				0 => "RCB_Setting_PreviewAuto", 
				_ => "RCB_Setting_PreviewClosed", 
			}), flag, GameFont.Small, flag ? RcbPalette.Stat : Dim, TextAnchor.MiddleCenter);
			if (Widgets.ButtonInvisible(rect2))
			{
				s.previewState = i;
			}
		}
		TooltipHandler.TipRegion(new Rect(inRect.x, num, 60f + 3f * (num2 + 4f), 30f), L("RCB_Setting_PreviewTip"));
		if (Widgets.ButtonText(new Rect(inRect.xMax - 104f, num, 104f, 30f), L("RCB_Setting_ResetAll")))
		{
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(L("RCB_Setting_ResetAllConfirm"), delegate
			{
				s.style = ViewStyle.Default(BarLayout.TopBar);
				MarkBarDirty();
			}, destructive: true));
		}
		num += 38f;
		float num3 = 30f;
		float num4 = inRect.width / (float)TabKeys.Length;
		int num5 = -1;
		for (int num6 = 0; num6 < TabKeys.Length; num6++)
		{
			Rect rect3 = new Rect(inRect.x + (float)num6 * num4, num, num4, num3);
			Widgets.DrawBoxSolid(rect3, (num6 == tab) ? TabActive : (Mouse.IsOver(rect3) ? TabHover : TabIdle));
			TextAnchor anchor = Text.Anchor;
			GameFont font = Text.Font;
			bool wordWrap = Text.WordWrap;
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.WordWrap = false;
			GUI.color = ((num6 == tab) ? RcbPalette.Stat : RcbPalette.TextDim);
			Widgets.Label(rect3, L(TabKeys[num6]));
			GUI.color = Color.white;
			Text.Anchor = anchor;
			Text.Font = font;
			Text.WordWrap = wordWrap;
			if (Widgets.ButtonInvisible(rect3))
			{
				num5 = num6;
			}
		}
		num += num3 + 6f;
		Rect outRect = new Rect(inRect.x, num, inRect.width, inRect.yMax - num);
		float height = ((MeasuredH[tab] > 0f) ? MeasuredH[tab] : TabHeight(tab, s));
		Rect rect4 = new Rect(0f, 0f, outRect.width - 20f, height);
		RcbPalette.BeginScroll(outRect, ref scrollPos, rect4);
		try
		{
			Listing_Standard listing_Standard = new Listing_Standard
			{
				maxOneColumn = true
			};
			listing_Standard.Begin(rect4);
			switch (tab)
			{
			case 0:
				BarTab(listing_Standard, s);
				break;
			case 1:
				CardsTab(listing_Standard, s);
				break;
			case 2:
				WhoTab(listing_Standard, s);
				break;
			case 3:
				AlertsTab(listing_Standard, s);
				break;
			case 4:
				PortraitsTab(listing_Standard, s);
				break;
			case 5:
				HoverTab(listing_Standard, s);
				break;
			case 6:
				ExtrasTab(listing_Standard, s);
				break;
			}
			listing_Standard.End();
			if (Event.current.type == EventType.Repaint)
			{
				MeasuredH[tab] = listing_Standard.CurHeight + 12f;
			}
		}
		finally
		{
			RcbPalette.EndScroll();
		}
		if (num5 >= 0 && num5 != tab)
		{
			tab = num5;
			scrollPos = Vector2.zero;
		}
	}

	private static float TabHeight(int t, RadiusColonistBarSettings s)
	{
		switch (t)
		{
		case 0:
			return 700f;
		case 1:
			return 640f + (SidearmsBridge.Active ? 34f : 0f);
		case 2:
			return 620f;
		case 3:
			return 340f;
		case 4:
		{
			float num = 460f;
			num += (s.useLiveCameras ? 200f : 170f);
			if (ColorCodedMoodBarBridge.Active)
			{
				num += 70f;
			}
			if (FacialAnimationBridge.Active)
			{
				num += 50f;
				if (s.faPortraitAnimation)
				{
					num += 80f;
				}
			}
			return num;
		}
		case 5:
			return 700f;
		case 6:
			return 460f + (s.followCam ? 110f : 0f);
		default:
			return 700f;
		}
	}

	private static void Header(Listing_Standard list, string key)
	{
		list.Gap(6f);
		GUI.color = Accent;
		list.Label(L(key));
		GUI.color = Color.white;
	}

	private static void Note(Listing_Standard list, string text)
	{
		GUI.color = Dim;
		list.Label("   " + text);
		GUI.color = Color.white;
	}

	private static void BarTab(Listing_Standard list, RadiusColonistBarSettings s)
	{
		Header(list, "RCB_Head_Look");
		list.Label(L("RCB_Setting_CardLayout"));
		if (list.RadioButton(L("RCB_Setting_CardLayout_Side"), s.cardLayout == 0, 8f, L("RCB_Setting_CardLayout_SideTip")) && s.cardLayout != 0)
		{
			s.cardLayout = 0;
			MarkBarDirty();
		}
		if (list.RadioButton(L("RCB_Setting_CardLayout_Stacked"), s.cardLayout == 1, 8f, L("RCB_Setting_CardLayout_StackedTip")) && s.cardLayout != 1)
		{
			s.cardLayout = 1;
			MarkBarDirty();
		}
		Note(list, L("RCB_Setting_CardLayoutNote"));
		list.Gap(6f);
		list.Label(L("RCB_Setting_IslandMaxWidth", Mathf.RoundToInt(s.islandMaxWidthPct)));
		float islandMaxWidthPct = s.islandMaxWidthPct;
		s.islandMaxWidthPct = Mathf.Round(list.Slider(s.islandMaxWidthPct, 30f, 100f) / 2f) * 2f;
		if (!Mathf.Approximately(islandMaxWidthPct, s.islandMaxWidthPct))
		{
			MarkBarDirty();
		}
		Note(list, L("RCB_Setting_IslandMaxWidthNote"));
		string text = (Mathf.Approximately(s.barPosXPct, 50f) ? L("RCB_Setting_NotchPosCentre") : (Mathf.RoundToInt(s.barPosXPct) + "%"));
		list.Label(L("RCB_Setting_NotchPos", text));
		float barPosXPct = s.barPosXPct;
		s.barPosXPct = Mathf.Round(list.Slider(s.barPosXPct, 0f, 100f) / 2f) * 2f;
		if (!Mathf.Approximately(barPosXPct, s.barPosXPct))
		{
			MarkBarDirty();
		}
		Note(list, L("RCB_Setting_NotchPosNote"));
		list.Label(L("RCB_Setting_OpenMaxH", PxOrAuto(s.openMaxHpx)));
		float openMaxHpx = s.openMaxHpx;
		s.openMaxHpx = Mathf.Round(list.Slider(s.openMaxHpx, 0f, 1000f) / 10f) * 10f;
		if (!Mathf.Approximately(openMaxHpx, s.openMaxHpx))
		{
			MarkBarDirty();
		}
		Note(list, L("RCB_Setting_OpenMaxHNote"));
		list.Label(L("RCB_Setting_CollapsedMaxH", PxOrAuto(s.collapsedMaxHpx)));
		s.collapsedMaxHpx = Mathf.Round(list.Slider(s.collapsedMaxHpx, 0f, 200f) / 2f) * 2f;
		Note(list, L("RCB_Setting_CollapsedMaxHNote"));
		list.Label(L("RCB_Setting_BarOpacity", Mathf.RoundToInt(s.barOpacityPct)));
		s.barOpacityPct = Mathf.Round(list.Slider(s.barOpacityPct, 20f, 100f) / 2f) * 2f;
		Note(list, L("RCB_Setting_BarOpacityNote"));
		list.Label(L("RCB_Setting_CardOpacity", Mathf.RoundToInt(s.cardOpacityPct)));
		s.cardOpacityPct = Mathf.Round(list.Slider(s.cardOpacityPct, 20f, 100f) / 2f) * 2f;
		Note(list, L("RCB_Setting_CardOpacityNote"));
		list.Label(L("RCB_Setting_PortraitScale", s.portraitScale.ToString("0.00")));
		float portraitScale = s.portraitScale;
		s.portraitScale = Mathf.Round(list.Slider(s.portraitScale, 0.5f, 2f) * 20f) / 20f;
		if (!Mathf.Approximately(portraitScale, s.portraitScale))
		{
			MarkBarDirty();
		}
		Note(list, L("RCB_Setting_PortraitScaleNote"));
		list.Label(L("RCB_Setting_BarTopOffset", Mathf.RoundToInt(s.barTopOffset)));
		s.barTopOffset = Mathf.Round(list.Slider(s.barTopOffset, -40f, 200f));
		Note(list, L("RCB_Setting_BarTopOffsetNote"));
		Header(list, "RCB_Head_Minimize");
		list.CheckboxLabeled(L("RCB_Setting_IslandCollapse"), ref s.islandCollapse, L("RCB_Setting_IslandCollapseTip"));
		bool islandCollapse = s.islandCollapse;
		if (!islandCollapse)
		{
			GUI.color = Dim;
		}
		list.Label("   " + ((s.islandCollapseDelaySec <= 0f) ? L("RCB_Setting_CollapseDelayNone") : L("RCB_Setting_CollapseDelay", DelayLabel(s.islandCollapseDelaySec))));
		float num = list.Slider(s.islandCollapseDelaySec, 0f, 300f);
		if (islandCollapse)
		{
			s.islandCollapseDelaySec = Mathf.Round(num / 5f) * 5f;
		}
		list.Label("   " + ((s.islandAnimMs <= 0) ? L("RCB_Setting_IslandAnimInstant") : L("RCB_Setting_IslandAnim", s.islandAnimMs)));
		float num2 = list.Slider(s.islandAnimMs, 0f, 1000f);
		if (islandCollapse)
		{
			s.islandAnimMs = Mathf.RoundToInt(num2 / 10f) * 10;
		}
		list.CheckboxLabeled("   " + L("RCB_Setting_CollapsedCount"), ref s.collapsedShowCount, L("RCB_Setting_CollapsedCountTip"));
		GUI.color = Color.white;
		Note(list, L(islandCollapse ? "RCB_Setting_CollapseDelayNote" : "RCB_Setting_CollapseDisabledNote"));
	}

	private static string DelayLabel(float sec)
	{
		if (sec < 60f)
		{
			return Mathf.RoundToInt(sec) + "s";
		}
		int num = Mathf.FloorToInt(sec / 60f);
		int num2 = Mathf.RoundToInt(sec - (float)num * 60f);
		return (num2 == 0) ? (num + "m") : (num + "m " + num2 + "s");
	}

	private static void CardsTab(Listing_Standard list, RadiusColonistBarSettings s)
	{
		ViewStyle v = s.Current;
		Note(list, L("RCB_Setting_CardIntro"));
		Header(list, "RCB_Head_Rows");
		list.CheckboxLabeled(L("RCB_Setting_ShowName"), ref v.showName);
		list.CheckboxLabeled(L("RCB_Setting_ShowRoyalTitle"), ref v.showRoyalTitle, L("RCB_Setting_ShowRoyalTitleTip"));
		TitleColorRow(list, v.showRoyalTitle, ref s.titleColorRoyal);
		list.CheckboxLabeled(L("RCB_Setting_ShowIdeoRole"), ref v.showIdeoRole);
		TitleColorRow(list, v.showIdeoRole, ref s.titleColorIdeo);
		list.CheckboxLabeled(L("RCB_Setting_ShowJobTitle"), ref v.showJobTitle, L("RCB_Setting_ShowJobTitleTip"));
		TitleColorRow(list, v.showJobTitle, ref s.titleColorJob);
		bool showJobTitle = v.showJobTitle;
		if (!showJobTitle)
		{
			GUI.color = Dim;
		}
		bool checkOn = s.jobTitleFallbackOnly;
		list.CheckboxLabeled("   " + L("RCB_Setting_JobTitleFallback"), ref checkOn, L("RCB_Setting_JobTitleFallbackTip"));
		if (showJobTitle && checkOn != s.jobTitleFallbackOnly)
		{
			s.jobTitleFallbackOnly = checkOn;
			MarkBarDirty();
		}
		GUI.color = Color.white;
		int num = ((!v.showJobLabel) ? 2 : (s.jobLabelHoverOnly ? 1 : 0));
		if (list.ButtonTextLabeled(L("RCB_Setting_ShowJobLabel"), L(TaskModeKeys[num])))
		{
			num = (num + 1) % 3;
			v.showJobLabel = num != 2;
			s.jobLabelHoverOnly = num == 1;
			MarkBarDirty();
		}
		Note(list, L("RCB_Setting_ShowJobLabelTip"));
		Header(list, "RCB_Head_Meters");
		Note(list, L("RCB_Setting_MetersNote"));
		StatDropdown(list, L("RCB_Setting_RailTop"), v.barTopStat, delegate(string k)
		{
			v.barTopStat = k;
		}, allowNone: true);
		StatDropdown(list, L("RCB_Setting_RailBottom"), v.barBottomStat, delegate(string k)
		{
			v.barBottomStat = k;
		}, allowNone: true);
		if (list.ButtonTextLabeled(L("RCB_Setting_HealthStyle"), L(HealthStyleKeys[Mathf.Clamp(s.healthMeterStyle, 0, 1)])))
		{
			s.healthMeterStyle = (s.healthMeterStyle + 1) % 2;
		}
		Note(list, L("RCB_Setting_HealthStyleNote"));
		Header(list, "RCB_Head_Markers");
		list.CheckboxLabeled(L("RCB_Setting_CondMarkers"), ref s.showConditionMarkers, L("RCB_Setting_CondMarkersTip"));
		if (s.showConditionMarkers)
		{
			list.Label("   " + ((s.conditionPulseAt <= 0) ? L("RCB_Setting_CondPulseOff") : L("RCB_Setting_CondPulse", s.conditionPulseAt)));
			int num2 = Mathf.RoundToInt(list.Slider((s.conditionPulseAt <= 0) ? 1f : ((float)s.conditionPulseAt), 1f, 12f));
			s.conditionPulseAt = ((num2 > 1) ? num2 : 0);
			Note(list, L("RCB_Setting_CondPulseNote"));
		}
		if (list.ButtonTextLabeled(L("RCB_Setting_StatePill"), L(PillModeKeys[Mathf.Clamp(s.statePillMode, 0, 2)])))
		{
			s.statePillMode = (s.statePillMode + 1) % 3;
		}
		Note(list, L("RCB_Setting_StatePillNote"));
		list.CheckboxLabeled(L("RCB_Setting_ShowStatusIndicators"), ref v.showStatusIndicators, L("RCB_Setting_ShowStatusIndicatorsTip"));
		list.CheckboxLabeled(L("RCB_Setting_PulseOnDamage"), ref v.pulseOnDamage, L("RCB_Setting_PulseOnDamageTip"));
		if (DefensivePositionsCompat.Active)
		{
			list.CheckboxLabeled(L("RCB_Setting_ShowSquadBadges"), ref v.showSquadBadges, L("RCB_Setting_ShowSquadBadgesTip"));
		}
		if (MarksHost.Available)
		{
			list.CheckboxLabeled(L("RCB_Setting_HostForeignMarks"), ref s.hostForeignMarks, L("RCB_Setting_HostForeignMarksTip"));
			Note(list, L("RCB_Setting_HostForeignMarksNote"));
		}
		Header(list, "RCB_Head_Frames");
		if (list.ButtonTextLabeled(L("RCB_Setting_Frames"), L(FrameModeKeys[Mathf.Clamp(s.frameMode, 0, 2)])))
		{
			s.frameMode = (s.frameMode + 1) % 3;
		}
		Note(list, L("RCB_Setting_FramesNote"));
		bool flag = s.frameMode > 0;
		if (!flag)
		{
			GUI.color = Dim;
		}
		list.Label("   " + L("RCB_Setting_FrameStrength", Mathf.RoundToInt(s.frameStrength * 100f)));
		float num3 = list.Slider(s.frameStrength, 0.2f, 1f);
		if (flag)
		{
			s.frameStrength = Mathf.Round(num3 * 20f) / 20f;
		}
		bool checkOn2 = s.frameWatermark;
		list.CheckboxLabeled("   " + L("RCB_Setting_FrameWatermark"), ref checkOn2, L("RCB_Setting_FrameWatermarkTip"));
		if (flag)
		{
			s.frameWatermark = checkOn2;
		}
		list.Label("   " + L("RCB_Setting_FrameSources"));
		DrawFrameSources(list, s, flag);
		GUI.color = Color.white;
		Header(list, "RCB_Head_Extras");
		list.CheckboxLabeled(L("RCB_Setting_ShowSpeechBubbles"), ref v.showSpeechBubbles, L("RCB_Setting_ShowSpeechBubblesTip"));
		if (v.showSpeechBubbles)
		{
			if (list.ButtonTextLabeled("   " + L("RCB_Setting_SpeechPlacement"), L(SpeechPlaceKeys[Mathf.Clamp(s.speechPlacement, 0, 1)])))
			{
				s.speechPlacement = (s.speechPlacement + 1) % 2;
			}
			Note(list, L("RCB_Setting_SpeechPlacementNote"));
		}
		if (v.showSpeechBubbles)
		{
			list.Label("   " + L("RCB_Setting_BubbleWidth", PxOrAuto(s.bubbleMaxWpx)));
			s.bubbleMaxWpx = Mathf.Round(list.Slider(s.bubbleMaxWpx, 0f, 700f) / 20f) * 20f;
			Note(list, L("RCB_Setting_BubbleWidthNote"));
			list.Label("   " + L("RCB_Setting_BubbleLines", Mathf.RoundToInt(s.bubbleMaxLines)));
			s.bubbleMaxLines = Mathf.Round(list.Slider(s.bubbleMaxLines, 2f, 12f));
			Note(list, L("RCB_Setting_BubbleLinesNote"));
		}
		string[] array = new string[3]
		{
			L("RCB_Setting_WeaponModeNever"),
			L("RCB_Setting_WeaponModeDrafted"),
			L("RCB_Setting_WeaponModeAlways")
		};
		if (list.ButtonTextLabeled(L("RCB_Setting_ShowWeaponIcon"), array[Mathf.Clamp(v.weaponMode, 0, 2)]))
		{
			v.weaponMode = (v.weaponMode + 1) % 3;
			MarkBarDirty();
		}
		if (SidearmsBridge.Active)
		{
			list.CheckboxLabeled("   " + L("RCB_Setting_WeaponPrimaryOnly"), ref v.weaponPrimaryOnly, L("RCB_Setting_WeaponPrimaryOnlyTip"));
		}
		list.CheckboxLabeled(L("RCB_Setting_DraftedWeaponCard"), ref s.draftedWeaponCard, L("RCB_Setting_DraftedWeaponCardTip"));
	}

	private static void TitleColorRow(Listing_Standard list, bool rowOn, ref int src)
	{
		if (!rowOn)
		{
			GUI.color = Dim;
		}
		int num = Mathf.Clamp(src, 0, 4);
		if (list.ButtonTextLabeled("   " + L("RCB_Setting_TitleColor"), L(TitleColor.Keys[num])) && rowOn)
		{
			src = (num + 1) % 5;
		}
		GUI.color = Color.white;
	}

	private static void WhoTab(Listing_Standard list, RadiusColonistBarSettings s)
	{
		Note(list, L("RCB_Setting_RosterIntro"));
		Header(list, "RCB_Head_Paging");
		list.Label((s.barMaxVisible <= 0) ? L("RCB_Setting_MaxVisibleOff") : L("RCB_Setting_MaxVisible", s.barMaxVisible));
		int num = Mathf.RoundToInt(list.Slider((s.barMaxVisible <= 0) ? 2f : ((float)s.barMaxVisible), 2f, 30f));
		int num2 = ((num >= 3) ? num : 0);
		if (num2 != s.barMaxVisible)
		{
			s.barMaxVisible = num2;
			MarkBarDirty();
		}
		Note(list, L("RCB_Setting_MaxVisibleNote"));
		Header(list, "RCB_Head_Order");
		string[] array = new string[4] { "RCB_Sort_Manual", "RCB_Sort_Urgency", "RCB_Sort_Mood", "RCB_Sort_Name" };
		if (list.ButtonTextLabeled(L("RCB_Setting_SortMode"), L(array[Mathf.Clamp(s.barSortMode, 0, 3)])))
		{
			List<FloatMenuOption> list2 = new List<FloatMenuOption>();
			for (int i = 0; i < array.Length; i++)
			{
				int mode = i;
				list2.Add(new FloatMenuOption(L(array[i]), delegate
				{
					s.barSortMode = mode;
					MarkBarDirty();
				}));
			}
			Find.WindowStack.Add(new FloatMenu(list2));
		}
		Note(list, L("RCB_Setting_SortModeNote"));
		Note(list, L("RCB_Setting_PinNote"));
		bool barCohortTabs = s.barCohortTabs;
		list.CheckboxLabeled(L("RCB_Setting_CohortTabs"), ref s.barCohortTabs, L("RCB_Setting_CohortTabsTip"));
		if (barCohortTabs != s.barCohortTabs)
		{
			MarkBarDirty();
		}
		Header(list, "RCB_Head_WhoShows");
		string[] array2 = new string[3] { "RCB_Animals_Off", "RCB_Animals_Bonded", "RCB_Animals_All" };
		if (list.ButtonTextLabeled(L("RCB_Setting_Animals"), L(array2[Mathf.Clamp(s.barAnimalMode, 0, 2)])))
		{
			List<FloatMenuOption> list3 = new List<FloatMenuOption>();
			for (int num3 = 0; num3 < array2.Length; num3++)
			{
				int mode2 = num3;
				list3.Add(new FloatMenuOption(L(array2[num3]), delegate
				{
					s.barAnimalMode = mode2;
					MarkBarDirty();
				}));
			}
			Find.WindowStack.Add(new FloatMenu(list3));
		}
		Note(list, L("RCB_Setting_AnimalsNote"));
		bool barHideSlaves = s.barHideSlaves;
		bool barHideMechs = s.barHideMechs;
		bool barHideCorpses = s.barHideCorpses;
		list.CheckboxLabeled(L("RCB_Setting_HideSlaves"), ref s.barHideSlaves, L("RCB_Setting_HideSlavesTip"));
		list.CheckboxLabeled(L("RCB_Setting_HideMechs"), ref s.barHideMechs, L("RCB_Setting_HideMechsTip"));
		list.CheckboxLabeled(L("RCB_Setting_HideCorpses"), ref s.barHideCorpses, L("RCB_Setting_HideCorpsesTip"));
		if (barHideSlaves != s.barHideSlaves || barHideMechs != s.barHideMechs || barHideCorpses != s.barHideCorpses)
		{
			MarkBarDirty();
		}
		Header(list, "RCB_Head_Hidden");
		BarSquads get = BarSquads.Get;
		if (get == null || get.hidden.Count == 0)
		{
			Note(list, L("RCB_Setting_HiddenNone"));
			return;
		}
		Note(list, L("RCB_Setting_HiddenNote"));
		hiddenScratch.Clear();
		hiddenScratch.AddRange(get.hidden);
		for (int num4 = 0; num4 < hiddenScratch.Count; num4++)
		{
			int id = hiddenScratch[num4];
			if (list.ButtonTextLabeled("   " + get.NameOfHidden(id), L("RCB_Setting_HiddenShow")))
			{
				get.Unhide(id);
			}
		}
		if (hiddenScratch.Count > 1 && list.ButtonText(L("RCB_Setting_HiddenShowAll")))
		{
			get.UnhideAll();
		}
	}

	private static void AlertsTab(Listing_Standard list, RadiusColonistBarSettings s)
	{
		Note(list, L("RCB_Setting_AlertsIntro"));
		list.CheckboxLabeled(L("RCB_Setting_AlertBand"), ref s.alertBand, L("RCB_Setting_AlertBandTip"));
		if (s.alertBand)
		{
			list.Label("   " + ((s.alertCycleMs <= 0) ? L("RCB_Setting_AlertCycleStatic") : L("RCB_Setting_AlertCycle", ((float)s.alertCycleMs / 1000f).ToString("0.0"))));
			int num = Mathf.RoundToInt(list.Slider((s.alertCycleMs <= 0) ? 900f : ((float)s.alertCycleMs), 900f, 10000f) / 100f) * 100;
			s.alertCycleMs = ((num >= 1000) ? num : 0);
			Note(list, L("RCB_Setting_AlertCycleNote"));
			list.CheckboxLabeled("   " + L("RCB_Setting_AlertInspired"), ref s.alertInspired, L("RCB_Setting_AlertInspiredTip"));
			if (ModLister.GetActiveModWithIdentifier("astryl.radiusui.missioncontrol", ignorePostfix: true) != null || ModLister.GetActiveModWithIdentifier("astryl.modernnotifications", ignorePostfix: true) != null)
			{
				list.CheckboxLabeled("   " + L("RCB_Setting_BandStandsIn"), ref s.bandStandsIn, L("RCB_Setting_BandStandsInTip"));
			}
			list.Label("   " + L("RCB_Setting_BandOpacity", Mathf.RoundToInt(s.bandOpacityPct)));
			s.bandOpacityPct = Mathf.Round(list.Slider(s.bandOpacityPct, 20f, 100f) / 2f) * 2f;
			Note(list, L("RCB_Setting_BandOpacityNote"));
		}
	}

	private static void PortraitsTab(Listing_Standard list, RadiusColonistBarSettings s)
	{
		Header(list, "RCB_Head_PortraitMode");
		string buttonLabel = L(s.useLiveCameras ? "RCB_Setting_RenderLiveCameras" : "RCB_Setting_RenderLivingPortraits");
		if (list.ButtonTextLabeled(L("RCB_Setting_RenderMode"), buttonLabel))
		{
			s.useLiveCameras = !s.useLiveCameras;
			MarkBarDirty();
		}
		Note(list, L(s.useLiveCameras ? "RCB_Setting_LiveCamerasWarning" : "RCB_Setting_LivingPortraitsNote"));
		Header(list, "RCB_Head_Framing");
		if (s.useLiveCameras)
		{
			list.Label(L("RCB_Setting_LiveZoom", s.liveZoom.ToString("0.00")));
			s.liveZoom = Mathf.Round(list.Slider(s.liveZoom, 0.4f, 3f) * 20f) / 20f;
			list.Label(L("RCB_Setting_LivePan", s.livePanX.ToString("0.00"), s.livePanY.ToString("0.00")));
			s.livePanX = Mathf.Round(list.Slider(s.livePanX, -2f, 2f) * 20f) / 20f;
			s.livePanY = Mathf.Round(list.Slider(s.livePanY, -2f, 2f) * 20f) / 20f;
			list.CheckboxLabeled(L("RCB_Setting_LiveWholeMap"), ref s.liveWholeMap, L("RCB_Setting_LiveWholeMapTip"));
		}
		else
		{
			list.Label(L("RCB_Setting_PortraitZoom", s.portraitZoom.ToString("0.00")));
			s.portraitZoom = Mathf.Round(list.Slider(s.portraitZoom, 0.4f, 4f) * 20f) / 20f;
			DrawRaceFraming(list, s);
			list.Label(L("RCB_Setting_PortraitPan", s.portraitPanX.ToString("0.00"), s.portraitPanY.ToString("0.00")));
			s.portraitPanX = Mathf.Round(list.Slider(s.portraitPanX, -2f, 2f) * 20f) / 20f;
			s.portraitPanY = Mathf.Round(list.Slider(s.portraitPanY, -2f, 2f) * 20f) / 20f;
		}
		Note(list, L("RCB_Setting_DuetNote"));
		Header(list, "RCB_Head_Quality");
		int num = QualityIndex(s.liveResolution);
		if (list.ButtonTextLabeled(L("RCB_Setting_Quality"), L(QualityKeys[num])))
		{
			List<FloatMenuOption> list2 = new List<FloatMenuOption>();
			for (int i = 0; i < QualityKeys.Length; i++)
			{
				int ii = i;
				list2.Add(new FloatMenuOption(L(QualityKeys[ii]), delegate
				{
					s.liveResolution = QualityPx[ii];
					MarkBarDirty();
				}));
			}
			Find.WindowStack.Add(new FloatMenu(list2));
		}
		Note(list, L("RCB_Setting_QualityNote"));
		if (ColorCodedMoodBarBridge.Active || FacialAnimationBridge.Active)
		{
			Header(list, "RCB_Head_Compat");
		}
		if (ColorCodedMoodBarBridge.Active)
		{
			list.CheckboxLabeled(L("RCB_Setting_UseCcmb"), ref s.useColorCodedMoodBar, L("RCB_Setting_UseCcmbTip"));
		}
		if (!FacialAnimationBridge.Active)
		{
			return;
		}
		bool faPortraitAnimation = s.faPortraitAnimation;
		list.CheckboxLabeled(L("RCB_Setting_FaPortraits"), ref s.faPortraitAnimation, L("RCB_Setting_FaPortraitsTip"));
		if (faPortraitAnimation != s.faPortraitAnimation)
		{
			MarkBarDirty();
		}
		if (s.faPortraitAnimation)
		{
			if (FacialAnimationBridge.FaDrivesPortraits)
			{
				Note(list, L("RCB_Setting_FaRateDeferred", FacialAnimationBridge.FaOwnInterval));
				return;
			}
			int num2 = Mathf.Clamp(s.faPortraitInterval, 1, 15);
			list.Label("   " + L("RCB_Setting_FaRate", Mathf.Max(1, Mathf.RoundToInt(60f / (float)num2))));
			s.faPortraitInterval = Mathf.RoundToInt(list.Slider(num2, 1f, 15f));
		}
	}

	private static void DrawFrameSources(Listing_Standard list, RadiusColonistBarSettings s, bool on)
	{
		List<int> list2 = s.frameSources;
		if (list2 == null)
		{
			list2 = (s.frameSources = FrameSource.DefaultOrder());
		}
		for (int i = 0; i < list2.Count; i++)
		{
			Rect rect = list.GetRect(24f);
			Rect rect2 = new Rect(rect.x + 24f, rect.y, rect.width - 24f - 60f, rect.height);
			Widgets.Label(rect2, "   " + L(FrameSource.Keys[Mathf.Clamp(list2[i], 0, 3)]));
			Rect rect3 = new Rect(rect.xMax - 58f, rect.y + 2f, 26f, 20f);
			Rect rect4 = new Rect(rect.xMax - 28f, rect.y + 2f, 26f, 20f);
			if (i > 0 && Widgets.ButtonText(rect3, "▲") && on)
			{
				int value = list2[i - 1];
				list2[i - 1] = list2[i];
				list2[i] = value;
			}
			if (Widgets.ButtonText(rect4, "−") && on)
			{
				list2.RemoveAt(i);
				break;
			}
		}
		for (int j = 0; j < 4; j++)
		{
			if (!list2.Contains(j))
			{
				Rect rect5 = list.GetRect(24f);
				Rect rect6 = new Rect(rect5.x + 24f, rect5.y, rect5.width - 24f - 60f, rect5.height);
				Color color = GUI.color;
				GUI.color = Dim;
				Widgets.Label(rect6, "   " + L(FrameSource.Keys[j]));
				GUI.color = color;
				Rect rect7 = new Rect(rect5.xMax - 28f, rect5.y + 2f, 26f, 20f);
				if (Widgets.ButtonText(rect7, "+") && on)
				{
					list2.Add(j);
				}
			}
		}
	}

	private static int QualityIndex(int px)
	{
		int result = 1;
		for (int i = 0; i < QualityPx.Length; i++)
		{
			if (px >= QualityPx[i])
			{
				result = i;
			}
		}
		return result;
	}

	private static void HoverTab(Listing_Standard list, RadiusColonistBarSettings s)
	{
		list.CheckboxLabeled(L("RCB_Setting_PortraitHoverInfo"), ref s.portraitHoverInfo, L("RCB_Setting_PortraitHoverInfoTip"));
		if (!s.portraitHoverInfo)
		{
			return;
		}
		list.Label(L("RCB_Setting_PopoutDelay", s.popoutDelay.ToString("0.00")), -1f, (TipSignal?)L("RCB_Setting_PopoutDelayTip"));
		s.popoutDelay = Mathf.Round(list.Slider(s.popoutDelay, 0f, 1f) * 20f) / 20f;
		Header(list, "RCB_Head_PopoutLook");
		list.CheckboxLabeled(L("RCB_Setting_PopoutOpacityLinked"), ref s.popoutOpacityLinked, L("RCB_Setting_PopoutOpacityLinkedTip"));
		bool popoutOpacityLinked = s.popoutOpacityLinked;
		if (popoutOpacityLinked)
		{
			GUI.color = Dim;
		}
		float num = (popoutOpacityLinked ? (Mathf.Clamp(s.barOpacityPct, 20f, 100f) / 100f) : s.popoutOpacity);
		list.Label("   " + L("RCB_Setting_PopoutOpacity", Mathf.RoundToInt(num * 100f)));
		float num2 = list.Slider(num, 0.2f, 1f);
		if (!popoutOpacityLinked)
		{
			s.popoutOpacity = Mathf.Round(num2 * 100f) / 100f;
		}
		GUI.color = Color.white;
		if (popoutOpacityLinked)
		{
			Note(list, L("RCB_Setting_PopoutOpacityLinkedNote", Mathf.RoundToInt(s.barOpacityPct)));
		}
		list.Label(L("RCB_Setting_PopoutScale", s.popoutScale.ToString("0.00")));
		s.popoutScale = Mathf.Round(list.Slider(s.popoutScale, 0.7f, 1.5f) * 20f) / 20f;
		list.Label(L("RCB_Setting_PopoutPad", Mathf.RoundToInt(s.popoutPad)));
		s.popoutPad = Mathf.Round(list.Slider(s.popoutPad, 0f, 24f));
		list.Label(L("RCB_Setting_PopoutOffset", Mathf.RoundToInt(s.popoutOffsetX), Mathf.RoundToInt(s.popoutOffsetY)));
		s.popoutOffsetX = Mathf.Round(list.Slider(s.popoutOffsetX, -300f, 300f));
		s.popoutOffsetY = Mathf.Round(list.Slider(s.popoutOffsetY, -300f, 300f));
		Header(list, "RCB_Head_QuickActions");
		list.CheckboxLabeled(L("RCB_Setting_QaShow"), ref s.qaShow, L("RCB_Setting_QaShowTip"));
		if (s.qaShow)
		{
			list.CheckboxLabeled("   " + L("RCB_Setting_QaDraft"), ref s.qaDraft);
			list.CheckboxLabeled("   " + L("RCB_Setting_QaJump"), ref s.qaJump);
			if (AdaptiveWorkBridge.Active)
			{
				list.CheckboxLabeled("   " + L("RCB_Setting_QaAdaptiveWork"), ref s.qaAdaptiveWork, L("RCB_Setting_QaAdaptiveWorkTip"));
			}
			list.CheckboxLabeled("   " + L("RCB_Setting_QaHealth"), ref s.qaHealth);
			list.CheckboxLabeled("   " + L("RCB_Setting_QaBio"), ref s.qaBio);
			list.CheckboxLabeled("   " + L("RCB_Setting_QaSocial"), ref s.qaSocial);
			list.CheckboxLabeled("   " + L("RCB_Setting_QaGear"), ref s.qaGear);
			list.CheckboxLabeled("   " + L("RCB_Setting_QaPolicies"), ref s.qaPolicies);
			list.CheckboxLabeled("   " + L("RCB_Setting_QaRename"), ref s.qaRename);
			if (DefensivePositionsCompat.Active)
			{
				list.CheckboxLabeled("   " + L("RCB_Setting_QaDefPos"), ref s.qaDefPos);
			}
		}
		if (Prefs.DevMode)
		{
			Header(list, "RCB_Head_Diag");
			list.CheckboxLabeled(L("RCB_Setting_PopoutDiag"), ref s.popoutDiag, L("RCB_Setting_PopoutDiagTip"));
		}
	}

	private static void ExtrasTab(Listing_Standard list, RadiusColonistBarSettings s)
	{
		Header(list, "RCB_Head_Camera");
		list.CheckboxLabeled(L("RCB_Setting_FollowCam"), ref s.followCam, L("RCB_Setting_FollowCamTip"));
		if (s.followCam)
		{
			list.CheckboxLabeled("   " + L("RCB_Setting_FollowCamStabilize"), ref s.followCamStabilize, L("RCB_Setting_FollowCamStabilizeTip"));
			list.Label("   " + L("RCB_Setting_FollowCamSmooth", s.followCamSmooth.ToString("0.00")));
			s.followCamSmooth = Mathf.Round(list.Slider(s.followCamSmooth, 0.02f, 0.5f) * 100f) / 100f;
			Note(list, L("RCB_Setting_FollowCamNote"));
		}
		Header(list, "RCB_Head_Perf");
		int num = Mathf.Clamp(s.perfSnapshotFrames, 5, 60);
		list.Label(L("RCB_Setting_InfoRefresh", Mathf.Max(1, Mathf.RoundToInt(60f / (float)num))));
		s.perfSnapshotFrames = Mathf.RoundToInt(list.Slider(num, 5f, 60f));
		Note(list, L("RCB_Setting_InfoRefreshNote"));
		if (s.useLiveCameras)
		{
			list.Label(L("RCB_Setting_CameraBudget", s.liveRendersPerFrame));
			s.liveRendersPerFrame = Mathf.RoundToInt(list.Slider(s.liveRendersPerFrame, 1f, 8f));
			Note(list, L("RCB_Setting_CameraBudgetNote"));
		}
	}

	private static void StatDropdown(Listing_Standard list, string label, string cur, Action<string> set, bool allowNone = false)
	{
		if (!list.ButtonTextLabeled(label, StatLabel(cur)))
		{
			return;
		}
		List<FloatMenuOption> list2 = new List<FloatMenuOption>();
		if (allowNone)
		{
			list2.Add(new FloatMenuOption(L("RCB_Stat_None"), delegate
			{
				set("__none__");
			}));
		}
		list2.Add(new FloatMenuOption(L("RCB_Stat_Health"), delegate
		{
			set("__health__");
		}));
		list2.Add(new FloatMenuOption(L("RCB_Stat_Mood"), delegate
		{
			set("__mood__");
		}));
		list2.Add(new FloatMenuOption(L("RCB_Stat_Readiness"), delegate
		{
			set("__readiness__");
		}));
		if (ModsConfig.RoyaltyActive)
		{
			list2.Add(new FloatMenuOption(L("RCB_Stat_Psyfocus"), delegate
			{
				set("__psyfocus__");
			}));
			list2.Add(new FloatMenuOption(L("RCB_Stat_Entropy"), delegate
			{
				set("__entropy__");
			}));
		}
		if (MmbBridge.Active)
		{
			list2.Add(new FloatMenuOption(L("RCB_Stat_Mmb"), delegate
			{
				set("__mmb__");
			}));
		}
		foreach (NeedDef item in DefDatabase<NeedDef>.AllDefsListForReading)
		{
			NeedDef d = item;
			list2.Add(new FloatMenuOption(d.LabelCap.ToString(), delegate
			{
				set(d.defName);
			}));
		}
		Find.WindowStack.Add(new FloatMenu(list2));
	}

	private static string StatLabel(string key)
	{
		if (key == "__none__")
		{
			return L("RCB_Stat_None");
		}
		if (string.IsNullOrEmpty(key) || key == "__health__")
		{
			return L("RCB_Stat_Health");
		}
		switch (key)
		{
		case "__mood__":
			return L("RCB_Stat_Mood");
		case "__readiness__":
			return L("RCB_Stat_Readiness");
		case "__mmb__":
			return L("RCB_Stat_Mmb");
		case "__psyfocus__":
			return L("RCB_Stat_Psyfocus");
		case "__entropy__":
			return L("RCB_Stat_Entropy");
		default:
		{
			NeedDef namedSilentFail = DefDatabase<NeedDef>.GetNamedSilentFail(key);
			return (namedSilentFail != null) ? namedSilentFail.LabelCap.ToString() : key;
		}
		}
	}

	private static void MarkBarDirty()
	{
		Patch_ColonistBarSize.InvalidateLayout();
		if (Current.ProgramState == ProgramState.Playing)
		{
			Find.ColonistBar?.MarkColonistsDirty();
		}
	}
}
