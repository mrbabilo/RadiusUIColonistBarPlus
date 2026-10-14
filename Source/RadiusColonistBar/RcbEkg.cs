using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RcbEkg
{
	private const int MaxSegs = 40;

	private const float StepPx = 2f;

	public static void Draw(Rect r, float frac, Pawn p)
	{
		if (!RcbGuard.Finite(r) || r.width < 8f)
		{
			return;
		}
		RcbSpatial.Meter(r, 0f, RcbPalette.PanelBG, RcbPalette.PanelBG);
		if (Event.current.type != EventType.Repaint)
		{
			return;
		}
		frac = Mathf.Clamp01(frac);
		Color color = ((frac > 0.6f) ? RcbPalette.Good : ((frac > 0.3f) ? RcbPalette.Warn : RcbPalette.Bad));
		color.a *= GUI.color.a;
		float y = r.center.y;
		float amp = Mathf.Lerp(1.5f, 4.5f, frac);
		float num = Mathf.Lerp(52f, 20f, frac);
		bool flat = frac < 0.05f;
		float num2 = (RcbAlertBand.PulsePhase + ((p != null) ? ((float)(p.thingIDNumber % 7) / 7f) : 0f)) * num;
		int num3 = Mathf.Min(40, Mathf.FloorToInt(r.width / 2f));
		if (num3 >= 2)
		{
			float num4 = r.width / (float)num3;
			float x = r.x;
			float y2 = y - Wave(num2, num, amp, flat);
			for (int i = 1; i <= num3; i++)
			{
				float num5 = r.x + (float)i * num4;
				float num6 = y - Wave(num5 - r.x + num2, num, amp, flat);
				Widgets.DrawLine(new Vector2(x, y2), new Vector2(num5, num6), color, 1f);
				x = num5;
				y2 = num6;
			}
		}
	}

	private static float Wave(float x, float beat, float amp, bool flat)
	{
		if (flat)
		{
			return 0f;
		}
		float num = x % beat / beat;
		if (num < 0f)
		{
			num += 1f;
		}
		if (num < 0.3f)
		{
			return 0f;
		}
		if (num < 0.36f)
		{
			return Mathf.Lerp(0f, 0.25f, (num - 0.3f) / 0.06f) * amp;
		}
		if (num < 0.42f)
		{
			return Mathf.Lerp(0.25f, 0f, (num - 0.36f) / 0.06f) * amp;
		}
		if (num < 0.46f)
		{
			return Mathf.Lerp(0f, -0.3f, (num - 0.42f) / 0.04f) * amp;
		}
		if (num < 0.5f)
		{
			return Mathf.Lerp(-0.3f, 1f, (num - 0.46f) / 0.04f) * amp;
		}
		if (num < 0.55f)
		{
			return Mathf.Lerp(1f, -0.45f, (num - 0.5f) / 0.05f) * amp;
		}
		if (num < 0.59f)
		{
			return Mathf.Lerp(-0.45f, 0f, (num - 0.55f) / 0.04f) * amp;
		}
		if (num < 0.7f)
		{
			return 0f;
		}
		if (num < 0.78f)
		{
			return Mathf.Lerp(0f, 0.35f, (num - 0.7f) / 0.08f) * amp;
		}
		if (num < 0.86f)
		{
			return Mathf.Lerp(0.35f, 0f, (num - 0.78f) / 0.08f) * amp;
		}
		return 0f;
	}
}
