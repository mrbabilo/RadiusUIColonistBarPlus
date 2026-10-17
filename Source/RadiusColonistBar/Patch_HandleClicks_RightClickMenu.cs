using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBarColonistDrawer), "HandleClicks")]
internal static class Patch_HandleClicks_RightClickMenu
{
	private const float DragThresholdSq = 36f;

	private static Vector2 pressPos;

	private static Pawn pressPawn;

	private static bool pressed;

	private static void Prefix(Rect rect, Pawn colonist)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return;
		}
		Event current = Event.current;
		if (current == null)
		{
			return;
		}
		Rect bounds;
		Rect rect2 = (ReorderBounds.TryGet(colonist, out bounds) ? bounds : rect);
		if (current.type == EventType.MouseDown && current.button == 1 && Mouse.IsOver(rect2))
		{
			pressed = true;
			pressPos = current.mousePosition;
			pressPawn = colonist;
		}
		else
		{
			if (current.type != EventType.MouseUp || current.button != 1 || !Mouse.IsOver(rect2))
			{
				return;
			}
			bool flag = pressed && pressPawn == colonist && (current.mousePosition - pressPos).sqrMagnitude < 36f;
			pressed = false;
			pressPawn = null;
			if (flag)
			{
				if (current.shift)
				{
					Dialog_CommandCenter.OpenFor(colonist, typeof(Module_Loadouts));
				}
				else
				{
					OpenInteractionMenu(colonist);
				}
				current.Use();
			}
		}
	}

	private static void OpenInteractionMenu(Pawn target)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		try
		{
			if (target != null && target.Spawned && target.Map == Find.CurrentMap)
			{
				Vector3 drawPos = target.DrawPos;
				string text = "GoHere".Translate();
				List<Pawn> list2 = new List<Pawn>();
				List<Pawn> selectedPawns = Find.Selector.SelectedPawns;
				if (selectedPawns != null)
				{
					for (int i = 0; i < selectedPawns.Count; i++)
					{
						Pawn pawn = selectedPawns[i];
						if (pawn != null && pawn.IsColonistPlayerControlled && pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.Map == Find.CurrentMap)
						{
							list2.Add(pawn);
						}
					}
				}
				FloatMenuContext context;
				if (list2.Count > 0)
				{
					List<FloatMenuOption> options = FloatMenuMakerMap.GetOptions(list2, drawPos, out context);
					for (int j = 0; j < options.Count; j++)
					{
						if (options[j] != null && options[j].Label != text)
						{
							list.Add(options[j]);
						}
					}
				}
				else
				{
					List<string> list3 = new List<string>();
					Dictionary<string, List<KeyValuePair<Pawn, FloatMenuOption>>> dictionary = new Dictionary<string, List<KeyValuePair<Pawn, FloatMenuOption>>>();
					List<Pawn> freeColonists = Find.CurrentMap.mapPawns.FreeColonists;
					List<object> selectedObjects = Find.Selector.SelectedObjects;
					List<object> collection = new List<object>(selectedObjects);
					try
					{
						for (int k = 0; k < freeColonists.Count; k++)
						{
							Pawn pawn2 = freeColonists[k];
							if (pawn2 == null || pawn2.Dead || !pawn2.Spawned || pawn2.Downed || pawn2 == target)
							{
								continue;
							}
							selectedObjects.Clear();
							selectedObjects.Add(pawn2);
							List<FloatMenuOption> options2;
							try
							{
								options2 = FloatMenuMakerMap.GetOptions(new List<Pawn> { pawn2 }, drawPos, out context);
							}
							catch
							{
								continue;
							}
							for (int l = 0; l < options2.Count; l++)
							{
								FloatMenuOption floatMenuOption = options2[l];
								if (floatMenuOption != null && !floatMenuOption.Disabled && !(floatMenuOption.Label == text))
								{
									if (!dictionary.TryGetValue(floatMenuOption.Label, out var value))
									{
										value = new List<KeyValuePair<Pawn, FloatMenuOption>>();
										dictionary[floatMenuOption.Label] = value;
										list3.Add(floatMenuOption.Label);
									}
									value.Add(new KeyValuePair<Pawn, FloatMenuOption>(pawn2, floatMenuOption));
								}
							}
						}
					}
					finally
					{
						selectedObjects.Clear();
						selectedObjects.AddRange(collection);
					}
					for (int m = 0; m < list3.Count; m++)
					{
						List<KeyValuePair<Pawn, FloatMenuOption>> list4 = dictionary[list3[m]];
						if (list4.Count == 1)
						{
							Pawn key = list4[0].Key;
							FloatMenuOption opt = list4[0].Value;
							list.Add(new FloatMenuOption(key.LabelShortCap + ": " + list3[m], delegate
							{
								try
								{
									opt.action?.Invoke();
								}
								catch
								{
								}
							}));
							continue;
						}
						List<KeyValuePair<Pawn, FloatMenuOption>> captured = list4;
						list.Add(new FloatMenuOptionSub(list3[m], delegate
						{
							List<FloatMenuOption> list5 = new List<FloatMenuOption>();
							for (int n = 0; n < captured.Count; n++)
							{
								Pawn key2 = captured[n].Key;
								FloatMenuOption opt2 = captured[n].Value;
								list5.Add(new FloatMenuOption(key2.LabelShortCap, delegate
								{
									try
									{
										opt2.action?.Invoke();
									}
									catch
									{
									}
								}));
							}
							return list5;
						}));
					}
				}
			}
		}
		catch
		{
		}
		if (target != null)
		{
			list.Add(new FloatMenuOption((RcbCohorts.IsPinned(target) ? "RCB_Pin_Remove" : "RCB_Pin_Add").Translate(), delegate
			{
				RcbCohorts.TogglePin(target);
			}));
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			if (settings != null && settings.followCam)
			{
				list.Add(new FloatMenuOption(FollowCam.Following(target) ? "Stop following" : "Follow with camera", delegate
				{
					FollowCam.Toggle(target);
				}));
			}
			BarSquads sqh = BarSquads.Get;
			if (sqh != null)
			{
				List<Pawn> group = Affected(target);
				string text2 = ((group.Count > 1) ? (" (" + group.Count + " selected)") : "");
				list.Add(new FloatMenuOption("Hide from bar" + text2, delegate
				{
					foreach (Pawn item in group)
					{
						sqh.Hide(item);
					}
				}));
			}
		}
		Find.WindowStack.Add(new RcbFloatMenu(list));
	}

	internal static void OpenPoliciesMenu(Pawn p)
	{
		Find.WindowStack.Add(new RcbFloatMenu(BuildPoliciesMenu(p)));
	}

	private static List<FloatMenuOption> BuildPoliciesMenu(Pawn p)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		List<Pawn> targets = Affected(p);
		int n = targets.Count;
		// Colonist Bar Plus: these texts were hard-coded in English; they go through keys, shared with the squad rules.
		string text = ((n > 1) ? ("  " + "RCB_Policies_Selected".Translate(n)) : "");
		try
		{
			if (p.outfits != null)
			{
				string text2 = Shared(targets, (Pawn t) => t.outfits?.CurrentApparelPolicy?.label);
				list.Add(new FloatMenuOptionSub("RCB_Policies_Line".Translate(SquadRules.Label(SquadRules.Rule.Apparel), text2) + text, delegate
				{
					List<FloatMenuOption> list2 = new List<FloatMenuOption>();
					foreach (ApparelPolicy allOutfit in Current.Game.outfitDatabase.AllOutfits)
					{
						ApparelPolicy pp = allOutfit;
						list2.Add(new FloatMenuOption(pp.label, delegate
						{
							foreach (Pawn item in targets)
							{
								try
								{
									if (item.outfits != null)
									{
										item.outfits.CurrentApparelPolicy = pp;
									}
								}
								catch
								{
								}
							}
							Confirm(SquadRules.Label(SquadRules.Rule.Apparel), pp.label, n);
						}));
					}
					return list2;
				}));
			}
			if (p.foodRestriction != null)
			{
				string text3 = Shared(targets, (Pawn t) => t.foodRestriction?.CurrentFoodPolicy?.label);
				list.Add(new FloatMenuOptionSub("RCB_Policies_Line".Translate(SquadRules.Label(SquadRules.Rule.Food), text3) + text, delegate
				{
					List<FloatMenuOption> list2 = new List<FloatMenuOption>();
					foreach (FoodPolicy allFoodRestriction in Current.Game.foodRestrictionDatabase.AllFoodRestrictions)
					{
						FoodPolicy pp = allFoodRestriction;
						list2.Add(new FloatMenuOption(pp.label, delegate
						{
							foreach (Pawn item2 in targets)
							{
								try
								{
									if (item2.foodRestriction != null)
									{
										item2.foodRestriction.CurrentFoodPolicy = pp;
									}
								}
								catch
								{
								}
							}
							Confirm(SquadRules.Label(SquadRules.Rule.Food), pp.label, n);
						}));
					}
					return list2;
				}));
			}
			if (p.drugs != null)
			{
				string text4 = Shared(targets, (Pawn t) => t.drugs?.CurrentPolicy?.label);
				list.Add(new FloatMenuOptionSub("RCB_Policies_Line".Translate(SquadRules.Label(SquadRules.Rule.Drugs), text4) + text, delegate
				{
					List<FloatMenuOption> list2 = new List<FloatMenuOption>();
					foreach (DrugPolicy allPolicy in Current.Game.drugPolicyDatabase.AllPolicies)
					{
						DrugPolicy pp = allPolicy;
						list2.Add(new FloatMenuOption(pp.label, delegate
						{
							foreach (Pawn item3 in targets)
							{
								try
								{
									if (item3.drugs != null)
									{
										item3.drugs.CurrentPolicy = pp;
									}
								}
								catch
								{
								}
							}
							Confirm(SquadRules.Label(SquadRules.Rule.Drugs), pp.label, n);
						}));
					}
					return list2;
				}));
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			list.Add(new FloatMenuOption("RCB_Policies_NoneAvailable".Translate(), null)
			{
				Disabled = true
			});
		}
		return list;
	}

	private static string Shared(List<Pawn> targets, Func<Pawn, string> get)
	{
		string text = null;
		bool flag = false;
		for (int i = 0; i < targets.Count; i++)
		{
			string text2 = null;
			try
			{
				text2 = get(targets[i]);
			}
			catch
			{
			}
			if (!flag)
			{
				text = text2;
				flag = true;
			}
			else if (text2 != text)
			{
				return "RCB_Rules_Mixed".Translate();
			}
		}
		return text ?? "RCB_Policies_None".Translate().ToString();
	}

	private static void Confirm(string kind, string label, int n)
	{
		if (n > 1)
		{
			Messages.Message("RCB_Policies_Set".Translate(kind, label, n), MessageTypeDefOf.SilentInput, historical: false);
		}
	}

	private static List<Pawn> Affected(Pawn target)
	{
		try
		{
			List<Pawn> selectedPawns = Find.Selector.SelectedPawns;
			if (selectedPawns != null && selectedPawns.Count > 1 && selectedPawns.Contains(target))
			{
				return new List<Pawn>(selectedPawns);
			}
		}
		catch
		{
		}
		return new List<Pawn> { target };
	}
}
