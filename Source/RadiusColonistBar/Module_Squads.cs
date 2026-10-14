using System.Collections.Generic;
using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal sealed class Module_Squads : ICCModule
{
	private readonly List<CCPattern.ChipEntry> chips = new List<CCPattern.ChipEntry>(8);

	private readonly List<CCPattern.Row> rows = new List<CCPattern.Row>(32);

	private readonly List<int> counts = new List<int>(8);

	private Vector2 scroll;

	public string Label => "RCB_CC_Squads".Translate();

	public RadiusIcon Icon => IconSet.CC.Squads;

	public string Section => "RCB_CC_Roster".Translate();

	public void Refresh(CCContext ctx)
	{
		chips.Clear();
		rows.Clear();
		counts.Clear();
		BarSquads squads = ctx.squads;
		List<Pawn> roster = ctx.roster;
		if (squads == null)
		{
			return;
		}
		for (int i = 0; i < squads.names.Count; i++)
		{
			counts.Add(0);
		}
		int num = 0;
		if (roster != null)
		{
			for (int j = 0; j < roster.Count; j++)
			{
				Pawn pawn = roster[j];
				if (pawn != null)
				{
					int num2 = squads.SquadIndexOf(pawn);
					if (num2 >= 0)
					{
						counts[num2]++;
					}
					else
					{
						num++;
					}
				}
			}
		}
		for (int k = 0; k < squads.names.Count; k++)
		{
			chips.Add(CCPattern.Faction("RCB_CC_SquadCount".Translate(squads.names[k].Named("NAME"), counts[k].Named("N")), k));
		}
		chips.Add(CCPattern.Item("RCB_CC_UnassignedCount".Translate(num.Named("N")), -1));
		List<Pawn> list = CCRoster.BySquad(roster ?? new List<Pawn>(), squads);
		for (int l = 0; l < list.Count; l++)
		{
			Pawn pawn2 = list[l];
			CCPattern.Row row = new CCPattern.Row
			{
				pawn = pawn2,
				bar = -1f,
				link = "RCB_CC_Move".Translate()
			};
			int num3 = squads.SquadIndexOf(pawn2);
			if (num3 >= 0)
			{
				CCPattern.FactionChip(ref row, squads.SquadName(num3));
			}
			else
			{
				CCPattern.ItemChip(ref row, "RCB_CC_Unassigned".Translate());
			}
			rows.Add(row);
		}
	}

	public void Draw(Rect r, CCContext ctx)
	{
		BarSquads squads = ctx.squads;
		float y = r.y;
		CCPattern.Primary(r, IconSet.CC.Squads, "RCB_CC_NewSquad".Translate(), out var clicked, ref y);
		if (clicked && squads != null)
		{
			int index = squads.NewSquad();
			Find.WindowStack.Add(new Dialog_RenameSquad(squads, index));
			ctx.requestRefresh();
		}
		CCPattern.Header(r, "RCB_CC_SquadsHeader".Translate(), ref y);
		int num = CCPattern.ChipRow(r, chips, ref y);
		if (num >= 0 && squads != null)
		{
			int num2 = (int)chips[num].tag;
			if (num2 >= 0)
			{
				SquadMenu(squads, num2, ctx);
			}
		}
		CCPattern.Header(r, "RCB_CC_Colonists".Translate(), ref y);
		int num3 = CCPattern.Table(r, rows, ref scroll, y, "RCB_CC_NoColonists".Translate());
		if (num3 >= 0 && num3 < rows.Count && squads != null)
		{
			MoveMenu(squads, rows[num3].pawn, ctx);
		}
	}

	private static void SquadMenu(BarSquads sq, int si, CCContext ctx)
	{
		// Squad orders (Colonist Bar Plus) first, then the squad's own management.
		List<FloatMenuOption> list = SquadOrderMenu.Options(si);
		list.Add(new FloatMenuOption("RCB_CC_SquadRename".Translate(), delegate
		{
			Find.WindowStack.Add(new Dialog_RenameSquad(sq, si));
		}));
		if (si > 0)
		{
			list.Add(new FloatMenuOption("RCB_CC_SquadMoveUp".Translate(), delegate
			{
				sq.MoveSquad(si, -1);
				ctx.requestRefresh();
			}));
		}
		if (si < sq.names.Count - 1)
		{
			list.Add(new FloatMenuOption("RCB_CC_SquadMoveDown".Translate(), delegate
			{
				sq.MoveSquad(si, 1);
				ctx.requestRefresh();
			}));
		}
		list.Add(new FloatMenuOption("RCB_CC_SquadDisband".Translate(), delegate
		{
			sq.Disband(si);
			ctx.requestRefresh();
		}));
		Find.WindowStack.Add(new RcbFloatMenu(list));
	}

	private static void MoveMenu(BarSquads sq, Pawn p, CCContext ctx)
	{
		if (p == null)
		{
			return;
		}
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		int num = sq.SquadIndexOf(p);
		for (int i = 0; i < sq.names.Count; i++)
		{
			if (i != num)
			{
				int si = i;
				list.Add(new FloatMenuOption(sq.names[i], delegate
				{
					sq.Assign(p, si);
					ctx.requestRefresh();
				}));
			}
		}
		if (num >= 0)
		{
			list.Add(new FloatMenuOption("RCB_CC_Unassigned".Translate(), delegate
			{
				sq.Remove(p);
				ctx.requestRefresh();
			}));
		}
		list.Add(new FloatMenuOption("RCB_CC_NewSquadEllipsis".Translate(), delegate
		{
			int num2 = sq.NewSquad();
			sq.Assign(p, num2);
			Find.WindowStack.Add(new Dialog_RenameSquad(sq, num2));
			ctx.requestRefresh();
		}));
		Find.WindowStack.Add(new FloatMenu(list));
	}
}
