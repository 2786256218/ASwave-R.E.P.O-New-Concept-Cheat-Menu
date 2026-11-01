using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cheat.Features.LocalPlayer;
using Cheat.Features.Teleport;
using UnityEngine;
using Photon.Pun;
using Object = UnityEngine.Object;

namespace Cheat.Features.Loot;

public static class LootTeleporter
{
	public static string LastStatus { get; private set; } = "可将当前地图中的松散物品批量传送到玩家附近。";

	private const float ValuableProtectionSeconds = 3.0f;
	private static bool _physicalPullRoutineActive;
	private static readonly MethodInfo ForceGrabMethod = typeof(PhysGrabber).GetMethod("ForceGrabPhysObject", BindingFlags.Instance | BindingFlags.NonPublic);

	public static int TeleportValuablesToPlayer()
	{
		if (!NativeTeleportModule.TryBeginAction(TeleportAction.Valuables))
		{
			LastStatus = NativeTeleportModule.LastStatus;
			return 0;
		}
		#region debug-point A:loot-valuables-entry
		LocalPlayerManager.DebugReport("A", "LootTeleporter.TeleportValuablesToPlayer", "进入有价值物品传送入口", "isMultiplayer=" + GameManager.Multiplayer() + ",isMasterOrSingle=" + SemiFunc.IsMasterClientOrSingleplayer() + ",player=" + (((Object)(object)PlayerController.instance != (Object)null) ? ((Component)PlayerController.instance).transform.position.ToString() : "null"));
		#endregion
		if (!TryGetLooseObjects(out List<PhysGrabObject> sourceObjects, out Transform playerTransform))
		{
			return 0;
		}

		Vector3 targetCenter = ResolvePointerGroundTarget(playerTransform);
		PlayerAvatar sourcePlayerAvatar = PlayerAvatar.instance;

		return ExecuteTeleportInternal(sourceObjects, targetCenter, playerTransform, sourcePlayerAvatar, teleportDoors: false);
	}

	public static int TeleportDoorsToPlayer()
	{
		if (!NativeTeleportModule.TryBeginAction(TeleportAction.Doors))
		{
			LastStatus = NativeTeleportModule.LastStatus;
			return 0;
		}
		#region debug-point A:loot-doors-entry
		LocalPlayerManager.DebugReport("A", "LootTeleporter.TeleportDoorsToPlayer", "进入门传送入口", "isMultiplayer=" + GameManager.Multiplayer() + ",isMasterOrSingle=" + SemiFunc.IsMasterClientOrSingleplayer() + ",player=" + (((Object)(object)PlayerController.instance != (Object)null) ? ((Component)PlayerController.instance).transform.position.ToString() : "null"));
		#endregion
		if (!TryGetLooseObjects(out List<PhysGrabObject> sourceObjects, out Transform playerTransform))
		{
			return 0;
		}

		Vector3 targetCenter = ResolvePointerGroundTarget(playerTransform);
		PlayerAvatar sourcePlayerAvatar = PlayerAvatar.instance;
		return ExecuteTeleportInternal(sourceObjects, targetCenter, playerTransform, sourcePlayerAvatar, teleportDoors: true);
	}

	public static int BreakAllDoors()
	{
		if (!TryGetLooseObjects(out List<PhysGrabObject> sourceObjects, out Transform playerTransform))
		{
			return 0;
		}

		PlayerAvatar sourcePlayerAvatar = PlayerAvatar.instance;
		List<PhysGrabObject> doors = new List<PhysGrabObject>();
		foreach (PhysGrabObject item in sourceObjects)
		{
			if (IsTeleportableDoor(item, sourcePlayerAvatar))
				doors.Add(item);
		}
		if (doors.Count == 0)
		{
			LastStatus = "当前没有可抓取的门。";
			return 0;
		}
		return StartPhysicalPull(doors, playerTransform, teleportDoors: true);
	}

