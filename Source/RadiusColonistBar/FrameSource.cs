using System.Collections.Generic;

namespace RadiusColonistBar;

public static class FrameSource
{
	public const int PsycasterPath = 0;

	public const int Psylink = 1;

	public const int IdeoRole = 2;

	public const int RoyalTitle = 3;

	public const int Count = 4;

	public static readonly string[] Keys = new string[4] { "RCB_FrameSource_Path", "RCB_FrameSource_Psylink", "RCB_FrameSource_IdeoRole", "RCB_FrameSource_Royal" };

	public static List<int> DefaultOrder()
	{
		return new List<int> { 0, 1, 2, 3 };
	}
}
