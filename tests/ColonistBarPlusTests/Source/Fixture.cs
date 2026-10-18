using System;
using System.Collections.Generic;
using System.Linq;
using RadiusColonistBar;
using RimWorld;
using Verse;
using Verse.AI;

namespace RCBPlusTests;

// Shared scene builder: colonists, a squad, trees to cut, meals, beds and a recreation spot near home.
internal static class Fixture
{
	internal static Map Map => Find.CurrentMap;

	internal static IntVec3 Home;

	internal static void Bind()
	{
		Home = Map.mapPawns.FreeColonistsSpawned.FirstOrDefault()?.Position ?? Map.Center;
	}

	internal static Pawn NewColonist(params WorkTypeDef[] capableOf)
	{
		for (int i = 0; i < 40; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer, null, forceGenerateNewPawn: true));
			if (pawn.workSettings != null && !pawn.workSettings.EverWork)
			{
				pawn.workSettings.EnableAndInitialize();
			}
			if (capableOf.Any((WorkTypeDef w) => pawn.WorkTypeIsDisabled(w)) || pawn.InMentalState || pawn.needs.joy == null)
			{
				pawn.Destroy();
				continue;
			}
			GenSpawn.Spawn(pawn, FreeCell(6), Map);
			foreach (WorkTypeDef w in capableOf)
			{
				pawn.workSettings.SetPriority(w, 3);
			}
			pawn.needs.food.CurLevelPercentage = 1f;
			pawn.needs.rest.CurLevel = 1f;
			pawn.needs.joy.CurLevel = 1f;
			return pawn;
		}
		throw new Exception("no suitable colonist generated");
	}

	internal static int Squad(params Pawn[] pawns)
	{
		BarSquads get = BarSquads.Get;
		int num = get.NewSquad();
		foreach (Pawn p in pawns)
		{
			get.Assign(p, num);
		}
		return num;
	}

	internal static IntVec3 FreeCell(int radius, IntVec3? near = null)
	{
		IntVec3 center = near ?? Home;
		for (int r = radius; r < 60; r += 4)
		{
			if (CellFinder.TryFindRandomCellNear(center, Map, r, (IntVec3 c) => c.Standable(Map) && !c.Fogged(Map) && c.GetFirstItem(Map) == null && c.GetFirstPawn(Map) == null && c.GetPlant(Map) == null && c.GetFirstBuilding(Map) == null && Map.reachability.CanReach(Home, c, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly), out var result))
			{
				return result;
			}
		}
		throw new Exception("no free cell");
	}

	internal static Plant Tree(IntVec3 cell)
	{
		Plant plant = (Plant)ThingMaker.MakeThing(ThingDef.Named("Plant_TreeOak"));
		GenSpawn.Spawn(plant, cell, Map, WipeMode.Vanish);
		plant.Growth = 1f;
		Map.designationManager.AddDesignation(new Designation(plant, DesignationDefOf.CutPlant));
		return plant;
	}

	internal static void Meals()
	{
		Thing thing = ThingMaker.MakeThing(ThingDef.Named("MealSurvivalPack"));
		thing.stackCount = 10;
		GenPlace.TryPlaceThing(thing, FreeCell(4), Map, ThingPlaceMode.Near);
	}

	internal static void Beds(int count)
	{
		for (int i = 0; i < count; i++)
		{
			Building_Bed building_Bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
			building_Bed.SetFaction(Faction.OfPlayer);
			GenSpawn.Spawn(building_Bed, FreeCell(8), Map, Rot4.North, WipeMode.Vanish);
		}
	}

	internal static void JoySpot()
	{
		Thing thing = ThingMaker.MakeThing(ThingDef.Named("HorseshoesPin"), ThingDefOf.WoodLog);
		thing.SetFaction(Faction.OfPlayer);
		GenSpawn.Spawn(thing, FreeCell(8), Map, Rot4.North, WipeMode.Vanish);
	}

	internal static void Disable(Pawn p, WorkTypeDef w)
	{
		List<WorkTypeDef> disabledWorkTypes = p.GetDisabledWorkTypes();
		if (!disabledWorkTypes.Contains(w))
		{
			disabledWorkTypes.Add(w);
		}
	}
}