	private static int ExecuteTeleportInternal(List<PhysGrabObject> sourceObjects, Vector3 targetCenter, Transform playerTransform, PlayerAvatar sourcePlayerAvatar, bool teleportDoors)
	{
		List<PhysGrabObject> candidates = new List<PhysGrabObject>();
		foreach (PhysGrabObject physGrabObject in sourceObjects)
		{
			bool canTeleport = teleportDoors ? IsTeleportableDoor(physGrabObject, sourcePlayerAvatar) : IsTeleportableValuable(physGrabObject, sourcePlayerAvatar);
			if (canTeleport)
			{
				candidates.Add(physGrabObject);
			}
		}

		if (candidates.Count == 0)
		{
			LastStatus = teleportDoors ? "当前未找到可传送的门。" : "未找到可传送的有价值物品，已自动排除门、玩家装备、持有物和无价值场景物体。";
			return 0;
		}

		// A non-host cannot authoritatively relocate a remote-owned rigidbody by
		// writing its local Transform. Use the normal owner-validated grab stream
		// so the master simulates and replicates the physical pull. Doors always
		// use this path so their hinge actually receives force and can break.
		if (teleportDoors || (GameManager.Multiplayer() && !SemiFunc.IsMasterClientOrSingleplayer()))
		{
			return StartPhysicalPull(candidates, playerTransform, teleportDoors);
		}

		#region debug-point B:loot-teleport-candidates
		LocalPlayerManager.DebugReport("B", "LootTeleporter.ExecuteTeleportInternal", "扫描到可传送对象", "teleportDoors=" + teleportDoors + ",sourceCount=" + sourceObjects.Count + ",candidateCount=" + candidates.Count + ",targetCenter=" + targetCenter);
		#endregion

		int teleportedCount = 0;
		float protectionSeconds = teleportDoors ? 0.35f : ValuableProtectionSeconds;
		for (int i = 0; i < candidates.Count; i++)
		{
			PhysGrabObject candidate = candidates[i];
			if ((Object)(object)candidate == (Object)null)
			{
				continue;
			}

			Vector3 targetPosition = ResolvePlacementPoint(targetCenter, playerTransform, i, candidate);
			if (TeleportObject(candidate, targetPosition, protectionSeconds))
				teleportedCount++;
		}

		if (teleportedCount == 0)
		{
			LastStatus = string.IsNullOrEmpty(NativeTeleportModule.LastStatus)
				? "没有对象能通过本地物理路径移动。"
				: NativeTeleportModule.LastStatus;
			return 0;
		}
		LastStatus = string.Format("已移动 {0} 件有价值物品，并开启短时保护。", teleportedCount);
		return teleportedCount;
	}

	private static int StartPhysicalPull(List<PhysGrabObject> objects, Transform playerTransform, bool teleportDoors)
	{
		PlayerAvatar player = PlayerAvatar.instance;
		PhysGrabber grabber = player != null ? player.physGrabber : null;
		if (_physicalPullRoutineActive)
		{
			LastStatus = "上一批物品仍在移动，请稍候。";
			return 0;
		}
		if (player == null || playerTransform == null || grabber == null || !grabber.isLocal)
		{
			LastStatus = "抓取功能暂不可用，无法移动物品。";
			return 0;
		}
		if (grabber.grabbed)
		{
			LastStatus = "请先松开当前手持物品，再启动批量牵引。";
			return 0;
		}

		float reach = Mathf.Clamp(NativeTeleportModule.Settings.MaxDistance + 5f, 15f, 10000f);
		_physicalPullRoutineActive = true;
		LocalPlayerManager.SetTemporaryGrabOverrides(5f, reach);
		player.StartCoroutine(PullObjectsRoutine(objects, playerTransform, grabber, teleportDoors, reach));
		LastStatus = teleportDoors
			? $"正在移动 {objects.Count} 扇门，抓取力量已提高；部分门可能会断开。"
			: $"正在移动 {objects.Count} 件物品。";
		return objects.Count;
	}

