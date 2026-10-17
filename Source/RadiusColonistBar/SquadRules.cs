using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

// Squad rules (Colonist Bar Plus): the vanilla policies of the Assign tab, read and set for a whole squad at once.
// They live on the pawns, as vanilla keeps them: nothing is saved. Each rule only touches the members vanilla would
// let the player set it for, with the same checks as the Assign tab columns.
internal static class SquadRules
{
	internal enum Rule
	{
		Apparel,
		Food,
		Drugs,
		Medical,
		Area,
		Hostility
	}

	internal static readonly Rule[] All = new Rule[6]
	{
		Rule.Apparel,
		Rule.Food,
		Rule.Drugs,
		Rule.Medical,
		Rule.Area,
		Rule.Hostility
	};

	// Living members on every map, in caravans and in transporters. A copy: the PawnsFinder list is shared and
	// refilled on every call.
	internal static List<Pawn> Members(int index)
	{
		List<Pawn> list = new List<Pawn>();
		BarSquads get = BarSquads.Get;
		if (get == null || index < 0)
		{
			return list;
		}
		List<Pawn> list2 = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
		for (int i = 0; i < list2.Count; i++)
		{
			if (get.SquadIndexOf(list2[i]) == index)
			{
				list.Add(list2[i]);
			}
		}
		return list;
	}

	// The checks of the Assign tab columns. An area belongs to a map: only members on the current map.
	internal static bool Applies(Pawn p, Rule rule)
	{
		switch (rule)
		{
		case Rule.Apparel:
			return p.outfits != null;
		case Rule.Food:
			return p.foodRestriction != null;
		case Rule.Drugs:
			return p.drugs != null;
		case Rule.Medical:
			return p.playerSettings != null;
		case Rule.Area:
			return p.playerSettings != null && p.Faction == Faction.OfPlayer && (!p.IsMutant || p.mutant.Def.respectsAllowedArea) && p.playerSettings.SupportsAllowedAreas && p.MapHeld != null && p.MapHeld == Find.CurrentMap;
		default:
			return p.playerSettings != null && p.RaceProps.Humanlike;
		}
	}

	internal static string Label(Rule rule)
	{
		return ("RCB_Rules_" + rule).Translate();
	}

	internal static string ValueLabel(Pawn p, Rule rule)
	{
		switch (rule)
		{
		case Rule.Apparel:
			return p.outfits.CurrentApparelPolicy?.label ?? "-";
		case Rule.Food:
			return p.foodRestriction.CurrentFoodPolicy?.label ?? "-";
		case Rule.Drugs:
			return p.drugs.CurrentPolicy?.label ?? "-";
		case Rule.Medical:
			return p.playerSettings.medCare.GetLabel();
		case Rule.Area:
			return p.playerSettings.AreaRestrictionInPawnCurrentMap?.Label ?? "NoAreaAllowed".Translate().ToString();
		default:
			return p.playerSettings.hostilityResponse.GetLabel();
		}
	}

	// The members this rule applies to, and their shared value; null when none applies.
	internal static string Shared(List<Pawn> members, Rule rule, List<Pawn> into, out bool mixed)
	{
		mixed = false;
		into.Clear();
		string text = null;
		for (int i = 0; i < members.Count; i++)
		{
			Pawn pawn = members[i];
			if (!Applies(pawn, rule))
			{
				continue;
			}
			into.Add(pawn);
			string text2 = ValueLabel(pawn, rule);
			if (text == null)
			{
				text = text2;
			}
			else if (text2 != text)
			{
				mixed = true;
			}
		}
		return text;
	}

