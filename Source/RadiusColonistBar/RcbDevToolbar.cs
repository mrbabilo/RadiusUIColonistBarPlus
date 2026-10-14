using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
internal static class RcbDevToolbar
{
	private struct Btn
	{
		public Texture2D tex;

		public string tip;

		public Action act;

		public bool toggled;
	}

	public const float Icon = 24f;

	public const float Gap = 4f;

	private static readonly List<Btn> buf = new List<Btn>(8);

	private static int drewFrame = -2;

	private static bool looked;

	private static bool ok;

	private static MethodInfo mLog;

	private static MethodInfo mTweak;

	private static MethodInfo mActions;

	private static MethodInfo mSettings;

	private static MethodInfo mLogMenu;

	private static MethodInfo mInspector;

	private static MethodInfo mPalette;

	private static MethodInfo mGod;

	private static AccessTools.FieldRef<Dialog_Debug, DebugTabMenu> fCurTabMenu;

	private static AccessTools.FieldRef<DebugWindowsOpener, float> fRowFinalX;

	private static readonly Color ChipFill = RcbPalette.WashStrong;

	private static readonly Color ChipHover = RcbPalette.WashSolid;

	private static readonly Color ChipOn = Palette.WithAlpha(Palette.Good, 0.55f);

	private static readonly List<Rect> lastRects = new List<Rect>(8);

	private const float HostWindowH = 25f;

	private const float HostWindowTop = 3f;

	private static bool DrewThisFrame => Time.frameCount - drewFrame <= 5;

	private static DebugWindowsOpener Opener
	{
		get
		{
			try
			{
				return Find.UIRoot?.debugWindowOpener;
			}
			catch
			{
				return null;
			}
		}
	}

	private static bool CanOwn
	{
		get
		{
			if (!Prefs.DevMode)
			{
				return false;
			}
			if (Current.ProgramState != ProgramState.Playing)
			{
				return false;
			}
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			if (!Patch_BarMetrics.IslandActive(settings))
			{
				return false;
			}
			Look();
			return ok;
		}
	}

	public static bool Owns => CanOwn && DrewThisFrame;

	public static void NotifyIslandDrew()
	{
		drewFrame = Time.frameCount;
	}

	private static void Look()
	{
		if (looked)
		{
			return;
		}
		looked = true;
		try
		{
			Type typeFromHandle = typeof(DebugWindowsOpener);
			mLog = AccessTools.Method(typeFromHandle, "ToggleLogWindow", (Type[])null, (Type[])null);
			mTweak = AccessTools.Method(typeFromHandle, "ToggleTweakValuesMenu", (Type[])null, (Type[])null);
			mActions = AccessTools.Method(typeFromHandle, "ToggleDebugActionsMenu", (Type[])null, (Type[])null);
			mSettings = AccessTools.Method(typeFromHandle, "ToggleDebugSettingsMenu", (Type[])null, (Type[])null);
			mLogMenu = AccessTools.Method(typeFromHandle, "ToggleDebugLogMenu", (Type[])null, (Type[])null);
			mInspector = AccessTools.Method(typeFromHandle, "ToggleDebugInspector", (Type[])null, (Type[])null);
			mPalette = AccessTools.Method(typeFromHandle, "TryOpenOrClosePalette", (Type[])null, (Type[])null);
			mGod = AccessTools.Method(typeFromHandle, "ToggleGodMode", (Type[])null, (Type[])null);
			ok = mLog != null && mTweak != null && mActions != null && mSettings != null && mLogMenu != null && mInspector != null && mPalette != null && mGod != null;
			try
			{
				fCurTabMenu = AccessTools.FieldRefAccess<Dialog_Debug, DebugTabMenu>("currentTabMenu");
			}
			catch
			{
				fCurTabMenu = null;
			}
			try
			{
				fRowFinalX = AccessTools.FieldRefAccess<DebugWindowsOpener, float>("widgetRowFinalX");
			}
			catch
			{
				fRowFinalX = null;
			}
		}
		catch
		{
			ok = false;
		}
		if (!ok)
		{
			Log.Warning("[Radius UI - Colonist Bar] Could not resolve the dev-toolbar actions; leaving the vanilla toolbar in place.");
		}
	}

	private static void Call(MethodInfo m)
	{
		DebugWindowsOpener opener = Opener;
		if (m == null || opener == null)
		{
			return;
		}
		try
		{
			m.Invoke(opener, null);
		}
		catch (Exception ex)
		{
			Log.Error("[Radius UI - Colonist Bar] dev action failed: " + ex);
		}
	}

	public static void MirrorHostWidth()
	{
		if (fRowFinalX == null)
		{
			return;
		}
		DebugWindowsOpener opener = Opener;
		if (opener == null)
		{
			return;
		}
		float num = RowWidth();
		if (num <= 0f || !RcbGuard.Finite(num))
		{
			return;
		}
		float num2 = RcbIsland.SlabOffsetX();
		if (RcbGuard.Finite(num2))
		{
			num += Mathf.Abs(num2) * 2f;
		}
		try
		{
			fRowFinalX.Invoke(opener) = num;
		}
		catch
		{
		}
	}

	private static bool WinOpen(Type t)
	{
		try
		{
			return Find.WindowStack?.IsOpen(t) ?? false;
		}
		catch
		{
			return false;
		}
	}

	private static DebugTabMenuDef OpenDebugTab()
	{
		try
		{
			if (fCurTabMenu == null)
			{
				return null;
			}
			Dialog_Debug dialog_Debug = Find.WindowStack?.WindowOfType<Dialog_Debug>();
			if (dialog_Debug == null)
			{
				return null;
			}
			return fCurTabMenu.Invoke(dialog_Debug)?.def;
		}
		catch
		{
			return null;
		}
	}