	private static IEnumerator PullObjectsRoutine(List<PhysGrabObject> objects, Transform playerTransform, PhysGrabber grabber, bool teleportDoors, float reach)
	{
		int moved = 0;
		int broken = 0;
		int failed = 0;
		try
		{
			for (int i = 0; i < objects.Count; i++)
			{
				PhysGrabObject item = objects[i];
				if (item == null || playerTransform == null || grabber == null || !item.gameObject.activeInHierarchy)
				{
					failed++;
					continue;
				}
				float initialDistance = Vector3.Distance(item.transform.position, playerTransform.position);
				if (initialDistance > reach)
				{
					failed++;
					continue;
				}
				if (item.playerGrabbing != null && item.playerGrabbing.Count > 0)
				{
					failed++;
					continue;
				}

				PhysGrabHinge hinge = item.GetComponent<PhysGrabHinge>();
				if (hinge == null)
					hinge = item.GetComponentInChildren<PhysGrabHinge>(true);
				PhotonView itemView = item.GetComponent<PhotonView>();
				if (GameManager.Multiplayer() && (itemView == null || itemView.ViewID == 0))
				{
					failed++;
					continue;
				}

				float grabWait = 0f;
				while (grabWait < 1.5f && grabber != null && !grabber.grabbed)
				{
					grabber.OverrideGrabDistance(initialDistance + 1f);
					grabber.OverrideGrab(item, 0.5f, false);
					// OverrideGrab normally waits for PhysGrabber's next Update tick.
					// Invoke the game's own grab routine too, so menu/input timing
					// cannot prevent the local player grab from starting.
					TryStartNativeGrab(grabber, item);
					grabWait += Time.deltaTime;
					yield return null;
				}
				// Give the game's grab-link RPC one more frame to populate the target.
				for (int frame = 0; frame < 3 && grabber != null && grabber.grabbed; frame++)
					yield return null;

				PhysGrabObject grabbedObject = ReflectionUtils.GetFieldValue<PhysGrabObject>(grabber, "grabbedPhysGrabObject");
				if (grabber == null || grabbedObject != item)
				{
					if (grabber != null)
						grabber.OverrideGrab(null, 0f, false);
					failed++;
					continue;
				}

				float elapsed = 0f;
				float pullTimeout = Mathf.Clamp(initialDistance / 2.5f + (teleportDoors ? 12f : 8f), 12f, 180f);
				bool hingeBroken = false;
				while (elapsed < pullTimeout && item != null && playerTransform != null &&
					ReflectionUtils.GetFieldValue<PhysGrabObject>(grabber, "grabbedPhysGrabObject") == item)
				{
					grabber.OverrideGrabDistance(1.5f);
					if (hinge != null && ReflectionUtils.GetFieldValue<bool>(hinge, "broken"))
						hingeBroken = true;
					if (Vector3.Distance(item.transform.position, playerTransform.position) <= 3.5f)
						break;
					elapsed += Time.deltaTime;
					yield return null;
				}

				float finalDistance = item != null && playerTransform != null
					? Vector3.Distance(item.transform.position, playerTransform.position)
					: float.MaxValue;
				int viewId = item != null && item.GetComponent<PhotonView>() != null ? item.GetComponent<PhotonView>().ViewID : -1;
				if (grabber != null && grabber.grabbed && viewId > 0)
					grabber.ReleaseObject(viewId, 0.1f);

				if (hinge != null && ReflectionUtils.GetFieldValue<bool>(hinge, "broken"))
					hingeBroken = true;
				if (finalDistance <= 4.5f || initialDistance - finalDistance >= 2.5f)
					moved++;
				else
					failed++;
				if (teleportDoors && hingeBroken)
					broken++;
				LastStatus = teleportDoors
					? $"门移动进度：{i + 1}/{objects.Count}；已移动 {moved}，已断开 {broken}，未移动 {failed}。"
					: $"物品移动进度：{i + 1}/{objects.Count}；已移动 {moved}，未移动 {failed}。";
			}
		}
		finally
		{
			if (grabber != null && grabber.grabbed)
			{
				PhysGrabObject current = ReflectionUtils.GetFieldValue<PhysGrabObject>(grabber, "grabbedPhysGrabObject");
				PhotonView currentView = current != null ? current.GetComponent<PhotonView>() : null;
				if (currentView != null)
					grabber.ReleaseObject(currentView.ViewID, 0.1f);
			}
			LocalPlayerManager.SetTemporaryGrabOverrides(1f, 0f);
			_physicalPullRoutineActive = false;
			LastStatus = teleportDoors
				? $"门移动完成：移动 {moved} 扇，断开 {broken} 扇，未移动 {failed} 扇。"
				: $"物品移动完成：移动 {moved} 件，未移动 {failed} 件。";
		}
	}

