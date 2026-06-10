using System;
using System.Reflection;
using Cheat.Config;
using UnityEngine;

namespace Cheat.Features.Visuals;

public class LaserBeamController : MonoBehaviour
{
	private LineRenderer _lineRenderer;

	private GameObject _hitMarker;

	private PlayerAvatar _player;

	private FieldInfo _grabbedField;

	public void Initialize(PlayerAvatar player)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Expected O, but got Unknown
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		_player = player;
		_grabbedField = typeof(PhysGrabber).GetField("grabbedPhysGrabObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		GameObject val = new GameObject("Cheat_LaserBeam");
		val.transform.SetParent(((Component)this).transform, false);
		val.transform.localPosition = Vector3.zero;
		_lineRenderer = val.AddComponent<LineRenderer>();
		_lineRenderer.useWorldSpace = true;
		_lineRenderer.startWidth = ConfigManager.Config.LaserSight.Width;
		_lineRenderer.endWidth = ConfigManager.Config.LaserSight.Width;
		_lineRenderer.positionCount = 2;
		((Renderer)_lineRenderer).material = new Material(Shader.Find("Sprites/Default"));
		_lineRenderer.startColor = ConfigManager.Config.LaserSight.Color;
		_lineRenderer.endColor = ConfigManager.Config.LaserSight.Color;
		((Renderer)_lineRenderer).enabled = false;
		_hitMarker = GameObject.CreatePrimitive((PrimitiveType)0);
		((UnityEngine.Object)_hitMarker).name = "Cheat_LaserHitMarker";
		_hitMarker.transform.SetParent(((Component)this).transform, false);
		_hitMarker.transform.localScale = Vector3.one * 0.1f;
		Collider component = _hitMarker.GetComponent<Collider>();
		if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)component);
		}
		Renderer component2 = _hitMarker.GetComponent<Renderer>();
		if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
		{
			component2.material = new Material(Shader.Find("Sprites/Default"));
			component2.material.color = ConfigManager.Config.LaserSight.Color;
		}
		_hitMarker.SetActive(false);
	}

	private void Update()
	{
		if ((UnityEngine.Object)(object)_player == (UnityEngine.Object)null || (UnityEngine.Object)(object)_lineRenderer == (UnityEngine.Object)null)
		{
			return;
		}
		if (!ConfigManager.Config.LaserSight.Enabled)
		{
			DisableVisuals();
			return;
		}
		bool flag = (UnityEngine.Object)(object)_player == (UnityEngine.Object)(object)PlayerAvatar.instance;
		if (flag && !ConfigManager.Config.LaserSight.ShowLocal)
		{
			DisableVisuals();
			return;
		}
		if (!flag && !ConfigManager.Config.LaserSight.ShowOthers)
		{
			DisableVisuals();
			return;
		}
		if ((UnityEngine.Object)(object)_player.physGrabber == (UnityEngine.Object)null)
		{
			DisableVisuals();
			return;
		}
		PhysGrabObject val = null;
		if (_grabbedField != null)
		{
			object value = _grabbedField.GetValue(_player.physGrabber);
			val = (PhysGrabObject)((value is PhysGrabObject) ? value : null);
		}
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			DisableVisuals();
		}
		else
		{
			UpdateLaser(((Component)val).transform);
		}
	}

	private void UpdateLaser(Transform origin)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		((Renderer)_lineRenderer).enabled = true;
		_lineRenderer.startWidth = ConfigManager.Config.LaserSight.Width;
		_lineRenderer.endWidth = ConfigManager.Config.LaserSight.Width;
		Color val = ConfigManager.Config.LaserSight.Color;
		Vector3 val2 = origin.position + origin.forward * 0.25f;
		Vector3 forward = origin.forward;
		Vector3 val3 = val2 + forward * 100f;
		int mask = LayerMask.GetMask(new string[5] { "Default", "StaticGrabObject", "Enemy", "Level", "PhysGrabObject" });
		RaycastHit[] array = Physics.RaycastAll(val2, forward, 100f, mask);
		Array.Sort(array, (RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance));
		bool flag = false;
		RaycastHit val4 = default(RaycastHit);
		RaycastHit[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			RaycastHit val5 = array2[num];
			if (!val5.collider.isTrigger && !((UnityEngine.Object)(object)val5.transform.root == (UnityEngine.Object)(object)((Component)_player).transform) && !(val5.distance < 0.1f))
			{
				val4 = val5;
				flag = true;
				if (((Component)val5.collider).gameObject.layer == LayerMask.NameToLayer("Enemy") || (UnityEngine.Object)(object)((Component)val5.collider).GetComponentInParent<EnemyParent>() != (UnityEngine.Object)null)
				{
					val = Color.green;
				}
				break;
			}
		}
		_lineRenderer.startColor = val;
		_lineRenderer.endColor = val;
		if ((UnityEngine.Object)(object)_hitMarker != (UnityEngine.Object)null)
		{
			Renderer component = _hitMarker.GetComponent<Renderer>();
			if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
			{
				component.material.color = val;
			}
		}
		if (flag)
		{
			val3 = val4.point;
			if ((UnityEngine.Object)(object)_hitMarker != (UnityEngine.Object)null)
			{
				_hitMarker.SetActive(true);
				_hitMarker.transform.position = val4.point;
			}
			LaserSight.SetHitInfo(((UnityEngine.Object)_player).GetInstanceID(), ((UnityEngine.Object)((Component)val4.collider).gameObject).name, val4.point);
		}
		else
		{
			if ((UnityEngine.Object)(object)_hitMarker != (UnityEngine.Object)null)
			{
				_hitMarker.SetActive(false);
			}
			LaserSight.ClearHitInfo(((UnityEngine.Object)_player).GetInstanceID());
		}
		_lineRenderer.SetPosition(0, val2);
		_lineRenderer.SetPosition(1, val3);
	}

	private void DisableVisuals()
	{
		if ((UnityEngine.Object)(object)_lineRenderer != (UnityEngine.Object)null)
		{
			((Renderer)_lineRenderer).enabled = false;
		}
		if ((UnityEngine.Object)(object)_hitMarker != (UnityEngine.Object)null)
		{
			_hitMarker.SetActive(false);
		}
		LaserSight.ClearHitInfo(((UnityEngine.Object)_player).GetInstanceID());
	}

	private void OnDestroy()
	{
		if ((UnityEngine.Object)(object)_lineRenderer != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)_lineRenderer).gameObject);
		}
		if ((UnityEngine.Object)(object)_hitMarker != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_hitMarker);
		}
	}
}
