using RimWorld;
using Verse;

namespace RadiusColonistBar;

internal static class HubRouter
{
	public static HubTarget ForKey(string key)
	{
		if (string.IsNullOrEmpty(key) || key == "__health__")
		{
			return HubTarget.Health;
		}
		return HubTarget.Needs;
	}

	public static void OpenTab(Pawn p, HubTarget target)
	{
		if (p == null || !p.Spawned || p.Map != Find.CurrentMap)
		{
			return;
		}
		try
		{
			CameraJumper.TryJumpAndSelect(p);
			InspectPaneUtility.OpenTab(target switch
			{
				HubTarget.Health => typeof(ITab_Pawn_Health), 
				HubTarget.Needs => typeof(ITab_Pawn_Needs), 
				HubTarget.Social => typeof(ITab_Pawn_Social), 
				HubTarget.Gear => typeof(ITab_Pawn_Gear), 
				_ => typeof(ITab_Pawn_Character), 
			});
		}
		catch
		{
		}
	}
}
