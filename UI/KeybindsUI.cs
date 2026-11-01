using System;
using System.Collections.Generic;
using System.Reflection;
using Cheat.Config;
using UnityEngine;

namespace Cheat.UI;

public static class KeybindsUI
{
	private sealed class ActiveFeature
	{
		public string Name;
		public KeyCode Key;
		public bool? Enabled;
	}

	private static readonly List<ActiveFeature> ActiveFeatures = new List<ActiveFeature>();
	private static float _lastCheckTime = -1f;

	public static void Draw()
	{
		if (ConfigManager.Config == null)
			return;
		if (_lastCheckTime < 0f || Time.unscaledTime - _lastCheckTime > 0.25f)
		{
			UpdateActiveFeatures();
			_lastCheckTime = Time.unscaledTime;
		}
		if (ActiveFeatures.Count == 0)
			return;

		const float x = 16f;
		const float y = 16f;
		const float width = 310f;
		const float rowHeight = 21f;
		float height = 30f + ActiveFeatures.Count * rowHeight;
		Color previous = GUI.color;
		GUI.color = new Color(0.015f, 0.025f, 0.04f, 0.78f);
		GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
		GUI.color = new Color(0.24f, 0.86f, 1f, 0.9f);
		GUI.DrawTexture(new Rect(x, y, width, 2f), Texture2D.whiteTexture);
		GUI.color = previous;
		Render.DrawStringOutlined(new Rect(x + 9f, y + 5f, width - 18f, 20f), "已绑定快捷键", new Color(0.35f, 0.9f, 1f, 1f), center: false, 13, bold: true);

		float rowY = y + 29f;
		for (int i = 0; i < ActiveFeatures.Count; i++)
		{
			ActiveFeature feature = ActiveFeatures[i];
			string text = feature.Name + "  [" + feature.Key + "]" + (feature.Enabled.HasValue ? (feature.Enabled.Value ? "  开启" : "  关闭") : string.Empty);
			Color color = feature.Enabled == false ? new Color(0.65f, 0.72f, 0.78f, 0.85f) : Color.white;
			Render.DrawStringOutlined(new Rect(x + 9f, rowY, width - 18f, rowHeight), text, color, center: false, 11, bold: feature.Enabled == true);
			rowY += rowHeight;
		}
	}

	private static void UpdateActiveFeatures()
	{
		ActiveFeatures.Clear();
		ConfigData config = ConfigManager.Config;
		if (config == null)
			return;

		FieldInfo[] groups = typeof(ConfigData).GetFields(BindingFlags.Instance | BindingFlags.Public);
		for (int i = 0; i < groups.Length; i++)
		{
			object settings = groups[i].GetValue(config);
			if (settings == null)
				continue;
			FieldInfo[] fields = settings.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
			for (int f = 0; f < fields.Length; f++)
			{
				FieldInfo keyField = fields[f];
				if (keyField.FieldType != typeof(KeyCode))
					continue;
				if (keyField.Name == "SolidAimKey")
					continue;
				KeyCode key = (KeyCode)keyField.GetValue(settings);
				if (key == KeyCode.None)
					continue;

				string boolName = ResolveBooleanFieldName(settings.GetType(), keyField.Name);
				bool? enabled = null;
				if (!string.IsNullOrEmpty(boolName))
				{
					FieldInfo stateField = settings.GetType().GetField(boolName, BindingFlags.Instance | BindingFlags.Public);
					if (stateField != null && stateField.FieldType == typeof(bool))
						enabled = (bool)stateField.GetValue(settings);
				}

				ActiveFeatures.Add(new ActiveFeature
				{
					Name = ResolveDisplayName(groups[i].Name, keyField.Name),
					Key = key,
					Enabled = enabled
				});
			}
		}

		ActiveFeatures.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
	}

	private static string ResolveBooleanFieldName(Type settingsType, string keyFieldName)
	{
		string candidate = keyFieldName.EndsWith("Key", StringComparison.Ordinal) ? keyFieldName.Substring(0, keyFieldName.Length - 3) : keyFieldName;
		if (candidate.Length > 0)
		{
			FieldInfo exact = settingsType.GetField(candidate, BindingFlags.Instance | BindingFlags.Public);
			if (exact != null && exact.FieldType == typeof(bool))
				return candidate;
		}
		if (keyFieldName == "ToggleKey")
		{
			FieldInfo enabled = settingsType.GetField("Enabled", BindingFlags.Instance | BindingFlags.Public);
			if (enabled != null && enabled.FieldType == typeof(bool))
				return "Enabled";
			FieldInfo espEnabled = settingsType.GetField("EspEnabled", BindingFlags.Instance | BindingFlags.Public);
			if (espEnabled != null && espEnabled.FieldType == typeof(bool))
				return "EspEnabled";
		}
		return null;
	}

	private static string ResolveDisplayName(string groupName, string keyFieldName)
	{
		string keyName = keyFieldName.EndsWith("Key", StringComparison.Ordinal) ? keyFieldName.Substring(0, keyFieldName.Length - 3) : keyFieldName;
		string group = groupName.Replace("Settings", string.Empty);
		string groupLabel = group;
		switch (group)
		{
			case "Loot": groupLabel = "物品透视"; break;
			case "Enemies": groupLabel = "敌人透视"; break;
			case "Minimap": groupLabel = "小地图"; break;
			case "Compass": groupLabel = "雷达"; break;
			case "Local": groupLabel = "本地"; break;
			case "Aimbot": groupLabel = "自瞄"; break;
			case "Specials": groupLabel = "特殊物品"; break;
			case "GameModifiers": groupLabel = "全局修改"; break;
			case "PlayerEsp": groupLabel = "玩家"; break;
			case "PlayerEquipmentEsp": groupLabel = "玩家装备"; break;
			case "LaserSight": groupLabel = "激光瞄准器"; break;
		}
		string feature = keyName;
		if (keyName == "Toggle")
			feature = group == "Compass" ? "雷达" : group == "Enemies" ? "敌人透视" : group == "Loot" ? "物品透视" : group;
		else
		{
			switch (keyName)
			{
				case "GodMode": feature = "无敌"; break;
				case "NoClip": feature = "穿墙移动"; break;
				case "ThirdPerson": feature = "第三人称"; break;
				case "TumbleFly": feature = "摔倒飞行"; break;
				case "Crosshair": feature = "准星"; break;
				case "ShowKeybinds": feature = "显示快捷键"; break;
				case "ShowFps": feature = "显示帧率"; break;
				case "EspEnabled": feature = "敌人透视"; break;
				case "DrawName": feature = "显示名称"; break;
				case "DrawHealth": feature = "显示血量"; break;
				case "DrawDistance": feature = "显示距离"; break;
				case "DrawTracers": feature = "绘制射线"; break;
				case "DrawStatus": feature = "显示血量状态"; break;
				case "DrawBox": feature = "绘制方框"; break;
				case "Enabled": feature = group == "Aimbot" ? "自瞄" : group == "Compass" ? "雷达" : group; break;
			}
		}

		if (group == "Local" && feature != "无敌" && feature != "穿墙移动" && feature != "第三人称" && feature != "摔倒飞行")
			return groupLabel + " " + feature;
		if (group == "PlayerEsp")
			return groupLabel + " " + feature;
		if (group == "PlayerEquipmentEsp")
			return groupLabel + " " + feature;
		if (group == "Enemies")
			return feature;
		if (group == "Compass")
			return feature;
		return groupLabel + " " + feature;
	}
}
