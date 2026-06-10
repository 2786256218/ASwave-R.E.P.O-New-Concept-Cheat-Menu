using UnityEngine;

namespace Cheat.UI;

public static class BootRenderer
{
	public static void Draw()
	{
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		if (!UIState.IsBooting)
		{
			return;
		}
		float num = Screen.width;
		float num2 = Screen.height;
		Vector2 val = default(Vector2);
		val = new Vector2(num / 2f, num2 / 2f);
		if (UIState.LogoScale <= 0.5f)
		{
			float num3 = UIState.LogoScale / 0.5f;
			float windowWidth = Theme.WindowWidth;
			float num4 = Mathf.Lerp(0f, windowWidth, num3);
			Render.DrawBox(new Rect(val.x - num4 / 2f, val.y - 1f, num4, 2f), Theme.Accent);
			Color accent = Theme.Accent;
			accent.a = 0.3f;
			Render.DrawBox(new Rect(val.x - num4 / 2f, val.y - 3f, num4, 6f), accent);
			return;
		}
		float num5 = (UIState.LogoScale - 0.5f) / 0.5f;
		float windowWidth2 = Theme.WindowWidth;
		float windowHeight = Theme.WindowHeight;
		float num6 = Mathf.Lerp(2f, windowHeight, num5);
		Color accent2 = Theme.Accent;
		accent2.a = Mathf.Clamp01(1f - num5);
		float num7 = num6 / 2f;
		float num8 = windowWidth2 / 2f;
		Render.DrawBox(new Rect(val.x - num8, val.y - num7, windowWidth2, 2f), accent2);
		Render.DrawBox(new Rect(val.x - num8, val.y + num7, windowWidth2, 2f), accent2);
		Render.DrawBox(new Rect(val.x - num8, val.y - num7, 2f, num6), accent2);
		Render.DrawBox(new Rect(val.x + num8, val.y - num7, 2f, num6), accent2);
		Color background = Theme.Background;
		background.a = 0.8f * num5;
		Render.DrawRoundedBox(new Rect(val.x - num8, val.y - num7, windowWidth2, num6), background);
		if (num5 < 0.9f)
		{
			string text = ((num5 < 0.5f) ? "INJECTING..." : "INITIALIZING INTERFACE...");
			Color text2 = Theme.Text;
			text2.a = 1f - num5;
			Render.DrawString(new Rect(val.x - 100f, val.y - 10f, 200f, 20f), text, text2, center: true, 14, bold: true);
		}
	}
}
