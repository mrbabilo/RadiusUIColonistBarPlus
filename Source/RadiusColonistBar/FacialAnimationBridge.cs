using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public static class FacialAnimationBridge
{
	private struct Memo
	{
		public int frame;

		public bool val;
	}

	private static bool init;

	private static bool present;

	private static Func<Pawn, bool> fnShouldDrawPawn;

	private static MethodInfo miShouldDrawPawn;

	private static Type ctrlCompType;

	private static MethodInfo miCheckUpdateable;

	private static readonly object[] arg1 = new object[1];

	private static Func<object> fnFaSettings;

	private static Func<object, object> rdEnablePortrait;

	private static Func<object, object> rdPortraitInterval;

	private static float nextFaSettingsAt = -1f;

	private const float FaSettingsRefreshSeconds = 2f;

	private static bool faDrivesPortraits;

	private static int faOwnInterval = 1;

	private static readonly Dictionary<int, object> compCache = new Dictionary<int, object>();

	private static readonly Dictionary<int, Memo> memo = new Dictionary<int, Memo>();

	public static bool FaDrivesPortraits
	{
		get
		{
			Ensure();
			if (!present)
			{
				return false;
			}
			EnsureFaSettings();
			return faDrivesPortraits;
		}
	}

	public static int FaOwnInterval
	{
		get
		{
			Ensure();
			if (present)
			{
				EnsureFaSettings();
			}
			return faOwnInterval;
		}
	}

	public static bool Active
	{
		get
		{
			Ensure();
			return present;
		}
	}

	private static Func<object, object> MemberReader(Type t, string name)
	{
		if (t == null || string.IsNullOrEmpty(name))
		{
			return null;
		}
		try
		{
			PropertyInfo pi = AccessTools.Property(t, name);
			if (pi != null && pi.CanRead)
			{
				return (object inst) => pi.GetValue(inst, null);
			}
			FieldInfo fi = AccessTools.Field(t, name);
			if (fi != null)
			{
				return (object inst) => fi.GetValue(inst);
			}
		}
		catch
		{
		}
		return null;
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
			Type type = AccessTools.TypeByName("FacialAnimation.FAHelper");
			if (type != null)
			{
				miShouldDrawPawn = AccessTools.Method(type, "ShouldDrawPawn", new Type[1] { typeof(Pawn) }, (Type[])null);
				if (miShouldDrawPawn != null)
				{
					try
					{
						fnShouldDrawPawn = AccessTools.MethodDelegate<Func<Pawn, bool>>(miShouldDrawPawn, (object)null, true, (Type[])null);
					}
					catch
					{
						fnShouldDrawPawn = null;
					}
				}
			}
			ctrlCompType = AccessTools.TypeByName("FacialAnimation.FacialAnimationControllerComp");
			if (ctrlCompType != null)
			{
				miCheckUpdateable = AccessTools.Method(ctrlCompType, "CheckUpdateableInitial", (Type[])null, (Type[])null);
			}
			Type type2 = AccessTools.TypeByName("FacialAnimation.FacialAnimationMod");
			if (type2 != null)
			{
				PropertyInfo pi = AccessTools.Property(type2, "Settings");
				if (pi != null && pi.CanRead)
				{
					fnFaSettings = () => pi.GetValue(null, null);
				}
				else
				{
					FieldInfo fi = AccessTools.Field(type2, "Settings");
					if (fi != null)
					{
						fnFaSettings = () => fi.GetValue(null);
					}
				}
			}
			Type t = AccessTools.TypeByName("FacialAnimation.FacialAnimationModSettings");
			rdEnablePortrait = MemberReader(t, "EnablePortraitAnimationInPortrait");
			rdPortraitInterval = MemberReader(t, "UpdateAnimationInPortraitInterval");
			present = fnShouldDrawPawn != null || miShouldDrawPawn != null;
		}
		catch
		{
			present = false;
		}
	}

	private static void EnsureFaSettings()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (nextFaSettingsAt >= 0f && realtimeSinceStartup < nextFaSettingsAt)
		{
			return;
		}
		nextFaSettingsAt = realtimeSinceStartup + 2f;
		try
		{
			object obj = fnFaSettings?.Invoke();
			if (obj == null)
			{
				faDrivesPortraits = false;
				return;
			}
			object obj2 = rdEnablePortrait?.Invoke(obj);
			bool flag = default(bool);
			int num;
			if (obj2 is bool)
			{
				flag = (bool)obj2;
				num = 1;
			}
			else
			{
				num = 0;
			}
			faDrivesPortraits = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
			if (rdPortraitInterval?.Invoke(obj) is int num2 && num2 > 0)
			{
				faOwnInterval = num2;
			}
		}
		catch
		{
			faDrivesPortraits = false;
		}
	}

	public static bool IsFaceAnimating(Pawn pawn)
	{
		if (pawn == null)
		{
			return false;
		}
		Ensure();
		if (!present)
		{
			return false;
		}
		int thingIDNumber = pawn.thingIDNumber;
		int frameCount = Time.frameCount;
		if (memo.TryGetValue(thingIDNumber, out var value) && value.frame == frameCount)
		{
			return value.val;
		}
		bool flag = Evaluate(pawn);
		if (memo.Count > 512)
		{
			memo.Clear();
		}
		memo[thingIDNumber] = new Memo
		{
			frame = frameCount,
			val = flag
		};
		return flag;
	}

	private static bool Evaluate(Pawn pawn)
	{
		try
		{
			bool flag;
			if (fnShouldDrawPawn != null)
			{
				flag = fnShouldDrawPawn(pawn);
			}
			else
			{
				arg1[0] = pawn;
				flag = (bool)miShouldDrawPawn.Invoke(null, arg1);
			}
			if (!flag)
			{
				return false;
			}
			if (ctrlCompType != null && miCheckUpdateable != null)
			{
				object obj = FindComp(pawn);
				if (obj != null && !(bool)miCheckUpdateable.Invoke(obj, null))
				{
					return false;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static object FindComp(Pawn p)
	{
		int thingIDNumber = p.thingIDNumber;
		if (compCache.TryGetValue(thingIDNumber, out var value))
		{
			return value;
		}
		object obj = null;
		List<ThingComp> allComps = p.AllComps;
		for (int i = 0; i < allComps.Count; i++)
		{
			if (ctrlCompType.IsInstanceOfType(allComps[i]))
			{
				obj = allComps[i];
				break;
			}
		}
		if (compCache.Count > 512)
		{
			compCache.Clear();
		}
		compCache[thingIDNumber] = obj;
		return obj;
	}
}
