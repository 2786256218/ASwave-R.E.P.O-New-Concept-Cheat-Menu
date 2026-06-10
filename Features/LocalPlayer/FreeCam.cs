using System.Reflection;
using Cheat.Config;
using UnityEngine;

namespace Cheat.Features.LocalPlayer;

public static class FreeCam
{
	private static Vector3 _position;

	private static Vector2 _rotation;

	private static GameObject _localPlayerMeshParent;

	private static Camera _mainCamera;

	private static Vector3 _originalLocalPos;

	private static Quaternion _originalLocalRot;

	private static Transform _originalParent;

	private static bool _originalAnimatorState = true;

	public static bool Enabled { get; private set; }

	public static void Update()
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		if (!Enabled || (UnityEngine.Object)(object)_mainCamera == (UnityEngine.Object)null)
		{
			return;
		}
		if (Input.GetKeyDown((KeyCode)27))
		{
			Toggle();
			return;
		}
		float num = Input.GetAxis("Mouse X") * ConfigManager.Config.Local.FreeCamSensitivity;
		float num2 = Input.GetAxis("Mouse Y") * ConfigManager.Config.Local.FreeCamSensitivity;
		_rotation.x += num;
		_rotation.y -= num2;
		_rotation.y = Mathf.Clamp(_rotation.y, -89f, 89f);
		Quaternion val = Quaternion.Euler(_rotation.y, _rotation.x, 0f);
		float num3 = ConfigManager.Config.Local.FreeCamSpeed;
		if (Input.GetKey((KeyCode)304))
		{
			num3 *= ConfigManager.Config.Local.FreeCamFastMultiplier;
		}
		Vector3 val2 = Vector3.zero;
		if (Input.GetKey((KeyCode)119))
		{
			val2 += Vector3.forward;
		}
		if (Input.GetKey((KeyCode)115))
		{
			val2 += Vector3.back;
		}
		if (Input.GetKey((KeyCode)97))
		{
			val2 += Vector3.left;
		}
		if (Input.GetKey((KeyCode)100))
		{
			val2 += Vector3.right;
		}
		if (Input.GetKey((KeyCode)32))
		{
			val2 += Vector3.up;
		}
		if (Input.GetKey((KeyCode)306))
		{
			val2 += Vector3.down;
		}
		val2 = val * val2;
		_position += val2 * num3 * Time.unscaledDeltaTime;
		((Component)_mainCamera).transform.position = _position;
		((Component)_mainCamera).transform.rotation = val;
		Cursor.lockState = (CursorLockMode)1;
		Cursor.visible = false;
	}

	public static void Toggle()
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Expected O, but got Unknown
		Enabled = !Enabled;
		if ((UnityEngine.Object)(object)_mainCamera == (UnityEngine.Object)null)
		{
			_mainCamera = Camera.main;
		}
		if ((UnityEngine.Object)(object)_mainCamera == (UnityEngine.Object)null)
		{
			Enabled = false;
			return;
		}
		if (Enabled)
		{
			_originalParent = ((Component)_mainCamera).transform.parent;
			_originalLocalPos = ((Component)_mainCamera).transform.localPosition;
			_originalLocalRot = ((Component)_mainCamera).transform.localRotation;
			_position = ((Component)_mainCamera).transform.position;
			_rotation = new Vector2(((Component)_mainCamera).transform.eulerAngles.y, ((Component)_mainCamera).transform.eulerAngles.x);
			((Component)_mainCamera).transform.SetParent((Transform)null);
			try
			{
				if ((UnityEngine.Object)(object)PlayerAvatar.instance != (UnityEngine.Object)null)
				{
					PlayerAvatarVisuals componentInChildren = ((Component)PlayerAvatar.instance).GetComponentInChildren<PlayerAvatarVisuals>();
					if ((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null)
					{
						FieldInfo field = typeof(PlayerAvatarVisuals).GetField("meshParent", BindingFlags.Instance | BindingFlags.Public);
						if (field != null)
						{
							_localPlayerMeshParent = (GameObject)field.GetValue(componentInChildren);
							if ((UnityEngine.Object)(object)_localPlayerMeshParent != (UnityEngine.Object)null)
							{
								_localPlayerMeshParent.SetActive(true);
							}
						}
						Animator component = ((Component)componentInChildren).GetComponent<Animator>();
						if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
						{
							_originalAnimatorState = ((Behaviour)component).enabled;
							((Behaviour)component).enabled = true;
						}
					}
				}
			}
			catch
			{
			}
			Cursor.lockState = (CursorLockMode)1;
			Cursor.visible = false;
			return;
		}
		if ((UnityEngine.Object)(object)PlayerAvatar.instance != (UnityEngine.Object)null)
		{
			PlayerAvatarVisuals componentInChildren2 = ((Component)PlayerAvatar.instance).GetComponentInChildren<PlayerAvatarVisuals>();
			if ((UnityEngine.Object)(object)componentInChildren2 != (UnityEngine.Object)null)
			{
				Animator component2 = ((Component)componentInChildren2).GetComponent<Animator>();
				if ((UnityEngine.Object)(object)component2 != (UnityEngine.Object)null)
				{
					((Behaviour)component2).enabled = _originalAnimatorState;
				}
			}
		}
		if ((UnityEngine.Object)(object)_localPlayerMeshParent != (UnityEngine.Object)null)
		{
			_localPlayerMeshParent.SetActive(false);
			_localPlayerMeshParent = null;
		}
		if ((UnityEngine.Object)(object)_originalParent != (UnityEngine.Object)null)
		{
			((Component)_mainCamera).transform.SetParent(_originalParent);
			((Component)_mainCamera).transform.localPosition = _originalLocalPos;
			((Component)_mainCamera).transform.localRotation = _originalLocalRot;
		}
	}

	public static void DrawOverlay()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		if (Enabled)
		{
			float num = 350f;
			float num2 = 120f;
			float num3 = ((float)Screen.width - num) / 2f;
			float num4 = 10f;
			GUI.color = new Color(0f, 0f, 0f, 0.8f);
			GUI.Box(new Rect(num3, num4, num, num2), "");
			GUI.color = Color.white;
			GUI.Label(new Rect(num3, num4 + 5f, num, 25f), "FREECAM ACTIVE");
			GUI.Label(new Rect(num3, num4 + 30f, num, 20f), "Your player should still be visible to others!");
			GUI.Label(new Rect(num3, num4 + 55f, num, 18f), "WASD - Move | Mouse - Look | Shift - Fast");
			GUI.Label(new Rect(num3, num4 + 73f, num, 18f), "Space/Ctrl - Up/Down");
			GUI.Label(new Rect(num3, num4 + 91f, num, 18f), "ESC - Exit FreeCam");
		}
	}
}
