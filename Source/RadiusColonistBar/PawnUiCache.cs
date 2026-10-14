using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RadiusColonistBar;

internal static class PawnUiCache
{
	internal sealed class Snap
	{
		public int nextRefresh = int.MinValue;

		public string name;

		public string royalTitle;

		public string jobTitle;

		public string role;

		public string job;

		public int psylink;

		public float heatSev;

		public float hypoSev;

		public float toxicSev;

		public bool plague;

		public bool anes;

		public float bloodLevel = 1f;

		public float tatter;

		public bool hardLabor;

		public bool sadWander;

		public bool needsTend;

		public Ability readyAbility;

		public CtxKind ctx;

		public float thrExtreme;

		public float thrMajor;

		public float thrMinor;

		public Color nameColor = Color.white;

		public Color ideoColor;

		public bool hasIdeoColor;

		public bool hasFrame;

		public Color frameHue;

		public Texture2D frameGlyph;

		public Texture2D frameEmblem;

		public List<Thing> weapons;

		public Pawn partner;

		public bool presentationSkipped;
	}

	private const int Interval = 15;

	private static readonly Dictionary<int, Snap> snaps = new Dictionary<int, Snap>();

	private static readonly Snap Empty = new Snap();

	private static HediffDef hypoDef;

	private static HediffDef heatDef;

	private static HediffDef toxicDef;

	private static HediffDef plagueDef;

	private static HediffDef anesDef;

	private static bool defsInit;

	private const float PartnerRange = 6f;

	private static readonly Comparison<Thing> byThingId = (Thing a, Thing b) => a.thingIDNumber.CompareTo(b.thingIDNumber);

	private static int IntervalFrames
	{
		get
		{
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			return (settings != null) ? Mathf.Clamp(settings.perfSnapshotFrames, 5, 60) : 15;
		}
	}

	private static bool Minimized => RcbIslandAnim.ContentA < 0.01f;

	public static Snap Get(Pawn p)
	{
		if (p == null)
		{
			return Empty;
		}
		int thingIDNumber = p.thingIDNumber;
		if (!snaps.TryGetValue(thingIDNumber, out var value))
		{
			value = new Snap();
			snaps[thingIDNumber] = value;
		}
		int frameCount = Time.frameCount;
		if (frameCount >= value.nextRefresh || (value.presentationSkipped && !Minimized))
		{
			Refresh(p, value);
			value.nextRefresh = frameCount + IntervalFrames + thingIDNumber % 7;
		}
		return value;
	}

	public static void Prune(HashSet<int> live)
	{
		RcbUtil.PruneById(snaps, live);
	}

	private static Pawn ResolvePartner(Pawn p)
	{
		if (p == null || !p.Spawned || p.Dead)
		{
			return null;
		}
		Job job = p.jobs?.curJob;
		if (job != null)
		{
			if (job.targetA.Thing is Pawn pawn && ValidPartner(p, pawn))
			{
				return pawn;
			}
			if (job.targetB.Thing is Pawn pawn2 && ValidPartner(p, pawn2))
			{
				return pawn2;
			}
		}
		if (p.stances?.curStance is Stance_Busy stance_Busy && stance_Busy.focusTarg.Thing is Pawn pawn3 && ValidPartner(p, pawn3))
		{
			return pawn3;
		}
		return null;
	}

	private static bool ValidPartner(Pawn p, Pawn t)
	{
		if (t == null || t == p || !t.Spawned || t.Dead || t.Map != p.Map)
		{
			return false;
		}
		return (float)p.Position.DistanceToSquared(t.Position) <= 36f;
	}

