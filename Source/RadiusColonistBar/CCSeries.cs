using System.Collections.Generic;
using Verse;

namespace RadiusColonistBar;

public class CCSeries : IExposable
{
	public int startHour = -1;

	public List<float> levels = new List<float>();

	public int Count => levels.Count;

	public float DayAt(int i)
	{
		return (float)(startHour + i) / 24f;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref startHour, "startHour", -1);
		Scribe_Collections.Look(ref levels, "levels", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && levels == null)
		{
			levels = new List<float>();
		}
	}
}
