using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus). The vanilla fight giver picks cover and melee; this subclass narrows its
// target to the pawn's assault mode, shoots that target instead of letting Wait_Combat auto-target, and never
// leaves the pawn waiting forever when no enemy is around.
public class JobGiver_SquadAssault : JobGiver_AIFightEnemies
{
	private const int MaxTierPasses = 3;

	private static readonly IntRange IdleRecheck = new IntRange(120, 250);

	// Filter of the pass in progress, read by ExtraTargetValidator; null outside a filtered pass. Think nodes are
	// shared between pawns and the game runs them on one thread, so a static is enough.
	private static Predicate<Thing> passFilter;

	private static readonly List<float> tiers = new List<float>();

	protected override Job TryGiveJob(Pawn pawn)
	{
		// The vanilla giver gives nothing to a colonist whose hostility response is not Attack: set it for the
		// call only, so nothing stays on the pawn.
		Pawn_PlayerSettings playerSettings = pawn.playerSettings;
		HostilityResponseMode hostilityResponseMode = playerSettings?.hostilityResponse ?? HostilityResponseMode.Attack;
		Job job;
		try
		{
			if (playerSettings != null)
			{
				playerSettings.hostilityResponse = HostilityResponseMode.Attack;
			}
			job = base.TryGiveJob(pawn);
		}
		finally
		{
			if (playerSettings != null)
			{
				playerSettings.hostilityResponse = hostilityResponseMode;
			}
		}
		if (job == null)
		{
			return JobMaker.MakeJob(JobDefOf.Wait_Combat, IdleRecheck.RandomInRange, checkOverrideOnExpiry: true);
		}
		if (job.def == JobDefOf.Wait_Combat)
		{
			Thing enemyTarget = pawn.mindState.enemyTarget;
			Verb verb = ((enemyTarget == null) ? null : pawn.TryGetAttackVerb(enemyTarget));
			if (verb != null && !verb.verbProps.IsMeleeAttack && verb.CanHitTarget(enemyTarget))
			{
				Job job2 = JobMaker.MakeJob(JobDefOf.AttackStatic, enemyTarget);
				job2.expiryInterval = ExpiryInterval_ShooterSucceeded.RandomInRange;
				job2.checkOverrideOnExpire = true;
				job2.endIfCantShootTargetFromCurPos = true;
				return job2;
			}
		}
		return job;
	}

	protected override Thing FindAttackTarget(Pawn pawn)
	{
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault == null || !squadAssault.TryGetMode(pawn, out var mode))
		{
			return base.FindAttackTarget(pawn);
		}
		Thing thing = null;
		if (mode == AssaultMode.Fleeing)
		{
			thing = FilteredPass(pawn, IsFleeing);
		}
		else
		{
			CollectTiers(pawn, mode == AssaultMode.Strongest);
			for (int i = 0; i < tiers.Count && i < MaxTierPasses && thing == null; i++)
			{
				float tier = tiers[i];
				thing = FilteredPass(pawn, (Thing t) => t is Pawn p && Mathf.Approximately(p.kindDef.combatPower, tier));
			}
		}
		return thing ?? base.FindAttackTarget(pawn);
	}

	protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
	{
		if (!base.ExtraTargetValidator(pawn, target))
		{
			return false;
		}
		return passFilter == null || passFilter(target);
	}

	private Thing FilteredPass(Pawn pawn, Predicate<Thing> filter)
	{
		passFilter = filter;
		try
		{
			return base.FindAttackTarget(pawn);
		}
		finally
		{
			passFilter = null;
		}
	}

	// Distinct combat powers of the enemy pawns within acquire range, strongest or weakest first. Turrets and other
	// non-pawn targets have no combat power: only the vanilla fallback takes them.
	private void CollectTiers(Pawn pawn, bool strongestFirst)
	{
		tiers.Clear();
		List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
		float num = targetAcquireRadius * targetAcquireRadius;
		for (int i = 0; i < potentialTargetsFor.Count; i++)
		{
			if (potentialTargetsFor[i].Thing is Pawn pawn2 && !pawn2.Downed && !pawn2.Fogged() && (float)(pawn2.Position - pawn.Position).LengthHorizontalSquared <= num)
			{
				float combatPower = pawn2.kindDef.combatPower;
				if (!tiers.Contains(combatPower))
				{
					tiers.Add(combatPower);
				}
			}
		}
		tiers.Sort();
		if (strongestFirst)
		{
			tiers.Reverse();
		}
	}

	// Fleeing covers panicked pawns and raiders leaving the map, not only the Flee jobs.
	private static bool IsFleeing(Thing t)
	{
		if (!(t is Pawn pawn))
		{
			return false;
		}
		JobDef curJobDef = pawn.CurJobDef;
		if (curJobDef == JobDefOf.Flee || curJobDef == JobDefOf.FleeAndCower)
		{
			return true;
		}
		if (pawn.MentalStateDef == MentalStateDefOf.PanicFlee)
		{
			return true;
		}
		DutyDef dutyDef = pawn.mindState?.duty?.def;
		if (dutyDef == DutyDefOf.ExitMapRandom || dutyDef == DutyDefOf.ExitMapBest)
		{
			return true;
		}
		return pawn.CurJob != null && pawn.CurJob.exitMapOnArrival;
	}
}
