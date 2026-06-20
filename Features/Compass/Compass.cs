using System;
using Cheat.Config;
using Cheat.Features.Enemies;
using Cheat.UI;
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
		if (ConfigManager.Config.Compass.Enabled && !((UnityEngine.Object)(object)Camera.main == (UnityEngine.Object)null))
		{
			DrawRadar();
		}
	}

	private void DrawRadar()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Invalid comparison between Unknown and I4
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Invalid comparison between Unknown and I4
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Invalid comparison between Unknown and I4
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03de: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		float num = Screen.width;
		float num2 = Screen.height;
		float size = ConfigManager.Config.Compass.Size;
		float num3 = num2 - size / 2f - 50f;
		float num4 = num / 2f;
		Vector2 val = default(Vector2);
		val = new Vector2(num4, num3);
		float num5 = 0.5f;
		float num6 = size;
		float num7 = size * num5;
		DrawEllipse(val, num6 / 2f, num7 / 2f, new Color(0f, 0f, 0f, 0.5f), 2f);
		DrawEllipse(val, num6 / 2f, num7 / 2f, ConfigManager.Config.Misc.MenuAccent, 1f);
		Render.DrawLine(new Vector2(val.x - 5f, val.y), new Vector2(val.x + 5f, val.y), Color.white, 1f);
		Render.DrawLine(new Vector2(val.x, val.y - 5f), new Vector2(val.x, val.y + 5f), Color.white, 1f);
		if ((UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null)
		{
			return;
		}
		Camera main = Camera.main;
		Vector3 position = ((Component)main).transform.position;
		Vector3 forward = ((Component)main).transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 right = ((Component)main).transform.right;
		right.y = 0f;
		right.Normalize();
		float range = ConfigManager.Config.Compass.Range;
		Vector2 val3 = default(Vector2);
		Vector2 val4 = default(Vector2);
		foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
		{
			if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null)
			{
				continue;
			}
			EnemyManager.EnemyData enemyData = EnemyManager.GetEnemyData(item);
			if (enemyData == null || (UnityEngine.Object)(object)enemyData.Enemy == (UnityEngine.Object)null)
			{
				continue;
			}
			Vector3 smoothedPosition = enemyData.SmoothedPosition;
			Vector3 val2 = smoothedPosition - position;
			float magnitude = val2.magnitude;
			if (!(magnitude > range))
			{
				float num8 = Vector3.Dot(val2, forward);
				float num9 = Vector3.Dot(val2, right);
				float num10 = num9 / range;
				float num11 = num8 / range;
				float num12 = val.x + num10 * (num6 / 2f);
				float num13 = val.y - num11 * (num7 / 2f);
				float num14 = smoothedPosition.y - position.y;
				float num15 = num14 * 2f;
				num15 = Mathf.Clamp(num15, -30f, 30f);
				val3 = new Vector2(num12, num13);
				val4 = new Vector2(num12, num13 + num15);
				Vector2 start = val3;
				Vector2 val5 = val3 - new Vector2(0f, num15);
				Render.DrawLine(start, val5, new Color(1f, 1f, 1f, 0.5f), 1f);
				float num16 = Mathf.Lerp(1.5f, 0.5f, magnitude / range) * ConfigManager.Config.Compass.Scale;
				float num17 = 10f * num16;
				Color color = Color.red;
				if ((int)enemyData.Enemy.CurrentState == 4 || (int)enemyData.Enemy.CurrentState == 3)
				{
					color = new Color(1f, 0.2f, 0.2f);
				}
				else if ((int)enemyData.Enemy.CurrentState == 7)
				{
					color = Color.yellow;
				}
				Render.DrawCircle(val5, num17 / 2f, color);
				string text = $"{magnitude:F0}m";
				Render.DrawString(new Rect(val5.x - 20f, val5.y + num17 / 2f + 2f, 40f, 20f), text, Color.white, center: true, 10);
			}
		}
	}

	private void DrawEllipse(Vector2 center, float radiusX, float radiusY, Color color, float thickness)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		int num = 36;
		float num2 = 360f / (float)num;
		Vector2 start = Vector2.zero;
		for (int i = 0; i <= num; i++)
		{
			float num3 = (float)i * num2 * ((float)Math.PI / 180f);
			float num4 = Mathf.Cos(num3) * radiusX;
			float num5 = Mathf.Sin(num3) * radiusY;
			Vector2 val = center + new Vector2(num4, num5);
			if (i > 0)
			{
				Render.DrawLine(start, val, color, thickness);
			}
			start = val;
		}
	}

	public void Update()
	{
	}
}
