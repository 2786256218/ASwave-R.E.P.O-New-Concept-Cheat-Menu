using System;
using System.Collections.Generic;
using System.Reflection;
using Cheat.Config;
using UnityEngine;

namespace Cheat.UI;

public static class KeybindsUI
{
	private class ActiveFeature
	{
		public string Name;

		public KeyCode Key;
	}

	private static List<ActiveFeature> _activeFeatures = new List<ActiveFeature>();

	private static float _lastCheckTime = 0f;

	public static void Draw()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		if (Time.time - _lastCheckTime > 1f)
		{
			UpdateActiveFeatures();
			_lastCheckTime = Time.time;
		}
		if (_activeFeatures.Count == 0)
		{
			return;
		}
		float num = (float)Screen.height / 2f - (float)_activeFeatures.Count * 25f / 2f;
		float num2 = 20f;
		foreach (ActiveFeature activeFeature in _activeFeatures)
		{
			string text = $"[{activeFeature.Key}] {activeFeature.Name}";
			Render.DrawStringOutlined(new Rect(num2, num, 300f, 25f), text, Color.white, center: false, 16, bold: true);
			num += 25f;
		}
	}

	private static void UpdateActiveFeatures()
	{
		_activeFeatures.Clear();
		CheckSettings(ConfigManager.Config.Loot, "Loot ESP");
		CheckSettings(ConfigManager.Config.Enemies, "Enemy ESP");
		CheckSettings(ConfigManager.Config.Minimap, "Minimap");
		CheckSettings(ConfigManager.Config.Compass, "Compass");
		CheckSettings(ConfigManager.Config.Local, "God Mode", "GodMode", "GodModeKey");
		CheckSettings(ConfigManager.Config.Local, "穿墙模式", "NoClip", "NoClipKey");
		if (Cheat.Features.LocalPlayer.FreeCam.Enabled)
		{
			_activeFeatures.Add(new ActiveFeature
			{
				Name = "自由视角",
				Key = ConfigManager.Config.Local.FreeCamToggleKey
			});
		}
	}

	private static void CheckSettings(object settingsObj, string displayName, string boolField = "Enabled", string keyField = "ToggleKey")
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Invalid comparison between Unknown and I4
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		if (settingsObj == null)
		{
			return;
		}
		Type type = settingsObj.GetType();
		bool flag = true;
		if (!string.IsNullOrEmpty(boolField))
		{
			FieldInfo field = type.GetField(boolField);
			if (field != null && field.GetValue(settingsObj) is bool flag2)
			{
				flag = flag2;
			}
		}
		if (flag)
		{
			FieldInfo field2 = type.GetField(keyField);
			if (field2 != null && field2.GetValue(settingsObj) is KeyCode val && (int)val > 0)
			{
				_activeFeatures.Add(new ActiveFeature
				{
					Name = displayName,
					Key = val
				});
			}
		}
	}

	private static void CheckSettings(object settingsObj, string displayName, string keyField)
	{
		CheckSettings(settingsObj, displayName, null, keyField);
	}
}
