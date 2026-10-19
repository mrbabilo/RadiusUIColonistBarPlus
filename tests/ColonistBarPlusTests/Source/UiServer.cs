using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RadiusColonistBar;
using RimWorld;
using UnityEngine;
using Verse;

namespace RCBPlusTests;

// Interactive UI run (-rcbui=<dir>): builds the scene, then serves commands from <dir>/cmd.txt and dumps game state
// to <dir>/state.json every few frames, so an outside driver can post real mouse events and check their effect.
// Commands, one per call: "<id> <verb> [args]"; the ack is "<id> <note>" in <dir>/ack.txt once done.
public class UiServer : GameComponent
{
	private static string dir;

	private bool ready;

	private int frames;

	private long lastCmd = -1;

	private int squad = -1;

	private readonly List<Pawn> pawns = new List<Pawn>();

	private int ackFrame = -1;

	private string ackText;

	private Fluency fluency;

	// Last MouseDown seen by the UI root (GUI coordinates), synthetic ones included.
	internal static Vector3 lastDown = new Vector3(-1f, -1f, -1f);

	internal static int lastDownFrame = -1;

	internal static int wheelFrame = -1;

	internal static float wheelDelta;

	// After a synthetic wheel pass: true when something consumed the event (the card does over its grid).
	internal static bool lastWheelUsed;

	internal static int lastWheelUsedFrame = -1;

	// Synthetic input, played one event per frame by Patch_RecordInput: the OS route (CGEventPostToPid, activation)
	// does not reach a background Unity window, so the driver injects at the IMGUI event level instead. The whole
	// widget path below UIRoot_Play.UIRootOnGUI (WindowStack routing, hit tests, consumption) stays real.
	internal struct Synth
	{
		public EventType type;

		public int button;

		public Vector2 pos;

		public float delta;
	}

	internal static readonly Queue<Synth> queue = new Queue<Synth>();

	internal static Vector2? hover;

	internal static int playedFrame = -1;

	// True while the current OnGUI pass carries a synthetic event: GUI.Window routes native events only, so windows
	// are then drawn by calling InnerWindowOnGUI directly under a GUI.BeginGroup (Patch_ForceWindow).
	internal static bool syntheticPass;

	// Diagnostics: synthetic passes played, windows force-drawn, ButtonInvisible calls seen and hits during them,
	// and the last rect/mouse pair checked on a MouseUp pass.
	internal static int dSynth;

	internal static int dForced;

	internal static int dBtnCalls;

	internal static int dBtnHits;

	internal static string dLastBtn = "";


	// GUIClip rebuilds Event.current.mousePosition from the hardware cursor, so a mutated position does not survive
	// into groups: the harness carries its own. synthPos is the global GUI position of the event being played; the
	// offset stack mirrors the groups entered (forced window, scroll views) so Local() yields the position in the
	// space the current rects are expressed in.
	internal static Vector2 synthPos;

	// Type and button of the synthetic event being dispatched: Event.current.type reads Ignore over a natively
	// registered window, so the seams decide from the harness state instead.
	internal static EventType synthType;

	internal static int synthButton;

	internal static readonly List<Vector2> offsets = new List<Vector2>();

	internal static Vector2? EffectivePos => syntheticPass ? synthPos : hover;

	internal static Vector2 Local(Vector2 global)
	{
		for (int i = 0; i < offsets.Count; i++)
		{
			global -= offsets[i];
		}
		return global;
	}

	// True when nothing covers the global position above the window being drawn (or above the map when none is).
	internal static bool TopmostHere(Vector2 global)
	{
		List<Window> windows = (List<Window>)AccessTools.Field(typeof(WindowStack), "windows").GetValue(Find.WindowStack);
		Window window = null;
		for (int i = windows.Count - 1; i >= 0; i--)
		{
			if (windows[i].windowRect.Contains(global))
			{
				window = windows[i];
				break;
			}
		}
		return window == Find.WindowStack.currentlyDrawnWindow || window == null;
	}

	public UiServer(Game game)
	{
	}

	public static bool On => dir != null;

