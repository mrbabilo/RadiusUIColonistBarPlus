using System;
using System.Collections.Generic;
using RadiusUI.Framework;
using Verse;

namespace RadiusColonistBar;

public static class RaceFramingTable
{
	private static DefTable<ThingDef, RaceFraming> table;

	private static bool built;

	public static void Invalidate()
	{
		built = false;
	}

	private static void Build()
	{
		built = true;
		table = new DefTable<ThingDef, RaceFraming>();
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings?.raceFraming == null)
		{
			return;
		}
		for (int i = 0; i < settings.raceFraming.Count; i++)
		{
			RaceFraming raceFraming = settings.raceFraming[i];
			if (raceFraming != null && !string.IsNullOrEmpty(raceFraming.race))
			{
				ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(raceFraming.race);
				if (namedSilentFail != null)
				{
					table.Set(namedSilentFail, raceFraming);
				}
			}
		}
	}

	public static RaceFraming For(Pawn p)
	{
		if (p == null)
		{
			return null;
		}
		if (!built)
		{
			Build();
		}
		RaceFraming value;
		return (table != null && table.TryGet(p.def, out value)) ? value : null;
	}

	public static List<ThingDef> PickableRaces()
	{
		List<ThingDef> list = new List<ThingDef>();
		List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			ThingDef thingDef = allDefsListForReading[i];
			if (thingDef?.race != null && (thingDef.race.Humanlike || thingDef.race.Animal || thingDef.race.IsMechanoid))
			{
				list.Add(thingDef);
			}
		}
		list.Sort((ThingDef a, ThingDef b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase));
		return list;
	}
}
