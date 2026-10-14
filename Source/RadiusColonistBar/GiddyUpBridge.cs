using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RadiusColonistBar;

public static class GiddyUpBridge
{
	private static bool init;

	private static PropertyInfo singletonP;

	private static PropertyInfo storeP;

	private static PropertyInfo mountP;

	private static void Ensure()
	{
		if (init)
		{
			return;
		}
		init = true;
		try
		{
			Type type = AccessTools.TypeByName("GiddyUp.ExtendedDataStorage");
			Type type2 = AccessTools.TypeByName("GiddyUp.ExtendedPawnData");
			if (!(type == null) && !(type2 == null))
			{
				singletonP = AccessTools.Property(type, "Singleton");
				storeP = AccessTools.Property(type, "ExtendedPawnDataStore");
				mountP = AccessTools.Property(type2, "Mount");
			}
		}
		catch
		{
		}
	}

	public static bool IsMounted(Pawn p)
	{
		if (p == null)
		{
			return false;
		}
		Ensure();
		if (singletonP == null || storeP == null || mountP == null)
		{
			return false;
		}
		try
		{
			object value = singletonP.GetValue(null);
			if (value == null)
			{
				return false;
			}
			if (!(storeP.GetValue(value) is IDictionary dictionary))
			{
				return false;
			}
			object obj = dictionary[p.thingIDNumber];
			return obj != null && mountP.GetValue(obj) != null;
		}
		catch
		{
			return false;
		}
	}
}