	// Measurement mode (-rcbscene=<dir>): the same command server, but no Harmony patch at all and no scene of its own
	// (the RCBPlusScene helper spawns colonists and raiders), so a profiler sees Colonist Bar Plus and nothing of ours.
	public static bool Scene;

	public static bool Interactive => On && !Scene;

	public static void Bind()
	{
		if (!GenCommandLine.TryGetCommandLineArg("rcbui", out dir))
		{
			Scene = GenCommandLine.TryGetCommandLineArg("rcbscene", out dir);
		}
	}

	public override void GameComponentUpdate()
	{
		if (dir == null || Find.CurrentMap == null || LongEventHandler.ShouldWaitForEvent)
		{
			return;
		}
		if (!ready)
		{
			if (++frames < 120)
			{
				return;
			}
			ready = true;
			try
			{
				if (Scene)
				{
					File.WriteAllText(Path.Combine(dir, "ack.txt"), "0 ready scene");
				}
				else
				{
					Setup();
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Path.Combine(dir, "ack.txt"), "-1 setup failed: " + ex);
			}
		}
		if (ackFrame >= 0 && Time.frameCount >= ackFrame)
		{
			File.WriteAllText(Path.Combine(dir, "ack.txt"), ackText);
			ackFrame = -1;
		}
		if (fluency != null)
		{
			fluency.Update();
			if (fluency.Done)
			{
				fluency = null;
			}
		}
		if (Time.frameCount % (Scene ? 30 : 5) == 0)
		{
			DumpState();
		}
		if (Scene && Time.frameCount % 10 != 0)
		{
			return;
		}
		string path = Path.Combine(dir, "cmd.txt");
		if (!File.Exists(path))
		{
			return;
		}
		string[] array;
		try
		{
			array = File.ReadAllText(path).Trim().Split(new char[1] { ' ' }, 3);
		}
		catch (IOException)
		{
			return;
		}
		if (array.Length < 2 || !long.TryParse(array[0], out var id) || id <= lastCmd)
		{
			return;
		}
		lastCmd = id;
		string note = "ok";
		int delay = 1;
		try
		{
			switch (array[1])
			{
			case "shot":
			{
				string name = array[2];
				ScreenCapture.CaptureScreenshot(Path.Combine(dir, name));
				delay = 5;
				break;
			}
			case "opencard":
			{
				string[] xy = array[2].Split(' ');
				AccessTools.Method(typeof(SquadOrderCard), "Toggle").Invoke(null, new object[2]
				{
					squad,
					new Rect(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture) - 4f, 0f, 0f)
				});
				break;
			}
			case "fluency":
			{
				string[] args = array[2].Split(' ');
				if (squad < 0)
				{
					note = "no squad: send squadall first";
					break;
				}
				fluency = new Fluency(squad, int.Parse(args[0]), args.Length > 1 ? int.Parse(args[1]) : 3600, dir);
				note = "started, result in fluency-tab" + args[0] + ".txt";
				break;
			}
			case "closecard":
				Card()?.Close();
				break;
			case "settab":
				AccessTools.StaticFieldRefAccess<int>(typeof(SquadOrderCard), "tab") = int.Parse(array[2]);
				break;
			case "setworknow":
				AccessTools.StaticFieldRefAccess<bool>(typeof(SquadOrderCard), "workNow") = array[2] == "1";
				break;
			case "setmanual":
				AccessTools.Method(AccessTools.TypeByName("RadiusColonistBar.SquadWork"), "SetManual").Invoke(null, new object[1] { array[2] == "1" });
				break;
			case "setprio":
			{
				string[] parts = array[2].Split(' ');
				Pawn pawn = pawns.First((Pawn p) => p.LabelShort == parts[0]);
				pawn.workSettings.SetPriority(WorkTypeDefOf.PlantCutting, int.Parse(parts[1]));
				break;
			}
			case "squadall":
			{
				List<Pawn> list = Find.CurrentMap.mapPawns.FreeColonistsSpawned.ToList();
				squad = Fixture.Squad(list.ToArray());
				pawns.Clear();
				note = "squad=" + squad + " members=" + list.Count;
				break;
			}
			case "opencc":
				Dialog_CommandCenter.Open();
				break;
			case "closeall":
				Find.WindowStack.Windows.Where((Window w) => w is SquadOrderCard || w is Dialog_CommandCenter).ToList().ForEach((Window w) => w.Close());
				break;
			case "draft":
				AccessTools.Method(AccessTools.TypeByName("RadiusColonistBar.SquadOrders"), "Draft").Invoke(null, new object[1] { squad });
				break;
			case "undraft":
				AccessTools.Method(AccessTools.TypeByName("RadiusColonistBar.SquadOrders"), "Undraft").Invoke(null, new object[1] { squad });
				break;
			case "assault":
				AccessTools.Method(AccessTools.TypeByName("RadiusColonistBar.SquadOrders"), "Assault").Invoke(null, new object[2] { squad, (AssaultMode)int.Parse(array[2]) });
				break;
			case "stopassault":
				AccessTools.Method(AccessTools.TypeByName("RadiusColonistBar.SquadOrders"), "StopAssault").Invoke(null, new object[1] { squad });
				break;
			case "deselect":
				Find.Selector.ClearSelection();
				break;
			case "click":
			case "rclick":
			{
				string[] xy = array[2].Split(' ');
				Vector2 pos = new Vector2(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture));
				int button = array[1] == "rclick" ? 1 : 0;
				queue.Enqueue(new Synth { type = EventType.MouseDown, button = button, pos = pos });
				queue.Enqueue(new Synth { type = EventType.MouseUp, button = button, pos = pos });
				delay = 4;
				break;
			}
			case "wheel":
			{
				string[] xy = array[2].Split(' ');
				queue.Enqueue(new Synth
				{
					type = EventType.ScrollWheel,
					pos = new Vector2(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture)),
					delta = float.Parse(xy[2], CultureInfo.InvariantCulture)
				});
				delay = 3;
				break;
			}
			case "hover":
			{
				string[] xy = array[2].Split(' ');
				hover = new Vector2(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture));
				break;
			}
			case "hoveroff":
				hover = null;
				break;
			case "quit":
				File.WriteAllText(Path.Combine(dir, "ack.txt"), id + " bye");
				Root.Shutdown();
				return;
			default:
				note = "unknown verb";
				break;
			}
		}
		catch (Exception ex)
		{
			note = "failed: " + ex.Message;
		}
		ackText = id + " " + note;
		ackFrame = Time.frameCount + delay;
	}

	private void Setup()
	{
		Fixture.Bind();
		pawns.Add(Fixture.NewColonist(WorkTypeDefOf.PlantCutting));
		pawns.Add(Fixture.NewColonist(WorkTypeDefOf.PlantCutting));
		pawns.Add(Fixture.NewColonist(WorkTypeDefOf.PlantCutting));
		Pawn pawn = Fixture.NewColonist();
		Fixture.Disable(pawn, WorkTypeDefOf.PlantCutting);
		pawns.Add(pawn);
		for (int i = 0; i < 6; i++)
		{
			Fixture.Tree(Fixture.FreeCell(10));
		}
		Fixture.Meals();
		Fixture.Beds(2);
		Fixture.JoySpot();
		squad = Fixture.Squad(pawns.ToArray());
		Find.TickManager.Pause();
		Find.Selector.ClearSelection();
		DumpState();
		File.WriteAllText(Path.Combine(dir, "ack.txt"), "0 ready squad=" + squad + " pawns=" + string.Join(",", pawns.Select((Pawn p) => p.LabelShort)));
	}

	private SquadOrderCard Card()
	{
		return (SquadOrderCard)AccessTools.StaticFieldRefAccess<object>(typeof(SquadOrderCard), "open");
	}

	private void DumpState()
	{
		SquadOrderCard card = Card();
		StringBuilder sb = new StringBuilder("{");
		sb.Append("\"frame\":" + Time.frameCount);
		sb.Append(",\"screenW\":" + Screen.width + ",\"screenH\":" + Screen.height);
		sb.Append(",\"uiScale\":" + Prefs.UIScale.ToString(CultureInfo.InvariantCulture));
		sb.Append(",\"paused\":" + (Find.TickManager.Paused ? "true" : "false"));
		sb.Append(",\"manual\":" + (Find.PlaySettings.useWorkPriorities ? "true" : "false"));
		sb.Append(",\"rootSize\":" + ((float)AccessTools.Field(typeof(CameraDriver), "rootSize").GetValue(Find.CameraDriver)).ToString(CultureInfo.InvariantCulture));
		sb.Append(",\"windows\":" + Find.WindowStack.Count);
		sb.Append(",\"floatMenu\":" + (Find.WindowStack.FloatMenu != null ? "true" : "false"));
		sb.Append(",\"selected\":" + Find.Selector.NumSelected);
		sb.Append(",\"fluency\":" + (fluency != null ? "true" : "false"));
		sb.Append(",\"lastDown\":[" + lastDown.x.ToString(CultureInfo.InvariantCulture) + "," + lastDown.y.ToString(CultureInfo.InvariantCulture) + "," + lastDown.z + "," + lastDownFrame + "]");
		sb.Append(",\"wheel\":[" + wheelDelta.ToString(CultureInfo.InvariantCulture) + "," + wheelFrame + "]");
		sb.Append(",\"wheelUsed\":[" + (lastWheelUsed ? "true" : "false") + "," + lastWheelUsedFrame + "]");
		sb.Append(",\"diag\":\"synth " + dSynth + ", forced " + dForced + ", btn " + dBtnCalls + "/" + dBtnHits + ", last " + dLastBtn + "\"");
		if (card != null)
		{
			sb.Append(",\"card\":{\"x\":" + card.windowRect.x.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + card.windowRect.y.ToString(CultureInfo.InvariantCulture) + ",\"w\":" + card.windowRect.width.ToString(CultureInfo.InvariantCulture) + ",\"h\":" + card.windowRect.height.ToString(CultureInfo.InvariantCulture));
			sb.Append(",\"tab\":" + AccessTools.StaticFieldRefAccess<int>(typeof(SquadOrderCard), "tab"));
			sb.Append(",\"workNow\":" + (AccessTools.StaticFieldRefAccess<bool>(typeof(SquadOrderCard), "workNow") ? "true" : "false"));
			sb.Append(",\"scrollY\":" + ((Vector2)AccessTools.Field(typeof(SquadOrderCard), "workScroll").GetValue(card)).y.ToString(CultureInfo.InvariantCulture) + "}");
		}
		else
		{
			sb.Append(",\"card\":null");
		}
		sb.Append(",\"pawns\":[");
		for (int i = 0; i < pawns.Count; i++)
		{
			Pawn p = pawns[i];
			sb.Append((i > 0 ? "," : "") + "{\"name\":\"" + p.LabelShort + "\",\"prio\":" + (p.workSettings?.GetPriority(WorkTypeDefOf.PlantCutting) ?? -1) + ",\"drafted\":" + (p.Drafted ? "true" : "false") + ",\"job\":\"" + (p.CurJobDef?.defName ?? "none") + "\"}");
		}
		sb.Append("]}");
		File.WriteAllText(Path.Combine(dir, "state.json"), sb.ToString());
	}
}

