using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal sealed class Module_Armory : ICCModule
{
	private struct Cand
	{
		public Thing thing;

		public Pawn holder;

		public float sort;
	}

	private readonly List<CCPattern.Row> rows = new List<CCPattern.Row>(32);

	private Vector2 scroll;

	private ArmoryProposal proposal;

	private string proposalHeader;

	private readonly List<Cand> cands = new List<Cand>(64);

	private readonly HashSet<Thing> seen = new HashSet<Thing>();

	public string Label => "RCB_CC_Armory".Translate();

	public RadiusIcon Icon => IconSet.CC.Armory;

	public string Section => null;

	public void Refresh(CCContext ctx)
	{
		rows.Clear();
		if (proposal != null)
		{
			RefreshProposal();
			return;
		}
		cands.Clear();
		seen.Clear();
		List<Pawn> roster = ctx.roster;
		try
		{
			if (roster != null)
			{
				for (int i = 0; i < roster.Count; i++)
				{
					ThingWithComps thingWithComps = roster[i]?.equipment?.Primary;
					if (thingWithComps != null)
					{
						Add(thingWithComps, roster[i]);
					}
				}
			}
			List<Map> maps = Find.Maps;
			for (int j = 0; j < maps.Count; j++)
			{
				List<Thing> list = maps[j].listerThings.ThingsInGroup(ThingRequestGroup.Weapon);
				for (int k = 0; k < list.Count; k++)
				{
					Add(list[k], null);
				}
			}
		}
		catch
		{
		}
		cands.Sort((Cand a, Cand b) => b.sort.CompareTo(a.sort));
		for (int num = 0; num < cands.Count; num++)
		{
			Cand cand = cands[num];
			CCPattern.Row row = new CCPattern.Row
			{
				thing = cand.thing,
				bar = -1f
			};
			try
			{
				row.name = cand.thing.LabelCapNoCount;
				QualityCategory qc;
				string text = (cand.thing.TryGetQuality(out qc) ? qc.GetLabel() : null);
				int num2 = (cand.thing.def.useHitPoints ? Mathf.RoundToInt(100f * (float)cand.thing.HitPoints / (float)Mathf.Max(1, cand.thing.MaxHitPoints)) : 100);
				CCPattern.ItemChip(ref row, string.IsNullOrEmpty(text) ? "RCB_CC_HpOnly".Translate(num2.Named("HP")).ToString() : "RCB_CC_QualityHp".Translate(text.Named("QUALITY"), num2.Named("HP")).ToString());
				if (cand.holder != null)
				{
					CCPattern.FactionChip2(ref row, PawnUiCache.Get(cand.holder).name);
				}
				else
				{
					row.link = "RCB_CC_Unassigned".Translate();
				}
			}
			catch
			{
			}
			rows.Add(row);
		}
	}

	private void Add(Thing t, Pawn holder)
	{
		try
		{
			if (t?.def != null && seen.Add(t) && t.def.IsWeapon && !t.def.IsApparel && t.def.stackLimit <= 1 && (holder != null || !t.Spawned || !t.Position.Fogged(t.Map)))
			{
				float sort = (t.def.IsRangedWeapon ? RCBStats.RangedDps(t, holder) : t.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS, applyPostProcess: true, 120));
				cands.Add(new Cand
				{
					thing = t,
					holder = holder,
					sort = sort
				});
			}
		}
		catch
		{
		}
	}

	private void RefreshProposal()
	{
		List<ArmoryProposal.Change> changes = proposal.changes;
		for (int i = 0; i < changes.Count; i++)
		{
			ArmoryProposal.Change change = changes[i];
			CCPattern.Row row = new CCPattern.Row
			{
				pawn = change.pawn,
				bar = -1f
			};
			try
			{
				CCPattern.ItemChip(ref row, (change.current != null) ? change.current.LabelCapNoCount.ToString() : "RCB_CC_Unarmed".Translate().ToString());
				CCPattern.FactionChip2(ref row, change.proposed.LabelCapNoCount);
			}
			catch
			{
			}
			rows.Add(row);
		}
		proposalHeader = "RCB_CC_ProposalHeader".Translate(changes.Count.Named("N")).ToString();
	}

	public void Draw(Rect r, CCContext ctx)
	{
		float y = r.y;
		if (proposal == null)
		{
			CCPattern.Primary(r, IconSet.CC.Armory, "RCB_CC_AutoAssign".Translate(), out var clicked, ref y);
			if (clicked)
			{
				proposal = ArmoryProposal.Compute(ctx.roster);
				if (proposal.Empty)
				{
					proposal = null;
					Messages.Message("RCB_CC_NoProposal".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
				}
				else
				{
					ctx.requestRefresh();
				}
			}
			CCPattern.Header(r, "RCB_CC_Weapons".Translate(), ref y);
			int num = CCPattern.Table(r, rows, ref scroll, y, "RCB_CC_NoWeapons".Translate());
			if (num >= 0 && num < rows.Count && rows[num].thing != null)
			{
				OfferWeapon(rows[num].thing, ctx);
			}
		}
		else
		{
			ArmoryProposal armoryProposal = proposal;
			CCPattern.Primary(r, IconSet.CC.Armory, "RCB_CC_Apply".Translate(), out var clicked2, ref y, "RCB_CC_Cancel".Translate(), delegate
			{
				proposal = null;
				ctx.requestRefresh();
			});
			if (clicked2)
			{
				int num2 = armoryProposal.Apply();
				proposal = null;
				Messages.Message("RCB_CC_Applied".Translate(num2.Named("N")), MessageTypeDefOf.TaskCompletion, historical: false);
				ctx.requestRefresh();
			}
			else
			{
				CCPattern.Header(r, proposalHeader, ref y);
				CCPattern.Table(r, rows, ref scroll, y, null);
			}
		}
	}

	private static void OfferWeapon(Thing t, CCContext ctx)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		List<Pawn> roster = ctx.roster;
		if (roster != null)
		{
			for (int i = 0; i < roster.Count; i++)
			{
				Pawn p = roster[i];
				if (p != null)
				{
					string text = CCOrders.Blocker(p, t, weapon: true);
					string text2 = "RCB_CC_EquipOn".Translate(PawnUiCache.Get(p).name.Named("PAWN"));
					list.Add((text == null) ? new FloatMenuOption(text2, delegate
					{
						CCOrders.TryOrder(p, JobDefOf.Equip, t);
						ctx.requestRefresh();
					}) : new FloatMenuOption(text2 + " (" + text + ")", null));
				}
			}
		}
		if (t.Spawned)
		{
			list.Add(new FloatMenuOption("RCB_CC_JumpToWeapon".Translate(), delegate
			{
				try
				{
					CameraJumper.TryJump(t);
				}
				catch
				{
				}
			}));
		}
		if (list.Count > 0)
		{
			Find.WindowStack.Add(new FloatMenu(list));
		}
	}
}
