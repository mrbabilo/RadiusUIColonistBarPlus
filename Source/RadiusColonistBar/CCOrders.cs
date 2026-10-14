using RimWorld;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

internal static class CCOrders
{
	public static string Blocker(Pawn p, Thing t, bool weapon)
	{
		try
		{
			if (p == null)
			{
				return "no pawn selected";
			}
			if (t == null || !t.Spawned)
			{
				return "item unavailable";
			}
			if (p.Dead || p.Downed || !p.Spawned)
			{
				return p.LabelShortCap + " unavailable";
			}
			if (p.Map != t.Map)
			{
				return "different map";
			}
			if (weapon && p.WorkTagIsDisabled(WorkTags.Violent))
			{
				return "incapable of violence";
			}
			if (!weapon)
			{
				try
				{
					if (!ApparelUtility.HasPartsToWear(p, t.def))
					{
						return "cannot wear";
					}
				}
				catch
				{
				}
			}
			try
			{
				if (!EquipmentUtility.CanEquip(t, p, out var cantReason))
				{
					return cantReason;
				}
			}
			catch
			{
			}
			if (t.IsForbidden(p))
			{
				return "forbidden";
			}
			if (!p.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
			{
				return "cannot reach";
			}
			return null;
		}
		catch
		{
			return "unavailable";
		}
	}

	public static void TryOrder(Pawn p, JobDef job, Thing t)
	{
		try
		{
			p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(job, t), JobTag.Misc);
		}
		catch
		{
		}
	}
}
