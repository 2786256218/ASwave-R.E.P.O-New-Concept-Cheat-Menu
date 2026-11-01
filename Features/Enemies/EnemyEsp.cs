using System.Collections.Generic;
using Cheat.Config;
using Cheat.Features.LocalPlayer;
using Cheat.UI;
using Cheat.Utils;
using UnityEngine;

namespace Cheat.Features.Enemies;

public class EnemyEsp
{
	private static readonly Color WarningColor = new Color(1f, 0.25f, 0.25f, 0.95f);

	private static readonly Color StatusAlertColor = new Color(1f, 0.35f, 0.35f, 0.95f);

	private static readonly Color StatusSearchColor = new Color(1f, 0.88f, 0.2f, 0.95f);

	private static readonly Vector3 EnemyFallbackSize = new Vector3(1.1f, 2.2f, 1.1f);

	private static readonly HashSet<int> DrawnEnemyIds = new HashSet<int>();

	public static void Draw()
	{
		if (!ConfigManager.Config.Enemies.EspEnabled || (UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null)
		{
			return;
		}
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)main == (UnityEngine.Object)null || (int)Event.current.type != 7)
		{
			return;
		}
		DrawnEnemyIds.Clear();
		foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
		{
			if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null)
			{
				continue;
			}
			EnemyManager.EnemyData enemyData = EnemyManager.GetEnemyData(item);
			if (enemyData == null || !EnemyManager.IsEnemyActive(enemyData) || (UnityEngine.Object)(object)enemyData.Enemy == (UnityEngine.Object)null)
			{
				continue;
			}
			Enemy enemy = enemyData.Enemy;
			int instanceID = ((UnityEngine.Object)enemy).GetInstanceID();
			if (!DrawnEnemyIds.Add(instanceID))
			{
				continue;
			}
			Vector3 smoothedPosition = enemyData.SmoothedPosition;
			float num3 = Vector3.Distance(((Component)main).transform.position, smoothedPosition);
			if (num3 > ConfigManager.Config.Enemies.MaxDistance)
			{
				continue;
			}
			EnemyEspLineRenderer component2 = ((Component)enemy).gameObject.GetComponent<EnemyEspLineRenderer>();
			if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
			{
				UnityEngine.Object.Destroy((UnityEngine.Object)(object)component2);
			}
			Bounds enemyBounds;
			bool hasEnemyBounds = EnemyManager.TryGetAccurateBounds(enemyData, out enemyBounds);
			if (!hasEnemyBounds)
				enemyBounds = new Bounds(((Component)enemy).transform.position, Vector3.zero);

			if (hasEnemyBounds && Time.unscaledTime >= enemyData.NextOutlineVisibilityCheck)
			{
				enemyData.OutlineVisible = EnemyVisibilityUtility.IsEnemyVisible(enemy, enemyBounds, main);
				enemyData.OutlineFullyOccluded = !enemyData.OutlineVisible;
				enemyData.NextOutlineVisibilityCheck = Time.unscaledTime + 0.08f;
			}
			// Occlusion affects the color only. Keep projecting and drawing the box
			// when a wall blocks the monster; visibility checks must not gate the
			// screen-space outline itself.

			Vector3 enemyTargetPoint = EnemyManager.GetTargetPoint(enemyData, enemyBounds);
			Rect rect;
			if (!EnemyManager.TryGetAccurateScreenRect(main, enemyData, 0f, 0f, out rect))
			{
				continue;
			}
			float width = rect.width;
			float height = rect.height;
			Color espColor = ResolveEnemyEspColor(enemy, enemyData);
			float labelBottomOffset = 2f;
			if (ConfigManager.Config.Enemies.DrawTracers)
			{
				Render.DrawLine(new Vector2((float)Screen.width * 0.5f, (float)Screen.height - 18f), new Vector2(rect.x + width * 0.5f, rect.y + height), new Color(espColor.r, espColor.g, espColor.b, 0.92f), 1.25f);
			}
			if (ConfigManager.Config.Enemies.DrawBox && ConfigManager.Config.Enemies.RenderMethod != 2)
			{
				Render.DrawEspBox(rect, espColor, 1.6f, ConfigManager.Config.Enemies.RenderMethod);
			}

			if (ConfigManager.Config.Aimbot != null && ConfigManager.Config.Aimbot.Enabled && ConfigManager.Config.Aimbot.DrawFov)
			{
				Vector3 targetScreen = MathUtils.WorldToScreen(main, enemyTargetPoint, LocalPlayerManager.GetAimbotReferenceFov());
				if (targetScreen.z > 0.05f)
				{
					Render.DrawBox(new Rect(targetScreen.x - 2f, targetScreen.y - 2f, 4f, 4f), Color.white);
				}
			}

			if (ConfigManager.Config.Enemies.DrawHealth && (UnityEngine.Object)(object)enemyData.Health != (UnityEngine.Object)null)
			{
				int currentHealth;
				int maxHealth;
				if (TryGetHealth(enemyData.Health, out currentHealth, out maxHealth) && maxHealth > 0)
				{
					labelBottomOffset = DrawVerticalHealthBar(rect, currentHealth, maxHealth);
				}
			}

