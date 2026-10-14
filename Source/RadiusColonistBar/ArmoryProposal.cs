using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal sealed class ArmoryProposal
{
	public struct Change
	{
		public Pawn pawn;

		public Thing current;

		public Thing proposed;

		public float scoreNow;

		public float scoreNew;
	}

	private struct Cand
	{
		public Thing thing;

		public bool ranged;

		public float meleeDps;
	}

	private const float MinGain = 0.1f;

	private const float MinAbs = 0.5f;

	public readonly List<Change> changes = new List<Change>();

	public int computedTick;

	public int Count => changes.Count;

	public bool Empty => changes.Count == 0;

	public static ArmoryProposal Compute(List<Pawn> roster)
	{
		ArmoryProposal armoryProposal = new ArmoryProposal
		{
			computedTick = (Find.TickManager?.TicksGame ?? 0)
		};
		if (roster == null || roster.Count == 0)
		{
			return armoryProposal;
		}
		List<Pawn> list = new List<Pawn>(roster.Count);
		for (int i = 0; i < roster.Count; i++)
		{
			Pawn pawn = roster[i];
			if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Map == null || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony || pawn.equipment == null)
			{
				continue;
			}
			try
			{
				if (pawn.WorkTagIsDisabled(WorkTags.Violent))
				{
					continue;
				}
			}
			catch
			{
				continue;
			}
			list.Add(pawn);
		}
		if (list.Count == 0)
		{
			return armoryProposal;
		}
		List<Cand> list2 = new List<Cand>();
		HashSet<Thing> hashSet = new HashSet<Thing>();
		List<Map> list3 = new List<Map>();
		for (int j = 0; j < list.Count; j++)
		{
			if (!list3.Contains(list[j].Map))
			{
				list3.Add(list[j].Map);
			}
		}
		for (int k = 0; k < list3.Count; k++)
		{
			List<Thing> list4;
			try
			{
				list4 = list3[k].listerThings.ThingsInGroup(ThingRequestGroup.Weapon);
			}
			catch
			{
				continue;
			}
			for (int l = 0; l < list4.Count; l++)
			{
				Thing thing = list4[l];
				try
				{
					if (thing?.def != null && thing.Spawned && hashSet.Add(thing) && thing.def.IsWeapon && !thing.def.IsApparel && thing.def.stackLimit <= 1 && !thing.Position.Fogged(thing.Map) && !thing.IsForbidden(Faction.OfPlayer) && !(thing.ParentHolder is Pawn))
					{
						list2.Add(MakeCand(thing));
					}
				}
				catch
				{
				}
			}
		}
		Cand[] array = new Cand[list.Count];
		float[] array2 = new float[list.Count];
		for (int m = 0; m < list.Count; m++)
		{
			ThingWithComps primary = list[m].equipment.Primary;
			array[m] = ((primary != null) ? MakeCand(primary) : default(Cand));
			array2[m] = ((primary != null) ? Score(list[m], array[m]) : 0f);
		}
		int count = list.Count;
		int count2 = list2.Count;
		if (count2 == 0)
		{
			return armoryProposal;
		}
		float[] array3 = new float[count * count2];
		for (int n = 0; n < count; n++)
		{
			Pawn pawn2 = list[n];
			bool flag = false;
			try
			{
				flag = pawn2.story?.traits?.HasTrait(TraitDefOf.Brawler) == true;
			}
			catch
			{
			}
			for (int num = 0; num < count2; num++)
			{
				Cand c = list2[num];
				float num2 = -1f;
				if (c.thing.Map == pawn2.Map && (!flag || !c.ranged) && CCOrders.Blocker(pawn2, c.thing, weapon: true) == null)
				{
					num2 = Score(pawn2, c);
				}
				array3[n * count2 + num] = num2;
			}
		}
		bool[] array4 = new bool[count];
		bool[] array5 = new bool[count2];
		while (true)
		{
			int num3 = -1;
			int num4 = -1;
			float num5 = 0f;
			for (int num6 = 0; num6 < count; num6++)
			{
				if (array4[num6])
				{
					continue;
				}
				float num7 = array2[num6];
				float num8 = Mathf.Max(0.5f, num7 * 0.1f);
				for (int num9 = 0; num9 < count2; num9++)
				{
					if (array5[num9])
					{
						continue;
					}
					float num10 = array3[num6 * count2 + num9];
					if (!(num10 < 0f))
					{
						float num11 = num10 - num7;
						if (num11 >= num8 && num11 > num5)
						{
							num5 = num11;
							num3 = num6;
							num4 = num9;
						}
					}
				}
			}
			if (num3 < 0)
			{
				break;
			}
			array4[num3] = true;
			array5[num4] = true;
			armoryProposal.changes.Add(new Change
			{
				pawn = list[num3],
				current = array[num3].thing,
				proposed = list2[num4].thing,
				scoreNow = array2[num3],
				scoreNew = array3[num3 * count2 + num4]
			});
		}
		armoryProposal.changes.Sort((Change a, Change b) => (b.scoreNew - b.scoreNow).CompareTo(a.scoreNew - a.scoreNow));
		return armoryProposal;
	}

	private static Cand MakeCand(Thing t)
	{
		Cand result = new Cand
		{
			thing = t,
			ranged = t.def.IsRangedWeapon
		};
		if (!result.ranged)
		{
			try
			{
				result.meleeDps = t.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS, applyPostProcess: true, 120);
			}
			catch
			{
				result.meleeDps = 0f;
			}
		}
		return result;
	}

	private static float Score(Pawn p, Cand c)
	{
		if (c.thing == null)
		{
			return 0f;
		}
		try
		{
			return c.ranged ? Mathf.Max(0f, RCBStats.RangedDps(c.thing, p)) : c.meleeDps;
		}
		catch
		{
			return 0f;
		}
	}

	public int Apply()
	{
		int num = 0;
		for (int i = 0; i < changes.Count; i++)
		{
			Change change = changes[i];
			try
			{
				if (change.pawn != null && change.proposed != null && change.pawn.equipment?.Primary == change.current && CCOrders.Blocker(change.pawn, change.proposed, weapon: true) == null)
				{
					CCOrders.TryOrder(change.pawn, JobDefOf.Equip, change.proposed);
					num++;
				}
			}
			catch
			{
			}
		}
		changes.Clear();
		return num;
	}
}
