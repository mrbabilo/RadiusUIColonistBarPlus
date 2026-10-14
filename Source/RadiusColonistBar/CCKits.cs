using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

public class CCKits(Game game) : GameComponent
{
	public List<CCKit> kits = new List<CCKit>();

	public Dictionary<int, int> kitOf = new Dictionary<int, int>();

	public static CCKits Get => Current.Game?.GetComponent<CCKits>();

	public override void ExposeData()
	{
		Scribe_Collections.Look(ref kits, "mcbKits", LookMode.Deep);
		Scribe_Collections.Look(ref kitOf, "mcbKitOf", LookMode.Value, LookMode.Value);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (kits == null)
			{
				kits = new List<CCKit>();
			}
			if (kitOf == null)
			{
				kitOf = new Dictionary<int, int>();
			}
		}
	}

	public int KitIndexOf(Pawn p)
	{
		int value;
		return (p != null && kitOf.TryGetValue(p.thingIDNumber, out value) && value >= 0 && value < kits.Count) ? value : (-1);
	}

	public void Assign(Pawn p, int kit)
	{
		if (p != null)
		{
			if (kit < 0)
			{
				kitOf.Remove(p.thingIDNumber);
			}
			else
			{
				kitOf[p.thingIDNumber] = kit;
			}
		}
	}

	public static int Missing(CCKit kit, Pawn p)
	{
		if (kit == null || p == null)
		{
			return 0;
		}
		int num = 0;
		try
		{
			if (!string.IsNullOrEmpty(kit.weapon) && p.equipment?.Primary?.def?.defName != kit.weapon)
			{
				num++;
			}
			List<Apparel> list = p.apparel?.WornApparel;
			for (int i = 0; i < kit.apparel.Count; i++)
			{
				bool flag = false;
				if (list != null)
				{
					for (int j = 0; j < list.Count; j++)
					{
						if (list[j]?.def?.defName == kit.apparel[i])
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					num++;
				}
			}
		}
		catch
		{
		}
		return num;
	}
}
