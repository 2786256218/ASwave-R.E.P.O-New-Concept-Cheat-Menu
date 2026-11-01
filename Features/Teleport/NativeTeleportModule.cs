using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using Cheat.Config;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

namespace Cheat.Features.Teleport;

public enum TeleportAction
{
	Doors,
	Valuables,
	EnemyToPlayer,
	PlayerToEnemy
}

/// <summary>Client-local movement helpers for the mod's teleport actions.</summary>
public static class NativeTeleportModule
{
	private static readonly Dictionary<TeleportAction, float> NextAllowedTime = new Dictionary<TeleportAction, float>();
	private const float PhysicsFreezeSeconds = 0.12f;

	public static string LastStatus { get; private set; } = string.Empty;
	public static ConfigData.TeleportSettings Settings => ConfigManager.Config.Teleport ?? (ConfigManager.Config.Teleport = new ConfigData.TeleportSettings());

	public static bool TryBeginAction(TeleportAction action)
	{
		float now = Time.unscaledTime;
		if (NextAllowedTime.TryGetValue(action, out float allowedAt) && now < allowedAt)
		{
			LastStatus = "操作太快了，请稍后再试。";
			return false;
		}
		NextAllowedTime[action] = now + Mathf.Clamp(Settings.CooldownSeconds, 0f, 30f);
		return true;
	}

	public static bool IsWithinDistance(Vector3 origin, Vector3 target)
	{
		float maximum = Mathf.Max(1f, Settings.MaxDistance);
		return (target - origin).sqrMagnitude <= maximum * maximum;
	}

	public static bool IsOwnedByAnotherClient(UnityEngine.Object target)
	{
		if (!GameManager.Multiplayer() || target == null)
			return false;
		Component component = target as Component;
		if (component == null && target is GameObject gameObject)
			component = gameObject.transform;
		PhotonView view = component != null ? FindPhotonView(component) : null;
		return view != null && !view.IsMine;
	}

	/// <summary>
	/// A PhotonView owner is always locally controllable. On non-owner clients, only
	/// supported local transform/physics targets are accepted; no ownership request
	/// or network RPC is issued by this module.
	/// </summary>
	public static bool IsLocalControllable(UnityEngine.Object target)
	{
		if (target == null)
			return false;
		Component component = target as Component;
		if (component == null && target is GameObject gameObject)
			component = gameObject.transform;
		if (component == null || !component.gameObject.activeInHierarchy)
			return false;

		PhotonView view = FindPhotonView(component);
		if (!GameManager.Multiplayer() || view == null || view.IsMine)
			return component.transform != null;

		if (component is PhysGrabObject)
			return FindCompatibleMethod(component.GetType(), "SetPositionLogic", new object[] { Vector3.zero, Quaternion.identity }) != null
				|| component.GetComponent<Rigidbody>() != null;
		if (component is Enemy)
			return component.transform != null;
		if (component is PlayerAvatar)
			return ReflectionUtils.GetFieldValue<bool>(component, "isLocal");
		return false;
	}

	/// <summary>Invokes an existing game method safely, with semantic name fallback.</summary>
	public static bool TryInvokeNative(Type type, string methodName, object target, params object[] args)
	{
		if (type == null)
		{
			LastStatus = "目标暂不可用，无法完成操作。";
			return false;
		}

		try
		{
			MethodInfo method = FindCompatibleMethod(type, methodName, args ?? Array.Empty<object>());
			if (method == null || (!method.IsStatic && target == null))
			{
				LastStatus = "当前游戏版本不支持此操作。";
				return false;
			}
			method.Invoke(target, args ?? Array.Empty<object>());
			return true;
		}
		catch (TargetInvocationException)
		{
			LastStatus = "操作未能完成，请稍后重试。";
			return false;
		}
		catch (Exception)
		{
			LastStatus = "操作未能完成，请稍后重试。";
			return false;
		}
	}

