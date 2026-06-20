using System.Collections.Generic;
using Cheat.Config;
using Cheat.UI;
using UnityEngine;

namespace Cheat.Features.Visuals;

public static class LaserSight
{
	public struct HitInfo
	{
		public string Name;

		public Vector3 Position;

		public float Time;
	}

	private static Dictionary<int, HitInfo> _hitInfos = new Dictionary<int, HitInfo>();

	public static void SetHitInfo(int playerId, string name, Vector3 pos)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		_hitInfos[playerId] = new HitInfo
		{
			Name = name,
			Position = pos,
			Time = Time.time
		};
	}

	public static void ClearHitInfo(int playerId)
	{
		if (_hitInfos.ContainsKey(playerId))
		{
			_hitInfos.Remove(playerId);
		}
	}

	public static void Update()
	{
		if ((UnityEngine.Object)(object)GameDirector.instance == (UnityEngine.Object)null)
		{
			return;
		}
		foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
		{
			if (!((UnityEngine.Object)(object)player == (UnityEngine.Object)null))
			{
				LaserBeamController component = ((Component)player).GetComponent<LaserBeamController>();
				if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
				{
					component = ((Component)player).gameObject.AddComponent<LaserBeamController>();
					component.Initialize(player);
				}
			}
		}
	}

	public static void Draw()
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		if (!ConfigManager.Config.LaserSight.Enabled || !ConfigManager.Config.LaserSight.ShowHitInfo)
		{
			return;
		}
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)main == (UnityEngine.Object)null)
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, HitInfo> hitInfo in _hitInfos)
		{
			if (Time.time - hitInfo.Value.Time > 0.1f)
			{
				list.Add(hitInfo.Key);
				continue;
			}
			Vector3 val = main.WorldToScreenPoint(hitInfo.Value.Position);
			if (val.z > 0f)
			{
				float num = (float)Screen.height - val.y;
				string name = hitInfo.Value.Name;
				Vector2 val2 = Render.MeasureString(name, 12);
				Render.DrawStringOutlined(new Rect(val.x - val2.x / 2f, num - 25f, val2.x, val2.y), name, ConfigManager.Config.LaserSight.Color, center: true, 12);
			}
		}
		foreach (int item in list)
		{
			_hitInfos.Remove(item);
		}
	}
}
