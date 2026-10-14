using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

internal static class KitSlots
{
	public static readonly KitSlot[] All = new KitSlot[6]
	{
		KitSlot.Weapon,
		KitSlot.Head,
		KitSlot.Torso,
		KitSlot.Legs,
		KitSlot.Feet,
		KitSlot.Belt
	};

	public static string Label(KitSlot s)
	{
		return s.ToString();
	}

	public static KitSlot SlotOf(ThingDef def)
	{
		try
		{
			if (def.IsWeapon)
			{
				return KitSlot.Weapon;
			}
			ApparelProperties apparel = def.apparel;
			if (apparel == null)
			{
				return KitSlot.Torso;
			}
			ApparelLayerDef lastLayer = apparel.LastLayer;
			if (lastLayer == ApparelLayerDefOf.Belt)
			{
				return KitSlot.Belt;
			}
			if (lastLayer == ApparelLayerDefOf.Overhead || lastLayer == ApparelLayerDefOf.EyeCover)
			{
				return KitSlot.Head;
			}
			List<BodyPartGroupDef> bodyPartGroups = apparel.bodyPartGroups;
			if (bodyPartGroups != null)
			{
				for (int i = 0; i < bodyPartGroups.Count; i++)
				{
					string defName = bodyPartGroups[i].defName;
					if (defName.Contains("Head") || defName.Contains("Eyes"))
					{
						return KitSlot.Head;
					}
				}
				bool flag = false;
				bool flag2 = false;
				for (int j = 0; j < bodyPartGroups.Count; j++)
				{
					string defName2 = bodyPartGroups[j].defName;
					if (defName2.Contains("Feet") || defName2.Contains("Foot"))
					{
						flag = true;
					}
					if (defName2.Contains("Leg"))
					{
						flag2 = true;
					}
				}
				if (flag)
				{
					return KitSlot.Feet;
				}
				if (flag2)
				{
					return KitSlot.Legs;
				}
			}
			return KitSlot.Torso;
		}
		catch
		{
			return KitSlot.Torso;
		}
	}

	public static List<string> ItemsIn(CCKit kit, KitSlot slot)
	{
		List<string> list = new List<string>();
		if (slot == KitSlot.Weapon)
		{
			if (!string.IsNullOrEmpty(kit.weapon))
			{
				list.Add(kit.weapon);
			}
			return list;
		}
		for (int i = 0; i < kit.apparel.Count; i++)
		{
			ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(kit.apparel[i]);
			if (namedSilentFail != null && SlotOf(namedSilentFail) == slot)
			{
				list.Add(kit.apparel[i]);
			}
		}
		return list;
	}
}
