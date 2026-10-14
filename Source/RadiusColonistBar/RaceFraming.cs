using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public class RaceFraming : IExposable
{
	public string race;

	public float zoom = 1f;

	public float panX;

	public float panY;

	public bool IsDefault => Mathf.Approximately(zoom, 1f) && Mathf.Approximately(panX, 0f) && Mathf.Approximately(panY, 0f);

	public RaceFraming()
	{
	}

	public RaceFraming(string race)
	{
		this.race = race;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref race, "race");
		Scribe_Values.Look(ref zoom, "zoom", 1f);
		Scribe_Values.Look(ref panX, "panX", 0f);
		Scribe_Values.Look(ref panY, "panY", 0f);
		zoom = Mathf.Clamp(RcbGuard.Sane(zoom, 1f), 0.25f, 4f);
		panX = Mathf.Clamp(RcbGuard.Sane(panX, 0f), -2f, 2f);
		panY = Mathf.Clamp(RcbGuard.Sane(panY, 0f), -2f, 2f);
	}
}
