using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public static class MmbBridge
{
	private static bool init;

	private static MethodInfo tryGetM;

	private static FieldInfo mhF;

	public static bool Active
	{
		get
		{
			Ensure();
			return tryGetM != null && mhF != null;
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
			Type type = AccessTools.TypeByName("MMB.MMB_PawnMH");
			if (!(type == null))
			{
				tryGetM = AccessTools.Method(type, "TryGet", (Type[])null, (Type[])null);
				mhF = AccessTools.Field(type, "mh01");
			}
		}
		catch
		{
		}
	}

	public static bool TryGetMH(Pawn p, out float mh01)
	{
		mh01 = 0f;
		if (p == null || !Active)
		{
			return false;
		}
		try
		{
			object[] array = new object[2] { p, null };
			if (!(bool)tryGetM.Invoke(null, array) || array[1] == null)
			{
				return false;
			}
			mh01 = Mathf.Clamp01((float)mhF.GetValue(array[1]));
			return true;
		}
		catch
		{
			return false;
		}
	}
}
