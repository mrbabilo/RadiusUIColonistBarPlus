using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class HubLinks
{
	private struct Link
	{
		public int id;

		public HubTarget t;

		public Rect r;
	}

	private static readonly List<Link> links = new List<Link>();

	private static int frame = -1;

	private static void EnsureFrame()
	{
		if (Time.frameCount != frame)
		{
			links.Clear();
			frame = Time.frameCount;
		}
	}

	public static void Add(Pawn p, HubTarget t, Rect r)
	{
		if (p != null)
		{
			EnsureFrame();
			links.Add(new Link
			{
				id = p.thingIDNumber,
				t = t,
				r = r
			});
		}
	}

	public static bool TryHit(Vector2 pos, int pawnId, out HubTarget t)
	{
		for (int num = links.Count - 1; num >= 0; num--)
		{
			if (links[num].id == pawnId && links[num].r.Contains(pos))
			{
				t = links[num].t;
				return true;
			}
		}
		t = HubTarget.Health;
		return false;
	}
}