			if (ConfigManager.Config.Enemies.TargetWarning && IsTargetingLocalPlayer(enemy))
			{
				Render.DrawStringOutlined(new Rect(rect.x - 32f, rect.y - 36f, width + 64f, 18f), "锁定你", WarningColor, center: true, 12, bold: true);
			}
			string text2 = BuildHeaderText(EnemyNameResolver.GetEnemyDisplayName(enemy, item), num3);
			if (!string.IsNullOrEmpty(text2))
			{
				Render.DrawStringOutlined(new Rect(rect.x - 40f, rect.y - 18f, width + 80f, 18f), text2, espColor, center: true, 12, bold: true);
			}
			if (!ConfigManager.Config.Enemies.DrawStatus)
			{
				continue;
			}
			string text3 = ResolveHealthStatus(enemyData.Health, out Color color2);
			Render.DrawStringOutlined(new Rect(rect.x - 60f, rect.y + height + labelBottomOffset, width + 120f, 18f), text3, color2, center: true, 12, bold: true);
		}
	}

	private static bool ShouldUseEnemyRenderer(Renderer renderer)
	{
		return !((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null) && renderer.enabled && !renderer.isPartOfStaticBatch;
	}

	private static Color ResolveEnemyEspColor(Enemy enemy, EnemyManager.EnemyData enemyData)
	{
		Color espColor = ConfigManager.Config.Enemies.EspColor;
		if (!ConfigManager.Config.Enemies.VisibilityCheck)
		{
			return espColor;
		}

		return enemyData.OutlineVisible ? espColor : ConfigManager.Config.Enemies.OccludedEspColor;
	}

	private static string BuildHeaderText(string enemyName, float distance)
	{
		bool drawInfo = ConfigManager.Config.Enemies.DrawInfo;
		bool drawDistance = ConfigManager.Config.Enemies.DrawDistance;
		if (!drawInfo && !drawDistance)
		{
			return null;
		}

		string distanceText = string.Format("{0:F0}m", distance);
		if (!drawInfo)
		{
			return distanceText;
		}

		if (!drawDistance)
		{
			return enemyName;
		}

		return string.Format("{0} [{1}]", enemyName, distanceText);
	}

	private static bool TryGetHealth(EnemyHealth healthComponent, out int currentHealth, out int maxHealth)
	{
		Enemy enemy = ((UnityEngine.Object)(object)healthComponent != (UnityEngine.Object)null) ? ((Component)healthComponent).GetComponent<Enemy>() : null;
		return EnemyHealthResolver.TryGetDisplayHealth(enemy, healthComponent, out currentHealth, out maxHealth);
	}

	private static string ResolveHealthStatus(EnemyHealth healthComponent, out Color color)
	{
		int currentHealth;
		int maxHealth;
		if (TryGetHealth(healthComponent, out currentHealth, out maxHealth) && maxHealth > 0)
		{
			int percent = Mathf.Clamp(Mathf.RoundToInt(currentHealth * 100f / maxHealth), 0, 100);
			if (percent >= 100)
			{
				color = new Color(0.3f, 1f, 0.48f, 0.96f);
				return "100% 健康";
			}
			if (percent >= 30)
			{
				color = new Color(1f, 0.8f, 0.2f, 0.96f);
				return "受伤 " + percent + "%";
			}
			color = StatusAlertColor;
			return "濒死 " + percent + "%";
		}

		color = Color.white;
		return "血量暂不可用";
	}

	private static float DrawVerticalHealthBar(Rect rect, int currentHealth, int maxHealth)
	{
		float healthPercent = Mathf.Clamp01((float)currentHealth / (float)maxHealth);
		float barWidth = 4f;
		Rect backgroundRect = new Rect(rect.x - 8f, rect.y, barWidth, rect.height);
		Render.DrawBox(backgroundRect, new Color(0f, 0f, 0f, 0.65f));

		float filledHeight = (backgroundRect.height - 2f) * healthPercent;
		Color healthColor = Color.Lerp(new Color(1f, 0.22f, 0.22f, 0.95f), new Color(0.22f, 1f, 0.35f, 0.95f), healthPercent);
		Render.DrawBox(new Rect(backgroundRect.x + 1f, backgroundRect.yMax - 1f - filledHeight, backgroundRect.width - 2f, filledHeight), healthColor);
		Render.DrawStringOutlined(new Rect(rect.x - 48f, rect.y + rect.height + 2f, rect.width + 96f, 18f), string.Format("{0}/{1}", currentHealth, maxHealth), Color.white, center: true, 11, bold: true);
		return 20f;
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

	private static bool IsTargetingLocalPlayer(Enemy enemy)
	{
		PlayerAvatar targetPlayer = ReflectionUtils.GetFieldValue<PlayerAvatar>(enemy, "TargetPlayerAvatar");
		return (UnityEngine.Object)(object)targetPlayer != (UnityEngine.Object)null && (UnityEngine.Object)(object)targetPlayer == (UnityEngine.Object)(object)PlayerAvatar.instance;
	}
}
