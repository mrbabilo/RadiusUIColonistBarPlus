using System.Collections.Generic;
using Verse;

namespace RadiusColonistBar;

public class CCKit : IExposable
{
	public string name = "Kit";

	public string weapon;

	public List<string> apparel = new List<string>();

	public void ExposeData()
	{
		Scribe_Values.Look(ref name, "name", "Kit");
		Scribe_Values.Look(ref weapon, "weapon");
		Scribe_Collections.Look(ref apparel, "apparel", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && apparel == null)
		{
			apparel = new List<string>();
		}
	}
}