// Plays the synthetic input queue (one event per frame, on the Repaint pass) and records every MouseDown and wheel
// event the UI root sees.
[HarmonyPatch(typeof(UIRoot_Play), "UIRootOnGUI")]
internal static class Patch_RecordInput
{
	private static void Prefix()
	{
		if (!UiServer.On)
		{
			return;
		}
		Event current = Event.current;
		if (current == null)
		{
			return;
		}
		if (UiServer.hover.HasValue)
		{
			current.mousePosition = UiServer.hover.Value;
		}
		UiServer.syntheticPass = false;
		UiServer.offsets.Clear();
		if (current.type == EventType.Repaint && UiServer.queue.Count > 0 && UiServer.playedFrame != Time.frameCount)
		{
			// The event is shown to the windows only, in stack order, then the pass resumes as a plain Repaint:
			// WindowStack.HandleEventsHighPriority and Selector.HandleMapClicks would otherwise use it up first.
			UiServer.playedFrame = Time.frameCount;
			UiServer.Synth synth = UiServer.queue.Dequeue();
			// A fresh Event carries the right rawType; mutating the pass's own event does not survive GUI group pushes.
			Event built = new Event
			{
				type = synth.type,
				button = synth.button,
				mousePosition = synth.pos,
				clickCount = 1
			};
			if (synth.type == EventType.ScrollWheel)
			{
				built.delta = new Vector2(0f, synth.delta);
			}
			Event original = current;
			Event.current = built;
			current = built;
			UiServer.synthPos = synth.pos;
			UiServer.synthType = synth.type;
			UiServer.synthButton = synth.button;
			UiServer.syntheticPass = true;
			UiServer.dSynth++;
			if (synth.type == EventType.MouseDown)
			{
				UiServer.lastDown = new Vector3(synth.pos.x, synth.pos.y, synth.button);
				UiServer.lastDownFrame = Time.frameCount;
			}
			else if (synth.type == EventType.ScrollWheel)
			{
				UiServer.wheelDelta = synth.delta;
				UiServer.wheelFrame = Time.frameCount;
			}
			List<Window> list = new List<Window>((List<Window>)AccessTools.Field(typeof(WindowStack), "windows").GetValue(Find.WindowStack));
			foreach (Window window in list)
			{
				try
				{
					window.WindowOnGUI();
				}
				catch (Exception ex)
				{
					// A window that fails on an injected event must not stop the others from seeing it.
					Log.Warning("[RCBPlusTests] injected event failed in " + window.GetType().Name + ": " + ex.Message);
				}
			}
			if (synth.type == EventType.ScrollWheel)
			{
				UiServer.lastWheelUsed = current.type == EventType.Used;
				UiServer.lastWheelUsedFrame = Time.frameCount;
			}
			Find.WindowStack.currentlyDrawnWindow = null;
			UiServer.syntheticPass = false;
			UiServer.synthType = EventType.Repaint;
			Event.current = original;
			current = original;
		}
		if (current.type == EventType.MouseDown)
		{
			UiServer.lastDown = new Vector3(current.mousePosition.x, current.mousePosition.y, current.button);
			UiServer.lastDownFrame = Time.frameCount;
		}
		else if (current.type == EventType.ScrollWheel)
		{
			UiServer.wheelDelta = current.delta.y;
			UiServer.wheelFrame = Time.frameCount;
		}
	}

