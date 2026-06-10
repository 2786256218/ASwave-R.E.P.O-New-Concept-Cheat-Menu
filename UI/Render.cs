using UnityEngine;

namespace Cheat.UI;

public static class Render
{
	private static GUIStyle _stringStyle;

	private static Material _glMaterial;

	private static void EnsureThemeResources()
	{
		if ((UnityEngine.Object)(object)Theme.WhiteTexture == (UnityEngine.Object)null || Theme.RoundedBoxStyle == null || (UnityEngine.Object)(object)Theme.CircleTexture == (UnityEngine.Object)null || (UnityEngine.Object)(object)Theme.ShadowTexture == (UnityEngine.Object)null)
		{
			Theme.Init();
		}
	}

	public static void DrawBox(Rect rect, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		EnsureThemeResources();
		Color color2 = GUI.color;
		GUI.color = color;
		Texture texture = (Texture)(object)Theme.WhiteTexture ?? (Texture)(object)Texture2D.whiteTexture;
		GUI.DrawTexture(rect, texture);
		GUI.color = color2;
	}

	public static void DrawRoundedBox(Rect rect, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		EnsureThemeResources();
		Color color2 = GUI.color;
		GUI.color = color;
		GUI.Box(rect, GUIContent.none, Theme.RoundedBoxStyle);
		GUI.color = color2;
	}

