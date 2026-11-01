using System;
using System.Collections.Generic;
using System.Reflection;
using Cheat.Config;
using Cheat.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace Cheat.Features.Enemies;

public class EnemyManager
{
	private static readonly Vector3 EnemyBoundsFallbackSize = new Vector3(1.1f, 2.2f, 1.1f);

	private const float MaxBoundsRendererDistance = 6f;

	private const float MaxEnemyBoundsWidth = 5.5f;

	private const float MaxEnemyBoundsDepth = 5.5f;

	private const float MaxEnemyBoundsHeight = 7.5f;

	public class EnemyData
	{
		public EnemyParent Parent;

		public Enemy Enemy;

		public Vector3 SmoothedPosition;

		public Vector3 LastPosition;

		public float LastUpdateTime;

		public List<Renderer> Renderers = new List<Renderer>();

		public NavMeshAgent Agent;

		public EnemyHealth Health;

		public List<Mesh> Meshes = new List<Mesh>();

		public List<Transform> MeshTransforms = new List<Transform>();

		public List<Mesh> BakedMeshes = new List<Mesh>();

		public List<Component> Animators = new List<Component>();

		public PathVisualizer PathViz;

		public List<GameObject> ChamsObjects = new List<GameObject>();

		public float LastSeenInDirectorTime;

		public float LastVisibilityRefreshTime;

		public float LastVisualRefreshTime;

		public int CachedBoundsFrame = -1;

		public bool HasCachedBounds;

		public Bounds CachedBounds;

		public int CachedRectFrame = -1;

		public int CachedRectCameraId = -1;

		public int CachedRectScreenWidth;

		public int CachedRectScreenHeight;

		public float CachedRectMinWidth;

		public float CachedRectMinHeight;

		public bool HasCachedRect;

		public Rect CachedRect;

		public Rect SmoothedOutlineRect;

		public bool HasSmoothedOutlineRect;

		public int InvalidOutlineRectFrames;

		public float LastValidOutlineRectTime;

		public float NextOutlineVisibilityCheck;

		public bool OutlineFullyOccluded;

		public bool OutlineVisible;
	}

	private static Dictionary<int, EnemyData> _enemyCache = new Dictionary<int, EnemyData>();

	private static PropertyInfo _animatorCullingModeProp;

	private const float VisibilityRefreshInterval = 0.35f;

	private const float VisualRefreshInterval = 0.4f;

	public static void Update()
	{
		//IL_0484: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0504: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Expected O, but got Unknown
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0419: Expected O, but got Unknown
		if ((UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null)
		{
			return;
		}
		bool keepEnemyCacheAlive = ConfigManager.Config.Enemies.EspEnabled
			|| ConfigManager.Config.Compass.Enabled
			|| (ConfigManager.Config.Aimbot != null && ConfigManager.Config.Aimbot.Enabled);
		if (!keepEnemyCacheAlive)
		{
			if (_enemyCache.Count > 0)
			{
				Cleanup();
			}
			return;
		}
		HashSet<int> activeDirectorIds = new HashSet<int>();
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, EnemyData> item in _enemyCache)
		{
			if (ShouldRemoveEnemyData(item.Value))
			{
				list.Add(item.Key);
			}
		}
		foreach (int item2 in list)
		{
			if (_enemyCache.TryGetValue(item2, out var value2))
			{
				RemoveEnemyData(item2, value2);
			}
		}
		foreach (EnemyParent item3 in EnemyDirector.instance.enemiesSpawned)
		{
			if ((UnityEngine.Object)(object)item3 == (UnityEngine.Object)null)
			{
				continue;
			}
			int instanceID = ((UnityEngine.Object)item3).GetInstanceID();
			activeDirectorIds.Add(instanceID);
			EnemyData value = EnsureEnemyData(item3);
			if (value == null)
			{
				continue;
			}
			if (value != null)
			{
				value.Parent = item3;
				value.LastSeenInDirectorTime = Time.time;
				if (ShouldRemoveEnemyData(value))
				{
					RemoveEnemyData(instanceID, value);
					continue;
				}
				RefreshEnemyVisualState(value);
				Bounds bounds;
				Vector3 position = TryGetAccurateBounds(value, out bounds)
					? bounds.center
					: ((Component)value.Enemy).transform.position;
				if ((UnityEngine.Object)(object)value.Enemy.CenterTransform != (UnityEngine.Object)null && Vector3.Distance(value.Enemy.CenterTransform.position, ((Component)value.Enemy).transform.position) < 5f)
				{
					position = value.Enemy.CenterTransform.position;
				}
				value.SmoothedPosition = Vector3.Lerp(value.SmoothedPosition, position, Time.deltaTime * 15f);
			}
		}

		list.Clear();
		foreach (KeyValuePair<int, EnemyData> item4 in _enemyCache)
		{
			if (!activeDirectorIds.Contains(item4.Key))
			{
				list.Add(item4.Key);
			}
		}
		foreach (int item5 in list)
		{
			if (_enemyCache.TryGetValue(item5, out var value3))
			{
				RemoveEnemyData(item5, value3);
			}
		}
	}

