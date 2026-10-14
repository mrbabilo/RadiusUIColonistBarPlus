using Verse;

namespace RadiusColonistBar;

public static class SidearmsBridge
{
	private static int active = -1;

	private static JobDef equipSecondary;

	private static bool jobLooked;

	public static bool Active
	{
		get
		{
			if (active < 0)
			{
				try
				{
					active = (ModsConfig.IsActive("petetimessix.simplesidearms") ? 1 : 0);
				}
				catch
				{
					active = 0;
				}
			}
			return active == 1;
		}
	}

	public static JobDef EquipSecondaryJob
	{
		get
		{
			if (!jobLooked)
			{
				jobLooked = true;
				try
				{
					if (Active)
					{
						equipSecondary = DefDatabase<JobDef>.GetNamedSilentFail("EquipSecondary");
					}
				}
				catch
				{
				}
			}
			return equipSecondary;
		}
	}
}
