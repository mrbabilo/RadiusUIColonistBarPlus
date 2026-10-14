using System;
using System.Reflection;
using HarmonyLib;
using RadiusUI.Framework;
using Verse;

namespace RadiusColonistBar;

[StaticConstructorOnStartup]
internal static class HarmonyBootstrap
{
	internal const string HarmonyId = "astryl.radiusui.colonistbar";

	static HarmonyBootstrap()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		FrameworkVersion.Require("Radius UI - Colonist Bar", 32);
		Harmony val = new Harmony("astryl.radiusui.colonistbar");
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in types)
		{
			try
			{
				val.CreateClassProcessor(type).Patch();
			}
			catch (Exception ex)
			{
				Log.Warning("[Radius UI - Colonist Bar] skipped patch class " + type.Name + ": " + ex.Message);
			}
		}
		Log.Message("[Radius UI - Colonist Bar] initialized.");
	}
}
