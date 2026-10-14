using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbIslandAnim
{
	private enum Fold
	{
		Auto,
		Shut,
		Armed
	}

	private static float openT = 1f;

	private static int advancedFrame = -1;

	private static float lastTime = -1f;

	private static bool hovered;

	private static float lastInteract = -999f;

	private static float lastSeen = -1f;

	private static Fold fold;

	public static float OpenT => openT;

	public static bool Animating => openT > 0.002f && openT < 0.998f;

	public static bool SuppressCellInput => openT < 0.998f;

	public static float HeightT => Ease(Mathf.Clamp01(openT));

	public static float ContentA => Mathf.Clamp01((openT - 0.55f) / 0.45f);

	public static float SummaryA => 1f - Mathf.Clamp01(openT / 0.3f);

	public static void ToggleFold()
	{
		fold = ((openT > 0.5f) ? Fold.Shut : Fold.Auto);
	}

	private static float Ease(float t)
	{
		return 1f - Mathf.Pow(1f - t, 3f);
	}

	public static void AdvanceOnce(Rect bounds)
	{
		if (advancedFrame == Time.frameCount)
		{
			return;
		}
		advancedFrame = Time.frameCount;
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		bool flag = lastSeen < 0f || realtimeSinceStartup - lastSeen > 1f;
		lastSeen = realtimeSinceStartup;
		RadiusColonistBarSettings settings = RadiusColonistBarMod.Settings;
		if (settings != null && settings.previewState != 0)
		{
			openT = ((settings.previewState == 1) ? 1f : 0f);
			lastInteract = realtimeSinceStartup;
			lastTime = realtimeSinceStartup;
			return;
		}
		if (flag)
		{
			fold = Fold.Auto;
			lastInteract = realtimeSinceStartup;
			lastTime = realtimeSinceStartup;
			openT = 1f;
		}
		if (RcbGuard.Finite(bounds) && bounds.width > 1f)
		{
			hovered = Mouse.IsOver(bounds);
		}
		if (hovered)
		{
			lastInteract = realtimeSinceStartup;
		}
		float num = (float)((settings != null) ? Mathf.Max(0, settings.islandAnimMs) : 330) / 1000f;
		float target;
		if (fold == Fold.Shut)
		{
			target = 0f;
			if (!hovered)
			{
				fold = Fold.Armed;
			}
		}
		else if (fold == Fold.Armed)
		{
			if (hovered)
			{
				fold = Fold.Auto;
				lastInteract = realtimeSinceStartup;
				target = 1f;
			}
			else
			{
				target = 0f;
			}
		}
		else
		{
			if (settings == null || !settings.islandCollapse)
			{
				openT = 1f;
				return;
			}
			float num2 = ((settings != null) ? Mathf.Clamp(settings.islandCollapseDelaySec, 0f, 300f) : 0f);
			target = ((hovered || (num2 > 0f && realtimeSinceStartup - lastInteract < num2)) ? 1f : 0f);
		}
		if (num <= 0.001f)
		{
			openT = target;
			return;
		}
		float realtimeSinceStartup2 = Time.realtimeSinceStartup;
		if (lastTime < 0f)
		{
			lastTime = realtimeSinceStartup2;
		}
		float num3 = Mathf.Clamp(realtimeSinceStartup2 - lastTime, 0f, 0.25f);
		lastTime = realtimeSinceStartup2;
		openT = Mathf.MoveTowards(openT, target, num3 / num);
	}
}