	private static bool TryStartNativeGrab(PhysGrabber grabber, PhysGrabObject item)
	{
		if (grabber == null || item == null || ForceGrabMethod == null)
			return false;
		try
		{
			ForceGrabMethod.Invoke(grabber, new object[] { item, false });
			return ReflectionUtils.GetFieldValue<PhysGrabObject>(grabber, "grabbedPhysGrabObject") == item;
		}
		catch (TargetInvocationException ex)
		{
			LocalPlayerManager.DebugReport("C", "LootTeleporter.TryStartNativeGrab", "游戏原生抓取调用异常", ex.InnerException?.Message ?? ex.Message);
			return false;
		}
		catch (Exception ex)
		{
			LocalPlayerManager.DebugReport("C", "LootTeleporter.TryStartNativeGrab", "游戏原生抓取调用异常", ex.Message);
			return false;
		}
	}

	private static bool TryGetLooseObjects(out List<PhysGrabObject> sourceObjects, out Transform playerTransform)
	{
		sourceObjects = null;
		playerTransform = null;
		#region debug-point A:loot-try-get-loose-objects
		LocalPlayerManager.DebugReport("A", "LootTeleporter.TryGetLooseObjects", "开始解析松散物体与玩家变换", "hasPlayerController=" + ((Object)(object)PlayerController.instance != (Object)null) + ",hasRoundDirector=" + ((Object)(object)RoundDirector.instance != (Object)null));
		#endregion
		if ((Object)(object)PlayerController.instance == (Object)null)
		{
			LastStatus = "你尚未进入游戏，暂时无法移动物品。";
			return false;
		}

		if ((Object)(object)RoundDirector.instance == (Object)null)
		{
			LastStatus = "场景尚未准备好，暂时无法查找物品。";
			return false;
		}

		sourceObjects = ReflectionUtils.GetFieldValue<List<PhysGrabObject>>(RoundDirector.instance, "physGrabObjects");
		if (sourceObjects == null || sourceObjects.Count == 0)
		{
			LastStatus = "当前地图没有可传送的物体。";
			return false;
		}

		playerTransform = ((Component)PlayerController.instance).transform;
		return true;
	}

	private static bool IsTeleportableValuable(PhysGrabObject physGrabObject, PlayerAvatar sourcePlayerAvatar)
	{
		if (!IsCommonTeleportCandidate(physGrabObject, sourcePlayerAvatar))
		{
			return false;
		}

		if ((Object)(object)((Component)physGrabObject).GetComponent<ValuableObject>() == (Object)null)
		{
			return false;
		}

		return !HasDoorHinge(physGrabObject);
	}

	private static bool IsTeleportableDoor(PhysGrabObject physGrabObject, PlayerAvatar sourcePlayerAvatar)
	{
		return IsCommonTeleportCandidate(physGrabObject, sourcePlayerAvatar) && HasDoorHinge(physGrabObject);
	}

	private static bool IsCommonTeleportCandidate(PhysGrabObject physGrabObject, PlayerAvatar sourcePlayerAvatar)
	{
		if ((Object)(object)physGrabObject == (Object)null)
		{
			return false;
		}

		GameObject gameObject = ((Component)physGrabObject).gameObject;
		if (!gameObject.scene.IsValid() || physGrabObject.dead)
		{
			return false;
		}

		if (ReflectionUtils.GetFieldValue<bool>(physGrabObject, "isPlayer") || ReflectionUtils.GetFieldValue<bool>(physGrabObject, "isEnemy"))
		{
			return false;
		}

		if (physGrabObject.playerGrabbing != null && physGrabObject.playerGrabbing.Count > 0)
		{
			return false;
		}

		if (physGrabObject.grabbedLocal)
		{
			return false;
		}

		if ((Object)(object)PlayerController.instance != (Object)null && (Object)(object)PlayerController.instance.physGrabObject == (Object)(object)gameObject)
		{
			return false;
		}

		if (BelongsToPlayerEquipment(physGrabObject, sourcePlayerAvatar))
		{
			return false;
		}

		if ((Object)(object)PlayerController.instance != (Object)null
			&& !NativeTeleportModule.IsWithinDistance(((Component)PlayerController.instance).transform.position, physGrabObject.transform.position))
			return false;

		PhysGrabObjectImpactDetector impactDetector = ReflectionUtils.GetFieldValue<PhysGrabObjectImpactDetector>(physGrabObject, "impactDetector");
		if ((Object)(object)impactDetector != (Object)null && ReflectionUtils.GetFieldValue<bool>(impactDetector, "inCart"))
		{
			return false;
		}

		return true;
	}

