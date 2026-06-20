using System.Collections.Generic;
using Cheat.Config;
using Cheat.UI;
using Cheat.Utils;
using UnityEngine;

namespace Cheat.Features.Loot;

public static class LootEsp
{
	private class Cluster
	{
		public Vector2 ScreenPos;

		public List<LootItem> Items = new List<LootItem>();

		public int TotalValue;
	}

	public static void Draw()
	{
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03de: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		if (!ConfigManager.Config.Loot.Enabled || LootManager.LootItems == null || LootManager.LootItems.Count == 0)
		{
			return;
		}
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)main == (UnityEngine.Object)null)
		{
			return;
		}
		Vector2 val = default(Vector2);
		val = new Vector2((float)Screen.width / 2f, (float)Screen.height / 2f);
		List<Cluster> list = new List<Cluster>();
		float num3 = 1600f;
		foreach (LootItem lootItem in LootManager.LootItems)
		{
			if ((UnityEngine.Object)(object)lootItem.PhysGrabObject == (UnityEngine.Object)null || lootItem.InCart)
			{
				continue;
			}
			Vector3 position = lootItem.Position;
			Vector3 val2 = MathUtils.WorldToScreen(main, position);
			if (val2.z <= 0f)
			{
				continue;
			}
			float num4 = 1f;
			if (ConfigManager.Config.Loot.DynamicOpacity)
			{
				float num5 = Vector2.Distance(new Vector2(val2.x, val2.y), val);
				if (num5 < 150f)
				{
					num4 = Mathf.Clamp01((num5 - 50f) / 100f);
				}
			}
			if (num4 <= 0.05f)
			{
				continue;
			}
			if (ConfigManager.Config.Loot.DrawBox && TryGetLootRect(main, lootItem, out var rect))
			{
				Color color = ConfigManager.Config.Loot.EspColor;
				color.a = num4;
				DrawLootBox(rect, color);
			}
			if (ConfigManager.Config.Loot.UseClustering)
			{
				bool flag = false;
				foreach (Cluster item in list)
				{
					Vector2 val3 = item.ScreenPos - new Vector2(val2.x, val2.y);
					if (val3.sqrMagnitude < num3)
					{
						item.Items.Add(lootItem);
						item.TotalValue += lootItem.Value;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(new Cluster
					{
						ScreenPos = new Vector2(val2.x, val2.y),
						Items = new List<LootItem> { lootItem },
						TotalValue = lootItem.Value
					});
				}
			}
			else
			{
				DrawItem(lootItem, new Vector2(val2.x, val2.y), num4);
			}
		}
		if (!ConfigManager.Config.Loot.UseClustering)
		{
			return;
		}
		foreach (Cluster item2 in list)
		{
			float num6 = 1f;
			if (ConfigManager.Config.Loot.DynamicOpacity)
			{
				float num7 = Vector2.Distance(item2.ScreenPos, val);
				if (num7 < 150f)
				{
					num6 = Mathf.Clamp01((num7 - 50f) / 100f);
				}
			}
			if (!(num6 <= 0.05f))
			{
				if (item2.Items.Count > 1)
				{
					string text = $"{item2.Items.Count}x Items [${item2.TotalValue}]";
					Color cyan = ConfigManager.Config.Loot.EspColor;
					cyan.a = num6;
					Vector2 val4 = Render.MeasureString(text, 12, bold: true);
					Render.DrawStringOutlined(new Rect(item2.ScreenPos.x - val4.x / 2f, item2.ScreenPos.y - val4.y / 2f, val4.x, val4.y), text, cyan, center: true, 12, bold: true);
				}
				else if (item2.Items.Count == 1)
				{
					DrawItem(item2.Items[0], item2.ScreenPos, num6);
				}
			}
		}
	}

	public static void DrawCartUI()
	{
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		if (!ConfigManager.Config.Loot.Enabled || !ConfigManager.Config.Loot.ShowCartUI || LootManager.LootItems == null)
		{
			return;
		}
		List<LootItem> list = new List<LootItem>();
		int num = 0;
		foreach (LootItem lootItem in LootManager.LootItems)
		{
			if (lootItem.InCart)
			{
				list.Add(lootItem);
				num += lootItem.Value;
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		foreach (LootItem item in list)
		{
			if (!dictionary.ContainsKey(item.Name))
			{
				dictionary[item.Name] = 0;
				dictionary2[item.Name] = 0;
			}
			dictionary[item.Name]++;
			dictionary2[item.Name] += item.Value;
		}
		float num2 = 250f;
		float num3 = 20f;
		float num4 = 10f;
		float num5 = 25f;
		float num6 = num5 + (float)dictionary.Count * num3 + num4 * 2f;
		float num7 = 20f;
		float num8 = (float)Screen.height - num6 - 20f;
		Color black = Color.black;
		black.a = 0.8f;
		Render.DrawRoundedBox(new Rect(num7, num8, num2, num6), black);
		Render.DrawBox(new Rect(num7, num8 + num5, num2, 1f), Theme.Accent);
		Render.DrawString(new Rect(num7 + num4, num8 + 2f, num2 - num4 * 2f, num5), $"CART INVENTORY [${num}]", Theme.Accent, center: false, 14, bold: true);
		float num9 = num8 + num5 + num4;
		foreach (KeyValuePair<string, int> item2 in dictionary)
		{
			string key = item2.Key;
			int value = item2.Value;
			int num10 = dictionary2[key];
			string text = $"{value}x {key}";
			string text2 = $"${num10}";
			Render.DrawString(new Rect(num7 + num4, num9, num2, num3), text, Theme.Text, center: false, 12);
			Vector2 val = Render.MeasureString(text2, 12);
			Render.DrawString(new Rect(num7 + num2 - num4 - val.x, num9, val.x, num3), text2, Theme.TextDim, center: false, 12);
			num9 += num3;
		}
	}

	private static void DrawItem(LootItem item, Vector2 screenPos, float alpha)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		text = ((!ConfigManager.Config.Loot.DrawName) ? $"${item.Value}" : $"{item.Name} [${item.Value}]");
		Color color = ConfigManager.Config.Loot.EspColor;
		color.a = alpha;
		Vector2 val = Render.MeasureString(text, 12, bold: true);
		Render.DrawStringOutlined(new Rect(screenPos.x - val.x / 2f, screenPos.y - val.y / 2f, val.x, val.y), text, color, center: true, 12, bold: true);
		if (ConfigManager.Config.Loot.DrawTracers)
		{
			Vector2 start = default(Vector2);
			start = new Vector2((float)Screen.width / 2f, (float)Screen.height);
			Render.DrawLine(start, screenPos, color, 1f);
		}
	}

	private static bool TryGetLootRect(Camera camera, LootItem item, out Rect rect)
	{
		rect = default(Rect);
		if ((UnityEngine.Object)(object)camera == (UnityEngine.Object)null || item == null || (UnityEngine.Object)(object)item.PhysGrabObject == (UnityEngine.Object)null)
		{
			return false;
		}

		return EspProjectionUtils.TryGetScreenRect(camera, ((Component)item.PhysGrabObject).transform, item.Renderers, ShouldUseLootRenderer, item.Position, new Vector3(0.35f, 0.35f, 0.35f), 14f, 14f, out rect);
	}

	private static bool ShouldUseLootRenderer(Renderer renderer)
	{
		return !((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null) && renderer.enabled && !(renderer is ParticleSystemRenderer) && !(renderer is TrailRenderer) && !(renderer is LineRenderer) && renderer.bounds.size.sqrMagnitude > 0.001f;
	}

	private static void DrawLootBox(Rect rect, Color color)
	{
		DrawCornerBox(rect, color, 1.5f);
	}

	private static void DrawCornerBox(Rect rect, Color color, float thickness)
	{
		float cornerWidth = Mathf.Clamp(rect.width * 0.32f, 8f, 18f);
		float cornerHeight = Mathf.Clamp(rect.height * 0.18f, 10f, 20f);
		Render.DrawBox(new Rect(rect.x, rect.y, cornerWidth, thickness), color);
		Render.DrawBox(new Rect(rect.x, rect.y, thickness, cornerHeight), color);
		Render.DrawBox(new Rect(rect.xMax - cornerWidth, rect.y, cornerWidth, thickness), color);
		Render.DrawBox(new Rect(rect.xMax - thickness, rect.y, thickness, cornerHeight), color);
		Render.DrawBox(new Rect(rect.x, rect.yMax - thickness, cornerWidth, thickness), color);
		Render.DrawBox(new Rect(rect.x, rect.yMax - cornerHeight, thickness, cornerHeight), color);
		Render.DrawBox(new Rect(rect.xMax - cornerWidth, rect.yMax - thickness, cornerWidth, thickness), color);
		Render.DrawBox(new Rect(rect.xMax - thickness, rect.yMax - cornerHeight, thickness, cornerHeight), color);
	}
}
