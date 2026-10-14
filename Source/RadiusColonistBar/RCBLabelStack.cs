using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RCBLabelStack
{
	private const int LineFont = 1;

	private const int LineWeight = 0;

	private static readonly Color RoyalCol = RcbPalette.TextDim;

	private static readonly Color JobCol = RcbPalette.TextDim;

	private static readonly Color RoleCol = RcbPalette.TextDim;

	private static readonly Color TaskCol = RcbPalette.TextDim;

	public static Color RoyalColor => RoyalCol;

	public static float DrawCentered(Pawn p, RadiusColonistBarSettings s, ViewStyle vs, float x, float y, float w, float linePad, float lineGap)
	{
		PawnUiCache.Snap snap = PawnUiCache.Get(p);
		if (s.showName && !string.IsNullOrEmpty(snap.name))
		{
			float num = Mathf.Ceil(RCBLabel.LineH(1));
			RCBLabel.DrawLine(new Rect(x, y, w, num + linePad), snap.name, snap.nameColor, 1, 0, TextAnchor.UpperCenter);
			y += num + lineGap;
		}
		if (s.showRoyalTitle && !string.IsNullOrEmpty(snap.royalTitle))
		{
			float num2 = Mathf.Ceil(RCBLabel.LineH(1));
			RCBLabel.DrawLine(new Rect(x, y, w, num2 + linePad), RCBLabel.Truncate(snap.royalTitle, w, 1, 0), RoyalCol, 1, 0, TextAnchor.UpperCenter);
			y += num2 + lineGap;
		}
		if (s.showJobTitle && !string.IsNullOrEmpty(snap.jobTitle))
		{
			float num3 = Mathf.Ceil(RCBLabel.LineH(1));
			RCBLabel.DrawLine(new Rect(x, y, w, num3 + linePad), RCBLabel.Truncate(snap.jobTitle, w, 1, 0), JobCol, 1, 0, TextAnchor.UpperCenter);
			y += num3 + lineGap;
		}
		if (s.showIdeoRole && !string.IsNullOrEmpty(snap.role))
		{
			float num4 = Mathf.Ceil(RCBLabel.LineH(1));
			RCBLabel.DrawLine(new Rect(x, y, w, num4 + linePad), RCBLabel.Truncate(snap.role, w, 1, 0), RoleCol, 1, 0, TextAnchor.UpperCenter);
			y += num4 + lineGap;
		}
		if (s.showJobLabel && !string.IsNullOrEmpty(snap.job))
		{
			float num5 = Mathf.Ceil(RCBLabel.LineH(1));
			RCBLabel.DrawFitted(new Rect(x, y, w, num5 + linePad), snap.job, TaskCol, 1, 0, TextAnchor.UpperCenter);
			y += num5 + lineGap;
		}
		GUI.color = Color.white;
		return y;
	}
}