	private static bool BelongsToPlayerEquipment(PhysGrabObject physGrabObject, PlayerAvatar sourcePlayerAvatar)
	{
		if ((Object)(object)physGrabObject == (Object)null || (Object)(object)sourcePlayerAvatar == (Object)null)
		{
			return false;
		}

		if ((Object)(object)((Component)physGrabObject).GetComponentInParent<PlayerAvatar>() == (Object)(object)sourcePlayerAvatar)
		{
			return true;
		}

		return IsTrackedInPlayerCollection(sourcePlayerAvatar, "physObjectStander", physGrabObject);
	}

	private static bool IsTrackedInPlayerCollection(PlayerAvatar sourcePlayerAvatar, string fieldName, PhysGrabObject physGrabObject)
	{
		object container = ReflectionUtils.GetFieldValue(sourcePlayerAvatar, fieldName);
		List<PhysGrabObject> trackedObjects = ReflectionUtils.GetFieldValue<List<PhysGrabObject>>(container, "physGrabObjects");
		if (trackedObjects == null)
		{
			return false;
		}

		for (int i = 0; i < trackedObjects.Count; i++)
		{
			if ((Object)(object)trackedObjects[i] == (Object)(object)physGrabObject)
			{
				return true;
			}
		}

		return false;
	}

	private static bool HasDoorHinge(PhysGrabObject physGrabObject)
	{
		if ((Object)(object)physGrabObject == (Object)null)
		{
			return false;
		}

		Component component = (Component)physGrabObject;
		if ((Object)(object)component.GetComponent<PhysGrabHinge>() != (Object)null || (Object)(object)component.GetComponentInParent<PhysGrabHinge>() != (Object)null || (Object)(object)component.GetComponentInChildren<PhysGrabHinge>(true) != (Object)null)
			return true;

		// Newer map prefabs can mark a hinge on PhysGrabObject without exposing
		// the PhysGrabHinge component on the same hierarchy node.
		return ReflectionUtils.GetFieldValue<bool>(physGrabObject, "hasHinge");
	}

	private static Vector3 ResolvePointerGroundTarget(Transform playerTransform)
	{
		Camera main = Camera.main;
		if ((Object)(object)main == (Object)null)
		{
			Vector3 vector = playerTransform.position + playerTransform.forward * 2f;
			#region debug-point A:pointer-target-no-camera
			LocalPlayerManager.DebugReport("A", "LootTeleporter.ResolvePointerGroundTarget", "主相机不存在，使用玩家前方作为传送中心", "player=" + playerTransform.position + ",result=" + vector);
			#endregion
			return vector;
		}

		Vector3 target = Vector3.zero;
		if (TryRaycastPlacement(main.ScreenPointToRay(Input.mousePosition), playerTransform.position.y, out target))
		{
			target = ResolveGroundPoint(target, null, 0.01f);
			#region debug-point A:pointer-target-mouse-hit
			LocalPlayerManager.DebugReport("A", "LootTeleporter.ResolvePointerGroundTarget", "鼠标射线命中放置点", "player=" + playerTransform.position + ",mouse=" + Input.mousePosition + ",result=" + target);
			#endregion
			return target;
		}

		Vector3 screenCenter = new Vector3((float)Screen.width * 0.5f, (float)Screen.height * 0.5f, 0f);
		if (TryRaycastPlacement(main.ScreenPointToRay(screenCenter), playerTransform.position.y, out target))
		{
			target = ResolveGroundPoint(target, null, 0.01f);
			#region debug-point A:pointer-target-center-hit
			LocalPlayerManager.DebugReport("A", "LootTeleporter.ResolvePointerGroundTarget", "屏幕中心射线命中放置点", "player=" + playerTransform.position + ",screenCenter=" + screenCenter + ",result=" + target);
			#endregion
			return target;
		}

		Vector3 fallback = playerTransform.position + playerTransform.forward * 2.2f;
		Vector3 vector2 = ResolveGroundPoint(fallback, null, 0.01f);
		#region debug-point A:pointer-target-fallback
		LocalPlayerManager.DebugReport("A", "LootTeleporter.ResolvePointerGroundTarget", "未命中射线，使用回退地面点", "player=" + playerTransform.position + ",fallback=" + fallback + ",result=" + vector2);
		#endregion
		return vector2;
	}

