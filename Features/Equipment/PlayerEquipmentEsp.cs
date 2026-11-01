using System;
using System.Collections.Generic;
using Cheat.Config;
using Cheat.UI;
using Cheat.Utils;
using UnityEngine;

namespace Cheat.Features.Equipment;

/// <summary>Draws markers for loose player equipment and weapons in the current scene.</summary>
public static class PlayerEquipmentEsp
{
	private static readonly Vector3 FallbackSize = new Vector3(0.45f, 0.45f, 0.45f);

	public static void Draw()
	{
		ConfigData.PlayerEquipmentEspSettings settings = ConfigManager.Config?.PlayerEquipmentEsp;
		if (settings == null || !settings.Enabled || RoundDirector.instance == null
			|| Event.current == null || Event.current.type != EventType.Repaint)
			return;

		Camera camera = Camera.main;
		if (camera == null)
			return;

		List<PhysGrabObject> objects = ReflectionUtils.GetFieldValue<List<PhysGrabObject>>(RoundDirector.instance, "physGrabObjects");
		if (objects == null)
			return;

		Vector3 cameraPosition = camera.transform.position;
		float maxDistance = Mathf.Clamp(settings.MaxDistance, 10f, 1000f);
		float maxDistanceSqr = maxDistance * maxDistance;
		Vector2 screenBottom = new Vector2(Screen.width * 0.5f, Screen.height - 18f);

		for (int i = 0; i < objects.Count; i++)
		{
			PhysGrabObject physGrabObject = objects[i];
			if (!TryGetLooseEquipment(physGrabObject, out ItemAttributes attributes))
				continue;

			Transform itemTransform = physGrabObject.transform;
			if ((itemTransform.position - cameraPosition).sqrMagnitude > maxDistanceSqr)
				continue;

			Renderer[] renderers = itemTransform.GetComponentsInChildren<Renderer>(true);
			Vector3 fallbackCenter = itemTransform.position;
			Bounds worldBounds = EspProjectionUtils.GetWorldBounds(itemTransform, renderers, ShouldUseRenderer,
				fallbackCenter, FallbackSize);
			if (!EspProjectionUtils.TryGetScreenRect(camera, itemTransform, renderers, ShouldUseRenderer,
				worldBounds.center, worldBounds.size, 12f, 12f, out Rect rect))
				continue;

			Color color = settings.Color;
			if (settings.DrawTracers)
				Render.DrawLine(screenBottom, new Vector2(rect.center.x, rect.yMax), color, 1.15f);
			if (settings.DrawBox)
				Render.DrawEspBox(rect, color, 1.5f, ConfigManager.Config.Enemies.RenderMethod);

			string itemName = ResolveItemName(attributes, physGrabObject);
			string label = settings.DrawName ? itemName : string.Empty;
			if (settings.DrawDistance)
			{
				string distance = Vector3.Distance(cameraPosition, itemTransform.position).ToString("F0") + "m";
				label = string.IsNullOrEmpty(label) ? distance : label + " [" + distance + "]";
			}
			if (!string.IsNullOrEmpty(label))
				Render.DrawStringOutlined(new Rect(rect.x - 48f, rect.y - 19f, rect.width + 96f, 18f), label, color, center: true, 12, bold: true);
		}
	}

	private static bool TryGetLooseEquipment(PhysGrabObject physGrabObject, out ItemAttributes attributes)
	{
		attributes = null;
		if (physGrabObject == null || physGrabObject.dead || !physGrabObject.gameObject.activeInHierarchy)
			return false;

		attributes = physGrabObject.GetComponent<ItemAttributes>();
		if (attributes == null || attributes.item == null || !IsPlayerEquipmentType(attributes.item.itemType))
			return false;

		if (physGrabObject.grabbedLocal || (physGrabObject.playerGrabbing != null && physGrabObject.playerGrabbing.Count > 0))
			return false;

		ItemEquippable equippable = physGrabObject.GetComponent<ItemEquippable>();
		if (equippable != null && equippable.IsEquipped())
			return false;

		if (physGrabObject.GetComponentInParent<PlayerAvatar>() != null)
			return false;

		PhysGrabObjectImpactDetector impactDetector = physGrabObject.GetComponent<PhysGrabObjectImpactDetector>();
		if (impactDetector != null && ReflectionUtils.GetFieldValue<bool>(impactDetector, "inCart"))
			return false;

		return true;
	}

	private static bool IsPlayerEquipmentType(SemiFunc.itemType itemType)
	{
		switch (itemType)
		{
		case SemiFunc.itemType.drone:
		case SemiFunc.itemType.orb:
		case SemiFunc.itemType.item_upgrade:
		case SemiFunc.itemType.player_upgrade:
		case SemiFunc.itemType.grenade:
		case SemiFunc.itemType.melee:
		case SemiFunc.itemType.healthPack:
		case SemiFunc.itemType.gun:
		case SemiFunc.itemType.tracker:
		case SemiFunc.itemType.mine:
		case SemiFunc.itemType.tool:
		case SemiFunc.itemType.launcher:
			return true;
		default:
			return false;
		}
	}

	private static bool ShouldUseRenderer(Renderer renderer)
	{
		return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy
			&& !(renderer is ParticleSystemRenderer) && !(renderer is TrailRenderer) && !(renderer is LineRenderer)
			&& renderer.bounds.size.sqrMagnitude > 0.0001f;
	}

	private static string ResolveItemName(ItemAttributes attributes, PhysGrabObject physGrabObject)
	{
		string name = attributes != null && attributes.item != null ? attributes.item.itemName : null;
		if (string.IsNullOrWhiteSpace(name))
			name = physGrabObject != null ? physGrabObject.gameObject.name : "装备";
		return name.Replace("(Clone)", string.Empty).Trim();
	}
}
