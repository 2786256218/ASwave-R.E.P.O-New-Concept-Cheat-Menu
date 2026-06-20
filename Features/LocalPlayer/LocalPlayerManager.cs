using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Cheat.Config;
using Cheat.Features.Loot;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

namespace Cheat.Features.LocalPlayer;

public static class LocalPlayerManager
{
	private const int LootTeleportModeValuables = 0;

	private const int LootTeleportModeDoors = 1;

	private const string DebugEnvPath = ".dbg\\home-teleport-noop.env";

	private const string DebugSessionId = "home-teleport-noop";

	private const string DebugRunId = "pre-fix";

	private const double DebugConfigRefreshIntervalSeconds = 5.0;

	private const float HomeStoredSpawnMatchDistance = 2.5f;

	private const float HomeTruckSpawnSearchRadius = 40f;

	private sealed class LocalPlayerNetHandler : MonoBehaviourPun
	{
		public bool RequestSpawn(Vector3 position, Quaternion rotation)
		{
			if ((UnityEngine.Object)(object)photonView == (UnityEngine.Object)null || !photonView.IsMine)
			{
				return false;
			}

			photonView.RPC("RequestSpawnRPC", RpcTarget.MasterClient, position, rotation);
			return true;
		}

		public bool RequestTeleportEnemyToPlayer(int enemyViewId)
		{
			if ((UnityEngine.Object)(object)photonView == (UnityEngine.Object)null || !photonView.IsMine || enemyViewId <= 0)
			{
				return false;
			}

			photonView.RPC("RequestTeleportEnemyToPlayerRPC", RpcTarget.MasterClient, enemyViewId);
			return true;
		}

		public bool RequestLootTeleport(Vector3 targetCenter, int mode)
		{
			if ((UnityEngine.Object)(object)photonView == (UnityEngine.Object)null || !photonView.IsMine)
			{
				return false;
			}

			photonView.RPC("RequestLootTeleportRPC", RpcTarget.MasterClient, targetCenter, mode);
			return true;
		}

		public bool RequestLooseItemProtection()
		{
			if ((UnityEngine.Object)(object)photonView == (UnityEngine.Object)null || !photonView.IsMine)
			{
				return false;
			}

			photonView.RPC("RequestLooseItemProtectionRPC", RpcTarget.MasterClient);
			return true;
		}

		[PunRPC]
		private void RequestSpawnRPC(Vector3 position, Quaternion rotation, PhotonMessageInfo _info = default(PhotonMessageInfo))
		{
			#region debug-point B:request-spawn-rpc
			DebugReport("B", "LocalPlayerManager.RequestSpawnRPC", "收到玩家传送请求", "isMaster=" + PhotonNetwork.IsMasterClient + ",position=" + position + ",rotationY=" + rotation.eulerAngles.y);
			#endregion
			if (!PhotonNetwork.IsMasterClient)
			{
				return;
			}

			PlayerAvatar component = ((Component)this).GetComponent<PlayerAvatar>();
			if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
			{
				Vector3 vector = ResolvePlayerSafeDestination(position);
				#region debug-point A:request-spawn-safe-destination
				DebugReport("A", "LocalPlayerManager.RequestSpawnRPC", "主机执行玩家传送", "requested=" + position + ",safe=" + vector + ",avatar=" + ((UnityEngine.Object)(object)component).name);
				#endregion
				ExecuteAuthoritativePlayerTeleport(component, vector, rotation, "RequestSpawnRPC");
			}
		}

		[PunRPC]
		private void RequestTeleportEnemyToPlayerRPC(int enemyViewId, PhotonMessageInfo _info = default(PhotonMessageInfo))
		{
			#region debug-point B:request-enemy-teleport-rpc
			DebugReport("B", "LocalPlayerManager.RequestTeleportEnemyToPlayerRPC", "收到怪物拉人身边请求", "isMaster=" + PhotonNetwork.IsMasterClient + ",enemyViewId=" + enemyViewId);
			#endregion
			if (!PhotonNetwork.IsMasterClient || enemyViewId <= 0)
			{
				return;
			}

			PlayerAvatar component = ((Component)this).GetComponent<PlayerAvatar>();
			if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
			{
				return;
			}

			PhotonView val = PhotonView.Find(enemyViewId);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				return;
			}

			Enemy component2 = ((Component)val).GetComponent<Enemy>();
			if ((UnityEngine.Object)(object)component2 == (UnityEngine.Object)null)
			{
				component2 = ((Component)val).GetComponentInChildren<Enemy>(true);
			}
			if ((UnityEngine.Object)(object)component2 == (UnityEngine.Object)null)
			{
				component2 = ((Component)val).GetComponentInParent<Enemy>();
			}
			if ((UnityEngine.Object)(object)component2 == (UnityEngine.Object)null)
			{
				return;
			}

			Transform transform = ((Component)component).transform;
			Vector3 destination = ResolveEnemyPullDestination(component, transform);
			#region debug-point A:enemy-teleport-destination
			DebugReport("A", "LocalPlayerManager.RequestTeleportEnemyToPlayerRPC", "主机准备拉怪到玩家身边", "enemy=" + ((UnityEngine.Object)(object)component2).name + ",player=" + transform.position + ",destination=" + destination);
			#endregion
			ExecuteAuthoritativeEnemyTeleport(component2, destination);
		}

		[PunRPC]
		private void RequestLootTeleportRPC(Vector3 targetCenter, int mode, PhotonMessageInfo _info = default(PhotonMessageInfo))
		{
			if (!PhotonNetwork.IsMasterClient)
			{
				return;
			}

			PlayerAvatar component = ((Component)this).GetComponent<PlayerAvatar>();
			if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
			{
				return;
			}

			int executeMode = mode == LootTeleportModeDoors ? LootTeleportModeDoors : LootTeleportModeValuables;
			LootTeleporter.ExecuteTeleport(executeMode, targetCenter, ((Component)component).transform, component);
		}

