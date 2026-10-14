using System.Collections.Generic;
using RadiusUI.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
public static class PawnStatusUtil
{
	public struct Status
	{
		public Texture2D icon;

		public Color color;

		public string text;
	}

	private static Texture2D icBreak;

	private static Texture2D icFlee;

	private static Texture2D icInspired;

	private static Texture2D icDanger;

	private static Texture2D icFocus;

	private static HediffDef dsDef;

	private static HediffDef efDef;

	private static MentalStateDef panicFlee;

	private static JobDef fleeJob;

	private static JobDef fleeCower;

	private static bool init;

	private static readonly Color BreakCol = Color.white;

	private static readonly Color FleeCol = Color.white;

	private static readonly Color InspiredCol = Color.white;

	private static readonly Color DangerCol = Color.white;

	private static readonly Color FocusCol = Color.white;

	private static void Init()
	{
		if (!init)
		{
			init = true;
			icBreak = IconSet.Status.Break;
			icFlee = IconSet.Status.Flee;
			icInspired = IconSet.Status.Inspired;
			icDanger = IconSet.Status.Danger;
			icFocus = IconSet.Status.Focus;
			panicFlee = DefDatabase<MentalStateDef>.GetNamedSilentFail("PanicFlee");
			fleeJob = DefDatabase<JobDef>.GetNamedSilentFail("Flee");
			fleeCower = DefDatabase<JobDef>.GetNamedSilentFail("FleeAndCower");
			dsDef = DefDatabase<HediffDef>.GetNamedSilentFail("AO_DangerSense");
			efDef = DefDatabase<HediffDef>.GetNamedSilentFail("AO_EngagementFocus");
		}
	}

	private static bool InBreak(Pawn p)
	{
		try
		{
			return p.InMentalState && p.MentalStateDef != panicFlee;
		}
		catch
		{
			return false;
		}
	}

	public static bool DownedIsIncidental(Pawn p)
	{
		try
		{
			if (p == null)
			{
				return false;
			}
			if (!p.Spawned)
			{
				return true;
			}
			return LifeStageUtility.AlwaysDowned(p);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsFleeing(Pawn p)
	{
		try
		{
			if (panicFlee != null && p.MentalStateDef == panicFlee)
			{
				return true;
			}
			JobDef curJobDef = p.CurJobDef;
			if (curJobDef != null && (curJobDef == fleeJob || curJobDef == fleeCower))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public static bool TryGetBadge(Pawn p, out Status badge)
	{
		Init();
		badge = default(Status);
		try
		{
			if (InBreak(p))
			{
				badge = new Status
				{
					icon = icBreak,
					color = BreakCol
				};
				return true;
			}
			if (IsFleeing(p))
			{
				badge = new Status
				{
					icon = icFlee,
					color = FleeCol
				};
				return true;
			}
			InspirationHandler inspirationHandler = p.mindState?.inspirationHandler;
			if (inspirationHandler != null && inspirationHandler.Inspired)
			{
				badge = new Status
				{
					icon = icInspired,
					color = InspiredCol
				};
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public static void Gather(Pawn p, List<Status> list)
	{
		Init();
		list.Clear();
		try
		{
			if (InBreak(p))
			{
				list.Add(new Status
				{
					icon = icBreak,
					color = BreakCol,
					text = "Break: " + p.MentalStateDef.LabelCap
				});
			}
			if (IsFleeing(p))
			{
				list.Add(new Status
				{
					icon = icFlee,
					color = FleeCol,
					text = "Fleeing"
				});
			}
			InspirationHandler inspirationHandler = p.mindState?.inspirationHandler;
			if (inspirationHandler != null && inspirationHandler.Inspired && inspirationHandler.CurStateDef != null)
			{
				list.Add(new Status
				{
					icon = icInspired,
					color = InspiredCol,
					text = "Inspired: " + inspirationHandler.CurStateDef.LabelCap
				});
			}
			if (dsDef != null)
			{
				Hediff firstHediffOfDef = p.health.hediffSet.GetFirstHediffOfDef(dsDef);
				if (firstHediffOfDef != null && firstHediffOfDef.Severity > 0.02f)
				{
					list.Add(new Status
					{
						icon = icDanger,
						color = DangerCol,
						text = "Danger sense: " + StageLabel(firstHediffOfDef) + " " + firstHediffOfDef.Severity.ToStringPercent()
					});
				}
			}
			if (efDef != null)
			{
				Hediff firstHediffOfDef2 = p.health.hediffSet.GetFirstHediffOfDef(efDef);
				if (firstHediffOfDef2 != null && firstHediffOfDef2.Severity > 0.02f)
				{
					list.Add(new Status
					{
						icon = icFocus,
						color = FocusCol,
						text = "Engagement: " + StageLabel(firstHediffOfDef2) + " " + firstHediffOfDef2.Severity.ToStringPercent()
					});
				}
			}
		}
		catch
		{
		}
	}

	private static string StageLabel(Hediff h)
	{
		try
		{
			return (h.CurStage != null && !h.CurStage.label.NullOrEmpty()) ? h.CurStage.label : h.def.label;
		}
		catch
		{
			return "";
		}
	}
}