	private static void Refresh(Pawn p, Snap s)
	{
		if (!defsInit)
		{
			defsInit = true;
			hypoDef = DefDatabase<HediffDef>.GetNamedSilentFail("Hypothermia");
			heatDef = DefDatabase<HediffDef>.GetNamedSilentFail("Heatstroke");
			toxicDef = DefDatabase<HediffDef>.GetNamedSilentFail("ToxicBuildup");
			plagueDef = DefDatabase<HediffDef>.GetNamedSilentFail("Plague");
			anesDef = DefDatabase<HediffDef>.GetNamedSilentFail("Anesthetic");
		}
		s.heatSev = Sev(p, heatDef);
		s.hypoSev = Sev(p, hypoDef);
		s.toxicSev = Sev(p, toxicDef);
		try
		{
			s.plague = plagueDef != null && p.health.hediffSet.GetFirstHediffOfDef(plagueDef) != null;
		}
		catch
		{
			s.plague = false;
		}
		try
		{
			s.anes = anesDef != null && p.health.hediffSet.HasHediff(anesDef);
		}
		catch
		{
			s.anes = false;
		}
		try
		{
			s.needsTend = HealthAIUtility.ShouldBeTendedNowByPlayer(p);
		}
		catch
		{
			s.needsTend = false;
		}
		try
		{
			MentalBreaker mentalBreaker = p.mindState?.mentalBreaker;
			if (mentalBreaker != null)
			{
				s.thrExtreme = mentalBreaker.BreakThresholdExtreme;
				s.thrMajor = mentalBreaker.BreakThresholdMajor;
				s.thrMinor = mentalBreaker.BreakThresholdMinor;
			}
			else
			{
				s.thrExtreme = 0f;
				s.thrMajor = 0f;
				s.thrMinor = 0f;
			}
		}
		catch
		{
			s.thrExtreme = 0f;
			s.thrMajor = 0f;
			s.thrMinor = 0f;
		}
		try
		{
			s.name = p.Name?.ToStringShort ?? p.LabelShortCap;
		}
		catch
		{
			s.name = null;
		}
		if (Minimized)
		{
			s.presentationSkipped = true;
			return;
		}
		s.presentationSkipped = false;
		RefreshPresentation(p, s);
	}

	private static void RefreshPresentation(Pawn p, Snap s)
	{
		try
		{
			s.partner = ResolvePartner(p);
		}
		catch
		{
			s.partner = null;
		}
		try
		{
			RoyalTitle royalTitle = p.royalty?.MostSeniorTitle;
			s.royalTitle = ((royalTitle?.def != null) ? royalTitle.def.GetLabelCapFor(p) : null);
		}
		catch
		{
			s.royalTitle = null;
		}
		try
		{
			s.jobTitle = p.story?.TitleCap;
		}
		catch
		{
			s.jobTitle = null;
		}
		try
		{
			FrameSources.Resolve(p, s, RadiusColonistBarMod.Settings);
		}
		catch
		{
			s.hasFrame = false;
		}
		s.role = null;
		s.hasIdeoColor = false;
		if (ModsConfig.IdeologyActive)
		{
			try
			{
				Ideo ideo = p.Ideo;
				s.role = ideo?.GetRole(p)?.LabelForPawn(p);
				if (ideo != null)
				{
					s.ideoColor = ideo.Color;
					s.hasIdeoColor = true;
				}
			}
			catch
			{
			}
		}
		try
		{
			string text = null;
			try
			{
				text = p.jobs?.curDriver?.GetReport();
			}
			catch
			{
			}
			if (string.IsNullOrEmpty(text))
			{
				try
				{
					text = p.jobs?.curJob?.def?.reportString;
				}
				catch
				{
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				try
				{
					text = p.MentalState?.InspectLine;
				}
				catch
				{
				}
			}
			if (!string.IsNullOrEmpty(text) && text.Contains("Target"))
			{
				text = null;
			}
			s.job = (string.IsNullOrEmpty(text) ? null : text.CapitalizeFirst());
		}
		catch
		{
			s.job = null;
		}
		try
		{
			s.psylink = p.GetPsylinkLevel();
		}
		catch
		{
			s.psylink = 0;
		}
		try
		{
			s.bloodLevel = Mathf.Clamp01(1f - (p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss)?.Severity ?? 0f));
		}
		catch
		{
			s.bloodLevel = 1f;
		}
		s.tatter = ComputeTatter(p);
		s.hardLabor = ComputeHardLabor(p);
		s.sadWander = ComputeSadWander(p);
		s.readyAbility = ComputeReadyAbility(p);
		s.ctx = ComputeCtx(p);
		try
		{
			s.nameColor = PawnNameColorUtility.PawnNameColorOf(p);
		}
		catch
		{
			s.nameColor = Color.white;
		}
		RefreshWeapons(p, s);
	}

