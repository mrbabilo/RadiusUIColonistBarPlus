using System;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public class Dialog_RenameSquad : Window
{
	private string name;

	private readonly Action<string> onDone;

	public override Vector2 InitialSize => new Vector2(300f, 150f);

	public Dialog_RenameSquad(BarSquads sq, int index)
	{
		name = sq.SquadName(index) ?? "Squad";
		onDone = delegate(string s)
		{
			sq.Rename(index, s);
		};
		Init();
	}

	public Dialog_RenameSquad(string currentName, Action<string> done)
	{
		name = currentName ?? "";
		onDone = done;
		Init();
	}

	private void Init()
	{
		forcePause = false;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnAccept = false;
	}

	// Squad orders (Colonist Bar Plus): give the keyboard focus back, or the squad hotkeys stay off after a rename.
	public override void PostClose()
	{
		base.PostClose();
		UI.UnfocusCurrentControl();
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Small;
		Widgets.Label(new Rect(0f, 0f, inRect.width, 26f), "RCB_Rename".Translate());
		name = Widgets.TextField(new Rect(0f, 30f, inRect.width, 30f), name);
		if (name != null && name.Length > 24)
		{
			name = name.Substring(0, 24);
		}
		bool flag = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
		if (Widgets.ButtonText(new Rect(0f, inRect.height - 32f, inRect.width, 30f), "OK") || flag)
		{
			if (!name.NullOrEmpty())
			{
				onDone?.Invoke(name.Trim());
			}
			Close();
		}
	}
}
