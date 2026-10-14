using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class FollowCam
{
	private static Pawn target;

	private static Vector3 lastApplied = Vector3.negativeInfinity;

	private static Vector2 camVel = Vector2.zero;

	private const float DeadZoneTiles = 0.9f;

	private static KeyBindingDef keyDef;

	private static readonly AccessTools.FieldRef<CameraDriver, Vector3> rootPosRef = AccessTools.FieldRefAccess<CameraDriver, Vector3>("rootPos");

	private static readonly MethodInfo applyPos = AccessTools.Method(typeof(CameraDriver), "ApplyPositionToGameObject", (Type[])null, (Type[])null);

	public static bool Active => target != null;

	private static KeyBindingDef KeyDef => keyDef ?? (keyDef = DefDatabase<KeyBindingDef>.GetNamedSilentFail("RCB_FollowCam"));

	public static bool Following(Pawn p)
	{
		return p != null && target == p;
	}

	public static void OnGUI()
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled || !settings.followCam)
		{
			target = null;
			return;
		}
		KeyBindingDef keyBindingDef = KeyDef;
		if (keyBindingDef == null || !keyBindingDef.KeyDownEvent)
		{
			return;
		}
		Event.current.Use();
		if (Active)
		{
			Stop(announce: true);
			return;
		}
		Pawn pawn = FirstSelectedPawn();
		if (pawn != null)
		{
			Toggle(pawn);
		}
		else
		{
			Messages.Message("Select a pawn to follow.", MessageTypeDefOf.RejectInput, historical: false);
		}
	}

	public static void Toggle(Pawn p)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.followCam || p == null)
		{
			return;
		}
		if (target == p)
		{
			Stop(announce: true);
			return;
		}
		target = p;
		lastApplied = Vector3.negativeInfinity;
		camVel = Vector2.zero;
		try
		{
			if (p.MapHeld != Find.CurrentMap)
			{
				CameraJumper.TryJump(p);
			}
		}
		catch
		{
		}
		string text = "the follow key";
		try
		{
			text = KeyDef?.MainKeyLabel ?? text;
		}
		catch
		{
		}
		Messages.Message("Following " + p.LabelShortCap + " - press " + text + " or pan the camera to stop.", MessageTypeDefOf.SilentInput, historical: false);
	}

	public static void Stop(bool announce)
	{
		if (target != null)
		{
			target = null;
			camVel = Vector2.zero;
			if (announce)
			{
				Messages.Message("Stopped following.", MessageTypeDefOf.SilentInput, historical: false);
			}
		}
	}

	private static Pawn FirstSelectedPawn()
	{
		try
		{
			List<Pawn> selectedPawns = Find.Selector.SelectedPawns;
			if (selectedPawns != null && selectedPawns.Count > 0)
			{
				return selectedPawns[0];
			}
		}
		catch
		{
		}
		return null;
	}

	public static void CameraTick(CameraDriver driver)
	{
		if (target == null)
		{
			return;
		}
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled || !settings.followCam)
		{
			target = null;
			return;
		}
		Map currentMap = Find.CurrentMap;
		if (currentMap == null)
		{
			return;
		}
		Pawn pawn = target;
		if (pawn.Destroyed)
		{
			Stop(announce: false);
			return;
		}
		Thing thing = null;
		try
		{
			thing = pawn.SpawnedParentOrMe;
		}
		catch
		{
		}
		if (thing == null || !thing.Spawned || thing.Map != currentMap)
		{
			Stop(announce: false);
			return;
		}
		ref Vector3 reference = ref rootPosRef.Invoke(driver);
		if (!float.IsNegativeInfinity(lastApplied.x))
		{
			float num = reference.x - lastApplied.x;
			float num2 = reference.z - lastApplied.z;
			if (num * num + num2 * num2 > 0.0004f)
			{
				Stop(announce: false);
				return;
			}
		}
		Vector3 drawPos = thing.DrawPos;
		drawPos.x = Mathf.Clamp(drawPos.x, 2f, (float)currentMap.Size.x - 2f);
		drawPos.z = Mathf.Clamp(drawPos.z, 2f, (float)currentMap.Size.z - 2f);
		float num3 = Mathf.Min(Time.deltaTime, 0.1f);
		Vector3 vector = reference;
		if (settings.followCamStabilize)
		{
			float num4 = drawPos.x - vector.x;
			float num5 = drawPos.z - vector.z;
			float num6 = Mathf.Sqrt(num4 * num4 + num5 * num5);
			Vector3 vector2 = vector;
			if (num6 > 0.9f)
			{
				float num7 = (num6 - 0.9f) / num6;
				vector2 = new Vector3(vector.x + num4 * num7, 0f, vector.z + num5 * num7);
			}
			float smoothTime = Mathf.Clamp(settings.followCamSmooth, 0.02f, 0.6f) * 2f;
			float currentVelocity = camVel.x;
			float currentVelocity2 = camVel.y;
			vector.x = Mathf.SmoothDamp(vector.x, vector2.x, ref currentVelocity, smoothTime, float.PositiveInfinity, num3);
			vector.z = Mathf.SmoothDamp(vector.z, vector2.z, ref currentVelocity2, smoothTime, float.PositiveInfinity, num3);
			camVel = new Vector2(currentVelocity, currentVelocity2);
			if (num6 <= 0.9f && camVel.sqrMagnitude < 0.0001f)
			{
				camVel = Vector2.zero;
				return;
			}
		}
		else
		{
			float num8 = Mathf.Clamp(settings.followCamSmooth, 0.02f, 0.6f);
			float t = 1f - Mathf.Exp(-0.6931472f * num3 / num8);
			vector.x = Mathf.Lerp(vector.x, drawPos.x, t);
			vector.z = Mathf.Lerp(vector.z, drawPos.z, t);
		}
		reference = vector;
		lastApplied = vector;
		try
		{
			applyPos?.Invoke(driver, null);
		}
		catch
		{
		}
	}
}
