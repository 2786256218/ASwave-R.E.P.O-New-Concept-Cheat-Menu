using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Cheat.Config;
using Cheat.Features.Enemies;
using Cheat.Features.ItemSpawner;
using Cheat.Features.Loot;
using Cheat.Features.Teleport;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

namespace Cheat.Features.LocalPlayer;

public static class LocalPlayerManager
{
        private const string DebugEnvPath = ".dbg\\global-stutter-aimbot.env";

        private const string DebugEnvPathFallback = "e:\\Projects\\REPOCheat\\REPO_New_Cheat\\.dbg\\global-stutter-aimbot.env";

        private const string DebugSessionId = "global-stutter-aimbot";

	private const string DebugRunId = "pre-fix";

	private const double DebugConfigRefreshIntervalSeconds = 5.0;
	private const float NetHandlerRefreshIntervalSeconds = 3f;
	private const float ItemSpawnRequestCooldownSeconds = 0.35f;
	private const float AimbotScanIntervalSeconds = 0.02f;
	private const float AimbotAssistGraceSeconds = 0.12f;
	private const float AimbotPullHoldSeconds = 0.08f;
	// ItemGun.aimVerticalOffset is negative when the barrel points up; the stock
	// value is -5 (standing) / -1 (crouching). Both are lowered by ~1.5 degrees
	// here because the held muzzle sat visibly too high.
	private const float StandAimVerticalOffset = -3.5f;
	private const float CrouchAimVerticalOffset = 0.5f;
	private const double DebugReportMinimumIntervalSeconds = 0.1;

	private const float HomeStoredSpawnMatchDistance = 2.5f;

	private const float HomeTruckSpawnSearchRadius = 40f;

	private sealed class LocalPlayerNetHandler : MonoBehaviourPun
	{
		private float _nextItemSpawnRequestTime;
		private float _nextGrabStrengthRequestTime;
		private bool _grabStrengthBaselineCaptured;
		private float _originalRemoteForceMax;
		private float _originalRemoteForceConstant;

		public bool RequestSpawnItem(string resourcePath, Vector3 position, Quaternion rotation)
		{
			if ((UnityEngine.Object)(object)photonView == (UnityEngine.Object)null || !photonView.IsMine || string.IsNullOrWhiteSpace(resourcePath))
			{
				return false;
			}

			photonView.RPC("RequestSpawnItemRPC", RpcTarget.MasterClient, resourcePath, position, rotation);
			return true;
		}

		public bool RequestSpawn(Vector3 position, Quaternion rotation)
		{
			#region debug-point A:request-spawn-entry
			DebugReport("A", "LocalPlayerManager.RequestSpawn", "准备发送玩家传送请求", "handler=" + ((UnityEngine.Object)(object)this).name + ",hasPhotonView=" + ((UnityEngine.Object)(object)photonView != (UnityEngine.Object)null) + ",isMine=" + (((UnityEngine.Object)(object)photonView != (UnityEngine.Object)null) ? photonView.IsMine.ToString() : "false") + ",position=" + position + ",rotationY=" + rotation.eulerAngles.y);
			#endregion
			if ((UnityEngine.Object)(object)photonView == (UnityEngine.Object)null || !photonView.IsMine)
			{
				#region debug-point E:request-spawn-rejected
				DebugReport("E", "LocalPlayerManager.RequestSpawn", "玩家传送请求发送被拒绝", "reason=no-photon-view-or-not-mine,handler=" + ((UnityEngine.Object)(object)this).name);
				#endregion
				return false;
			}

			photonView.RPC("RequestSpawnRPC", RpcTarget.MasterClient, position, rotation);
			return true;
		}

