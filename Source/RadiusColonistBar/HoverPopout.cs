using System;
using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
public static class HoverPopout
{
	private struct QAction
	{
		public string label;

		public string shortLabel;

		public Texture2D icon;

		public int count;

		public int badge;

		public Action act;
	}

	private sealed class Layout
	{
		public Pawn pawn;

		public Rect panel;

		public float infoColW;

		public float headerH;

		public float taskH;

		public float buttonsH;

		public float contentW;

		public float statusH;

		public float cellsH;

		public string name;

		public string royalTitle;

		public string jobTitle;

		public string role;

		public string task;

		public string nameHdr;

		public float hdrH;

		public float mood;

		public float health;

		public int age;

		public string moodPct;

		public string healthPct;

		public bool hasRoyalTitle;

		public bool hasJobTitle;

		public bool hasRole;

		public bool hasMood;

		public int groupCount;

		public List<Pawn> groupPawns;

		public float baseW;

		public float baseH;

		public float scale = 1f;

		public readonly List<PawnStatusUtil.Status> statuses = new List<PawnStatusUtil.Status>();

		public readonly List<string> progLines = new List<string>();

		public readonly List<Color> progColors = new List<Color>();
	}

	private sealed class PopoutWindow : Window
	{
		public Layout lay;

		public float alpha;

		public RadiusColonistBarSettings stg;

		protected override float Margin => 0f;

		public override Vector2 InitialSize => new Vector2(lay?.baseW ?? 10f, lay?.baseH ?? 10f);

		public PopoutWindow()
		{
			layer = WindowLayer.Super;
			doWindowBackground = false;
			drawShadow = false;
			absorbInputAroundWindow = false;
			closeOnClickedOutside = false;
			closeOnCancel = false;
			closeOnAccept = false;
			focusWhenOpened = false;
			preventCameraMotion = false;
			soundAppear = null;
			soundClose = null;
		}

		protected override void SetInitialSizeAndPosition()
		{
		}

		public override void WindowOnGUI()
		{
			float num = ((lay != null) ? lay.scale : 1f);
			if (lay == null || Mathf.Approximately(num, 1f))
			{
				base.WindowOnGUI();
				return;
			}
			if (num >= 0.01f && RcbGuard.Finite(windowRect.x) && RcbGuard.Finite(windowRect.y) && RcbGuard.Finite(lay.baseW) && RcbGuard.Finite(lay.baseH))
			{
				Rect rect = windowRect;
				windowRect = new Rect(rect.x, rect.y, lay.baseW, lay.baseH);
				Matrix4x4 matrix = GUI.matrix;
				Vector3 vector = new Vector3(windowRect.x, windowRect.y, 0f);
				GUI.matrix = matrix * Matrix4x4.TRS(vector, Quaternion.identity, Vector3.one) * Matrix4x4.Scale(new Vector3(num, num, 1f)) * Matrix4x4.TRS(-vector, Quaternion.identity, Vector3.one);
				try
				{
					base.WindowOnGUI();
					return;
				}
				finally
				{
					GUI.matrix = matrix;
					windowRect = new Rect(rect.x, rect.y, Mathf.Max(rect.width, lay.baseW), Mathf.Max(rect.height, lay.baseH));
				}
			}
			string[] obj = new string[8]
			{
				"scale=",
				num.ToString(),
				" windowRect=",
				null,
				null,
				null,
				null,
				null
			};
			Rect rect2 = windowRect;
			obj[3] = rect2.ToString();
			obj[4] = " baseW=";
			obj[5] = lay.baseW.ToString();
			obj[6] = " baseH=";
			obj[7] = lay.baseH.ToString();
			RcbGuard.WarnOnce("HoverPopout.WindowOnGUI", string.Concat(obj));
		}

		public override void DoWindowContents(Rect inRect)
		{
			if (lay == null)
			{
				return;
			}
			try
			{
				DrawPanel(lay, new Rect(0f, 0f, lay.baseW, lay.baseH), alpha, stg);
			}
			catch
			{
			}
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			if (Time.frameCount - drawFrame > 3)
			{
				Close(doCloseSound: false);
			}
		}
	}

	private static readonly List<TlEvent> tlChips = new List<TlEvent>(8);

	private static readonly List<TlEvent> tlFuture = new List<TlEvent>(4);

	private const float PortW = 74f;

	private const float PortH = 100f;

	private const float Gap = 8f;

	private const float BtnGap = 4f;

	private const float BtnH = 30f;

	private const float BtnMinW = 100f;

	private const float HdrLineH = 15f;

	private const float WordLineH = 22f;

	private const float CapLineH = 16f;

	private const float HdrTopInset = 4f;

	private const float CellH = 44f;

	private const float CellGap = 6f;

	private const float SecGap = 8f;

	private const int BtnCols = 3;

	private const int BtnMax = 9;

	private static Pawn pawn;

	private static Rect anchor;

	private static int frame = -1;

	private static float startTime;

