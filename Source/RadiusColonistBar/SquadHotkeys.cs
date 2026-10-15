using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): hotkeys for the squads of the selected pawns. Read with the bar's other keys
// (Patch_ColonistBarOnGUI_Popout), never while a text field has focus.
internal static class SquadHotkeys
{
	private static KeyBindingDef draft;

	private static KeyBindingDef fire;

	private static KeyBindingDef regroup;

	private static KeyBindingDef strongest;

	private static KeyBindingDef weakest;

	private static KeyBindingDef fleeing;

	private static KeyBindingDef station;

	private static bool bound;

	private static readonly List<int> targets = new List<int>();

	internal static void OnGUI()
	{
		// Orders act on the current map: none from the world view.
		if (Event.current.type != EventType.KeyDown || GUIUtility.keyboardControl != 0 || WorldRendererUtility.WorldSelected)
		{
			return;
		}
		Bind();
		if (Pressed(draft))
		{
			Run(delegate(int i)
			{
				SquadStatus.State state = SquadStatus.Fresh(i);
				if (state.present > 0 && state.drafted == state.present)
				{
					SquadOrders.Undraft(i);
				}
				else
				{
					SquadOrders.Draft(i);
				}
			});
		}
		else if (Pressed(fire))
		{
			Run(SquadOrders.ToggleFireAtWill);
		}
		else if (Pressed(regroup))
		{
			Run(SquadOrders.Regroup);
		}
		else if (Pressed(strongest))
		{
			Run(delegate(int i)
			{
				SquadOrders.Assault(i, AssaultMode.Strongest);
			});
		}
		else if (Pressed(weakest))
		{
			Run(delegate(int i)
			{
				SquadOrders.Assault(i, AssaultMode.Weakest);
			});
		}
		else if (Pressed(fleeing))
		{
			Run(delegate(int i)
			{
				SquadOrders.Assault(i, AssaultMode.Fleeing);
			});
		}
		else if (Pressed(station))
		{
			Run(SquadStations.Go);
		}
	}

	private static void Bind()
	{
		if (!bound)
		{
			bound = true;
			draft = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadDraft");
			fire = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadFireAtWill");
			regroup = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadRegroup");
			strongest = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadAssaultStrongest");
			weakest = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadAssaultWeakest");
			fleeing = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadAssaultFleeing");
			station = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_SquadGoToStation");
		}
	}

	private static bool Pressed(KeyBindingDef def)
	{
		return def != null && def.MainKey != KeyCode.None && def.KeyDownEvent;
	}

	// Each squad of the selection once. Assault selects the squad's members, so the targets are read first.
	private static void Run(System.Action<int> order)
	{
		Event.current.Use();
		targets.Clear();
		BarSquads get = BarSquads.Get;
		if (get != null)
		{
			foreach (Pawn selectedPawn in Find.Selector.SelectedPawns)
			{
				int num = get.SquadIndexOf(selectedPawn);
				if (num >= 0 && !targets.Contains(num))
				{
					targets.Add(num);
				}
			}
		}
		if (targets.Count == 0)
		{
			Messages.Message("RCB_Hotkey_NoSquad".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		for (int i = 0; i < targets.Count; i++)
		{
			order(targets[i]);
		}
	}
}