	public static bool TryMovePhysGrabObjectLocally(PhysGrabObject item, Vector3 position, Quaternion rotation)
	{
		if (item == null || !IsFinite(position) || !IsFinite(rotation) || !IsLocalControllable(item))
		{
			LastStatus = "物品暂不可用，无法移动。";
			return false;
		}

		Transform itemTransform = item.transform;
		Rigidbody body = item.rb != null ? item.rb : item.GetComponent<Rigidbody>();
		bool wasKinematic = body != null && body.isKinematic;
		if (body != null)
			FreezeBody(body);

		// The game's public Teleport method is the native path on the host and in
		// single-player. For a non-host it intentionally does nothing for ordinary
		// props; use SetPositionLogic only for the local visual/prediction update.
		bool isAuthority = SemiFunc.IsMasterClientOrSingleplayer();
		bool nativeMoved = isAuthority
			? TryInvokeNative(typeof(PhysGrabObject), "Teleport", item, position, rotation)
			: TryInvokeNative(typeof(PhysGrabObject), "SetPositionLogic", item, position, rotation);
		if (!nativeMoved && isAuthority)
		{
			nativeMoved = TryInvokeNative(typeof(PhysGrabObject), "SetPositionLogic", item, position, rotation);
		}
		itemTransform.SetPositionAndRotation(position, rotation);
		if (body != null)
		{
			body.position = position;
			body.rotation = rotation;
			body.velocity = Vector3.zero;
			body.angularVelocity = Vector3.zero;
		}
		SetVectorFields(item, position, new string[] { "target", "position" }, new string[] { "current", "position" });
		Physics.SyncTransforms();
		if (body != null)
			item.StartCoroutine(ResumeBody(body, wasKinematic, Settings.KeepPhysics));

		PhotonView itemView = FindPhotonView(item);
		bool remoteOwned = GameManager.Multiplayer() && itemView != null && !itemView.IsMine;
		LastStatus = remoteOwned
			? "物品已移动，但房主可能会将它恢复到原位置。"
			: isAuthority && nativeMoved
				? "物品已移动。"
				: nativeMoved
					? "物品已移动。"
					: "物品已在当前画面中移动。";
		return true;
	}

	public static bool TryTeleportEnemyLocally(Enemy enemy, Vector3 position)
	{
		bool isAuthority = SemiFunc.IsMasterClientOrSingleplayer();
		if (enemy == null || !IsFinite(position) || (!isAuthority && !IsLocalControllable(enemy)))
		{
			LastStatus = "怪物暂不可用，无法移动。";
			return false;
		}

		if (isAuthority && GameManager.Multiplayer())
		{
			if (FindPhotonView(enemy) == null)
			{
				LastStatus = "未找到可移动的怪物。";
				return false;
			}
			try
			{
				// This is the game's supported authoritative relocation path. It
				// updates the NavMesh agent, physics state, and remote clients.
				enemy.EnemyTeleported(position);
				Rigidbody authorityBody = enemy.GetComponent<Rigidbody>();
				if (authorityBody == null)
					authorityBody = enemy.GetComponentInChildren<Rigidbody>(true);
				if (authorityBody != null)
				{
					authorityBody.velocity = Vector3.zero;
					authorityBody.angularVelocity = Vector3.zero;
				}
				Physics.SyncTransforms();
				LastStatus = "怪物已移动到目标位置。";
				return true;
			}
			catch (Exception)
			{
				LastStatus = "怪物移动失败，请稍后重试。";
				return false;
			}
		}

		Transform root = enemy.transform;
		Quaternion rotation = root.rotation;
		Rigidbody body = enemy.GetComponent<Rigidbody>();
		if (body == null)
			body = enemy.GetComponentInChildren<Rigidbody>(true);
		bool wasKinematic = body != null && body.isKinematic;
		if (body != null)
			FreezeBody(body);

		root.SetPositionAndRotation(position, rotation);
		NavMeshAgent agent = ResolveEnemyAgent(enemy);
		if (agent != null && agent.isActiveAndEnabled)
			agent.Warp(position);
		if (body != null)
		{
			body.position = position;
			body.rotation = rotation;
			body.velocity = Vector3.zero;
			body.angularVelocity = Vector3.zero;
		}

		// Keep the client's AI interpolation target aligned without invoking
		// Enemy.EnemyTeleported, which broadcasts an RPC and is master-validated.
		SetVectorFields(enemy, position, new string[] { "position", "target" }, new string[] { "teleport", "position" });
		SetFloatFields(enemy, 0f, new string[] { "position", "distance" });
		SetFloatFields(enemy, 1f, new string[] { "teleported", "timer" });

		Physics.SyncTransforms();
		if (body != null)
			enemy.StartCoroutine(ResumeBody(body, wasKinematic, Settings.KeepPhysics));
		PhotonView enemyView = FindPhotonView(enemy);
		LastStatus = GameManager.Multiplayer() && enemyView != null && !enemyView.IsMine
			? "怪物位置已改变，房主可能会将它恢复到原位置。"
			: "怪物已移动到目标位置。";
		return true;
	}