	private static float lastAlive;

	private static float suppressUntil;

	private static int layFrame = -1;

	private static Layout layCache;

	private static readonly List<QAction> actions = new List<QAction>();

	private static Texture2D _icDraft;

	private static Texture2D _icJump;

	private static Texture2D _icHealth;

	private static Texture2D _icBio;

	private static Texture2D _icSocial;

	private static Texture2D _icGear;

	private static Texture2D _icRename;

	private static Texture2D _icDefPos;

	private static Texture2D _icPolicies;

	private static Texture2D _icWork;

	private static bool _icInit;

	private static readonly Color NameCol = RcbPalette.Stat;

	private static readonly Color DimCol = RcbPalette.TextDim;

	private static readonly Color RoyalCol = RcbPalette.TextDim;

	private static readonly Color JobCol = RcbPalette.TextDim;

	private static readonly Color SuiteGood = RcbPalette.Good;

	private static readonly Color SuiteWarn = RcbPalette.Warn;

	private static readonly Color SuiteBad = RcbPalette.Bad;

	private static PopoutWindow popWin;

	internal static int drawFrame = -1;

	private const int LayoutRefreshFrames = 6;

	private static readonly Color PanelInk = RcbPalette.Ink;

	private static float Pad => Mathf.Clamp(RadiusColonistBarMod.Settings?.popoutPad ?? 8f, 4f, 20f);

	private static float HeaderLineH()
	{
		GameFont font = Text.Font;
		Text.Font = GameFont.Small;
		float num = Mathf.Ceil(Text.LineHeight);
		Text.Font = font;
		return Mathf.Max(15f, num + 2f);
	}

	public static void Record(Pawn p, Rect cellPortrait)
	{
		if (p != null)
		{
			if (p != pawn || Time.frameCount - frame > 1)
			{
				startTime = Time.realtimeSinceStartup;
			}
			pawn = p;
			anchor = cellPortrait;
			frame = Time.frameCount;
		}
	}

