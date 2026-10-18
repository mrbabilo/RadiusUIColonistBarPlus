using Verse;
using Verse.AI;

namespace RadiusColonistBar;

// Squad assault (Colonist Bar Plus): lets JobGiver_SquadAssault run for drafted pawns sent on an assault.
public class ThinkNode_ConditionalSquadAssault : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		SquadAssault squadAssault = SquadAssault.Get;
		if (squadAssault == null || !pawn.Drafted)
		{
			return false;
		}
		// Vanilla keeps the incapable of violence out of the fight giver by their hostility response, which the
		// assault giver overrides: they stay out of the branch and wait drafted, as in vanilla.
		if (pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return false;
		}
		return squadAssault.TryGetMode(pawn, out _);
	}
}
