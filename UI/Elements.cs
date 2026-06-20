using System;
using System.Runtime.CompilerServices;
using Cheat.Utils;
using UnityEngine;

namespace Cheat.UI;

public static class Elements
{
	private struct DropdownResult
	{
		public string ID;

		public int Value;
	}

	private static float _hue = 0f;

	private static float _sat = 1f;

	private static float _val = 1f;

	private static float _alpha = 1f;

	private static string _activePickerId = null;

	private static Color _tempColor;

	private static Rect _pickerRect;

	private static string _activeDropdownId = null;

	private static Rect _dropdownRect;

	private static int _lastCloseFrame = -1;

	private static string[] _dropdownOptions;

	private static DropdownResult? _lastDropdownResult;

	private static string _activeBinderId = null;

	public static bool IsDropdownOpen => !string.IsNullOrEmpty(_activeDropdownId);

	public static void Window(Rect rect, string title, Action content, ref bool isDragging, ref Vector2 dragOffset)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Invalid comparison between Unknown and I4
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		Render.DrawShadow(rect);
		Render.DrawGlowBox(rect, Theme.Glow, 15f);
		Render.DrawRoundedBox(rect, Theme.Background);
		float num = 50f;
		Rect val = default(Rect);
		val = new Rect(rect.x, rect.y, rect.width, num);
		if ((int)Event.current.type == 0 && val.Contains(Event.current.mousePosition))
		{
			isDragging = true;
			dragOffset = Event.current.mousePosition - rect.position;
			Event.current.Use();
		}
		else if ((int)Event.current.type == 1)
		{
			isDragging = false;
		}
		Render.DrawLine(new Vector2(rect.x, rect.y + num), new Vector2(rect.x + rect.width, rect.y + num), Theme.Separator, 1f);
		Render.DrawString(new Rect(rect.x + 20f, rect.y, rect.width - 40f, num), title, Theme.Text, center: false, 18, bold: true);
		Render.DrawString(new Rect(rect.x + rect.width - 120f, rect.y, 100f, num), AppVersion.Display, Theme.TextDim, center: true, 12);
		float sidebarWidth = Theme.SidebarWidth;
		Rect val2 = default(Rect);
		val2 = new Rect(rect.x, rect.y + num, sidebarWidth, rect.height - num);
		Render.DrawLine(new Vector2(rect.x + sidebarWidth, rect.y + num), new Vector2(rect.x + sidebarWidth, rect.y + rect.height), Theme.Separator, 1f);
	}

	public static bool TextField(string label, ref string text, float x, float y, float width = -1f)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		float num = ((width > 0f) ? width : Theme.PanelWidth);
		Rect rect = default(Rect);
		rect = new Rect(x, y, num, Theme.ItemHeight);
		bool active = rect.Contains(Event.current.mousePosition);
		string text2 = "tf_" + label;
		float animationValue = UIState.GetAnimationValue(text2 + "_hover", active, 5f);
		Color color = Color.Lerp(Theme.ItemBackground, Theme.ContentPanelBackground, animationValue * 0.5f);
		Render.DrawRoundedBox(rect, color);
		Render.DrawString(new Rect(x + 15f, y, 150f, Theme.ItemHeight), label, Theme.Text);
		float num2 = num * 0.4f;
		Rect val = default(Rect);
		val = new Rect(x + num - num2 - 10f, y + 5f, num2, Theme.ItemHeight - 10f);
		string text3 = GUI.TextField(val, text, 25);
		if (text3 != text)
		{
			text = text3;
			return true;
		}
		return false;
	}

	public static bool Toggle(string text, ref bool value, float x, float y, ref KeyCode key, float width = -1f)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		float num = ((width > 0f) ? width : Theme.PanelWidth);
		Rect rect = default(Rect);
		rect = new Rect(x, y, num, Theme.ItemHeight);
		bool flag = rect.Contains(Event.current.mousePosition);
		string text2 = "toggle_" + text;
		float animationValue = UIState.GetAnimationValue(text2 + "_hover", flag, 5f);
		float animationValue2 = UIState.GetAnimationValue(text2 + "_active", value, 8f);
		if (flag)
		{
			Render.DrawRoundedBox(rect, Color.Lerp(Color.clear, Theme.ItemHover, animationValue));
		}
		Render.DrawString(new Rect(x + 10f, y, num - 80f, Theme.ItemHeight), text, value ? Color.white : Theme.Text);
		float num2 = 44f;
		float num3 = 24f;
		float num4 = x + num - num2 - 10f;
		float num5 = y + (Theme.ItemHeight - num3) / 2f;
		float num6 = 50f;
		float x2 = x + num - num6 - 5f;
		num4 -= num6 + 5f;
		HotkeyBinderSmall(text, ref key, x2, y + (Theme.ItemHeight - 20f) / 2f);
		Color color = Color.Lerp(new Color(0.2f, 0.2f, 0.2f), Theme.Accent, animationValue2);
		Render.DrawRoundedBox(new Rect(num4, num5, num2, num3), color);
		float num7 = 18f;
		float num8 = (num3 - num7) / 2f;
		float num9 = Mathf.Lerp(num4 + num8, num4 + num2 - num7 - num8, animationValue2);
		Render.DrawCircle(new Vector2(num9 + num7 / 2f, num5 + num3 / 2f), num7 / 2f, Color.white);
		int num10;
		if (key != KeyCode.None || _activeBinderId == text)
		{
			Rect val = new Rect(x + num - 70f, y, 70f, Theme.ItemHeight);
			num10 = (val.Contains(Event.current.mousePosition) ? 1 : 0);
		}
		else
		{
			num10 = 0;
		}
		bool flag2 = (byte)num10 != 0;
		if (flag && !flag2 && (int)Event.current.type == 0 && Event.current.button == 0)
		{
			value = !value;
			result = true;
		}
		return result;
	}

	public static bool Toggle(string text, ref bool value, float x, float y, float width = -1f)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		KeyCode key = (KeyCode)0;
		return Toggle(text, ref value, x, y, ref key, width);
	}

	public static bool Slider(string text, ref int value, int min, int max, float x, float y, float width = -1f)
	{
		float value2 = value;
		bool result = Slider(text, ref value2, min, max, x, y, width);
		value = Mathf.RoundToInt(value2);
		return result;
	}

	public static bool Slider(string text, ref float value, float min, float max, float x, float y, float width = -1f)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Invalid comparison between Unknown and I4
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		float num = ((width > 0f) ? width : Theme.PanelWidth);
		Rect rect = default(Rect);
		rect = new Rect(x, y, num, Theme.ItemHeight);
		bool flag = rect.Contains(Event.current.mousePosition);
		string text2 = "slider_" + text;
		float animationValue = UIState.GetAnimationValue(text2 + "_hover", flag, 5f);
		if (flag)
		{
			Render.DrawRoundedBox(rect, Color.Lerp(Color.clear, Theme.ItemHover, animationValue));
		}
		Render.DrawString(new Rect(x + 10f, y + 2f, num - 60f, 20f), text, Theme.Text, center: false, 13);
		string text3 = ((max > 10f) ? value.ToString("F0") : value.ToString("F2"));
		if (text.Contains("Speed") || text.Contains("Multiplier"))
		{
			text3 += "x";
		}
		Render.DrawString(new Rect(x + num - 60f, y + 2f, 50f, 20f), text3, Theme.Accent, center: true, 12, bold: true);
		float num2 = 10f;
		float num3 = num - num2 * 2f;
		float num4 = 6f;
		float num5 = x + num2;
		float num6 = y + 25f;
		Render.DrawRoundedBox(new Rect(num5, num6, num3, num4), new Color(0.15f, 0.15f, 0.15f));
		float num7 = Mathf.Clamp01((value - min) / (max - min));
		float num8 = num7 * num3;
		if (num8 > 0f)
		{
			Render.DrawRoundedBox(new Rect(num5, num6, num8, num4), Theme.Accent);
		}
		float num9 = 6f;
		float num10 = 10f;
		float num11 = num5 + num8 - num9 / 2f;
		float num12 = num6 + (num4 - num10) / 2f;
		Render.DrawRoundedBox(new Rect(num11, num12, num9, num10), Color.white);
		Rect val = default(Rect);
		val = new Rect(num5 - 5f, y + 15f, num3 + 10f, 20f);
		if (val.Contains(Event.current.mousePosition) && ((int)Event.current.type == 0 || (int)Event.current.type == 3) && Event.current.button == 0)
		{
			float x2 = Event.current.mousePosition.x;
			float num13 = Mathf.Clamp01((x2 - num5) / num3);
			float num14 = min + num13 * (max - min);
			if (Math.Abs(num14 - value) > 0.001f)
			{
				value = num14;
				result = true;
			}
		}
		return result;
	}

	public static bool Button(string text, float x, float y, float width = -1f)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		float num = ((width > 0f) ? width : Theme.PanelWidth);
		Rect rect = default(Rect);
		rect = new Rect(x, y, num, Theme.ItemHeight);
		bool flag = rect.Contains(Event.current.mousePosition);
		string text2 = "btn_" + text;
		float animationValue = UIState.GetAnimationValue(text2 + "_hover", flag, 10f);
		Color itemBackground = Theme.ItemBackground;
		Color itemHover = Theme.ItemHover;
		Color accent = Theme.Accent;
		bool flag2 = flag && (int)Event.current.type == 0 && Event.current.button == 0;
		Color val = Color.Lerp(itemBackground, itemHover, animationValue);
		if (flag2)
		{
			val = Color.Lerp(val, accent, 0.5f);
		}
		float num2 = Mathf.Lerp(1f, 1.02f, animationValue);
		Matrix4x4 matrix = GUI.matrix;
		GUIUtility.ScaleAroundPivot(new Vector2(num2, num2), rect.center);
		Render.DrawRoundedBox(rect, val);
		Render.DrawString(rect, text, Theme.Text, center: true);
		GUI.matrix = matrix;
		if (flag2)
		{
			Event.current.Use();
			return true;
		}
		return false;
	}

	public static Vector2 BeginCustomScrollView(Rect position, Vector2 scrollPosition, Rect viewRect)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Invalid comparison between Unknown and I4
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		float height = viewRect.height;
		float height2 = position.height;
		float num = 6f;
		float num2 = 4f;
		if (height > height2)
		{
			Rect position2 = default(Rect);
			position2 = new Rect(position.x + position.width - num - num2, position.y, num, position.height);
			scrollPosition.y = Render.DrawCustomScrollbar(position2, scrollPosition.y, height2, 0f, height - height2, vertical: true);
		}
		if ((int)Event.current.type == 6 && position.Contains(Event.current.mousePosition))
		{
			scrollPosition.y += Event.current.delta.y * 20f;
			scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0f, Math.Max(0f, height - height2));
			Event.current.Use();
		}
		GUI.BeginGroup(position);
		GUI.BeginGroup(new Rect(0f, 0f - scrollPosition.y, viewRect.width, viewRect.height));
		return scrollPosition;
	}

	public static void EndCustomScrollView()
	{
		GUI.EndGroup();
		GUI.EndGroup();
	}

	public static bool TabButton(string icon, bool active, float x, float y, int index = 0)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		float num = Theme.SidebarWidth - 20f;
		float num2 = 40f;
		Rect rect = default(Rect);
		rect = new Rect(x + 10f, y, num, num2);
		bool flag = rect.Contains(Event.current.mousePosition);
		string text = "tab_" + icon;
		float animationValue = UIState.GetAnimationValue(text + "_hover", flag, 10f);
		float animationValue2 = UIState.GetAnimationValue(text + "_active", active, 10f);
		float easedAnimationValue = UIState.GetEasedAnimationValue(text + "_entrance", active: true, 2f, UIState.Easing.EaseOutBack);
		float num3 = (1f - easedAnimationValue) * (-50f - (float)index * 10f);
		rect.x = rect.x + num3;
		if (active || animationValue > 0.01f)
		{
			Color val = (active ? new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.2f) : new Color(1f, 1f, 1f, 0.05f));
			Color color = Color.Lerp(Color.clear, val, Mathf.Max(animationValue2, animationValue));
			Render.DrawRoundedBox(rect, color);
		}
		Color color2 = Color.Lerp(Theme.TextDim, active ? Theme.IconActive : Theme.IconHover, Mathf.Max(animationValue, animationValue2));
		if (!active && !flag)
		{
			color2 = Theme.IconInactive;
		}
		Rect rect2 = default(Rect);
		rect2 = new Rect(rect.x + 10f, rect.y + 10f, 20f, 20f);
		Render.DrawIcon(rect2, icon, color2);
		Render.DrawString(new Rect(rect.x + 40f, rect.y, num - 40f, num2), icon, color2, center: false, 14, active);
		return flag && (int)Event.current.type == 0 && Event.current.button == 0;
	}

	public static bool SidebarButton(string text, string icon, bool active, bool expanded, bool hasSubTabs, float x, float y, float width)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		float num = 40f;
		float num2 = width - 20f;
		Rect rect = default(Rect);
		rect = new Rect(x + 10f, y, num2, num);
		bool flag = rect.Contains(Event.current.mousePosition);
		string text2 = "sidebar_" + text;
		float animationValue = UIState.GetAnimationValue(text2 + "_hover", flag, 10f);
		float animationValue2 = UIState.GetAnimationValue(text2 + "_active", active, 10f);
		if (active || animationValue > 0.01f)
		{
			Color val = (active ? new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.2f) : new Color(1f, 1f, 1f, 0.05f));
			Color color = Color.Lerp(Color.clear, val, Mathf.Max(animationValue2, animationValue));
			Render.DrawRoundedBox(rect, color);
		}
		Color color2 = Color.Lerp(Theme.TextDim, active ? Theme.IconActive : Theme.IconHover, Mathf.Max(animationValue, animationValue2));
		if (!active && !flag)
		{
			color2 = Theme.IconInactive;
		}
		Render.DrawString(new Rect(rect.x + 10f, rect.y, 30f, num), icon, color2, center: false, 16);
		Render.DrawString(new Rect(rect.x + 40f, rect.y, num2 - 60f, num), text, color2, center: false, 14, active);
		if (hasSubTabs)
		{
			string text3 = (expanded ? "▼" : "▶");
			Render.DrawString(new Rect(rect.x + num2 - 20f, rect.y, 20f, num), text3, color2, center: true, 10);
		}
		return flag && (int)Event.current.type == 0 && Event.current.button == 0;
	}

	public static bool SubTabButton(string text, bool active, float x, float y, float width)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		float num = 30f;
		float num2 = width - 40f;
		Rect rect = default(Rect);
		rect = new Rect(x + 30f, y, num2, num);
		bool flag = rect.Contains(Event.current.mousePosition);
		string text2 = "subtab_" + text;
		float animationValue = UIState.GetAnimationValue(text2 + "_hover", flag, 10f);
		if (active || animationValue > 0.01f)
		{
			Color color = (active ? new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.15f) : new Color(1f, 1f, 1f, 0.03f));
			Render.DrawRoundedBox(rect, color);
		}
		Color color2 = (active ? Theme.Accent : Theme.TextDim);
		if (flag && !active)
		{
			color2 = Theme.Text;
		}
		Render.DrawString(new Rect(rect.x + 10f, rect.y, num2 - 10f, num), text, color2, center: false, 13, active);
		return flag && (int)Event.current.type == 0 && Event.current.button == 0;
	}

	public static bool ColorPicker(string text, ref Color color, float x, float y, float width = -1f)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		float num = ((width > 0f) ? width : Theme.PanelWidth);
		Rect rect = default(Rect);
		rect = new Rect(x, y, num, Theme.ItemHeight);
		bool active = rect.Contains(Event.current.mousePosition);
		string text2 = "cp_" + text;
		float animationValue = UIState.GetAnimationValue(text2 + "_hover", active, 5f);
		Color color2 = Color.Lerp(Theme.ItemBackground, Theme.ContentPanelBackground, animationValue * 0.5f);
		Render.DrawRoundedBox(rect, color2);
		Render.DrawString(new Rect(x + 15f, y, 200f, Theme.ItemHeight), text, Theme.Text);
		float num2 = 40f;
		float num3 = 20f;
		float num4 = x + num - num2 - 15f;
		float num5 = y + (Theme.ItemHeight - num3) / 2f;
		Rect rect2 = default(Rect);
		rect2 = new Rect(num4, num5, num2, num3);
		Render.DrawBox(rect2, color);
		Render.DrawBox(new Rect(rect2.x - 1f, rect2.y - 1f, rect2.width + 2f, rect2.height + 2f), new Color(0f, 0f, 0f, 0.5f));
		if (rect2.Contains(Event.current.mousePosition) && (int)Event.current.type == 0 && Event.current.button == 0)
		{
			if (_activePickerId == text)
			{
				_activePickerId = null;
			}
			else
			{
				_activePickerId = text;
				_tempColor = color;
				Color.RGBToHSV(color, out _hue, out _sat, out _val);
				_alpha = color.a;
				Vector2 val = GUIUtility.GUIToScreenPoint(new Vector2(num4 + num2 + 10f, num5));
				_pickerRect = new Rect(val.x, val.y, 200f, 220f);
			}
			Event.current.Use();
		}
		if (_activePickerId == text && color != _tempColor)
		{
			color = _tempColor;
			result = true;
		}
		return result;
	}

	public static void DrawColorPickerPopup()
	{
		if (string.IsNullOrEmpty(_activePickerId))
		{
			return;
		}
		RenderQueue.Add(delegate
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0151: Unknown result type (might be due to invalid IL or missing references)
			//IL_0156: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_016b: Unknown result type (might be due to invalid IL or missing references)
			//IL_016f: Unknown result type (might be due to invalid IL or missing references)
			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_026b: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_027e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d9: Invalid comparison between Unknown and I4
			//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f9: Invalid comparison between Unknown and I4
			//IL_028a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0290: Invalid comparison between Unknown and I4
			//IL_0205: Unknown result type (might be due to invalid IL or missing references)
			//IL_020c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0214: Unknown result type (might be due to invalid IL or missing references)
			//IL_021b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0398: Unknown result type (might be due to invalid IL or missing references)
			//IL_039e: Invalid comparison between Unknown and I4
			//IL_031e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0324: Invalid comparison between Unknown and I4
			//IL_029c: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0404: Unknown result type (might be due to invalid IL or missing references)
			//IL_0409: Unknown result type (might be due to invalid IL or missing references)
			//IL_040e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0410: Unknown result type (might be due to invalid IL or missing references)
			//IL_0417: Unknown result type (might be due to invalid IL or missing references)
			//IL_0445: Unknown result type (might be due to invalid IL or missing references)
			//IL_044c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0457: Unknown result type (might be due to invalid IL or missing references)
			//IL_045e: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_04e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0341: Unknown result type (might be due to invalid IL or missing references)
			//IL_034d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0353: Invalid comparison between Unknown and I4
			//IL_035f: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val = GUIUtility.ScreenToGUIPoint(_pickerRect.position);
			Rect rect = default(Rect);
			rect = new Rect(val.x, val.y, _pickerRect.width, _pickerRect.height);
			Render.DrawShadow(rect);
			Render.DrawRoundedBox(rect, Theme.ItemBackground);
			float x = rect.x;
			float y = rect.y;
			float width = rect.width;
			float num = 120f;
			float num2 = x + (width - num) / 2f;
			float num3 = y + 10f;
			Rect val2 = default(Rect);
			val2 = new Rect(num2, num3, num, num);
			GUI.DrawTexture(val2, (Texture)(object)Theme.HueTexture);
			float num4 = num * 0.5f;
			float num5 = num2 + (num - num4) / 2f;
			float num6 = num3 + (num - num4) / 2f;
			Rect rect2 = default(Rect);
			rect2 = new Rect(num5, num6, num4, num4);
			Color color = Color.HSVToRGB(_hue, 1f, 1f);
			Render.DrawBox(rect2, color);
			float num7 = num3 + num + 15f;
			Rect rect3 = default(Rect);
			rect3 = new Rect(x + 20f, num7, width - 40f, 10f);
			Render.DrawBox(rect3, Color.white);
			Render.DrawBox(new Rect(rect3.x, rect3.y, rect3.width * _alpha, rect3.height), _tempColor);
			Vector2 mousePosition = Event.current.mousePosition;
			bool flag = rect.Contains(mousePosition);
			if ((int)Event.current.type == 0 && !flag)
			{
				_activePickerId = null;
			}
			else
			{
				if (val2.Contains(mousePosition) && !rect2.Contains(mousePosition) && ((int)Event.current.type == 0 || (int)Event.current.type == 3))
				{
					Vector2 val3 = default(Vector2);
					val3 = new Vector2(num2 + num / 2f, num3 + num / 2f);
					float num8 = Mathf.Atan2(mousePosition.y - val3.y, mousePosition.x - val3.x) * 57.29578f;
					if (num8 < 0f)
					{
						num8 += 360f;
					}
					_hue = num8 / 360f;
					UpdateTempColor();
					Event.current.Use();
				}
				if (rect2.Contains(mousePosition) && ((int)Event.current.type == 0 || (int)Event.current.type == 3))
				{
					_sat = Mathf.Clamp01((mousePosition.x - num5) / num4);
					_val = 1f - Mathf.Clamp01((mousePosition.y - num6) / num4);
					UpdateTempColor();
					Event.current.Use();
				}
				if (rect3.Contains(mousePosition) || ((int)Event.current.type == 3 && GUIUtility.hotControl == _activePickerId.GetHashCode()))
				{
					if ((int)Event.current.type == 0)
					{
						GUIUtility.hotControl = _activePickerId.GetHashCode();
					}
					if ((int)Event.current.type == 0 || (int)Event.current.type == 3)
					{
						_alpha = Mathf.Clamp01((mousePosition.x - rect3.x) / rect3.width);
						UpdateTempColor();
						Event.current.Use();
					}
				}
				if ((int)Event.current.type == 1)
				{
					GUIUtility.hotControl = 0;
				}
				float num9 = _hue * 360f * ((float)Math.PI / 180f);
				float num10 = num / 2f - 5f;
				Vector2 val4 = default(Vector2);
				val4 = new Vector2(num2 + num / 2f, num3 + num / 2f);
				Vector2 center = val4 + new Vector2(Mathf.Cos(num9), Mathf.Sin(num9)) * num10;
				Render.DrawCircle(center, 4f, Color.white);
				Vector2 center2 = default(Vector2);
				center2 = new Vector2(num5 + _sat * num4, num6 + (1f - _val) * num4);
				Render.DrawCircle(center2, 4f, Color.black);
				Render.DrawCircle(center2, 2f, Color.white);
				float num11 = rect3.x + _alpha * rect3.width;
				Render.DrawBox(new Rect(num11 - 2f, rect3.y - 2f, 4f, rect3.height + 4f), Color.black);
				Render.DrawString(new Rect(x, num7 + 20f, width, 20f), "#" + ColorUtility.ToHtmlStringRGBA(_tempColor), Theme.Text, center: true, 12);
			}
		});
	}

	private static void UpdateTempColor()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		Color tempColor = Color.HSVToRGB(_hue, _sat, _val);
		tempColor.a = _alpha;
		_tempColor = tempColor;
	}

	public static bool Dropdown(string text, ref int selectedIndex, string[] options, float x, float y, float width = -1f, Vector2? manualScreenPos = null)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		float num = ((width > 0f) ? width : Theme.PanelWidth);
		Rect rect = default(Rect);
		rect = new Rect(x, y, num, Theme.ItemHeight);
		bool flag = rect.Contains(Event.current.mousePosition);
		string text2 = "dd_" + text;
		float animationValue = UIState.GetAnimationValue(text2 + "_hover", flag, 5f);
		Color color = Color.Lerp(Theme.ItemBackground, Theme.ContentPanelBackground, animationValue * 0.5f);
		Render.DrawRoundedBox(rect, color);
		Render.DrawString(new Rect(x + 15f, y, 150f, Theme.ItemHeight), text, Theme.Text);
		string text3 = ((selectedIndex >= 0 && selectedIndex < options.Length) ? options[selectedIndex] : "Select...");
		Render.DrawString(new Rect(x + num - 120f, y, 100f, Theme.ItemHeight), text3, Theme.Accent, center: true, 12);
		Render.DrawString(new Rect(x + num - 25f, y, 20f, Theme.ItemHeight), "▼", Theme.TextDim, center: true, 10);
		if (flag && (int)Event.current.type == 0 && Event.current.button == 0)
		{
			if (Time.frameCount > _lastCloseFrame + 5)
			{
				if (_activeDropdownId == text)
				{
					_activeDropdownId = null;
					_dropdownOptions = null;
					_lastCloseFrame = Time.frameCount;
				}
				else
				{
					_activeDropdownId = text;
					_dropdownOptions = options;
					if (manualScreenPos.HasValue)
					{
						_dropdownRect = new Rect(manualScreenPos.Value.x, manualScreenPos.Value.y, num, Mathf.Min((float)options.Length * 25f + 10f, 200f));
					}
					else
					{
						Vector2 val = GUIUtility.GUIToScreenPoint(new Vector2(x, y + Theme.ItemHeight));
						_dropdownRect = new Rect(val.x, val.y + 5f, num, Mathf.Min((float)options.Length * 25f + 10f, 200f));
					}
				}
			}
			Event.current.Use();
		}
		if (_lastDropdownResult.HasValue && _lastDropdownResult.Value.ID == text)
		{
			if (selectedIndex != _lastDropdownResult.Value.Value)
			{
				selectedIndex = _lastDropdownResult.Value.Value;
				result = true;
			}
			_lastDropdownResult = null;
		}
		return result;
	}

	public static void DrawActiveDropdownPopup()
	{
		if (string.IsNullOrEmpty(_activeDropdownId) || _dropdownOptions == null)
		{
			return;
		}
		RenderQueue.Add(delegate
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
			//IL_021f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_022d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f4: Invalid comparison between Unknown and I4
			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0201: Invalid comparison between Unknown and I4
			if (_dropdownOptions != null && _dropdownOptions.Length != 0)
			{
				Vector2 val = GUIUtility.ScreenToGUIPoint(_dropdownRect.position);
				Rect rect = default(Rect);
				rect = new Rect(val.x, val.y, _dropdownRect.width, _dropdownRect.height);
				Render.DrawShadow(rect);
				Render.DrawRoundedBox(rect, Theme.ItemBackground);
				float num = 25f;
				float num2 = rect.y + 5f;
				bool flag = false;
				Rect rect2 = default(Rect);
				for (int i = 0; i < _dropdownOptions.Length; i++)
				{
					rect2 = new Rect(rect.x, num2, rect.width, num);
					bool flag2 = rect2.Contains(Event.current.mousePosition);
					if (flag2)
					{
						Render.DrawBox(rect2, Theme.ContentPanelBackground);
					}
					string text = _dropdownOptions[i] ?? "";
					Render.DrawString(new Rect(rect2.x + 15f, rect2.y, rect2.width - 30f, rect2.height), text, flag2 ? Theme.Accent : Theme.Text, center: false, 12);
					if (flag2 && (int)Event.current.type == 0 && Event.current.button == 0)
					{
						_lastDropdownResult = new DropdownResult
						{
							ID = _activeDropdownId,
							Value = i
						};
						_activeDropdownId = null;
						_dropdownOptions = null;
						_lastCloseFrame = Time.frameCount;
						flag = true;
						Event.current.Use();
						return;
					}
					num2 += num;
				}
				if (!flag && rect.Contains(Event.current.mousePosition) && ((int)Event.current.type == 0 || (int)Event.current.type == 1 || (int)Event.current.type == 6))
				{
					Event.current.Use();
				}
				if ((int)Event.current.type == 0 && !rect.Contains(Event.current.mousePosition))
				{
					_activeDropdownId = null;
					_dropdownOptions = null;
					_lastCloseFrame = Time.frameCount;
				}
			}
		});
	}

	public static void HotkeyBinder(string text, ref KeyCode key, float x, float y)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Invalid comparison between Unknown and I4
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Invalid comparison between Unknown and I4
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Expected I4, but got Unknown
		Rect rect = default(Rect);
		rect = new Rect(x, y, Theme.PanelWidth, Theme.ItemHeight);
		bool flag = rect.Contains(Event.current.mousePosition);
		string text2 = "binder_" + text;
		float animationValue = UIState.GetAnimationValue(text2 + "_hover", flag, 5f);
		Color color = Color.Lerp(Theme.ItemBackground, Theme.ContentPanelBackground, animationValue * 0.5f);
		Render.DrawRoundedBox(rect, color);
		Render.DrawString(new Rect(x + 15f, y, 200f, Theme.ItemHeight), text, Theme.Text);
		bool flag2 = _activeBinderId == text;
		string text3 = (flag2 ? "..." : (((int)key == 0) ? "..." : key.ToString()));
		Color color2 = (flag2 ? Theme.Accent : (((int)key == 0) ? Theme.TextDim : Color.white));
		float num = 120f;
		float num2 = 24f;
		float num3 = x + Theme.PanelWidth - num - 15f;
		float num4 = y + (Theme.ItemHeight - num2) / 2f;
		Rect rect2 = default(Rect);
		rect2 = new Rect(num3, num4, num, num2);
		Render.DrawRoundedBox(rect2, flag2 ? Theme.ContentPanelBackground : Theme.ItemBackground);
		Render.DrawString(rect2, text3, color2, center: true, 12);
		if (flag && (int)Event.current.type == 0 && Event.current.button == 0)
		{
			if (_activeBinderId == text)
			{
				_activeBinderId = null;
			}
			else
			{
				_activeBinderId = text;
			}
			Event.current.Use();
		}
		Event currentEvent = Event.current;
		if (flag2 && currentEvent != null && currentEvent.type == EventType.KeyDown)
		{
			if ((int)currentEvent.keyCode != 27)
			{
				key = (KeyCode)(int)currentEvent.keyCode;
			}
			_activeBinderId = null;
			currentEvent.Use();
		}
		else if (flag2 && currentEvent != null && currentEvent.type == EventType.MouseDown)
		{
			if (!rect2.Contains(currentEvent.mousePosition))
			{
				_activeBinderId = null;
			}
			else
			{
				key = KeyCode.Mouse0 + currentEvent.button;
				_activeBinderId = null;
			}
			currentEvent.Use();
		}
		if (flag2 && (int)currentEvent.type == 0 && !flag)
		{
			_activeBinderId = null;
		}
	}

	private static void HotkeyBinderSmall(string text, ref KeyCode key, float x, float y)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Invalid comparison between Unknown and I4
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Invalid comparison between Unknown and I4
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected I4, but got Unknown
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		bool flag = _activeBinderId == text;
		string text2 = (flag ? "..." : (((int)key == 0) ? "Bind" : key.ToString()));
		Color color = (flag ? Theme.Accent : (((int)key == 0) ? Theme.TextDim : Color.white));
		Rect rect = default(Rect);
		rect = new Rect(x, y, 60f, 20f);
		Render.DrawRoundedBox(rect, flag ? Theme.ContentPanelBackground : Theme.ItemBackground);
		Render.DrawString(rect, text2, color, center: true, 10);
		if (rect.Contains(Event.current.mousePosition) && (int)Event.current.type == 0 && Event.current.button == 0)
		{
			if (_activeBinderId == text)
			{
				_activeBinderId = null;
			}
			else
			{
				_activeBinderId = text;
			}
			Event.current.Use();
		}
		Event currentEvent = Event.current;
		if (flag && currentEvent != null && currentEvent.type == EventType.KeyDown)
		{
			if ((int)currentEvent.keyCode == 27)
			{
				key = (KeyCode)0;
			}
			else
			{
				key = (KeyCode)(int)currentEvent.keyCode;
			}
			_activeBinderId = null;
			currentEvent.Use();
		}
		else if (flag && currentEvent != null && currentEvent.type == EventType.MouseDown)
		{
			if (!rect.Contains(currentEvent.mousePosition))
			{
				_activeBinderId = null;
			}
			else
			{
				key = KeyCode.Mouse0 + currentEvent.button;
				_activeBinderId = null;
			}
			currentEvent.Use();
		}
		if (flag && (int)currentEvent.type == 0 && !rect.Contains(currentEvent.mousePosition))
		{
			_activeBinderId = null;
		}
	}
}