	private static void Postfix()
	{
		if (UiServer.syntheticPass && Event.current != null && UiServer.wheelFrame == Time.frameCount)
		{
			UiServer.lastWheelUsed = Event.current.type == EventType.Used;
			UiServer.lastWheelUsedFrame = Time.frameCount;
		}
		UiServer.syntheticPass = false;
	}
}

// The map selector and other pre-window consumers gate on the hardware cursor through UI.MousePositionOnUIInverted:
// with the real cursor outside the window they treat an injected MouseDown as a map click and use it up before the
// card draws. Both getters follow the harness position instead.
[HarmonyPatch(typeof(UI), "MousePositionOnUIInverted", MethodType.Getter)]
internal static class Patch_SynthMouseInverted
{
	private static bool Prefix(ref Vector2 __result)
	{
		Vector2? effective = UiServer.EffectivePos;
		if (!effective.HasValue)
		{
			return true;
		}
		__result = effective.Value;
		return false;
	}
}

[HarmonyPatch(typeof(UI), "MousePositionOnUI", MethodType.Getter)]
internal static class Patch_SynthMouse
{
	private static bool Prefix(ref Vector2 __result)
	{
		Vector2? effective = UiServer.EffectivePos;
		if (!effective.HasValue)
		{
			return true;
		}
		__result = new Vector2(effective.Value.x, (float)UI.screenHeight - effective.Value.y);
		return false;
	}
}



