using System;
using System.Reflection;
using Cheat.Config;
using UnityEngine;

namespace Cheat.Features.LocalPlayer;

public static class LocalPlayerManager
{
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

	private static FieldInfo _debugInfiniteBatteryField;

	private static float _nextBatteryRefreshTime;

	public static PlayerController LocalPlayer => PlayerController.instance;

	public static void Update()
	{
		if (!((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null))
		{
			if (!_initialized)
			{
				Initialize();
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
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
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
				_playerRigidbody.useGravity = false;
				_playerRigidbody.drag = 5f;
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
			}
			_noClipActive = false;
			Console.WriteLine("[LocalPlayer] NoClip DISABLED");
		}
		if (!_noClipActive)
		{
			return;
		}

		PlayerController.instance.CustomGravity = 0f;
		PlayerController.instance.Velocity = new Vector3(PlayerController.instance.Velocity.x, 0f, PlayerController.instance.Velocity.z);
		
		if ((UnityEngine.Object)(object)_playerRigidbody != (UnityEngine.Object)null)
		{
			_playerRigidbody.velocity = new Vector3(_playerRigidbody.velocity.x, 0f, _playerRigidbody.velocity.z);
		}
		float num = ConfigManager.Config.Local.NoClipSpeed;
		if (Input.GetKey((KeyCode)304))
		{
			num *= 2f;
		}
		Vector3 val = Vector3.zero;
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)main != (UnityEngine.Object)null)
		{
			if (Input.GetKey((KeyCode)119))
			{
				val += ((Component)main).transform.forward;
			}
			if (Input.GetKey((KeyCode)115))
			{
				val -= ((Component)main).transform.forward;
			}
			if (Input.GetKey((KeyCode)97))
			{
				val -= ((Component)main).transform.right;
			}
			if (Input.GetKey((KeyCode)100))
			{
				val += ((Component)main).transform.right;
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
			((Component)PlayerController.instance).transform.position += val.normalized * num * Time.unscaledDeltaTime;
		}
		
		if ((UnityEngine.Object)(object)_playerRigidbody != (UnityEngine.Object)null)
		{
			_playerRigidbody.velocity = Vector3.zero;
		}
	}

	private static void ApplyNoRagdoll()
	{
		if ((UnityEngine.Object)(object)PlayerAvatar.instance == (UnityEngine.Object)null || !ConfigManager.Config.Local.NoRagdoll || _tumbleField == null)
		{
			return;
		}
		try
		{
			object tumbleObj = _tumbleField.GetValue(PlayerAvatar.instance);
			if (tumbleObj == null) return;
			
			FieldInfo field = tumbleObj.GetType().GetField("isTumbling", BindingFlags.Instance | BindingFlags.Public);
			if (field != null && (bool)field.GetValue(tumbleObj))
			{
				MethodInfo method = tumbleObj.GetType().GetMethod("TumbleEnd", BindingFlags.Instance | BindingFlags.Public);
				if (method != null)
				{
					method.Invoke(tumbleObj, null);
				}
			}
		}
		catch
		{
		}
	}

	private static void ApplySpeedModifiers()
	{
		if ((UnityEngine.Object)(object)PlayerController.instance == (UnityEngine.Object)null)
		{
			return;
		}
		if (FreeCam.Enabled)
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
		_initialized = false;
		_nextBatteryRefreshTime = 0f;
		Console.WriteLine("[LocalPlayer] Cleaned up");
	}
}
