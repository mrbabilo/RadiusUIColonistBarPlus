using System;
using System.Reflection;
using HarmonyLib;

namespace RadiusColonistBar;

[HarmonyPatch]
internal static class Patch_SuppressModernUIBarBackground
{
	private const string TypeName = "ModernUI.ColonistBar_Background_Patch";

	private static bool Prepare()
	{
		return AccessTools.TypeByName("ModernUI.ColonistBar_Background_Patch") != null;
	}

	private static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("ModernUI.ColonistBar_Background_Patch");
		return (type != null) ? AccessTools.Method(type, "Prefix", (Type[])null, (Type[])null) : null;
	}

	private static bool Prefix()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		return settings == null || !settings.masterEnabled;
	}
}
