using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RadiusColonistBar;

public static class AdaptiveWorkBridge
{
	private static bool init;

	private static Func<Pawn, float, float, bool> fnOpenCard;

	private static Func<Pawn, int> fnPending;

	private static MethodInfo miOpenCard;

	private static MethodInfo miPending;

	private static readonly object[] arg3 = new object[3];

	private static readonly object[] arg1 = new object[1];

	public static bool Active
	{
		get
		{
			Ensure();
			return fnOpenCard != null || miOpenCard != null;
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
			Type type = AccessTools.TypeByName("AdaptiveWork.AdaptiveWorkAPI");
			if (type == null)
			{
				return;
			}
			miOpenCard = AccessTools.Method(type, "OpenPawnCard", new Type[3]
			{
				typeof(Pawn),
				typeof(float),
				typeof(float)
			}, (Type[])null);
			miPending = AccessTools.Method(type, "PendingCount", new Type[1] { typeof(Pawn) }, (Type[])null);
			if (miOpenCard != null)
			{
				try
				{
					fnOpenCard = AccessTools.MethodDelegate<Func<Pawn, float, float, bool>>(miOpenCard, (object)null, true, (Type[])null);
				}
				catch
				{
					fnOpenCard = null;
				}
			}
			if (!(miPending != null))
			{
				return;
			}
			try
			{
				fnPending = AccessTools.MethodDelegate<Func<Pawn, int>>(miPending, (object)null, true, (Type[])null);
			}
			catch
			{
				fnPending = null;
			}
		}
		catch
		{
			miOpenCard = null;
			miPending = null;
		}
	}

	public static bool OpenCard(Pawn p, float x, float y)
	{
		if (p == null)
		{
			return false;
		}
		Ensure();
		try
		{
			if (fnOpenCard != null)
			{
				return fnOpenCard(p, x, y);
			}
			if (miOpenCard == null)
			{
				return false;
			}
			arg3[0] = p;
			arg3[1] = x;
			arg3[2] = y;
			return (bool)miOpenCard.Invoke(null, arg3);
		}
		catch (Exception ex)
		{
			Log.WarningOnce("[Radius UI - Colonist Bar] Adaptive Work OpenPawnCard failed: " + ex.Message, 81231746);
			return false;
		}
	}

	public static int PendingCount(Pawn p)
	{
		if (p == null)
		{
			return 0;
		}
		Ensure();
		try
		{
			if (fnPending != null)
			{
				return fnPending(p);
			}
			if (miPending == null)
			{
				return 0;
			}
			arg1[0] = p;
			return (int)miPending.Invoke(null, arg1);
		}
		catch
		{
			return 0;
		}
	}
}
