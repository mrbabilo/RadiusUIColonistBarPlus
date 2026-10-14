using HarmonyLib;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(CameraDriver), "Update")]
internal static class Patch_CameraDriver_Follow
{
	private static void Postfix(CameraDriver __instance)
	{
		try
		{
			FollowCam.CameraTick(__instance);
		}
		catch
		{
		}
	}
}
