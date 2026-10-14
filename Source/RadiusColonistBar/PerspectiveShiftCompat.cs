using System;
using System.Reflection;
using Verse;

namespace RadiusColonistBar;

public static class PerspectiveShiftCompat
{
	private static bool init;

	private static Type stateType;

	private static Type avatarType;

	private static PropertyInfo isActiveP;

	private static FieldInfo avatarF;

	private static FieldInfo pawnF;

	public static bool Installed
	{
		get
		{
			Init();
			return stateType != null;
		}
	}

	private static void Init()
	{
		if (init)
		{
			return;
		}
		init = true;
		try
		{
			stateType = GenTypes.GetTypeInAnyAssembly("PerspectiveShift.State");
			avatarType = GenTypes.GetTypeInAnyAssembly("PerspectiveShift.Avatar");
			if (stateType != null)
			{
				isActiveP = stateType.GetProperty("IsActive", BindingFlags.Static | BindingFlags.Public);
				avatarF = stateType.GetField("Avatar", BindingFlags.Static | BindingFlags.Public);
			}
			if (avatarType != null)
			{
				pawnF = avatarType.GetField("pawn", BindingFlags.Instance | BindingFlags.Public);
			}
		}
		catch
		{
		}
	}

	public static Pawn PerspectivePawn()
	{
		Init();
		if (stateType == null || pawnF == null)
		{
			return null;
		}
		try
		{
			if (isActiveP != null && isActiveP.GetValue(null) is bool flag && !flag)
			{
				return null;
			}
			object obj = avatarF?.GetValue(null);
			if (obj == null)
			{
				return null;
			}
			return pawnF.GetValue(obj) as Pawn;
		}
		catch
		{
			return null;
		}
	}
}
