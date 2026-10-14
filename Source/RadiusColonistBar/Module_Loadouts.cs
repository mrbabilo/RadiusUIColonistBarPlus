using System.Collections.Generic;
using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal sealed class Module_Loadouts : ICCModule
{
	private readonly List<CCPattern.ChipEntry> chips = new List<CCPattern.ChipEntry>(8);

	private readonly List<CCPattern.Row> rows = new List<CCPattern.Row>(32);

	private Vector2 scroll;

	public string Label => "RCB_CC_Loadouts".Translate();

	public RadiusIcon Icon => IconSet.CC.Loadouts;

	public string Section => null;

	public void Refresh(CCContext ctx)
	{
		chips.Clear();
		rows.Clear();
		CCKits get = CCKits.Get;
		if (get != null)
		{
			for (int i = 0; i < get.kits.Count; i++)
			{
				chips.Add(CCPattern.Item(get.kits[i].name, i));
			}
		}
		chips.Add(CCPattern.Link("RCB_CC_AddLoadout".Translate(), -1));
		List<Pawn> roster = ctx.roster;
		if (roster == null)
		{
			return;
		}
		for (int j = 0; j < roster.Count; j++)
		{
			Pawn pawn = roster[j];
			if (pawn != null)
			{
				CCPattern.Row row = new CCPattern.Row
				{
					pawn = pawn,
					bar = -1f,
					link = "RCB_CC_Edit".Translate()
				};
				int num = get?.KitIndexOf(pawn) ?? (-1);
				if (num >= 0)
				{
					CCKit cCKit = get.kits[num];
					CCPattern.ItemChip(ref row, cCKit.name);
					int num2 = CCKits.Missing(cCKit, pawn);
					CCPattern.SkillChip2(ref row, (num2 == 0) ? "RCB_CC_Complete".Translate().ToString() : "RCB_CC_Missing".Translate(num2.Named("N")).ToString());
				}
				else
				{
					row.chip = "RCB_CC_NoLoadout".Translate();
					row.chipFill = Palette.Flat.SegOff;
					row.chipInk = Palette.Flat.SegOffInk;
				}
				rows.Add(row);
			}
		}
	}

	public void Draw(Rect r, CCContext ctx)
	{
		float y = r.y;
		CCPattern.Primary(r, IconSet.CC.Loadouts, "RCB_CC_EditLoadouts".Translate(), out var clicked, ref y);
		if (clicked)
		{
			Dialog_LoadoutEditor.Open(ctx, -1);
		}
		CCPattern.Header(r, "RCB_CC_LoadoutsHeader".Translate(), ref y);
		int num = CCPattern.ChipRow(r, chips, ref y);
		if (num >= 0)
		{
			int num2 = (int)chips[num].tag;
			if (num2 < 0)
			{
				CCKits get = CCKits.Get;
				if (get != null)
				{
					get.kits.Add(new CCKit
					{
						name = "RCB_CC_KitDefaultName".Translate((get.kits.Count + 1).Named("N"))
					});
					Dialog_LoadoutEditor.Open(ctx, get.kits.Count - 1);
					ctx.requestRefresh();
				}
			}
			else
			{
				Dialog_LoadoutEditor.Open(ctx, num2);
			}
		}
		CCPattern.Header(r, "RCB_CC_Colonists".Translate(), ref y);
		int num3 = CCPattern.Table(r, rows, ref scroll, y, null);
		if (num3 >= 0 && num3 < rows.Count)
		{
			EditMenu(rows[num3].pawn, ctx);
		}
	}

	private static void EditMenu(Pawn p, CCContext ctx)
	{
		CCKits store = CCKits.Get;
		if (store == null || p == null)
		{
			return;
		}
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		int num = store.KitIndexOf(p);
		for (int i = 0; i < store.kits.Count; i++)
		{
			int ki = i;
			string name = store.kits[i].name;
			list.Add(new FloatMenuOption((ki == num) ? "RCB_CC_AssignedKit".Translate(name.Named("KIT")) : "RCB_CC_AssignKit".Translate(name.Named("KIT")), delegate
			{
				store.Assign(p, ki);
				ctx.requestRefresh();
			}));
		}
		if (num >= 0)
		{
			CCKit kit = store.kits[num];
			list.Add(new FloatMenuOption("RCB_CC_ApplyNow".Translate(), delegate
			{
				LoadoutEditor.ApplyKit(kit, p);
				ctx.requestRefresh();
			}));
			list.Add(new FloatMenuOption("RCB_CC_ClearLoadout".Translate(), delegate
			{
				store.Assign(p, -1);
				ctx.requestRefresh();
			}));
		}
		if (list.Count == 0)
		{
			list.Add(new FloatMenuOption("RCB_CC_NoKitsYet".Translate(), null));
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}
}
