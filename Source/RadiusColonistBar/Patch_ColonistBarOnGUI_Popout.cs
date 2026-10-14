using HarmonyLib;
using RimWorld;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(ColonistBar), "ColonistBarOnGUI")]
internal static class Patch_ColonistBarOnGUI_Popout
{
	private static void Postfix()
	{
		try
		{
			FollowCam.OnGUI();
		}
		catch
		{
		}
		try
		{
			BadgeLayer.Flush();
		}
		catch
		{
		}
		try
		{
			SpeechBubbles.Flush();
		}
		catch
		{
		}
		try
		{
			HoverPopout.Draw();
		}
		catch
		{
		}
	}
}
