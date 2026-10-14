using System.Collections.Generic;

namespace RadiusColonistBar;

internal static class RcbUtil
{
	private static readonly List<int> pruneTmp = new List<int>();

	public static void PruneById<T>(Dictionary<int, T> d, HashSet<int> live)
	{
		if (d.Count == 0)
		{
			return;
		}
		pruneTmp.Clear();
		foreach (int key in d.Keys)
		{
			if (!live.Contains(key))
			{
				pruneTmp.Add(key);
			}
		}
		for (int i = 0; i < pruneTmp.Count; i++)
		{
			d.Remove(pruneTmp[i]);
		}
	}
}
