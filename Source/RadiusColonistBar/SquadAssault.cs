using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): the assault mode of each pawn sent on an assault. ThinkNode_ConditionalSquadAssault
// reads it. Only drafted, spawned, standing pawns are kept; a mode never comes back on its own once removed.
public class SquadAssault : GameComponent
{
	private const int CleanInterval = 250;

	private Dictionary<Pawn, AssaultMode> modes = new Dictionary<Pawn, AssaultMode>();

	private List<Pawn> tmpPawns;

	private List<AssaultMode> tmpModes;

	private static readonly List<Pawn> stale = new List<Pawn>();

	private static SquadAssault cachedComp;

	private static Game cachedGame;

	public static SquadAssault Get
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
				cachedComp = game.GetComponent<SquadAssault>();
			}
			return cachedComp;
		}
	}

	public SquadAssault(Game game)
	{
	}

	public void Set(Pawn pawn, AssaultMode mode)
	{
		if (pawn != null)
		{
			modes[pawn] = mode;
		}
	}

	public bool TryGetMode(Pawn pawn, out AssaultMode mode)
	{
		mode = AssaultMode.Strongest;
		if (pawn == null)
		{
			return false;
		}
		return modes.TryGetValue(pawn, out mode);
	}

	public void Clear(Pawn pawn)
	{
		if (pawn != null)
		{
			modes.Remove(pawn);
		}
	}

	public override void GameComponentTick()
	{
		if (modes.Count > 0 && Find.TickManager.TicksGame % CleanInterval == 0)
		{
			Clean();
		}
	}

	private void Clean()
	{
		stale.Clear();
		foreach (KeyValuePair<Pawn, AssaultMode> mode in modes)
		{
			Pawn key = mode.Key;
			if (key.Dead || key.Destroyed || !key.Spawned || key.Downed || key.drafter == null || !key.drafter.Drafted)
			{
				stale.Add(key);
			}
		}
		for (int i = 0; i < stale.Count; i++)
		{
			modes.Remove(stale[i]);
		}
		stale.Clear();
	}

	public override void ExposeData()
	{
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			Clean();
		}
		// logNullErrors: false — a pawn gone since the save is skipped quietly; Clean() handles the rest.
		Scribe_Collections.Look(ref modes, "squadAssault", LookMode.Reference, LookMode.Value, ref tmpPawns, ref tmpModes, logNullErrors: false);
		if (modes == null)
		{
			modes = new Dictionary<Pawn, AssaultMode>();
		}
	}
}
