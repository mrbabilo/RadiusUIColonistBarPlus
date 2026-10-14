using HarmonyLib;
using Verse;

namespace RadiusColonistBar;

[HarmonyPatch(typeof(DebugWindowsOpener), "DrawButtons")]
internal static class Patch_SuppressVanillaDevButtons
{
	private static bool Prefix()
	{
		if (!RcbDevToolbar.Owns)
		{
			return true;
		}
		RcbDevToolbar.HandleInputInWindow();
		RcbDevToolbar.MirrorHostWidth();
		return false;
	}
}
