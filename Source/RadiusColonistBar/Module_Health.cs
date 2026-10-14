using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal sealed class Module_Health : ICCModule
{
	private readonly List<CCPattern.Row> rows = new List<CCPattern.Row>(32);

	private Vector2 scroll;

	public string Label => "RCB_CC_Health".Translate();

	public RadiusIcon Icon => IconSet.CC.Health;

	public string Section => null;

	public void Refresh(CCContext ctx)
	{
		rows.Clear();
		List<Pawn> roster = ctx.roster;
		if (roster == null)
		{
			return;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			Pawn pawn = roster[i];
			if (pawn == null)
			{
				continue;
			}
			CCPattern.Row row = new CCPattern.Row
			{
				pawn = pawn
			};
			try
			{
				PawnUiCache.Snap snap = PawnUiCache.Get(pawn);
				if ((pawn.health != null && pawn.health.hediffSet.BleedRateTotal > 0.01f) || snap.needsTend)
				{
					CCPattern.AlertChip(ref row, "RCB_CC_NeedsTending".Translate());
				}
				else
				{
					Hediff hediff = null;
					float num = -1f;
					List<Hediff> list = pawn.health?.hediffSet?.hediffs;
					if (list != null)
					{
						for (int j = 0; j < list.Count; j++)
						{
							Hediff hediff2 = list[j];
							if (hediff2 != null && hediff2.Visible && hediff2.def.isBad)
							{
								float severity = hediff2.Severity;
								if (hediff == null || severity > num)
								{
									hediff = hediff2;
									num = severity;
								}
							}
						}
					}
					CCPattern.ItemChip(ref row, (hediff != null) ? hediff.LabelCap.ToString() : "RCB_CC_Healthy".Translate().ToString());
				}
				row.barLabel = Mathf.RoundToInt((row.bar = RCBStats.Health(pawn)) * 100f) + "%";
			}
			catch
			{
				row.bar = -1f;
			}
			row.link = "RCB_CC_HealthLink".Translate();
			rows.Add(row);
		}
	}

	public void Draw(Rect r, CCContext ctx)
	{
		float y = r.y;
		CCPattern.Primary(r, IconSet.CC.Health, "RCB_CC_MedicalPolicies".Translate(), out var clicked, ref y);
		if (clicked)
		{
			try
			{
				Find.WindowStack.Add(new Dialog_MedicalDefaults());
			}
			catch
			{
			}
		}
		CCPattern.Header(r, "RCB_CC_Colonists".Translate(), ref y);
		int num = CCPattern.Table(r, rows, ref scroll, y, null);
		if (num >= 0 && num < rows.Count)
		{
			HubRouter.OpenTab(rows[num].pawn, HubTarget.Health);
		}
	}
}
