using System;
using Cheat.Config;
using UnityEngine;

namespace Cheat.Features.Enemies;

public class EnemyPreview
{
	private static RenderTexture _previewTexture;

	private static Camera _previewCamera;

	private static GameObject _previewRoot;

	private static GameObject _dummyModel;

	private static Light _previewLight;

	private static float _rotation = 0f;

	private static bool _isInitialized = false;

	private static readonly Vector3 PreviewLocation = new Vector3(0f, -2000f, 0f);

	public static Bounds CurrentBounds { get; private set; }

	public static bool HasTarget => (UnityEngine.Object)(object)_dummyModel != (UnityEngine.Object)null;

	public static void Initialize()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected O, but got Unknown
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		if (_isInitialized)
		{
			return;
		}
		try
		{
			_previewTexture = new RenderTexture(256, 256, 16, (RenderTextureFormat)0);
			_previewTexture.Create();
			_previewRoot = new GameObject("CheatEnemyPreviewRoot");
			_previewRoot.transform.position = PreviewLocation;
			UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)(object)_previewRoot);
			GameObject val = new GameObject("PreviewCamera");
			val.transform.SetParent(_previewRoot.transform);
			val.transform.localPosition = new Vector3(0f, 1f, -2.5f);
			_previewCamera = val.AddComponent<Camera>();
			_previewCamera.targetTexture = _previewTexture;
			_previewCamera.clearFlags = (CameraClearFlags)2;
			_previewCamera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
			_previewCamera.cullingMask = int.MinValue;
			_previewCamera.fieldOfView = 40f;
			_previewCamera.nearClipPlane = 0.1f;
			_previewCamera.farClipPlane = 100f;
			((Behaviour)_previewCamera).enabled = false;
			GameObject val2 = new GameObject("PreviewLight");
			val2.transform.SetParent(_previewRoot.transform);
			val2.transform.localPosition = new Vector3(0f, 3f, -2f);
			val2.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);
			_previewLight = val2.AddComponent<Light>();
			_previewLight.type = (LightType)1;
			_previewLight.color = Color.white;
			_previewLight.intensity = 1.2f;
			_previewLight.cullingMask = int.MinValue;
			_isInitialized = true;
			Console.WriteLine("[EnemyPreview] Initialized");
		}
		catch (Exception ex)
		{
			Console.WriteLine("[EnemyPreview] Failed to initialize: " + ex.Message);
		}
	}

	public static void Cleanup()
	{
		if ((UnityEngine.Object)(object)_previewTexture != (UnityEngine.Object)null)
		{
			_previewTexture.Release();
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_previewTexture);
			_previewTexture = null;
		}
		if ((UnityEngine.Object)(object)_previewRoot != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_previewRoot);
			_previewRoot = null;
		}
		_previewCamera = null;
		_dummyModel = null;
		_previewLight = null;
		_isInitialized = false;
	}

	public static void SetPreviewTarget(EnemyParent enemy)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected O, but got Unknown
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Expected O, but got Unknown
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Expected O, but got Unknown
		if (!_isInitialized)
		{
			Initialize();
		}
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null || (UnityEngine.Object)(object)_previewRoot == (UnityEngine.Object)null)
		{
			return;
		}
		try
		{
			if ((UnityEngine.Object)(object)_dummyModel != (UnityEngine.Object)null)
			{
				UnityEngine.Object.Destroy((UnityEngine.Object)(object)_dummyModel);
			}
			_dummyModel = new GameObject("DummyModel");
			((UnityEngine.Object)_dummyModel).name = ((UnityEngine.Object)enemy).name + "_Preview";
			_dummyModel.transform.SetParent(_previewRoot.transform);
			_dummyModel.transform.localPosition = Vector3.zero;
			_dummyModel.layer = 31;
			Renderer[] componentsInChildren = ((Component)enemy).GetComponentsInChildren<Renderer>(true);
			Renderer[] array = componentsInChildren;
			foreach (Renderer val in array)
			{
				Mesh val2 = null;
				SkinnedMeshRenderer val3 = (SkinnedMeshRenderer)(object)((val is SkinnedMeshRenderer) ? val : null);
				if (val3 != null)
				{
					val2 = new Mesh();
					val3.BakeMesh(val2);
				}
				else
				{
					MeshRenderer val4 = (MeshRenderer)(object)((val is MeshRenderer) ? val : null);
					if (val4 != null)
					{
						MeshFilter component = ((Component)val).GetComponent<MeshFilter>();
						if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
						{
							val2 = component.sharedMesh;
						}
					}
				}
				if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
				{
					GameObject val5 = new GameObject(((UnityEngine.Object)val).name);
					val5.transform.SetParent(_dummyModel.transform);
					Vector3 localPosition = ((Component)enemy).transform.InverseTransformPoint(((Component)val).transform.position);
					Quaternion localRotation = Quaternion.Inverse(((Component)enemy).transform.rotation) * ((Component)val).transform.rotation;
					val5.transform.localPosition = localPosition;
					val5.transform.localRotation = localRotation;
					val5.transform.localScale = ((Component)val).transform.lossyScale;
					val5.layer = 31;
					MeshFilter val6 = val5.AddComponent<MeshFilter>();
					val6.sharedMesh = val2;
					MeshRenderer val7 = val5.AddComponent<MeshRenderer>();
					if ((UnityEngine.Object)(object)EnemyChams.ChamsMaterial != (UnityEngine.Object)null && ConfigManager.Config.Enemies.HighlightEnabled)
					{
						((Renderer)val7).material = EnemyChams.ChamsMaterial;
						continue;
					}
					Material material = new Material(Shader.Find("Standard"));
					((Renderer)val7).material = material;
				}
			}
			Bounds bounds = default(Bounds);
			bounds = new Bounds(Vector3.zero, Vector3.zero);
			bool flag = false;
			Renderer[] componentsInChildren2 = _dummyModel.GetComponentsInChildren<Renderer>();
			Renderer[] array2 = componentsInChildren2;
			foreach (Renderer val8 in array2)
			{
				if (!flag)
				{
					bounds = val8.bounds;
					flag = true;
				}
				else
				{
					bounds.Encapsulate(val8.bounds);
				}
			}
			CurrentBounds = bounds;
			if (flag && (UnityEngine.Object)(object)_previewCamera != (UnityEngine.Object)null)
			{
				Vector3 center = bounds.center;
				Vector3 val9 = bounds.extents;
				float magnitude = val9.magnitude;
				float y = bounds.size.y;
				float num = magnitude * 2f / Mathf.Sin((float)Math.PI / 180f * _previewCamera.fieldOfView / 2f);
				num *= 1.1f;
				num = Mathf.Max(num, 1.5f);
				val9 = new Vector3(0f, 0.2f, -1f);
				Vector3 normalized = val9.normalized;
				((Component)_previewCamera).transform.position = center - normalized * num;
				((Component)_previewCamera).transform.LookAt(center);
			}
			_rotation = 0f;
		}
		catch (Exception ex)
		{
			Console.WriteLine("[EnemyPreview] Error setting target: " + ex.Message);
		}
	}

	public static void Update()
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		if (!_isInitialized)
		{
			Initialize();
		}
		if (Time.frameCount % 60 == 0)
		{
			EnemyParent nearestEnemy = GetNearestEnemy();
			if ((UnityEngine.Object)(object)nearestEnemy != (UnityEngine.Object)null && ((UnityEngine.Object)(object)_dummyModel == (UnityEngine.Object)null || ((UnityEngine.Object)_dummyModel).name != ((UnityEngine.Object)nearestEnemy).name + "_Preview"))
			{
				SetPreviewTarget(nearestEnemy);
			}
		}
		if ((UnityEngine.Object)(object)_dummyModel == (UnityEngine.Object)null || (UnityEngine.Object)(object)_previewCamera == (UnityEngine.Object)null)
		{
			return;
		}
		Bounds currentBounds = CurrentBounds;
		if (currentBounds.size != Vector3.zero)
		{
			_dummyModel.transform.localRotation = Quaternion.Euler(0f, _rotation, 0f);
		}
		else
		{
			_dummyModel.transform.localRotation = Quaternion.Euler(0f, _rotation, 0f);
		}
		if ((UnityEngine.Object)(object)EnemyChams.ChamsMaterial != (UnityEngine.Object)null && ConfigManager.Config.Enemies.HighlightEnabled)
		{
			Renderer[] componentsInChildren = _dummyModel.GetComponentsInChildren<Renderer>();
			Renderer[] array = componentsInChildren;
			foreach (Renderer val in array)
			{
				if ((UnityEngine.Object)(object)val.sharedMaterial != (UnityEngine.Object)(object)EnemyChams.ChamsMaterial)
				{
					val.sharedMaterial = EnemyChams.ChamsMaterial;
				}
			}
		}
		_previewCamera.Render();
	}

	public static Vector2 WorldToPreviewUV(Vector3 worldPos)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)_previewCamera == (UnityEngine.Object)null)
		{
			return Vector2.zero;
		}
		Vector3 val = _previewCamera.WorldToViewportPoint(worldPos);
		return new Vector2(val.x, 1f - val.y);
	}

	private static EnemyParent GetNearestEnemy()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null)
		{
			return null;
		}
		Vector3 val = Vector3.zero;
		if ((UnityEngine.Object)(object)Camera.main != (UnityEngine.Object)null)
		{
			val = ((Component)Camera.main).transform.position;
		}
		else if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
		{
			val = ((Component)PlayerController.instance).transform.position;
		}
		EnemyParent result = null;
		float num = float.MaxValue;
		foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
		{
			if (!((UnityEngine.Object)(object)item == (UnityEngine.Object)null))
			{
				float num2 = Vector3.Distance(val, ((Component)item).transform.position);
				if (num2 < num)
				{
					num = num2;
					result = item;
				}
			}
		}
		return result;
	}

	public static RenderTexture GetPreviewTexture()
	{
		if (!_isInitialized)
		{
			Initialize();
		}
		return _previewTexture;
	}

	public static void HandleMouseInput(float deltaX)
	{
		if ((UnityEngine.Object)(object)_dummyModel != (UnityEngine.Object)null)
		{
			_rotation -= deltaX * 2f;
		}
	}
}
