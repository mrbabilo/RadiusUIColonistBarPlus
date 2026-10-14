using System;
using System.Collections.Generic;
using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public class FloatMenuOptionSub : FloatMenuOption
{
	private readonly Func<List<FloatMenuOption>> submenu;

	public FloatMenuOptionSub(string label, Func<List<FloatMenuOption>> submenu)
		: base(label, null)
	{
		this.submenu = submenu;
		action = delegate
		{
			Open(null);
		};
		// Colonist Bar Plus: translated instead of hard-coded English.
		tooltip = new TipSignal("RCB_Sub_RightClickTip".Translate());
		extraPartWidth = 18f;
		extraPartOnGUI = delegate(Rect r)
		{
			TextAnchor anchor = Text.Anchor;
			Color color = GUI.color;
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = Palette.WithAlpha(RcbPalette.TextDim, color.a);
			Widgets.Label(r, "»");
			GUI.color = color;
			Text.Anchor = anchor;
			return false;
		};
	}

	public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
	{
		Event current = Event.current;
		if (!base.Disabled && current.type == EventType.MouseDown && current.button == 1 && rect.Contains(current.mousePosition))
		{
			Open(floatMenu as RcbFloatMenu);
			current.Use();
			return false;
		}
		return base.DoGUI(rect, colonistOrdering, floatMenu);
	}

	private void Open(RcbFloatMenu parent)
	{
		List<FloatMenuOption> list = null;
		try
		{
			list = submenu?.Invoke();
		}
		catch
		{
		}
		if (list != null && list.Count != 0)
		{
			RcbFloatMenu rcbFloatMenu = new RcbFloatMenu(list);
			Find.WindowStack.Add(rcbFloatMenu);
			parent?.NotifyChildOpened(rcbFloatMenu);
		}
	}
}
