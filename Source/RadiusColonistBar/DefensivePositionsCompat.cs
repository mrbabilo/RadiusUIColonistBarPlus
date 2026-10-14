using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public static class DefensivePositionsCompat
{
	private static bool inited;

	private static bool active;

	private static Type mapCompType;

	private static Type squadType;

	private static MethodInfo getHandler;

	private static MethodInfo onBasicAction;

	private static PropertyInfo squadsProp;

	private static PropertyInfo squadIdProp;

	private static FieldInfo membersField;

	private static readonly Dictionary<int, int> squadCache = new Dictionary<int, int>();

	private static int squadCacheFrame = -9999;

	public static bool Active
	{
		get
		{
			Init();
			return active;
		}
	}

	private static void Init()
	{
		if (inited)
		{
			return;
		}
		inited = true;
		try
		{
			mapCompType = AccessTools.TypeByName("DefensivePositions.DefensivePositionsMapComponent");
			if (!(mapCompType == null))
			{
				Type type = AccessTools.TypeByName("DefensivePositions.IDefensivePositionGizmoHandler");
				squadType = AccessTools.TypeByName("DefensivePositions.PawnSquad");
				getHandler = AccessTools.Method(mapCompType, "GetOrAddPawnHandler", (Type[])null, (Type[])null);
				onBasicAction = ((type != null) ? AccessTools.Method(type, "OnBasicGizmoAction", (Type[])null, (Type[])null) : null);
				squadsProp = AccessTools.Property(mapCompType, "PawnSquads");
				if (squadType != null)
				{
					squadIdProp = AccessTools.Property(squadType, "SquadId");
					membersField = AccessTools.Field(squadType, "members");
				}
				active = getHandler != null && onBasicAction != null;
			}
		}
		catch
		{
			active = false;
		}
	}

	private static object MapComp(Map map)
	{
		if (map?.components == null)
		{
			return null;
		}
		for (int i = 0; i < map.components.Count; i++)
		{
			if (mapCompType.IsInstanceOfType(map.components[i]))
			{
				return map.components[i];
			}
		}
		return null;
	}

	public static void SendOne(Pawn p)
	{
		Init();
		if (!active || p == null || p.Map == null)
		{
			return;
		}
		try
		{
			object obj = MapComp(p.Map);
			if (obj != null)
			{
				object obj2 = getHandler.Invoke(obj, new object[1] { p });
				if (obj2 != null)
				{
					onBasicAction.Invoke(obj2, null);
				}
			}
		}
		catch
		{
		}
	}

	public static int SquadNumber(Pawn p)
	{
		Init();
		if (!active || squadsProp == null || squadIdProp == null || membersField == null || p?.Map == null)
		{
			return -1;
		}
		if (Time.frameCount - squadCacheFrame > 15)
		{
			squadCache.Clear();
			squadCacheFrame = Time.frameCount;
		}
		int thingIDNumber = p.thingIDNumber;
		if (squadCache.TryGetValue(thingIDNumber, out var value))
		{
			return value;
		}
		int num = Compute(p);
		squadCache[thingIDNumber] = num;
		return num;
	}

	private static int Compute(Pawn p)
	{
		try
		{
			object obj = MapComp(p.Map);
			if (obj == null)
			{
				return -1;
			}
			if (!(squadsProp.GetValue(obj) is IList list))
			{
				return -1;
			}
			int num = -1;
			foreach (object item in list)
			{
				if (item == null || !(membersField.GetValue(item) is IList list2))
				{
					continue;
				}
				bool flag = false;
				foreach (object item2 in list2)
				{
					if (item2 == p)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					int num2 = (int)squadIdProp.GetValue(item);
					if (num < 0 || num2 < num)
					{
						num = num2;
					}
				}
			}
			return num;
		}
		catch
		{
			return -1;
		}
	}
}
