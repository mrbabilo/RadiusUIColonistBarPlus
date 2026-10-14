using System.Collections.Generic;

namespace RadiusColonistBar;

internal static class SmallStrings
{
	private static readonly string[] plusStr = Build("+", 1, 12);

	private static readonly Dictionary<int, string> bpmStr = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> intStr = new Dictionary<int, string>();

	private static string[] Build(string prefix, int lo, int hi)
	{
		string[] array = new string[hi - lo + 1];
		for (int i = lo; i <= hi; i++)
		{
			array[i - lo] = prefix + i;
		}
		return array;
	}

	public static string Plus(int n)
	{
		return (n >= 1 && n <= 12) ? plusStr[n - 1] : ("+" + n);
	}

	public static string Int(int n)
	{
		if (!intStr.TryGetValue(n, out var value))
		{
			if (intStr.Count > 512)
			{
				intStr.Clear();
			}
			value = n.ToString();
			intStr[n] = value;
		}
		return value;
	}

	public static string Bpm(int bpm)
	{
		if (!bpmStr.TryGetValue(bpm, out var value))
		{
			if (bpmStr.Count > 256)
			{
				bpmStr.Clear();
			}
			value = bpm + " bpm";
			bpmStr[bpm] = value;
		}
		return value;
	}
}
