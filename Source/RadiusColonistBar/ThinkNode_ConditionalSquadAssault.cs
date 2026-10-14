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
		return squadAssault.TryGetMode(pawn, out _);
	}
}