	private static void RefreshWeapons(Pawn p, Snap s)
	{
		if (s.weapons == null)
		{
			s.weapons = new List<Thing>(2);
		}
		else
		{
			s.weapons.Clear();
		}
		try
		{
			ThingWithComps thingWithComps = p.equipment?.Primary;
			if (thingWithComps != null && thingWithComps.def.IsWeapon)
			{
				s.weapons.Add(thingWithComps);
			}
			if (SidearmsBridge.Active)
			{
				ThingOwner<Thing> thingOwner = p.inventory?.innerContainer;
				if (thingOwner != null)
				{
					for (int i = 0; i < thingOwner.Count; i++)
					{
						Thing thing = thingOwner[i];
						ThingDef thingDef = thing?.def;
						if (thingDef != null && thingDef.IsWeapon && !thingDef.IsApparel && !thingDef.IsIngestible && !thingDef.IsStuff)
						{
							s.weapons.Add(thing);
						}
					}
				}
			}
			if (s.weapons.Count > 1)
			{
				s.weapons.Sort(byThingId);
			}
		}
		catch
		{
		}
	}

	private static float Sev(Pawn p, HediffDef def)
	{
		if (def == null)
		{
			return 0f;
		}
		try
		{
			Hediff firstHediffOfDef = p.health.hediffSet.GetFirstHediffOfDef(def);
			return (firstHediffOfDef != null) ? Mathf.Clamp01(firstHediffOfDef.Severity) : 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private static float ComputeTatter(Pawn p)
	{
		try
		{
			List<Apparel> list = p.apparel?.WornApparel;
			if (list == null || list.Count == 0)
			{
				return 0f;
			}
			float num = 1f;
			for (int i = 0; i < list.Count; i++)
			{
				Apparel apparel = list[i];
				if (apparel.def.useHitPoints && apparel.MaxHitPoints > 0)
				{
					num = Mathf.Min(num, (float)apparel.HitPoints / (float)apparel.MaxHitPoints);
				}
			}
			return Mathf.Clamp01((0.5f - num) / 0.5f);
		}
		catch
		{
			return 0f;
		}
	}

	private static bool ComputeHardLabor(Pawn p)
	{
		try
		{
			WorkTypeDef workTypeDef = p.CurJob?.workGiverDef?.workType;
			return workTypeDef == WorkTypeDefOf.Mining || workTypeDef == WorkTypeDefOf.Construction;
		}
		catch
		{
			return false;
		}
	}

	private static bool ComputeSadWander(Pawn p)
	{
		try
		{
			if (!p.InMentalState || p.MentalStateDef == null)
			{
				return false;
			}
			string defName = p.MentalStateDef.defName;
			return defName.Contains("Wander") || defName.Contains("Daze") || defName.Contains("Sad");
		}
		catch
		{
			return false;
		}
	}

	private static Ability ComputeReadyAbility(Pawn p)
	{
		try
		{
			List<Ability> list = p.abilities?.AllAbilitiesForReading;
			if (list == null)
			{
				return null;
			}
			for (int i = 0; i < list.Count; i++)
			{
				Ability ability = list[i];
				if (ability?.def != null && ability.CooldownTicksRemaining <= 0 && (bool)ability.CanCast)
				{
					return ability;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static CtxKind ComputeCtx(Pawn p)
	{
		try
		{
			if (p.IsBurning())
			{
				return CtxKind.Burning;
			}
			if (p.Spawned && p.stances?.curStance is Stance_Warmup)
			{
				return CtxKind.Aiming;
			}
			if (p.IsFormingCaravan())
			{
				return CtxKind.Caravan;
			}
			if (GiddyUpBridge.IsMounted(p))
			{
				return CtxKind.Mounted;
			}
			if (p.Drafted && AutoCombatBridge.IsAutoCombat(p))
			{
				return CtxKind.AutoCombat;
			}
			if (p.InBed() && p.CurrentBed() != null && p.CurrentBed().Medical)
			{
				return CtxKind.MedicalRest;
			}
			WorkTypeDef workTypeDef = p.CurJob?.workGiverDef?.workType;
			if (workTypeDef == WorkTypeDefOf.Doctor)
			{
				return CtxKind.Doctoring;
			}
			if (workTypeDef == WorkTypeDefOf.Research)
			{
				return CtxKind.Researching;
			}
			if (!p.Awake())
			{
				return CtxKind.Sleeping;
			}
			if (p.mindState != null && p.mindState.IsIdle && GenDate.DaysPassed >= 1)
			{
				return CtxKind.Idle;
			}
		}
		catch
		{
		}
		return CtxKind.None;
	}
}