	private static bool TryRaycastPlacement(Ray ray, float preferredHeight, out Vector3 point)
	{
		point = Vector3.zero;
		RaycastHit[] array = Physics.RaycastAll(ray, 60f, -1, QueryTriggerInteraction.Ignore);
		if (array == null || array.Length == 0)
		{
			return false;
		}

		Array.Sort(array, (left, right) => left.distance.CompareTo(right.distance));
		bool flag = false;
		float num = float.MaxValue;
		RaycastHit raycastHit = default(RaycastHit);
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit raycastHit2 = array[i];
			if (raycastHit2.normal.y < 0.25f)
			{
				continue;
			}

			if (IsDynamicPlacementBlocker(raycastHit2.collider))
			{
				continue;
			}

			float num2 = Mathf.Abs(raycastHit2.point.y - preferredHeight);
			if (raycastHit2.point.y > preferredHeight)
			{
				num2 += 1f;
			}

			if (!flag || num2 < num)
			{
				num = num2;
				raycastHit = raycastHit2;
				flag = true;
			}
		}

		if (flag)
		{
			point = raycastHit.point;
			#region debug-point A:try-raycast-placement-height-aware-hit
			LocalPlayerManager.DebugReport("A", "LootTeleporter.TryRaycastPlacement", "按玩家高度选中放置命中点", "preferredHeight=" + preferredHeight + ",hitCollider=" + (((Object)(object)raycastHit.collider != (Object)null) ? ((Object)(object)raycastHit.collider).name : "null") + ",hitPoint=" + raycastHit.point + ",score=" + num.ToString("F3"));
			#endregion
			return true;
		}