	internal static void OpenMenu(int index, Rule rule, string current)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		switch (rule)
		{
		case Rule.Apparel:
			foreach (ApparelPolicy allOutfit in Current.Game.outfitDatabase.AllOutfits)
			{
				ApparelPolicy policy = allOutfit;
				list.Add(Option(policy.label, current, delegate
				{
					Set(index, rule, policy.label, delegate(Pawn p)
					{
						p.outfits.CurrentApparelPolicy = policy;
						return true;
					});
				}));
			}
			break;
		case Rule.Food:
			foreach (FoodPolicy allFoodRestriction in Current.Game.foodRestrictionDatabase.AllFoodRestrictions)
			{
				FoodPolicy policy2 = allFoodRestriction;
				list.Add(Option(policy2.label, current, delegate
				{
					Set(index, rule, policy2.label, delegate(Pawn p)
					{
						p.foodRestriction.CurrentFoodPolicy = policy2;
						return true;
					});
				}));
			}
			break;
		case Rule.Drugs:
			foreach (DrugPolicy allPolicy in Current.Game.drugPolicyDatabase.AllPolicies)
			{
				DrugPolicy policy3 = allPolicy;
				list.Add(Option(policy3.label, current, delegate
				{
					Set(index, rule, policy3.label, delegate(Pawn p)
					{
						p.drugs.CurrentPolicy = policy3;
						return true;
					});
				}));
			}
			break;
		case Rule.Medical:
			for (int i = 0; i < 5; i++)
			{
				MedicalCareCategory care = (MedicalCareCategory)i;
				list.Add(Option(care.GetLabel(), current, delegate
				{
					Set(index, rule, care.GetLabel(), delegate(Pawn p)
					{
						p.playerSettings.medCare = care;
						return true;
					});
				}));
			}
			break;
		case Rule.Area:
		{
			Map currentMap = Find.CurrentMap;
			if (currentMap == null)
			{
				return;
			}
			string none = "NoAreaAllowed".Translate();
			list.Add(Option(none, current, delegate
			{
				Set(index, rule, none, delegate(Pawn p)
				{
					p.playerSettings.AreaRestrictionInPawnCurrentMap = null;
					return true;
				});
			}));
			foreach (Area allArea in currentMap.areaManager.AllAreas)
			{
				Area area = allArea;
				if (area.AssignableAsAllowed())
				{
					list.Add(Option(area.Label, current, delegate
					{
						Set(index, rule, area.Label, delegate(Pawn p)
						{
							p.playerSettings.AreaRestrictionInPawnCurrentMap = area;
							return true;
						});
					}));
				}
			}
			break;
		}
		default:
			foreach (HostilityResponseMode value in System.Enum.GetValues(typeof(HostilityResponseMode)))
			{
				HostilityResponseMode mode = value;
				FloatMenuOption floatMenuOption = Option(mode.GetLabel(), current, delegate
				{
					Set(index, rule, mode.GetLabel(), delegate(Pawn p)
					{
						// As in vanilla's response button: a pawn incapable of violence never gets Attack.
						if (mode == HostilityResponseMode.Attack && p.WorkTagIsDisabled(WorkTags.Violent))
						{
							return false;
						}
						p.playerSettings.hostilityResponse = mode;
						return true;
					});
				});
				list.Add(floatMenuOption);
			}
			break;
		}
		if (list.Count > 0)
		{
			Find.WindowStack.Add(new RcbFloatMenu(list));
		}
	}

	private static FloatMenuOption Option(string label, string current, System.Action action)
	{
		return new FloatMenuOption((label == current) ? ("RCB_Rules_Current".Translate(label).ToString()) : label, action);
	}

	// Sets the rule on every member it applies to; set returns false for a member it must skip (only Attack does).
	// A setter that throws is logged by SquadOrders.Try and counted nowhere.
	private static void Set(int index, Rule rule, string valueLabel, System.Func<Pawn, bool> set)
	{
		int num = 0;
		int skipped = 0;
		foreach (Pawn pawn in Members(index))
		{
			if (!Applies(pawn, rule))
			{
				continue;
			}
			SquadOrders.Try(pawn, delegate
			{
				if (set(pawn))
				{
					num++;
				}
				else
				{
					skipped++;
				}
			});
		}
		SquadStatus.Invalidate();
		string text = "RCB_Rules_Set".Translate(Label(rule), valueLabel, num).ToString();
		if (skipped > 0)
		{
			text = text + " " + "RCB_Rules_Skipped".Translate(skipped).ToString();
		}
		Messages.Message(text, MessageTypeDefOf.SilentInput, historical: false);
	}

	internal static bool HasEditor(Rule rule)
	{
		return rule == Rule.Apparel || rule == Rule.Food || rule == Rule.Drugs || rule == Rule.Area;
	}

	// Opens vanilla's manager on the first member's policy. The caller closes the card first: it sits above dialogs.
	internal static void OpenEditor(Rule rule, List<Pawn> members)
	{
		Pawn pawn = ((members.Count > 0) ? members[0] : null);
		switch (rule)
		{
		case Rule.Apparel:
			Find.WindowStack.Add(new Dialog_ManageApparelPolicies(pawn?.outfits?.CurrentApparelPolicy));
			break;
		case Rule.Food:
			Find.WindowStack.Add(new Dialog_ManageFoodPolicies(pawn?.foodRestriction?.CurrentFoodPolicy));
			break;
		case Rule.Drugs:
			Find.WindowStack.Add(new Dialog_ManageDrugPolicies(pawn?.drugs?.CurrentPolicy));
			break;
		case Rule.Area:
			if (Find.CurrentMap != null)
			{
				Find.WindowStack.Add(new Dialog_ManageAreas(Find.CurrentMap));
			}
			break;
		}
	}

	internal static int ForcedCount(List<Pawn> members)
	{
		int num = 0;
		for (int i = 0; i < members.Count; i++)
		{
			if (members[i].outfits != null && members[i].outfits.forcedHandler.SomethingIsForced)
			{
				num++;
			}
		}
		return num;
	}

	internal static void ReleaseForced(int index)
	{
		foreach (Pawn pawn in Members(index))
		{
			if (pawn.outfits != null && pawn.outfits.forcedHandler.SomethingIsForced)
			{
				SquadOrders.Try(pawn, delegate
				{
					pawn.outfits.forcedHandler.Reset();
				});
			}
		}
		SquadStatus.Invalidate();
	}
}