	public static void Draw()
	{
		if (pawn == null)
		{
			return;
		}
		if (Time.realtimeSinceStartup < suppressUntil)
		{
			Release();
			return;
		}
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled || !settings.liveView || !settings.portraitHoverInfo || pawn.Dead || !pawn.Spawned)
		{
			Release();
			return;
		}
		Layout layout = Compute(pawn, anchor, settings);
		bool flag = Time.frameCount - frame <= 2;
		bool flag2 = layout.panel.Contains(Event.current.mousePosition);
		if (flag || flag2)
		{
			lastAlive = Time.realtimeSinceStartup;
		}
		if (Time.realtimeSinceStartup - lastAlive > 0.22f)
		{
			Release();
			return;
		}
		float num = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / 0.14f);
		float num2 = 1f - (1f - num) * (1f - num);
		drawFrame = Time.frameCount;
		Rect windowRect = new Rect(Mathf.Round(layout.panel.x), Mathf.Round(layout.panel.y + (1f - num2) * 10f), Mathf.Round(layout.panel.width), Mathf.Round(layout.panel.height));
		if (popWin == null || !popWin.IsOpen)
		{
			popWin = new PopoutWindow();
			popWin.windowRect = windowRect;
			popWin.lay = layout;
			popWin.alpha = num2;
			popWin.stg = settings;
			Find.WindowStack.Add(popWin);
		}
		else
		{
			popWin.lay = layout;
			popWin.alpha = num2;
			popWin.stg = settings;
			popWin.windowRect = windowRect;
		}
	}

	private static void Release()
	{
		pawn = null;
		layCache = null;
		layFrame = -1;
		if (popWin == null)
		{
			return;
		}
		try
		{
			if (popWin.IsOpen)
			{
				popWin.Close(doCloseSound: false);
			}
		}
		catch
		{
		}
		popWin = null;
	}

	private static Layout Compute(Pawn p, Rect cell, RadiusColonistBarSettings s)
	{
		if (layCache != null && layCache.pawn == p)
		{
			int num = Time.frameCount - layFrame;
			if (num == 0)
			{
				return layCache;
			}
			if (num < 6)
			{
				PositionPanel(layCache, cell, s);
				return layCache;
			}
		}
		Layout layout = new Layout
		{
			pawn = p
		};
		layout.name = p.LabelShortCap;
		layout.royalTitle = RoyalTitleLine(p);
		layout.jobTitle = JobTitleLine(p);
		layout.role = RoleLine(p);
		layout.task = TaskLine(p);
		layout.mood = MoodFrac(p);
		layout.health = HealthFrac(p);
		layout.age = AgeYears(p);
		layout.moodPct = ((layout.mood >= 0f) ? Mathf.Clamp01(layout.mood).ToStringPercent() : null);
		layout.healthPct = Mathf.Clamp01(layout.health).ToStringPercent();
		layout.hasRoyalTitle = !layout.royalTitle.NullOrEmpty();
		layout.hasJobTitle = !layout.jobTitle.NullOrEmpty();
		layout.hasRole = !layout.role.NullOrEmpty();
		layout.hasMood = layout.mood >= 0f;
		Text.Font = GameFont.Small;
		try
		{
			string text = p.Name?.ToStringFull;
			if (!string.IsNullOrEmpty(text))
			{
				layout.name = text;
			}
		}
		catch
		{
		}
		layout.nameHdr = layout.name ?? "";
		float a = RcbFont.Width(layout.nameHdr, bold: true, GameFont.Small);
		float num2 = (layout.hasRoyalTitle ? Text.CalcSize(layout.royalTitle).x : 0f);
		if (layout.hasJobTitle)
		{
			num2 = Mathf.Max(num2, Text.CalcSize(layout.jobTitle).x);
		}
		if (layout.hasRole)
		{
			num2 = Mathf.Max(num2, Text.CalcSize(layout.role).x);
		}
		layout.infoColW = Mathf.Clamp(Mathf.Max(Mathf.Max(a, num2) + 6f, 132f), 132f, 330f);
		float num3 = Pad + 74f + 8f + layout.infoColW + Pad;
		if (s != null && s.showStatusIndicators)
		{
			PawnStatusUtil.Gather(p, layout.statuses);
		}
		try
		{
			List<Hediff> list = p.health?.hediffSet?.hediffs;
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					Hediff hediff = list[i];
					if (hediff?.def == null || hediff.TryGetComp<HediffComp_Immunizable>() == null)
					{
						continue;
					}
					float num4 = 0f;
					try
					{
						num4 = p.health.immunity.GetImmunity(hediff.def);
					}
					catch
					{
					}
					if (!(num4 >= 0.999f))
					{
						float num5 = Mathf.Clamp01((hediff.def.lethalSeverity > 0f) ? (hediff.Severity / hediff.def.lethalSeverity) : hediff.Severity);
						if (!(num5 < 0.01f) || !(num4 < 0.01f))
						{
							float num6 = num4 - num5;
							layout.progLines.Add(hediff.LabelBase.CapitalizeFirst() + " " + Mathf.RoundToInt(num5 * 100f) + "% vs immunity " + Mathf.RoundToInt(num4 * 100f) + "%");
							layout.progColors.Add((num6 > 0.05f) ? SuiteGood : ((num6 > -0.05f) ? SuiteWarn : SuiteBad));
						}
					}
				}
			}
		}
		catch
		{
		}
		Text.Font = GameFont.Small;
		for (int j = 0; j < layout.statuses.Count; j++)
		{
			num3 = Mathf.Max(num3, RcbText.CalcSize(layout.statuses[j].text).x + 22f + Pad * 2f);
		}
		for (int k = 0; k < layout.progLines.Count; k++)
		{
			num3 = Mathf.Max(num3, RcbText.CalcSize(layout.progLines[k]).x + 22f + Pad * 2f);
		}
		layout.statusH = 0f;
		if (layout.statuses.Count > 0)
		{
			tlChips.Clear();
			for (int l = 0; l < layout.statuses.Count; l++)
			{
				tlChips.Add(new TlEvent
				{
					title = layout.statuses[l].text,
					col = layout.statuses[l].color
				});
			}
			float plateWidth = Mathf.Max(120f, num3 - Pad * 2f - 24f);
			layout.statusH += RcbTimeline.NowClusterH(RcbTimeline.ChipRows(tlChips, plateWidth)) + 10f;
		}
		if (layout.progLines.Count > 0)
		{
			layout.statusH += 20f + (float)layout.progLines.Count * 42f;
		}
		try
		{
			List<Pawn> list2 = Find.Selector?.SelectedPawns;
			if (list2 != null && list2.Count > 1 && list2.Contains(p))
			{
				layout.groupCount = list2.Count;
				layout.groupPawns = new List<Pawn>(list2);
			}
		}
		catch
		{
		}
		num3 = Mathf.Min(num3, 380f);
		layout.contentW = num3 - Pad * 2f;
		layout.hdrH = HeaderLineH();
		float b = 4f + layout.hdrH + 22f + ((layout.hasRoyalTitle || layout.hasJobTitle || layout.hasRole) ? 16f : 0f);
		layout.headerH = Mathf.Max(100f, b);
		layout.cellsH = 94f;
		if (!layout.task.NullOrEmpty())
		{
			Text.Font = GameFont.Small;
			layout.taskH = Mathf.Min(Text.CalcHeight(layout.task, layout.contentW), 56f) + 4f;
		}
		BuildActions(p, s);
		int count = actions.Count;
		if (count > 0)
		{
			int num7 = Mathf.Min(3, count);
			num3 = Mathf.Max(num3, (float)num7 * 100f + (float)(num7 - 1) * 4f + Pad * 2f);
		}
		num3 = Mathf.Min(num3, 460f);
		layout.contentW = num3 - Pad * 2f;
		if (count > 0)
		{
			int num8 = Mathf.Min(3, count);
			int num9 = Mathf.CeilToInt((float)count / (float)num8);
			layout.buttonsH = (float)num9 * 34f + 2f;
		}
		Text.Font = GameFont.Small;
		float baseH = Pad + layout.headerH + 6f + layout.cellsH + ((layout.statusH > 0f) ? (8f + layout.statusH) : 0f) + ((layout.taskH > 0f) ? (8f + layout.taskH) : 0f) + ((layout.buttonsH > 0f) ? (8f + layout.buttonsH) : 0f) + Pad;
		float scale = Mathf.Clamp(s?.popoutScale ?? 1f, 0.7f, 1.5f);
		layout.baseW = num3;
		layout.baseH = baseH;
		layout.scale = scale;
		PositionPanel(layout, cell, s);
		layCache = layout;
		layFrame = Time.frameCount;
		return layout;
	}

	private static void PositionPanel(Layout lay, Rect cell, RadiusColonistBarSettings s)
	{
		float num = lay.baseW * lay.scale;
		float num2 = lay.baseH * lay.scale;
		float num3;
		float num4;
		if (s != null && s.partyPanel && s.liveView)
		{
			num3 = cell.xMax + 6f;
			if (num3 + num > (float)UI.screenWidth - 4f)
			{
				num3 = cell.x - num - 6f;
			}
			num4 = Mathf.Clamp(cell.center.y - num2 * 0.5f, 4f, (float)UI.screenHeight - num2 - 4f);
		}
		else
		{
			num3 = Mathf.Clamp(cell.center.x - num * 0.5f, 4f, (float)UI.screenWidth - num - 4f);
			num4 = cell.yMax + 6f;
			if (num4 + num2 > (float)UI.screenHeight - 4f)
			{
				num4 = cell.y - num2 - 6f;
			}
		}
		if (s != null)
		{
			num3 += s.popoutOffsetX;
			num4 += s.popoutOffsetY;
		}
		num3 = Mathf.Clamp(num3, 4f, Mathf.Max(4f, (float)UI.screenWidth - num - 4f));
		num4 = Mathf.Clamp(num4, 4f, Mathf.Max(4f, (float)UI.screenHeight - num2 - 4f));
		lay.panel = new Rect(num3, num4, num, num2);
	}

	private static void InitIcons()
	{
		if (!_icInit)
		{
			_icInit = true;
			_icDraft = IconSet.Action.Draft;
			_icJump = IconSet.Action.Jump;
			_icHealth = IconSet.Alert.Info;
			_icBio = IconSet.Action.Bio;
			_icSocial = IconSet.Action.Social;
			_icGear = IconSet.Action.Apparel;
			_icRename = IconSet.Action.Rename;
			_icPolicies = IconSet.Action.Policies;
			_icDefPos = IconSet.Action.DefPos;
			_icWork = IconSet.Action.Grid;
		}
	}

	private static void BuildActions(Pawn p, RadiusColonistBarSettings s)
	{
		actions.Clear();
		if (!s.qaShow)
		{
			return;
		}
		InitIcons();
		if (s.qaDraft && p.drafter != null)
		{
			int num = SelectionIfMulti(p)?.Count ?? 0;
			// Colonist Bar Plus: quick action labels translated (vanilla keys where the game has one).
			string text = (p.Drafted ? "CommandUndraftLabel" : "CommandDraftLabel").Translate();
			string label = text + ((num > 1) ? (" " + "RCB_Qa_Selected".Translate(num)) : "");
			actions.Add(new QAction
			{
				label = label,
				shortLabel = text,
				icon = _icDraft,
				count = num,
				act = delegate
				{
					DoDraft(p);
				}
			});
		}
		if (s.qaJump)
		{
			actions.Add(new QAction
			{
				label = "RCB_Qa_JumpTip".Translate(),
				shortLabel = "RCB_Qa_Jump".Translate(),
				icon = _icJump,
				act = delegate
				{
					try
					{
						CameraJumper.TryJumpAndSelect(p);
					}
					catch
					{
					}
				}
			});
		}
		if (s.qaAdaptiveWork && p.IsColonistPlayerControlled && AdaptiveWorkBridge.Active)
		{
			int num2 = AdaptiveWorkBridge.PendingCount(p);
			actions.Add(new QAction
			{
				label = ((num2 > 0) ? ((string)"RCB_Qa_WorkTipPending".Translate(num2)) : ((string)"RCB_Qa_WorkTip".Translate())),
				shortLabel = "RCB_Qa_Work".Translate(),
				icon = _icWork,
				badge = num2,
				act = delegate
				{
					OpenAdaptiveCard(p);
				}
			});
		}
		if (s.qaHealth)
		{
			actions.Add(TabAction("TabHealth".Translate(), _icHealth, typeof(ITab_Pawn_Health), p));
		}
		if (s.qaBio)
		{
			actions.Add(TabAction("TabCharacter".Translate(), _icBio, typeof(ITab_Pawn_Character), p));
		}
		if (s.qaSocial)
		{
			actions.Add(TabAction("TabSocial".Translate(), _icSocial, typeof(ITab_Pawn_Social), p));
		}
		if (s.qaGear)
		{
			actions.Add(TabAction("TabGear".Translate(), _icGear, typeof(ITab_Pawn_Gear), p));
		}
		if (s.qaRename)
		{
			actions.Add(new QAction
			{
				label = "Rename".Translate(),
				shortLabel = "Rename".Translate(),
				icon = _icRename,
				act = delegate
				{
					try
					{
						Find.WindowStack.Add(p.NamePawnDialog());
					}
					catch
					{
					}
				}
			});
		}
		if (s.qaPolicies && p.IsColonistPlayerControlled)
		{
			actions.Add(new QAction
			{
				label = "RCB_Qa_PoliciesTip".Translate(),
				shortLabel = "RCB_Qa_Policies".Translate(),
				icon = _icPolicies,
				act = delegate
				{
					try
					{
						Patch_HandleClicks_RightClickMenu.OpenPoliciesMenu(p);
					}
					catch
					{
					}
				}
			});
		}
		if (s.qaDefPos && DefensivePositionsCompat.Active)
		{
			int num3 = SelectionIfMulti(p)?.Count ?? 0;
			actions.Add(new QAction
			{
				label = "RCB_Qa_DefendTip".Translate((num3 > 1) ? (" (" + num3 + ")") : ""),
				shortLabel = "RCB_Qa_Defend".Translate(),
				icon = _icDefPos,
				count = num3,
				act = delegate
				{
					List<Pawn> list = SelectionIfMulti(p);
					if (list != null)
					{
						Pawn[] array = list.ToArray();
						Pawn[] array2 = array;
						foreach (Pawn p2 in array2)
						{
							DefensivePositionsCompat.SendOne(p2);
						}
					}
					else
					{
						DefensivePositionsCompat.SendOne(p);
					}
				}
			});
		}
		if (actions.Count > 9)
		{
			actions.RemoveRange(9, actions.Count - 9);
		}
	}

	private static List<Pawn> SelectionIfMulti(Pawn p)
	{
		try
		{
			List<Pawn> selectedPawns = Find.Selector.SelectedPawns;
			if (selectedPawns != null && selectedPawns.Count > 1 && selectedPawns.Contains(p))
			{
				return selectedPawns;
			}
		}
		catch
		{
		}
		return null;
	}

	private static void OpenAdaptiveCard(Pawn p)
	{
		Rect rect = ((layCache != null) ? layCache.panel : anchor);
		Release();
		suppressUntil = Time.realtimeSinceStartup + 0.35f;
		if (!AdaptiveWorkBridge.OpenCard(p, rect.x, rect.y))
		{
			Messages.Message("RCB_Qa_WorkUntracked".Translate(p.LabelShortCap), MessageTypeDefOf.RejectInput, historical: false);
		}
	}

	private static void DoDraft(Pawn p)
	{
		try
		{
			bool drafted = !p.Drafted;
			List<Pawn> list = SelectionIfMulti(p);
			if (list != null)
			{
				Pawn[] array = list.ToArray();
				Pawn[] array2 = array;
				foreach (Pawn pawn in array2)
				{
					if (pawn?.drafter != null)
					{
						try
						{
							pawn.drafter.Drafted = drafted;
						}
						catch
						{
						}
					}
				}
			}
			else
			{
				p.drafter.Drafted = drafted;
			}
		}
		catch
		{
		}
	}

	private static QAction TabAction(string label, Texture2D icon, Type tabType, Pawn p)
	{
		return new QAction
		{
			label = label,
			shortLabel = label,
			icon = icon,
			act = delegate
			{
				try
				{
					Find.Selector.ClearSelection();
					Find.Selector.Select(p);
					InspectPaneUtility.OpenTab(tabType);
				}
				catch
				{
				}
			}
		};
	}

	private static void DrawPanel(Layout lay, Rect panel, float a, RadiusColonistBarSettings s)
	{
		float num = Mathf.Clamp01(s?.EffectivePopoutOpacity ?? 1f);
		if (num > 0.004f)
		{
			Color c = Palette.WithAlpha(PanelInk, 0.94f * num);
			RcbSpatial.Rounded(panel, Alpha(c, a), 18f);
		}
		Pawn pawn = lay.pawn;
		if (s != null && s.frameMode == 2 && pawn != null && lay.groupCount <= 1)
		{
			PawnUiCache.Snap snap = PawnUiCache.Get(pawn);
			if (snap.hasFrame)
			{
				Color color = GUI.color;
				GUI.color = new Color(1f, 1f, 1f, a);
				if (s.frameWatermark && snap.frameEmblem != null)
				{
					GUI.BeginGroup(panel);
					try
					{
						Rect panel2 = new Rect(0f, 0f, panel.width, panel.height);
						UIKit.Flat.Watermark(panel2, snap.frameEmblem, snap.frameHue, 0.08f, Mathf.Min(panel.height, 160f));
					}
					finally
					{
						GUI.EndGroup();
					}
				}
				UIKit.Flat.Frame(panel, snap.frameHue, s.frameStrength, snap.frameGlyph);
				GUI.color = color;
			}
		}
		float num2 = panel.y + Pad;
		Rect rect = new Rect(panel.x + Pad, num2, 74f, 100f);
		if (lay.groupCount > 1 && lay.groupPawns != null)
		{
			DrawGroupGrid(rect, lay, a);
		}
		else
		{
			Texture portraitTexture = LiveViewManager.GetPortraitTexture(pawn, new Vector2(74f * LiveViewManager.PortraitK, 100f * LiveViewManager.PortraitK));
			if (portraitTexture != null)
			{
				GUI.color = new Color(1f, 1f, 1f, a);
				GUI.DrawTexture(rect, portraitTexture);
				GUI.color = Color.white;
			}
		}
		float num3 = rect.xMax + 8f;
		float num4 = Mathf.Max(40f, panel.xMax - Pad - num3);
		float num5 = ((lay.hdrH > 0f) ? lay.hdrH : HeaderLineH());
		float num6 = num2 + 4f;
		string text = RcbFont.Fit(lay.nameHdr, num4, bold: true, GameFont.Small);
		RcbFont.Label(new Rect(num3, num6, num4, num5), text, bold: true, GameFont.Small, Alpha(RcbPalette.TextDim2, a));
		if (Prefs.DevMode && s != null && s.popoutDiag)
		{
			bool flag = text != lay.nameHdr;
			float num7 = RcbFont.Width(lay.nameHdr, bold: true, GameFont.Small);
			Color col = (GUI.color = (flag ? RcbPalette.Bad : RcbPalette.Good));
			Widgets.DrawBox(new Rect(num3, num6, num4, num5));
			Widgets.DrawBox(panel);
			GUI.color = Color.white;
			RcbPalette.MicroLabel(new Rect(num3, num6 + num5, num4 + 200f, 12f), "need " + num7.ToString("0") + " / have " + num4.ToString("0") + " | col " + lay.infoColW.ToString("0") + " | panel " + panel.width.ToString("0") + " | cut " + (flag ? "YES" : "no") + " | len " + lay.nameHdr.Length, col, TextAnchor.UpperLeft, shadow: false);
		}
		num6 += num5;
		string s2 = ((!lay.task.NullOrEmpty()) ? lay.task : "RCB_State_Idle".Translate().ToString());
		Color c2 = RcbPalette.Stat;
		RcbState rcbState = CardPalette.State(pawn);
		if (rcbState == RcbState.Bad)
		{
			c2 = RcbPalette.Bad;
		}
		else if (rcbState == RcbState.Warn || pawn.Drafted)
		{
			c2 = RcbPalette.Warn;
		}
		RcbFont.Label(new Rect(num3, num6, num4, 22f), RcbFont.Fit(s2, num4, bold: true, GameFont.Small), bold: true, GameFont.Small, Alpha(c2, a));
		num6 += 22f;
		string text2 = (lay.hasRoyalTitle ? lay.royalTitle : (lay.hasJobTitle ? lay.jobTitle : (lay.hasRole ? lay.role : null)));
		if (!text2.NullOrEmpty())
		{
			RcbPalette.MicroLabel(new Rect(num3, num6, num4, 16f), RcbFont.Fit(text2, num4, bold: false, GameFont.Small), Alpha(RcbPalette.TextDim2, a), TextAnchor.MiddleLeft, shadow: false);
		}
		float num8 = panel.x + Pad;
		float num9 = panel.width - Pad * 2f;
		float num10 = num2 + lay.headerH + 6f;
		float num11 = (num9 - 6f) * 0.5f;
		if (lay.hasMood)
		{
			ValueCell(new Rect(num8, num10, num11, 44f), "RCB_Cell_Mood".Translate(), lay.moodPct, CardPalette.MoodColor(pawn, lay.mood), a);
			ValueCell(new Rect(num8 + num11 + 6f, num10, num11, 44f), "RCB_Cell_Health".Translate(), lay.healthPct, CardPalette.ZoneColor(lay.health), a);
		}
		else
		{
			ValueCell(new Rect(num8, num10, num9, 44f), "RCB_Cell_Health".Translate(), lay.healthPct, CardPalette.ZoneColor(lay.health), a);
		}
		WeaponCell(new Rect(num8, num10 + 44f + 6f, num9, 44f), pawn, s, a);
		float num12 = num2 + lay.headerH + 6f + lay.cellsH;
		float num13 = num12 + ((lay.statusH > 0f) ? 8f : 0f);
		float num14 = num13 + lay.statusH + ((lay.taskH > 0f) ? 8f : 0f);
		float y = num14 + lay.taskH + ((lay.buttonsH > 0f) ? 8f : 0f);
		float num15 = num13;
		Rect area = new Rect(panel.x + Pad, num15, lay.contentW, lay.statusH);
		tlChips.Clear();
		for (int i = 0; i < lay.statuses.Count; i++)
		{
			tlChips.Add(new TlEvent
			{
				title = lay.statuses[i].text,
				col = lay.statuses[i].color,
				icon = lay.statuses[i].icon
			});
		}
		tlFuture.Clear();
		for (int j = 0; j < lay.progLines.Count; j++)
		{
			tlFuture.Add(new TlEvent
			{
				title = lay.progLines[j],
				col = ((j < lay.progColors.Count) ? lay.progColors[j] : RcbPalette.Accent),
				projected = true
			});
		}
		if (tlChips.Count > 0)
		{
			num15 = RcbTimeline.DrawNow(area, num15, null, tlChips, RcbTimeline.ChipRows(tlChips, area.width - 24f));
		}
		if (tlFuture.Count > 0)
		{
			RcbTimeline.Spine(new Rect(area.x, num15, area.width, Mathf.Max(0f, area.yMax - num15)));
			num15 = RcbTimeline.DrawRule(area, num15, "Still to come");
			for (int k = 0; k < tlFuture.Count; k++)
			{
				num15 = RcbTimeline.DrawProjected(area, num15, tlFuture[k], k);
			}
		}
		if (lay.taskH > 0f)
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			Label(new Rect(panel.x + Pad, num14, lay.contentW, lay.taskH - 4f), lay.task, JobCol, a);
		}
		if (lay.buttonsH > 0f)
		{
			DrawButtons(new Rect(panel.x + Pad, y, lay.contentW, lay.buttonsH), a);
		}
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		if (Event.current.type == EventType.MouseDown && Mouse.IsOver(panel))
		{
			Event.current.Use();
		}
	}

	private static void DrawGroupGrid(Rect area, Layout lay, float a)
	{
		int count = lay.groupPawns.Count;
		int num = Mathf.Max(1, Mathf.FloorToInt(area.width / 30f));
		int num2 = Mathf.Max(1, Mathf.CeilToInt((float)count / (float)num));
		float num3 = Mathf.Min((area.width - (float)(num - 1) * 2f) / (float)num, (area.height - (float)(num2 - 1) * 2f) / (float)num2);
		float num4 = (float)num * num3 + (float)(num - 1) * 2f;
		float num5 = area.x + (area.width - num4) * 0.5f;
		for (int i = 0; i < count; i++)
		{
			int num6 = i % num;
			int num7 = i / num;
			Rect rect = new Rect(num5 + (float)num6 * (num3 + 2f), area.y + (float)num7 * (num3 + 2f), num3, num3);
			Pawn pawn = lay.groupPawns[i];
			Texture texture = ((pawn != null) ? LiveViewManager.GetPortraitTexture(pawn, new Vector2(num3 * LiveViewManager.PortraitK, num3 * LiveViewManager.PortraitK)) : null);
			bool flag = pawn == lay.pawn;
			Color innerFill = new Color(0f, 0f, 0f, 0.22f * a);
			if (flag)
			{
				RcbSpatial.Ring(rect, Alpha(CardPalette.Accent, a), innerFill);
			}
			else
			{
				RcbSpatial.Well(rect, 0.22f * a);
			}
			Rect position = (flag ? rect.ContractedBy(2f) : rect);
			if (texture != null)
			{
				GUI.color = new Color(1f, 1f, 1f, a);
				GUI.DrawTexture(position, texture);
				GUI.color = Color.white;
			}
		}
	}

	private static void DrawButtons(Rect area, float a)
	{
		int num = Mathf.Min(3, Mathf.Max(1, actions.Count));
		float num2 = (area.width - (float)(num - 1) * 4f) / (float)num;
		float num3 = area.x;
		float num4 = area.y;
		int num5 = 0;
		foreach (QAction action in actions)
		{
			if (DrawLabeledButton(new Rect(num3, num4, num2, 30f), action.icon, action.shortLabel ?? action.label, action.label, action.count, action.badge, a))
			{
				action.act?.Invoke();
			}
			if (++num5 >= num)
			{
				num5 = 0;
				num3 = area.x;
				num4 += 34f;
			}
			else
			{
				num3 += num2 + 4f;
			}
		}
	}

	private static bool DrawLabeledButton(Rect r, Texture2D icon, string label, string tip, int count, int badge, float a)
	{
		bool flag = Mouse.IsOver(r);
		Plate(r, flag ? 0.16f : 0.08f, a);
		float num = r.x + 8f;
		if (icon != null)
		{
			Rect position = new Rect(r.x + 6f, r.center.y - 9f, 18f, 18f);
			GUI.color = Alpha(flag ? Color.white : DimCol, a);
			GUI.DrawTexture(position, icon);
			GUI.color = Color.white;
			num = position.xMax + 5f;
		}
		float num2 = Mathf.Max(10f, r.xMax - num - 6f);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		Label(new Rect(num, r.y, num2, r.height), RcbFont.Fit(label ?? "", num2, bold: false, GameFont.Small), flag ? NameCol : DimCol, a);
		int num3 = ((badge > 0) ? badge : ((count > 1) ? count : 0));
		if (num3 > 0)
		{
			Text.Anchor = TextAnchor.UpperRight;
			Label(new Rect(r.x, r.y + 1f, r.width - 3f, 14f), SmallStrings.Int(num3), (badge > 0) ? Palette.Flat.AlertInk : DimCol, a);
			Text.Anchor = TextAnchor.MiddleLeft;
		}
		if (flag && !tip.NullOrEmpty())
		{
			TooltipHandler.TipRegion(r, tip);
		}
		return Widgets.ButtonInvisible(r);
	}

	private static void Plate(Rect r, float lift, float a)
	{
		Color c = Color.Lerp(PanelInk, Color.white, Mathf.Clamp01(lift));
		float t = Mathf.Clamp01(RadiusColonistBarMod.Settings?.EffectivePopoutOpacity ?? 1f);
		c.a = a * Mathf.Lerp(0.55f, 1f, t);
		RcbSpatial.Rounded(r, c);
	}

	private static void ValueCell(Rect r, string caption, string value, Color valCol, float a)
	{
		if (RcbGuard.Finite(r))
		{
			Plate(r, 0.055f, a);
			float num = 9f;
			RcbPalette.MicroLabel(new Rect(r.x + num, r.y + 6f, r.width - num * 2f, 12f), caption, Alpha(RcbPalette.TextDim2, a), TextAnchor.MiddleLeft, shadow: false);
			RcbFont.Label(new Rect(r.x + num, r.y + 19f, r.width - num * 2f, 20f), value ?? "-", bold: true, GameFont.Medium, Alpha(valCol, a));
		}
	}

	private static void WeaponCell(Rect r, Pawn p, RadiusColonistBarSettings s, float a)
	{
		if (RcbGuard.Finite(r) && s != null)
		{
			Plate(r, 0.055f, a);
			float num = 9f;
			RcbPalette.MicroLabel(new Rect(r.x + num, r.y + 6f, r.width - num * 2f, 12f), "RCB_Cell_Weapon".Translate(), Alpha(RcbPalette.TextDim2, a), TextAnchor.MiddleLeft, shadow: false);
			float num2 = RcbWeaponChips.Height(p, s, 1f);
			if (num2 > 0f)
			{
				float num3 = 62f;
				RcbWeaponChips.Draw(r.x + num + num3 * 0.5f, r.y + 21f, p, s, 1f);
			}
			else
			{
				RcbPalette.MicroLabel(new Rect(r.x + num, r.y + 20f, r.width - num * 2f, 18f), "RCB_Cell_Unarmed".Translate(), Alpha(RcbPalette.TextDim, a), TextAnchor.MiddleLeft, shadow: false);
			}
		}
	}

	private static void Label(Rect r, string text, Color col, float a)
	{
		GUI.color = Alpha(col, a);
		Widgets.Label(r, text);
		GUI.color = Color.white;
	}

	private static Color Alpha(Color c, float a)
	{
		return new Color(c.r, c.g, c.b, c.a * a);
	}

	private static float MoodFrac(Pawn p)
	{
		try
		{
			Need_Mood need_Mood = p.needs?.mood;
			if (need_Mood != null)
			{
				return need_Mood.CurLevelPercentage;
			}
		}
		catch
		{
		}
		return -1f;
	}

	private static float HealthFrac(Pawn p)
	{
		try
		{
			return Mathf.Clamp01(p.health.summaryHealth.SummaryHealthPercent);
		}
		catch
		{
			return 1f;
		}
	}

	private static int AgeYears(Pawn p)
	{
		try
		{
			return p.ageTracker.AgeBiologicalYears;
		}
		catch
		{
			return 0;
		}
	}

	private static string RoyalTitleLine(Pawn p)
	{
		try
		{
			if (p.royalty != null)
			{
				RoyalTitle mostSeniorTitle = p.royalty.MostSeniorTitle;
				if (mostSeniorTitle != null)
				{
					return mostSeniorTitle.def.GetLabelCapFor(p);
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string JobTitleLine(Pawn p)
	{
		try
		{
			return p.story?.TitleShortCap;
		}
		catch
		{
		}
		return null;
	}

	private static string RoleLine(Pawn p)
	{
		try
		{
			Ideo ideo = p.Ideo;
			if (ideo != null)
			{
				Precept_Role role = ideo.GetRole(p);
				if (role != null)
				{
					return role.LabelCap;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string TaskLine(Pawn p)
	{
		try
		{
			string str = p.jobs?.curDriver?.GetReport();
			return str.NullOrEmpty() ? null : str.CapitalizeFirst();
		}
		catch
		{
			return null;
		}
	}
}
