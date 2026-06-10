using Cheat.Config;
using Cheat.Utils;
using UnityEngine;

namespace Cheat.Features.Enemies;

public class EnemyEspLineRenderer : MonoBehaviour
{
	private static readonly Vector3 EnemyFallbackSize = new Vector3(1.1f, 2.2f, 1.1f);

	private LineRenderer _lineRenderer;

	private EnemyManager.EnemyData _data;

	private Camera _mainCamera;

	private GameObject _boxObj;

	public void Initialize(EnemyManager.EnemyData data)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		_data = data;
		_mainCamera = Camera.main;
		if ((UnityEngine.Object)(object)_boxObj == (UnityEngine.Object)null)
		{
			_boxObj = new GameObject("Cheat_ESP_Box");
			_boxObj.transform.SetParent(((Component)this).transform, false);
			_boxObj.transform.localPosition = Vector3.zero;
			_lineRenderer = _boxObj.AddComponent<LineRenderer>();
			_lineRenderer.useWorldSpace = true;
			_lineRenderer.startWidth = 0.05f;
			_lineRenderer.endWidth = 0.05f;
			_lineRenderer.positionCount = 16;
			((Renderer)_lineRenderer).material = new Material(Shader.Find("Sprites/Default"));
			_lineRenderer.startColor = ConfigManager.Config.Enemies.EspColor;
			_lineRenderer.endColor = ConfigManager.Config.Enemies.EspColor;
		}
	}

	private void Update()
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		if (_data == null || !EnemyManager.IsEnemyActive(_data) || (UnityEngine.Object)(object)_data.Enemy == (UnityEngine.Object)null || (UnityEngine.Object)(object)_lineRenderer == (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)this);
			return;
		}
		if (ConfigManager.Config.Enemies.RenderMethod != 2)
		{
			((Renderer)_lineRenderer).enabled = false;
			return;
		}
		((Renderer)_lineRenderer).enabled = true;
		_lineRenderer.startColor = ConfigManager.Config.Enemies.EspColor;
		_lineRenderer.endColor = ConfigManager.Config.Enemies.EspColor;
		Bounds bounds = EspProjectionUtils.GetWorldBounds(((Component)this).transform, _data.Renderers, ShouldUseEnemyRenderer, ((Component)this).transform.position + Vector3.up, EnemyFallbackSize);
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		Vector3 val = default(Vector3);
		val = new Vector3(min.x, min.y, min.z);
		Vector3 val2 = default(Vector3);
		val2 = new Vector3(max.x, min.y, min.z);
		Vector3 val3 = default(Vector3);
		val3 = new Vector3(max.x, min.y, max.z);
		Vector3 val4 = default(Vector3);
		val4 = new Vector3(min.x, min.y, max.z);
		Vector3 val5 = default(Vector3);
		val5 = new Vector3(min.x, max.y, min.z);
		Vector3 val6 = default(Vector3);
		val6 = new Vector3(max.x, max.y, min.z);
		Vector3 val7 = default(Vector3);
		val7 = new Vector3(max.x, max.y, max.z);
		Vector3 val8 = default(Vector3);
		val8 = new Vector3(min.x, max.y, max.z);
		Vector3[] array = (Vector3[])(object)new Vector3[16]
		{
			val, val2, val3, val4, val, val5, val6, val7, val8, val5,
			val6, val2, val3, val7, val8, val4
		};
		_lineRenderer.positionCount = array.Length;
		_lineRenderer.SetPositions(array);
	}

	private static bool ShouldUseEnemyRenderer(Renderer renderer)
	{
		return !((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null) && renderer.enabled;
	}

	private void OnDestroy()
	{
		if ((UnityEngine.Object)(object)_boxObj != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_boxObj);
		}
	}
}
