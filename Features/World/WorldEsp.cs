using System;
using System.Collections.Generic;
using System.Reflection;
using Cheat.Config;
using Cheat.UI;
using Cheat.Utils;
using UnityEngine;

namespace Cheat.Features.World;

public static class WorldEsp
{
	private enum TargetCategory
	{
		ExtractionPoint,
		EvacuationPoint,
		Trap
	}

	private sealed class RuntimeTarget
	{
		public Component Component;

		public Transform Root;

		public Transform BoundsRoot;

		public Transform Anchor;

		public Vector3 AnchorWorldPosition;

		public Bounds? BoundsOverride;

		public Renderer[] Renderers;

		public TargetCategory Category;

		public string Label;

		public Vector3 FallbackSize;

		public float MinWidth;

		public float MinHeight;
	}

	private struct TargetDefinition
	{
		public string TypeName;

		public TargetCategory Category;

		public string Label;

		public Vector3 FallbackSize;

		public float MinWidth;

		public float MinHeight;

		public TargetDefinition(string typeName, TargetCategory category, string label, Vector3 fallbackSize, float minWidth, float minHeight)
		{
			TypeName = typeName;
			Category = category;
			Label = label;
			FallbackSize = fallbackSize;
			MinWidth = minWidth;
			MinHeight = minHeight;
		}
	}

	private static readonly TargetDefinition[] Definitions = new TargetDefinition[5]
	{
		new TargetDefinition("ExtractionPoint", TargetCategory.ExtractionPoint, "提取点", new Vector3(2f, 3f, 2f), 42f, 84f),
		new TargetDefinition("TruckSafetySpawnPoint", TargetCategory.EvacuationPoint, "撤离点", new Vector3(2.4f, 2.4f, 2.4f), 44f, 44f),
		new TargetDefinition("MuseumLaserLogic", TargetCategory.Trap, "激光陷阱", new Vector3(3.5f, 0.6f, 0.6f), 34f, 20f),
		new TargetDefinition("DeathPitForce", TargetCategory.Trap, "坑洞陷阱", new Vector3(4f, 2f, 4f), 46f, 24f),
		new TargetDefinition("HurtCollider", TargetCategory.Trap, "环境陷阱", new Vector3(3f, 1f, 3f), 40f, 22f)
	};

	private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>(StringComparer.Ordinal);

	private static readonly List<RuntimeTarget> CachedTargets = new List<RuntimeTarget>(64);

	private static readonly HashSet<int> SeenInstanceIds = new HashSet<int>();

	private const float RefreshInterval = 1f;

	private static float _nextRefreshAt;

	public static void Draw()
	{
		ConfigData.StructureEspSettings settings = ConfigManager.Config?.Structures;
		if (settings == null || !HasAnyEnabled(settings))
		{
			return;
		}

		Camera mainCamera = Camera.main;
		if ((UnityEngine.Object)(object)mainCamera == (UnityEngine.Object)null || Event.current == null || (int)Event.current.type != 7)
		{
			return;
		}

		if (Time.unscaledTime >= _nextRefreshAt || CachedTargets.Count == 0)
		{
			RefreshTargets(settings);
		}

		Vector2 tracerOrigin = new Vector2((float)Screen.width * 0.5f, (float)Screen.height - 18f);
		Vector3 cameraPosition = ((Component)mainCamera).transform.position;
		for (int i = CachedTargets.Count - 1; i >= 0; i--)
		{
			RuntimeTarget target = CachedTargets[i];
			if (!IsValidTarget(target) || !IsCategoryEnabled(settings, target.Category))
			{
				if (!IsValidTarget(target))
				{
					CachedTargets.RemoveAt(i);
				}
				continue;
			}

			Vector3 worldPosition = GetWorldPosition(target);
			float distance = Vector3.Distance(cameraPosition, worldPosition);
			if (distance > settings.MaxDistance)
			{
				continue;
			}

			Rect rect;
			if (target.BoundsOverride.HasValue)
			{
				if (!EspProjectionUtils.TryGetScreenRect(mainCamera, target.BoundsOverride.Value, target.MinWidth, target.MinHeight, out rect))
				{
					continue;
				}
			}
			else
			{
				Transform boundsRoot = (UnityEngine.Object)(object)target.BoundsRoot != (UnityEngine.Object)null ? target.BoundsRoot : target.Root;
				if (!EspProjectionUtils.TryGetScreenRect(mainCamera, boundsRoot, target.Renderers, ShouldUseRenderer, worldPosition, target.FallbackSize, target.MinWidth, target.MinHeight, out rect))
				{
					continue;
				}
			}

			Color color = GetCategoryColor(settings, target.Category);
			if (settings.DrawTracers)
			{
				Render.DrawLine(tracerOrigin, new Vector2(rect.center.x, rect.yMax), color, 1.2f);
			}

			if (settings.DrawBox)
			{
				DrawTargetMarker(target.Category, rect, color);
			}

			if (settings.DrawName)
			{
				Render.DrawStringOutlined(new Rect(rect.x - 50f, rect.y - 18f, rect.width + 100f, 18f), target.Label, color, center: true, 12, bold: true);
			}

			if (settings.DrawDistance)
			{
				Render.DrawStringOutlined(new Rect(rect.x - 50f, rect.yMax + 2f, rect.width + 100f, 18f), string.Format("{0:F0}m", distance), color, center: true, 12, bold: true);
			}
		}
	}

