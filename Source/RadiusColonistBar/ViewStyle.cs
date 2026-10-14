using Verse;

namespace RadiusColonistBar;

public class ViewStyle : IExposable
{
	public bool showName = true;

	public bool showRoyalTitle = true;

	public bool showJobTitle = true;

	public bool showIdeoRole = true;

	public bool showJobLabel = true;

	public bool showActivityIcons = false;

	public float activityIconScale = 0.6f;

	public bool showStatusIndicators = true;

	public bool showSquadBadges = true;

	public bool showSpeechBubbles = true;

	public string barTopStat = "__health__";

	public string barBottomStat = "__mood__";

	public int weaponMode = 1;

	public bool weaponPrimaryOnly = false;

	public bool pulseOnDamage = true;

	public bool warbandHideSelected = true;

	public bool warbandHidePerspective = true;

	public static ViewStyle Default(BarLayout view)
	{
		return new ViewStyle();
	}

	public ViewStyle Clone()
	{
		return (ViewStyle)MemberwiseClone();
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref showName, "showName", defaultValue: true);
		Scribe_Values.Look(ref showJobTitle, "showTitle", defaultValue: true);
		Scribe_Values.Look(ref showRoyalTitle, "showRoyalTitle", defaultValue: true);
		Scribe_Values.Look(ref showIdeoRole, "showIdeoRole", defaultValue: true);
		Scribe_Values.Look(ref showJobLabel, "showJobLabel", defaultValue: true);
		Scribe_Values.Look(ref showActivityIcons, "showActivityIcons", defaultValue: false);
		Scribe_Values.Look(ref activityIconScale, "activityIconScale", 0.6f);
		Scribe_Values.Look(ref showStatusIndicators, "showStatusIndicators", defaultValue: true);
		Scribe_Values.Look(ref showSquadBadges, "showSquadBadges", defaultValue: true);
		Scribe_Values.Look(ref showSpeechBubbles, "showSpeechBubbles", defaultValue: false);
		Scribe_Values.Look(ref barTopStat, "barTopStat", "__health__");
		Scribe_Values.Look(ref barBottomStat, "barBottomStat", "__mood__");
		Scribe_Values.Look(ref weaponMode, "weaponMode", 1);
		Scribe_Values.Look(ref weaponPrimaryOnly, "weaponPrimaryOnly", defaultValue: false);
		Scribe_Values.Look(ref pulseOnDamage, "pulseOnDamage", defaultValue: true);
		Scribe_Values.Look(ref warbandHideSelected, "warbandHideSelected", defaultValue: true);
		Scribe_Values.Look(ref warbandHidePerspective, "warbandHidePerspective", defaultValue: true);
		activityIconScale = RcbGuard.Sane(activityIconScale, 0.6f);
	}
}
