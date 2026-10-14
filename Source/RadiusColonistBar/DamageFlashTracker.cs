using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal sealed class DamageFlashTracker
{
	private const float Seconds = 2.5f;

	private readonly Dictionary<int, float> lastHealth = new Dictionary<int, float>();

	private readonly Dictionary<int, float> flashUntil = new Dictionary<int, float>();

	public float Flash(Pawn p)
	{
		int thingIDNumber = p.thingIDNumber;
		float num = 1f;
		try
		{
			num = (p.Dead ? 0f : Mathf.Clamp01(p.health.summaryHealth.SummaryHealthPercent));
		}
		catch
		{
		}
		if (lastHealth.TryGetValue(thingIDNumber, out var value) && num < value - 0.004f)
		{
			flashUntil[thingIDNumber] = Time.realtimeSinceStartup + 2.5f;
		}
		lastHealth[thingIDNumber] = num;
		if (flashUntil.TryGetValue(thingIDNumber, out var value2))
		{
			float num2 = value2 - Time.realtimeSinceStartup;
			if (num2 > 0f)
			{
				return Mathf.Clamp01(num2 / 2.5f);
			}
		}
		return 0f;
	}

	public void Prune(HashSet<int> live)
	{
		RcbUtil.PruneById(lastHealth, live);
		RcbUtil.PruneById(flashUntil, live);
	}
}
