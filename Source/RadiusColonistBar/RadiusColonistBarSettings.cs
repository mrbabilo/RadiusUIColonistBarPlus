using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public class RadiusColonistBarSettings : ModSettings
{
	public bool masterEnabled = true;

	public bool islandCollapse = true;

	public int islandAnimMs = 520;

	public const int DefaultAnimMs = 520;

	public float islandCollapseDelaySec = 10f;

	public const float DefaultCollapseDelaySec = 10f;

	public bool collapsedShowCount = true;

	public bool showConditionMarkers = true;

	public int conditionPulseAt = 5;

	public bool alertBand = true;

	public int alertCycleMs = 4000;

	public bool alertInspired = true;

	public bool hostForeignMarks = true;

	public float islandMaxWidthPct = 86f;

	public float openMaxHpx = 0f;

	public float barPosXPct = 50f;

	public const float DefaultBarPosXPct = 50f;

	public float bubbleMaxWpx = 0f;

	public float bubbleMaxLines = 6f;

	public float collapsedMaxWpx = 0f;

	public float collapsedMaxHpx = 0f;

	public int cardLayout = 0;

	[Unsaved(false)]
	public int previewState = 0;

	public float barOpacityPct = 92f;

	public const float DefaultBarOpacityPct = 92f;

	public float bandOpacityPct = 100f;

	public const float DefaultBandOpacityPct = 100f;

	public float cardOpacityPct = 100f;

	public const float DefaultCardOpacityPct = 100f;

	public bool jobLabelHoverOnly;

	public bool jobTitleFallbackOnly;

	public int titleColorRoyal = 0;

	public int titleColorIdeo = 4;

	public int titleColorJob = 3;

	public bool draftedWeaponCard = true;

	public int frameMode;

	public float frameStrength = 0.75f;

	public bool frameWatermark = true;

	public List<int> frameSources = FrameSource.DefaultOrder();

	public int speechPlacement;

	public int healthMeterStyle;

	public bool bandStandsIn = true;

	public int statePillMode;

	public bool barCohortTabs = true;

	public int barAnimalMode;

	public int barSortMode;

	public int barMaxVisible = 5;

	public const int DefaultMaxVisible = 5;

	public bool useLiveCameras = false;

	public float portraitZoom = 1.28f;

	public float portraitPanX = 0f;

	public float portraitPanY = 0.3f;

	public List<RaceFraming> raceFraming = new List<RaceFraming>();

	public bool portraitHoverInfo = true;

	public float popoutOpacity = 0.97f;

	public bool popoutOpacityLinked = true;

	public float popoutScale = 1f;

	// Colonist Bar Plus: seconds of hover before the pop-out opens.
	public float popoutDelay = 0.25f;

	public float popoutPad = 8f;

	public float popoutOffsetX = 0f;

	public float popoutOffsetY = 0f;

	public bool qaShow = true;

	public bool qaDraft = true;

	public bool qaJump = true;

	public bool qaHealth = true;

	public bool qaBio = true;

	public bool qaSocial = true;

	public bool qaGear = true;

	public bool qaRename = false;

	public bool popoutDiag = false;

	public bool qaPolicies = true;

	public bool qaDefPos = true;

	public bool qaAdaptiveWork = true;

	public bool followCam = true;

	public float followCamSmooth = 0.12f;

	public bool followCamStabilize = true;

	public bool useColorCodedMoodBar = true;

	public bool faPortraitAnimation = true;

	public int faPortraitInterval = 3;

	public float portraitScale = 1f;

	public float horizontalPadding = 0f;

	public float verticalPadding = 0f;

	public float barTopOffset = 0f;

	public bool barHideSlaves = false;

	public bool barHideMechs = false;

	public bool barHideCorpses = false;

	public int perfSnapshotFrames = 15;

	public bool liveWholeMap = true;

	public float liveZoom = 2.4f;

	public float livePanX = 0f;

	public float livePanY = 0f;

	public int liveResolution = 192;

	public int liveRendersPerFrame = 3;

	public bool ccRailCollapsed;

	public ViewStyle style;

	public const int CurrentSettingsVersion = 2;

	public int settingsVersion = 2;

	public float EffectiveCardOpacity => Mathf.Clamp(RcbGuard.Sane(cardOpacityPct, 100f), 20f, 100f) / 100f;

	public float EffectiveBandOpacity => Mathf.Clamp(RcbGuard.Sane(bandOpacityPct, 100f), 20f, 100f) / 100f;

	public bool barShowBondedAnimals => barAnimalMode != 0;

	public bool liveView
	{
		get
		{
			return true;
		}
		set
		{
		}
	}

	public float EffectivePopoutOpacity => popoutOpacityLinked ? (Mathf.Clamp(barOpacityPct, 20f, 100f) / 100f) : popoutOpacity;

	public BarLayout activeView => BarLayout.TopBar;

	public ViewStyle Current
	{
		get
		{
			EnsureViews();
			return style;
		}
	}

	public bool partyPanel
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public bool warbandMode
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public bool showName
	{
		get
		{
			return Current.showName;
		}
		set
		{
			Current.showName = value;
		}
	}

	public bool showRoyalTitle
	{
		get
		{
			return Current.showRoyalTitle;
		}
		set
		{
			Current.showRoyalTitle = value;
		}
	}

	public bool showJobTitle
	{
		get
		{
			return Current.showJobTitle;
		}
		set
		{
			Current.showJobTitle = value;
		}
	}

	public bool showIdeoRole
	{
		get
		{
			return Current.showIdeoRole;
		}
		set
		{
			Current.showIdeoRole = value;
		}
	}

	public bool showJobLabel
	{
		get
		{
			return Current.showJobLabel;
		}
		set
		{
			Current.showJobLabel = value;
		}
	}

	public bool showActivityIcons
	{
		get
		{
			return Current.showActivityIcons;
		}
		set
		{
			Current.showActivityIcons = value;
		}
	}

	public float activityIconScale
	{
		get
		{
			return Current.activityIconScale;
		}
		set
		{
			Current.activityIconScale = value;
		}
	}

	public bool showStatusIndicators
	{
		get
		{
			return Current.showStatusIndicators;
		}
		set
		{
			Current.showStatusIndicators = value;
		}
	}

	public bool showSquadBadges
	{
		get
		{
			return Current.showSquadBadges;
		}
		set
		{
			Current.showSquadBadges = value;
		}
	}

	public bool showSpeechBubbles
	{
		get
		{
			return Current.showSpeechBubbles;
		}
		set
		{
			Current.showSpeechBubbles = value;
		}
	}

	public string barTopStat
	{
		get
		{
			return Current.barTopStat;
		}
		set
		{
			Current.barTopStat = value;
		}
	}

	public string barBottomStat
	{
		get
		{
			return Current.barBottomStat;
		}
		set
		{
			Current.barBottomStat = value;
		}
	}

	public int weaponMode
	{
		get
		{
			return Current.weaponMode;
		}
		set
		{
			Current.weaponMode = value;
		}
	}

	public bool weaponPrimaryOnly
	{
		get
		{
			return Current.weaponPrimaryOnly;
		}
		set
		{
			Current.weaponPrimaryOnly = value;
		}
	}

	public bool pulseOnDamage
	{
		get
		{
			return Current.pulseOnDamage;
		}
		set
		{
			Current.pulseOnDamage = value;
		}
	}

	public bool warbandHideSelected
	{
		get
		{
			return Current.warbandHideSelected;
		}
		set
		{
			Current.warbandHideSelected = value;
		}
	}

	public bool warbandHidePerspective
	{
		get
		{
			return Current.warbandHidePerspective;
		}
		set
		{
			Current.warbandHidePerspective = value;
		}
	}

	private void EnsureViews()
	{
		if (style == null)
		{
			style = ViewStyle.Default(BarLayout.TopBar);
		}
	}

	public ViewStyle StyleFor(BarLayout v)
	{
		EnsureViews();
		return style;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref masterEnabled, "masterEnabled", defaultValue: true);
		Scribe_Values.Look(ref islandCollapse, "islandCollapse", defaultValue: false);
		Scribe_Values.Look(ref islandAnimMs, "islandAnimMs", 330);
		islandAnimMs = Mathf.Clamp(islandAnimMs, 0, 1000);
		Scribe_Values.Look(ref islandCollapseDelaySec, "islandCollapseDelaySec", 0f);
		islandCollapseDelaySec = Mathf.Clamp(RcbGuard.Sane(islandCollapseDelaySec, 0f), 0f, 300f);
		Scribe_Values.Look(ref collapsedShowCount, "collapsedShowCount", defaultValue: true);
		Scribe_Values.Look(ref showConditionMarkers, "showConditionMarkers", defaultValue: true);
		Scribe_Values.Look(ref conditionPulseAt, "conditionPulseAt", 5);
		conditionPulseAt = Mathf.Clamp(conditionPulseAt, 0, 12);
		Scribe_Values.Look(ref alertBand, "alertBand", defaultValue: true);
		Scribe_Values.Look(ref alertCycleMs, "alertCycleMs", 4000);
		if (alertCycleMs != 0)
		{
			alertCycleMs = Mathf.Clamp(alertCycleMs, 1000, 10000);
		}
		Scribe_Values.Look(ref alertInspired, "alertInspired", defaultValue: true);
		Scribe_Values.Look(ref hostForeignMarks, "hostForeignMarks", defaultValue: true);
		Scribe_Values.Look(ref islandMaxWidthPct, "islandMaxWidthPct", 86f);
		islandMaxWidthPct = Mathf.Clamp(RcbGuard.Sane(islandMaxWidthPct, 86f), 30f, 100f);
		Scribe_Values.Look(ref openMaxHpx, "openMaxHpx", 0f);
		openMaxHpx = Mathf.Clamp(RcbGuard.Sane(openMaxHpx, 0f), 0f, 2000f);
		Scribe_Values.Look(ref collapsedMaxWpx, "collapsedMaxWpx", 0f);
		collapsedMaxWpx = Mathf.Clamp(RcbGuard.Sane(collapsedMaxWpx, 0f), 0f, 3000f);
		Scribe_Values.Look(ref collapsedMaxHpx, "collapsedMaxHpx", 0f);
		collapsedMaxHpx = Mathf.Clamp(RcbGuard.Sane(collapsedMaxHpx, 0f), 0f, 300f);
		Scribe_Values.Look(ref cardLayout, "cardLayout", 0);
		cardLayout = Mathf.Clamp(cardLayout, 0, 1);
		Scribe_Values.Look(ref barOpacityPct, "barOpacityPct", 92f);
		barOpacityPct = Mathf.Clamp(RcbGuard.Sane(barOpacityPct, 92f), 20f, 100f);
		Scribe_Values.Look(ref cardOpacityPct, "cardOpacityPct", 100f);
		cardOpacityPct = Mathf.Clamp(RcbGuard.Sane(cardOpacityPct, 100f), 20f, 100f);
		Scribe_Values.Look(ref jobLabelHoverOnly, "jobLabelHoverOnly", defaultValue: false);
		Scribe_Values.Look(ref jobTitleFallbackOnly, "jobTitleFallbackOnly", defaultValue: false);
		Scribe_Values.Look(ref statePillMode, "statePillMode", 0);
		statePillMode = Mathf.Clamp(statePillMode, 0, 2);
		Scribe_Values.Look(ref titleColorRoyal, "titleColorRoyal", 0);
		Scribe_Values.Look(ref titleColorIdeo, "titleColorIdeo", 4);
		Scribe_Values.Look(ref titleColorJob, "titleColorJob", 3);
		titleColorRoyal = Mathf.Clamp(titleColorRoyal, 0, 4);
		titleColorIdeo = Mathf.Clamp(titleColorIdeo, 0, 4);
		titleColorJob = Mathf.Clamp(titleColorJob, 0, 4);
		Scribe_Values.Look(ref draftedWeaponCard, "draftedWeaponCard", defaultValue: true);
		Scribe_Values.Look(ref frameMode, "frameMode", 0);
		frameMode = Mathf.Clamp(frameMode, 0, 2);
		Scribe_Values.Look(ref frameStrength, "frameStrength", 0.75f);
		frameStrength = Mathf.Clamp(RcbGuard.Sane(frameStrength, 0.75f), 0.2f, 1f);
		Scribe_Values.Look(ref frameWatermark, "frameWatermark", defaultValue: true);
		Scribe_Collections.Look(ref frameSources, "frameSources", LookMode.Value);
		if (frameSources == null)
		{
			frameSources = FrameSource.DefaultOrder();
		}
		else
		{
			frameSources.RemoveAll((int v) => v < 0 || v >= 4);
		}
		Scribe_Values.Look(ref speechPlacement, "speechPlacement", 0);
		speechPlacement = Mathf.Clamp(speechPlacement, 0, 1);
		Scribe_Values.Look(ref healthMeterStyle, "healthMeterStyle", 0);
		healthMeterStyle = Mathf.Clamp(healthMeterStyle, 0, 1);
		Scribe_Values.Look(ref bandStandsIn, "bandStandsIn", defaultValue: true);
		Scribe_Values.Look(ref bandOpacityPct, "bandOpacityPct", 100f);
		bandOpacityPct = Mathf.Clamp(RcbGuard.Sane(bandOpacityPct, 100f), 20f, 100f);
		Scribe_Values.Look(ref bubbleMaxWpx, "bubbleMaxWpx", 0f);
		bubbleMaxWpx = Mathf.Clamp(RcbGuard.Sane(bubbleMaxWpx, 0f), 0f, 700f);
		Scribe_Values.Look(ref bubbleMaxLines, "bubbleMaxLines", 6f);
		bubbleMaxLines = Mathf.Clamp(RcbGuard.Sane(bubbleMaxLines, 6f), 2f, 12f);
		Scribe_Values.Look(ref barPosXPct, "barPosXPct", 50f);
		barPosXPct = Mathf.Clamp(RcbGuard.Sane(barPosXPct, 50f), 0f, 100f);
		Scribe_Values.Look(ref ccRailCollapsed, "ccRailCollapsed", defaultValue: false);
		Scribe_Values.Look(ref barCohortTabs, "barCohortTabs", defaultValue: true);
		Scribe_Values.Look(ref barAnimalMode, "barAnimalMode", 0);
		barAnimalMode = Mathf.Clamp(barAnimalMode, 0, 2);
		bool value = false;
		Scribe_Values.Look(ref value, "barShowBondedAnimals", defaultValue: false);
		if (Scribe.mode == LoadSaveMode.LoadingVars && value && barAnimalMode == 0)
		{
			barAnimalMode = 1;
		}
		Scribe_Values.Look(ref barSortMode, "barSortMode", 0);
		Scribe_Values.Look(ref barMaxVisible, "barMaxVisible", 0);
		if (barMaxVisible != 0)
		{
			barMaxVisible = Mathf.Clamp(barMaxVisible, 3, 30);
		}
		Scribe_Values.Look(ref settingsVersion, "settingsVersion", 0);
		barSortMode = Mathf.Clamp(barSortMode, 0, 3);
		bool value2 = true;
		Scribe_Values.Look(ref value2, "liveView", defaultValue: true);
		Scribe_Values.Look(ref useLiveCameras, "useLiveCameras", defaultValue: true);
		Scribe_Values.Look(ref portraitZoom, "portraitZoom", 1.28f);
		Scribe_Values.Look(ref portraitPanX, "portraitPanX", 0f);
		Scribe_Values.Look(ref portraitPanY, "portraitPanY", 0.3f);
		Scribe_Collections.Look(ref raceFraming, "raceFraming", LookMode.Deep);
		if (raceFraming == null)
		{
			raceFraming = new List<RaceFraming>();
		}
		RaceFramingTable.Invalidate();
		Scribe_Values.Look(ref portraitHoverInfo, "portraitHoverInfo", defaultValue: true);
		Scribe_Values.Look(ref popoutOpacity, "popoutOpacity", 0.97f);
		Scribe_Values.Look(ref popoutOpacityLinked, "popoutOpacityLinked", defaultValue: true);
		Scribe_Values.Look(ref popoutScale, "popoutScale", 1f);
		Scribe_Values.Look(ref popoutDelay, "popoutDelay", 0.25f);
		Scribe_Values.Look(ref popoutPad, "popoutPad", 8f);
		Scribe_Values.Look(ref popoutOffsetX, "popoutOffsetX", 0f);
		Scribe_Values.Look(ref popoutOffsetY, "popoutOffsetY", 0f);
		Scribe_Values.Look(ref qaShow, "qaShow", defaultValue: true);
		Scribe_Values.Look(ref qaDraft, "qaDraft", defaultValue: true);
		Scribe_Values.Look(ref qaJump, "qaJump", defaultValue: true);
		Scribe_Values.Look(ref qaHealth, "qaHealth", defaultValue: true);
		Scribe_Values.Look(ref qaBio, "qaBio", defaultValue: true);
		Scribe_Values.Look(ref qaSocial, "qaSocial", defaultValue: true);
		Scribe_Values.Look(ref qaGear, "qaGear", defaultValue: true);
		Scribe_Values.Look(ref qaRename, "qaRename", defaultValue: false);
		Scribe_Values.Look(ref popoutDiag, "popoutDiag", defaultValue: false);
		Scribe_Values.Look(ref qaPolicies, "qaPolicies", defaultValue: true);
		Scribe_Values.Look(ref qaDefPos, "qaDefPos", defaultValue: true);
		Scribe_Values.Look(ref qaAdaptiveWork, "qaAdaptiveWork", defaultValue: true);
		Scribe_Values.Look(ref followCam, "followCam", defaultValue: true);
		Scribe_Values.Look(ref followCamSmooth, "followCamSmooth", 0.12f);
		Scribe_Values.Look(ref followCamStabilize, "followCamStabilize", defaultValue: true);
		Scribe_Values.Look(ref useColorCodedMoodBar, "useColorCodedMoodBar", defaultValue: true);
		Scribe_Values.Look(ref faPortraitAnimation, "faPortraitAnimation", defaultValue: true);
		Scribe_Values.Look(ref faPortraitInterval, "faPortraitInterval", 3);
		faPortraitInterval = Mathf.Clamp(faPortraitInterval, 1, 15);
		Scribe_Values.Look(ref portraitScale, "portraitScale", 1f);
		Scribe_Values.Look(ref horizontalPadding, "horizontalPadding", 0f);
		Scribe_Values.Look(ref verticalPadding, "verticalPadding", 0f);
		Scribe_Values.Look(ref barTopOffset, "barTopOffset", 0f);
		Scribe_Values.Look(ref barHideSlaves, "barHideSlaves", defaultValue: false);
		Scribe_Values.Look(ref barHideMechs, "barHideMechs", defaultValue: false);
		Scribe_Values.Look(ref barHideCorpses, "barHideCorpses", defaultValue: false);
		bool value3 = false;
		bool value4 = true;
		Scribe_Values.Look(ref value3, "groupBySituation", defaultValue: false);
		Scribe_Values.Look(ref value4, "showBarControls", defaultValue: true);
		Scribe_Values.Look(ref perfSnapshotFrames, "perfSnapshotFrames", 15);
		perfSnapshotFrames = Mathf.Clamp(perfSnapshotFrames, 5, 60);
		Scribe_Values.Look(ref liveWholeMap, "liveWholeMap", defaultValue: true);
		Scribe_Values.Look(ref liveZoom, "liveZoomFactor", 2.4f);
		liveZoom = Mathf.Clamp(RcbGuard.Sane(liveZoom, 2.4f), 0.4f, 3f);
		Scribe_Values.Look(ref livePanX, "livePanX", 0f);
		Scribe_Values.Look(ref livePanY, "livePanY", 0f);
		Scribe_Values.Look(ref liveResolution, "liveResolution", 128);
		liveResolution = Mathf.Clamp(liveResolution, 96, 512);
		Scribe_Values.Look(ref liveRendersPerFrame, "liveRendersPerFrame", 3);
		liveRendersPerFrame = Mathf.Clamp(liveRendersPerFrame, 1, 8);
		int value5 = 0;
		Scribe_Values.Look(ref value5, "activeView", 0);
		List<ViewStyle> list = null;
		Scribe_Collections.Look(ref list, "views", LookMode.Deep);
		Scribe_Deep.Look(ref style, "style");
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			if (style == null)
			{
				if (list != null && list.Count > 0 && list[0] != null)
				{
					style = list[0];
				}
				else
				{
					MigrateLegacyViews();
				}
			}
			bool value6 = false;
			bool value7 = false;
			Scribe_Values.Look(ref value6, "partyPanel", defaultValue: false);
			Scribe_Values.Look(ref value7, "warbandMode", defaultValue: false);
			if (settingsVersion < 1)
			{
				if (barMaxVisible == 0)
				{
					barMaxVisible = 5;
				}
				if (!islandCollapse)
				{
					islandCollapse = true;
					if (islandCollapseDelaySec <= 0f)
					{
						islandCollapseDelaySec = 10f;
					}
				}
				useLiveCameras = false;
				if (style != null)
				{
					style.showSpeechBubbles = true;
				}
			}
			if (settingsVersion < 2 && islandAnimMs == 330)
			{
				islandAnimMs = 520;
			}
			settingsVersion = 2;
		}
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
		}
		portraitZoom = RcbGuard.Sane(portraitZoom, 1.28f);
		portraitPanX = RcbGuard.Sane(portraitPanX, 0f);
		portraitPanY = RcbGuard.Sane(portraitPanY, 0.3f);
		popoutOpacity = RcbGuard.Sane(popoutOpacity, 0.97f);
		popoutScale = RcbGuard.Sane(popoutScale, 1f);
		popoutDelay = RcbGuard.Sane(popoutDelay, 0.25f);
		popoutPad = RcbGuard.Sane(popoutPad, 8f);
		popoutOffsetX = RcbGuard.Sane(popoutOffsetX, 0f);
		popoutOffsetY = RcbGuard.Sane(popoutOffsetY, 0f);
		followCamSmooth = RcbGuard.Sane(followCamSmooth, 0.12f);
		portraitScale = RcbGuard.Sane(portraitScale, 1f);
		horizontalPadding = RcbGuard.Sane(horizontalPadding, 0f);
		verticalPadding = RcbGuard.Sane(verticalPadding, 0f);
		barTopOffset = RcbGuard.Sane(barTopOffset, 0f);
		livePanX = RcbGuard.Sane(livePanX, 0f);
		livePanY = RcbGuard.Sane(livePanY, 0f);
		EnsureViews();
	}

	private void MigrateLegacyViews()
	{
		ViewStyle viewStyle = style ?? new ViewStyle();
		Scribe_Values.Look(ref viewStyle.showName, "showName", defaultValue: true);
		Scribe_Values.Look(ref viewStyle.showJobTitle, "showTitle", defaultValue: true);
		Scribe_Values.Look(ref viewStyle.showIdeoRole, "showIdeoRole", defaultValue: true);
		Scribe_Values.Look(ref viewStyle.showJobLabel, "showJobLabel", defaultValue: true);
		Scribe_Values.Look(ref viewStyle.showActivityIcons, "showActivityIcons", defaultValue: false);
		Scribe_Values.Look(ref viewStyle.activityIconScale, "activityIconScale", 0.6f);
		Scribe_Values.Look(ref viewStyle.showStatusIndicators, "showStatusIndicators", defaultValue: true);
		Scribe_Values.Look(ref viewStyle.showSquadBadges, "showSquadBadges", defaultValue: true);
		Scribe_Values.Look(ref viewStyle.showSpeechBubbles, "showSpeechBubbles", defaultValue: false);
		Scribe_Values.Look(ref viewStyle.barTopStat, "barTopStat", "__health__");
		Scribe_Values.Look(ref viewStyle.barBottomStat, "barBottomStat", "__mood__");
		Scribe_Values.Look(ref viewStyle.weaponMode, "weaponMode", 1);
		Scribe_Values.Look(ref viewStyle.weaponPrimaryOnly, "weaponPrimaryOnly", defaultValue: false);
		Scribe_Values.Look(ref viewStyle.pulseOnDamage, "pulseOnDamage", defaultValue: true);
		Scribe_Values.Look(ref viewStyle.warbandHideSelected, "warbandHideSelected", defaultValue: true);
		Scribe_Values.Look(ref viewStyle.warbandHidePerspective, "warbandHidePerspective", defaultValue: true);
		style = viewStyle;
	}
}