	public static void DrawShadow(Rect rect)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		EnsureThemeResources();
		Color color = GUI.color;
		GUI.color = Theme.Shadow;
		Texture texture = (Texture)(object)Theme.ShadowTexture ?? (Texture)(object)Texture2D.whiteTexture;
		GUI.DrawTexture(new Rect(rect.x - 10f, rect.y - 10f, rect.width + 20f, rect.height + 20f), texture);
		GUI.color = color;
	}

	public static void DrawCircle(Vector2 center, float radius, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		EnsureThemeResources();
		Color color2 = GUI.color;
		GUI.color = color;
		Texture texture = (Texture)(object)Theme.CircleTexture ?? (Texture)(object)Texture2D.whiteTexture;
		GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), texture);
		GUI.color = color2;
	}

	public static Vector2 MeasureString(string text, int fontSize = 14, bool bold = false)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (_stringStyle == null)
		{
			_stringStyle = new GUIStyle(GUI.skin.label);
		}
		_stringStyle.fontSize = fontSize;
		_stringStyle.fontStyle = (FontStyle)(bold ? 1 : 0);
		return _stringStyle.CalcSize(new GUIContent(text));
	}

	public static void DrawString(Rect rect, string text, Color color, bool center = false, int fontSize = 14, bool bold = false)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (_stringStyle == null)
		{
			_stringStyle = new GUIStyle(GUI.skin.label);
		}
		_stringStyle.fontSize = fontSize;
		_stringStyle.fontStyle = (FontStyle)(bold ? 1 : 0);
		_stringStyle.alignment = (TextAnchor)(center ? 4 : 3);
		_stringStyle.normal.textColor = color;
		rect.x = Mathf.Round(rect.x);
		rect.y = Mathf.Round(rect.y);
		GUI.Label(rect, text, _stringStyle);
	}

	public static void DrawStringOutlined(Rect rect, string text, Color color, bool center = false, int fontSize = 14, bool bold = false)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		if (_stringStyle == null)
		{
			_stringStyle = new GUIStyle(GUI.skin.label);
		}
		_stringStyle.fontSize = fontSize;
		_stringStyle.fontStyle = (FontStyle)(bold ? 1 : 0);
		_stringStyle.alignment = (TextAnchor)(center ? 4 : 3);
		rect.x = Mathf.Round(rect.x);
		rect.y = Mathf.Round(rect.y);
		_stringStyle.normal.textColor = Color.black;
		GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, _stringStyle);
		GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, _stringStyle);
		GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, _stringStyle);
		GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text, _stringStyle);
		_stringStyle.normal.textColor = color;
		GUI.Label(rect, text, _stringStyle);
	}

	public static void DrawIcon(Rect rect, string iconName, Color color, Color? tint = null)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		Color color2 = GUI.color;
		GUI.color = tint ?? color;
		float num = rect.x + rect.width / 2f;
		float num2 = rect.y + rect.height / 2f;
		float num3 = Mathf.Min(rect.width, rect.height) * 0.6f;
		Rect val = default(Rect);
		val = new Rect(num - num3 / 2f, num2 - num3 / 2f, num3, num3);
		Texture2D val2 = null;
		switch (iconName)
		{
		case "Loot":
			val2 = Theme.IconLoot;
			break;
		case "Enemies":
			val2 = Theme.IconEnemies;
			break;
		case "Local":
			val2 = Theme.IconLocal;
			break;
		case "Misc":
			val2 = Theme.IconMisc;
			break;
		case "Settings":
			val2 = Theme.IconSettings;
			break;
		}
		if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2);
		}
		else
		{
			GUI.Label(rect, iconName.Substring(0, 1), _stringStyle);
		}
		GUI.color = color2;
	}

	public static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		EnsureThemeResources();
		Color color2 = GUI.color;
		GUI.color = color;
		Vector2 val = end - start;
		if (val.sqrMagnitude <= 0.0001f)
		{
			GUI.color = color2;
			return;
		}
		float num = Mathf.Atan2(val.y, val.x) * 57.29578f;
		int num2 = (int)Mathf.Ceil(width / 2f);
		Matrix4x4 matrix = GUI.matrix;
		GUIUtility.RotateAroundPivot(num, start);
		Texture texture = (Texture)(object)Theme.WhiteTexture ?? (Texture)(object)Texture2D.whiteTexture;
		GUI.DrawTexture(new Rect(start.x, start.y - (float)num2, val.magnitude, width), texture);
		GUI.matrix = matrix;
		GUI.color = color2;
	}

	public static float DrawCustomScrollbar(Rect position, float val, float size, float min, float max, bool vertical)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Expected I4, but got Unknown
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		float num = max - min;
		float num2 = num + size;
		if (num2 <= size)
		{
			return val;
		}
		float num3 = Mathf.Max(20f, size / num2 * (vertical ? position.height : position.width));
		float num4 = (vertical ? position.height : position.width) - num3;
		float num5 = Mathf.Clamp01((val - min) / num);
		float num6 = num5 * num4;
		float num7 = 4f;
		Rect rect = default(Rect);
		if (vertical)
		{
			float num8 = position.x + (position.width - num7) / 2f;
			rect = new Rect(num8, position.y + num6, num7, num3);
		}
		else
		{
			float num9 = position.y + (position.height - num7) / 2f;
			rect = new Rect(position.x + num6, num9, num3, num7);
		}
		int controlID = GUIUtility.GetControlID((FocusType)2);
		Event current = Event.current;
		EventType typeForControl = current.GetTypeForControl(controlID);
		EventType val2 = typeForControl;
		switch ((int)val2)
		{
		case 0:
			if (position.Contains(current.mousePosition))
			{
				GUIUtility.hotControl = controlID;
				current.Use();
			}
			break;
		case 3:
			if (GUIUtility.hotControl == controlID)
			{
				float num10 = (vertical ? current.delta.y : current.delta.x);
				float num11 = num10 / num4;
				val += num11 * num;
				val = Mathf.Clamp(val, min, max);
				current.Use();
			}
			break;
		case 1:
			if (GUIUtility.hotControl == controlID)
			{
				GUIUtility.hotControl = 0;
				current.Use();
			}
			break;
		}
		Color color2 = (Color)((rect.Contains(current.mousePosition) || GUIUtility.hotControl == controlID || position.Contains(current.mousePosition)) ? Theme.Accent : new Color(0.4f, 0.4f, 0.4f, 0.5f));
		DrawRoundedBox(rect, color2);
		GUI.color = color;
		return val;
	}

	public static void DrawGlowBox(Rect rect, Color color, float glowRadius = 10f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		EnsureThemeResources();
		Color color2 = GUI.color;
		GUI.color = new Color(color.r, color.g, color.b, 0.3f);
		Texture texture = (Texture)(object)Theme.ShadowTexture ?? (Texture)(object)Texture2D.whiteTexture;
		GUI.DrawTexture(new Rect(rect.x - glowRadius, rect.y - glowRadius, rect.width + glowRadius * 2f, rect.height + glowRadius * 2f), texture);
		GUI.color = color;
		GUI.Box(rect, GUIContent.none, Theme.RoundedBoxStyle);
		GUI.color = color2;
	}

	public static void CreateGLMaterial()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		if (_glMaterial == null)
		{
			Shader val = Shader.Find("Hidden/Internal-Colored");
			_glMaterial = new Material(val)
			{
				hideFlags = (HideFlags)61
			};
			_glMaterial.SetInt("_SrcBlend", 5);
			_glMaterial.SetInt("_DstBlend", 10);
			_glMaterial.SetInt("_Cull", 0);
			_glMaterial.SetInt("_ZWrite", 0);
		}
	}

	public static void DrawBoxGL(Rect rect, Color color, float thickness = 1f)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)_glMaterial == (UnityEngine.Object)null)
		{
			CreateGLMaterial();
		}
		_glMaterial.SetPass(0);
		GL.PushMatrix();
		GL.LoadPixelMatrix(0f, (float)Screen.width, (float)Screen.height, 0f);
		GL.Begin(1);
		GL.Color(color);
		float x = rect.x;
		float y = rect.y;
		float width = rect.width;
		float height = rect.height;
		GL.Vertex3(x, y, 0f);
		GL.Vertex3(x + width, y, 0f);
		GL.Vertex3(x, y + height, 0f);
		GL.Vertex3(x + width, y + height, 0f);
		GL.Vertex3(x, y, 0f);
		GL.Vertex3(x, y + height, 0f);
		GL.Vertex3(x + width, y, 0f);
		GL.Vertex3(x + width, y + height, 0f);
		GL.End();
		GL.PopMatrix();
	}

	public static void DrawBoxGraphics(Rect rect, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		Color color2 = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), (Texture)(object)Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height, rect.width, 1f), (Texture)(object)Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), (Texture)(object)Texture2D.whiteTexture);
		GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y, 1f, rect.height + 1f), (Texture)(object)Texture2D.whiteTexture);
		GUI.color = color2;
	}
}
