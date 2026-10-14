using System;
using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public class Dialog_LoadoutEditor : Window
{
	private static readonly Vector2 FrameSize = new Vector2(860f, 560f);

	private readonly LoadoutEditor editor = new LoadoutEditor();

	private readonly CCContext ctx;

	public override Vector2 InitialSize => FrameSize;

	protected override float Margin => 0f;

	internal static void Open(CCContext ctx, int kit)
	{
		Find.WindowStack.WindowOfType<Dialog_LoadoutEditor>()?.Close(doCloseSound: false);
		Dialog_LoadoutEditor dialog_LoadoutEditor = new Dialog_LoadoutEditor(ctx);
		if (kit >= 0)
		{
			dialog_LoadoutEditor.editor.EditKit(kit);
		}
		Find.WindowStack.Add(dialog_LoadoutEditor);
	}

	private Dialog_LoadoutEditor(CCContext ctx)
	{
		this.ctx = ctx;
		doCloseX = false;
		drawShadow = false;
		doWindowBackground = false;
		draggable = true;
		resizeable = false;
		absorbInputAroundWindow = false;
		preventCameraMotion = false;
		closeOnClickedOutside = false;
		closeOnCancel = true;
		focusWhenOpened = true;
		forcePause = false;
		layer = WindowLayer.GameUI;
	}

	protected override void SetInitialSizeAndPosition()
	{
		windowRect = new Rect(Mathf.Round(((float)UI.screenWidth - FrameSize.x) * 0.5f), Mathf.Round(((float)UI.screenHeight - FrameSize.y) * 0.5f), FrameSize.x, FrameSize.y);
	}

	public override void DoWindowContents(Rect inRect)
	{
		Rect rect = new Rect(inRect.x, inRect.y, inRect.width - 14f, inRect.height);
		CardChrome.Rounded(rect, Palette.Flat.Panel, 10f);
		if (UIKit.Flat.CloseX(rect))
		{
			Close();
			return;
		}
		Rect r = new Rect(rect.x + 26f, rect.y + 22f, rect.width - 52f, rect.height - 44f);
		try
		{
			editor.Draw(r, ctx);
		}
		catch (Exception ex)
		{
			Log.ErrorOnce("[Radius UI - Colonist Bar] loadout editor threw: " + ex, 481524);
		}
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		Text.WordWrap = true;
	}

	public override void PostClose()
	{
		base.PostClose();
		ctx.requestRefresh?.Invoke();
	}
}
