using System.Collections.Generic;
using System.Linq;
using RadiusUI.Framework;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public class BarSquads : GameComponent
{
	public List<string> names = new List<string>();

	public List<Color> colors = new List<Color>();

	public List<int> icons = new List<int>();

	public Dictionary<int, int> squadOf = new Dictionary<int, int>();

	public HashSet<int> hidden = new HashSet<int>();

	public bool revealSlaves;

	public bool revealPrisoners;

	public HashSet<int> ccPinned = new HashSet<int>();

	public int cohortTab;

	private List<int> hiddenScratch;

	private List<int> pinnedScratch;

	private static BarSquads cachedComp;

	private static Game cachedGame;

	private const int SquadGlyphN = 8;

	private static Color SquadFill => Palette.Flat.Faction;

	public static BarSquads Get
	{
		get
		{
			Game game = Current.Game;
			if (game == null)
			{
				cachedGame = null;
				cachedComp = null;
				return null;
			}
			if (game != cachedGame)
			{
				cachedGame = game;
				cachedComp = game.GetComponent<BarSquads>();
			}
			return cachedComp;
		}
	}

	public BarSquads(Game game)
	{
	}

	public override void ExposeData()
	{
		Scribe_Collections.Look(ref names, "mcbSquadNames", LookMode.Value);
		Scribe_Collections.Look(ref colors, "mcbSquadColors", LookMode.Value);
		Scribe_Collections.Look(ref icons, "mcbSquadIcons", LookMode.Value);
		Scribe_Collections.Look(ref squadOf, "mcbSquadOf", LookMode.Value, LookMode.Value);
		Scribe_Values.Look(ref revealSlaves, "mcbRevealSlaves", defaultValue: false);
		Scribe_Values.Look(ref revealPrisoners, "mcbRevealPrisoners", defaultValue: false);
		Scribe_Values.Look(ref cohortTab, "mcbCohortTab", 0);
		if (cohortTab < 0 || cohortTab > 5)
		{
			cohortTab = 0;
		}
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			pinnedScratch = new List<int>(ccPinned);
		}
		Scribe_Collections.Look(ref pinnedScratch, "mcbCcPinned", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			ccPinned = ((pinnedScratch != null) ? new HashSet<int>(pinnedScratch) : new HashSet<int>());
		}
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			hiddenScratch = hidden.ToList();
		}
		Scribe_Collections.Look(ref hiddenScratch, "mcbHidden", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			hidden = ((hiddenScratch != null) ? new HashSet<int>(hiddenScratch) : new HashSet<int>());
		}
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (names == null)
			{
				names = new List<string>();
			}
			if (colors == null)
			{
				colors = new List<Color>();
			}
			if (icons == null)
			{
				icons = new List<int>();
			}
			if (squadOf == null)
			{
				squadOf = new Dictionary<int, int>();
			}
			if (hidden == null)
			{
				hidden = new HashSet<int>();
			}
			if (ccPinned == null)
			{
				ccPinned = new HashSet<int>();
			}
			while (colors.Count < names.Count)
			{
				colors.Add(SquadFill);
			}
			while (icons.Count < names.Count)
			{
				icons.Add(icons.Count % 8);
			}
		}
	}

	public int SquadIndexOf(Pawn p)
	{
		int value;
		return (p != null && squadOf.TryGetValue(p.thingIDNumber, out value) && value >= 0 && value < names.Count) ? value : (-1);
	}

	public string SquadName(int i)
	{
		return (i >= 0 && i < names.Count) ? names[i] : null;
	}

	public Color SquadColor(int i)
	{
		return SquadFill;
	}

	public void SetColor(int i, Color c)
	{
		if (i >= 0 && i < colors.Count)
		{
			colors[i] = c;
			Dirty();
		}
	}

	public int SquadIcon(int i)
	{
		return (i >= 0 && i < icons.Count) ? icons[i] : (-1);
	}

	public void CycleIcon(int i)
	{
		if (i >= 0 && i < icons.Count)
		{
			int num = icons[i];
			icons[i] = ((num >= 7) ? (-1) : (num + 1));
			Dirty();
		}
	}

	public void SetIcon(int i, int glyph)
	{
		if (i >= 0 && i < icons.Count)
		{
			icons[i] = glyph;
			Dirty();
		}
	}

	public void Assign(Pawn p, int i)
	{
		if (p != null)
		{
			squadOf[p.thingIDNumber] = i;
			Dirty();
		}
	}

	public void Remove(Pawn p)
	{
		if (p != null)
		{
			squadOf.Remove(p.thingIDNumber);
			Dirty();
		}
	}

	public void Rename(int i, string s)
	{
		if (i >= 0 && i < names.Count && !s.NullOrEmpty())
		{
			names[i] = s;
			Dirty();
		}
	}

	public int NewSquad()
	{
		names.Add("Squad " + (names.Count + 1));
		colors.Add(SquadFill);
		icons.Add((names.Count - 1) % 8);
		Dirty();
		return names.Count - 1;
	}

	public void Disband(int i)
	{
		if (i < 0 || i >= names.Count)
		{
			return;
		}
		names.RemoveAt(i);
		if (i < colors.Count)
		{
			colors.RemoveAt(i);
		}
		if (i < icons.Count)
		{
			icons.RemoveAt(i);
		}
		List<int> list = new List<int>(squadOf.Keys);
		for (int j = 0; j < list.Count; j++)
		{
			int num = squadOf[list[j]];
			if (num == i)
			{
				squadOf.Remove(list[j]);
			}
			else if (num > i)
			{
				squadOf[list[j]] = num - 1;
			}
		}
		if (cohortTab == 100 + i)
		{
			cohortTab = 0;
		}
		else if (cohortTab > 100 + i)
		{
			cohortTab--;
		}
		Dirty();
	}

	public void MoveSquad(int i, int dir)
	{
		int num = i + dir;
		if (i < 0 || num < 0 || i >= names.Count || num >= names.Count)
		{
			return;
		}
		List<string> list = names;
		int index = i;
		List<string> list2 = names;
		int index2 = num;
		string value = names[num];
		string value2 = names[i];
		list[index] = value;
		list2[index2] = value2;
		if (i < colors.Count && num < colors.Count)
		{
			List<Color> list3 = colors;
			index2 = i;
			List<Color> list4 = colors;
			index = num;
			Color value3 = colors[num];
			Color value4 = colors[i];
			list3[index2] = value3;
			list4[index] = value4;
		}
		if (i < icons.Count && num < icons.Count)
		{
			List<int> list5 = icons;
			index = i;
			List<int> list6 = icons;
			index2 = num;
			int value5 = icons[num];
			int value6 = icons[i];
			list5[index] = value5;
			list6[index2] = value6;
		}
		List<int> list7 = new List<int>(squadOf.Keys);
		for (int j = 0; j < list7.Count; j++)
		{
			int num2 = squadOf[list7[j]];
			if (num2 == i)
			{
				squadOf[list7[j]] = num;
			}
			else if (num2 == num)
			{
				squadOf[list7[j]] = i;
			}
		}
		if (cohortTab == 100 + i)
		{
			cohortTab = 100 + num;
		}
		else if (cohortTab == 100 + num)
		{
			cohortTab = 100 + i;
		}
		Dirty();
	}

	public bool IsHidden(Pawn p)
	{
		return p != null && hidden.Contains(p.thingIDNumber);
	}

	public void Hide(Pawn p)
	{
		if (p != null)
		{
			hidden.Add(p.thingIDNumber);
			Dirty();
		}
	}

	public void Unhide(int id)
	{
		hidden.Remove(id);
		Dirty();
	}

	public void UnhideAll()
	{
		hidden.Clear();
		Dirty();
	}

	public string NameOfHidden(int id)
	{
		try
		{
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				List<Pawn> allPawns = maps[i].mapPawns.AllPawns;
				for (int j = 0; j < allPawns.Count; j++)
				{
					if (allPawns[j].thingIDNumber == id)
					{
						return allPawns[j].LabelShortCap;
					}
				}
			}
			List<Caravan> list = Find.WorldObjects?.Caravans;
			if (list != null)
			{
				for (int k = 0; k < list.Count; k++)
				{
					List<Pawn> pawnsListForReading = list[k].PawnsListForReading;
					for (int l = 0; l < pawnsListForReading.Count; l++)
					{
						if (pawnsListForReading[l].thingIDNumber == id)
						{
							return pawnsListForReading[l].LabelShortCap;
						}
					}
				}
			}
		}
		catch
		{
		}
		return "Unknown pawn";
	}

	private static void Dirty()
	{
		// Squad orders (Colonist Bar Plus): indices and members changed, the cached squad state is stale.
		SquadStatus.Invalidate();
		if (Patch_Recache_SquadsAndHidden.Rebuilding)
		{
			Patch_Recache_SquadsAndHidden.PendingRebuild = true;
			return;
		}
		try
		{
			Find.ColonistBar?.MarkColonistsDirty();
		}
		catch
		{
		}
	}
}