	public static bool TryTeleportLocalPlayer(PlayerAvatar avatar, Vector3 position, Quaternion rotation)
	{
		if (avatar == null || !IsFinite(position) || !IsFinite(rotation) || !IsLocalControllable(avatar))
		{
			LastStatus = "当前玩家暂不可用，无法传送。";
			return false;
		}

		Transform avatarTransform = avatar.transform;
		avatarTransform.SetPositionAndRotation(position, rotation);
		SetVectorFields(avatar, position, new string[] { "client", "position" }, new string[] { "client", "position", "current" }, new string[] { "navmesh", "position" });
		SetQuaternionFields(avatar, rotation, new string[] { "client", "rotation" }, new string[] { "client", "rotation", "current" });
		Rigidbody avatarBody = avatar.GetComponent<Rigidbody>();
		if (avatarBody != null)
		{
			bool wasKinematic = avatarBody.isKinematic;
			FreezeBody(avatarBody);
			avatarBody.position = position;
			avatarBody.rotation = rotation;
			avatarBody.velocity = Vector3.zero;
			avatarBody.angularVelocity = Vector3.zero;
			avatar.StartCoroutine(ResumeBody(avatarBody, wasKinematic, Settings.KeepPhysics));
		}

		PlayerController controller = PlayerController.instance;
		if (controller != null && controller.playerAvatarScript == avatar)
		{
			controller.transform.SetPositionAndRotation(position, rotation);
			controller.Velocity = Vector3.zero;
			controller.VelocityRelative = Vector3.zero;
			if (controller.rb != null)
			{
				controller.rb.position = position;
				controller.rb.rotation = rotation;
				controller.rb.velocity = Vector3.zero;
				controller.rb.angularVelocity = Vector3.zero;
			}
		}

		PlayerTumble tumble = avatar.GetComponent<PlayerTumble>();
		PhysGrabObject playerBody = tumble != null ? tumble.GetComponent<PhysGrabObject>() : null;
		bool nativeOwnerTeleportRequested = false;
		if (playerBody != null && GameManager.Multiplayer() && !SemiFunc.IsMasterClientOrSingleplayer()
			&& ReflectionUtils.GetFieldValue<bool>(avatar, "isLocal")
			&& avatar.photonView != null && avatar.photonView.IsMine)
		{
			// PhysGrabObject.Teleport has a special player-body branch: the owner
			// sends SetPositionRPC to the master, whose receiver accepts the avatar
			// owner. This is the game's existing client-authorized teleport path.
			nativeOwnerTeleportRequested = TryInvokeNative(typeof(PhysGrabObject), "Teleport", playerBody, position, rotation);
		}
		if (playerBody != null)
			TryMovePhysGrabObjectLocally(playerBody, position, rotation);

		Physics.SyncTransforms();
		LastStatus = nativeOwnerTeleportRequested
			? "已传送到目标位置。"
			: "位置已改变，房主可能会将你恢复到原位置。";
		return true;
	}

	private static NavMeshAgent ResolveEnemyAgent(Enemy enemy)
	{
		Component agentController = FindComponentByTypeName(enemy.transform, "EnemyNavMeshAgent");
		if (agentController == null)
			return enemy.GetComponent<NavMeshAgent>();
		object value = GetFieldBySemantic(agentController, typeof(NavMeshAgent), "agent");
		return value as NavMeshAgent;
	}

	private static PhotonView FindPhotonView(Component component)
	{
		PhotonView view = component.GetComponent<PhotonView>();
		if (view == null) view = component.GetComponentInParent<PhotonView>();
		if (view == null) view = component.GetComponentInChildren<PhotonView>(true);
		return view;
	}