		[PunRPC]
		private void RequestSpawnItemRPC(string resourcePath, Vector3 position, Quaternion rotation, PhotonMessageInfo info = default(PhotonMessageInfo))
		{
			if (!PhotonNetwork.IsMasterClient || string.IsNullOrWhiteSpace(resourcePath)
				|| info.Sender == null || info.Sender.ActorNumber != photonView.OwnerActorNr
				|| !IsFinite(position))
			{
				return;
			}

			PlayerAvatar avatar = ((Component)this).GetComponent<PlayerAvatar>();
			if ((UnityEngine.Object)(object)avatar == (UnityEngine.Object)null)
			{
				return;
			}

			float now = Time.unscaledTime;
			if ((_nextItemSpawnRequestTime > 0f && now < _nextItemSpawnRequestTime) || Vector3.Distance(avatar.transform.position, position) > 25f)
			{
				return;
			}
			_nextItemSpawnRequestTime = now + ItemSpawnRequestCooldownSeconds;

			Cheat.Features.ItemSpawner.ItemSpawner spawner = Cheat.Features.ItemSpawner.ItemSpawner.Instance;
			if ((UnityEngine.Object)(object)spawner == (UnityEngine.Object)null || !spawner.TrySpawnRequestedItem(resourcePath, position, rotation))
			{
				UnityEngine.Debug.LogWarning((object)("Host rejected client item spawn request: " + resourcePath));
			}
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

		public bool RequestGrabStrengthMultiplier(float multiplier)
		{
			if ((UnityEngine.Object)(object)photonView == (UnityEngine.Object)null || !photonView.IsMine
				|| float.IsNaN(multiplier) || float.IsInfinity(multiplier))
			{
				return false;
			}

			photonView.RPC("RequestGrabStrengthMultiplierRPC", RpcTarget.MasterClient, Mathf.Clamp(multiplier, 1f, MaxStableGrabStrengthMultiplier));
			return true;
		}

		[PunRPC]
		private void RequestGrabStrengthMultiplierRPC(float multiplier, PhotonMessageInfo info = default(PhotonMessageInfo))
		{
			if (!PhotonNetwork.IsMasterClient || info.Sender == null
				|| info.Sender.ActorNumber != photonView.OwnerActorNr
				|| float.IsNaN(multiplier) || float.IsInfinity(multiplier))
			{
				return;
			}

			float now = Time.unscaledTime;
			multiplier = Mathf.Clamp(multiplier, 1f, MaxStableGrabStrengthMultiplier);
			if (now < _nextGrabStrengthRequestTime && multiplier > 1.001f)
			{
				return;
			}
			_nextGrabStrengthRequestTime = now + 0.18f;

			PlayerAvatar avatar = ((Component)this).GetComponent<PlayerAvatar>();
			PhysGrabber grabber = avatar != null ? avatar.physGrabber : null;
			if ((UnityEngine.Object)(object)grabber == (UnityEngine.Object)null)
			{
				return;
			}

			if (!_grabStrengthBaselineCaptured)
			{
				_originalRemoteForceMax = grabber.forceMax;
				_originalRemoteForceConstant = grabber.forceConstant;
				_grabStrengthBaselineCaptured = true;
			}

			grabber.forceMax = _originalRemoteForceMax * multiplier;
			grabber.forceConstant = _originalRemoteForceConstant * multiplier;
			float baseGrabStrength = ReflectionUtils.GetFieldValue<float>(grabber, "grabStrength");
			grabber.OverrideGrabStrength(baseGrabStrength * multiplier, 0.35f);
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
	private static bool _temporaryHealthLock;
	private static float _temporaryGrabStrengthMultiplier = 1f;
	private static float _temporaryGrabReach;

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

	private static MethodInfo _physGrabObjectOverrideMassMethod;
	private static MethodInfo _physGrabObjectOverrideTorqueStrengthMethod;
	private static MethodInfo _physGrabObjectOverrideDragMethod;

	private static bool _noClipActive = false;

	private static Collider[] _playerColliders;

	private static MonoBehaviour _playerCollision;

	private static Rigidbody _playerRigidbody;
	private static Rigidbody _playerAvatarRigidbody;
	private static NoClipHoverGuard _noClipHoverGuard;
	private static bool _originalAvatarRigidbodyUseGravity;
	private static bool _originalAvatarRigidbodyKinematic;
	private static float _originalAvatarRigidbodyDrag;
	private static bool _avatarRigidbodyStateCaptured;

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

	private static float _nextGrabStrengthNetworkSyncTime;

	private static float _lastGrabStrengthNetworkMultiplier = -1f;

	private static float _nextNetHandlerRefreshTime;

	private static bool _debugConfigLoaded;

	private static bool _debugReportEnabled;

	private static string _debugServerUrl = "http://127.0.0.1:7777/event";

	private static string _debugSessionId = DebugSessionId;

	private static string _debugRunId = DebugRunId;

	private static DateTime _nextDebugConfigRefreshUtc = DateTime.MinValue;

	private static Vector3 _pendingHomeTeleportTarget;

	private static Quaternion _pendingHomeTeleportRotation = Quaternion.identity;

	private static string _pendingHomeTeleportSource = string.Empty;

	private static PlayerAvatar _cachedLocalAvatar;
	private static float _cachedLocalAvatarTime;
	private static float _nextItemInvincibilityTime;
	private static float _nextStationHealTime;
	private static float _lastEnemyActionAmount;
	private static int _lastEnemyDirectorInstanceId;

	public static string LastActionStatus { get; set; } = string.Empty;

	public static PlayerController LocalPlayer => PlayerController.instance;

	public static bool IsRuntimeDebugEnabled()
	{
		RefreshDebugConfigIfNeeded();
		return _debugReportEnabled;
	}

	private static float _homeTeleportNoTumbleTimer = 0f;



	private static float _aimbotAssistUntilTime = 0f;
	private static float _nextAimbotScanTime = 0f;
	private static float _aimbotPullHoldUntilTime = 0f;

	private static Vector3 _aimbotTargetPoint = Vector3.zero;

	private static Enemy _aimbotTarget;
	private static int _debugReportInFlight;
	private static long _lastDebugReportTicks;

		public static float GetAimbotReferenceFov()
		{
			if (_originalFOV > 1f)
			{
				return _originalFOV;
			}

			if ((UnityEngine.Object)(object)Camera.main != (UnityEngine.Object)null)
			{
				return Camera.main.fieldOfView;
			}

			return 60f;
		}

	public static void FixedUpdate()
	{
		if (ConfigManager.Config == null)
		{
			return;
		}

		PhysGrabObject currentlyGrabbedObject = GetCurrentlyGrabbedObject();
		if (currentlyGrabbedObject == null)
		{
			return;
		}

		if (_aimbotAssistUntilTime > Time.unscaledTime)
		{
			ApplySolidAim(currentlyGrabbedObject, true, _aimbotTargetPoint);
			return;
		}

	}

	public static void Update()
	{
		if (!((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null))
		{
			if (_homeTeleportNoTumbleTimer > 0f)
			{
				_homeTeleportNoTumbleTimer -= Time.deltaTime;
				PlayerAvatar avatar = ResolveLocalPlayerAvatar();
				PlayerTumble tumble = GetPlayerTumble(avatar);
				if ((UnityEngine.Object)(object)tumble != (UnityEngine.Object)null && ReflectionUtils.GetFieldValue<bool>(tumble, "isTumbling"))
				{
					tumble.TumbleSet(false, false);
				}
			}

			if (!_initialized)
			{
				Initialize();
			}
			
			ApplyGodMode();
			ApplyInfiniteStamina();
			ApplyInfiniteBattery();
			ApplyNoClip();
			UpdateAimbotState();
			ApplySpeedModifiers();
			ApplyGrabModifiers();
			ApplyJumpModifiers();
			ApplyGravityModifiers();
			ApplyFOV();
			ApplyThirdPerson();
			ApplyTumbleFly();
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
				_playerColliders = ((Component)PlayerController.instance).GetComponentsInChildren<Collider>(true);
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
                bool runtimeDebugEnabled = IsRuntimeDebugEnabled();
                long startTimestamp = runtimeDebugEnabled ? Stopwatch.GetTimestamp() : 0L;
		PlayerAvatar[] array = UnityEngine.Object.FindObjectsByType<PlayerAvatar>((FindObjectsSortMode)0);
		for (int i = 0; i < array.Length; i++)
		{
			PlayerAvatar val = array[i];
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && (UnityEngine.Object)(object)((Component)val).GetComponent<LocalPlayerNetHandler>() == (UnityEngine.Object)null)
			{
				((Component)val).gameObject.AddComponent<LocalPlayerNetHandler>();
			}
		}
                if (runtimeDebugEnabled)
                {
                        double elapsedMilliseconds = GetElapsedMilliseconds(startTimestamp);
                        if (elapsedMilliseconds >= 1.5)
                        {
                                #region debug-point B:local-ensure-net-handlers-spike
                                DebugReport("B", "LocalPlayerManager.EnsureNetHandlers", "本地网络处理器刷新耗时偏高", "elapsedMs=" + elapsedMilliseconds.ToString("F2") + ",avatarCount=" + array.Length);
                                #endregion
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
			bool shouldLockHealth = ConfigManager.Config.Local.GodMode || _temporaryHealthLock;
			bool flag = (bool)_godModeField.GetValue(playerHealth);
			if (flag != shouldLockHealth)
			{
				_godModeField.SetValue(playerHealth, shouldLockHealth);
			}
		}
	}

	/// <summary>Temporarily preserves local health during the game's native health-sharing interaction.</summary>
	public static void SetTemporaryHealthLock(bool enabled)
	{
		_temporaryHealthLock = enabled;
		if (_godModeField == null)
		{
			_godModeField = typeof(PlayerHealth).GetField("godMode", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		ApplyGodMode();
	}

	private static void ApplyInfiniteStamina()
	{
		if (!((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null) && !(_energyCurrentField == null) && ConfigManager.Config.Local.InfiniteStamina)
		{
			float energyStart = PlayerController.instance.EnergyStart;
			_energyCurrentField.SetValue(PlayerController.instance, energyStart);
		}
	}

	/// <summary>Reasserts zero gravity and a stationary physics state after the game's own player update.</summary>
	public static void EnforceNoClipHover()
	{
		if (!_noClipActive || (UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null)
			return;

		PlayerController.instance.CustomGravity = 0f;
		PlayerController.instance.VelocityRelative = Vector3.zero;
		Rigidbody controllerBody = _playerRigidbody != null ? _playerRigidbody : PlayerController.instance.rb;
		if ((UnityEngine.Object)(object)controllerBody != (UnityEngine.Object)null)
		{
			controllerBody.isKinematic = true;
			controllerBody.useGravity = false;
			controllerBody.velocity = Vector3.zero;
			controllerBody.angularVelocity = Vector3.zero;
		}
		if ((UnityEngine.Object)(object)_playerAvatarRigidbody != (UnityEngine.Object)null)
		{
			_playerAvatarRigidbody.isKinematic = true;
			_playerAvatarRigidbody.useGravity = false;
			_playerAvatarRigidbody.velocity = Vector3.zero;
			_playerAvatarRigidbody.angularVelocity = Vector3.zero;
		}
	}

	/// <summary>Enables collision-free movement and restores original physics when the feature is disabled.</summary>
	private static void ApplyNoClip()
	{
		if ((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null)
		{
			return;
		}
		bool noClip = ConfigManager.Config.Local.NoClip;
		if (noClip && !_noClipActive)
		{
			if (_playerColliders != null)
			{
				foreach (var col in _playerColliders)
				{
					if (col != null) col.enabled = false;
				}
			}
			PlayerAvatar localAvatar = ResolveLocalPlayerAvatar();
			if (localAvatar != null)
			{
				foreach (var col in localAvatar.GetComponentsInChildren<Collider>(true))
				{
					if (col != null) col.enabled = false;
				}
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
				_playerRigidbody.velocity = Vector3.zero;
				_playerRigidbody.angularVelocity = Vector3.zero;
			}
			PlayerAvatar avatarForPhysics = ResolveLocalPlayerAvatar();
			_playerAvatarRigidbody = (UnityEngine.Object)(object)avatarForPhysics != (UnityEngine.Object)null
				? ReflectionUtils.GetFieldValue<Rigidbody>(avatarForPhysics, "rb")
				: null;
			if ((UnityEngine.Object)(object)_playerAvatarRigidbody != (UnityEngine.Object)null
				&& (UnityEngine.Object)(object)_playerAvatarRigidbody != (UnityEngine.Object)(object)_playerRigidbody)
			{
				_originalAvatarRigidbodyUseGravity = _playerAvatarRigidbody.useGravity;
				_originalAvatarRigidbodyKinematic = _playerAvatarRigidbody.isKinematic;
				_originalAvatarRigidbodyDrag = _playerAvatarRigidbody.drag;
				_avatarRigidbodyStateCaptured = true;
			}
			else
			{
				_playerAvatarRigidbody = null;
				_avatarRigidbodyStateCaptured = false;
			}
			_noClipActive = true;
			GameObject controllerObject = ((Component)PlayerController.instance).gameObject;
			_noClipHoverGuard = controllerObject.GetComponent<NoClipHoverGuard>();
			if ((UnityEngine.Object)(object)_noClipHoverGuard == (UnityEngine.Object)null)
				_noClipHoverGuard = controllerObject.AddComponent<NoClipHoverGuard>();
			Console.WriteLine("[LocalPlayer] NoClip ENABLED");
		}
		else if (!noClip && _noClipActive)
		{
			if (_playerColliders != null)
			{
				foreach (var col in _playerColliders)
				{
					if (col != null) col.enabled = true;
				}
			}
			PlayerAvatar localAvatar = ResolveLocalPlayerAvatar();
			if (localAvatar != null)
			{
				foreach (var col in localAvatar.GetComponentsInChildren<Collider>(true))
				{
					if (col != null) col.enabled = true;
				}
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
			if (_avatarRigidbodyStateCaptured && (UnityEngine.Object)(object)_playerAvatarRigidbody != (UnityEngine.Object)null)
			{
				_playerAvatarRigidbody.useGravity = _originalAvatarRigidbodyUseGravity;
				_playerAvatarRigidbody.drag = _originalAvatarRigidbodyDrag;
				_playerAvatarRigidbody.isKinematic = _originalAvatarRigidbodyKinematic;
				_playerAvatarRigidbody.velocity = Vector3.zero;
				_playerAvatarRigidbody.angularVelocity = Vector3.zero;
			}
			_playerAvatarRigidbody = null;
			_avatarRigidbodyStateCaptured = false;
			_noClipActive = false;
			Console.WriteLine("[LocalPlayer] NoClip DISABLED");
		}
		if (!_noClipActive)
		{
			return;
		}

		// Keep both controller and avatar physics pinned while noclip is active, including when no key is held.
		PlayerController.instance.CustomGravity = 0f;
		PlayerController.instance.VelocityRelative = Vector3.zero;

		if ((UnityEngine.Object)(object)_playerRigidbody != (UnityEngine.Object)null)
		{
			_playerRigidbody.isKinematic = true;
			_playerRigidbody.useGravity = false;
			_playerRigidbody.velocity = Vector3.zero;
			_playerRigidbody.angularVelocity = Vector3.zero;
		}
		if ((UnityEngine.Object)(object)_playerAvatarRigidbody != (UnityEngine.Object)null)
		{
			_playerAvatarRigidbody.isKinematic = true;
			_playerAvatarRigidbody.useGravity = false;
			_playerAvatarRigidbody.velocity = Vector3.zero;
			_playerAvatarRigidbody.angularVelocity = Vector3.zero;
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
			Vector3 forward = Vector3.ProjectOnPlane(((Component)main).transform.forward, Vector3.up).normalized;
			Vector3 right = Vector3.ProjectOnPlane(((Component)main).transform.right, Vector3.up).normalized;
			if (Input.GetKey(KeyCode.W))
			{
				val += forward;
			}
			if (Input.GetKey(KeyCode.S))
			{
				val -= forward;
			}
			if (Input.GetKey(KeyCode.A))
			{
				val -= right;
			}
			if (Input.GetKey(KeyCode.D))
			{
				val += right;
			}
			if (Input.GetKey(KeyCode.Space))
			{
				val += Vector3.up;
			}
			if (Input.GetKey(KeyCode.LeftControl))
			{
				val -= Vector3.up;
			}
		}

		if (val.sqrMagnitude > 0.001f)
		{
			Vector3 position = ((Component)PlayerController.instance).transform.position;
			position += val.normalized * num * Time.deltaTime;
			((Component)PlayerController.instance).transform.position = position;
			
			if ((UnityEngine.Object)(object)_playerRigidbody != (UnityEngine.Object)null)
			{
				_playerRigidbody.position = position;
			}

			if (!SemiFunc.IsMasterClientOrSingleplayer())
			{
				PlayerAvatar localAvatar = ResolveLocalPlayerAvatar();
				if ((UnityEngine.Object)(object)localAvatar != (UnityEngine.Object)null)
				{
					Rigidbody avatarRb = ReflectionUtils.GetFieldValue<Rigidbody>(localAvatar, "rb");
					if (avatarRb != null)
					{
						avatarRb.isKinematic = true;
						avatarRb.position = position;
					}
					localAvatar.transform.position = position;
				}
			}
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

	private static void ApplyGlobalItemInvincibility()
	{
		if (ConfigManager.Config.Misc.GlobalItemInvincibility && Time.unscaledTime > _nextItemInvincibilityTime)
		{
			_nextItemInvincibilityTime = Time.unscaledTime + 2.0f;
			foreach (PhysGrabObject item in UnityEngine.Object.FindObjectsByType<PhysGrabObject>(FindObjectsSortMode.None))
			{
				if (item != null)
				{
					item.OverrideIndestructible(3.0f);
					item.OverrideBreakEffects(3.0f);
				}
			}
		}
	}

	public static void UpdateRare()
	{
		if (!_initialized) return;

                bool runtimeDebugEnabled = IsRuntimeDebugEnabled();
                long startTimestamp = runtimeDebugEnabled ? Stopwatch.GetTimestamp() : 0L;
		if (Time.unscaledTime >= _nextNetHandlerRefreshTime)
		{
			_nextNetHandlerRefreshTime = Time.unscaledTime + NetHandlerRefreshIntervalSeconds;
			EnsureNetHandlers();
		}
		ApplyGlobalItemInvincibility();
		ApplyGameModifiers();
                if (runtimeDebugEnabled)
                {
                        double elapsedMilliseconds = GetElapsedMilliseconds(startTimestamp);
                        if (elapsedMilliseconds >= 2.0)
                        {
                                #region debug-point B:local-update-rare-spike
                                DebugReport("B", "LocalPlayerManager.UpdateRare", "本地低频更新耗时偏高", "elapsedMs=" + elapsedMilliseconds.ToString("F2") + ",globalItemInvincible=" + ConfigManager.Config.Misc.GlobalItemInvincibility + ",nextNetRefresh=" + _nextNetHandlerRefreshTime.ToString("F2"));
                                #endregion
                        }
                }
	}

	private static void ApplyGameModifiers()
	{
		ConfigData.GameModifiersSettings settings = ConfigManager.Config.GameModifiers;
		bool isMaster = !GameManager.Multiplayer() || PhotonNetwork.IsMasterClient;

		PlayerAvatar[] playerAvatars = UnityEngine.Object.FindObjectsByType<PlayerAvatar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < playerAvatars.Length; i++)
		{
			PlayerAvatar avatar = playerAvatars[i];
			if (avatar != null && avatar.playerHealth != null && ReflectionUtils.GetFieldValue<bool>(avatar, "isLocal"))
				ReflectionUtils.SetFieldValue(avatar.playerHealth, "godMode", settings.AllTeammatesGodMode || ConfigManager.Config.Local.GodMode);
		}

		if (settings.InfiniteChargingStation || settings.ChargingStationHeal)
		{
			foreach (ChargingStation station in UnityEngine.Object.FindObjectsByType<ChargingStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (station == null)
					continue;

				if (settings.InfiniteChargingStation && isMaster)
				{
					// Current builds store station energy in chargeTotal/chargeFloat;
					// amountCurrent/amountMax belonged to an older game build.
					ReflectionUtils.SetFieldValue(station, "chargeTotal", 100);
					ReflectionUtils.SetFieldValue(station, "chargeFloat", 1f);
				}
				if (settings.ChargingStationHeal && Time.unscaledTime >= _nextStationHealTime)
				{
					Collider[] colliders = station.GetComponentsInChildren<Collider>(true);
					PlayerAvatar[] avatars = UnityEngine.Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None);
					for (int i = 0; i < avatars.Length; i++)
					{
						PlayerAvatar avatar = avatars[i];
						if (avatar == null || ReflectionUtils.GetFieldValue<bool>(avatar, "isDisabled") || avatar.playerHealth == null || ReflectionUtils.GetFieldValue<int>(avatar.playerHealth, "health") >= ReflectionUtils.GetFieldValue<int>(avatar.playerHealth, "maxHealth"))
							continue;
						bool nearStation = false;
						for (int c = 0; c < colliders.Length; c++)
						{
							Collider collider = colliders[c];
							if (collider != null && collider.enabled && Vector3.Distance(collider.bounds.ClosestPoint(avatar.transform.position), avatar.transform.position) <= 2f)
							{
								nearStation = true;
								break;
							}
						}
						if (nearStation)
							avatar.playerHealth.HealOther(2, true);
					}
					_nextStationHealTime = Time.unscaledTime + 1f;
				}
			}
		}

		if (settings.InfiniteWeaponBattery)
		{
			foreach (ItemBattery item in UnityEngine.Object.FindObjectsByType<ItemBattery>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				PhotonView view = ReflectionUtils.GetFieldValue<PhotonView>(item, "photonView");
				bool locallyWritable = !GameManager.Multiplayer() || view == null || view.IsMine;
				if (item != null && locallyWritable && !item.isUnchargable && item.batteryLife < 100f)
				{
					item.SetBatteryLife(100);
				}
			}
		}

		if (settings.SpawnRateMultiplier != 1f && isMaster && EnemyDirector.instance != null)
		{
			int directorId = ((UnityEngine.Object)EnemyDirector.instance).GetInstanceID();
			float current = ReflectionUtils.GetFieldValue<float>(EnemyDirector.instance, "enemyActionAmount");
			if (_lastEnemyDirectorInstanceId != directorId || current < _lastEnemyActionAmount)
			{
				_lastEnemyDirectorInstanceId = directorId;
				_lastEnemyActionAmount = current;
			}
			else
			{
				float delta = current - _lastEnemyActionAmount;
				float multiplier = Mathf.Clamp(settings.SpawnRateMultiplier, 0.1f, 10f);
				ReflectionUtils.SetFieldValue(EnemyDirector.instance, "enemyActionAmount", _lastEnemyActionAmount + delta * multiplier);
				_lastEnemyActionAmount = _lastEnemyActionAmount + delta * multiplier;
			}
		}
		else
		{
			_lastEnemyDirectorInstanceId = 0;
		}

		if (PlayerController.instance != null)
			ReflectionUtils.SetFieldValue(PlayerController.instance, "DebugDisableOvercharge", settings.LimitActionAndOvercharge);
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
			float num = Mathf.Max(Mathf.Clamp(ConfigManager.Config.Local.GrabStrength, 1f, MaxStableGrabStrengthMultiplier), _temporaryGrabStrengthMultiplier);
			if (_originalGrabRange > 0f)
			{
				float grabRange = Mathf.Max(_originalGrabRange * ConfigManager.Config.Local.GrabRange, _temporaryGrabReach);
				PhysGrabber.instance.grabRange = grabRange;
				PhysGrabber.instance.grabReleaseDistance = Mathf.Max(_originalGrabReleaseDistance * ConfigManager.Config.Local.GrabRange, _temporaryGrabReach + 5f);
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

			SyncGrabStrengthMultiplierToHost(num);
		}

		ApplyHeldGunAimPitch();
	}

	public static void SetTemporaryGrabOverrides(float strengthMultiplier, float reach)
	{
		_temporaryGrabStrengthMultiplier = Mathf.Clamp(strengthMultiplier, 1f, MaxStableGrabStrengthMultiplier);
		_temporaryGrabReach = Mathf.Clamp(reach, 0f, 10000f);
		ApplyGrabModifiers();
	}

	private static void SyncGrabStrengthMultiplierToHost(float multiplier)
	{
		if (!GameManager.Multiplayer() || SemiFunc.IsMasterClientOrSingleplayer())
		{
			_lastGrabStrengthNetworkMultiplier = -1f;
			return;
		}

		bool needsRefresh = multiplier > 1.001f;
		bool needsReset = _lastGrabStrengthNetworkMultiplier > 1.001f && multiplier <= 1.001f;
		bool changed = _lastGrabStrengthNetworkMultiplier < 0f
			|| Mathf.Abs(_lastGrabStrengthNetworkMultiplier - multiplier) > 0.001f;
		if ((!needsRefresh && !needsReset) || (!changed && Time.unscaledTime < _nextGrabStrengthNetworkSyncTime))
		{
			return;
		}

		if (Time.unscaledTime < _nextGrabStrengthNetworkSyncTime && !needsReset)
		{
			return;
		}

		LocalPlayerNetHandler handler = GetLocalNetHandler();
		if (handler != null && handler.RequestGrabStrengthMultiplier(multiplier))
		{
			_lastGrabStrengthNetworkMultiplier = multiplier;
			_nextGrabStrengthNetworkSyncTime = Time.unscaledTime + (needsRefresh ? 0.2f : 0.5f);
		}
	}

	/// <summary>
	/// True while the local avatar is crouching. PlayerAvatar.isCrouching is
	/// internal, so it has to be read through reflection.
	/// </summary>
	private static bool IsLocalPlayerCrouching()
	{
		if ((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null
			|| (UnityEngine.Object)(object)PlayerController.instance.playerAvatarScript == (UnityEngine.Object)null)
		{
			return false;
		}

		object crouchObj = ReflectionUtils.GetFieldValue(PlayerController.instance.playerAvatarScript, "isCrouching");
		return crouchObj is bool crouching && crouching;
	}

	private static float GetHeldGunAimVerticalOffset()
	{
		return IsLocalPlayerCrouching() ? CrouchAimVerticalOffset : StandAimVerticalOffset;
	}

	/// <summary>
	/// Applies the lowered muzzle pitch every frame, so it also takes effect while
	/// neither solid aim nor the aimbot is active (the stock ItemGun value is -5).
	/// </summary>
	private static void ApplyHeldGunAimPitch()
	{
		PhysGrabObject heldObject = GetCurrentlyGrabbedObject();
		if ((UnityEngine.Object)(object)heldObject == (UnityEngine.Object)null)
		{
			return;
		}

		ItemGun gun = heldObject.GetComponent<ItemGun>();
		if ((UnityEngine.Object)(object)gun == (UnityEngine.Object)null)
		{
			return;
		}

		gun.aimVerticalOffset = GetHeldGunAimVerticalOffset();
	}

	private static void RefillAllVisibleBatteries()
	{
		foreach (ItemBattery item in UnityEngine.Object.FindObjectsByType<ItemBattery>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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

	private static void UpdateAimbotState()
	{
                bool runtimeDebugEnabled = IsRuntimeDebugEnabled();
                long startTimestamp = runtimeDebugEnabled ? Stopwatch.GetTimestamp() : 0L;
		if (ConfigManager.Config == null || ConfigManager.Config.Aimbot == null || !ConfigManager.Config.Aimbot.Enabled)
		{
			_aimbotTarget = null;
			_aimbotAssistUntilTime = 0f;
			_aimbotPullHoldUntilTime = 0f;
			return;
		}

		if (ConfigManager.IsMenuVisible || !IsAimKeyHeld(ConfigManager.Config.Aimbot.AimKey))
		{
			_aimbotTarget = null;
			_aimbotAssistUntilTime = 0f;
			_aimbotPullHoldUntilTime = 0f;
			return;
		}

		PhysGrabObject grabbedObject = GetCurrentlyGrabbedObject();
		if ((UnityEngine.Object)(object)grabbedObject == (UnityEngine.Object)null)
		{
			_aimbotTarget = null;
			_aimbotAssistUntilTime = 0f;
			_aimbotPullHoldUntilTime = 0f;
			return;
		}

		ItemGun gun = grabbedObject.GetComponent<ItemGun>();
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)gun == (UnityEngine.Object)null || (UnityEngine.Object)(object)main == (UnityEngine.Object)null)
		{
			_aimbotTarget = null;
			_aimbotAssistUntilTime = 0f;
			_aimbotPullHoldUntilTime = 0f;
			return;
		}
		TryGetGunAimPose(gun, out var aimOrigin, out var aimForward);

		Enemy target = null;
		Vector3 targetPoint = Vector3.zero;
		bool refreshedTarget = false;
		if (Time.unscaledTime >= _nextAimbotScanTime || (UnityEngine.Object)(object)_aimbotTarget == (UnityEngine.Object)null)
		{
			_nextAimbotScanTime = Time.unscaledTime + AimbotScanIntervalSeconds;
			if (TryFindBestAimbotTarget(main, gun, out target, out targetPoint))
			{
				refreshedTarget = true;
			}
		}

		if (!refreshedTarget)
		{
			EnemyManager.EnemyData cachedTargetData = EnemyManager.GetEnemyData(_aimbotTarget);
			Rect cachedRect;
			float cachedScore;
			Vector2 screenCenter = Cheat.Utils.MathUtils.GetScreenCenter();
			if (_aimbotTarget != null
				&& _aimbotAssistUntilTime > Time.unscaledTime
				&& TryEvaluateAimbotTarget(main, cachedTargetData, screenCenter, aimOrigin, aimForward, out targetPoint, out cachedRect, out cachedScore))
			{
				target = _aimbotTarget;
			}
			else
			{
				_aimbotTarget = null;
				_aimbotAssistUntilTime = 0f;
				_aimbotPullHoldUntilTime = 0f;
				return;
			}
		}

		_aimbotTarget = target;
		_aimbotTargetPoint = targetPoint;
		_aimbotAssistUntilTime = Time.unscaledTime + AimbotAssistGraceSeconds;
		_aimbotPullHoldUntilTime = Time.unscaledTime + AimbotPullHoldSeconds;

		Quaternion desiredRotation = Quaternion.LookRotation((targetPoint - ((Component)main).transform.position).normalized, Vector3.up);
		float smoothStrength = Mathf.Lerp(28f, 96f, Mathf.Clamp01(ConfigManager.Config.Aimbot.Smoothness));
		float lerpFactor = 1f - Mathf.Exp((0f - smoothStrength) * Time.deltaTime);
		Quaternion blendedRotation = Quaternion.Slerp(((Component)main).transform.rotation, desiredRotation, lerpFactor);
		if (Quaternion.Angle(blendedRotation, desiredRotation) <= 0.35f)
		{
			blendedRotation = desiredRotation;
		}

		if ((UnityEngine.Object)(object)CameraAim.Instance != (UnityEngine.Object)null)
		{
			CameraAim.Instance.SetPlayerAim(blendedRotation, _setRotation: true);
			CameraAim.Instance.OverrideAimSmooth(Mathf.Lerp(28f, 84f, Mathf.Clamp01(ConfigManager.Config.Aimbot.Smoothness)), 0.12f);
			CameraAim.Instance.OverrideNoSmooth(0.12f);
		}

		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
		{
			PlayerController.instance.OverrideTurnRotation(Quaternion.Euler(0f, blendedRotation.eulerAngles.y, 0f), 0.12f);
		}
                if (runtimeDebugEnabled)
                {
                        double elapsedMilliseconds = GetElapsedMilliseconds(startTimestamp);
                        if (elapsedMilliseconds >= 1.5)
                        {
                                #region debug-point B:aimbot-update-spike
                                DebugReport("B", "LocalPlayerManager.UpdateAimbotState", "自瞄更新耗时偏高", "elapsedMs=" + elapsedMilliseconds.ToString("F2") + ",hasTarget=" + (((UnityEngine.Object)(object)_aimbotTarget != (UnityEngine.Object)null) ? "true" : "false") + ",smoothness=" + ConfigManager.Config.Aimbot.Smoothness.ToString("F2") + ",drawFov=" + ConfigManager.Config.Aimbot.DrawFov + ",checkVisibility=" + ConfigManager.Config.Aimbot.CheckVisibility);
                                #endregion
                        }
                }
	}

	private static bool TryFindBestAimbotTarget(Camera mainCamera, ItemGun gun, out Enemy targetEnemy, out Vector3 targetPoint)
	{
		targetEnemy = null;
		targetPoint = Vector3.zero;
		if ((UnityEngine.Object)(object)mainCamera == (UnityEngine.Object)null || (UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null)
		{
			return false;
		}

		TryGetGunAimPose(gun, out var aimOrigin, out var aimForward);
		Vector2 screenCenter = Cheat.Utils.MathUtils.GetScreenCenter();
		float bestScore = float.MaxValue;
		foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
		{
			if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null)
			{
				continue;
			}

			EnemyManager.EnemyData enemyData = EnemyManager.GetEnemyData(item);
			if (enemyData == null || !EnemyManager.IsEnemyActive(enemyData) || (UnityEngine.Object)(object)enemyData.Enemy == (UnityEngine.Object)null)
			{
				continue;
			}
			Rect enemyRect;
			float candidateScore;
			Vector3 candidatePoint;
			if (!TryEvaluateAimbotTarget(mainCamera, enemyData, screenCenter, aimOrigin, aimForward, out candidatePoint, out enemyRect, out candidateScore))
			{
				continue;
			}

			if (!(candidateScore < bestScore))
			{
				continue;
			}

			bestScore = candidateScore;
			targetEnemy = enemyData.Enemy;
			targetPoint = candidatePoint;
		}

		return (UnityEngine.Object)(object)targetEnemy != (UnityEngine.Object)null;
	}

	private static bool TryEvaluateAimbotTarget(Camera mainCamera, EnemyManager.EnemyData enemyData, Vector2 screenCenter, Vector3 aimOrigin, Vector3 aimForward, out Vector3 targetPoint, out Rect enemyRect, out float candidateScore)
	{
		targetPoint = Vector3.zero;
		enemyRect = default(Rect);
		candidateScore = float.MaxValue;
		if (enemyData == null || !EnemyManager.IsEnemyActive(enemyData) || (UnityEngine.Object)(object)enemyData.Enemy == (UnityEngine.Object)null)
		{
			return false;
		}

		Bounds enemyBounds;
		if (!EnemyManager.TryGetAccurateBounds(enemyData, out enemyBounds))
		{
			return false;
		}

		targetPoint = EnemyManager.GetTargetPoint(enemyData, enemyBounds);
		if (!Cheat.Utils.EspProjectionUtils.TryGetScreenRect(mainCamera, enemyBounds, 42f, 90f, out enemyRect))
		{
			return false;
		}

		float referenceFov = GetAimbotReferenceFov();
		Vector3 projectedTargetPoint = Cheat.Utils.MathUtils.WorldToScreen(mainCamera, targetPoint, referenceFov);
		if (projectedTargetPoint.z <= 0.05f)
		{
			return false;
		}

		Vector2 screenPoint = new Vector2(projectedTargetPoint.x, projectedTargetPoint.y);
		float crosshairDistance = Cheat.Utils.MathUtils.GetScreenReferenceDistance(screenPoint, screenCenter);
		if (crosshairDistance > Cheat.Utils.MathUtils.GetScreenReferenceRadius(ConfigManager.Config.Aimbot.FovRadius))
		{
			return false;
		}

		Vector3 toTarget = targetPoint - aimOrigin;
		float targetDistance = toTarget.magnitude;
		if (targetDistance <= 0.05f)
		{
			return false;
		}

		Vector3 targetDirection = toTarget / targetDistance;

		if (ConfigManager.Config.Aimbot.CheckVisibility && !IsAimbotPathClear(enemyData.Enemy, aimOrigin, targetDirection, targetDistance))
		{
			return false;
		}

		candidateScore = crosshairDistance;
		return true;
	}

	private static bool TryGetGunAimPose(ItemGun gun, out Vector3 origin, out Vector3 forward)
	{
		Transform aimTransform = ((UnityEngine.Object)(object)gun != (UnityEngine.Object)null && (UnityEngine.Object)(object)gun.gunMuzzle != (UnityEngine.Object)null)
			? gun.gunMuzzle
			: (((UnityEngine.Object)(object)gun != (UnityEngine.Object)null) ? ((Component)gun).transform : ((UnityEngine.Object)(object)Camera.main != (UnityEngine.Object)null ? ((Component)Camera.main).transform : null));
		if ((UnityEngine.Object)(object)aimTransform == (UnityEngine.Object)null)
		{
			origin = Vector3.zero;
			forward = Vector3.forward;
			return false;
		}

		origin = aimTransform.position;
		forward = aimTransform.forward.sqrMagnitude > 0.001f ? aimTransform.forward.normalized : Vector3.forward;
		return true;
	}

	/// <summary>
	/// Resolves the world-space point represented by the screen-center crosshair at
	/// the selected target's depth. A real scene hit takes precedence; when the
	/// center ray has no hit, the point on the ray at target depth is used.
	/// </summary>
	private static bool TryGetCrosshairAimPoint(Camera camera, Vector3 selectedTargetPoint, out Ray crosshairRay, out Vector3 aimPoint)
	{
		crosshairRay = default(Ray);
		aimPoint = Vector3.zero;
		if ((UnityEngine.Object)(object)camera == (UnityEngine.Object)null)
		{
			return false;
		}

		crosshairRay = camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
		float targetDepth = Vector3.Dot(selectedTargetPoint - crosshairRay.origin, crosshairRay.direction);
		float rayDistance = targetDepth > 0.05f ? targetDepth : 100f;
		RaycastHit[] hits = Physics.RaycastAll(crosshairRay, rayDistance + 0.05f, -1, QueryTriggerInteraction.Ignore);
		Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
		for (int i = 0; i < hits.Length; i++)
		{
			Collider collider = hits[i].collider;
			if ((UnityEngine.Object)(object)collider == (UnityEngine.Object)null || collider.isTrigger || ShouldIgnoreAimbotHit(collider.transform))
			{
				continue;
			}

			aimPoint = hits[i].point;
			return true;
		}

		// No collider intersects the crosshair ray. Preserve its direction and use
		// the selected enemy's depth (or a far point if it is behind the camera).
		aimPoint = crosshairRay.GetPoint(rayDistance);
		return true;
	}

	private static bool IsAimbotPathClear(Enemy targetEnemy, Vector3 origin, Vector3 direction, float distance)
	{
		int mask = (int)SemiFunc.LayerMaskGetVisionObstruct() + LayerMask.GetMask("Enemy");
		RaycastHit[] hits = Physics.SphereCastAll(origin, 0.08f, direction, distance, mask, QueryTriggerInteraction.Ignore);
		Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
		for (int i = 0; i < hits.Length; i++)
		{
			Collider collider = hits[i].collider;
			if ((UnityEngine.Object)(object)collider == (UnityEngine.Object)null || collider.isTrigger)
			{
				continue;
			}

			Transform hitTransform = collider.transform;
			if (IsTransformPartOfEnemy(hitTransform, targetEnemy))
			{
				return true;
			}

			if (ShouldIgnoreAimbotHit(hitTransform))
			{
				continue;
			}

			return false;
		}

		return true;
	}

	private static bool IsTransformPartOfEnemy(Transform hitTransform, Enemy targetEnemy)
	{
		if ((UnityEngine.Object)(object)hitTransform == (UnityEngine.Object)null || (UnityEngine.Object)(object)targetEnemy == (UnityEngine.Object)null)
		{
			return false;
		}

		return hitTransform.IsChildOf(((Component)targetEnemy).transform)
			|| (UnityEngine.Object)(object)hitTransform.GetComponentInParent<Enemy>() == (UnityEngine.Object)(object)targetEnemy;
	}

	private static bool ShouldIgnoreAimbotHit(Transform hitTransform)
	{
		if ((UnityEngine.Object)(object)hitTransform == (UnityEngine.Object)null)
		{
			return true;
		}

		PlayerAvatar localAvatar = ResolveLocalPlayerAvatar();
		if ((UnityEngine.Object)(object)localAvatar != (UnityEngine.Object)null && hitTransform.IsChildOf(((Component)localAvatar).transform))
		{
			return true;
		}

		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null && hitTransform.IsChildOf(((Component)PlayerController.instance).transform))
		{
			return true;
		}

		PhysGrabObject heldObject = GetCurrentlyGrabbedObject();
		if ((UnityEngine.Object)(object)heldObject != (UnityEngine.Object)null && hitTransform.IsChildOf(((Component)heldObject).transform))
		{
			return true;
		}

		return false;
	}

	private static bool IsAimKeyHeld(KeyCode keyCode)
	{
		if (keyCode == KeyCode.None)
		{
			return false;
		}

		try
		{
			return Input.GetKey(keyCode);
		}
		catch
		{
			return false;
		}
	}

	private static void ApplySolidAim(PhysGrabObject obj, bool isFixedUpdate, Vector3? targetPoint)
	{
                long startTimestamp = IsRuntimeDebugEnabled() ? Stopwatch.GetTimestamp() : 0L;
		if ((UnityEngine.Object)(object)obj == (UnityEngine.Object)null || ConfigManager.Config == null || (!ConfigManager.Config.Local.SolidAim && !targetPoint.HasValue))
		{
			return;
		}
		
		ItemGun gun = obj.GetComponent<ItemGun>();
		if ((UnityEngine.Object)(object)gun == (UnityEngine.Object)null)
		{
			return;
		}

		gun.aimVerticalOffset = GetHeldGunAimVerticalOffset();
		
		if (_physGrabObjectOverrideMassMethod == null) _physGrabObjectOverrideMassMethod = typeof(PhysGrabObject).GetMethod("OverrideMass", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (_physGrabObjectOverrideTorqueStrengthMethod == null) _physGrabObjectOverrideTorqueStrengthMethod = typeof(PhysGrabObject).GetMethod("OverrideTorqueStrength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (_physGrabObjectOverrideDragMethod == null) _physGrabObjectOverrideDragMethod = typeof(PhysGrabObject).GetMethod("OverrideDrag", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		float grabStrength = targetPoint.HasValue ? 0.82f : 1.05f;
		float torqueStrength = targetPoint.HasValue ? 4f : 8f;
		float dragStrength = targetPoint.HasValue ? 2.8f : 2f;
		float angularDrag = targetPoint.HasValue ? 30f : 24f;
		float massOverride = targetPoint.HasValue ? 0.22f : 0.25f;

		TryInvoke(_physGrabObjectOverrideMassMethod, obj, massOverride, 0.1f);
		TryInvoke(_physGrabberOverrideGrabStrengthMethod, PhysGrabber.instance, grabStrength, 0.1f);
		ApplyHeldObjectGrabOverrides(obj, grabStrength);
		TryInvoke(_physGrabObjectOverrideTorqueStrengthMethod, obj, torqueStrength, 0.1f);
		TryInvoke(_physGrabObjectOverrideDragMethod, obj, dragStrength, 0.1f);
		obj.OverrideAngularDrag(angularDrag, 0.1f);

		Camera mainCamera = Camera.main;
		if ((UnityEngine.Object)(object)mainCamera == (UnityEngine.Object)null)
		{
			return;
		}

		Quaternion desiredRotation = ((targetPoint.HasValue && (targetPoint.Value - ((Component)mainCamera).transform.position).sqrMagnitude > 0.0001f) ? Quaternion.LookRotation((targetPoint.Value - ((Component)mainCamera).transform.position).normalized, Vector3.up) : ((Component)mainCamera).transform.rotation);
		if (targetPoint.HasValue && (UnityEngine.Object)(object)gun.gunMuzzle != (UnityEngine.Object)null)
		{
			Transform muzzle = gun.gunMuzzle;
			// Aim the barrel at the enemy itself. The previous code aimed the muzzle at
			// the screen-centre hit point instead; while the camera is still travelling
			// towards the target that point sits off to one side, so the gun was dragged
			// sideways (to the right of the target) and never settled on the enemy.
			Vector3 muzzleToTarget = targetPoint.Value - muzzle.position;
			if (muzzleToTarget.sqrMagnitude > 0.0001f)
			{
				Vector3 desiredMuzzleForward = muzzleToTarget.normalized;
				Vector3 actualMuzzleForward = muzzle.forward.sqrMagnitude > 0.0001f ? muzzle.forward.normalized : desiredMuzzleForward;

				// The game fires along gunMuzzle.forward and ItemGun.UpdateMaster adds a
				// fixed barrel elevation on top, so correct the live muzzle direction
				// around the held object's pivot. Building the delta from a LookRotation
				// keeps the roll stable, unlike a FromToRotation multiplied onto rb.rotation.
				Quaternion objectRotation = ((UnityEngine.Object)(object)obj.rb != (UnityEngine.Object)null) ? obj.rb.rotation : obj.transform.rotation;
				Vector3 muzzleLocalForward = Quaternion.Inverse(objectRotation) * actualMuzzleForward;
				Quaternion muzzleLocalRotation = muzzleLocalForward.sqrMagnitude > 0.0001f
					&& Mathf.Abs(Vector3.Dot(muzzleLocalForward.normalized, Vector3.up)) < 0.99f
					? Quaternion.LookRotation(muzzleLocalForward.normalized, Vector3.up)
					: Quaternion.identity;
				desiredRotation = Quaternion.LookRotation(desiredMuzzleForward, Vector3.up) * Quaternion.Inverse(muzzleLocalRotation);

				if (IsRuntimeDebugEnabled())
				{
					UnityEngine.Debug.DrawRay(muzzle.position, desiredMuzzleForward * 20f, Color.red);
					UnityEngine.Debug.DrawRay(muzzle.position, actualMuzzleForward * 20f, Color.yellow);
				}
			}
		}
		ForceHeldObjectLookRotation(obj, desiredRotation);
		
		if (obj.rb != null && isFixedUpdate)
		{
			float rotationSpeed = targetPoint.HasValue ? 12f : 16f;
			obj.rb.rotation = Quaternion.Slerp(obj.rb.rotation, desiredRotation, Time.fixedDeltaTime * rotationSpeed);

			if (!SemiFunc.IsMasterClientOrSingleplayer())
			{
				ReflectionUtils.SetFieldValue(obj, "targetRot", obj.rb.rotation);
				ReflectionUtils.SetFieldValue(obj, "currentRotation", obj.rb.rotation);
				obj.rb.angularVelocity = Vector3.zero;
			}
		}

		bool isResourceEmpty = false;
		object stateObj = ReflectionUtils.GetFieldValue(gun, "stateCurrent");
		if (stateObj != null && (int)stateObj == 1)
		{
			isResourceEmpty = true;
		}
		
		object battery = ReflectionUtils.GetFieldValue(gun, "itemBattery");
		if (battery != null)
		{
			object batteryLifeObj = ReflectionUtils.GetFieldValue(battery, "batteryLife");
			if (batteryLifeObj != null && (float)batteryLifeObj <= 0.1f)
			{
				isResourceEmpty = true;
			}
		}

		if (!isResourceEmpty && targetPoint.HasValue)
		{
			gun.torqueMultiplier = 1f;
		}
                if (IsRuntimeDebugEnabled())
                {
                        double elapsedMilliseconds = GetElapsedMilliseconds(startTimestamp);
                        if (elapsedMilliseconds >= 1.5)
                        {
                                #region debug-point B:solid-aim-spike
                                DebugReport("B", "LocalPlayerManager.ApplySolidAim", "稳枪/自瞄物理覆写耗时偏高", "elapsedMs=" + elapsedMilliseconds.ToString("F2") + ",isFixedUpdate=" + isFixedUpdate + ",hasTargetPoint=" + targetPoint.HasValue + ",isClient=" + (!SemiFunc.IsMasterClientOrSingleplayer()).ToString());
                                #endregion
                        }
                }
	}

	private static void ForceHeldObjectLookRotation(PhysGrabObject obj, Quaternion desiredRotation)
	{
		if ((UnityEngine.Object)(object)obj == (UnityEngine.Object)null)
		{
			return;
		}

		PlayerAvatar localAvatar = ResolveLocalPlayerAvatar();
		Transform fallbackOverride = ((UnityEngine.Object)(object)localAvatar != (UnityEngine.Object)null && (UnityEngine.Object)(object)localAvatar.localCamera != (UnityEngine.Object)null) ? localAvatar.localCamera.GetOverrideTransform() : ((UnityEngine.Object)(object)Camera.main != (UnityEngine.Object)null ? ((Component)Camera.main).transform : null);
		if ((UnityEngine.Object)(object)fallbackOverride == (UnityEngine.Object)null)
		{
			return;
		}

		if ((UnityEngine.Object)(object)PhysGrabber.instance != (UnityEngine.Object)null)
		{
			PhysGrabber.instance.cameraRelativeGrabbedForward = fallbackOverride.InverseTransformDirection(desiredRotation * Vector3.forward).normalized;
			PhysGrabber.instance.cameraRelativeGrabbedUp = fallbackOverride.InverseTransformDirection(desiredRotation * Vector3.up).normalized;
		}

		foreach (PhysGrabber item in obj.playerGrabbing)
		{
			if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null)
			{
				continue;
			}

			Transform overrideTransform = ((UnityEngine.Object)(object)item.playerAvatar != (UnityEngine.Object)null && (UnityEngine.Object)(object)item.playerAvatar.localCamera != (UnityEngine.Object)null) ? item.playerAvatar.localCamera.GetOverrideTransform() : fallbackOverride;
			item.cameraRelativeGrabbedForward = overrideTransform.InverseTransformDirection(desiredRotation * Vector3.forward).normalized;
			item.cameraRelativeGrabbedUp = overrideTransform.InverseTransformDirection(desiredRotation * Vector3.up).normalized;
		}
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
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)CameraZoom.Instance != (UnityEngine.Object)null)
		{
			if (_originalFOV < 0f && CameraZoom.Instance.playerZoomDefault > 0f)
			{
				_originalFOV = CameraZoom.Instance.playerZoomDefault;
			}

			if (ConfigManager.Config.Misc.FOV <= 1f)
			{
				return;
			}

			float fOV = Mathf.Clamp(ConfigManager.Config.Misc.FOV, 5f, 170f);
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

			if (ConfigManager.Config.Misc.FOV <= 1f)
			{
				return;
			}

			float fOV = Mathf.Clamp(ConfigManager.Config.Misc.FOV, 5f, 170f);
			if (Math.Abs(main.fieldOfView - fOV) > 0.1f)
			{
				main.fieldOfView = fOV;
			}
		}
	}

	private static bool _prevThirdPersonEnabled = false;

	private static void ApplyThirdPerson()
	{
		bool enabled = ConfigManager.Config.Local.ThirdPerson;
		if (enabled)
		{
			_prevThirdPersonEnabled = true;
			PlayerAvatar localAvatar = ResolveLocalPlayerAvatar();
			if (localAvatar != null)
			{
				if (localAvatar.localCamera != null && localAvatar.localCamera.transform != null)
				{
					Transform camTransform = localAvatar.localCamera.transform;
					Vector3 localPos = camTransform.localPosition;
					localPos.z = -ConfigManager.Config.Local.ThirdPersonDistance;
					localPos.y = ConfigManager.Config.Local.ThirdPersonHeight;
					localPos.x = ConfigManager.Config.Local.ThirdPersonHorizontal;
					camTransform.localPosition = localPos;
				}
				
				PlayerAvatarVisuals visuals = ReflectionUtils.GetFieldValue<PlayerAvatarVisuals>(localAvatar, "playerAvatarVisuals");
				if (visuals != null)
				{
					float scale = ConfigManager.Config.Local.ThirdPersonModelScale;
					visuals.transform.localScale = new Vector3(scale, scale, scale);
					
					var shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
					foreach (Renderer renderer in visuals.GetComponentsInChildren<Renderer>(true))
					{
						if (renderer != null && renderer.gameObject.layer != LayerMask.NameToLayer("UI"))
						{
							renderer.shadowCastingMode = shadowCastingMode;
							if (renderer.gameObject.layer == LayerMask.NameToLayer("PlayerLocal"))
							{
								renderer.gameObject.layer = LayerMask.NameToLayer("Default");
							}
						}
					}
				}
			}
		}
		else if (_prevThirdPersonEnabled)
		{
			_prevThirdPersonEnabled = false;
			PlayerAvatar localAvatar = ResolveLocalPlayerAvatar();
			if (localAvatar != null)
			{
				if (localAvatar.localCamera != null && localAvatar.localCamera.transform != null)
				{
					localAvatar.localCamera.transform.localPosition = Vector3.zero;
				}
				
				PlayerAvatarVisuals visuals = ReflectionUtils.GetFieldValue<PlayerAvatarVisuals>(localAvatar, "playerAvatarVisuals");
				if (visuals != null)
				{
					visuals.transform.localScale = Vector3.one;
					
					var shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
					foreach (Renderer renderer in visuals.GetComponentsInChildren<Renderer>(true))
					{
						if (renderer != null && renderer.gameObject.layer != LayerMask.NameToLayer("UI"))
						{
							renderer.shadowCastingMode = shadowCastingMode;
							if (renderer.gameObject.layer == LayerMask.NameToLayer("Default"))
							{
								renderer.gameObject.layer = LayerMask.NameToLayer("PlayerLocal");
							}
						}
					}
				}
			}
		}
	}

	private static void ApplyTumbleFly()
	{
		if (!ConfigManager.Config.Local.TumbleFly)
			return;

		PlayerAvatar avatar = ResolveLocalPlayerAvatar();
		if ((UnityEngine.Object)(object)avatar == (UnityEngine.Object)null)
			return;

		PlayerTumble tumble = GetPlayerTumble(avatar);
		if ((UnityEngine.Object)(object)tumble == (UnityEngine.Object)null)
			return;

		bool isTumbling = ReflectionUtils.GetFieldValue<bool>(tumble, "isTumbling");
		if (!isTumbling)
			return;

		Rigidbody rb = ReflectionUtils.GetFieldValue<Rigidbody>(tumble, "rb");
		if ((UnityEngine.Object)(object)rb == (UnityEngine.Object)null)
			return;

		float speed = 12f;
		if (Input.GetKey(KeyCode.LeftShift) || (PlayerController.instance != null && PlayerController.instance.sprinting))
		{
			speed = 28f;
		}

		Vector3 movement = Vector3.zero;
		Camera mainCamera = Camera.main;

		if ((UnityEngine.Object)(object)mainCamera != (UnityEngine.Object)null)
		{
			Vector3 forward = mainCamera.transform.forward;
			Vector3 right = mainCamera.transform.right;
			
			if (Input.GetKey(KeyCode.W)) movement += forward;
			if (Input.GetKey(KeyCode.S)) movement -= forward;
			if (Input.GetKey(KeyCode.A)) movement -= right;
			if (Input.GetKey(KeyCode.D)) movement += right;
		}
		
		if (Input.GetKey(KeyCode.Space)) movement += Vector3.up;
		if (Input.GetKey(KeyCode.LeftControl)) movement -= Vector3.up;

		if (movement.sqrMagnitude > 0.001f)
		{
			rb.velocity = movement.normalized * speed;
		}
	}

	public static bool TeleportLocalPlayer(Vector3 destination, Quaternion rotation, string successMessage)
	{
		PlayerAvatar localPlayerAvatar = ResolveLocalPlayerAvatar();
		if ((UnityEngine.Object)(object)localPlayerAvatar == (UnityEngine.Object)null)
		{
			LastActionStatus = "你当前不可用，无法执行传送。";
			return false;
		}

		destination = ResolvePlayerSafeDestination(destination);
		ResetLocalPlayerVelocity();
		#region debug-point B:teleport-local-player
		DebugReport("B", "LocalPlayerManager.TeleportLocalPlayer", "开始执行玩家传送", "singleOrMaster=" + SemiFunc.IsMasterClientOrSingleplayer() + ",avatar=" + DescribePlayerAvatar(localPlayerAvatar) + ",destination=" + destination + ",rotationY=" + rotation.eulerAngles.y + ",avatarPosBefore=" + GetTransformPosition(localPlayerAvatar) + ",controllerPosBefore=" + GetTransformPosition(PlayerController.instance) + ",rbPosBefore=" + GetRigidbodyPosition(PlayerController.instance));
		#endregion
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

		// The local player's PhysGrabObject.Teleport path is owner-validated by the
		// game and relays the position to the master. Keep local prediction for
		// responsiveness; the master then publishes the authoritative transform.
		if (NativeTeleportModule.TryTeleportLocalPlayer(localPlayerAvatar, destination, rotation))
		{
			LastActionStatus = NativeTeleportModule.LastStatus;
			return true;
		}

		// Compatibility fallback for game versions without the player-body native path.
		PlayerController.instance.transform.position = destination;
		PlayerController.instance.transform.rotation = rotation;
		if ((UnityEngine.Object)(object)PlayerController.instance.rb != (UnityEngine.Object)null)
		{
			PlayerController.instance.rb.position = destination;
			PlayerController.instance.rb.rotation = rotation;
			PlayerController.instance.rb.velocity = Vector3.zero;
			PlayerController.instance.rb.angularVelocity = Vector3.zero;
		}

		if ((UnityEngine.Object)(object)localPlayerAvatar != (UnityEngine.Object)null)
		{
			localPlayerAvatar.transform.position = destination;
			localPlayerAvatar.transform.rotation = rotation;
		}

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
		if (!NativeTeleportModule.TryBeginAction(TeleportAction.EnemyToPlayer))
		{
			LastActionStatus = NativeTeleportModule.LastStatus;
			return false;
		}

		Transform playerTransform = ((Component)PlayerController.instance).transform;
		if (!NativeTeleportModule.IsWithinDistance(playerTransform.position, enemy.transform.position))
		{
			LastActionStatus = string.Format("{0} 超出设置的传送距离。", enemyName);
			return false;
		}

		Vector3 destination = ResolveEnemyPullDestination(ResolveLocalPlayerAvatar(), playerTransform);
		if (!NativeTeleportModule.TryTeleportEnemyLocally(enemy, destination))
		{
			LastActionStatus = NativeTeleportModule.LastStatus;
			return false;
		}

		LastActionStatus = NativeTeleportModule.LastStatus;
		return true;
	}

	private static System.Collections.IEnumerator RestoreKinematicRoutine(Rigidbody rb, float delay)
	{
		yield return new WaitForSeconds(delay);
		if (rb != null)
		{
			rb.isKinematic = false;
		}
	}

	/// <summary>Requests the master client to spawn a catalogued item at the supplied position.</summary>
	public static bool RequestSpawnItem(string resourcePath, Vector3 position, Quaternion rotation)
	{
		if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient || string.IsNullOrWhiteSpace(resourcePath) || !IsFinite(position))
		{
			return false;
		}

		LocalPlayerNetHandler localNetHandler = GetLocalNetHandler();
		return (UnityEngine.Object)(object)localNetHandler != (UnityEngine.Object)null
			&& localNetHandler.RequestSpawnItem(resourcePath, position, rotation);
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

		if (!SemiFunc.IsMasterClientOrSingleplayer() && (UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
		{
			if (GameDirector.instance != null && GameDirector.instance.currentState == GameDirector.gameState.Main)
			{
				PlayerAvatar avatar = ResolveLocalPlayerAvatar();
				PlayerTumble tumble = GetPlayerTumble(avatar);
				if ((UnityEngine.Object)(object)tumble != (UnityEngine.Object)null && ReflectionUtils.GetFieldValue<bool>(tumble, "isTumbling"))
				{
					// 如果玩家已经在翻滚状态，先强行取消翻滚再改坐标，否则触发不了机制
					tumble.TumbleSet(false, false);
				}

				Vector3 voidPos = PlayerController.instance.transform.position;
				voidPos.y = -150f;
				
				PlayerController.instance.transform.position = voidPos;
				if ((UnityEngine.Object)(object)PlayerController.instance.rb != (UnityEngine.Object)null)
				{
					PlayerController.instance.rb.position = voidPos;
					PlayerController.instance.rb.velocity = Vector3.zero;
				}
				
				if ((UnityEngine.Object)(object)avatar != (UnityEngine.Object)null)
				{
					avatar.transform.position = voidPos;
				}

				_homeTeleportNoTumbleTimer = 1.5f;

				LastActionStatus = "检测到异常坠落，正在返回安全位置……";
				return true;
			}
			else
			{
				LastActionStatus = "当前场景无法自动返回安全位置。";
				return false;
			}
		}

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
		#region debug-point A:get-local-net-handler
		DebugReport("A", "LocalPlayerManager.GetLocalNetHandler", "尝试获取本地网络处理器", "avatar=" + DescribePlayerAvatar(val) + ",controllerAvatar=" + DescribePlayerAvatar((PlayerController.instance != null) ? PlayerController.instance.playerAvatarScript : null));
		#endregion
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
		long nowTicks = DateTime.UtcNow.Ticks;
		long lastTicks = Interlocked.Read(ref _lastDebugReportTicks);
		if (lastTicks != 0L && new TimeSpan(nowTicks - lastTicks).TotalSeconds < DebugReportMinimumIntervalSeconds)
		{
			return;
		}
		if (Interlocked.CompareExchange(ref _debugReportInFlight, 1, 0) != 0)
		{
			return;
		}
		Interlocked.Exchange(ref _lastDebugReportTicks, nowTicks);
		ThreadPool.QueueUserWorkItem(delegate
		{
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
			finally
			{
				Interlocked.Exchange(ref _debugReportInFlight, 0);
			}
		});
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
		string debugEnvPath = DebugEnvPath;
		if (!File.Exists(debugEnvPath) && File.Exists(DebugEnvPathFallback))
		{
			debugEnvPath = DebugEnvPathFallback;
		}
		if (!File.Exists(debugEnvPath))
		{
			return;
		}

		try
		{
			string[] array = File.ReadAllLines(debugEnvPath);
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
		if ((UnityEngine.Object)(object)_cachedLocalAvatar != (UnityEngine.Object)null && Time.unscaledTime - _cachedLocalAvatarTime < 1f)
		{
			if (IsOwnedLocalAvatar(_cachedLocalAvatar))
			{
				return _cachedLocalAvatar;
			}
		}

		PlayerAvatar controllerAvatar = ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null) ? PlayerController.instance.playerAvatarScript : null;
		if (IsOwnedLocalAvatar(controllerAvatar))
		{
			_cachedLocalAvatar = controllerAvatar;
			_cachedLocalAvatarTime = Time.unscaledTime;
			return controllerAvatar;
		}

		if (IsOwnedLocalAvatar(PlayerAvatar.instance))
		{
			_cachedLocalAvatar = PlayerAvatar.instance;
			_cachedLocalAvatarTime = Time.unscaledTime;
			return PlayerAvatar.instance;
		}

		PlayerAvatar[] array = UnityEngine.Object.FindObjectsByType<PlayerAvatar>((FindObjectsSortMode)0);
		for (int i = 0; i < array.Length; i++)
		{
			PlayerAvatar val = array[i];
			if (IsOwnedLocalAvatar(val))
			{
				_cachedLocalAvatar = val;
				_cachedLocalAvatarTime = Time.unscaledTime;
				return val;
			}
		}

		if (!GameManager.Multiplayer())
		{
			if ((UnityEngine.Object)(object)controllerAvatar != (UnityEngine.Object)null)
			{
				_cachedLocalAvatar = controllerAvatar;
				_cachedLocalAvatarTime = Time.unscaledTime;
				return controllerAvatar;
			}

			if ((UnityEngine.Object)(object)PlayerAvatar.instance != (UnityEngine.Object)null)
			{
				_cachedLocalAvatar = PlayerAvatar.instance;
				_cachedLocalAvatarTime = Time.unscaledTime;
				return PlayerAvatar.instance;
			}
		}

		return null;
	}

	private static bool IsOwnedLocalAvatar(PlayerAvatar avatar)
	{
		if ((UnityEngine.Object)(object)avatar == (UnityEngine.Object)null)
		{
			return false;
		}

		if (!GameManager.Multiplayer())
		{
			return true;
		}

		if ((UnityEngine.Object)(object)avatar.photonView != (UnityEngine.Object)null && avatar.photonView.IsMine)
		{
			return true;
		}

		return ReflectionUtils.GetFieldValue<bool>(avatar, "isLocal");
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

	public static void TeleportPlayerByMaster(PlayerAvatar avatar, Vector3 destination, Quaternion rotation, string source)
	{
		ExecuteAuthoritativePlayerTeleport(avatar, destination, rotation, source);
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

	public static void ApplySkillModifications()
	{
		PlayerAvatar localAvatar = ResolveLocalPlayerAvatar();
		if (localAvatar == null || StatsManager.instance == null || PunManager.instance == null)
		{
			Console.WriteLine("[Skill] Failed to apply skills: Local avatar, StatsManager, or PunManager not found.");
			return;
		}

		string steamID = ReflectionUtils.GetFieldValue<string>(localAvatar, "steamID");
		if (string.IsNullOrEmpty(steamID))
		{
			Console.WriteLine("[Skill] Failed to apply skills: Local steamID is empty.");
			return;
		}

		ConfigData.SkillSettings s = ConfigManager.Config.Skills;
		ApplySkillDelta(steamID, "Stamina", s.Stamina, StatsManager.instance.playerUpgradeStamina);
		ApplySkillDelta(steamID, "Health", s.Health, StatsManager.instance.playerUpgradeHealth);
		ApplySkillDelta(steamID, "TumbleWings", s.TumbleWings, StatsManager.instance.playerUpgradeTumbleWings);
		ApplySkillDelta(steamID, "CrouchRest", s.CrouchRest, StatsManager.instance.playerUpgradeCrouchRest);
		ApplySkillDelta(steamID, "DeathHeadBattery", s.DeathHeadBattery, StatsManager.instance.playerUpgradeDeathHeadBattery);
		ApplySkillDelta(steamID, "Launch", s.Launch, StatsManager.instance.playerUpgradeLaunch);
		ApplySkillDelta(steamID, "MapPlayerCount", s.MapPlayerCount, StatsManager.instance.playerUpgradeMapPlayerCount);
		ApplySkillDelta(steamID, "Throw", s.Throw, StatsManager.instance.playerUpgradeThrow);
		ApplySkillDelta(steamID, "TumbleClimb", s.TumbleClimb, StatsManager.instance.playerUpgradeTumbleClimb);

		Console.WriteLine("[Skill] Applied skill modifications.");
	}

	private static void ApplySkillDelta(string steamID, string type, int targetLevel, Dictionary<string, int> statsDict)
	{
		int currentLevel = statsDict != null && statsDict.ContainsKey(steamID) ? statsDict[steamID] : 0;
		int delta = targetLevel - currentLevel;
		if (delta == 0) return;

		switch (type)
		{
			case "Stamina": PunManager.instance.UpgradePlayerEnergy(steamID, delta); break;
			case "Health": PunManager.instance.UpgradePlayerHealth(steamID, delta); break;
			case "TumbleWings": PunManager.instance.UpgradePlayerTumbleWings(steamID, delta); break;
			case "CrouchRest": PunManager.instance.UpgradePlayerCrouchRest(steamID, delta); break;
			case "DeathHeadBattery": PunManager.instance.UpgradeDeathHeadBattery(steamID, delta); break;
			case "Launch": PunManager.instance.UpgradePlayerTumbleLaunch(steamID, delta); break;
			case "MapPlayerCount": PunManager.instance.UpgradeMapPlayerCount(steamID, delta); break;
			case "Throw": PunManager.instance.UpgradePlayerThrowStrength(steamID, delta); break;
			case "TumbleClimb": PunManager.instance.UpgradePlayerTumbleClimb(steamID, delta); break;
		}
	}

	public static void Cleanup()
	{
		_temporaryHealthLock = false;
		_temporaryGrabStrengthMultiplier = 1f;
		_temporaryGrabReach = 0f;
		if (GameManager.Multiplayer() && !SemiFunc.IsMasterClientOrSingleplayer()
			&& _lastGrabStrengthNetworkMultiplier > 1.001f)
		{
			GetLocalNetHandler()?.RequestGrabStrengthMultiplier(1f);
			_lastGrabStrengthNetworkMultiplier = 1f;
		}

		if (_noClipActive)
		{
			ConfigManager.Config.Local.NoClip = false;
			ApplyNoClip();
		}
		if (_prevThirdPersonEnabled)
		{
			ConfigManager.Config.Local.ThirdPerson = false;
			ApplyThirdPerson();
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