		return false;
	}

	private static bool IsDynamicPlacementBlocker(Collider collider)
	{
		if ((Object)(object)collider == (Object)null)
		{
			return true;
		}

		Transform transform = collider.transform;
		return (Object)(object)transform.GetComponentInParent<PhysGrabObject>() != (Object)null || (Object)(object)transform.GetComponentInParent<PlayerAvatar>() != (Object)null || (Object)(object)transform.GetComponentInParent<Enemy>() != (Object)null;
	}

	private static Vector3 ResolvePlacementPoint(Vector3 center, Transform playerTransform, int index, PhysGrabObject item)
	{
		int row = index / 4;
		int column = index % 4;
		Vector3 forward = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up).normalized;
		Vector3 right = Vector3.ProjectOnPlane(playerTransform.right, Vector3.up).normalized;
		if (forward.sqrMagnitude <= 0.001f)
		{
			forward = Vector3.forward;
		}
		if (right.sqrMagnitude <= 0.001f)
		{
			right = Vector3.right;
		}

		float horizontalOffset = ((float)column - 1.5f) * 1.05f;
		float forwardOffset = 0.9f + (float)row * 1.05f;
		Vector3 roughPoint = center + right * horizontalOffset + forward * forwardOffset;
		Vector3 target;
		if (TryRaycastPlacement(new Ray(roughPoint, Vector3.down), roughPoint.y, out target))
		{
			return ResolveGroundPoint(target, item, 0.01f);
		}
		else
		{
			return ResolveGroundPoint(roughPoint, item, 0.01f);
		}
	}

	private static Vector3 ResolveGroundPoint(Vector3 roughPoint, PhysGrabObject item, float margin)
	{
		float verticalOffset = Mathf.Max(GetItemVerticalOffset(item), margin);
		Vector3 groundedPoint = LocalPlayerManager.ResolveGroundedPoint(roughPoint, verticalOffset);
		Vector3 vector = EnsureStaticClearance(groundedPoint, item, verticalOffset);
		#region debug-point A:resolve-item-ground-point
		LocalPlayerManager.DebugReport("A", "LootTeleporter.ResolveGroundPoint", "计算物品落点", "item=" + (((Object)(object)item != (Object)null) ? ((Object)(object)item).name : "null") + ",rough=" + roughPoint + ",verticalOffset=" + verticalOffset + ",grounded=" + groundedPoint + ",result=" + vector);
		#endregion
		return vector;
	}

	private static float GetItemVerticalOffset(PhysGrabObject item)
	{
		if (!TryGetPlacementBounds(item, out _, out Vector3 extents))
		{
			return 0.08f;
		}

		return Mathf.Max(extents.y + 0.05f, 0.08f);
	}

	private static Vector3 EnsureStaticClearance(Vector3 groundedPoint, PhysGrabObject item, float minLift)
	{
		if (!TryGetPlacementBounds(item, out Vector3 centerOffset, out Vector3 halfExtents))
		{
			return groundedPoint;
		}

		Vector3 result = groundedPoint;
		float liftStep = Mathf.Max(halfExtents.y * 0.7f, 0.12f, minLift * 0.35f);
		for (int i = 0; i < 8; i++)
		{
			Vector3 overlapCenter = result + centerOffset;
			if (!HasStaticIntersection(overlapCenter, halfExtents, item))
			{
				return result;
			}

			result += Vector3.up * liftStep;
		}

		return result;
	}

	private static bool TryGetPlacementBounds(PhysGrabObject item, out Vector3 centerOffset, out Vector3 halfExtents)
	{
		centerOffset = Vector3.zero;
		halfExtents = Vector3.one * 0.15f;
		if ((Object)(object)item == (Object)null)
		{
			return false;
		}

		Bounds bounds = default(Bounds);
		bool hasBounds = false;
		Collider[] componentsInChildren = ((Component)item).GetComponentsInChildren<Collider>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Collider collider = componentsInChildren[i];
			if ((Object)(object)collider == (Object)null || !collider.enabled || collider.isTrigger)
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
				bounds.Encapsulate(collider.bounds);
			}
		}

		if (!hasBounds)
		{
			Renderer[] componentsInChildren2 = ((Component)item).GetComponentsInChildren<Renderer>(true);
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				Renderer renderer = componentsInChildren2[j];
				if ((Object)(object)renderer == (Object)null || !renderer.enabled)
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
					bounds.Encapsulate(renderer.bounds);
				}
			}
		}

		if (!hasBounds)
		{
			return false;
		}

		Vector3 position = ((Component)item).transform.position;
		centerOffset = bounds.center - position;
		halfExtents = new Vector3(Mathf.Max(bounds.extents.x + 0.02f, 0.08f), Mathf.Max(bounds.extents.y + 0.02f, 0.08f), Mathf.Max(bounds.extents.z + 0.02f, 0.08f));
		return true;
	}

	private static bool HasStaticIntersection(Vector3 center, Vector3 halfExtents, PhysGrabObject item)
	{
		Collider[] array = Physics.OverlapBox(center, halfExtents, ((Component)item).transform.rotation, -1, QueryTriggerInteraction.Ignore);
		if (array == null || array.Length == 0)
		{
			return false;
		}

		for (int i = 0; i < array.Length; i++)
		{
			Collider collider = array[i];
			if ((Object)(object)collider == (Object)null || collider.isTrigger)
			{
				continue;
			}

			if (collider.transform.IsChildOf(((Component)item).transform))
			{
				continue;
			}

			if (IsDynamicPlacementBlocker(collider))
			{
				continue;
			}

			return true;
		}

		return false;
	}

	private static bool TeleportObject(PhysGrabObject physGrabObject, Vector3 targetPosition, float protectionSeconds)
	{
		Quaternion rotation = ((Component)physGrabObject).transform.rotation;
		#region debug-point C:teleport-object-before
		LocalPlayerManager.DebugReport("C", "LootTeleporter.TeleportObject", "执行物体传送前", "item=" + ((Object)(object)physGrabObject).name + ",current=" + ((Component)physGrabObject).transform.position + ",target=" + targetPosition + ",protection=" + protectionSeconds);
		#endregion

		if (!NativeTeleportModule.TryMovePhysGrabObjectLocally(physGrabObject, targetPosition, rotation))
		{
			LastStatus = NativeTeleportModule.LastStatus;
			return false;
		}

		// 通过调用无敌无人机相同的保护接口来保证主机计算时的无敌
		physGrabObject.OverrideIndestructible(protectionSeconds);
		physGrabObject.OverrideBreakEffects(protectionSeconds);

		#region debug-point C:teleport-object-after
		LocalPlayerManager.DebugReport("C", "LootTeleporter.TeleportObject", "执行物体传送后", "item=" + ((Object)(object)physGrabObject).name + ",transform=" + ((Component)physGrabObject).transform.position + ",rb=" + (((Object)(object)physGrabObject.rb != (Object)null) ? physGrabObject.rb.position.ToString() : "null") + ",targetPosField=" + ReflectionUtils.GetFieldValue<Vector3>(physGrabObject, "targetPos") + ",currentPosField=" + ReflectionUtils.GetFieldValue<Vector3>(physGrabObject, "currentPosition"));
		#endregion
		return true;
	}
}
