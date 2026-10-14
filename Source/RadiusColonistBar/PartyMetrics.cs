using UnityEngine;

namespace RadiusColonistBar;

internal static class PartyMetrics
{
	private static int frame = -1;

	private static float right;

	public static void Record(float xMax)
	{
		if (Time.frameCount != frame)
		{
			frame = Time.frameCount;
			right = xMax;
		}
		else
		{
			right = Mathf.Max(right, xMax);
		}
	}

	public static float RightEdge()
	{
		return (Time.frameCount - frame <= 1) ? right : 0f;
	}
}
