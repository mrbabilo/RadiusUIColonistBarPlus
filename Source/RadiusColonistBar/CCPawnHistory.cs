using System.Collections.Generic;
using Verse;

namespace RadiusColonistBar;

public class CCPawnHistory : IExposable
{
	public Dictionary<string, CCSeries> series = new Dictionary<string, CCSeries>();

	public int lastSeenHour = -1;

	public void ExposeData()
	{
		Scribe_Collections.Look(ref series, "series", LookMode.Value, LookMode.Deep);
		Scribe_Values.Look(ref lastSeenHour, "lastSeenHour", -1);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && series == null)
		{
			series = new Dictionary<string, CCSeries>();
		}
	}
}
