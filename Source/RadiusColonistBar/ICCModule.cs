using RadiusUI.Framework;
using UnityEngine;

namespace RadiusColonistBar;

internal interface ICCModule
{
	string Label { get; }

	RadiusIcon Icon { get; }

	string Section { get; }

	void Refresh(CCContext ctx);

	void Draw(Rect rect, CCContext ctx);
}