		[PunRPC]
		private void RequestLooseItemProtectionRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
		{
			if (!PhotonNetwork.IsMasterClient)
			{
				return;
			}

			LootManager.ApplyProtectionToLooseItems();
		}
	}

	private const float MaxStableGrabStrengthMultiplier = 5f;

	private static bool _initialized = false;

	private static float _originalMoveSpeed = -1f;

	private static float _originalSprintSpeed = -1f;

	private static float _originalCrouchSpeed = -1f;

	private static float _originalEnergyMax = -1f;

	private static float _originalGrabRange = -1f;

	private static float _originalGrabReleaseDistance = -1f;

	private static float _originalForceMax = -1f;

	private static float _originalForceConstant = -1f;

	private static float _originalGrabStrength = -1f;

	private static float _originalJumpForce = -1f;

	private static float _originalGravity = -1f;

	private static float _originalFOV = -1f;

	private static FieldInfo _godModeField;

	private static FieldInfo _energyCurrentField;

	private static FieldInfo _energyStartField;

	private static FieldInfo _moveSpeedField;

	private static FieldInfo _sprintSpeedField;

	private static FieldInfo _tumbleField;

	private static FieldInfo _rbField;

	private static FieldInfo _grabRangeField;

	private static FieldInfo _grabReleaseDistanceField;

	private static FieldInfo _forceMaxField;

	private static FieldInfo _forceConstantField;

	private static FieldInfo _grabStrengthField;

	private static FieldInfo _grabbedPhysGrabObjectField;

	private static FieldInfo _physGrabObjectOverrideGrabStrengthField;

	private static FieldInfo _physGrabObjectOverrideGrabStrengthTimerField;

	private static FieldInfo _physGrabObjectOverrideMinGrabStrengthField;

	private static FieldInfo _physGrabObjectOverrideMinGrabStrengthTimerField;

	private static MethodInfo _physGrabberOverrideGrabStrengthMethod;

	private static MethodInfo _physGrabObjectOverrideGrabStrengthMethod;

	private static MethodInfo _physGrabObjectOverrideMinGrabStrengthMethod;

	private static bool _noClipActive = false;

	private static Collider _playerCollider;

	private static MonoBehaviour _playerCollision;

	private static Rigidbody _playerRigidbody;

	private static bool _originalGravityUseGravity;

	private static float _originalGravityDrag;

	private static bool _originalRigidbodyKinematic;

	private static CollisionDetectionMode _originalCollisionDetectionMode;

	private static FieldInfo _debugInfiniteBatteryField;

	private static FieldInfo _playerAvatarSpawnPositionField;

	private static FieldInfo _playerAvatarSpawnRotationField;

	private static FieldInfo _playerAvatarLastNavmeshPositionField;

	private static FieldInfo _playerAvatarLastNavmeshPositionTimerField;

	private static FieldInfo _playerAvatarClientPositionField;

	private static FieldInfo _playerAvatarClientPositionCurrentField;

	private static FieldInfo _playerAvatarClientRotationField;

	private static FieldInfo _playerAvatarClientRotationCurrentField;

	private static FieldInfo _playerTumblePhysGrabObjectField;

	private static float _nextBatteryRefreshTime;

	private static float _nextNetHandlerRefreshTime;

	private static bool _debugConfigLoaded;

	private static bool _debugReportEnabled;

	private static string _debugServerUrl = "http://127.0.0.1:7777/event";

	private static string _debugSessionId = DebugSessionId;

	private static string _debugRunId = DebugRunId;

	private static DateTime _nextDebugConfigRefreshUtc = DateTime.MinValue;

	private static int _pendingHomeTeleportDebugFrames;

	private static Vector3 _pendingHomeTeleportTarget;

	private static Quaternion _pendingHomeTeleportRotation = Quaternion.identity;

	private static string _pendingHomeTeleportSource = string.Empty;

	public static string LastActionStatus { get; private set; } = string.Empty;

	public static PlayerController LocalPlayer => PlayerController.instance;

	public static bool IsRuntimeDebugEnabled()
	{
		RefreshDebugConfigIfNeeded();
		return _debugReportEnabled;
	}

	public static void Update()
	{
		if (!((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null))
		{
			bool flag = IsRuntimeDebugEnabled();
			long num = flag ? Stopwatch.GetTimestamp() : 0L;
			double num2 = 0.0;
			bool flag2 = false;
			if (!_initialized)
			{
				Initialize();
			}
			if (Time.unscaledTime >= _nextNetHandlerRefreshTime)
			{
				flag2 = true;
				long num4 = flag ? Stopwatch.GetTimestamp() : 0L;
				EnsureNetHandlers();
				if (flag)
				{
					num2 = GetElapsedMilliseconds(num4);
				}
				_nextNetHandlerRefreshTime = Time.unscaledTime + 1.5f;
			}
			ApplyGodMode();
			ApplyInfiniteStamina();
			ApplyInfiniteBattery();
			ApplyNoClip();
			ApplyNoRagdoll();
			ApplySpeedModifiers();
			ApplyGrabModifiers();
			ApplyJumpModifiers();
			ApplyGravityModifiers();
			ApplyFOV();
			if (flag)
			{
				double elapsedMilliseconds = GetElapsedMilliseconds(num);
				if (elapsedMilliseconds >= 3.0 || num2 >= 2.0)
				{
					#region debug-point A:local-player-update-cost
					DebugReport("A", "LocalPlayerManager.Update", "本地玩家模块出现可见耗时", "totalMs=" + elapsedMilliseconds.ToString("F2") + ",ranNetHandlers=" + flag2 + ",netMs=" + num2.ToString("F2"));
					#endregion
				}
			}
			if (_pendingHomeTeleportDebugFrames > 0)
			{
				_pendingHomeTeleportDebugFrames--;
				PlayerAvatar localPlayerAvatar = ResolveLocalPlayerAvatar();
				PlayerController instance = PlayerController.instance;
				#region debug-point H4:home-teleport-post-frames
				DebugReport("H4", "LocalPlayerManager.Update", "观察回家传送后的对象状态", "framesLeft=" + _pendingHomeTeleportDebugFrames + ",target=" + _pendingHomeTeleportTarget + ",targetRotationY=" + _pendingHomeTeleportRotation.eulerAngles.y + ",source=" + (_pendingHomeTeleportSource ?? string.Empty) + ",avatar=" + DescribePlayerAvatar(localPlayerAvatar) + ",avatarPos=" + GetTransformPosition(localPlayerAvatar) + ",controllerPos=" + GetTransformPosition(instance) + ",rbPos=" + GetRigidbodyPosition(instance) + ",controllerAvatar=" + DescribePlayerAvatar((instance != null) ? instance.playerAvatarScript : null));
				#endregion
			}
		}
	}

	private static void Initialize()
	{
		try
		{
			_godModeField = typeof(PlayerHealth).GetField("godMode", BindingFlags.Instance | BindingFlags.NonPublic);
			_energyCurrentField = typeof(PlayerController).GetField("EnergyCurrent", BindingFlags.Instance | BindingFlags.Public);
			_energyStartField = typeof(PlayerController).GetField("EnergyStart", BindingFlags.Instance | BindingFlags.Public);
			_moveSpeedField = typeof(PlayerController).GetField("MoveSpeed", BindingFlags.Instance | BindingFlags.Public);
			_sprintSpeedField = typeof(PlayerController).GetField("SprintSpeed", BindingFlags.Instance | BindingFlags.Public);
			_tumbleField = typeof(PlayerAvatar).GetField("tumble", BindingFlags.Instance | BindingFlags.Public);
			_rbField = typeof(PlayerController).GetField("rb", BindingFlags.Instance | BindingFlags.Public);
			_grabRangeField = typeof(PhysGrabber).GetField("grabRange", BindingFlags.Instance | BindingFlags.Public);
			_grabReleaseDistanceField = typeof(PhysGrabber).GetField("grabReleaseDistance", BindingFlags.Instance | BindingFlags.Public);
			_forceMaxField = typeof(PhysGrabber).GetField("forceMax", BindingFlags.Instance | BindingFlags.Public);
			_forceConstantField = typeof(PhysGrabber).GetField("forceConstant", BindingFlags.Instance | BindingFlags.Public);
			_grabStrengthField = typeof(PhysGrabber).GetField("grabStrength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_grabbedPhysGrabObjectField = typeof(PhysGrabber).GetField("grabbedPhysGrabObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_physGrabberOverrideGrabStrengthMethod = typeof(PhysGrabber).GetMethod("OverrideGrabStrength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_physGrabObjectOverrideGrabStrengthMethod = typeof(PhysGrabObject).GetMethod("OverrideGrabStrength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_physGrabObjectOverrideMinGrabStrengthMethod = typeof(PhysGrabObject).GetMethod("OverrideMinGrabStrength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_physGrabObjectOverrideGrabStrengthField = typeof(PhysGrabObject).GetField("overrideGrabStrength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_physGrabObjectOverrideGrabStrengthTimerField = typeof(PhysGrabObject).GetField("overrideGrabStrengthTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_physGrabObjectOverrideMinGrabStrengthField = typeof(PhysGrabObject).GetField("overrideMinGrabStrength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_physGrabObjectOverrideMinGrabStrengthTimerField = typeof(PhysGrabObject).GetField("overrideMinGrabStrengthTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_playerAvatarSpawnPositionField = typeof(PlayerAvatar).GetField("spawnPosition", BindingFlags.Instance | BindingFlags.NonPublic);
			_playerAvatarSpawnRotationField = typeof(PlayerAvatar).GetField("spawnRotation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_playerAvatarLastNavmeshPositionField = typeof(PlayerAvatar).GetField("LastNavmeshPosition", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_playerAvatarLastNavmeshPositionTimerField = typeof(PlayerAvatar).GetField("LastNavMeshPositionTimer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_playerAvatarClientPositionField = typeof(PlayerAvatar).GetField("clientPosition", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_playerAvatarClientPositionCurrentField = typeof(PlayerAvatar).GetField("clientPositionCurrent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_playerAvatarClientRotationField = typeof(PlayerAvatar).GetField("clientRotation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_playerAvatarClientRotationCurrentField = typeof(PlayerAvatar).GetField("clientRotationCurrent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_playerTumblePhysGrabObjectField = typeof(PlayerTumble).GetField("physGrabObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
			{
				_originalMoveSpeed = PlayerController.instance.MoveSpeed;
				_originalSprintSpeed = PlayerController.instance.SprintSpeed;
				_originalCrouchSpeed = PlayerController.instance.CrouchSpeed;
				_originalEnergyMax = PlayerController.instance.EnergyStart;
				_originalJumpForce = PlayerController.instance.JumpForce;
				_originalGravity = PlayerController.instance.CustomGravity;
				_playerCollider = ((Component)PlayerController.instance).GetComponent<Collider>();
				if ((UnityEngine.Object)(object)_playerCollider == (UnityEngine.Object)null)
				{
					_playerCollider = ((Component)PlayerController.instance).GetComponentInChildren<Collider>();
				}
				_playerCollision = (MonoBehaviour)((Component)PlayerController.instance).GetComponent("PlayerCollision");
				if (_playerCollision == null && PlayerController.instance != null)
				{
					// Try to get via reflection if type is accessible, or just use the field PlayerCollision.
					FieldInfo pcField = typeof(PlayerController).GetField("PlayerCollision", BindingFlags.Public | BindingFlags.Instance);
					if (pcField != null)
					{
						_playerCollision = (MonoBehaviour)pcField.GetValue(PlayerController.instance);
					}
				}
				_playerRigidbody = PlayerController.instance.rb;
			}
			if ((UnityEngine.Object)(object)PhysGrabber.instance != (UnityEngine.Object)null)
			{
				_originalGrabRange = PhysGrabber.instance.grabRange;
				_originalGrabReleaseDistance = PhysGrabber.instance.grabReleaseDistance;
				if (_forceMaxField != null)
				{
					_originalForceMax = (float)_forceMaxField.GetValue(PhysGrabber.instance);
				}
				if (_forceConstantField != null)
				{
					_originalForceConstant = (float)_forceConstantField.GetValue(PhysGrabber.instance);
				}
				if (_grabStrengthField != null)
				{
					_originalGrabStrength = (float)_grabStrengthField.GetValue(PhysGrabber.instance);
				}
			}
			_initialized = true;
			Console.WriteLine("[LocalPlayer] Initialized successfully");
		}
		catch (Exception ex)
		{
			Console.WriteLine("[LocalPlayer] Init error: " + ex.Message);
		}
	}

	private static void EnsureNetHandlers()
	{
		PlayerAvatar[] array = UnityEngine.Object.FindObjectsByType<PlayerAvatar>((FindObjectsSortMode)0);
		for (int i = 0; i < array.Length; i++)
		{
			PlayerAvatar val = array[i];
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && (UnityEngine.Object)(object)((Component)val).GetComponent<LocalPlayerNetHandler>() == (UnityEngine.Object)null)
			{
				((Component)val).gameObject.AddComponent<LocalPlayerNetHandler>();
			}
		}
	}

	private static bool ResolveHomeDestination(out Vector3 position, out Quaternion rotation, out string description)
	{
		PlayerAvatar localPlayerAvatar = ResolveLocalPlayerAvatar();
		position = Vector3.zero;
		rotation = Quaternion.identity;
		description = string.Empty;
		Transform truckTransform = ((UnityEngine.Object)(object)TruckSafetySpawnPoint.instance != (UnityEngine.Object)null) ? ((Component)TruckSafetySpawnPoint.instance).transform : null;
		SpawnPoint spawnPoint = ResolveHomeSpawnPoint(localPlayerAvatar, truckTransform);
		if ((UnityEngine.Object)(object)spawnPoint != (UnityEngine.Object)null)
		{
			position = spawnPoint.transform.position;
			rotation = spawnPoint.transform.rotation;
			description = ((UnityEngine.Object)(object)truckTransform != (UnityEngine.Object)null) ? "电车出生点" : "出生点";
			#region debug-point H5:home-destination-spawn-point
			DebugReport("H5", "LocalPlayerManager.ResolveHomeDestination", "解析到回家目标出生点", "description=" + description + ",spawnPoint=" + ((UnityEngine.Object)(object)spawnPoint).name + ",position=" + position + ",rotationY=" + rotation.eulerAngles.y + ",truck=" + (((UnityEngine.Object)(object)truckTransform != (UnityEngine.Object)null) ? truckTransform.position.ToString() : "null") + ",avatar=" + DescribePlayerAvatar(localPlayerAvatar));
			#endregion
			return true;
		}

		if ((UnityEngine.Object)(object)truckTransform != (UnityEngine.Object)null && IsFinite(truckTransform.position))
		{
			position = truckTransform.position;
			rotation = truckTransform.rotation;
			description = "电车安全点";
			#region debug-point H5:home-destination-truck
			DebugReport("H5", "LocalPlayerManager.ResolveHomeDestination", "回退到电车安全点", "description=" + description + ",truckPosition=" + position + ",rotationY=" + rotation.eulerAngles.y + ",avatar=" + DescribePlayerAvatar(localPlayerAvatar));
			#endregion
			return true;
		}

		#region debug-point H5:home-destination-miss
		DebugReport("H5", "LocalPlayerManager.ResolveHomeDestination", "未解析到可用回家目标", "avatar=" + DescribePlayerAvatar(localPlayerAvatar) + ",truck=" + (((UnityEngine.Object)(object)truckTransform != (UnityEngine.Object)null) ? truckTransform.position.ToString() : "null"));
		#endregion
		return false;
	}

	private static SpawnPoint ResolveHomeSpawnPoint(PlayerAvatar localPlayerAvatar, Transform truckTransform)
	{
		List<SpawnPoint> homeSpawnPointCandidates = GetHomeSpawnPointCandidates();
		if (homeSpawnPointCandidates.Count == 0)
		{
			return null;
		}

		bool flag = TryGetStoredPlayerSpawnPosition(localPlayerAvatar, out var position);
		float num = HomeStoredSpawnMatchDistance * HomeStoredSpawnMatchDistance;
		float num2 = HomeTruckSpawnSearchRadius * HomeTruckSpawnSearchRadius;
		if (flag)
		{
			SpawnPoint spawnPoint = FindClosestSpawnPoint(homeSpawnPointCandidates, position, out var sqrDistance);
			if ((UnityEngine.Object)(object)spawnPoint != (UnityEngine.Object)null && sqrDistance <= num && ((UnityEngine.Object)(object)truckTransform == (UnityEngine.Object)null || (((Component)spawnPoint).transform.position - truckTransform.position).sqrMagnitude <= num2))
			{
				return spawnPoint;
			}
		}

		if ((UnityEngine.Object)(object)truckTransform != (UnityEngine.Object)null)
		{
			SpawnPoint spawnPoint2 = FindClosestSpawnPoint(homeSpawnPointCandidates, truckTransform.position, out _);
			if ((UnityEngine.Object)(object)spawnPoint2 != (UnityEngine.Object)null)
			{
				return spawnPoint2;
			}
		}

		if (flag)
		{
			return FindClosestSpawnPoint(homeSpawnPointCandidates, position, out _);
		}

		return null;
	}

	private static List<SpawnPoint> GetHomeSpawnPointCandidates()
	{
		SpawnPoint[] array = UnityEngine.Object.FindObjectsByType<SpawnPoint>((FindObjectsSortMode)0);
		List<SpawnPoint> list = new List<SpawnPoint>(array.Length);
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			SpawnPoint spawnPoint = array[i];
			if ((UnityEngine.Object)(object)spawnPoint == (UnityEngine.Object)null || !((Component)spawnPoint).gameObject.scene.IsValid())
			{
				continue;
			}

			list.Add(spawnPoint);
			if (spawnPoint.debug)
			{
				flag = true;
			}
		}

		if (!flag)
		{
			return list;
		}

		List<SpawnPoint> list2 = new List<SpawnPoint>(list.Count);
		for (int j = 0; j < list.Count; j++)
		{
			SpawnPoint spawnPoint2 = list[j];
			if ((UnityEngine.Object)(object)spawnPoint2 != (UnityEngine.Object)null && spawnPoint2.debug)
			{
				list2.Add(spawnPoint2);
			}
		}

		return (list2.Count > 0) ? list2 : list;
	}

	private static SpawnPoint FindClosestSpawnPoint(List<SpawnPoint> spawnPoints, Vector3 anchor, out float sqrDistance)
	{
		sqrDistance = float.MaxValue;
		if (!IsFinite(anchor))
		{
			return null;
		}

		SpawnPoint result = null;
		for (int i = 0; i < spawnPoints.Count; i++)
		{
			SpawnPoint spawnPoint = spawnPoints[i];
			if ((UnityEngine.Object)(object)spawnPoint == (UnityEngine.Object)null)
			{
				continue;
			}

			float sqrMagnitude = (((Component)spawnPoint).transform.position - anchor).sqrMagnitude;
			if (sqrMagnitude < sqrDistance)
			{
				sqrDistance = sqrMagnitude;
				result = spawnPoint;
			}
		}

		return result;
	}

	private static bool TryGetStoredPlayerSpawnPosition(PlayerAvatar localPlayerAvatar, out Vector3 position)
	{
		position = Vector3.zero;
		if ((UnityEngine.Object)(object)localPlayerAvatar == (UnityEngine.Object)null || _playerAvatarSpawnPositionField == null)
		{
			return false;
		}

		try
		{
			position = (Vector3)_playerAvatarSpawnPositionField.GetValue(localPlayerAvatar);
			return IsFinite(position);
		}
		catch
		{
			return false;
		}
	}

	private static void ApplyGodMode()
	{
		if ((UnityEngine.Object)(object)PlayerAvatar.instance == (UnityEngine.Object)null)
		{
			return;
		}
		PlayerHealth playerHealth = PlayerAvatar.instance.playerHealth;
		if (!((UnityEngine.Object)(object)playerHealth == (UnityEngine.Object)null) && !(_godModeField == null))
		{
			bool flag = (bool)_godModeField.GetValue(playerHealth);
			if (flag != ConfigManager.Config.Local.GodMode)
			{
				_godModeField.SetValue(playerHealth, ConfigManager.Config.Local.GodMode);
			}
		}
	}

	private static void ApplyInfiniteStamina()
	{
		if (!((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null) && !(_energyCurrentField == null) && ConfigManager.Config.Local.InfiniteStamina)
		{
			float energyStart = PlayerController.instance.EnergyStart;
			_energyCurrentField.SetValue(PlayerController.instance, energyStart);
		}
	}

	private static void ApplyNoClip()
	{
		if ((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null)
		{
			return;
		}
		bool noClip = ConfigManager.Config.Local.NoClip;
		if (noClip && !_noClipActive)
		{
			if ((UnityEngine.Object)(object)_playerCollider != (UnityEngine.Object)null)
			{
				_playerCollider.enabled = false;
			}
			if ((UnityEngine.Object)(object)_playerCollision != (UnityEngine.Object)null)
			{
				((Behaviour)_playerCollision).enabled = false;
			}
			PlayerController.instance.DebugNoTumble = true;
			if ((UnityEngine.Object)(object)_playerRigidbody != (UnityEngine.Object)null)
			{
				_originalGravityUseGravity = _playerRigidbody.useGravity;
				_originalGravityDrag = _playerRigidbody.drag;
				_originalRigidbodyKinematic = _playerRigidbody.isKinematic;
				_originalCollisionDetectionMode = _playerRigidbody.collisionDetectionMode;
				_playerRigidbody.useGravity = false;
				_playerRigidbody.drag = 0f;
				_playerRigidbody.isKinematic = true;
				_playerRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
				_playerRigidbody.velocity = Vector3.zero;
				_playerRigidbody.angularVelocity = Vector3.zero;
			}
			_noClipActive = true;
			Console.WriteLine("[LocalPlayer] NoClip ENABLED - Others will see you desync!");
		}
		else if (!noClip && _noClipActive)
		{
			if ((UnityEngine.Object)(object)_playerCollider != (UnityEngine.Object)null)
			{
				_playerCollider.enabled = true;
			}
			if ((UnityEngine.Object)(object)_playerCollision != (UnityEngine.Object)null)
			{
				((Behaviour)_playerCollision).enabled = true;
			}
			PlayerController.instance.DebugNoTumble = false;
			PlayerController.instance.CustomGravity = _originalGravity;
			if ((UnityEngine.Object)(object)_playerRigidbody != (UnityEngine.Object)null)
			{
				_playerRigidbody.useGravity = _originalGravityUseGravity;
				_playerRigidbody.drag = _originalGravityDrag;
				_playerRigidbody.isKinematic = _originalRigidbodyKinematic;
				_playerRigidbody.collisionDetectionMode = _originalCollisionDetectionMode;
				_playerRigidbody.velocity = Vector3.zero;
				_playerRigidbody.angularVelocity = Vector3.zero;
			}
			_noClipActive = false;
			Console.WriteLine("[LocalPlayer] NoClip DISABLED");
		}
		if (!_noClipActive)
		{
			return;
		}

		PlayerController.instance.CustomGravity = 0f;
		PlayerController.instance.AntiGravity(0.25f);
		PlayerController.instance.DebugNoTumble = true;
		PlayerController.instance.Velocity = Vector3.zero;
		if ((UnityEngine.Object)(object)PlayerController.instance.CollisionController != (UnityEngine.Object)null)
		{
			PlayerController.instance.CollisionController.GroundedDisableTimer = 0.25f;
			PlayerController.instance.CollisionController.OverrideDisableFallLoop(0.25f);
			PlayerController.instance.CollisionController.ResetFalling();
		}
		PlayerTumble playerTumble = GetPlayerTumble();
		if ((UnityEngine.Object)(object)playerTumble != (UnityEngine.Object)null && ReflectionUtils.GetFieldValue<bool>(playerTumble, "isTumbling"))
		{
			playerTumble.TumbleSet(_isTumbling: false, _playerInput: false);
			playerTumble.DisableCustomGravity(0.25f);
			Rigidbody fieldValue = ReflectionUtils.GetFieldValue<Rigidbody>(playerTumble, "rb");
			if ((UnityEngine.Object)(object)fieldValue != (UnityEngine.Object)null)
			{
				fieldValue.velocity = Vector3.zero;
				fieldValue.angularVelocity = Vector3.zero;
			}
		}
		if ((UnityEngine.Object)(object)_playerRigidbody != (UnityEngine.Object)null)
		{
			_playerRigidbody.velocity = Vector3.zero;
			_playerRigidbody.angularVelocity = Vector3.zero;
		}
		float num = ConfigManager.Config.Local.NoClipSpeed;
		if (Input.GetKey((KeyCode)304))
		{
			num *= Mathf.Max(1f, ConfigManager.Config.Local.NoClipFastMultiplier);
		}
		Vector3 val = Vector3.zero;
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)main != (UnityEngine.Object)null)
		{
			Vector3 normalized = Vector3.ProjectOnPlane(((Component)main).transform.forward, Vector3.up).normalized;
			Vector3 normalized2 = Vector3.ProjectOnPlane(((Component)main).transform.right, Vector3.up).normalized;
			if (Input.GetKey((KeyCode)119))
			{
				val += normalized;
			}
			if (Input.GetKey((KeyCode)115))
			{
				val -= normalized;
			}
			if (Input.GetKey((KeyCode)97))
			{
				val -= normalized2;
			}
			if (Input.GetKey((KeyCode)100))
			{
				val += normalized2;
			}
			if (Input.GetKey((KeyCode)32))
			{
				val += Vector3.up;
			}
			if (Input.GetKey((KeyCode)306))
			{
				val -= Vector3.up;
			}
		}
		if (val != Vector3.zero)
		{
			Vector3 position = ((Component)PlayerController.instance).transform.position + val.normalized * num * Time.unscaledDeltaTime;
			if ((UnityEngine.Object)(object)_playerRigidbody != (UnityEngine.Object)null)
			{
				_playerRigidbody.MovePosition(position);
			}
			else
			{
				((Component)PlayerController.instance).transform.position = position;
			}
		}
		if ((UnityEngine.Object)(object)_playerRigidbody != (UnityEngine.Object)null)
		{
			_playerRigidbody.velocity = Vector3.zero;
			_playerRigidbody.angularVelocity = Vector3.zero;
		}
	}

	private static void ApplyNoRagdoll()
	{
		if ((UnityEngine.Object)(object)PlayerAvatar.instance == (UnityEngine.Object)null || _tumbleField == null)
		{
			return;
		}
		if (!ConfigManager.Config.Local.NoRagdoll)
		{
			if (!_noClipActive && !((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null))
			{
				PlayerController.instance.DebugNoTumble = false;
			}
			return;
		}
		try
		{
			object tumbleObj = _tumbleField.GetValue(PlayerAvatar.instance);
			if (tumbleObj == null)
			{
				return;
			}

			PlayerController.instance.DebugNoTumble = true;
			MethodInfo disableCustomGravityMethod = tumbleObj.GetType().GetMethod("DisableCustomGravity", BindingFlags.Instance | BindingFlags.Public);
			disableCustomGravityMethod?.Invoke(tumbleObj, new object[1] { 0.25f });
			FieldInfo field = tumbleObj.GetType().GetField("isTumbling", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (field != null && (bool)field.GetValue(tumbleObj))
			{
				MethodInfo method = tumbleObj.GetType().GetMethod("TumbleSet", BindingFlags.Instance | BindingFlags.Public);
				method?.Invoke(tumbleObj, new object[2] { false, false });
			}
		}
		catch
		{
		}
	}

	private static PlayerTumble GetPlayerTumble()
	{
		if ((UnityEngine.Object)(object)PlayerAvatar.instance == (UnityEngine.Object)null || _tumbleField == null)
		{
			return null;
		}
		return _tumbleField.GetValue(PlayerAvatar.instance) as PlayerTumble;
	}

	private static void ApplySpeedModifiers()
	{
		if ((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null)
		{
			return;
		}
		if (FreeCam.Enabled || _noClipActive)
		{
			PlayerController.instance.MoveSpeed = 0f;
			PlayerController.instance.SprintSpeed = 0f;
			PlayerController.instance.CrouchSpeed = 0f;
			return;
		}
		if (_originalSprintSpeed > 0f)
		{
			float num = ConfigManager.Config.Local.RunSpeedEnabled ? (_originalSprintSpeed * ConfigManager.Config.Local.RunSpeed) : _originalSprintSpeed;
			if (Math.Abs(PlayerController.instance.SprintSpeed - num) > 0.01f)
			{
				PlayerController.instance.SprintSpeed = num;
			}
		}
		if (_originalMoveSpeed > 0f && Math.Abs(PlayerController.instance.MoveSpeed) < 0.01f)
		{
			PlayerController.instance.MoveSpeed = _originalMoveSpeed;
		}
		if (_originalCrouchSpeed > 0f && Math.Abs(PlayerController.instance.CrouchSpeed) < 0.01f)
		{
			PlayerController.instance.CrouchSpeed = _originalCrouchSpeed;
		}
	}

	private static void ApplyInfiniteBattery()
	{
		if (!((UnityEngine.Object)(object)RoundDirector.instance == (UnityEngine.Object)null))
		{
			if (_debugInfiniteBatteryField == null)
			{
				_debugInfiniteBatteryField = typeof(RoundDirector).GetField("debugInfiniteBattery", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (_debugInfiniteBatteryField != null)
			{
				_debugInfiniteBatteryField.SetValue(RoundDirector.instance, ConfigManager.Config.Local.InfiniteBattery);
			}
		}
		if (!ConfigManager.Config.Local.InfiniteBattery || Time.unscaledTime < _nextBatteryRefreshTime)
		{
			return;
		}

		_nextBatteryRefreshTime = Time.unscaledTime + 0.15f;
		RefillAllVisibleBatteries();
		RefillDeathHeadBattery();
	}

	private static void ApplyGrabModifiers()
	{
		if (!((UnityEngine.Object)(object)PhysGrabber.instance == (UnityEngine.Object)null))
		{
			float num = Mathf.Clamp(ConfigManager.Config.Local.GrabStrength, 1f, MaxStableGrabStrengthMultiplier);
			if (_originalGrabRange > 0f)
			{
				float grabRange = _originalGrabRange * ConfigManager.Config.Local.GrabRange;
				PhysGrabber.instance.grabRange = grabRange;
				PhysGrabber.instance.grabReleaseDistance = _originalGrabReleaseDistance * ConfigManager.Config.Local.GrabRange;
			}
			if (_grabStrengthField != null)
			{
				float num2 = (_originalGrabStrength > 0f) ? (_originalGrabStrength * num) : num;
				_grabStrengthField.SetValue(PhysGrabber.instance, num2);
				TryInvoke(_physGrabberOverrideGrabStrengthMethod, PhysGrabber.instance, num2, 0.25f);
				PhysGrabObject currentlyGrabbedObject = GetCurrentlyGrabbedObject();
				ApplyHeldObjectGrabOverrides(currentlyGrabbedObject, num2);
			}
			if (_originalForceMax > 0f && _forceMaxField != null)
			{
				float num3 = _originalForceMax * num;
				float num4 = _originalForceConstant * num;
				_forceMaxField.SetValue(PhysGrabber.instance, num3);
				_forceConstantField.SetValue(PhysGrabber.instance, num4);
			}
		}
	}

	private static void RefillAllVisibleBatteries()
	{
		foreach (ItemBattery item in UnityEngine.Object.FindObjectsOfType<ItemBattery>(true))
		{
			RefillBattery(item);
		}
	}

	private static void RefillBattery(ItemBattery battery)
	{
		if ((UnityEngine.Object)(object)battery == (UnityEngine.Object)null)
		{
			return;
		}

		try
		{
			if (battery.batteryLife <= 0f)
			{
				battery.batteryLife = 1f;
			}

			battery.SetBatteryLife(100);
			ReflectionUtils.SetFieldValue(battery, "batteryLifePrev", 100f);
			ReflectionUtils.SetFieldValue(battery, "drainRate", 0f);
			ReflectionUtils.SetFieldValue(battery, "drainTimer", 0f);
			ReflectionUtils.SetFieldValue(battery, "chargeRate", 0f);
			ReflectionUtils.SetFieldValue(battery, "chargeTimer", 0f);
			ReflectionUtils.SetFieldValue(battery, "isCharging", false);
		}
		catch
		{
		}
	}

	private static void RefillDeathHeadBattery()
	{
		if ((UnityEngine.Object)(object)SpectateCamera.instance != (UnityEngine.Object)null)
		{
			ReflectionUtils.SetFieldValue(SpectateCamera.instance, "headEnergy", 1f);
			ReflectionUtils.SetFieldValue(SpectateCamera.instance, "headEnergyEnough", true);
			ReflectionUtils.SetFieldValue(SpectateCamera.instance, "headEnergyPauseTimer", 1f);
		}

		PlayerDeathHead fieldValue = ((UnityEngine.Object)(object)PlayerAvatar.instance != (UnityEngine.Object)null) ? ReflectionUtils.GetFieldValue<PlayerDeathHead>(PlayerAvatar.instance, "playerDeathHead") : null;
		if ((UnityEngine.Object)(object)fieldValue != (UnityEngine.Object)null)
		{
			fieldValue.SpectatedLowEnergySet(_active: false);
		}
	}

	private static PhysGrabObject GetCurrentlyGrabbedObject()
	{
		if ((UnityEngine.Object)(object)PhysGrabber.instance == (UnityEngine.Object)null || _grabbedPhysGrabObjectField == null)
		{
			return null;
		}
		return (PhysGrabObject)_grabbedPhysGrabObjectField.GetValue(PhysGrabber.instance);
	}

	private static void ApplyHeldObjectGrabOverrides(PhysGrabObject grabbedObject, float strengthMultiplier)
	{
		if ((UnityEngine.Object)(object)grabbedObject == (UnityEngine.Object)null)
		{
			return;
		}
		TrySetFloat(_physGrabObjectOverrideGrabStrengthField, grabbedObject, strengthMultiplier);
		TrySetFloat(_physGrabObjectOverrideGrabStrengthTimerField, grabbedObject, 0.25f);
		TrySetFloat(_physGrabObjectOverrideMinGrabStrengthField, grabbedObject, strengthMultiplier);
		TrySetFloat(_physGrabObjectOverrideMinGrabStrengthTimerField, grabbedObject, 0.25f);
		TryInvoke(_physGrabObjectOverrideGrabStrengthMethod, grabbedObject, strengthMultiplier, 0.25f);
		TryInvoke(_physGrabObjectOverrideMinGrabStrengthMethod, grabbedObject, strengthMultiplier, 0.25f);
	}

	private static void TrySetFloat(FieldInfo field, object instance, float value)
	{
		if (field == null || instance == null)
		{
			return;
		}
		try
		{
			field.SetValue(instance, value);
		}
		catch
		{
		}
	}

	private static void TryInvoke(MethodInfo method, object instance, params object[] args)
	{
		if (method == null || instance == null)
		{
			return;
		}
		try
		{
			method.Invoke(instance, args);
		}
		catch
		{
		}
	}

	private static void ApplyJumpModifiers()
	{
		if ((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null)
		{
			return;
		}
		if (FreeCam.Enabled)
		{
			PlayerController.instance.JumpForce = 0f;
		}
		else if (_originalJumpForce > 0f)
		{
			float num = ConfigManager.Config.Local.JumpForceEnabled ? (_originalJumpForce * ConfigManager.Config.Local.JumpForce) : _originalJumpForce;
			if (Math.Abs(PlayerController.instance.JumpForce - num) > 0.01f)
			{
				PlayerController.instance.JumpForce = num;
			}
		}
	}

	private static void ApplyGravityModifiers()
	{
		if (!((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null) && !_noClipActive && _originalGravity > 0f)
		{
			float num = ConfigManager.Config.Local.GravityEnabled ? (_originalGravity * ConfigManager.Config.Local.Gravity) : _originalGravity;
			if (Math.Abs(PlayerController.instance.CustomGravity - num) > 0.01f)
			{
				PlayerController.instance.CustomGravity = num;
			}
		}
	}

	private static void ApplyFOV()
	{
		float fOV = Mathf.Clamp(ConfigManager.Config.Misc.FOV, 5f, 170f);
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)CameraZoom.Instance != (UnityEngine.Object)null)
		{
			if (_originalFOV < 0f && CameraZoom.Instance.playerZoomDefault > 0f)
			{
				_originalFOV = CameraZoom.Instance.playerZoomDefault;
			}
			if (Math.Abs(CameraZoom.Instance.playerZoomDefault - fOV) > 0.1f)
			{
				CameraZoom.Instance.playerZoomDefault = fOV;
			}
			if (CameraZoom.Instance.cams != null)
			{
				foreach (Camera cam in CameraZoom.Instance.cams)
				{
					if (!((UnityEngine.Object)(object)cam == (UnityEngine.Object)null) && Math.Abs(cam.fieldOfView - fOV) > 0.1f)
					{
						cam.fieldOfView = fOV;
					}
				}
			}
		}
		if (!((UnityEngine.Object)(object)main == (UnityEngine.Object)null))
		{
			if (_originalFOV < 0f)
			{
				_originalFOV = main.fieldOfView;
			}
			if (Math.Abs(main.fieldOfView - fOV) > 0.1f)
			{
				main.fieldOfView = fOV;
			}
		}
	}

	public static bool TeleportLocalPlayer(Vector3 destination, Quaternion rotation, string successMessage)
	{
		PlayerAvatar localPlayerAvatar = ResolveLocalPlayerAvatar();
		if ((UnityEngine.Object)(object)localPlayerAvatar == (UnityEngine.Object)null)
		{
			LastActionStatus = "本地玩家未初始化，无法执行传送。";
			return false;
		}

		destination = ResolvePlayerSafeDestination(destination);
		ResetLocalPlayerVelocity();
		#region debug-point B:teleport-local-player
		DebugReport("B", "LocalPlayerManager.TeleportLocalPlayer", "开始执行玩家传送", "singleOrMaster=" + SemiFunc.IsMasterClientOrSingleplayer() + ",avatar=" + DescribePlayerAvatar(localPlayerAvatar) + ",destination=" + destination + ",rotationY=" + rotation.eulerAngles.y + ",avatarPosBefore=" + GetTransformPosition(localPlayerAvatar) + ",controllerPosBefore=" + GetTransformPosition(PlayerController.instance) + ",rbPosBefore=" + GetRigidbodyPosition(PlayerController.instance));
		#endregion
		_pendingHomeTeleportDebugFrames = 12;
		_pendingHomeTeleportTarget = destination;
		_pendingHomeTeleportRotation = rotation;
		_pendingHomeTeleportSource = successMessage;

		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			ExecuteAuthoritativePlayerTeleport(localPlayerAvatar, destination, rotation, "TeleportLocalPlayer");
			#region debug-point H4:teleport-local-direct-after-spawn
			DebugReport("H4", "LocalPlayerManager.TeleportLocalPlayer", "本地/主机直接调用 Spawn 后状态", "avatar=" + DescribePlayerAvatar(localPlayerAvatar) + ",destination=" + destination + ",rotationY=" + rotation.eulerAngles.y + ",avatarPosAfter=" + GetTransformPosition(localPlayerAvatar) + ",controllerPosAfter=" + GetTransformPosition(PlayerController.instance) + ",rbPosAfter=" + GetRigidbodyPosition(PlayerController.instance));
			#endregion
			LastActionStatus = successMessage;
			return true;
		}

		LocalPlayerNetHandler localNetHandler = GetLocalNetHandler();
		if ((UnityEngine.Object)(object)localNetHandler == (UnityEngine.Object)null || !localNetHandler.RequestSpawn(destination, rotation))
		{
			LastActionStatus = "本地玩家网络传送处理器未就绪。";
			return false;
		}

		#region debug-point H2:teleport-local-network-requested
		DebugReport("H2", "LocalPlayerManager.TeleportLocalPlayer", "已向主机发送回家传送请求", "avatar=" + DescribePlayerAvatar(localPlayerAvatar) + ",netHandler=" + (((UnityEngine.Object)(object)localNetHandler != (UnityEngine.Object)null) ? ((UnityEngine.Object)(object)localNetHandler).name : "null") + ",destination=" + destination + ",rotationY=" + rotation.eulerAngles.y);
		#endregion
		LastActionStatus = successMessage;
		return true;
	}

	public static bool TeleportEnemyToLocalPlayer(Enemy enemy, string enemyName)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null || (UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null)
		{
			LastActionStatus = "目标怪物或本地玩家不可用，无法执行传送。";
			return false;
		}

		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			PlayerAvatar playerAvatar = ResolveLocalPlayerAvatar();
			Transform transform = ((Component)PlayerController.instance).transform;
			Vector3 destination = ResolveEnemyPullDestination(playerAvatar, transform);
			ExecuteAuthoritativeEnemyTeleport(enemy, destination);
			LastActionStatus = string.Format("已将 `{0}` 传送到玩家身边。", enemyName);
			return true;
		}

		PhotonView component = ResolveEnemyPhotonView(enemy);
		LocalPlayerNetHandler localNetHandler = GetLocalNetHandler();
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null || component.ViewID == 0 || (UnityEngine.Object)(object)localNetHandler == (UnityEngine.Object)null || !localNetHandler.RequestTeleportEnemyToPlayer(component.ViewID))
		{
			LastActionStatus = string.Format("无法向主机发送 `{0}` 的传送请求。", enemyName);
			return false;
		}

		LastActionStatus = string.Format("已向主机发送 `{0}` 拉到身边的请求。", enemyName);
		return true;
	}

	public static bool RequestLootTeleport(Vector3 targetCenter, bool teleportDoors, out string status)
	{
		status = "本地玩家网络传送处理器未就绪。";
		LocalPlayerNetHandler localNetHandler = GetLocalNetHandler();
		if ((UnityEngine.Object)(object)localNetHandler == (UnityEngine.Object)null)
		{
			return false;
		}

		int mode = teleportDoors ? LootTeleportModeDoors : LootTeleportModeValuables;
		if (!localNetHandler.RequestLootTeleport(targetCenter, mode))
		{
			return false;
		}

		status = teleportDoors ? "已向主机发送门传送请求。" : "已向主机发送有价值物品传送请求。";
		return true;
	}

	public static void RequestLooseItemProtection()
	{
		LocalPlayerNetHandler localNetHandler = GetLocalNetHandler();
		if ((UnityEngine.Object)(object)localNetHandler == (UnityEngine.Object)null)
		{
			return;
		}

		localNetHandler.RequestLooseItemProtection();
	}

	public static bool TeleportHome()
	{
		#region debug-point H1:teleport-home-click
		DebugReport("H1", "LocalPlayerManager.TeleportHome", "收到一键回家请求", "controller=" + GetTransformPosition(PlayerController.instance) + ",avatar=" + DescribePlayerAvatar(ResolveLocalPlayerAvatar()) + ",controllerAvatar=" + DescribePlayerAvatar((PlayerController.instance != null) ? PlayerController.instance.playerAvatarScript : null));
		#endregion
		if (!ResolveHomeDestination(out var position, out var rotation, out var description))
		{
			LastActionStatus = "当前关卡未找到可用的出生点或电车安全点，无法一键回家。";
			return false;
		}

		#region debug-point H1:teleport-home-resolved
		DebugReport("H1", "LocalPlayerManager.TeleportHome", "一键回家已解析目标", "description=" + description + ",position=" + position + ",rotationY=" + rotation.eulerAngles.y);
		#endregion
		return TeleportLocalPlayer(position, rotation, "已返回" + description + "。");
	}

	private static LocalPlayerNetHandler GetLocalNetHandler()
	{
		PlayerAvatar val = ResolveLocalPlayerAvatar();
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			return null;
		}

		LocalPlayerNetHandler component = ((Component)val).GetComponent<LocalPlayerNetHandler>();
		if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
		{
			component = ((Component)val).gameObject.AddComponent<LocalPlayerNetHandler>();
		}

		return component;
	}

	private static Vector3 ResolveEnemyPullDestination(PlayerAvatar playerAvatar, Transform playerTransform)
	{
		Vector3 forward = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up).normalized;
		if (forward.sqrMagnitude <= 0.001f)
		{
			forward = Vector3.forward;
		}

		Vector3 playerBasePosition = ResolvePlayerTeleportBasePosition(playerAvatar, playerTransform);
		Vector3 desiredPoint = playerBasePosition + forward * 1.6f;
		Vector3 groundedPoint = ResolveGroundedPointNearHeight(desiredPoint, 0.12f, 0.4f, 4.5f);
		bool flag = NavMesh.SamplePosition(groundedPoint, out NavMeshHit hit, 2.5f, NavMesh.AllAreas);
		if (flag)
		{
			groundedPoint = hit.position + Vector3.up * 0.1f;
		}

		#region debug-point A:resolve-enemy-pull-destination
		DebugReport("A", "LocalPlayerManager.ResolveEnemyPullDestination", "计算怪物拉到玩家身边的目标点", "player=" + playerTransform.position + ",playerBase=" + playerBasePosition + ",desired=" + desiredPoint + ",grounded=" + groundedPoint + ",navHit=" + flag);
		#endregion

		return groundedPoint;
	}

	private static Vector3 ResolvePlayerTeleportBasePosition(PlayerAvatar playerAvatar, Transform playerTransform)
	{
		if ((UnityEngine.Object)(object)playerAvatar != (UnityEngine.Object)null && _playerAvatarLastNavmeshPositionField != null && _playerAvatarLastNavmeshPositionTimerField != null)
		{
			try
			{
				float num = (float)_playerAvatarLastNavmeshPositionTimerField.GetValue(playerAvatar);
				Vector3 vector = (Vector3)_playerAvatarLastNavmeshPositionField.GetValue(playerAvatar);
				if (num <= 1.25f && IsFinite(vector))
				{
					return vector;
				}
			}
			catch
			{
			}
		}

		return ((UnityEngine.Object)(object)playerTransform != (UnityEngine.Object)null) ? playerTransform.position : Vector3.zero;
	}

	internal static Vector3 ResolveGroundedPoint(Vector3 roughPoint, float minLift)
	{
		Vector3 probeStart = roughPoint + Vector3.up * 2f;
		RaycastHit[] array = Physics.RaycastAll(probeStart, Vector3.down, 10f, -1, QueryTriggerInteraction.Ignore);
		if (array != null && array.Length > 0)
		{
			Array.Sort(array, (left, right) => left.distance.CompareTo(right.distance));
			bool flag = false;
			float num = float.MaxValue;
			RaycastHit raycastHit = default(RaycastHit);
			for (int i = 0; i < array.Length; i++)
			{
				RaycastHit raycastHit2 = array[i];
				if (raycastHit2.normal.y < 0.3f || IsDynamicGroundBlocker(raycastHit2.collider))
				{
					continue;
				}

				if (raycastHit2.point.y > roughPoint.y + 0.5f)
				{
					continue;
				}

				float num2 = Mathf.Abs(raycastHit2.point.y - roughPoint.y);

				if (!flag || num2 < num)
				{
					num = num2;
					raycastHit = raycastHit2;
					flag = true;
				}
			}

			if (flag)
			{
				Vector3 vector = raycastHit.point + Vector3.up * Mathf.Max(minLift, 0.08f);
				#region debug-point A:resolve-grounded-point-hit
				DebugReport("A", "LocalPlayerManager.ResolveGroundedPoint", "按目标高度选中地面候选点", "rough=" + roughPoint + ",probeStart=" + probeStart + ",hitCollider=" + ((UnityEngine.Object)(object)raycastHit.collider != (UnityEngine.Object)null ? ((UnityEngine.Object)(object)raycastHit.collider).name : "null") + ",hitPoint=" + raycastHit.point + ",score=" + num.ToString("F3") + ",result=" + vector);
				#endregion
				return vector;
			}
		}

		Vector3 vector2 = roughPoint + Vector3.up * Mathf.Max(minLift, 0.08f);
		#region debug-point A:resolve-grounded-point-fallback
		DebugReport("A", "LocalPlayerManager.ResolveGroundedPoint", "未命中静态地面，使用回退目标点", "rough=" + roughPoint + ",probeStart=" + probeStart + ",result=" + vector2);
		#endregion
		return vector2;
	}

	internal static Vector3 ResolveGroundedPointNearHeight(Vector3 roughPoint, float minLift, float maxRise, float maxDrop)
	{
		float num = Mathf.Max(maxRise, 0.2f);
		float num2 = Mathf.Max(maxDrop, 0.5f);
		Vector3 probeStart = roughPoint + Vector3.up * num;
		RaycastHit[] array = Physics.RaycastAll(probeStart, Vector3.down, num + num2, -1, QueryTriggerInteraction.Ignore);
		if (array != null && array.Length > 0)
		{
			Array.Sort(array, (left, right) => left.distance.CompareTo(right.distance));
			float num3 = roughPoint.y + num + 0.05f;
			float num4 = float.MaxValue;
			bool flag = false;
			Vector3 vector = Vector3.zero;
			for (int i = 0; i < array.Length; i++)
			{
				RaycastHit raycastHit = array[i];
				if (raycastHit.normal.y < 0.3f || IsDynamicGroundBlocker(raycastHit.collider) || raycastHit.point.y > num3)
				{
					continue;
				}

				if (raycastHit.point.y > roughPoint.y + 0.5f)
				{
					continue;
				}

				Vector3 vector2 = raycastHit.point + Vector3.up * Mathf.Max(minLift, 0.08f);
				float num5 = Mathf.Abs(raycastHit.point.y - roughPoint.y);
				if (raycastHit.point.y > roughPoint.y)
				{
					num5 += 1f;
				}

				if (!flag || num5 < num4)
				{
					num4 = num5;
					vector = vector2;
					flag = true;
				}
			}

			if (flag)
			{
				#region debug-point A:resolve-grounded-point-near-height-hit
				DebugReport("A", "LocalPlayerManager.ResolveGroundedPointNearHeight", "按高度约束命中地面候选点", "rough=" + roughPoint + ",probeStart=" + probeStart + ",result=" + vector + ",maxRise=" + maxRise + ",maxDrop=" + maxDrop);
				#endregion
				return vector;
			}
		}

		return ResolveGroundedPoint(roughPoint, minLift);
	}

	private static bool IsDynamicGroundBlocker(Collider collider)
	{
		if ((UnityEngine.Object)(object)collider == (UnityEngine.Object)null)
		{
			return true;
		}

		Transform transform = collider.transform;
		return (UnityEngine.Object)(object)transform.GetComponentInParent<PhysGrabObject>() != (UnityEngine.Object)null || (UnityEngine.Object)(object)transform.GetComponentInParent<PlayerAvatar>() != (UnityEngine.Object)null || (UnityEngine.Object)(object)transform.GetComponentInParent<Enemy>() != (UnityEngine.Object)null;
	}

	internal static void DebugReport(string hypothesisId, string location, string msg, string data)
	{
		if (!IsRuntimeDebugEnabled())
		{
			return;
		}
		try
		{
			string s = "{\"sessionId\":\"" + EscapeJson(_debugSessionId) + "\",\"runId\":\"" + EscapeJson(_debugRunId) + "\",\"hypothesisId\":\"" + EscapeJson(hypothesisId) + "\",\"location\":\"" + EscapeJson(location) + "\",\"msg\":\"[DEBUG] " + EscapeJson(msg) + "\",\"data\":{\"text\":\"" + EscapeJson(data ?? string.Empty) + "\"},\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() + "}";
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(_debugServerUrl);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/json";
			httpWebRequest.Timeout = 250;
			httpWebRequest.ReadWriteTimeout = 250;
			httpWebRequest.Proxy = null;
			using (Stream requestStream = httpWebRequest.GetRequestStream())
			{
				requestStream.Write(bytes, 0, bytes.Length);
			}

			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
		}
		catch
		{
		}
	}

	private static double GetElapsedMilliseconds(long startTimestamp)
	{
		return (double)(Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / (double)Stopwatch.Frequency;
	}

	private static void RefreshDebugConfigIfNeeded()
	{
		DateTime utcNow = DateTime.UtcNow;
		if (_debugConfigLoaded && utcNow < _nextDebugConfigRefreshUtc)
		{
			return;
		}

		_debugConfigLoaded = true;
		_nextDebugConfigRefreshUtc = utcNow.AddSeconds(DebugConfigRefreshIntervalSeconds);
		_debugReportEnabled = false;
		_debugServerUrl = "http://127.0.0.1:7777/event";
		_debugSessionId = DebugSessionId;
		_debugRunId = DebugRunId;
		if (!File.Exists(DebugEnvPath))
		{
			return;
		}

		try
		{
			string[] array = File.ReadAllLines(DebugEnvPath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				if (text.StartsWith("DEBUG_REPORT_ENABLED=", StringComparison.Ordinal))
				{
					_debugReportEnabled = ParseDebugBoolean(text.Substring("DEBUG_REPORT_ENABLED=".Length));
				}
				else if (text.StartsWith("DEBUG_SERVER_URL=", StringComparison.Ordinal))
				{
					_debugServerUrl = text.Substring("DEBUG_SERVER_URL=".Length).Trim();
				}
				else if (text.StartsWith("DEBUG_SESSION_ID=", StringComparison.Ordinal))
				{
					_debugSessionId = text.Substring("DEBUG_SESSION_ID=".Length).Trim();
				}
				else if (text.StartsWith("DEBUG_RUN_ID=", StringComparison.Ordinal))
				{
					_debugRunId = text.Substring("DEBUG_RUN_ID=".Length).Trim();
				}
			}
		}
		catch
		{
			_debugReportEnabled = false;
			_debugServerUrl = "http://127.0.0.1:7777/event";
			_debugSessionId = DebugSessionId;
			_debugRunId = DebugRunId;
		}
	}

	private static bool ParseDebugBoolean(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		value = value.Trim();
		return string.Equals(value, "1", StringComparison.Ordinal) || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
	}

	private static string EscapeJson(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
	}

	private static PlayerAvatar ResolveLocalPlayerAvatar()
	{
		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null && (UnityEngine.Object)(object)PlayerController.instance.playerAvatarScript != (UnityEngine.Object)null)
		{
			return PlayerController.instance.playerAvatarScript;
		}

		if ((UnityEngine.Object)(object)PlayerAvatar.instance != (UnityEngine.Object)null)
		{
			return PlayerAvatar.instance;
		}

		PlayerAvatar[] array = UnityEngine.Object.FindObjectsByType<PlayerAvatar>((FindObjectsSortMode)0);
		for (int i = 0; i < array.Length; i++)
		{
			PlayerAvatar val = array[i];
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null) && !((UnityEngine.Object)(object)val.photonView == (UnityEngine.Object)null) && val.photonView.IsMine)
			{
				return val;
			}
		}

		return null;
	}

	private static PhotonView ResolveEnemyPhotonView(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return null;
		}

		PhotonView component = ((Component)enemy).GetComponent<PhotonView>();
		if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null && component.ViewID != 0)
		{
			return component;
		}

		component = ((Component)enemy).GetComponentInParent<PhotonView>();
		if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null && component.ViewID != 0)
		{
			return component;
		}

		component = ((Component)enemy).GetComponentInChildren<PhotonView>(true);
		if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null && component.ViewID != 0)
		{
			return component;
		}

		return null;
	}

	private static Vector3 ResolvePlayerSafeDestination(Vector3 desiredPoint)
	{
		Vector3 groundedPoint = ResolveGroundedPoint(desiredPoint, 0.12f);
		if (NavMesh.SamplePosition(groundedPoint, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
		{
			groundedPoint = hit.position + Vector3.up * 0.12f;
		}

		return groundedPoint;
	}

	private static void ExecuteAuthoritativePlayerTeleport(PlayerAvatar avatar, Vector3 destination, Quaternion rotation, string source)
	{
		if ((UnityEngine.Object)(object)avatar == (UnityEngine.Object)null)
		{
			return;
		}

		avatar.Spawn(destination, rotation);
		ApplyNativePlayerTeleportState(avatar, destination, rotation, source);
	}

	private static void ApplyNativePlayerTeleportState(PlayerAvatar avatar, Vector3 destination, Quaternion rotation, string source)
	{
		if ((UnityEngine.Object)(object)avatar == (UnityEngine.Object)null)
		{
			return;
		}

		Transform avatarTransform = ((Component)avatar).transform;
		avatarTransform.position = destination;
		avatarTransform.rotation = rotation;
		TrySetVector3(_playerAvatarClientPositionField, avatar, destination);
		TrySetVector3(_playerAvatarClientPositionCurrentField, avatar, destination);
		TrySetQuaternion(_playerAvatarClientRotationField, avatar, rotation);
		TrySetQuaternion(_playerAvatarClientRotationCurrentField, avatar, rotation);
		TrySetVector3(_playerAvatarLastNavmeshPositionField, avatar, destination);
		TrySetFloat(_playerAvatarLastNavmeshPositionTimerField, avatar, 0f);
		PlayerTumble playerTumble = GetPlayerTumble(avatar);
		PhysGrabObject playerPhysGrabObject = GetPlayerTumblePhysGrabObject(playerTumble);
		if ((UnityEngine.Object)(object)playerTumble != (UnityEngine.Object)null && (UnityEngine.Object)(object)playerPhysGrabObject != (UnityEngine.Object)null)
		{
			playerPhysGrabObject.Teleport(destination, rotation);
			if ((UnityEngine.Object)(object)playerPhysGrabObject.rb != (UnityEngine.Object)null)
			{
				playerPhysGrabObject.rb.position = destination;
				playerPhysGrabObject.rb.rotation = rotation;
				playerPhysGrabObject.rb.velocity = Vector3.zero;
				playerPhysGrabObject.rb.angularVelocity = Vector3.zero;
			}

			ReflectionUtils.SetFieldValue(playerPhysGrabObject, "targetPos", destination);
			ReflectionUtils.SetFieldValue(playerPhysGrabObject, "currentPosition", destination);
			playerTumble.DisableCustomGravity(0.35f);
		}

		avatar.FallDamageResetSet(0.75f);
		if (PlayerController.instance != null && (UnityEngine.Object)(object)PlayerController.instance.playerAvatarScript == (UnityEngine.Object)(object)avatar)
		{
			PlayerController.instance.transform.position = destination;
			PlayerController.instance.transform.rotation = rotation;
			if ((UnityEngine.Object)(object)PlayerController.instance.rb != (UnityEngine.Object)null)
			{
				PlayerController.instance.rb.position = destination;
				PlayerController.instance.rb.rotation = rotation;
				PlayerController.instance.rb.velocity = Vector3.zero;
				PlayerController.instance.rb.angularVelocity = Vector3.zero;
			}

			PlayerController.instance.Velocity = Vector3.zero;
			PlayerController.instance.VelocityRelative = Vector3.zero;
			PlayerController.instance.InputDisable(0.15f);
			PlayerController.instance.Kinematic(0.15f);
			if ((UnityEngine.Object)(object)PlayerController.instance.CollisionController != (UnityEngine.Object)null)
			{
				PlayerController.instance.CollisionController.GroundedDisableTimer = Mathf.Max(PlayerController.instance.CollisionController.GroundedDisableTimer, 0.15f);
				PlayerController.instance.CollisionController.OverrideDisableFallLoop(0.35f);
				PlayerController.instance.CollisionController.ResetFalling();
			}
		}

		ResetLocalPlayerVelocity();
		Physics.SyncTransforms();
		#region debug-point H4:apply-native-player-teleport
		DebugReport("H4", "LocalPlayerManager.ApplyNativePlayerTeleportState", "已按原生物理链路修正玩家传送状态", "source=" + (source ?? string.Empty) + ",avatar=" + DescribePlayerAvatar(avatar) + ",destination=" + destination + ",rotationY=" + rotation.eulerAngles.y + ",avatarPos=" + avatarTransform.position + ",controllerPos=" + GetTransformPosition(PlayerController.instance) + ",rbPos=" + GetRigidbodyPosition(PlayerController.instance) + ",hasTumble=" + ((UnityEngine.Object)(object)playerTumble != (UnityEngine.Object)null));
		#endregion
	}

	private static void ExecuteAuthoritativeEnemyTeleport(Enemy enemy, Vector3 destination)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return;
		}

		enemy.EnemyTeleported(destination);
		ApplyNativeEnemyTeleportState(enemy, destination);
	}

	private static void ApplyNativeEnemyTeleportState(Enemy enemy, Vector3 destination)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return;
		}

		Transform enemyTransform = ((Component)enemy).transform;
		enemyTransform.position = destination;

		try
		{
			EnemyNavMeshAgent enemyNav = ReflectionUtils.GetFieldValue<EnemyNavMeshAgent>(enemy, "NavMeshAgent");
			if ((UnityEngine.Object)(object)enemyNav != (UnityEngine.Object)null)
			{
				NavMeshAgent agent = ReflectionUtils.GetFieldValue<NavMeshAgent>(enemyNav, "Agent");
				if ((UnityEngine.Object)(object)agent != (UnityEngine.Object)null && agent.isActiveAndEnabled)
				{
					agent.Warp(destination);
				}
			}
		}
		catch { }

		Rigidbody rb = ((Component)enemy).GetComponent<Rigidbody>();
		if ((UnityEngine.Object)(object)rb == (UnityEngine.Object)null)
		{
			rb = ((Component)enemy).GetComponentInChildren<Rigidbody>();
		}
		if ((UnityEngine.Object)(object)rb != (UnityEngine.Object)null)
		{
			rb.position = destination;
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}

		try
		{
			object enemyRb = ReflectionUtils.GetFieldValue(enemy, "EnemyRigidbody");
			if (enemyRb != null)
			{
				PhysGrabObject pgo = ReflectionUtils.GetFieldValue<PhysGrabObject>(enemyRb, "physGrabObject");
				if ((UnityEngine.Object)(object)pgo != (UnityEngine.Object)null)
				{
					pgo.Teleport(destination, enemyTransform.rotation);
					if ((UnityEngine.Object)(object)pgo.rb != (UnityEngine.Object)null)
					{
						pgo.rb.position = destination;
						pgo.rb.velocity = Vector3.zero;
						pgo.rb.angularVelocity = Vector3.zero;
					}
					ReflectionUtils.SetFieldValue(pgo, "targetPos", destination);
					ReflectionUtils.SetFieldValue(pgo, "currentPosition", destination);
				}
			}
		}
		catch { }
	}

	private static void ResetLocalPlayerVelocity()
	{
		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null && (UnityEngine.Object)(object)PlayerController.instance.rb != (UnityEngine.Object)null)
		{
			PlayerController.instance.rb.velocity = Vector3.zero;
			PlayerController.instance.rb.angularVelocity = Vector3.zero;
		}
	}

	private static PlayerTumble GetPlayerTumble(PlayerAvatar avatar)
	{
		if ((UnityEngine.Object)(object)avatar == (UnityEngine.Object)null || _tumbleField == null)
		{
			return null;
		}

		return _tumbleField.GetValue(avatar) as PlayerTumble;
	}

	private static PhysGrabObject GetPlayerTumblePhysGrabObject(PlayerTumble playerTumble)
	{
		if ((UnityEngine.Object)(object)playerTumble == (UnityEngine.Object)null || _playerTumblePhysGrabObjectField == null)
		{
			return null;
		}

		try
		{
			return _playerTumblePhysGrabObjectField.GetValue(playerTumble) as PhysGrabObject;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsFinite(Vector3 value)
	{
		return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
	}

	private static string DescribePlayerAvatar(PlayerAvatar avatar)
	{
		if ((UnityEngine.Object)(object)avatar == (UnityEngine.Object)null)
		{
			return "null";
		}

		PhotonView photonView = avatar.photonView;
		bool fieldValue = ReflectionUtils.GetFieldValue<bool>(avatar, "isLocal");
		bool fieldValue2 = ReflectionUtils.GetFieldValue<bool>(avatar, "spawned");
		return ((UnityEngine.Object)(object)avatar).name + "|viewId=" + (((UnityEngine.Object)(object)photonView != (UnityEngine.Object)null) ? photonView.ViewID.ToString() : "null") + "|isMine=" + (((UnityEngine.Object)(object)photonView != (UnityEngine.Object)null) ? photonView.IsMine.ToString() : "false") + "|isLocal=" + fieldValue + "|spawned=" + fieldValue2;
	}

	private static string GetTransformPosition(Component component)
	{
		return ((UnityEngine.Object)(object)component != (UnityEngine.Object)null) ? component.transform.position.ToString() : "null";
	}

	private static string GetRigidbodyPosition(PlayerController controller)
	{
		return (controller != null && (UnityEngine.Object)(object)controller.rb != (UnityEngine.Object)null) ? controller.rb.position.ToString() : "null";
	}

	private static void TrySetVector3(FieldInfo field, object instance, Vector3 value)
	{
		if (field == null || instance == null)
		{
			return;
		}
		try
		{
			field.SetValue(instance, value);
		}
		catch
		{
		}
	}

	private static void TrySetQuaternion(FieldInfo field, object instance, Quaternion value)
	{
		if (field == null || instance == null)
		{
			return;
		}
		try
		{
			field.SetValue(instance, value);
		}
		catch
		{
		}
	}

	public static void Cleanup()
	{
		if (_noClipActive)
		{
			ConfigManager.Config.Local.NoClip = false;
			ApplyNoClip();
		}
		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
		{
			if (_originalSprintSpeed > 0f)
			{
				PlayerController.instance.SprintSpeed = _originalSprintSpeed;
			}
			if (_originalCrouchSpeed > 0f)
			{
				PlayerController.instance.CrouchSpeed = _originalCrouchSpeed;
			}
			if (_originalJumpForce > 0f)
			{
				PlayerController.instance.JumpForce = _originalJumpForce;
			}
			if (_originalGravity > 0f)
			{
				PlayerController.instance.CustomGravity = _originalGravity;
			}
		}
		if ((UnityEngine.Object)(object)PhysGrabber.instance != (UnityEngine.Object)null)
		{
			if (_originalGrabRange > 0f)
			{
				PhysGrabber.instance.grabRange = _originalGrabRange;
				PhysGrabber.instance.grabReleaseDistance = _originalGrabReleaseDistance;
			}
			if (_originalForceMax > 0f && _forceMaxField != null)
			{
				_forceMaxField.SetValue(PhysGrabber.instance, _originalForceMax);
				_forceConstantField.SetValue(PhysGrabber.instance, _originalForceConstant);
			}
		}
		if ((UnityEngine.Object)(object)RoundDirector.instance != (UnityEngine.Object)null && _debugInfiniteBatteryField != null)
		{
			_debugInfiniteBatteryField.SetValue(RoundDirector.instance, false);
		}
		if (_originalFOV > 0f)
		{
			if ((UnityEngine.Object)(object)CameraZoom.Instance != (UnityEngine.Object)null)
			{
				CameraZoom.Instance.playerZoomDefault = _originalFOV;
				if (CameraZoom.Instance.cams != null)
				{
					foreach (Camera cam in CameraZoom.Instance.cams)
					{
						if (!((UnityEngine.Object)(object)cam == (UnityEngine.Object)null))
						{
							cam.fieldOfView = _originalFOV;
						}
					}
				}
			}
			if ((UnityEngine.Object)(object)Camera.main != (UnityEngine.Object)null)
			{
				Camera.main.fieldOfView = _originalFOV;
			}
		}
		if ((UnityEngine.Object)(object)PlayerAvatar.instance != (UnityEngine.Object)null && _godModeField != null)
		{
			PlayerHealth playerHealth = PlayerAvatar.instance.playerHealth;
			if ((UnityEngine.Object)(object)playerHealth != (UnityEngine.Object)null)
			{
				_godModeField.SetValue(playerHealth, false);
			}
		}
		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
		{
			PlayerController.instance.DebugNoTumble = false;
		}
		_initialized = false;
		_nextBatteryRefreshTime = 0f;
		LastActionStatus = string.Empty;
		Console.WriteLine("[LocalPlayer] Cleaned up");
	}
}
