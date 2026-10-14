using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public static class LiveViewManager
{
	private static readonly Dictionary<Pawn, RenderTexture> rts = new Dictionary<Pawn, RenderTexture>();

	private static readonly List<Pawn> eligible = new List<Pawn>();

	private static readonly List<Pawn> shown = new List<Pawn>();

	private static readonly List<Pawn> toRelease = new List<Pawn>();

	private static Camera liveCam;

	private static int rrIndex;

	private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.3f);

	private const float PortraitZoom = 1.28205f;

	private const float SizeQuantum = 16f;

	private static int Quality => RadiusColonistBarMod.Settings?.liveResolution ?? 192;

	public static float PortraitK => Mathf.Clamp((float)Quality / 80f, 1.6f, 4f);

	private static float MaxPortraitPx => (Quality >= 320) ? 1024f : 512f;

	private static bool VanillaPortraitDiscarded(Pawn pawn)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		return settings != null && settings.masterEnabled && settings.liveView && pawn != null;
	}

	public static RenderTexture BarPortrait(Pawn pawn, Vector2 size, Rot4 rotation, Vector3 cameraOffset, float cameraZoom, bool supersample, bool compensateForUIScale, bool renderHeadgear, bool renderClothes, IReadOnlyDictionary<Apparel, Color> overrideApparelColors, Color? overrideHairColor, bool stylingStation, PawnHealthState? healthStateOverride)
	{
		if (VanillaPortraitDiscarded(pawn))
		{
			return null;
		}
		try
		{
			return PortraitsCache.Get(pawn, size, rotation, cameraOffset, cameraZoom, supersample, compensateForUIScale, renderHeadgear, renderClothes, overrideApparelColors, overrideHairColor, stylingStation, healthStateOverride);
		}
		catch
		{
			return null;
		}
	}

	public static Texture TryLive(Pawn pawn)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled || !settings.useLiveCameras || pawn == null)
		{
			return null;
		}
		RenderTexture value;
		if (pawn.Dead)
		{
			return (rts.TryGetValue(pawn, out value) && value != null && value.IsCreated()) ? value : null;
		}
		if (!pawn.Spawned || pawn.Map == null || pawn.Map != Find.CurrentMap)
		{
			return null;
		}
		if (pawn.Downed)
		{
			return null;
		}
		if (!settings.liveWholeMap && !IsInView(pawn))
		{
			return null;
		}
		RenderTexture value2;
		return (rts.TryGetValue(pawn, out value2) && value2 != null && value2.IsCreated()) ? value2 : null;
	}

	public static Texture MaybeReplaceWithLiveView(Texture vanilla, Pawn pawn)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled || !settings.liveView || pawn == null)
		{
			return vanilla ?? BaseContent.ClearTex;
		}
		return BaseContent.ClearTex;
	}

	public static Texture2D FilterCellTex(Texture2D tex, Pawn p)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings != null && settings.masterEnabled && settings.liveView)
		{
			return BaseContent.ClearTex;
		}
		return tex;
	}

	public static Texture2D FilterBgTex(Texture2D tex, Pawn p)
	{
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings == null || !settings.masterEnabled)
		{
			return tex;
		}
		if ((!settings.liveView || settings.useLiveCameras) && !settings.partyPanel && !settings.warbandMode && CardPalette.TryMoodBacking(p, settings, out var col))
		{
			return CardPalette.MoodTex(col);
		}
		if (settings.liveView)
		{
			return BaseContent.ClearTex;
		}
		return tex;
	}

	public static Vector2 QuantizePortraitSize(Vector2 px)
	{
		if (px.y < 1f || px.x < 1f)
		{
			return px;
		}
		float num = px.x / px.y;
		float num2 = Mathf.Ceil(px.y / 16f) * 16f;
		return new Vector2(num2 * num, num2);
	}

	public static Vector2 UpRes(Vector2 px)
	{
		float num = ((Quality >= 256) ? 2f : 1f);
		return new Vector2(px.x * num, px.y * num);
	}

	public static Texture GetPortraitTexture(Pawn p, Vector2 pxSize)
	{
		return GetPortraitTexture(p, pxSize, Rot4.South, 1f);
	}

	public static Texture GetPortraitTexture(Pawn p, Vector2 pxSize, Rot4 rot, float zoomMul)
	{
		if (p == null)
		{
			return null;
		}
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		Vector2 vector = QuantizePortraitSize(pxSize);
		float x = Mathf.Clamp(vector.x, 32f, MaxPortraitPx);
		float y = Mathf.Clamp(vector.y, 32f, MaxPortraitPx);
		Vector3 vector2 = ((settings != null) ? new Vector3(settings.portraitPanX, 0f, settings.portraitPanY) : PortraitOffset);
		float num = ((settings != null) ? Mathf.Clamp(settings.portraitZoom, 0.4f, 4f) : 1.28205f) * zoomMul;
		RaceFraming raceFraming = RaceFramingTable.For(p);
		if (raceFraming != null)
		{
			vector2 += new Vector3(raceFraming.panX, 0f, raceFraming.panY);
			num *= raceFraming.zoom;
		}
		num = Mathf.Clamp(num, 0.2f, 8f);
		try
		{
			Vector2 size = new Vector2(x, y);
			Vector3 cameraOffset = vector2;
			float cameraZoom = num;
			PawnHealthState? healthStateOverride = PawnHealthState.Mobile;
			return PortraitsCache.Get(p, size, rot, cameraOffset, cameraZoom, supersample: true, compensateForUIScale: true, renderHeadgear: true, renderClothes: true, null, null, stylingStation: false, healthStateOverride);
		}
		catch
		{
			return null;
		}
	}

	private static void ReleaseAllRTs()
	{
		foreach (KeyValuePair<Pawn, RenderTexture> rt in rts)
		{
			if (rt.Value != null)
			{
				rt.Value.Release();
			}
		}
		rts.Clear();
	}

	private static bool IsInView(Pawn p)
	{
		CameraDriver cameraDriver = Find.CameraDriver;
		return cameraDriver != null && cameraDriver.CurrentViewRect.Contains(p.Position);
	}

	public static void OnDynamicThingsDrawn()
	{
		try
		{
			if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null)
			{
				return;
			}
			RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
			if (settings != null && settings.masterEnabled && settings.liveView && !(RcbIslandAnim.ContentA < 0.35f))
			{
				if (settings.useLiveCameras)
				{
					RenderDue(settings);
				}
				else if (rts.Count > 0)
				{
					ReleaseAllRTs();
				}
			}
		}
		catch (Exception ex)
		{
			Log.WarningOnce("[Radius UI - Colonist Bar] live view render error: " + ex, 81231744);
		}
	}

	private static void RenderDue(RadiusColonistBarSettings s)
	{
		ColonistBar colonistBar = Find.ColonistBar;
		if (colonistBar == null)
		{
			return;
		}
		shown.Clear();
		eligible.Clear();
		List<ColonistBar.Entry> entries = colonistBar.Entries;
		for (int i = 0; i < entries.Count; i++)
		{
			Pawn pawn = entries[i].pawn;
			if (pawn != null)
			{
				shown.Add(pawn);
				if (!pawn.Dead && pawn.Spawned && pawn.Map == Find.CurrentMap && IsInView(pawn))
				{
					eligible.Add(pawn);
				}
			}
		}
		ReleaseUnshown();
		if (eligible.Count == 0)
		{
			return;
		}
		EnsureCam();
		Camera camera = Find.Camera;
		if (!(camera == null))
		{
			int a = Mathf.Clamp(s.liveRendersPerFrame, 1, 16);
			int num = Mathf.Min(a, eligible.Count);
			for (int j = 0; j < num; j++)
			{
				rrIndex = (rrIndex + 1) % eligible.Count;
				RenderOne(eligible[rrIndex], camera, s);
			}
		}
	}

	private static void RenderOne(Pawn p, Camera main, RadiusColonistBarSettings s)
	{
		float aspect = LiveAspect(s);
		RenderTexture orCreateRT = GetOrCreateRT(p, s.liveResolution, aspect);
		if (!(orCreateRT == null))
		{
			Vector3 drawPos = p.DrawPos;
			float num = Mathf.Clamp(7.5f / Mathf.Max(0.5f, s.liveZoom), 0.2f, 12f);
			liveCam.CopyFrom(main);
			liveCam.enabled = false;
			liveCam.clearFlags = CameraClearFlags.Color;
			liveCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
			liveCam.aspect = aspect;
			liveCam.targetTexture = orCreateRT;
			liveCam.transform.rotation = main.transform.rotation;
			Vector3 vector = drawPos;
			Pawn partner = PawnUiCache.Get(p).partner;
			if (partner != null && partner.Spawned && partner.Map == p.Map)
			{
				Vector3 drawPos2 = partner.DrawPos;
				vector = new Vector3((drawPos.x + drawPos2.x) * 0.5f, drawPos.y, (drawPos.z + drawPos2.z) * 0.5f);
				float num2 = drawPos.x - drawPos2.x;
				float num3 = drawPos.z - drawPos2.z;
				float num4 = Mathf.Sqrt(num2 * num2 + num3 * num3) * 0.5f;
				num = Mathf.Clamp(Mathf.Max(num, num4 + 1.6f), num, 24f);
			}
			liveCam.transform.position = new Vector3(vector.x + s.livePanX * num, main.transform.position.y, vector.z + s.livePanY * num);
			liveCam.orthographicSize = num;
			float num5 = (AltitudeLayer.MapDataOverlay.AltitudeFor() + AltitudeLayer.MetaOverlays.AltitudeFor()) * 0.5f;
			liveCam.nearClipPlane = Mathf.Max(0.05f, main.transform.position.y - num5);
			liveCam.Render();
			liveCam.targetTexture = null;
		}
	}

	private static float LiveAspect(RadiusColonistBarSettings s)
	{
		return Mathf.Clamp(23f / 28f, 0.2f, 5f);
	}

	private static void EnsureCam()
	{
		if (!(liveCam != null))
		{
			GameObject gameObject = new GameObject("RCB_LiveViewCamera");
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			liveCam = gameObject.AddComponent<Camera>();
			liveCam.enabled = false;
		}
	}

	private static RenderTexture GetOrCreateRT(Pawn p, int res, float aspect)
	{
		res = Mathf.Clamp(res, 32, 512);
		int num = res;
		int num2 = Mathf.Clamp(Mathf.RoundToInt((float)res * aspect), 16, 1024);
		if (rts.TryGetValue(p, out var value))
		{
			if (value != null && value.width == num2 && value.height == num && value.IsCreated())
			{
				return value;
			}
			if (value != null)
			{
				value.Release();
			}
			rts.Remove(p);
		}
		RenderTexture renderTexture = new RenderTexture(num2, num, 16)
		{
			name = "RCB_Live_" + p.thingIDNumber,
			antiAliasing = 1
		};
		renderTexture.Create();
		rts[p] = renderTexture;
		return renderTexture;
	}

	private static void ReleaseUnshown()
	{
		if (rts.Count == 0)
		{
			return;
		}
		toRelease.Clear();
		foreach (KeyValuePair<Pawn, RenderTexture> rt in rts)
		{
			Pawn key = rt.Key;
			if (key == null || key.Destroyed || !shown.Contains(key))
			{
				toRelease.Add(key);
			}
		}
		for (int i = 0; i < toRelease.Count; i++)
		{
			if (rts.TryGetValue(toRelease[i], out var value) && value != null)
			{
				value.Release();
			}
			rts.Remove(toRelease[i]);
		}
	}
}
