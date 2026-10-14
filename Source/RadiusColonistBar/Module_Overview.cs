using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal sealed class Module_Overview : ICCModule
{
	private string dayLine;

	private readonly string[] statValue = new string[5];

	private readonly string[] statLabel = new string[5];

	private readonly List<CCPattern.Row> rows = new List<CCPattern.Row>(8);

	private readonly List<byte> rowVerb = new List<byte>(8);

	private Vector2 scroll;

	public string Label => "RCB_CC_Overview".Translate();

	public RadiusIcon Icon => IconSet.CC.Overview;

	public string Section => null;

	public void Refresh(CCContext ctx)
	{
		List<Pawn> roster = ctx.roster;
		int num = roster?.Count ?? 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		float num5 = 0f;
		rows.Clear();
		rowVerb.Clear();
		for (int i = 0; i < num; i++)
		{
			Pawn pawn = roster[i];
			if (pawn == null)
			{
				continue;
			}
			try
			{
				if (pawn.Drafted)
				{
					num2++;
				}
				PawnUiCache.Snap snap = PawnUiCache.Get(pawn);
				bool flag = pawn.health != null && pawn.health.hediffSet.BleedRateTotal > 0.01f;
				bool needsTend = snap.needsTend;
				if (flag || needsTend)
				{
					num3++;
				}
				Need_Mood need_Mood = pawn.needs?.mood;
				if (need_Mood != null)
				{
					num5 += need_Mood.CurLevelPercentage;
					num4++;
				}
				CCPattern.Row row = new CCPattern.Row
				{
					pawn = pawn,
					bar = -1f
				};
				if (flag && pawn.Spawned)
				{
					int num6 = HealthUtility.TicksUntilDeathDueToBloodLoss(pawn);
					string arg = ((num6 < int.MaxValue) ? num6.ToStringTicksToPeriod(allowSeconds: true, shortForm: true) : "");
					CCPattern.AlertChip(ref row, needsTend ? "RCB_CC_BleedingUntended".Translate(arg.Named("TIME")).ToString() : "RCB_CC_Bleeding".Translate(arg.Named("TIME")).ToString());
					row.link = "RCB_CC_Tend".Translate();
					rowVerb.Add(0);
					goto IL_036f;
				}
				if (pawn.InMentalState && pawn.MentalStateDef != null)
				{
					CCPattern.AlertChip(ref row, pawn.MentalStateDef.LabelCap.ToString());
					row.link = "RCB_CC_Inspect".Translate();
					rowVerb.Add(1);
					goto IL_036f;
				}
				if (needsTend)
				{
					CCPattern.AlertChip(ref row, "RCB_CC_NeedsTending".Translate());
					row.link = "RCB_CC_Tend".Translate();
					rowVerb.Add(0);
					goto IL_036f;
				}
				if (need_Mood != null && snap.thrMinor > 0f && need_Mood.CurLevelPercentage < snap.thrMinor + 0.1f)
				{
					float num7 = (need_Mood.CurLevelPercentage - snap.thrMinor) * 100f;
					CCPattern.AlertChip(ref row, (num7 >= 0f) ? "RCB_CC_Stressed".Translate(Mathf.RoundToInt(num7).Named("PCT")).ToString() : "RCB_CC_BreakRisk".Translate(Mathf.RoundToInt(need_Mood.CurLevelPercentage * 100f).Named("PCT")).ToString());
					row.link = "RCB_CC_Inspect".Translate();
					rowVerb.Add(1);
					goto IL_036f;
				}
				goto end_IL_005f;
				IL_036f:
				rows.Add(row);
				end_IL_005f:;
			}
			catch
			{
			}
		}
		float num8 = 0f;
		try
		{
			List<Map> maps = Find.Maps;
			for (int j = 0; j < maps.Count; j++)
			{
				if (maps[j].IsPlayerHome)
				{
					num8 += maps[j].wealthWatcher.WealthTotal;
				}
			}
		}
		catch
		{
		}
		dayLine = "RCB_CC_ColonyDay".Translate(GenDate.DaysPassed.Named("DAY")).ToString();
		statValue[0] = SmallStrings.Int(num);
		statLabel[0] = "RCB_CC_StatColonists".Translate();
		statValue[1] = SmallStrings.Int(num2);
		statLabel[1] = "RCB_CC_StatDrafted".Translate();
		statValue[2] = SmallStrings.Int(num3);
		statLabel[2] = "RCB_CC_StatTending".Translate();
		statValue[3] = ((num4 > 0) ? (Mathf.RoundToInt(num5 / (float)num4 * 100f) + "%") : "-");
		statLabel[3] = "RCB_CC_StatMood".Translate();
		statValue[4] = num8.ToString("N0");
		statLabel[4] = "RCB_CC_StatWealth".Translate();
	}

	public void Draw(Rect r, CCContext ctx)
	{
		float y = r.y;
		CCPattern.Header(r, dayLine, ref y);
		float statCardH = UIKit.Flat.StatCardH;
		float num = r.x;
		for (int i = 0; i < statValue.Length; i++)
		{
			float num2 = UIKit.Flat.StatCardWidth(statValue[i], statLabel[i]);
			if (num + num2 > r.xMax && num > r.x)
			{
				num = r.x;
				y += statCardH + 10f;
			}
			UIKit.Flat.StatCard(new Rect(num, y, num2, statCardH), statValue[i], statLabel[i]);
			num += num2 + 10f;
		}
		y += statCardH + 18f;
		CCPattern.Header(r, "RCB_CC_NeedsAttention".Translate(), ref y);
		int num3 = CCPattern.Table(r, rows, ref scroll, y, "RCB_CC_AllQuiet".Translate());
		if (num3 < 0 || num3 >= rows.Count)
		{
			return;
		}
		Pawn pawn = rows[num3].pawn;
		if (rowVerb[num3] == 0)
		{
			HubRouter.OpenTab(pawn, HubTarget.Health);
			return;
		}
		try
		{
			CameraJumper.TryJumpAndSelect(pawn);
		}
		catch
		{
		}
	}
}
