using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RadiusColonistBar;

public static class BpcBridge
{
	private static bool init;

	private static MethodInfo onM;

	private static MethodInfo offM;

	private static PropertyInfo onAlertP;

	public static bool Active
	{
		get
		{
			Ensure();
			return onM != null && offM != null && onAlertP != null;
		}
	}

	public static bool OnAlert
	{
		get
		{
			try
			{
				return Active && (bool)onAlertP.GetValue(null);
			}
			catch
			{
				return false;
			}
		}
	}

	private static void Ensure()
	{
		if (init)
		{
			return;
		}
		init = true;
		try
		{
			Type type = AccessTools.TypeByName("BetterPawnControl.Patches.PlaySettings_DoPlaySettingsGlobalControls");
			Type type2 = AccessTools.TypeByName("BetterPawnControl.AlertManager");
			if (!(type == null) && !(type2 == null))
			{
				onM = AccessTools.Method(type, "EmergencyToogleON", (Type[])null, (Type[])null);
				offM = AccessTools.Method(type, "EmergencyToogleOFF", (Type[])null, (Type[])null);
				onAlertP = AccessTools.Property(type2, "OnAlert");
			}
		}
		catch
		{
		}
	}

	public static void Toggle()
	{
		if (!Active)
		{
			return;
		}
		try
		{
			(OnAlert ? offM : onM).Invoke(null, new object[1]);
		}
		catch (Exception ex)
		{
			Log.WarningOnce("[Radius UI - Colonist Bar] BPC emergency toggle failed: " + ex.Message, 81231745);
		}
	}
}
