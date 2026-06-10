using System;
using System.Collections.Generic;
using System.Reflection;
using Cheat.Config;
using Cheat.Features.Enemies;
using Cheat.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cheat.Features.Minimap;

public class Minimap : MonoBehaviour
{
	public static Minimap Instance;

	private Camera _minimapCamera;

	private RenderTexture _renderTexture;

	private GameObject _cameraObject;

	private float _padding = 20f;

	private bool _initialized = false;

	private bool _levelBoundsCalculated = false;

	private Bounds _cachedBounds;

	private Dictionary<int, LineRenderer> _paths = new Dictionary<int, LineRenderer>();

	private Material _glowMaterial;

	private Material _pathMaterial;

	private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();

	private Light _minimapLight;

	private bool _isDragging = false;

	private Vector2 _dragOffset;

	private bool _isResizing = false;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		SetupCamera();
	}

	private void SetupCamera()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Expected O, but got Unknown
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		if (!((UnityEngine.Object)(object)_cameraObject != (UnityEngine.Object)null) && ConfigManager.Config.Minimap.Enabled)
		{
			_cameraObject = new GameObject("MinimapCamera");
			_cameraObject.transform.SetParent(((Component)this).transform);
			_minimapCamera = _cameraObject.AddComponent<Camera>();
			_cameraObject.AddComponent<MinimapRenderHook>();
			_minimapCamera.orthographic = true;
			_minimapCamera.orthographicSize = 20f;
			_minimapCamera.clearFlags = (CameraClearFlags)2;
			_minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
			_minimapCamera.farClipPlane = 1000f;
			_renderTexture = new RenderTexture(512, 512, 16, (RenderTextureFormat)0);
			_renderTexture.Create();
			_minimapCamera.targetTexture = _renderTexture;
			int mask = LayerMask.GetMask(new string[5] { "Default", "Enemy", "Level", "PhysGrabObject", "RoomVolume" });
			_minimapCamera.cullingMask = mask;
			GameObject val = new GameObject("MinimapLight");
			val.transform.SetParent(_cameraObject.transform);
			val.transform.localPosition = Vector3.zero;
			val.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
			_minimapLight = val.AddComponent<Light>();
			_minimapLight.type = (LightType)1;
			_minimapLight.intensity = 5f;
			_minimapLight.color = Color.white;
			_minimapLight.cullingMask = mask;
			_minimapLight.shadows = (LightShadows)0;
			((Behaviour)_minimapLight).enabled = false;
			_cameraObject.transform.position = new Vector3(0f, 100f, 0f);
			_cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
			_initialized = true;
		}
	}

	private void Update()
	{
		try
		{
			if (ConfigManager.Config.Minimap.Enabled && !_initialized)
			{
				SetupCamera();
			}
			else if (!ConfigManager.Config.Minimap.Enabled && _initialized)
			{
				if ((UnityEngine.Object)(object)_minimapCamera != (UnityEngine.Object)null)
				{
					((Behaviour)_minimapCamera).enabled = false;
				}
			}
			else if (ConfigManager.Config.Minimap.Enabled && (UnityEngine.Object)(object)_minimapCamera != (UnityEngine.Object)null)
			{
				((Behaviour)_minimapCamera).enabled = true;
			}
			if (_initialized && !((UnityEngine.Object)(object)_minimapCamera == (UnityEngine.Object)null))
			{
				UpdateCameraPosition();
				UpdatePaths();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[Minimap Update Error] " + ex.Message);
		}
	}

	private void UpdateCameraPosition()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)LevelGenerator.Instance != (UnityEngine.Object)null && !LevelGenerator.Instance.Generated)
		{
			_levelBoundsCalculated = false;
		}
		Vector3 val = Vector3.zero;
		bool flag = false;
		if (ConfigManager.Config.Minimap.AutoCenter && (UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
		{
			val = ((Component)PlayerController.instance).transform.position;
			flag = true;
		}
		else if ((UnityEngine.Object)(object)LevelGenerator.Instance != (UnityEngine.Object)null && LevelGenerator.Instance.Generated)
		{
			if (!_levelBoundsCalculated)
			{
				CalculateLevelBounds();
			}
			if (_levelBoundsCalculated)
			{
				val = _cachedBounds.center;
				flag = true;
			}
		}
		else if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
		{
			val = ((Component)PlayerController.instance).transform.position;
			flag = true;
		}
		if (flag)
		{
			if (ConfigManager.Config.Minimap.AutoCenter)
			{
				_cameraObject.transform.position = new Vector3(val.x, val.y + 100f, val.z);
			}
			else if ((UnityEngine.Object)(object)LevelGenerator.Instance != (UnityEngine.Object)null && LevelGenerator.Instance.Generated && _levelBoundsCalculated)
			{
				Vector3 center = _cachedBounds.center;
				_cameraObject.transform.position = new Vector3(center.x, _cachedBounds.max.y + 100f, center.z);
			}
			float num = 20f;
			if ((UnityEngine.Object)(object)LevelGenerator.Instance != (UnityEngine.Object)null && LevelGenerator.Instance.Generated && _levelBoundsCalculated)
			{
				float num2 = Mathf.Max(_cachedBounds.size.x, _cachedBounds.size.z);
				num = num2 / 2f * 1.1f;
			}
			float num3 = Mathf.Clamp(ConfigManager.Config.Minimap.Zoom, 0.1f, 10f);
			_minimapCamera.orthographicSize = num / num3;
			_minimapCamera.farClipPlane = 200f;
			if (_levelBoundsCalculated)
			{
				float num4 = _cameraObject.transform.position.y - _cachedBounds.min.y;
				_minimapCamera.farClipPlane = num4 + 100f;
			}
		}
	}

	private void CalculateLevelBounds()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)LevelGenerator.Instance.LevelParent == (UnityEngine.Object)null)
		{
			return;
		}
		Bounds bounds = default(Bounds);
		bounds = new Bounds(Vector3.zero, Vector3.zero);
		bool flag = false;
		Renderer[] componentsInChildren = LevelGenerator.Instance.LevelParent.GetComponentsInChildren<Renderer>();
		Renderer[] array = componentsInChildren;
		foreach (Renderer val in array)
		{
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
			{
				if (flag)
				{
					bounds.Encapsulate(val.bounds);
					continue;
				}
				bounds = val.bounds;
				flag = true;
			}
		}
		if (flag)
		{
			_cachedBounds = bounds;
			_levelBoundsCalculated = true;
		}
	}

	private void OnGUI()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (_initialized && !((UnityEngine.Object)(object)_renderTexture == (UnityEngine.Object)null) && ConfigManager.Config.Minimap.Enabled)
			{
				float size = ConfigManager.Config.Minimap.Size;
				Vector2 position = ConfigManager.Config.Minimap.Position;
				if (position.x == -1f && position.y == -1f)
				{
					position.x = (float)Screen.width - size - _padding;
					position.y = (float)Screen.height - size - _padding;
				}
				Rect val = default(Rect);
				val = new Rect(position.x, position.y, size, size);
				if (Cheat.Config.ConfigManager.IsMenuVisible)
				{
					HandleInput(val);
				}
				Render.DrawBox(new Rect(val.x - 2f, val.y - 2f, val.width + 4f, val.height + 4f), Color.black);
				Render.DrawBox(val, new Color(0f, 0f, 0f, 0.5f));
				GUI.DrawTexture(val, (Texture)(object)_renderTexture, (ScaleMode)2, false);
				if (ConfigManager.Config.Minimap.ShowIcons && ConfigManager.Config.Minimap.RenderMode == 0)
				{
					DrawEntities(val);
				}
				Render.DrawString(new Rect(val.x, val.y - 20f, val.width, 20f), "SPECTATOR MAP", Color.white, center: true);
				if (Cheat.Config.ConfigManager.IsMenuVisible)
				{
					Render.DrawBox(new Rect(val.x + val.width - 10f, val.y + val.height - 10f, 10f, 10f), Color.white);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[Minimap OnGUI Error] " + ex.Message);
		}
	}

	private void HandleInput(Rect rect)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Invalid comparison between Unknown and I4
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Invalid comparison between Unknown and I4
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		Event current = Event.current;
		Vector2 mousePosition = current.mousePosition;
		if (rect.Contains(mousePosition) && (int)current.type == 6)
		{
			float y = current.delta.y;
			if (y > 0f)
			{
				ConfigManager.Config.Minimap.Zoom -= ConfigManager.Config.Minimap.ZoomSpeed;
			}
			else if (y < 0f)
			{
				ConfigManager.Config.Minimap.Zoom += ConfigManager.Config.Minimap.ZoomSpeed;
			}
			ConfigManager.Config.Minimap.Zoom = Mathf.Clamp(ConfigManager.Config.Minimap.Zoom, 0.1f, 10f);
			current.Use();
		}
		Rect val = default(Rect);
		val = new Rect(rect.x + rect.width - 15f, rect.y + rect.height - 15f, 15f, 15f);
		if ((int)current.type == 0 && current.button == 0)
		{
			if (val.Contains(mousePosition))
			{
				_isResizing = true;
			}
			else if (rect.Contains(mousePosition))
			{
				_isDragging = true;
				_dragOffset = mousePosition - new Vector2(rect.x, rect.y);
			}
		}
		else if ((int)current.type == 1)
		{
			_isDragging = false;
			_isResizing = false;
		}
		else if ((int)current.type == 3)
		{
			if (_isResizing)
			{
				float size = Mathf.Max(100f, mousePosition.x - rect.x);
				ConfigManager.Config.Minimap.Size = size;
			}
			else if (_isDragging)
			{
				ConfigManager.Config.Minimap.Position = mousePosition - _dragOffset;
			}
		}
	}

	private void DrawEntities(Rect mapRect)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)_minimapCamera == (UnityEngine.Object)null)
		{
			return;
		}
		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
		{
			DrawEntityOnMap(((Component)PlayerController.instance).transform.position, Color.green, 8f, mapRect);
		}
		if ((UnityEngine.Object)(object)GameDirector.instance != (UnityEngine.Object)null)
		{
			foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
			{
				if ((UnityEngine.Object)(object)player != (UnityEngine.Object)null && (UnityEngine.Object)(object)player != (UnityEngine.Object)(object)PlayerController.instance.playerAvatarScript)
				{
					DrawEntityOnMap(((Component)player).transform.position, Color.cyan, 8f, mapRect);
				}
			}
		}
		if (!((UnityEngine.Object)(object)EnemyDirector.instance != (UnityEngine.Object)null))
		{
			return;
		}
		foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
		{
			if ((UnityEngine.Object)(object)item != (UnityEngine.Object)null)
			{
				EnemyManager.EnemyData enemyData = EnemyManager.GetEnemyData(item);
				if (enemyData != null && (UnityEngine.Object)(object)enemyData.Enemy != (UnityEngine.Object)null)
				{
					DrawEntityOnMap(((Component)enemyData.Enemy).transform.position, Color.red, 6f, mapRect);
				}
			}
		}
	}

	private bool IsPointInMap(Vector3 viewportPos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		return viewportPos.x >= 0f && viewportPos.x <= 1f && viewportPos.y >= 0f && viewportPos.y <= 1f;
	}

	private Vector2 ViewportToScreen(Vector3 viewportPos, Rect mapRect)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		float num = mapRect.x + viewportPos.x * mapRect.width;
		float num2 = mapRect.y + (1f - viewportPos.y) * mapRect.height;
		return new Vector2(num, num2);
	}

	private void DrawEntityOnMap(Vector3 worldPos, Color color, float size, Rect mapRect)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		Vector3 viewportPos = _minimapCamera.WorldToViewportPoint(worldPos);
		if (IsPointInMap(viewportPos))
		{
			Vector2 center = ViewportToScreen(viewportPos, mapRect);
			Render.DrawCircle(center, size / 2f, color);
		}
	}

	public void OnPreMinimapRender()
	{
		if ((UnityEngine.Object)(object)_minimapLight != (UnityEngine.Object)null)
		{
			((Behaviour)_minimapLight).enabled = true;
		}
		if (ConfigManager.Config.Minimap.RenderMode == 2)
		{
			ApplyGlowMaterials();
		}
		if (!ConfigManager.Config.Minimap.ShowPath)
		{
			return;
		}
		foreach (KeyValuePair<int, LineRenderer> path in _paths)
		{
			if ((UnityEngine.Object)(object)path.Value != (UnityEngine.Object)null)
			{
				((Component)path.Value).gameObject.SetActive(true);
			}
		}
	}

	public void OnPostMinimapRender()
	{
		if ((UnityEngine.Object)(object)_minimapLight != (UnityEngine.Object)null)
		{
			((Behaviour)_minimapLight).enabled = false;
		}
		if (ConfigManager.Config.Minimap.RenderMode == 2)
		{
			RestoreMaterials();
		}
		foreach (KeyValuePair<int, LineRenderer> path in _paths)
		{
			if ((UnityEngine.Object)(object)path.Value != (UnityEngine.Object)null)
			{
				((Component)path.Value).gameObject.SetActive(false);
			}
		}
	}

	private void ApplyGlowMaterials()
	{
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		_originalMaterials.Clear();
		Material glowMaterial = GetGlowMaterial();
		if ((UnityEngine.Object)(object)GameDirector.instance != (UnityEngine.Object)null)
		{
			FieldInfo field = typeof(PlayerAvatar).GetField("isLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
			{
				bool flag = field != null && (bool)field.GetValue(player);
				if ((UnityEngine.Object)(object)player != (UnityEngine.Object)null && !flag)
				{
					ApplyGlowToRenderers(((Component)player).GetComponentsInChildren<Renderer>(), Color.green);
				}
			}
		}
		if (!((UnityEngine.Object)(object)EnemyDirector.instance != (UnityEngine.Object)null))
		{
			return;
		}
		foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
		{
			if ((UnityEngine.Object)(object)item != (UnityEngine.Object)null)
			{
				EnemyManager.EnemyData enemyData = EnemyManager.GetEnemyData(item);
				if (enemyData != null && enemyData.Renderers != null)
				{
					ApplyGlowToRenderers(enemyData.Renderers.ToArray(), ConfigManager.Config.Minimap.RingColor);
				}
			}
		}
	}

	private void ApplyGlowToRenderers(Renderer[] renderers, Color color)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		Material glowMaterial = GetGlowMaterial();
		glowMaterial.color = color;
		foreach (Renderer val in renderers)
		{
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
			{
				_originalMaterials[val] = val.sharedMaterials;
				val.sharedMaterials = (Material[])(object)new Material[1] { glowMaterial };
			}
		}
	}

	private void RestoreMaterials()
	{
		foreach (KeyValuePair<Renderer, Material[]> originalMaterial in _originalMaterials)
		{
			if ((UnityEngine.Object)(object)originalMaterial.Key != (UnityEngine.Object)null)
			{
				originalMaterial.Key.sharedMaterials = originalMaterial.Value;
			}
		}
		_originalMaterials.Clear();
	}

	private void UpdatePaths()
	{
		if (!ConfigManager.Config.Minimap.ShowPath)
		{
			foreach (KeyValuePair<int, LineRenderer> path in _paths)
			{
				if ((UnityEngine.Object)(object)path.Value != (UnityEngine.Object)null)
				{
					UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)path.Value).gameObject);
				}
			}
			_paths.Clear();
		}
		else
		{
			if ((UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null)
			{
				return;
			}
			List<int> list = new List<int>();
			foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
			{
				if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null)
				{
					continue;
				}
				EnemyManager.EnemyData enemyData = EnemyManager.GetEnemyData(item);
				if (enemyData != null && (UnityEngine.Object)(object)enemyData.Agent != (UnityEngine.Object)null && enemyData.Agent.hasPath)
				{
					int instanceID = ((UnityEngine.Object)item).GetInstanceID();
					list.Add(instanceID);
					if (!_paths.TryGetValue(instanceID, out var value))
					{
						value = CreatePathRenderer();
						_paths[instanceID] = value;
					}
					if ((UnityEngine.Object)(object)value != (UnityEngine.Object)null)
					{
						value.positionCount = enemyData.Agent.path.corners.Length;
						value.SetPositions(enemyData.Agent.path.corners);
						((Component)value).gameObject.SetActive(false);
					}
				}
			}
			List<int> list2 = new List<int>();
			foreach (KeyValuePair<int, LineRenderer> path2 in _paths)
			{
				if (!list.Contains(path2.Key))
				{
					list2.Add(path2.Key);
					if ((UnityEngine.Object)(object)path2.Value != (UnityEngine.Object)null)
					{
						UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)path2.Value).gameObject);
					}
				}
			}
			foreach (int item2 in list2)
			{
				_paths.Remove(item2);
			}
		}
	}

	private LineRenderer CreatePathRenderer()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("MinimapPath");
		val.transform.SetParent(((Component)this).transform);
		LineRenderer val2 = val.AddComponent<LineRenderer>();
		val2.useWorldSpace = true;
		val2.startWidth = 0.5f;
		val2.endWidth = 0.5f;
		((Renderer)val2).material = GetPathMaterial();
		val2.startColor = Color.yellow;
		val2.endColor = Color.yellow;
		((Renderer)val2).shadowCastingMode = (ShadowCastingMode)0;
		((Renderer)val2).receiveShadows = false;
		return val2;
	}

	private Material GetGlowMaterial()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		if ((UnityEngine.Object)(object)_glowMaterial == (UnityEngine.Object)null)
		{
			Shader val = Shader.Find("Unlit/Color");
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				val = Shader.Find("GUI/Text Shader");
			}
			_glowMaterial = new Material(val);
		}
		return _glowMaterial;
	}

	private Material GetPathMaterial()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		if ((UnityEngine.Object)(object)_pathMaterial == (UnityEngine.Object)null)
		{
			Shader val = Shader.Find("Sprites/Default");
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				val = Shader.Find("GUI/Text Shader");
			}
			_pathMaterial = new Material(val);
		}
		return _pathMaterial;
	}

	public void OnDestroy()
	{
		if ((UnityEngine.Object)(object)_renderTexture != (UnityEngine.Object)null)
		{
			_renderTexture.Release();
		}
		if ((UnityEngine.Object)(object)_cameraObject != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_cameraObject);
		}
		foreach (KeyValuePair<int, LineRenderer> path in _paths)
		{
			if ((UnityEngine.Object)(object)path.Value != (UnityEngine.Object)null)
			{
				UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)path.Value).gameObject);
			}
		}
		_paths.Clear();
		RestoreMaterials();
	}
}