	private static void RefreshTargets(ConfigData.StructureEspSettings settings)
	{
		CachedTargets.Clear();
		SeenInstanceIds.Clear();

		for (int i = 0; i < Definitions.Length; i++)
		{
			TargetDefinition definition = Definitions[i];
			if (!IsCategoryEnabled(settings, definition.Category))
			{
				continue;
			}

			Type runtimeType = ResolveRuntimeType(definition.TypeName);
			if (runtimeType == null)
			{
				continue;
			}

			UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(runtimeType);
			if (objects == null)
			{
				continue;
			}

			for (int j = 0; j < objects.Length; j++)
			{
				Component component = objects[j] as Component;
				if (!TryCreateTarget(component, definition, out RuntimeTarget target))
				{
					continue;
				}

				int instanceId = ((UnityEngine.Object)component).GetInstanceID();
				if (SeenInstanceIds.Add(instanceId))
				{
					CachedTargets.Add(target);
				}
			}
		}

		_nextRefreshAt = Time.unscaledTime + RefreshInterval;
	}

	private static bool TryCreateTarget(Component component, TargetDefinition definition, out RuntimeTarget target)
	{
		target = null;
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null || (UnityEngine.Object)(object)((Component)component).transform == (UnityEngine.Object)null)
		{
			return false;
		}

		GameObject gameObject = ((Component)component).gameObject;
		if ((UnityEngine.Object)(object)gameObject == (UnityEngine.Object)null || !gameObject.scene.IsValid() || !gameObject.activeInHierarchy)
		{
			return false;
		}

		if (ShouldSkipTarget(component, definition.Category))
		{
			return false;
		}

