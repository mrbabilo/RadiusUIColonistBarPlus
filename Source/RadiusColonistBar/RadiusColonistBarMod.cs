using UnityEngine;
using Verse;

namespace RadiusColonistBar;

public class RadiusColonistBarMod : Mod
{
	public static RadiusColonistBarSettings Settings;

	public static RadiusColonistBarMod Instance;

	public RadiusColonistBarMod(ModContentPack content)
		: base(content)
	{
		Instance = this;
		Settings = GetSettings<RadiusColonistBarSettings>();
	}

	public override string SettingsCategory()
	{
		return "RCB_SettingsCategory".Translate();
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		SettingsUI.Draw(inRect, Settings);
	}
}