// GUI.Button's native state machine never sees the injected events: during a synthetic pass, ButtonInvisible is
// decided here instead — a MouseUp inside the rect is a click, any button, like the native control.
[HarmonyPatch(typeof(Widgets), "ButtonInvisible")]
internal static class Patch_SynthButton
{
	private static bool Prefix(Rect butRect, ref bool __result)
	{
		if (!UiServer.syntheticPass)
		{
			return true;
		}
		UiServer.dBtnCalls++;
		Vector2 local = UiServer.Local(UiServer.synthPos);
		__result = UiServer.synthType == EventType.MouseUp && butRect.Contains(local) && UiServer.TopmostHere(UiServer.synthPos);
		if (UiServer.synthType == EventType.MouseUp)
		{
			UiServer.dLastBtn = butRect.ToString() + " vs " + local.ToString() + " top=" + UiServer.TopmostHere(UiServer.synthPos);
		}
		if (__result)
		{
			UiServer.dBtnHits++;
			Event.current.Use();
		}
		return false;
	}
}

// Same seam for the native scroll view: a synthetic wheel over the view moves the scroll position by hand, then the
// event stays live so the drawing code's own consumption (the card uses it up over its grid) is what the test sees.
[HarmonyPatch(typeof(Widgets), "BeginScrollView")]
internal static class Patch_SynthScroll
{
	private static void Prefix(Rect outRect, ref Vector2 scrollPosition, Rect viewRect)
	{
		if (!UiServer.syntheticPass)
		{
			return;
		}
		if (UiServer.synthType == EventType.ScrollWheel && UiServer.syntheticPass && outRect.Contains(UiServer.Local(UiServer.synthPos)))
		{
			scrollPosition.y = Mathf.Clamp(scrollPosition.y + UiServer.wheelDelta * 20f, 0f, Mathf.Max(0f, viewRect.height - outRect.height));
		}
	}

