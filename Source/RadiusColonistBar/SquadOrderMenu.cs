using System;
using System.Collections.Generic;
using Verse;

namespace RadiusColonistBar;

// Squad orders (Colonist Bar Plus): the order list shared by the squad tab right-click and the Squads module.
internal static class SquadOrderMenu
{
	internal static void Open(int index)
	{
		Find.WindowStack.Add(new RcbFloatMenu(Options(index)));
	}

	internal static List<FloatMenuOption> Options(int index)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		List<Pawn> list2 = SquadMembers.Able(Find.CurrentMap, index);
		if (list2.Count == 0)
		{
			// A null action draws the option disabled.
			list.Add(new FloatMenuOption("RCB_Orders_NoMember".Translate(), null));
			return list;
		}
		bool flag = SquadOrders.AllDrafted(list2);
		list.Add(Option(flag ? "RCB_Orders_Undraft" : "RCB_Orders_Draft", flag ? "RCB_Orders_UndraftTip" : "RCB_Orders_DraftTip", delegate
		{
			if (flag)
			{
				SquadOrders.Undraft(index);
			}
			else
			{
				SquadOrders.Draft(index);
			}
		}));
		bool flag2 = SquadOrders.AllFireAtWill(list2);
		list.Add(Option(flag2 ? "RCB_Orders_FireAtWillOff" : "RCB_Orders_FireAtWillOn", "RCB_Orders_FireAtWillTip", delegate
		{
			SquadOrders.ToggleFireAtWill(index);
		}));
		list.Add(Option("RCB_Orders_Regroup", "RCB_Orders_RegroupTip", delegate
		{
			SquadOrders.Regroup(index);
		}));
		bool hasMode = SquadOrders.CommonMode(list2, out var current);
		list.Add(new FloatMenuOptionSub("RCB_Orders_Assault".Translate(), () => AssaultOptions(index, hasMode, current)));
		return list;
	}

	private static List<FloatMenuOption> AssaultOptions(int index, bool hasMode, AssaultMode current)
	{
		return new List<FloatMenuOption>
		{
			AssaultOption(index, AssaultMode.Strongest, "RCB_Orders_AssaultStrongest", "RCB_Orders_AssaultStrongestTip", hasMode && current == AssaultMode.Strongest),
			AssaultOption(index, AssaultMode.Weakest, "RCB_Orders_AssaultWeakest", "RCB_Orders_AssaultWeakestTip", hasMode && current == AssaultMode.Weakest),
			AssaultOption(index, AssaultMode.Fleeing, "RCB_Orders_AssaultFleeing", "RCB_Orders_AssaultFleeingTip", hasMode && current == AssaultMode.Fleeing)
		};
	}

	private static FloatMenuOption AssaultOption(int index, AssaultMode mode, string labelKey, string tipKey, bool active)
	{
		string text = labelKey.Translate();
		if (active)
		{
			text = "RCB_Orders_ActiveMark".Translate(text);
		}
		return new FloatMenuOption(text, delegate
		{
			SquadOrders.Assault(index, mode);
		})
		{
			tooltip = new TipSignal(tipKey.Translate())
		};
	}

	private static FloatMenuOption Option(string labelKey, string tipKey, Action action)
	{
		return new FloatMenuOption(labelKey.Translate(), action)
		{
			tooltip = new TipSignal(tipKey.Translate())
		};
	}
}
