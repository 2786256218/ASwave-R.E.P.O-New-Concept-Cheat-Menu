using System.Collections.Generic;
using Cheat.Config;
using UnityEngine;

namespace Cheat.Features.Loot;

public class LootManager
{
	public static List<LootItem> LootItems = new List<LootItem>();

	private static float _lastUpdateTime;

	private static float _updateInterval = 0.5f;

	public static void Update()
	{
		if (!ConfigManager.Config.Loot.Enabled && !ConfigManager.Config.Loot.HighlightEnabled)
		{
			if (LootItems.Count > 0)
			{
				LootItems.Clear();
			}
		}
		else if (!(Time.time - _lastUpdateTime < _updateInterval))
		{
			_lastUpdateTime = Time.time;
			UpdateCache();
		}
	}

	private static void UpdateCache()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)RoundDirector.instance == (UnityEngine.Object)null)
		{
			return;
		}
		List<PhysGrabObject> fieldValue = ReflectionUtils.GetFieldValue<List<PhysGrabObject>>(RoundDirector.instance, "physGrabObjects");
		if (fieldValue == null)
		{
			return;
		}
		List<LootItem> list = new List<LootItem>();
		float num = ConfigManager.Config.Loot.MaxDistance * ConfigManager.Config.Loot.MaxDistance;
		Vector3 val = (((UnityEngine.Object)(object)Camera.main != (UnityEngine.Object)null) ? ((Component)Camera.main).transform.position : Vector3.zero);
		foreach (PhysGrabObject item in fieldValue)
		{
			if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null || (UnityEngine.Object)(object)((Component)item).gameObject == (UnityEngine.Object)null || (item.playerGrabbing != null && item.playerGrabbing.Count > 0))
			{
				continue;
			}
			if ((UnityEngine.Object)(object)Camera.main != (UnityEngine.Object)null)
			{
				Vector3 val2 = ((Component)item).transform.position - val;
				float sqrMagnitude = val2.sqrMagnitude;
				if (sqrMagnitude > num)
				{
					continue;
				}
			}
			ValuableObject component = ((Component)item).GetComponent<ValuableObject>();
			if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
			{
				continue;
			}
			int num2 = (int)ReflectionUtils.GetFieldValue<float>(component, "dollarValueCurrent");
			string name = ((UnityEngine.Object)item).name;
			if (num2 <= 0 || num2 < ConfigManager.Config.Loot.MinValue)
			{
				continue;
			}
			name = name.Replace("(Clone)", "").Trim();
			bool inCart = false;
			PhysGrabObjectImpactDetector component2 = ((Component)item).GetComponent<PhysGrabObjectImpactDetector>();
			if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
			{
				inCart = ReflectionUtils.GetFieldValue<bool>(component2, "inCart");
			}
			List<Renderer> list2 = new List<Renderer>();
			List<Mesh> list3 = new List<Mesh>();
			Renderer[] componentsInChildren = ((Component)item).GetComponentsInChildren<Renderer>();
			foreach (Renderer val3 in componentsInChildren)
			{
				if ((UnityEngine.Object)(object)val3 == (UnityEngine.Object)null)
				{
					continue;
				}
				list2.Add(val3);
				MeshRenderer val4 = (MeshRenderer)(object)((val3 is MeshRenderer) ? val3 : null);
				if (val4 != null)
				{
					MeshFilter component3 = ((Component)val4).GetComponent<MeshFilter>();
					if ((UnityEngine.Object)(object)component3 != (UnityEngine.Object)null && (UnityEngine.Object)(object)component3.sharedMesh != (UnityEngine.Object)null)
					{
						list3.Add(component3.sharedMesh);
					}
				}
				else
				{
					SkinnedMeshRenderer val5 = (SkinnedMeshRenderer)(object)((val3 is SkinnedMeshRenderer) ? val3 : null);
					if (val5 != null && (UnityEngine.Object)(object)val5.sharedMesh != (UnityEngine.Object)null)
					{
						list3.Add(val5.sharedMesh);
					}
				}
			}
			list.Add(new LootItem
			{
				PhysGrabObject = item,
				ValuableObject = component,
				Position = ((Component)item).transform.position,
				Value = num2,
				Name = name,
				InCart = inCart,
				Renderers = list2,
				Meshes = list3
			});
		}
		LootItems = list;
	}

	public static void Cleanup()
	{
		LootItems.Clear();
	}
}