	// Keeps the harness offset stack aligned with the scroll view's group: rects inside are in view space.
	private static void Postfix(Rect outRect, Vector2 scrollPosition)
	{
		if (UiServer.syntheticPass || UiServer.hover.HasValue)
		{
			UiServer.offsets.Add(outRect.position - scrollPosition);
		}
	}
}

[HarmonyPatch(typeof(Widgets), "EndScrollView")]
internal static class Patch_SynthScrollEnd
{
	private static void Prefix()
	{
		if ((UiServer.syntheticPass || UiServer.hover.HasValue) && UiServer.offsets.Count > 0)
		{
			UiServer.offsets.RemoveAt(UiServer.offsets.Count - 1);
		}
	}
}

// Mouse.IsOver reads the GUIClip-translated hardware position: during synthetic or hover passes, answer from the
// harness position instead, in the same local space as the rect, with a topmost-window check.
[HarmonyPatch(typeof(Mouse), "IsOver")]
internal static class Patch_SynthIsOver
{
	private static bool Prefix(Rect rect, ref bool __result)
	{
		Vector2? effective = UiServer.EffectivePos;
		if (!effective.HasValue)
		{
			return true;
		}
		__result = rect.Contains(UiServer.Local(effective.Value)) && UiServer.TopmostHere(effective.Value);
		return false;
	}
}

// During a synthetic pass, GUI.Window would route by the pass's original native event and drop the injected one:
// draw the window body directly instead, inside a group at windowRect so every rect and mouse read stays local.
[HarmonyPatch(typeof(Window), "WindowOnGUI")]
internal static class Patch_ForceWindow
{
	private static readonly MethodInfo inner = AccessTools.Method(typeof(Window), "InnerWindowOnGUI");

	private static bool Prefix(Window __instance)
	{
		// Forced during synthetic and hover passes only: forcing every pass breaks GUILayout windows (EditWindow_Log).
		if (!UiServer.syntheticPass && !UiServer.hover.HasValue)
		{
			return true;
		}
		UiServer.dForced++;
		GUI.BeginGroup(__instance.windowRect);
		UiServer.offsets.Add(__instance.windowRect.position);
		try
		{
			inner.Invoke(__instance, new object[1] { __instance.ID });
		}
		finally
		{
			UiServer.offsets.RemoveAt(UiServer.offsets.Count - 1);
			GUI.EndGroup();
		}
		return false;
	}
}
