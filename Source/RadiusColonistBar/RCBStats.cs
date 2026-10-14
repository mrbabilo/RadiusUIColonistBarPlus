using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RCBStats
{
	public struct ReadinessInfo
	{
		public float total;

		public float condition;

		public float offense;

		public float defense;

		public float effDps;

		public float sharp;

		public float blunt;

		public string dpsSrc;

		public bool pacifist;
	}

	private struct RdyCacheEntry
	{
		public float at;

		public ReadinessInfo info;
	}

	public struct WorldBench
	{
		public float tech;

		public float avgPower;

		public float expDps;

		public float expSharp;

		public float expBlunt;

		public int factions;
	}

	private static readonly Dictionary<string, NeedDef> needCache = new Dictionary<string, NeedDef>();

	private static readonly Dictionary<int, RdyCacheEntry> rdyCache = new Dictionary<int, RdyCacheEntry>();

	private static readonly float[] DpsRamp = new float[8] { 4f, 4f, 4f, 5.5f, 10f, 13f, 15f, 16f };

	private static readonly float[] SharpRamp = new float[8] { 0.12f, 0.12f, 0.14f, 0.3f, 0.55f, 0.85f, 1f, 1.1f };

	private static readonly float[] BluntRamp = new float[8] { 0.1f, 0.1f, 0.12f, 0.25f, 0.35f, 0.55f, 0.7f, 0.8f };

	private static WorldBench bench;

	private static int benchAt = -999999;

	private static NeedDef CachedNeed(string key)
	{
		if (!needCache.TryGetValue(key, out var value))
		{
			value = DefDatabase<NeedDef>.GetNamedSilentFail(key);
			needCache[key] = value;
		}
		return value;
	}

	public static float Health(Pawn p)
	{
		try
		{
			return Mathf.Clamp01(p.health.summaryHealth.SummaryHealthPercent);
		}
		catch
		{
			return 1f;
		}
	}

	public static float Readiness(Pawn p)
	{
		return ReadinessFor(p).total;
	}

	public static ReadinessInfo ReadinessFor(Pawn p)
	{
		if (p == null)
		{
			return default(ReadinessInfo);
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (rdyCache.TryGetValue(p.thingIDNumber, out var value) && realtimeSinceStartup - value.at < 0.75f)
		{
			return value.info;
		}
		ReadinessInfo readinessInfo = ComputeReadiness(p);
		if (rdyCache.Count > 512)
		{
			rdyCache.Clear();
		}
		rdyCache[p.thingIDNumber] = new RdyCacheEntry
		{
			at = realtimeSinceStartup,
			info = readinessInfo
		};
		return readinessInfo;
	}

	private static ReadinessInfo ComputeReadiness(Pawn p)
	{
		ReadinessInfo result = new ReadinessInfo
		{
			dpsSrc = "unarmed"
		};
		try
		{
			if (p.Dead || p.Downed)
			{
				return result;
			}
			WorldBench worldBench = Benchmark();
			PawnCapacitiesHandler pawnCapacitiesHandler = p.health?.capacities;
			float num = Health(p);
			float num2 = pawnCapacitiesHandler?.GetLevel(PawnCapacityDefOf.Consciousness) ?? 1f;
			float num3 = pawnCapacitiesHandler?.GetLevel(PawnCapacityDefOf.Moving) ?? 1f;
			float num4 = pawnCapacitiesHandler?.GetLevel(PawnCapacityDefOf.Manipulation) ?? 1f;
			result.condition = Mathf.Clamp01(num * num2 * (0.55f + 0.45f * num3) * (0.7f + 0.3f * num4));
			result.effDps = EffectiveDps(p, out result.dpsSrc);
			result.offense = Mathf.Clamp01(result.effDps / Mathf.Max(0.1f, worldBench.expDps));
			result.sharp = OverallArmor(p, StatDefOf.ArmorRating_Sharp);
			result.blunt = OverallArmor(p, StatDefOf.ArmorRating_Blunt);
			result.defense = Mathf.Clamp01(0.65f * (result.sharp / Mathf.Max(0.01f, worldBench.expSharp)) + 0.35f * (result.blunt / Mathf.Max(0.01f, worldBench.expBlunt)));
			float num5 = 0.6f * result.offense + 0.4f * result.defense;
			float num6 = result.condition * (0.15f + 0.85f * num5);
			result.pacifist = p.WorkTagIsDisabled(WorkTags.Violent);
			if (result.pacifist)
			{
				num6 *= 0.15f;
			}
			result.total = Mathf.Clamp01(num6);
		}
		catch
		{
		}
		return result;
	}

	public static string ReadinessTip(Pawn p)
	{
		try
		{
			ReadinessInfo readinessInfo = ReadinessFor(p);
			WorldBench worldBench = Benchmark();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(p.LabelShortCap).Append(" - combat readiness ").Append(Pct01(readinessInfo.total));
			stringBuilder.Append("\n\nCondition ").Append(Pct01(readinessInfo.condition)).Append("  (health, consciousness, mobility)");
			stringBuilder.Append("\nOffense ").Append(Pct01(readinessInfo.offense)).Append("  (")
				.Append(readinessInfo.effDps.ToString("0.0"))
				.Append(" effective DPS, ")
				.Append(readinessInfo.dpsSrc)
				.Append(", vs ")
				.Append(worldBench.expDps.ToString("0.0"))
				.Append(" expected)");
			stringBuilder.Append("\nDefense ").Append(Pct01(readinessInfo.defense)).Append("  (armor ")
				.Append(Pct01(readinessInfo.sharp))
				.Append(" sharp / ")
				.Append(Pct01(readinessInfo.blunt))
				.Append(" blunt, vs ")
				.Append(Pct01(worldBench.expSharp))
				.Append(" / ")
				.Append(Pct01(worldBench.expBlunt))
				.Append(" expected)");
			if (readinessInfo.pacifist)
			{
				stringBuilder.Append("\nIncapable of violence: x0.15");
			}
			stringBuilder.Append("\n\nWorld benchmark: ").Append(TechName(worldBench.tech)).Append("-era enemies");
			if (worldBench.factions > 0)
			{
				stringBuilder.Append(" (").Append(worldBench.factions).Append((worldBench.factions == 1) ? " faction" : " factions")
					.Append(", avg combat power ")
					.Append(Mathf.RoundToInt(worldBench.avgPower))
					.Append(")");
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return "";
		}
	}

	private static string Pct01(float f)
	{
		return Mathf.RoundToInt(f * 100f) + "%";
	}

	private static string TechName(float t)
	{
		try
		{
			return ((TechLevel)Mathf.Clamp(Mathf.RoundToInt(t), 0, 7)).ToStringHuman().CapitalizeFirst();
		}
		catch
		{
			return "Industrial";
		}
	}

	private static float Ramp(float[] a, float t)
	{
		t = Mathf.Clamp(t, 0f, a.Length - 1);
		int num = Mathf.FloorToInt(t);
		int num2 = Mathf.Min(num + 1, a.Length - 1);
		return Mathf.Lerp(a[num], a[num2], t - (float)num);
	}

	public static WorldBench Benchmark()
	{
		int num = 0;
		try
		{
			num = Find.TickManager?.TicksGame ?? 0;
		}
		catch
		{
		}
		if (bench.expDps > 0f && num - benchAt < 2500)
		{
			return bench;
		}
		benchAt = num;
		WorldBench result = new WorldBench
		{
			tech = 4f,
			avgPower = 65f
		};
		try
		{
			FactionManager factionManager = Find.FactionManager;
			if (factionManager != null)
			{
				Dictionary<Faction, int> dictionary = new Dictionary<Faction, int>();
				try
				{
					List<Settlement> list = Find.WorldObjects?.Settlements;
					if (list != null)
					{
						for (int i = 0; i < list.Count; i++)
						{
							Faction faction = list[i]?.Faction;
							if (faction != null)
							{
								dictionary.TryGetValue(faction, out var value);
								dictionary[faction] = value + 1;
							}
						}
					}
				}
				catch
				{
				}
				List<Faction> allFactionsListForReading = factionManager.AllFactionsListForReading;
				float num2 = 0f;
				float num3 = 0f;
				float num4 = 0f;
				float num5 = 0f;
				int num6 = 0;
				for (int j = 0; j < 2; j++)
				{
					if (num6 != 0)
					{
						break;
					}
					num2 = (num3 = (num4 = (num5 = 0f)));
					for (int k = 0; k < allFactionsListForReading.Count; k++)
					{
						Faction faction2 = allFactionsListForReading[k];
						if (faction2 == null || faction2.IsPlayer || faction2.defeated || faction2.Hidden || faction2.temporary)
						{
							continue;
						}
						FactionDef def = faction2.def;
						if (def == null || !def.humanlikeFaction || (j == 0 && !faction2.HostileTo(Faction.OfPlayer)))
						{
							continue;
						}
						dictionary.TryGetValue(faction2, out var value2);
						float num7 = 1f + (float)value2;
						num2 += num7;
						num3 += (float)(int)faction2.def.techLevel * num7;
						num6++;
						List<PawnGroupMaker> pawnGroupMakers = faction2.def.pawnGroupMakers;
						if (pawnGroupMakers == null)
						{
							continue;
						}
						for (int l = 0; l < pawnGroupMakers.Count; l++)
						{
							PawnGroupMaker pawnGroupMaker = pawnGroupMakers[l];
							if (pawnGroupMaker == null || pawnGroupMaker.kindDef != PawnGroupKindDefOf.Combat || pawnGroupMaker.options == null)
							{
								continue;
							}
							for (int m = 0; m < pawnGroupMaker.options.Count; m++)
							{
								PawnGenOption pawnGenOption = pawnGroupMaker.options[m];
								if (pawnGenOption?.kind != null)
								{
									float num8 = Mathf.Max(0.01f, pawnGenOption.selectionWeight) * num7;
									num4 += pawnGenOption.kind.combatPower * num8;
									num5 += num8;
								}
							}
						}
					}
				}
				if (num6 > 0)
				{
					result.tech = num3 / Mathf.Max(1f, num2);
					result.factions = num6;
					if (num5 > 0f)
					{
						result.avgPower = num4 / num5;
					}
				}
			}
		}
		catch
		{
		}
		float num9 = Mathf.Clamp(result.avgPower / 65f, 0.8f, 1.25f);
		result.expDps = Ramp(DpsRamp, result.tech) * num9;
		result.expSharp = Ramp(SharpRamp, result.tech) * num9;
		result.expBlunt = Ramp(BluntRamp, result.tech) * num9;
		bench = result;
		return result;
	}

	public static float OverallArmor(Pawn p, StatDef stat)
	{
		try
		{
			float num = Mathf.Clamp01(p.GetStatValue(stat) / 2f);
			List<BodyPartRecord> list = p.RaceProps?.body?.AllParts;
			if (list == null)
			{
				return 0f;
			}
			List<Apparel> list2 = p.apparel?.WornApparel;
			float num2 = 0f;
			for (int i = 0; i < list.Count; i++)
			{
				float num3 = 1f - num;
				if (list2 != null)
				{
					for (int j = 0; j < list2.Count; j++)
					{
						if (list2[j].def.apparel.CoversBodyPart(list[i]))
						{
							num3 *= 1f - Mathf.Clamp01(list2[j].GetStatValue(stat) / 2f);
						}
					}
				}
				num2 += list[i].coverageAbs * (1f - num3);
			}
			return Mathf.Clamp(num2 * 2f, 0f, 2f);
		}
		catch
		{
			return 0f;
		}
	}

	public static float EffectiveDps(Pawn p, out string src)
	{
		src = "unarmed";
		float num = 0f;
		try
		{
			ThingWithComps thingWithComps = p.equipment?.Primary;
			float num2 = RangedEffDps(p, thingWithComps);
			if (num2 > num)
			{
				num = num2;
				src = thingWithComps.def.label;
			}
			if (SidearmsBridge.Active)
			{
				ThingOwner<Thing> thingOwner = p.inventory?.innerContainer;
				if (thingOwner != null)
				{
					for (int i = 0; i < thingOwner.Count; i++)
					{
						Thing thing = thingOwner[i];
						if (thing?.def != null && thing.def.IsRangedWeapon)
						{
							float num3 = RangedEffDps(p, thing);
							if (num3 > num)
							{
								num = num3;
								src = thing.def.label;
							}
						}
					}
				}
			}
			StatWorker worker = StatDefOf.MeleeDPS.Worker;
			if (worker == null || !worker.IsDisabledFor(p))
			{
				float num4 = p.GetStatValue(StatDefOf.MeleeDPS);
				try
				{
					StatWorker worker2 = StatDefOf.MeleeHitChance.Worker;
					if (worker2 == null || !worker2.IsDisabledFor(p))
					{
						num4 *= Mathf.Clamp01(p.GetStatValue(StatDefOf.MeleeHitChance));
					}
				}
				catch
				{
				}
				if (num4 > num)
				{
					num = num4;
					src = ((thingWithComps != null && thingWithComps.def?.IsMeleeWeapon == true) ? thingWithComps.def.label : "fists");
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private static float RangedEffDps(Pawn p, Thing weapon)
	{
		try
		{
			VerbProperties verbProperties = RangedVerb(weapon?.def);
			if (verbProperties == null)
			{
				return -1f;
			}
			float num = RangedDps(weapon, p);
			if (num < 0f)
			{
				return -1f;
			}
			float dist = Mathf.Clamp(15f, verbProperties.minRange + 0.1f, Mathf.Max(verbProperties.minRange + 0.1f, verbProperties.range));
			return num * AdjustedHitFactor(verbProperties, weapon, p, dist);
		}
		catch
		{
			return -1f;
		}
	}

	public static float Mood(Pawn p)
	{
		try
		{
			Need_Mood need_Mood = p.needs?.mood;
			return (need_Mood != null) ? Mathf.Clamp01(need_Mood.CurLevelPercentage) : 0.5f;
		}
		catch
		{
			return 0.5f;
		}
	}

	private static string Short(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "?";
		}
		s = s.CapitalizeFirst();
		return (s.Length > 5) ? s.Substring(0, 5) : s;
	}

	public static void ResolveValue(Pawn p, string key, out float frac, out Color col, out bool isHealth)
	{
		Resolve(p, key, null, out var _, out frac, out col, out isHealth, wantLabel: false);
	}

	public static void Resolve(Pawn p, string key, string fallback, out string label, out float frac, out Color col, out bool isHealth)
	{
		Resolve(p, key, fallback, out label, out frac, out col, out isHealth, wantLabel: true);
	}

	private static void Resolve(Pawn p, string key, string fallback, out string label, out float frac, out Color col, out bool isHealth, bool wantLabel)
	{
		isHealth = false;
		if (key == "__none__")
		{
			label = "";
			frac = 0f;
			col = RcbPalette.TextDim2;
			return;
		}
		if (string.IsNullOrEmpty(key) || key == "__health__")
		{
			label = "Health";
			frac = Health(p);
			col = CardPalette.ZoneColor(frac);
			isHealth = true;
			return;
		}
		switch (key)
		{
		case "__mood__":
			label = "Mood";
			frac = Mood(p);
			col = CardPalette.MoodColor(p, frac);
			return;
		case "__mmb__":
		{
			label = "Mind";
			frac = (MmbBridge.TryGetMH(p, out var mh) ? mh : 0f);
			col = CardPalette.ZoneColor(frac);
			return;
		}
		case "__psyfocus__":
		{
			float num3 = 0f;
			try
			{
				num3 = Mathf.Clamp01(p.psychicEntropy?.CurrentPsyfocus ?? 0f);
			}
			catch
			{
			}
			label = "Psy";
			frac = num3;
			col = CardPalette.ZoneColor(num3);
			return;
		}
		case "__entropy__":
		{
			float num2 = 0f;
			try
			{
				Pawn_PsychicEntropyTracker psychicEntropy = p.psychicEntropy;
				if (psychicEntropy != null && psychicEntropy.MaxEntropy > 0.01f)
				{
					num2 = Mathf.Clamp01(psychicEntropy.EntropyValue / psychicEntropy.MaxEntropy);
				}
			}
			catch
			{
			}
			label = "Heat";
			frac = num2;
			col = CardPalette.ZoneColor(1f - num2);
			return;
		}
		case "__readiness__":
		{
			float num = Readiness(p);
			label = "Ready";
			frac = num;
			col = CardPalette.ZoneColor(num);
			return;
		}
		}
		try
		{
			NeedDef needDef = CachedNeed(key);
			if (needDef != null)
			{
				Need need = p.needs?.TryGetNeed(needDef);
				label = (wantLabel ? Short(needDef.label) : null);
				frac = ((need != null) ? Mathf.Clamp01(need.CurLevelPercentage) : 0f);
				col = CardPalette.ZoneColor(frac);
				return;
			}
		}
		catch
		{
		}
		label = fallback;
		frac = 0f;
		col = RcbPalette.TextDim2;
	}

	public static float RangedDps(Thing weapon, Pawn shooter = null)
	{
		try
		{
			VerbProperties verbProperties = RangedVerb(weapon?.def);
			if (verbProperties == null)
			{
				return -1f;
			}
			int num = ShotDamage(weapon, verbProperties);
			if (num <= 0)
			{
				return -1f;
			}
			int num2 = Mathf.Max(1, verbProperties.burstShotCount);
			float num3 = 1f;
			if (shooter != null)
			{
				try
				{
					num3 = shooter.GetStatValue(StatDefOf.AimingDelayFactor);
				}
				catch
				{
				}
			}
			float num4 = verbProperties.warmupTime * num3 + weapon.GetStatValue(StatDefOf.RangedWeapon_Cooldown) + (float)((num2 - 1) * verbProperties.ticksBetweenBurstShots) / 60f;
			return (num4 > 0.001f) ? ((float)(num * num2) / num4) : (-1f);
		}
		catch
		{
			return -1f;
		}
	}

	private static int ShotDamage(Thing weapon, VerbProperties vp)
	{
		try
		{
			return (weapon.TryGetComp<CompChangeableProjectile>()?.Projectile?.projectile ?? vp.defaultProjectile?.projectile)?.GetDamageAmount(weapon) ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	public static VerbProperties RangedVerb(ThingDef def)
	{
		List<VerbProperties> list = def?.Verbs;
		if (list == null)
		{
			return null;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] != null && !list[i].IsMeleeAttack)
			{
				return list[i];
			}
		}
		return null;
	}

	private static float AdjustedHitFactor(VerbProperties vp, Thing weapon, Pawn shooter, float dist)
	{
		float num = vp.GetHitChanceFactor(weapon, dist);
		if (shooter != null && CanShoot(shooter))
		{
			try
			{
				num *= ShotReport.HitFactorFromShooter(shooter, dist);
			}
			catch
			{
			}
		}
		return Mathf.Min(num, 1f);
	}

	private static bool CanShoot(Pawn p)
	{
		try
		{
			return !(StatDefOf.ShootingAccuracyPawn.Worker?.IsDisabledFor(p) ?? false);
		}
		catch
		{
			return false;
		}
	}

	public static string RangedDpsTip(Pawn p, Thing weapon)
	{
		try
		{
			VerbProperties verbProperties = RangedVerb(weapon?.def);
			float num = RangedDps(weapon, p);
			if (verbProperties == null || num < 0f)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(weapon.LabelShortCap).Append("\nSustained DPS: ").Append(num.ToString("0.0"));
			stringBuilder.Append("\nRange: ").Append((verbProperties.minRange > 0.01f) ? (verbProperties.minRange.ToString("0") + "-") : "").Append(verbProperties.range.ToString("0"));
			float[] array = new float[4] { 3f, 12f, 25f, 40f };
			string[] array2 = new string[4] { "touch", "short", "medium", "long" };
			stringBuilder.Append("\n\nAdjusted DPS (").Append((p != null) ? p.LabelShortCap : "weapon").Append("):");
			for (int i = 0; i < array.Length; i++)
			{
				if (!(array[i] > verbProperties.range + 0.01f) && !(array[i] < verbProperties.minRange - 0.01f))
				{
					float num2 = AdjustedHitFactor(verbProperties, weapon, p, array[i]);
					stringBuilder.Append("\n  ").Append(array2[i]).Append(" (")
						.Append(array[i].ToString("0"))
						.Append("): ")
						.Append((num * num2).ToString("0.0"))
						.Append("  (")
						.Append(Mathf.RoundToInt(num2 * 100f))
						.Append("% hit)");
				}
			}
			int num3 = Mathf.Max(1, Mathf.CeilToInt(verbProperties.minRange));
			int num4 = Mathf.FloorToInt(verbProperties.range);
			if (num4 >= num3)
			{
				int value = num3;
				float num5 = -1f;
				for (int j = num3; j <= num4; j++)
				{
					float num6 = AdjustedHitFactor(verbProperties, weapon, p, j);
					if (num6 > num5)
					{
						num5 = num6;
						value = j;
					}
				}
				stringBuilder.Append("\nOptimal range: ").Append(value).Append(" (")
					.Append((num * num5).ToString("0.0"))
					.Append(" DPS)");
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return "";
		}
	}

	public static string KeyLabel(string key)
	{
		if (key == "__none__")
		{
			return "(none)";
		}
		if (string.IsNullOrEmpty(key) || key == "__health__")
		{
			return "Health";
		}
		switch (key)
		{
		case "__mood__":
			return "Mood";
		case "__mmb__":
			return "Mental health";
		case "__psyfocus__":
			return "Psyfocus";
		case "__entropy__":
			return "Neural heat";
		case "__readiness__":
			return "Combat readiness";
		default:
		{
			NeedDef namedSilentFail = DefDatabase<NeedDef>.GetNamedSilentFail(key);
			return (namedSilentFail != null) ? namedSilentFail.LabelCap.ToString() : key;
		}
		}
	}

	public static List<FloatMenuOption> StatMenu(Action<string> pick)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>
		{
			new FloatMenuOption("Health", delegate
			{
				pick("__health__");
			}),
			new FloatMenuOption("Mood", delegate
			{
				pick("__mood__");
			}),
			new FloatMenuOption("Combat readiness", delegate
			{
				pick("__readiness__");
			})
		};
		if (ModsConfig.RoyaltyActive)
		{
			list.Add(new FloatMenuOption("Psyfocus", delegate
			{
				pick("__psyfocus__");
			}));
			list.Add(new FloatMenuOption("Neural heat", delegate
			{
				pick("__entropy__");
			}));
		}
		if (MmbBridge.Active)
		{
			list.Add(new FloatMenuOption("Mental health (MMB)", delegate
			{
				pick("__mmb__");
			}));
		}
		foreach (NeedDef item in DefDatabase<NeedDef>.AllDefsListForReading)
		{
			NeedDef d = item;
			list.Add(new FloatMenuOption(d.LabelCap.ToString(), delegate
			{
				pick(d.defName);
			}));
		}
		return list;
	}
}