	private static void Build()
	{
		buf.Clear();
		DebugTabMenuDef debugTabMenuDef = OpenDebugTab();
		buf.Add(new Btn
		{
			tex = TexButton.ToggleLog,
			tip = "Open the debug log.",
			toggled = WinOpen(typeof(EditWindow_Log)),
			act = delegate
			{
				Call(mLog);
			}
		});
		buf.Add(new Btn
		{
			tex = TexButton.ToggleTweak,
			tip = "Open tweakvalues menu.",
			toggled = WinOpen(typeof(EditWindow_TweakValues)),
			act = delegate
			{
				Call(mTweak);
			}
		});
		buf.Add(new Btn
		{
			tex = TexButton.OpenDebugActionsMenu,
			tip = "Open debug actions menu.",
			toggled = (debugTabMenuDef == DebugTabMenuDefOf.Actions),
			act = delegate
			{
				Call(mActions);
			}
		});
		buf.Add(new Btn
		{
			tex = TexButton.OpenInspectSettings,
			tip = "Open the view settings.",
			toggled = (debugTabMenuDef == DebugTabMenuDefOf.Settings),
			act = delegate
			{
				Call(mSettings);
			}
		});
		buf.Add(new Btn
		{
			tex = TexButton.OpenDebugActionsMenu,
			tip = "Open debug logging menu.",
			toggled = (debugTabMenuDef == DebugTabMenuDefOf.Output),
			act = delegate
			{
				Call(mLogMenu);
			}
		});
		buf.Add(new Btn
		{
			tex = TexButton.OpenInspector,
			tip = "Open the inspector.",
			toggled = WinOpen(typeof(EditWindow_DebugInspector)),
			act = delegate
			{
				Call(mInspector);
			}
		});
		if (Current.ProgramState == ProgramState.Playing)
		{
			buf.Add(new Btn
			{
				tex = TexButton.ToggleDevPalette,
				tip = "Toggle the dev palette.",
				toggled = DebugSettings.devPalette,
				act = delegate
				{
					DebugSettings.devPalette = !DebugSettings.devPalette;
					Call(mPalette);
				}
			});
			buf.Add(new Btn
			{
				tex = (DebugSettings.godMode ? TexButton.GodModeEnabled : TexButton.GodModeDisabled),
				tip = "Toggle god mode.",
				toggled = DebugSettings.godMode,
				act = delegate
				{
					Call(mGod);
				}
			});
		}
	}

	public static float RowWidth()
	{
		if (!CanOwn)
		{
			return 0f;
		}
		Build();
		int count = buf.Count;
		return (count <= 0) ? 0f : ((float)count * 24f + (float)(count - 1) * 4f);
	}

	public static bool HandleInput()
	{
		return false;
	}

	public static bool HandleInputInWindow()
	{
		Event current = Event.current;
		if (current == null || current.button != 0)
		{
			return false;
		}
		if (current.type != EventType.MouseDown)
		{
			return false;
		}
		Build();
		int count = buf.Count;
		if (count <= 0)
		{
			return false;
		}
		float num = (float)count * 24f + (float)(count - 1) * 4f;
		float num2 = (float)UI.screenWidth * 0.5f;
		float num3 = 0f;
		try
		{
			DebugWindowsOpener opener = Opener;
			if (fRowFinalX != null && opener != null)
			{
				float num4 = fRowFinalX.Invoke(opener);
				if (num4 > 1f && RcbGuard.Finite(num4))
				{
					num2 = (float)UI.screenWidth * 0.5f - num4 * 0.5f;
					float num5 = RcbIsland.LastSlab.center.x - num * 0.5f;
					if (RcbGuard.Finite(num5))
					{
						num3 = num5 - num2;
					}
				}
			}
		}
		catch
		{
			num3 = 0f;
		}
		if (!RcbGuard.Finite(num3))
		{
			num3 = 0f;
		}
		Vector2 mousePosition = current.mousePosition;
		if (mousePosition.y < 0f || mousePosition.y > 25f)
		{
			return false;
		}
		if (mousePosition.x >= num3 && mousePosition.x <= num3 + num)
		{
			int index = Mathf.Clamp(Mathf.FloorToInt((mousePosition.x - num3) / 28f), 0, count - 1);
			SoundDefOf.Click.PlayOneShotOnCamera();
			buf[index].act?.Invoke();
			current.Use();
			return true;
		}
		Vector2 p = new Vector2(mousePosition.x + num2, mousePosition.y + 3f);
		if (RcbIsland.TryChromeClickAt(p))
		{
			current.Use();
			return true;
		}
		return false;
	}

	public static void Draw(float centreX, float y)
	{
		if (!CanOwn)
		{
			if (lastRects.Count > 0)
			{
				lastRects.Clear();
			}
			return;
		}
		Build();
		int count = buf.Count;
		if (count == 0)
		{
			lastRects.Clear();
			return;
		}
		float num = (float)count * 24f + (float)(count - 1) * 4f;
		float num2 = centreX - num * 0.5f;
		lastRects.Clear();
		for (int i = 0; i < count; i++)
		{
			Rect rect = new Rect(num2 + (float)i * 28f, y, 24f, 24f);
			lastRects.Add(rect);
			if (RcbGuard.Finite(rect))
			{
				Btn btn = buf[i];
				bool flag = Mouse.IsOver(rect);
				RcbSpatial.Chip(rect, btn.toggled ? ChipOn : (flag ? ChipHover : ChipFill));
				if (btn.tex != null)
				{
					Rect position = rect.ContractedBy(4f);
					GUI.DrawTexture(position, btn.tex);
				}
				if (!string.IsNullOrEmpty(btn.tip) && flag)
				{
					TooltipHandler.TipRegion(rect, btn.tip);
				}
			}
		}
	}
}
