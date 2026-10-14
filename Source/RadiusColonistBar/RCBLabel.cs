using RadiusUI.Framework;
using UnityEngine;
using Verse;

namespace RadiusColonistBar;

internal static class RCBLabel
{
	public static GameFont Font(int f)
	{
		return (GameFont)Mathf.Clamp(f, 0, 2);
	}

	public static FontStyle Style(int st)
	{
		return (FontStyle)Mathf.Clamp(st, 0, 3);
	}

	private static bool Bold(int st)
	{
		st = Mathf.Clamp(st, 0, 3);
		return st == 1 || st == 3;
	}

	private static float Px(int font, int style)
	{
		return RcbFont.Px(Font(font), Bold(style));
	}

	public static string FontLabel(int f)
	{
		return Mathf.Clamp(f, 0, 2) switch
		{
			0 => "RCB_Font_Tiny".Translate(), 
			2 => "RCB_Font_Medium".Translate(), 
			_ => "RCB_Font_Small".Translate(), 
		};
	}

	public static string StyleLabel(int st)
	{
		return Mathf.Clamp(st, 0, 3) switch
		{
			1 => "RCB_Weight_Bold".Translate(), 
			2 => "RCB_Weight_Italic".Translate(), 
			3 => "RCB_Weight_BoldItalic".Translate(), 
			_ => "RCB_Weight_Regular".Translate(), 
		};
	}

	public static float LineH(int font)
	{
		return RadiusFont.LineHAt(Px(font, 0));
	}

	public static float LineH(int font, int style)
	{
		return RadiusFont.LineHAt(Px(font, style), Bold(style));
	}

	public static string Truncate(string text, float width, int font, int style)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return RadiusFont.FitAt(text, width, Px(font, style), Bold(style));
	}

	public static void DrawLine(Rect r, string text, Color col, int font, int style, TextAnchor anchor)
	{
		if (!string.IsNullOrEmpty(text))
		{
			RadiusFont.LabelAt(r, text, Px(font, style), col, anchor, Bold(style));
		}
	}

	public static void DrawFitted(Rect r, string text, Color col, int font, int style, TextAnchor anchor)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		float num = RadiusFont.WidthAt(text, Px(font, style), Bold(style));
		if (num <= r.width || num <= 1f || r.width <= 4f)
		{
			DrawLine(r, text, col, font, style, anchor);
			return;
		}
		float num2 = num + 2f;
		float num3 = r.width / num2;
		if (!RcbGuard.Finite(r) || !RcbGuard.Finite(num) || !(num3 >= 0.0001f))
		{
			string[] obj = new string[9] { "rect=", null, null, null, null, null, null, null, null };
			Rect rect = r;
			obj[1] = rect.ToString();
			obj[2] = " wpx=";
			obj[3] = num.ToString();
			obj[4] = " k=";
			obj[5] = num3.ToString();
			obj[6] = " text=\"";
			obj[7] = text;
			obj[8] = "\"";
			RcbGuard.WarnOnce("RCBLabel.DrawFitted", string.Concat(obj));
			DrawLine(r, text, col, font, style, anchor);
		}
		else
		{
			Vector3 vector = ((anchor == TextAnchor.MiddleLeft) ? new Vector3(r.x, r.center.y, 0f) : new Vector3(r.center.x, r.y, 0f));
			Rect r2 = ((anchor == TextAnchor.MiddleLeft) ? new Rect(r.x, r.center.y - r.height * 0.5f / num3, num2, r.height / num3) : new Rect(r.center.x - num2 * 0.5f, r.y, num2, r.height / num3));
			Matrix4x4 matrix = GUI.matrix;
			GUI.matrix = matrix * Matrix4x4.TRS(vector, Quaternion.identity, Vector3.one) * Matrix4x4.Scale(new Vector3(num3, num3, 1f)) * Matrix4x4.TRS(-vector, Quaternion.identity, Vector3.one);
			DrawLine(r2, text, col, font, style, anchor);
			GUI.matrix = matrix;
		}
	}
}