		Transform boundsRoot = ResolveBoundsRoot(component, definition.Category);
		Bounds? boundsOverride = ResolveBoundsOverride(component, definition.Category);
		Renderer[] renderers = ((Component)((UnityEngine.Object)(object)boundsRoot != (UnityEngine.Object)null ? boundsRoot : ((Component)component).transform)).GetComponentsInChildren<Renderer>(true);
		target = new RuntimeTarget
		{
			Component = component,
			Root = ((Component)component).transform,
			BoundsRoot = boundsRoot,
			Anchor = ResolveAnchorTransform(component, definition.Category),
			AnchorWorldPosition = ResolveAnchorPosition(component, definition.Category),
			BoundsOverride = boundsOverride,
			Renderers = renderers,
			Category = definition.Category,
			Label = ResolveLabel(component, definition),
			FallbackSize = definition.FallbackSize,
			MinWidth = definition.MinWidth,
			MinHeight = definition.MinHeight
		};
		return true;
	}

	private static bool IsValidTarget(RuntimeTarget target)
	{
		return target != null && (UnityEngine.Object)(object)target.Component != (UnityEngine.Object)null && (UnityEngine.Object)(object)target.Root != (UnityEngine.Object)null && target.Root.gameObject.scene.IsValid() && target.Root.gameObject.activeInHierarchy;
	}

	private static Vector3 GetWorldPosition(RuntimeTarget target)
	{
		if (target == null)
		{
			return Vector3.zero;
		}

		if ((UnityEngine.Object)(object)target.Anchor != (UnityEngine.Object)null)
		{
			return target.Anchor.position;
		}

		if (target.BoundsOverride.HasValue)
		{
			return target.BoundsOverride.Value.center;
		}

		if (target.AnchorWorldPosition.sqrMagnitude > 0.001f)
		{
			return target.AnchorWorldPosition;
		}

		if ((UnityEngine.Object)(object)target.BoundsRoot != (UnityEngine.Object)null)
		{
			return target.BoundsRoot.position;
		}

		return ((UnityEngine.Object)(object)target.Root != (UnityEngine.Object)null) ? target.Root.position : Vector3.zero;
	}

	private static bool HasAnyEnabled(ConfigData.StructureEspSettings settings)
	{
		return settings.ExtractionPointsEnabled || settings.EvacuationPointsEnabled || settings.TrapsEnabled;
	}

	private static bool IsCategoryEnabled(ConfigData.StructureEspSettings settings, TargetCategory category)
	{
		switch (category)
		{
		case TargetCategory.ExtractionPoint:
			return settings.ExtractionPointsEnabled;
		case TargetCategory.EvacuationPoint:
			return settings.EvacuationPointsEnabled;
		case TargetCategory.Trap:
			return settings.TrapsEnabled;
		default:
			return false;
		}
	}

	private static Color GetCategoryColor(ConfigData.StructureEspSettings settings, TargetCategory category)
	{
		switch (category)
		{
		case TargetCategory.ExtractionPoint:
			return settings.ExtractionPointColor;
		case TargetCategory.EvacuationPoint:
			return settings.EvacuationPointColor;
		case TargetCategory.Trap:
			return settings.TrapColor;
		default:
			return Color.white;
		}
	}

	private static bool ShouldUseRenderer(Renderer renderer)
	{
		return !((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null) && renderer.enabled && !(renderer is ParticleSystemRenderer) && !(renderer is TrailRenderer) && !(renderer is LineRenderer) && renderer.bounds.size.sqrMagnitude > 0.001f && renderer.bounds.size.y < 80f;
	}

	private static Transform ResolveBoundsRoot(Component component, TargetCategory category)
	{
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			return null;
		}

		switch (category)
		{
		case TargetCategory.ExtractionPoint:
			return ResolveExtractionReferenceTransform(component);
		}

		return ((Component)component).transform;
	}

	private static Bounds? ResolveBoundsOverride(Component component, TargetCategory category)
	{
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			return null;
		}

		switch (category)
		{
		case TargetCategory.ExtractionPoint:
			if (TryResolveExtractionBounds(component, out var bounds2))
			{
				return bounds2;
			}
			break;
		case TargetCategory.Trap:
			if (TryResolveTrapBounds(component, out var bounds))
			{
				return bounds;
			}
			break;
		}

		return null;
	}

	private static Vector3 ResolveAnchorPosition(Component component, TargetCategory category)
	{
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			return Vector3.zero;
		}

		switch (category)
		{
		case TargetCategory.ExtractionPoint:
			if (TryResolveExtractionBounds(component, out var extractionBounds))
			{
				return extractionBounds.center;
			}
			break;
		case TargetCategory.EvacuationPoint:
			return ((Component)component).transform.position;
		case TargetCategory.Trap:
			if (TryResolveTrapBounds(component, out var trapBounds))
			{
				return trapBounds.center;
			}
			break;
		}

		return ((Component)component).transform.position;
	}

	private static Transform ResolveAnchorTransform(Component component, TargetCategory category)
	{
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			return null;
		}

		if (category == TargetCategory.ExtractionPoint)
		{
			return ResolveExtractionAnchorTransform(component);
		}

		return ResolveBoundsRoot(component, category);
	}

	private static void DrawTargetMarker(TargetCategory category, Rect rect, Color color)
	{
		switch (category)
		{
		case TargetCategory.ExtractionPoint:
			DrawBracketMarker(rect, color, 2f);
			DrawDiamond(CenterRect(rect, 16f, 16f), new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * 0.85f)), 1.4f);
			break;
		case TargetCategory.EvacuationPoint:
			DrawBracketMarker(CenterRect(rect, Mathf.Max(rect.width, 42f), Mathf.Max(rect.height, 42f)), color, 2f);
			break;
		case TargetCategory.Trap:
			DrawBracketMarker(CenterRect(rect, Mathf.Max(rect.width, 34f), Mathf.Max(rect.height, 24f)), color, 2f);
			DrawDiamond(CenterRect(rect, 14f, 14f), new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * 0.9f)), 1.5f);
			break;
		default:
			DrawCornerBox(rect, color, 1.7f);
			break;
		}
	}

	private static Rect CenterRect(Rect rect, float width, float height)
	{
		return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
	}

	private static void DrawCornerBox(Rect rect, Color color, float thickness)
	{
		float cornerWidth = Mathf.Clamp(rect.width * 0.32f, 7f, 20f);
		float cornerHeight = Mathf.Clamp(rect.height * 0.18f, 8f, 22f);
		Render.DrawBox(new Rect(rect.x, rect.y, cornerWidth, thickness), color);
		Render.DrawBox(new Rect(rect.x, rect.y, thickness, cornerHeight), color);
		Render.DrawBox(new Rect(rect.xMax - cornerWidth, rect.y, cornerWidth, thickness), color);
		Render.DrawBox(new Rect(rect.xMax - thickness, rect.y, thickness, cornerHeight), color);
		Render.DrawBox(new Rect(rect.x, rect.yMax - thickness, cornerWidth, thickness), color);
		Render.DrawBox(new Rect(rect.x, rect.yMax - cornerHeight, thickness, cornerHeight), color);
		Render.DrawBox(new Rect(rect.xMax - cornerWidth, rect.yMax - thickness, cornerWidth, thickness), color);
		Render.DrawBox(new Rect(rect.xMax - thickness, rect.yMax - cornerHeight, thickness, cornerHeight), color);
	}

	private static void DrawDiamond(Rect rect, Color color, float thickness)
	{
		Vector2 top = new Vector2(rect.center.x, rect.yMin);
		Vector2 right = new Vector2(rect.xMax, rect.center.y);
		Vector2 bottom = new Vector2(rect.center.x, rect.yMax);
		Vector2 left = new Vector2(rect.xMin, rect.center.y);
		Render.DrawLine(top, right, color, thickness);
		Render.DrawLine(right, bottom, color, thickness);
		Render.DrawLine(bottom, left, color, thickness);
		Render.DrawLine(left, top, color, thickness);
	}

	private static void DrawBracketMarker(Rect rect, Color color, float thickness)
	{
		DrawCornerBox(rect, color, thickness);
		Color accent = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * 0.35f));
		Render.DrawLine(new Vector2(rect.center.x, rect.yMin + 4f), new Vector2(rect.center.x, rect.yMax - 4f), accent, 1f);
		Render.DrawLine(new Vector2(rect.xMin + 4f, rect.center.y), new Vector2(rect.xMax - 4f, rect.center.y), accent, 1f);
	}

	private static Type ResolveRuntimeType(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return null;
		}

		if (TypeCache.TryGetValue(typeName, out Type cachedType))
		{
			return cachedType;
		}

		Type resolvedType = Type.GetType(typeName) ?? Type.GetType(typeName + ", Assembly-CSharp");
		if (resolvedType == null)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				try
				{
					resolvedType = assemblies[i].GetType(typeName);
					if (resolvedType != null)
					{
						break;
					}
				}
				catch
				{
				}
			}
		}

		TypeCache[typeName] = resolvedType;
		return resolvedType;
	}

	private static bool ShouldSkipTarget(Component component, TargetCategory category)
	{
		switch (category)
		{
		case TargetCategory.Trap:
			return !IsSupportedTrapTarget(component);
		default:
			return false;
		}
	}

	private static bool IsSupportedTrapTarget(Component component)
	{
		string componentTypeName = GetComponentTypeName(component);
		if (string.Equals(componentTypeName, "MuseumLaserLogic", StringComparison.Ordinal) || string.Equals(componentTypeName, "DeathPitForce", StringComparison.Ordinal))
		{
			return true;
		}

		return string.Equals(componentTypeName, "HurtCollider", StringComparison.Ordinal) && TryResolveTrapLabel(component, out _);
	}

	private static string ResolveLabel(Component component, TargetDefinition definition)
	{
		if (definition.Category == TargetCategory.Trap && TryResolveTrapLabel(component, out var label))
		{
			return label;
		}

		return definition.Label;
	}

	private static bool TryResolveTrapLabel(Component component, out string label)
	{
		label = null;
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			return false;
		}

		string componentTypeName = GetComponentTypeName(component);
		if (string.Equals(componentTypeName, "MuseumLaserLogic", StringComparison.Ordinal))
		{
			label = "激光陷阱";
			return true;
		}

		if (string.Equals(componentTypeName, "DeathPitForce", StringComparison.Ordinal))
		{
			label = "坑洞陷阱";
			return true;
		}

		if (!string.Equals(componentTypeName, "HurtCollider", StringComparison.Ordinal) || !IsEnvironmentTrapHurtCollider(component))
		{
			return false;
		}

		if (ReflectionUtils.GetFieldValue<bool>(component, "deathPit"))
		{
			label = "坑洞陷阱";
			return true;
		}

		string text = GetHierarchyContext(((Component)component).transform);
		if (ContainsAny(text, "laser"))
		{
			label = "激光陷阱";
			return true;
		}

		if (ContainsAny(text, "lava", "magma"))
		{
			label = "岩浆池";
			return true;
		}

		if (ContainsAny(text, "poison", "acid", "toxic", "slime", "ooze"))
		{
			label = "毒池";
			return true;
		}

		if (ContainsAny(text, "pit", "death", "void", "hole", "fall"))
		{
			label = "坑洞陷阱";
			return true;
		}

		Collider collider = ((Component)component).GetComponent<Collider>();
		if ((UnityEngine.Object)(object)collider != (UnityEngine.Object)null)
		{
			Vector3 size = collider.bounds.size;
			if (size.x * size.z >= 6f && size.y <= 8f)
			{
				label = "环境陷阱";
				return true;
			}
		}

		return false;
	}

	private static bool IsEnvironmentTrapHurtCollider(Component component)
	{
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			return false;
		}

		Behaviour behaviour = component as Behaviour;
		if (behaviour != null && !behaviour.enabled)
		{
			return false;
		}

		if (HasParentType(component, "PhysGrabObject", "PlayerAvatar", "PlayerController", "Enemy", "ItemVehicle", "Trap", "UpgradeStand", "MuseumLaserLogic", "ExtractionPoint", "ShopKeeper", "ItemMelee", "ItemGun", "ParticlePrefabExplosion", "SlowProjectile"))
		{
			return false;
		}

		Collider collider = ((Component)component).GetComponent<Collider>();
		return !((UnityEngine.Object)(object)collider == (UnityEngine.Object)null);
	}

	private static bool TryResolveTrapBounds(Component component, out Bounds bounds)
	{
		bounds = default(Bounds);
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			return false;
		}

		string componentTypeName = GetComponentTypeName(component);
		if (string.Equals(componentTypeName, "MuseumLaserLogic", StringComparison.Ordinal))
		{
			Transform transform = GetComponentTransformField(component, "laserBall1Transform");
			Transform transform2 = GetComponentTransformField(component, "laserBall2Transform");
			if ((UnityEngine.Object)(object)transform != (UnityEngine.Object)null && (UnityEngine.Object)(object)transform2 != (UnityEngine.Object)null)
			{
				bounds = new Bounds(transform.position, Vector3.one * 0.35f);
				EncapsulatePointWithExtents(ref bounds, transform2.position, Vector3.one * 0.35f);
				Transform transform3 = GetComponentTransformField(component, "hurtColliderTransform");
				if ((UnityEngine.Object)(object)transform3 != (UnityEngine.Object)null)
				{
					EncapsulatePointWithExtents(ref bounds, transform3.position, new Vector3(0.4f, 0.4f, 0.4f));
				}
				return true;
			}
		}
		else if (string.Equals(componentTypeName, "DeathPitForce", StringComparison.Ordinal))
		{
			BoxCollider fieldValue = ReflectionUtils.GetFieldValue<BoxCollider>(component, "boxCollider");
			if ((UnityEngine.Object)(object)fieldValue != (UnityEngine.Object)null)
			{
				bounds = fieldValue.bounds;
				return bounds.size.sqrMagnitude > 0.001f;
			}
		}
		else if (string.Equals(componentTypeName, "HurtCollider", StringComparison.Ordinal))
		{
			Collider component2 = ((Component)component).GetComponent<Collider>();
			if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
			{
				bounds = component2.bounds;
				return bounds.size.sqrMagnitude > 0.001f;
			}
		}

		return false;
	}

	private static Transform ResolveExtractionReferenceTransform(Component component)
	{
		Transform transform = GetComponentTransformField(component, "platform");
		if ((UnityEngine.Object)(object)transform != (UnityEngine.Object)null)
		{
			return transform;
		}

		Transform fieldComponentTransform = GetFieldComponentTransform(component, "buttonGrabObject");
		if ((UnityEngine.Object)(object)fieldComponentTransform != (UnityEngine.Object)null)
		{
			return fieldComponentTransform;
		}

		Transform transform2 = GetComponentTransformField(component, "extractionTube");
		if ((UnityEngine.Object)(object)transform2 != (UnityEngine.Object)null)
		{
			return transform2;
		}

		return ((Component)component).transform;
	}

	private static Transform ResolveExtractionAnchorTransform(Component component)
	{
		Transform fieldComponentTransform = GetFieldComponentTransform(component, "buttonGrabObject");
		if ((UnityEngine.Object)(object)fieldComponentTransform != (UnityEngine.Object)null)
		{
			return fieldComponentTransform;
		}

		Transform transform = GetComponentTransformField(component, "platform");
		if ((UnityEngine.Object)(object)transform != (UnityEngine.Object)null)
		{
			return transform;
		}

		return GetComponentTransformField(component, "extractionTube");
	}

	private static bool TryResolveExtractionBounds(Component component, out Bounds bounds)
	{
		bounds = default(Bounds);
		bool hasBounds = false;
		AddTransformBounds(GetComponentTransformField(component, "platform"), ref bounds, ref hasBounds);
		AddTransformBounds(GetFieldComponentTransform(component, "buttonGrabObject"), ref bounds, ref hasBounds);
		AddTransformBounds(GetComponentTransformField(component, "button"), ref bounds, ref hasBounds);
		AddTransformBounds(GetComponentTransformField(component, "extractionTube"), ref bounds, ref hasBounds);
		AddTransformBounds(GetComponentTransformField(component, "emojiScreen"), ref bounds, ref hasBounds);
		if (!hasBounds)
		{
			Transform extractionReferenceTransform = ResolveExtractionReferenceTransform(component);
			if ((UnityEngine.Object)(object)extractionReferenceTransform == (UnityEngine.Object)null)
			{
				return false;
			}

			bounds = new Bounds(extractionReferenceTransform.position, new Vector3(1.8f, 3.2f, 1.8f));
			return true;
		}

		bounds.Expand(new Vector3(0.35f, 0.2f, 0.35f));
		return true;
	}

	private static Transform GetComponentTransformField(Component component, string fieldName)
	{
		return ReflectionUtils.GetFieldValue<Transform>(component, fieldName);
	}

	private static Transform GetFieldComponentTransform(Component component, string fieldName)
	{
		Component fieldValue = ReflectionUtils.GetFieldValue<Component>(component, fieldName);
		return ((UnityEngine.Object)(object)fieldValue != (UnityEngine.Object)null) ? ((Component)fieldValue).transform : null;
	}

	private static void AddTransformBounds(Transform transform, ref Bounds bounds, ref bool hasBounds)
	{
		if ((UnityEngine.Object)(object)transform == (UnityEngine.Object)null || !TryGetTransformWorldBounds(transform, out var bounds2))
		{
			return;
		}

		if (!hasBounds)
		{
			bounds = bounds2;
			hasBounds = true;
		}
		else
		{
			bounds.Encapsulate(bounds2.min);
			bounds.Encapsulate(bounds2.max);
		}
	}

	private static bool TryGetTransformWorldBounds(Transform transform, out Bounds bounds)
	{
		bounds = default(Bounds);
		if ((UnityEngine.Object)(object)transform == (UnityEngine.Object)null)
		{
			return false;
		}

		bool hasBounds = false;
		Renderer[] componentsInChildren = transform.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Renderer renderer = componentsInChildren[i];
			if (!ShouldUseRenderer(renderer))
			{
				continue;
			}

			if (!hasBounds)
			{
				bounds = renderer.bounds;
				hasBounds = true;
			}
			else
			{
				bounds.Encapsulate(renderer.bounds.min);
				bounds.Encapsulate(renderer.bounds.max);
			}
		}

		Collider[] componentsInChildren2 = transform.GetComponentsInChildren<Collider>(true);
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			Collider collider = componentsInChildren2[j];
			if ((UnityEngine.Object)(object)collider == (UnityEngine.Object)null || !collider.enabled || collider.isTrigger || collider.bounds.size.sqrMagnitude <= 0.001f)
			{
				continue;
			}

			if (!hasBounds)
			{
				bounds = collider.bounds;
				hasBounds = true;
			}
			else
			{
				bounds.Encapsulate(collider.bounds.min);
				bounds.Encapsulate(collider.bounds.max);
			}
		}

		return hasBounds;
	}

	private static void EncapsulatePointWithExtents(ref Bounds bounds, Vector3 center, Vector3 extents)
	{
		bounds.Encapsulate(center - extents);
		bounds.Encapsulate(center + extents);
	}

	private static bool HasParentType(Component component, params string[] typeNames)
	{
		Component[] componentsInParent = ((Component)component).GetComponentsInParent<Component>(true);
		for (int i = 0; i < componentsInParent.Length; i++)
		{
			Component component2 = componentsInParent[i];
			if ((UnityEngine.Object)(object)component2 == (UnityEngine.Object)null)
			{
				continue;
			}

			string name = component2.GetType().Name;
			for (int j = 0; j < typeNames.Length; j++)
			{
				if (string.Equals(name, typeNames[j], StringComparison.Ordinal))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static string GetHierarchyContext(Transform transform)
	{
		string text = string.Empty;
		int num = 0;
		while ((UnityEngine.Object)(object)transform != (UnityEngine.Object)null && num < 6)
		{
			if (text.Length > 0)
			{
				text = text + "/" + transform.name;
			}
			else
			{
				text = transform.name;
			}

			transform = transform.parent;
			num++;
		}

		return text.ToLowerInvariant();
	}

	private static bool ContainsAny(string source, params string[] values)
	{
		if (string.IsNullOrEmpty(source))
		{
			return false;
		}

		for (int i = 0; i < values.Length; i++)
		{
			if (source.IndexOf(values[i], StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}

		return false;
	}

	private static string GetComponentTypeName(Component component)
	{
		return ((UnityEngine.Object)(object)component != (UnityEngine.Object)null) ? component.GetType().Name : string.Empty;
	}
}
