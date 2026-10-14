using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RadiusColonistBar;

public static class AutoCombatBridge
{
	private static bool init;

	private static bool searchedBool;

	private static FieldInfo dictF;

	private static FieldInfo boolF;

	private static void Ensure()
	{
		if (init)
		{
			return;
		}
		init = true;
		try
		{
			Type type = AccessTools.TypeByName("BigAndSmall.DraftedActionHolder");
			if (type != null)
			{
				dictF = AccessTools.Field(type, "pawnDraftActionData");
			}
		}
		catch
		{
		}
	}

	public static bool IsAutoCombat(Pawn p)
	{
		if (p == null)
		{
			return false;
		}
		Ensure();
		if (dictF == null)
		{
			return false;
		}
		try
		{
			if (!(dictF.GetValue(null) is IDictionary dictionary))
			{
				return false;
			}
			object obj = dictionary[p.ThingID];
			if (obj == null)
			{
				return false;
			}
			if (!searchedBool)
			{
				searchedBool = true;
				FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
				foreach (FieldInfo fieldInfo in fields)
				{
					if (fieldInfo.FieldType == typeof(bool) && fieldInfo.Name.IndexOf("auto", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						boolF = fieldInfo;
						break;
					}
				}
			}
			return boolF != null && (bool)boolF.GetValue(obj);
		}
		catch
		{
			return false;
		}
	}
}