	public static EnemyData GetEnemyData(EnemyParent parent)
	{
		if ((UnityEngine.Object)(object)parent == (UnityEngine.Object)null)
		{
			return null;
		}
		return EnsureEnemyData(parent);
	}

	public static EnemyData GetEnemyData(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return null;
		}

		foreach (KeyValuePair<int, EnemyData> item in _enemyCache)
		{
			if ((UnityEngine.Object)(object)item.Value?.Enemy == (UnityEngine.Object)(object)enemy)
			{
				return item.Value;
			}
		}

		return null;
	}

	public static bool IsEnemyActive(EnemyData data)
	{
		return !ShouldRemoveEnemyData(data);
	}

	public static bool TryGetAccurateBounds(EnemyData data, out Bounds bounds)
	{
		bounds = default(Bounds);
		if (data == null || (UnityEngine.Object)(object)data.Enemy == (UnityEngine.Object)null)
		{
			return false;
		}

		if (data.CachedBoundsFrame == Time.frameCount)
		{
			bounds = data.CachedBounds;
			return data.HasCachedBounds;
		}

		Transform root = ((UnityEngine.Object)(object)data.Parent != (UnityEngine.Object)null) ? ((Component)data.Parent).transform : ((Component)data.Enemy).transform;
		// Current renderer bounds track animated meshes. Collider bounds are fallbacks
		// for ragdoll/uninitialized renderers, rather than a competing oversized union.
		if (!TryGetPrimaryMeshBounds(data.Renderers, out bounds)
			&& !TryGetRagdollBounds(root, out bounds)
			&& !TryGetNearbyColliderBounds(root, EnemyVisibilityUtility.GetFocusPoint(data), out bounds))
		{
			data.CachedBoundsFrame = Time.frameCount;
			data.HasCachedBounds = false;
			data.CachedBounds = bounds;
			return false;
		}

		bool hasBounds = bounds.size.sqrMagnitude > 0.0001f;
		data.CachedBoundsFrame = Time.frameCount;
		data.HasCachedBounds = hasBounds;
		data.CachedBounds = bounds;
		return hasBounds;
	}

	public static bool TryGetAccurateScreenRect(Camera camera, EnemyData data, float minWidth, float minHeight, out Rect rect)
	{
		rect = default(Rect);
		if ((UnityEngine.Object)(object)camera == (UnityEngine.Object)null || data == null || (UnityEngine.Object)(object)data.Enemy == (UnityEngine.Object)null)
		{
			return false;
		}

		int cameraId = ((UnityEngine.Object)camera).GetInstanceID();
		if (data.CachedRectFrame == Time.frameCount
			&& data.CachedRectCameraId == cameraId
			&& data.CachedRectScreenWidth == Screen.width
			&& data.CachedRectScreenHeight == Screen.height
			&& Mathf.Approximately(data.CachedRectMinWidth, minWidth)
			&& Mathf.Approximately(data.CachedRectMinHeight, minHeight))
		{
			rect = data.CachedRect;
			return data.HasCachedRect;
		}

		Bounds accurateBounds;
		Transform root = ((UnityEngine.Object)(object)data.Parent != (UnityEngine.Object)null)
			? ((Component)data.Parent).transform
			: ((Component)data.Enemy).transform;
		bool hasBounds = TryGetAccurateBounds(data, out accurateBounds);
		Rect boundsRect;
		if (hasBounds && !EspProjectionUtils.TryGetScreenRect(camera, accurateBounds, 0f, 0f, out boundsRect))
		{
			data.HasSmoothedOutlineRect = false;
			data.InvalidOutlineRectFrames = 0;
			data.HasCachedRect = false;
			data.CachedRectFrame = Time.frameCount;
			return false;
		}
		Vector3 fallbackCenter = hasBounds ? accurateBounds.center : root.position;
		Vector3 fallbackSize = hasBounds ? accurateBounds.size : Vector3.zero;
		bool success = EspProjectionUtils.TryGetScreenRect(camera, root, data.Renderers, ShouldUseEnemyRenderer,
			fallbackCenter, fallbackSize, minWidth, minHeight, out rect);

		if (success)
		{
			ConfigData.EnemySettings settings = ConfigManager.Config.Enemies;
			float padding = Mathf.Max(0f, settings.BoxPaddingPixels);
			rect = Rect.MinMaxRect(rect.xMin - padding, rect.yMin - padding, rect.xMax + padding, rect.yMax + padding);
			float maxFraction = Mathf.Clamp(settings.BoxMaximumScreenFraction, 0.1f, 1f);
			float maxWidth = Mathf.Max(1f, Screen.width * maxFraction);
			float maxHeight = Mathf.Max(1f, Screen.height * maxFraction);
			float effectiveMinWidth = Mathf.Min(maxWidth, Mathf.Max(1f, settings.BoxMinimumWidth));
			float effectiveMinHeight = Mathf.Min(maxHeight, Mathf.Max(1f, settings.BoxMinimumHeight));
			float width = Mathf.Clamp(rect.width, effectiveMinWidth, maxWidth);
			float height = Mathf.Clamp(rect.height, effectiveMinHeight, maxHeight);
			rect = new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
			rect = ClipRectToScreen(rect);

			if (data.HasSmoothedOutlineRect && settings.BoxSmoothingSeconds > 0f)
			{
				float alpha = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.001f, settings.BoxSmoothingSeconds));
				Rect previous = data.SmoothedOutlineRect;
				float left = rect.xMin < previous.xMin ? rect.xMin : Mathf.Lerp(previous.xMin, rect.xMin, alpha);
				float top = rect.yMin < previous.yMin ? rect.yMin : Mathf.Lerp(previous.yMin, rect.yMin, alpha);
				float right = rect.xMax > previous.xMax ? rect.xMax : Mathf.Lerp(previous.xMax, rect.xMax, alpha);
				float bottom = rect.yMax > previous.yMax ? rect.yMax : Mathf.Lerp(previous.yMax, rect.yMax, alpha);
				rect = ClipRectToScreen(Rect.MinMaxRect(left, top, right, bottom));
			}

			data.SmoothedOutlineRect = rect;
			data.HasSmoothedOutlineRect = true;
			data.InvalidOutlineRectFrames = 0;
			data.LastValidOutlineRectTime = Time.unscaledTime;
		}
		else if (data.HasSmoothedOutlineRect && Time.unscaledTime - data.LastValidOutlineRectTime <= 0.2f)
		{
			// Briefly preserve the last valid rectangle while Unity swaps/initializes
			// renderers, then recover automatically when fresh bounds arrive.
			rect = data.SmoothedOutlineRect;
			data.InvalidOutlineRectFrames++;
			success = true;
		}

		data.CachedRectFrame = Time.frameCount;
		data.CachedRectCameraId = cameraId;
		data.CachedRectScreenWidth = Screen.width;
		data.CachedRectScreenHeight = Screen.height;
		data.CachedRectMinWidth = minWidth;
		data.CachedRectMinHeight = minHeight;
		data.HasCachedRect = success;
		data.CachedRect = rect;
		return success;
	}

	private static Rect ClipRectToScreen(Rect rect)
	{
		float left = Mathf.Clamp(rect.xMin, 0f, Screen.width);
		float top = Mathf.Clamp(rect.yMin, 0f, Screen.height);
		float right = Mathf.Clamp(rect.xMax, left, Screen.width);
		float bottom = Mathf.Clamp(rect.yMax, top, Screen.height);
		return Rect.MinMaxRect(left, top, right, bottom);
	}

	public static Vector3 GetTargetPoint(EnemyData data, Bounds bounds)
	{
		// Aim at a live weakpoint or body collider, independently of the ESP bounds center.
		if (data != null && (UnityEngine.Object)(object)data.Enemy != (UnityEngine.Object)null)
		{
			Transform root = ((Component)data.Enemy).transform;
			Transform[] points = root.GetComponentsInChildren<Transform>(true);
			foreach (Transform point in points)
			{
				string pointName = point.name.ToLowerInvariant();
				if ((pointName.Contains("weak") || pointName.Contains("core") || pointName.Contains("head"))
					&& (point.position - bounds.center).sqrMagnitude <= 64f)
				{
					return point.position;
				}
			}

			if ((UnityEngine.Object)(object)data.Enemy.CenterTransform != (UnityEngine.Object)null
				&& (data.Enemy.CenterTransform.position - bounds.center).sqrMagnitude > 0.0001f)
				return data.Enemy.CenterTransform.position;

			Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
			Vector3 bestPoint = root.position;
			float bestDistance = float.MaxValue;
			Vector2 crosshair = MathUtils.GetScreenCenter();
			Camera camera = Camera.main;
			foreach (Collider collider in colliders)
			{
				if ((UnityEngine.Object)(object)collider == (UnityEngine.Object)null || !collider.enabled || collider.isTrigger)
					continue;
				Vector3 candidate = collider.bounds.center;
				if ((candidate - bounds.center).sqrMagnitude <= 0.0001f)
					continue;
				Vector3 screen = camera != null ? MathUtils.WorldToScreen(camera, candidate) : Vector3.zero;
				float distance = camera != null && screen.z > 0.05f
					? (new Vector2(screen.x, screen.y) - crosshair).sqrMagnitude
					: (candidate - bounds.center).sqrMagnitude;
				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestPoint = candidate;
				}
			}
			if ((bestPoint - bounds.center).sqrMagnitude > 0.0001f)
				return bestPoint;
			return root.position;
		}

		return bounds.center + Vector3.up * Mathf.Max(bounds.size.y * 0.15f, 0.01f);
	}

	public static void Cleanup()
	{
		List<int> list = new List<int>(_enemyCache.Keys);
		foreach (int item in list)
		{
			if (_enemyCache.TryGetValue(item, out var value))
			{
				RemoveEnemyData(item, value);
			}
		}
		_enemyCache.Clear();
	}

	private static bool ShouldRemoveEnemyData(EnemyData data)
	{
		if (data == null || (UnityEngine.Object)(object)data.Parent == (UnityEngine.Object)null || (UnityEngine.Object)(object)data.Enemy == (UnityEngine.Object)null)
		{
			return true;
		}
		if (!((Component)data.Parent).gameObject.activeInHierarchy || !((Component)data.Enemy).gameObject.activeInHierarchy)
		{
			return true;
		}
		if ((UnityEngine.Object)(object)data.Health == (UnityEngine.Object)null)
		{
			return false;
		}
		if (ReflectionUtils.GetFieldValue<bool>(data.Health, "dead") || ReflectionUtils.GetFieldValue<bool>(data.Health, "deadImpulse"))
		{
			return true;
		}
		object fieldValue = ReflectionUtils.GetFieldValue(data.Health, "healthCurrent");
		return fieldValue is int num && data.Health.health > 0 && num <= 0;
	}

	private static void RemoveEnemyData(int key, EnemyData data)
	{
		if (data != null)
		{
			if ((UnityEngine.Object)(object)data.Enemy != (UnityEngine.Object)null)
			{
				EnemyEspLineRenderer component = ((Component)data.Enemy).GetComponent<EnemyEspLineRenderer>();
				if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
				{
					UnityEngine.Object.Destroy((UnityEngine.Object)(object)component);
				}
			}
			foreach (Mesh bakedMesh in data.BakedMeshes)
			{
				if ((UnityEngine.Object)(object)bakedMesh != (UnityEngine.Object)null)
				{
					UnityEngine.Object.Destroy((UnityEngine.Object)(object)bakedMesh);
				}
			}
			if ((UnityEngine.Object)(object)data.PathViz != (UnityEngine.Object)null)
			{
				UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)data.PathViz).gameObject);
			}
		}
		_enemyCache.Remove(key);
	}

	private static void EnforceVisibility(EnemyData data)
	{
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		// if ((UnityEngine.Object)(object)data.Parent != (UnityEngine.Object)null)
		// {
		// 	ReflectionUtils.SetFieldValue(data.Parent, "playerClose", true);
		// }
		if ((UnityEngine.Object)(object)data.Enemy != (UnityEngine.Object)null)
		{
			object fieldValue = ReflectionUtils.GetFieldValue(data.Enemy, "OnScreen");
			if (fieldValue != null)
			{
				ReflectionUtils.SetFieldValue(fieldValue, "maxDistance", 10000f);
			}
		}
		if (data.Renderers != null)
		{
			foreach (Renderer renderer in data.Renderers)
			{
				if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null)
				{
					continue;
				}
				SkinnedMeshRenderer val = (SkinnedMeshRenderer)(object)((renderer is SkinnedMeshRenderer) ? renderer : null);
				if (val != null)
				{
					if (!val.updateWhenOffscreen)
					{
						val.updateWhenOffscreen = true;
					}
				}
			}
		}
		if (data.Animators == null || !(_animatorCullingModeProp != null))
		{
			return;
		}
		foreach (Component animator in data.Animators)
		{
			if (!((UnityEngine.Object)(object)animator == (UnityEngine.Object)null))
			{
				try
				{
					_animatorCullingModeProp.SetValue(animator, 0, null);
				}
				catch
				{
				}
			}
		}
	}

	private static EnemyData EnsureEnemyData(EnemyParent parent)
	{
		if ((UnityEngine.Object)(object)parent == (UnityEngine.Object)null)
		{
			return null;
		}

		int instanceID = ((UnityEngine.Object)parent).GetInstanceID();
		if (_enemyCache.TryGetValue(instanceID, out var existing))
		{
			existing.Parent = parent;
			return existing;
		}

		Enemy enemy = ReflectionUtils.GetFieldValue<Enemy>(parent, "Enemy");
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return null;
		}

		EnemyData enemyData = new EnemyData
		{
			Parent = parent,
			Enemy = enemy,
			SmoothedPosition = ((Component)enemy).transform.position,
			LastPosition = ((Component)enemy).transform.position,
			LastUpdateTime = Time.time,
			LastSeenInDirectorTime = Time.time,
			LastVisibilityRefreshTime = -999f,
			LastVisualRefreshTime = -999f
		};

		EnemyNavMeshAgent enemyNavMeshAgent = ReflectionUtils.GetFieldValue<EnemyNavMeshAgent>(enemy, "NavMeshAgent");
		if ((UnityEngine.Object)(object)enemyNavMeshAgent != (UnityEngine.Object)null)
		{
			enemyData.Agent = ReflectionUtils.GetFieldValue<NavMeshAgent>(enemyNavMeshAgent, "Agent");
			if ((UnityEngine.Object)(object)enemyData.Agent != (UnityEngine.Object)null && (UnityEngine.Object)(object)enemyData.PathViz == (UnityEngine.Object)null)
			{
				GameObject gameObject = new GameObject($"PathVisualizer_{instanceID}");
				enemyData.PathViz = gameObject.AddComponent<PathVisualizer>();
				enemyData.PathViz.Initialize(enemyData.Agent);
			}
		}

		enemyData.Health = ReflectionUtils.GetFieldValue<EnemyHealth>(enemy, "Health");
		RefreshVisualReferences(enemyData, force: true);
		_enemyCache[instanceID] = enemyData;
		return enemyData;
	}

	private static void RefreshEnemyVisualState(EnemyData data)
	{
		if (data == null)
		{
			return;
		}

		if (data.Renderers.Count == 0 || Time.time - data.LastVisualRefreshTime >= VisualRefreshInterval || !HasUsableBoundsSource(data))
		{
			RefreshVisualReferences(data, force: true);
		}

		if (Time.time - data.LastVisibilityRefreshTime >= VisibilityRefreshInterval)
		{
			EnforceVisibility(data);
			data.LastVisibilityRefreshTime = Time.time;
		}
	}

	private static void RefreshVisualReferences(EnemyData data, bool force)
	{
		if (data == null)
		{
			return;
		}

		Transform root = ((UnityEngine.Object)(object)data.Parent != (UnityEngine.Object)null) ? ((Component)data.Parent).transform : (((UnityEngine.Object)(object)data.Enemy != (UnityEngine.Object)null) ? ((Component)data.Enemy).transform : null);
		if ((UnityEngine.Object)(object)root == (UnityEngine.Object)null)
		{
			return;
		}

		if (!force && Time.time - data.LastVisualRefreshTime < VisualRefreshInterval)
		{
			return;
		}

		foreach (Mesh bakedMesh in data.BakedMeshes)
		{
			if ((UnityEngine.Object)(object)bakedMesh != (UnityEngine.Object)null)
			{
				UnityEngine.Object.Destroy((UnityEngine.Object)(object)bakedMesh);
			}
		}

		data.Renderers.Clear();
		data.Meshes.Clear();
		data.MeshTransforms.Clear();
		data.Animators.Clear();
		data.BakedMeshes.Clear();
		Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer))
			{
				continue;
			}

			data.Renderers.Add(renderer);
			SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
			if ((UnityEngine.Object)(object)skinnedMeshRenderer != (UnityEngine.Object)null)
			{
				skinnedMeshRenderer.updateWhenOffscreen = true;
				data.BakedMeshes.Add(new Mesh());
				continue;
			}

			data.BakedMeshes.Add(null);
			MeshFilter component = ((Component)renderer).GetComponent<MeshFilter>();
			if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null && (UnityEngine.Object)(object)component.sharedMesh != (UnityEngine.Object)null)
			{
				data.Meshes.Add(component.sharedMesh);
				data.MeshTransforms.Add(((Component)renderer).transform);
			}
		}

		Type type = Type.GetType("UnityEngine.Animator, UnityEngine.AnimationModule") ?? Type.GetType("UnityEngine.Animator, UnityEngine");
		if (type != null)
		{
			Component[] componentsInChildren2 = root.GetComponentsInChildren(type, true);
			data.Animators.AddRange(componentsInChildren2);
			if (_animatorCullingModeProp == null)
			{
				_animatorCullingModeProp = type.GetProperty("cullingMode");
			}
		}

		data.LastVisualRefreshTime = Time.time;
		InvalidateProjectionCache(data);
	}

	private static void InvalidateProjectionCache(EnemyData data)
	{
		if (data == null)
		{
			return;
		}

		data.CachedBoundsFrame = -1;
		data.HasCachedBounds = false;
		data.CachedRectFrame = -1;
		data.HasCachedRect = false;
	}

	private static bool HasUsableBoundsSource(EnemyData data)
	{
		if (data == null)
		{
			return false;
		}

		Vector3 focusPoint = EnemyVisibilityUtility.GetFocusPoint(data);
		if (TryGetFilteredRendererBounds(data.Renderers, focusPoint, out var filteredRendererBounds) && filteredRendererBounds.size.sqrMagnitude > 0.0001f)
		{
			return true;
		}

		Transform root = ((UnityEngine.Object)(object)data.Parent != (UnityEngine.Object)null) ? ((Component)data.Parent).transform : (((UnityEngine.Object)(object)data.Enemy != (UnityEngine.Object)null) ? ((Component)data.Enemy).transform : null);
		return TryGetNearbyColliderBounds(root, focusPoint, out var nearbyColliderBounds) && nearbyColliderBounds.size.sqrMagnitude > 0.0001f;
	}

	/// <summary>Unions valid live mesh renderer bounds so detached limbs and asymmetric parts are included.</summary>
	private static bool TryGetPrimaryMeshBounds(IEnumerable<Renderer> renderers, out Bounds bounds)
	{
		bounds = default(Bounds);
		bool found = false;
		foreach (Renderer renderer in renderers ?? Array.Empty<Renderer>())
		{
			if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.isPartOfStaticBatch)
				continue;
			if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer))
				continue;
			Vector3 size = renderer.bounds.size;
			if (size.sqrMagnitude <= 0.0001f || float.IsNaN(size.sqrMagnitude) || float.IsInfinity(size.sqrMagnitude))
				continue;
			if (!found)
				bounds = renderer.bounds;
			else
				bounds.Encapsulate(renderer.bounds);
			found = true;
		}
		return found;
	}

	private static bool ShouldUseEnemyRenderer(Renderer renderer)
	{
		return (UnityEngine.Object)(object)renderer != (UnityEngine.Object)null
			&& renderer.enabled && renderer.gameObject.activeInHierarchy && !renderer.isPartOfStaticBatch
			&& (renderer is SkinnedMeshRenderer || renderer is MeshRenderer);
	}

	/// <summary>Unions enabled colliders attached to actively simulated rigidbodies for ragdoll bounds.</summary>
	private static bool TryGetRagdollBounds(Transform root, out Bounds bounds)
	{
		bounds = default(Bounds);
		if ((UnityEngine.Object)(object)root == (UnityEngine.Object)null)
			return false;
		Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
		bool hasBounds = false;
		foreach (Rigidbody body in bodies)
		{
			if ((UnityEngine.Object)(object)body == (UnityEngine.Object)null || body.isKinematic || !body.gameObject.activeInHierarchy)
				continue;
			Collider[] colliders = body.GetComponents<Collider>();
			foreach (Collider collider in colliders)
			{
				if ((UnityEngine.Object)(object)collider == (UnityEngine.Object)null || !collider.enabled || collider.isTrigger || collider.bounds.size.sqrMagnitude <= 0.0001f)
					continue;
				if (!hasBounds)
				{
					bounds = collider.bounds;
					hasBounds = true;
				}
				else
					bounds.Encapsulate(collider.bounds);
			}
		}
		return hasBounds;
	}

	private static bool TryGetReferenceBounds(Transform root, IEnumerable<Renderer> renderers, Vector3 focusPoint, out Bounds bounds)
	{
		bounds = default(Bounds);
		if (TryGetFilteredRendererBounds(renderers, focusPoint, out bounds))
		{
			return true;
		}

		if (TryGetNearbyColliderBounds(root, focusPoint, out bounds))
		{
			return true;
		}

		return TryGetCharacterBounds(root, out bounds);
	}

	private static bool TryGetNearbyColliderBounds(Transform root, Vector3 focusPoint, out Bounds bounds)
	{
		bounds = default(Bounds);
		if ((UnityEngine.Object)(object)root == (UnityEngine.Object)null)
		{
			return false;
		}

		Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
		bool hasBounds = false;
		foreach (Collider collider in colliders)
		{
			if ((UnityEngine.Object)(object)collider == (UnityEngine.Object)null || !collider.enabled || collider.isTrigger)
			{
				continue;
			}

			Bounds colliderBounds = collider.bounds;
			Vector3 size = colliderBounds.size;
			if (size.sqrMagnitude <= 0.0001f)
			{
				continue;
			}

			float maxDimension = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
			if (maxDimension > 10f)
			{
				continue;
			}

			float distanceFromFocus = Vector3.Distance(colliderBounds.center, focusPoint);
			if (distanceFromFocus > Mathf.Max(MaxBoundsRendererDistance, maxDimension * 1.75f))
			{
				continue;
			}

			if (!hasBounds)
			{
				bounds = colliderBounds;
				hasBounds = true;
			}
			else
			{
				bounds.Encapsulate(colliderBounds);
			}
		}

		return hasBounds && bounds.size.sqrMagnitude > 0.0001f;
	}

	private static bool TryGetFilteredRendererBounds(IEnumerable<Renderer> renderers, Vector3 focusPoint, out Bounds bounds)
	{
		// Keep reference sizing aligned with the same primary animated mesh used for ESP.
		return TryGetPrimaryMeshBounds(renderers, out bounds);
	}

	private static bool TryGetCharacterBounds(Transform root, out Bounds bounds)
	{
		bounds = default(Bounds);
		if ((UnityEngine.Object)(object)root == (UnityEngine.Object)null)
		{
			return false;
		}

		CharacterController characterController = root.GetComponent<CharacterController>();
		if ((UnityEngine.Object)(object)characterController != (UnityEngine.Object)null && characterController.bounds.size.sqrMagnitude > 0.001f)
		{
			bounds = characterController.bounds;
			return true;
		}

		CapsuleCollider capsuleCollider = root.GetComponent<CapsuleCollider>();
		if ((UnityEngine.Object)(object)capsuleCollider != (UnityEngine.Object)null && capsuleCollider.bounds.size.sqrMagnitude > 0.001f)
		{
			bounds = capsuleCollider.bounds;
			return true;
		}

		return false;
	}

	private static bool ShouldUseBoundsRenderer(Renderer renderer, Vector3 focusPoint)
	{
		if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null || !renderer.enabled || renderer.isPartOfStaticBatch)
		{
			return false;
		}

		Bounds bounds = renderer.bounds;
		Vector3 size = bounds.size;
		if (size.sqrMagnitude <= 0.0001f)
		{
			return false;
		}

		float maxDimension = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
		if (maxDimension > 12f)
		{
			return false;
		}

		float distanceFromFocus = Vector3.Distance(bounds.center, focusPoint);
		if (distanceFromFocus > Mathf.Max(MaxBoundsRendererDistance, maxDimension * 1.5f))
		{
			return false;
		}

		return true;
	}
}
