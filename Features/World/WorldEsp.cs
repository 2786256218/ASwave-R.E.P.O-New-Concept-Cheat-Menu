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
		Trap,
		Cart,
		Special
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

	private static readonly TargetDefinition[] Definitions = new TargetDefinition[]
	{
		new TargetDefinition("ExtractionPoint", TargetCategory.ExtractionPoint, "提取点", new Vector3(2f, 3f, 2f), 42f, 84f),
		new TargetDefinition("TruckSafetySpawnPoint", TargetCategory.EvacuationPoint, "撤离点", new Vector3(2.4f, 2.4f, 2.4f), 44f, 44f),
		new TargetDefinition("MuseumLaserLogic", TargetCategory.Trap, "激光陷阱", new Vector3(3.5f, 0.6f, 0.6f), 34f, 20f),
		new TargetDefinition("DeathPitForce", TargetCategory.Trap, "坑洞陷阱", new Vector3(4f, 2f, 4f), 46f, 24f),
		new TargetDefinition("HurtCollider", TargetCategory.Trap, "环境陷阱", new Vector3(3f, 1f, 3f), 40f, 22f),
		new TargetDefinition("Fireplace", TargetCategory.Trap, "壁炉", new Vector3(2f, 2f, 2f), 30f, 30f),
                new TargetDefinition("Fire", TargetCategory.Trap, "火焰", new Vector3(1f, 1f, 1f), 20f, 20f),
                new TargetDefinition("BarrelValuable", TargetCategory.Trap, "火桶", new Vector3(1f, 1.5f, 1f), 20f, 20f),
                new TargetDefinition("PropaneTankTrap", TargetCategory.Trap, "煤气罐", new Vector3(1f, 1.5f, 1f), 20f, 20f),
                new TargetDefinition("PhysGrabCart", TargetCategory.Cart, "小推车", new Vector3(1.5f, 1.5f, 2f), 30f, 30f),
                new TargetDefinition("ValuableArcticSnowBike", TargetCategory.Cart, "雪地摩托", new Vector3(2.2f, 1.8f, 3.2f), 34f, 28f),
                new TargetDefinition("ValuableDesertGolfcart", TargetCategory.Cart, "沙漠载物车", new Vector3(2.4f, 2f, 3.4f), 36f, 30f),
                new TargetDefinition("ItemVehicle", TargetCategory.Cart, "载具", new Vector3(2.2f, 1.8f, 3.2f), 34f, 28f),
                new TargetDefinition("CosmeticWorldObject", TargetCategory.Special, "代币箱子", new Vector3(0.5f, 0.5f, 0.5f), 30f, 30f),
                new TargetDefinition("ShopKeycard", TargetCategory.Special, "办公室门禁卡", new Vector3(0.2f, 0.05f, 0.2f), 30f, 30f)
        };

	private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>(StringComparer.Ordinal);

	private static readonly List<RuntimeTarget> CachedTargets = new List<RuntimeTarget>(64);

	private static readonly HashSet<int> SeenInstanceIds = new HashSet<int>();

	private const float RefreshInterval = 1f;

	private static float _nextRefreshAt;

	public static void Draw()
	{
		ConfigData.StructureEspSettings settings = ConfigManager.Config?.Structures;
		ConfigData.CartEspSettings cartSettings = ConfigManager.Config?.Carts;
		ConfigData.SpecialsSettings specialSettings = ConfigManager.Config?.Specials;

		if (settings == null || !HasAnyEnabled(settings, cartSettings, specialSettings))
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
			RefreshTargets(settings, cartSettings, specialSettings);
		}
		
		// Update dynamic labels per frame for extraction points
		if (settings.ExtractionPointsEnabled)
		{
			for (int i = 0; i < CachedTargets.Count; i++)
			{
				if (CachedTargets[i].Category == TargetCategory.ExtractionPoint && CachedTargets[i].Component != null)
				{
					ExtractionPoint ep = CachedTargets[i].Component as ExtractionPoint;
					if (ep != null)
					{
						string baseLabel = "提取点";
						var stateField = typeof(ExtractionPoint).GetField("currentState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
						if (stateField != null)
						{
							ExtractionPoint.State state = (ExtractionPoint.State)stateField.GetValue(ep);
							switch (state)
							{
								case ExtractionPoint.State.Idle:
									CachedTargets[i].Label = baseLabel + " [未开启]";
									break;
								case ExtractionPoint.State.Active:
								case ExtractionPoint.State.Extracting:
									CachedTargets[i].Label = baseLabel + " [已开启]";
									break;
								case ExtractionPoint.State.Complete:
								case ExtractionPoint.State.Success:
									CachedTargets[i].Label = baseLabel + " [已完成]";
									break;
								default:
									CachedTargets[i].Label = baseLabel;
									break;
							}
						}
					}
				}
			}
		}

		Vector2 tracerOrigin = new Vector2((float)Screen.width * 0.5f, (float)Screen.height - 18f);
		Vector3 cameraPosition = ((Component)mainCamera).transform.position;
		for (int i = CachedTargets.Count - 1; i >= 0; i--)
		{
			RuntimeTarget target = CachedTargets[i];
			if (!IsValidTarget(target) || !IsCategoryEnabled(settings, cartSettings, target.Category))
			{
				if (!IsValidTarget(target))
				{
					CachedTargets.RemoveAt(i);
				}
				continue;
			}

			if (target.Category == TargetCategory.Special)
			{
				if (specialSettings == null) continue;
				string typeName = target.Component != null ? target.Component.GetType().Name : "";
				if (typeName == "CosmeticWorldObject" && !specialSettings.TokenBoxEnabled) continue;
				if (typeName == "ShopKeycard" && !specialSettings.KeycardEnabled) continue;
			}

			Vector3 worldPosition = GetWorldPosition(target);
			float distance = Vector3.Distance(cameraPosition, worldPosition);
			float maxDistance = settings.MaxDistance;
			if (target.Category == TargetCategory.Cart)
			{
				maxDistance = cartSettings != null ? cartSettings.MaxDistance : 250f;
			}
			else if (target.Category == TargetCategory.Special)
			{
				string typeName = target.Component != null ? target.Component.GetType().Name : "";
				if (typeName == "CosmeticWorldObject")
				{
					maxDistance = specialSettings != null ? specialSettings.TokenBoxMaxDistance : 250f;
				}
				else if (typeName == "ShopKeycard")
				{
					maxDistance = specialSettings != null ? specialSettings.KeycardMaxDistance : 250f;
				}
			}
			if (distance > maxDistance)
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

			Color color = GetCategoryColor(settings, cartSettings, target);
			
			bool drawTracers = settings.DrawTracers;
			bool drawBox = settings.DrawBox;
			bool drawName = settings.DrawName;
			bool drawDistance = settings.DrawDistance;
			bool drawInfo = false;

			if (target.Category == TargetCategory.Cart)
			{
				drawTracers = cartSettings != null && cartSettings.DrawTracers;
				drawBox = cartSettings != null && cartSettings.DrawBox;
				drawName = cartSettings != null && cartSettings.DrawName;
				drawDistance = cartSettings != null && cartSettings.DrawDistance;
				drawInfo = cartSettings != null && cartSettings.DrawInfo;
			}
			else if (target.Category == TargetCategory.Special)
			{
				string typeName = target.Component != null ? target.Component.GetType().Name : "";
				if (typeName == "CosmeticWorldObject")
				{
					drawTracers = specialSettings != null && specialSettings.TokenBoxTracers;
					drawBox = specialSettings != null && specialSettings.TokenBoxBox;
					drawName = specialSettings != null && specialSettings.TokenBoxName;
					drawDistance = specialSettings != null && specialSettings.TokenBoxDistance;
					drawInfo = specialSettings != null && specialSettings.TokenBoxInfo;
				}
				else if (typeName == "ShopKeycard")
				{
					drawTracers = specialSettings != null && specialSettings.KeycardTracers;
					drawBox = specialSettings != null && specialSettings.KeycardBox;
					drawName = specialSettings != null && specialSettings.KeycardName;
					drawDistance = specialSettings != null && specialSettings.KeycardDistance;
				}
			}

			if (drawTracers)
			{
				Render.DrawLine(tracerOrigin, new Vector2(rect.center.x, rect.yMax), color, 1.2f);
			}

			if (drawBox)
			{
				DrawTargetMarker(target.Category, rect, color);
			}

			if (drawName)
			{
				Render.DrawStringOutlined(new Rect(rect.x - 50f, rect.y - 18f, rect.width + 100f, 18f), target.Label, color, center: true, 12, bold: true);
			}

			if (drawDistance)
			{
				Render.DrawStringOutlined(new Rect(rect.x - 50f, rect.yMax + 2f, rect.width + 100f, 18f), string.Format("{0:F0}m", distance), color, center: true, 12, bold: true);
			}
			
			if (target.Category == TargetCategory.Special && target.Component != null && drawInfo)
			{
				string typeName = target.Component.GetType().Name;
				if (typeName == "CosmeticWorldObject")
				{
					try
					{
						int healthCurrent = 0;
						string rarityStr = "Common";
						
						var rarityField = target.Component.GetType().GetField("rarity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
						if (rarityField != null)
						{
							object rarityVal = rarityField.GetValue(target.Component);
							if (rarityVal != null)
							{
								rarityStr = rarityVal.ToString();
							}
						}

						Component notValuableObj = target.Component.GetComponent("NotValuableObject");
						if (notValuableObj != null)
						{
							var healthField = notValuableObj.GetType().GetField("healthCurrent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
							if (healthField != null)
							{
								healthCurrent = (int)healthField.GetValue(notValuableObj);
							}
						}

						float currentY = rect.y - 36f;
						Render.DrawStringOutlined(new Rect(rect.x - 100f, currentY, rect.width + 200f, 18f), $"[{rarityStr} | 血量: {healthCurrent}]", color, center: true, 12, bold: true);
					}
					catch { }
				}
			}
			if (target.Category == TargetCategory.Cart && target.Component != null && drawInfo)
			{
				try
				{
					float currentY = rect.y - 36f; // 调整到框的上方，避免被底部的距离等信息遮挡
					var cartObj = target.Component;
					Transform inCart = cartObj.transform.Find("In Cart");
					if (inCart != null)
					{
						Collider[] array = Physics.OverlapBox(inCart.position, inCart.localScale / 2f, inCart.rotation);
						int itemCount = 0;
						int totalValue = 0;
						List<int> countedIds = new List<int>();

						foreach (Collider collider in array)
						{
							if (collider.gameObject.layer != LayerMask.NameToLayer("PhysGrabObject")) continue;

							MonoBehaviour[] monos = collider.GetComponentsInParent<MonoBehaviour>();
							MonoBehaviour physGrab = null;
							MonoBehaviour valuableObj = null;

							foreach (var m in monos)
							{
								if (m == null) continue;
								string typeName = m.GetType().Name;
								if (typeName == "PhysGrabObject" && physGrab == null) physGrab = m;
								if (typeName == "ValuableObject" && valuableObj == null) valuableObj = m;
							}

							if (physGrab != null)
							{
								int id = physGrab.gameObject.GetInstanceID();
								if (!countedIds.Contains(id))
								{
									countedIds.Add(id);
									itemCount++;

									if (valuableObj != null)
									{
										var dollarValField = valuableObj.GetType().GetField("dollarValueCurrent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
										if (dollarValField != null)
										{
											try
											{
												object val = dollarValField.GetValue(valuableObj);
												if (val != null)
												{
													totalValue += System.Convert.ToInt32(val);
												}
											}
											catch { }
										}
									}
								}
							}
						}

						if (itemCount == 0)
						{
							Render.DrawStringOutlined(new Rect(rect.x - 50f, currentY, rect.width + 100f, 18f), "[空推车]", color, center: true, 12, bold: true);
						}
						else
						{
							Render.DrawStringOutlined(new Rect(rect.x - 100f, currentY, rect.width + 200f, 18f), $"[物品: {itemCount} | 价值: ${totalValue}]", color, center: true, 12, bold: true);
						}
					}
				}
				catch { }
			}
		}
	}

	private static void RefreshTargets(ConfigData.StructureEspSettings settings, ConfigData.CartEspSettings cartSettings, ConfigData.SpecialsSettings specialSettings)
	{
		CachedTargets.Clear();
		SeenInstanceIds.Clear();

		for (int i = 0; i < Definitions.Length; i++)
		{
			TargetDefinition definition = Definitions[i];
			if (!IsCategoryEnabled(settings, cartSettings, definition.Category))
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
				if (target.Category == TargetCategory.Cart)
				{
					PhysGrabObject physGrabObject = component.GetComponentInParent<PhysGrabObject>();
					if ((UnityEngine.Object)(object)physGrabObject != (UnityEngine.Object)null)
						instanceId = ((UnityEngine.Object)physGrabObject).GetInstanceID();
				}
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

	private static bool HasAnyEnabled(ConfigData.StructureEspSettings settings, ConfigData.CartEspSettings cartSettings, ConfigData.SpecialsSettings specialSettings)
	{
		return settings.ExtractionPointsEnabled || settings.EvacuationPointsEnabled || settings.TrapsEnabled || (cartSettings != null && cartSettings.Enabled) || (specialSettings != null && specialSettings.Enabled);
	}

	private static bool IsCategoryEnabled(ConfigData.StructureEspSettings settings, ConfigData.CartEspSettings cartSettings, TargetCategory category)
	{
		switch (category)
		{
		case TargetCategory.ExtractionPoint:
			return settings.ExtractionPointsEnabled;
		case TargetCategory.EvacuationPoint:
			return settings.EvacuationPointsEnabled;
		case TargetCategory.Trap:
			return settings.TrapsEnabled;
		case TargetCategory.Cart:
			return cartSettings != null && cartSettings.Enabled;
		case TargetCategory.Special:
			var specialSettings = ConfigManager.Config?.Specials;
			return specialSettings != null && specialSettings.Enabled;
		default:
			return false;
		}
	}

	private static Color GetCategoryColor(ConfigData.StructureEspSettings settings, ConfigData.CartEspSettings cartSettings, RuntimeTarget target)
	{
		switch (target.Category)
		{
		case TargetCategory.ExtractionPoint:
			return settings.ExtractionPointColor;
		case TargetCategory.EvacuationPoint:
			return settings.EvacuationPointColor;
		case TargetCategory.Trap:
			return settings.TrapColor;
		case TargetCategory.Cart:
			return cartSettings != null ? cartSettings.Color : Color.white;
		case TargetCategory.Special:
			string typeName = target.Component != null ? target.Component.GetType().Name : string.Empty;
			ConfigData.SpecialsSettings specialSettings = ConfigManager.Config?.Specials;
			if (specialSettings != null && typeName == "CosmeticWorldObject")
			{
				return specialSettings.TokenBoxColor;
			}
			if (specialSettings != null && typeName == "ShopKeycard")
			{
				return specialSettings.KeycardColor;
			}
			return Color.white;
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
		Render.DrawEspBox(rect, color, thickness, ConfigManager.Config.Enemies.RenderMethod);
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
		if (string.Equals(componentTypeName, "MuseumLaserLogic", StringComparison.Ordinal) || 
			string.Equals(componentTypeName, "DeathPitForce", StringComparison.Ordinal) || 
			string.Equals(componentTypeName, "Fireplace", StringComparison.Ordinal) || 
			string.Equals(componentTypeName, "Fire", StringComparison.Ordinal) || 
			string.Equals(componentTypeName, "BarrelValuable", StringComparison.Ordinal) || 
			string.Equals(componentTypeName, "PropaneTankTrap", StringComparison.Ordinal))
		{
			return true;
		}

		return string.Equals(componentTypeName, "HurtCollider", StringComparison.Ordinal) && TryResolveTrapLabel(component, out _);
	}

	private static string ResolveLabel(Component component, TargetDefinition definition)
	{
		if (definition.Category == TargetCategory.Cart && component != null && definition.TypeName != "PhysGrabCart")
		{
			ItemAttributes attributes = component.GetComponentInParent<ItemAttributes>();
			if (attributes == null)
				attributes = component.GetComponentInChildren<ItemAttributes>(true);
			string itemName = attributes != null && attributes.item != null ? attributes.item.itemName : null;
			if (!string.IsNullOrWhiteSpace(itemName))
				return itemName.Trim();
		}

		if (definition.Category == TargetCategory.Trap && TryResolveTrapLabel(component, out var label))
		{
			return label;
		}

		if (definition.Category == TargetCategory.ExtractionPoint && component != null)
		{
			ExtractionPoint ep = component as ExtractionPoint;
			if (ep != null)
			{
				var stateField = typeof(ExtractionPoint).GetField("currentState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
				if (stateField != null)
				{
					ExtractionPoint.State state = (ExtractionPoint.State)stateField.GetValue(ep);
					switch (state)
					{
						case ExtractionPoint.State.Idle:
							return definition.Label + " [未开启]";
						case ExtractionPoint.State.Active:
						case ExtractionPoint.State.Extracting:
							return definition.Label + " [已开启]";
						case ExtractionPoint.State.Complete:
						case ExtractionPoint.State.Success:
							return definition.Label + " [已完成]";
					}
				}
			}
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

		if (string.Equals(componentTypeName, "Fireplace", StringComparison.Ordinal))
		{
			label = "壁炉";
			return true;
		}

		if (string.Equals(componentTypeName, "Fire", StringComparison.Ordinal))
		{
			label = "火焰";
			return true;
		}

		if (string.Equals(componentTypeName, "BarrelValuable", StringComparison.Ordinal))
		{
			label = "火桶";
			return true;
		}

		if (string.Equals(componentTypeName, "PropaneTankTrap", StringComparison.Ordinal))
		{
			label = "煤气罐";
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

		if (IsFireTrap(component, text))
		{
			label = "火焰陷阱";
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

	private static bool IsFireTrap(Component component, string hierarchyContext)
	{
		if (ContainsAny(hierarchyContext, "fire", "flame", "fireplace", "campfire", "bonfire", "furnace", "burn", "embers", "coal", "torch"))
		{
			return true;
		}

		Transform transform = ((Component)component).transform;
		if ((UnityEngine.Object)(object)transform == (UnityEngine.Object)null)
		{
			return false;
		}

		ParticleSystem[] componentsInChildren = transform.GetComponentsInChildren<ParticleSystem>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			ParticleSystem particleSystem = componentsInChildren[i];
			if ((UnityEngine.Object)(object)particleSystem == (UnityEngine.Object)null)
			{
				continue;
			}

			string text = (particleSystem.name + "/" + particleSystem.gameObject.name).ToLowerInvariant();
			if (ContainsAny(text, "fire", "flame", "smoke", "ember", "burn", "ash"))
			{
				return true;
			}
		}

		Light[] componentsInChildren2 = transform.GetComponentsInChildren<Light>(true);
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			Light light = componentsInChildren2[j];
			if ((UnityEngine.Object)(object)light == (UnityEngine.Object)null)
			{
				continue;
			}

			string text2 = (light.name + "/" + light.gameObject.name).ToLowerInvariant();
			if (ContainsAny(text2, "fire", "flame", "torch", "campfire", "fireplace"))
			{
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