	private static MethodInfo FindCompatibleMethod(Type type, string requestedName, object[] args)
	{
		MethodInfo semanticMatch = null;
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		for (int i = 0; i < methods.Length; i++)
		{
			MethodInfo method = methods[i];
			if (!ArgumentsMatch(method.GetParameters(), args))
				continue;
			if (string.Equals(method.Name, requestedName, StringComparison.Ordinal))
				return method;
			if (semanticMatch == null && IsSemanticMovementMethod(method.Name, requestedName))
				semanticMatch = method;
		}
		return semanticMatch;
	}

	private static bool ArgumentsMatch(ParameterInfo[] parameters, object[] args)
	{
		if (parameters.Length != args.Length)
			return false;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (args[i] == null)
			{
				if (parameters[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(parameters[i].ParameterType) == null)
					return false;
			}
			else if (!parameters[i].ParameterType.IsInstanceOfType(args[i]))
				return false;
		}
		return true;
	}

	private static bool IsSemanticMovementMethod(string actualName, string requestedName)
	{
		string actual = actualName.ToLowerInvariant();
		string requested = (requestedName ?? string.Empty).ToLowerInvariant();
		if (requested.Contains("setposition") || requested.Contains("positionlogic"))
			return actual.Contains("setposition") || actual.Contains("positionlogic") || actual.Contains("moveto");
		if (requested.Contains("teleport"))
			return actual.Contains("teleport");
		return requested.Contains("grab") && actual.Contains("grab");
	}

	private static Component FindComponentByTypeName(Transform root, string semanticName)
	{
		Component[] components = root.GetComponentsInChildren<Component>(true);
		for (int i = 0; i < components.Length; i++)
			if (components[i] != null && components[i].GetType().Name.IndexOf(semanticName, StringComparison.OrdinalIgnoreCase) >= 0)
				return components[i];
		return null;
	}

	private static object GetFieldBySemantic(object target, Type fieldType, params string[] tokens)
	{
		FieldInfo field = FindField(target?.GetType(), fieldType, tokens);
		try { return field?.GetValue(target); }
		catch { return null; }
	}

	private static void SetVectorFields(object target, Vector3 value, params string[][] semanticGroups)
	{
		for (int i = 0; i < semanticGroups.Length; i++)
			SetFieldBySemantic(target, typeof(Vector3), value, semanticGroups[i]);
	}

	private static void SetQuaternionFields(object target, Quaternion value, params string[][] semanticGroups)
	{
		for (int i = 0; i < semanticGroups.Length; i++)
			SetFieldBySemantic(target, typeof(Quaternion), value, semanticGroups[i]);
	}

	private static void SetFloatFields(object target, float value, params string[][] semanticGroups)
	{
		for (int i = 0; i < semanticGroups.Length; i++)
			SetFieldBySemantic(target, typeof(float), value, semanticGroups[i]);
	}

	private static void SetFieldBySemantic(object target, Type fieldType, object value, string[] tokens)
	{
		FieldInfo field = FindField(target?.GetType(), fieldType, tokens);
		if (field == null || field.IsInitOnly || field.IsLiteral)
			return;
		try { field.SetValue(target, value); }
		catch { }
	}

	private static FieldInfo FindField(Type type, Type fieldType, string[] tokens)
	{
		for (Type current = type; current != null; current = current.BaseType)
		{
			FieldInfo[] fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].FieldType != fieldType)
					continue;
				string name = fields[i].Name.ToLowerInvariant();
				bool matches = true;
				for (int t = 0; t < tokens.Length; t++)
					if (!name.Contains(tokens[t].ToLowerInvariant())) { matches = false; break; }
				if (matches) return fields[i];
			}
		}
		return null;
	}

	private static void FreezeBody(Rigidbody body)
	{
		body.velocity = Vector3.zero;
		body.angularVelocity = Vector3.zero;
		body.isKinematic = true;
	}

	private static IEnumerator ResumeBody(Rigidbody body, bool wasKinematic, bool keepPhysics)
	{
		yield return new WaitForSecondsRealtime(PhysicsFreezeSeconds);
		if (body == null)
			yield break;
		body.isKinematic = keepPhysics ? wasKinematic : true;
		body.velocity = Vector3.zero;
		body.angularVelocity = Vector3.zero;
	}

	private static bool IsFinite(Vector3 value)
	{
		return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
	}

	private static bool IsFinite(Quaternion value)
	{
		return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}
}
