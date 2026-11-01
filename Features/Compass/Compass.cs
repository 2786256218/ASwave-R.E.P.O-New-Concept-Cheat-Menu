using Cheat.Config;
using Cheat.Features.Enemies;
using Cheat.UI;
using Cheat.Utils;
using UnityEngine;

namespace Cheat.Features.Compass;

public class Compass : MonoBehaviour
{
	public static Compass Instance;

	private void Awake()
	{
		Instance = this;
	}

	private void OnGUI()
	{
		if (ConfigManager.Config != null && ConfigManager.Config.Compass.Enabled && Camera.main != null && Event.current.type == EventType.Repaint)
		{
			DrawRadar();
		}
	}

	private void DrawRadar()
	{
		ConfigData.CompassSettings settings = ConfigManager.Config.Compass;
		float size = Mathf.Clamp(settings.Size, 100f, 600f);
		float radius = size * 0.5f;
		Vector2 center = new Vector2(radius + 12f + settings.PositionX, radius + 12f + settings.PositionY + settings.YOffset);
		center.x = Mathf.Clamp(center.x, radius + 4f, Screen.width - radius - 4f);
		center.y = Mathf.Clamp(center.y, radius + 4f, Screen.height - radius - 4f);
		Color accent = ConfigManager.Config.Misc.MenuAccent;

		Render.DrawCircle(center, radius, new Color(0.015f, 0.035f, 0.055f, 0.78f));
		Render.DrawCircleOutline(center, radius, new Color(accent.r, accent.g, accent.b, 0.8f), 2f, 80);
		Render.DrawCircleOutline(center, radius * 0.66f, new Color(0.55f, 0.78f, 0.9f, 0.20f), 1f, 64);
		Render.DrawCircleOutline(center, radius * 0.33f, new Color(0.55f, 0.78f, 0.9f, 0.15f), 1f, 48);
		Render.DrawLine(new Vector2(center.x - radius, center.y), new Vector2(center.x + radius, center.y), new Color(0.55f, 0.78f, 0.9f, 0.10f), 1f);
		Render.DrawLine(new Vector2(center.x, center.y - radius), new Vector2(center.x, center.y + radius), new Color(0.55f, 0.78f, 0.9f, 0.10f), 1f);
		DrawScan(center, radius);
		Render.DrawCircle(center, 3.5f, Color.white);
		Render.DrawString(new Rect(center.x - 35f, center.y - radius - 20f, 70f, 18f), "RADAR", accent, center: true, 11, bold: true);

		if (EnemyDirector.instance != null)
		{
			foreach (EnemyParent parent in EnemyDirector.instance.enemiesSpawned)
			{
				if (parent == null)
					continue;
				EnemyManager.EnemyData data = EnemyManager.GetEnemyData(parent);
				if (data == null || data.Enemy == null || !EnemyManager.IsEnemyActive(data))
					continue;
				DrawTarget(center, radius, data.SmoothedPosition, Color.red, "怪物");
			}
		}

		if ((settings.ShowPlayers || settings.ShowDeathHeads) && GameDirector.instance != null && GameDirector.instance.PlayerList != null)
		{
			foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
			{
				if (player == null || player == PlayerAvatar.instance)
					continue;

				bool isDisabled = ReflectionUtils.GetFieldValue<bool>(player, "isDisabled");
				if (!isDisabled && settings.ShowPlayers)
				{
					DrawTarget(center, radius, player.transform.position, new Color(0.25f, 0.8f, 1f, 1f), "玩家");
				}

				if (isDisabled && settings.ShowDeathHeads)
				{
					PlayerDeathHead head = ReflectionUtils.GetFieldValue<PlayerDeathHead>(player, "playerDeathHead");
					if (head != null && head.gameObject.activeInHierarchy && ReflectionUtils.GetFieldValue<bool>(head, "triggered"))
					{
						DrawTarget(center, radius, head.transform.position, new Color(1f, 0.35f, 0.8f, 1f), "頭顱");
					}
				}
			}
		}
	}

	private static void DrawScan(Vector2 center, float radius)
	{
		float sweepAngle = Mathf.Repeat(Time.unscaledTime * 65f, 360f);
		for (int i = 10; i >= 0; i--)
		{
			float angle = (sweepAngle - i * 2.8f) * Mathf.Deg2Rad;
			Vector2 direction = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle));
			float alpha = 0.018f + (10 - i) * 0.0045f;
			Render.DrawLine(center, center + direction * radius, new Color(0.2f, 1f, 0.74f, alpha), 1.2f);
		}
	}

	private static void DrawTarget(Vector2 center, float radius, Vector3 worldPosition, Color color, string label)
	{
		Camera camera = Camera.main;
		if (camera == null)
			return;

		Vector3 offset = worldPosition - camera.transform.position;
		Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
		Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
		float forwardDistance = Vector3.Dot(offset, forward);
		float rightDistance = Vector3.Dot(offset, right);
		float flatDistance = new Vector2(forwardDistance, rightDistance).magnitude;
		float range = Mathf.Max(1f, ConfigManager.Config.Compass.Range);
		if (flatDistance > range)
			return;

		Vector2 point = center + new Vector2(rightDistance / range * radius, -forwardDistance / range * radius);
		float size = Mathf.Lerp(8f, 4f, Mathf.Clamp01(flatDistance / range)) * Mathf.Clamp(ConfigManager.Config.Compass.Scale, 0.5f, 3f);
		float altitude = Mathf.Clamp(worldPosition.y - camera.transform.position.y, -15f, 15f);
		if (Mathf.Abs(altitude) > 2f)
		{
			Render.DrawLine(point, point + Vector2.up * Mathf.Sign(altitude) * 5f, color, 1f);
		}
		Render.DrawCircle(point, size * 0.5f + 1f, new Color(0f, 0f, 0f, 0.85f));
		Render.DrawCircle(point, size * 0.5f, color);
		Render.DrawString(new Rect(point.x - 22f, point.y + size * 0.5f + 1f, 44f, 14f), Mathf.RoundToInt(flatDistance) + "m", Color.white, center: true, 9);
	}
}
