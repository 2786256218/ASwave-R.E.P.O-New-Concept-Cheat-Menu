using System.Collections.Generic;
using System.Reflection;
using Cheat.Config;
using Cheat.UI;
using Cheat.Utils;
using UnityEngine;

namespace Cheat.Features.LocalPlayer;

public class PlayerEsp
{
	private static FieldInfo _isLocalField;

	private static FieldInfo _isDisabledField;

	private static FieldInfo _playerNameField;

	private static FieldInfo _healthCurrentField;

	private static FieldInfo _maxHealthField;

	private static FieldInfo _isTumblingField;

	private static FieldInfo _isCrouchingField;

	private static FieldInfo _isCrawlingField;

	private static FieldInfo _isSlidingField;

	private static FieldInfo _physGrabberGrabbedField;

	private static bool _fieldsInitialized = false;

	private static Dictionary<int, Material[]> _originalMaterials = new Dictionary<int, Material[]>();

	private static Dictionary<int, float> _chamsLastUpdateTime = new Dictionary<int, float>();

	// Keep only a short, screen-space presentation history. Bounds continue to
	// come from the interpolated visual model; this rejects one-frame network
	// spikes without letting a stale world transform drag the box across screen.
	private sealed class PlayerBoxState
	{
		public Rect rect;
		public float updatedAt;
	}

	private static readonly Dictionary<int, PlayerBoxState> _playerBoxStates = new Dictionary<int, PlayerBoxState>();

	private static Material _chamsMaterial;

	private static readonly Vector3 PlayerFallbackSize = new Vector3(0.8f, 1.8f, 0.8f);

	private static readonly Vector3 DeathHeadFallbackSize = new Vector3(0.65f, 0.65f, 0.65f);

	private static bool ShouldIgnoreRenderer(Renderer renderer)
	{
		if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null || renderer is LineRenderer || renderer is TrailRenderer || renderer is ParticleSystemRenderer)
		{
			return true;
		}
		GameObject gameObject = ((Component)renderer).gameObject;
		if ((UnityEngine.Object)(object)gameObject == (UnityEngine.Object)null)
		{
			return true;
		}
		string name = ((UnityEngine.Object)gameObject).name;
		if (!string.IsNullOrEmpty(name) && name.StartsWith("Cheat_", System.StringComparison.Ordinal))
		{
			return true;
		}
		return (UnityEngine.Object)(object)((Component)renderer).GetComponentInParent<Cheat.Features.Visuals.LaserBeamController>() != (UnityEngine.Object)null;
	}

	private static void InitializeFields()
	{
		if (!_fieldsInitialized)
		{
			_isLocalField = typeof(PlayerAvatar).GetField("isLocal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_isDisabledField = typeof(PlayerAvatar).GetField("isDisabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_playerNameField = typeof(PlayerAvatar).GetField("playerName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_isTumblingField = typeof(PlayerAvatar).GetField("isTumbling", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_isCrouchingField = typeof(PlayerAvatar).GetField("isCrouching", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_isCrawlingField = typeof(PlayerAvatar).GetField("isCrawling", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_isSlidingField = typeof(PlayerAvatar).GetField("isSliding", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_healthCurrentField = typeof(PlayerHealth).GetField("health", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_maxHealthField = typeof(PlayerHealth).GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_physGrabberGrabbedField = typeof(PhysGrabber).GetField("grabbed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_fieldsInitialized = true;
		}
	}

	private static void ApplyChams(PlayerAvatar player, Color color)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null)
		{
			return;
		}
		int instanceID = ((UnityEngine.Object)player).GetInstanceID();
		Renderer[] componentsInChildren = ((Component)player).GetComponentsInChildren<Renderer>();
		if ((UnityEngine.Object)(object)_chamsMaterial == (UnityEngine.Object)null)
		{
			_chamsMaterial = ShaderUtils.CreateChamsMaterial(color);
		}
		else if (_chamsMaterial.color != color)
		{
			_chamsMaterial.color = color;
		}
		if ((UnityEngine.Object)(object)_chamsMaterial == (UnityEngine.Object)null)
		{
			return;
		}
		Renderer[] array = componentsInChildren;
		foreach (Renderer val in array)
		{
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				continue;
			}
			int instanceID2 = ((UnityEngine.Object)val).GetInstanceID();
			if (!_originalMaterials.ContainsKey(instanceID2))
			{
				_originalMaterials[instanceID2] = val.sharedMaterials;
			}
			if (val.sharedMaterials.Length != 0 && (UnityEngine.Object)(object)val.sharedMaterials[0] != (UnityEngine.Object)(object)_chamsMaterial)
			{
				Material[] array2 = (Material[])(object)new Material[val.sharedMaterials.Length];
				for (int j = 0; j < array2.Length; j++)
				{
					array2[j] = _chamsMaterial;
				}
				val.sharedMaterials = array2;
			}
		}
		_chamsLastUpdateTime[instanceID] = Time.time;
	}

	private static void RemoveChams(PlayerAvatar player)
	{
		if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null)
		{
			return;
		}
		int instanceID = ((UnityEngine.Object)player).GetInstanceID();
		if (!_chamsLastUpdateTime.ContainsKey(instanceID))
		{
			return;
		}
		Renderer[] componentsInChildren = ((Component)player).GetComponentsInChildren<Renderer>();
		Renderer[] array = componentsInChildren;
		foreach (Renderer val in array)
		{
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null))
			{
				int instanceID2 = ((UnityEngine.Object)val).GetInstanceID();
				if (_originalMaterials.ContainsKey(instanceID2))
				{
					val.sharedMaterials = _originalMaterials[instanceID2];
					_originalMaterials.Remove(instanceID2);
				}
			}
		}
		_chamsLastUpdateTime.Remove(instanceID);
	}

	public static void Draw()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		//IL_0496: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0595: Unknown result type (might be due to invalid IL or missing references)
		//IL_059a: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0854: Unknown result type (might be due to invalid IL or missing references)
		//IL_0859: Unknown result type (might be due to invalid IL or missing references)
		//IL_0864: Unknown result type (might be due to invalid IL or missing references)
		//IL_087c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0890: Unknown result type (might be due to invalid IL or missing references)
		//IL_0897: Unknown result type (might be due to invalid IL or missing references)
		//IL_089e: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0746: Unknown result type (might be due to invalid IL or missing references)
		//IL_0748: Unknown result type (might be due to invalid IL or missing references)
		//IL_074a: Unknown result type (might be due to invalid IL or missing references)
		//IL_074f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0765: Unknown result type (might be due to invalid IL or missing references)
		//IL_0775: Unknown result type (might be due to invalid IL or missing references)
		//IL_078e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0790: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09df: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ed: Unknown result type (might be due to invalid IL or missing references)
		if (!ConfigManager.Config.PlayerEsp.Enabled || (UnityEngine.Object)(object)GameDirector.instance == (UnityEngine.Object)null)
		{
			return;
		}
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)main == (UnityEngine.Object)null || (int)Event.current.type != 7)
		{
			return;
		}
		InitializeFields();
		Rect val3 = default(Rect);
		Color color3 = default(Color);
		Vector2 start = default(Vector2);
		Rect rect = default(Rect);
		foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
		{
			if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null || IsLocalPlayer(player) || IsPlayerDisabled(player))
			{
				continue;
			}
			Transform visualRoot = GetPlayerVisualRoot(player);
			Vector3 position = visualRoot != null ? visualRoot.position : ((Component)player).transform.position;
			float num3 = Vector3.Distance(((Component)main).transform.position, position);
			Renderer[] componentsInChildren = GetPlayerRenderers(player);
			if (!TryGetStablePlayerRect(main, player, componentsInChildren, position + Vector3.up, PlayerFallbackSize, 4f, 8f, out val3))
			{
				continue;
			}
			float width = val3.width;
			float height = val3.height;
			Color color = ConfigManager.Config.PlayerEsp.Color;
			Render.DrawEspBox(val3, color, 1.6f, ConfigManager.Config.Enemies.RenderMethod);

			if (ConfigManager.Config.PlayerEsp.DrawLine)
			{
				Vector2 bottomCenter = new Vector2(val3.x + width / 2f, val3.y + height);
				Vector2 screenBottom = new Vector2((float)Screen.width / 2f, Screen.height);
				Render.DrawLine(screenBottom, bottomCenter, color, 1.5f);
			}
			if (ConfigManager.Config.PlayerEsp.DrawHealth && (UnityEngine.Object)(object)player.playerHealth != (UnityEngine.Object)null)
			{
				int num10 = ((_healthCurrentField != null) ? ((int)_healthCurrentField.GetValue(player.playerHealth)) : 0);
				int num11 = ((_maxHealthField != null) ? ((int)_maxHealthField.GetValue(player.playerHealth)) : 100);
				if (num11 > 0)
				{
					float num12 = Mathf.Clamp01((float)num10 / (float)num11);
					float num13 = 2f;
					float num14 = height;
					float num15 = val3.x - num13 - 2f;
					float y = val3.y;
					Render.DrawBox(new Rect(num15, y, num13, num14), Color.black);
					float num16 = num14 * num12;
					Color color2 = Color.Lerp(Color.red, Color.green, num12);
					Render.DrawBox(new Rect(num15, y + (num14 - num16), num13, num16), color2);
				}
			}
			bool flag3 = false;
			if ((UnityEngine.Object)(object)player.physGrabber != (UnityEngine.Object)null)
			{
				flag3 = ((!(_physGrabberGrabbedField != null)) ? player.physGrabber.grabbed : ((bool)_physGrabberGrabbedField.GetValue(player.physGrabber)));
			}
			if (ConfigManager.Config.PlayerEsp.DrawHeldItem && (UnityEngine.Object)(object)player.physGrabber != (UnityEngine.Object)null && flag3)
			{
				PhysGrabObject val4 = null;
				FieldInfo field = typeof(PhysGrabber).GetField("grabbedPhysGrabObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null)
				{
					object value = field.GetValue(player.physGrabber);
					val4 = (PhysGrabObject)((value is PhysGrabObject) ? value : null);
				}
				if ((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null)
				{
					Vector3 position2 = ((Component)val4).transform.position;
					Vector3 val5 = MathUtils.WorldToScreen(main, position2);
					if (val5.z > 0f)
					{
						color3 = ConfigManager.Config.PlayerEsp.EquipmentColor;
						start = new Vector2(val3.x + width / 2f, val3.y + height / 2f);
						if (ConfigManager.Config.PlayerEsp.DrawLine)
						{
							Render.DrawLine(start, new Vector2(val5.x, val5.y), color3, 1.5f);
						}
						float num17 = 10f;
						rect = new Rect(val5.x - num17 / 2f, val5.y - num17 / 2f, num17, num17);
						Render.DrawEspBox(rect, color3, 1.2f, ConfigManager.Config.Enemies.RenderMethod);
					}
				}
			}
			List<string> list = new List<string>();
			if (ConfigManager.Config.PlayerEsp.DrawName)
			{
				string text = ((_playerNameField != null) ? ((string)_playerNameField.GetValue(player)) : null);
				if (string.IsNullOrEmpty(text))
				{
					text = "Player";
				}
				list.Add(text);
			}
			if (ConfigManager.Config.PlayerEsp.DrawDistance)
			{
				list.Add($"{num3:F0}m");
			}
			if (list.Count > 0)
			{
				string text2 = string.Join(" - ", list);
				int fontSize = 12;
				Vector2 val6 = Render.MeasureString(text2, fontSize);
				float num18 = val3.x + (width - val6.x) / 2f;
				float num19 = val3.y - val6.y - 2f;
				Render.DrawStringOutlined(new Rect(num18, num19, val6.x, val6.y), text2, color, center: true, fontSize);
			}
			string text3 = "";
			bool flag4 = _isTumblingField != null && (bool)_isTumblingField.GetValue(player);
			bool flag5 = _isCrouchingField != null && (bool)_isCrouchingField.GetValue(player);
			bool flag6 = _isCrawlingField != null && (bool)_isCrawlingField.GetValue(player);
			bool flag7 = _isSlidingField != null && (bool)_isSlidingField.GetValue(player);
			if (flag4)
			{
				text3 = "Tumbling";
			}
			else if (flag5)
			{
				text3 = "Crouching";
			}
			else if (flag6)
			{
				text3 = "Crawling";
			}
			else if (flag7)
			{
				text3 = "Sliding";
			}
			if (!string.IsNullOrEmpty(text3))
			{
				int fontSize2 = 10;
				Vector2 val7 = Render.MeasureString(text3, fontSize2);
				float num20 = val3.x + (width - val7.x) / 2f;
				float num21 = val3.y + height + 2f;
				Render.DrawStringOutlined(new Rect(num20, num21, val7.x, val7.y), text3, Color.yellow, center: true, fontSize2);
			}
		}
		PlayerDeathHead[] array = UnityEngine.Object.FindObjectsByType<PlayerDeathHead>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (PlayerDeathHead val8 in array)
		{
			DrawDeathHead(main, val8);
		}
		PrunePlayerBoxStates();
	}

	private static void DrawDeathHead(Camera mainCamera, PlayerDeathHead deathHead)
	{
		if ((UnityEngine.Object)(object)deathHead == (UnityEngine.Object)null)
		{
			return;
		}
		PlayerAvatar playerAvatar = deathHead.playerAvatar;
		if ((UnityEngine.Object)(object)playerAvatar == (UnityEngine.Object)null || IsLocalPlayer(playerAvatar) || !IsPlayerDisabled(playerAvatar))
		{
			return;
		}
		Renderer[] componentsInChildren = ((Component)deathHead).GetComponentsInChildren<Renderer>(true);
		PhysGrabObject fieldValue = ReflectionUtils.GetFieldValue<PhysGrabObject>(deathHead, "physGrabObject");
		Vector3 val = (((UnityEngine.Object)(object)fieldValue != (UnityEngine.Object)null) ? fieldValue.centerPoint : ((Component)deathHead).transform.position);
		Rect val2;
		if (!TryGetStableRect(mainCamera, deathHead, componentsInChildren, val, DeathHeadFallbackSize, 4f, 6f, out val2))
		{
			return;
		}
		float width = val2.width;
		float height = val2.height;
		float num = Vector3.Distance(((Component)mainCamera).transform.position, val);
		Color color = ConfigManager.Config.PlayerEsp.Color;
		Render.DrawEspBox(val2, color, 1.6f, ConfigManager.Config.Enemies.RenderMethod);
		if (ConfigManager.Config.PlayerEsp.DrawHealth && (UnityEngine.Object)(object)playerAvatar.playerHealth != (UnityEngine.Object)null)
		{
			int currentHealth;
			int maxHealth;
			if (TryGetPlayerHealth(playerAvatar, out currentHealth, out maxHealth) && maxHealth > 0)
			{
				float num2 = Mathf.Clamp01((float)currentHealth / (float)maxHealth);
				float num3 = 2f;
				float num4 = val2.x - num3 - 2f;
				Render.DrawBox(new Rect(num4, val2.y, num3, height), Color.black);
				float num5 = height * num2;
				Color color2 = Color.Lerp(Color.red, Color.green, num2);
				Render.DrawBox(new Rect(num4, val2.y + (height - num5), num3, num5), color2);
			}
		}
		List<string> list = new List<string>();
		if (ConfigManager.Config.PlayerEsp.DrawName)
		{
			list.Add(GetPlayerName(playerAvatar) + " [头]");
		}
		if (ConfigManager.Config.PlayerEsp.DrawDistance)
		{
			list.Add($"{num:F0}m");
		}
		if (list.Count > 0)
		{
			string text = string.Join(" - ", list);
			int fontSize = 12;
			Vector2 val3 = Render.MeasureString(text, fontSize);
			float num6 = val2.x + (width - val3.x) / 2f;
			float num7 = val2.y - val3.y - 2f;
			Render.DrawStringOutlined(new Rect(num6, num7, val3.x, val3.y), text, color, center: true, fontSize);
		}
		string text2 = "死亡头颅";
		int fontSize2 = 10;
		Vector2 val4 = Render.MeasureString(text2, fontSize2);
		float num8 = val2.x + (width - val4.x) / 2f;
		float num9 = val2.y + height + 2f;
		Render.DrawStringOutlined(new Rect(num8, num9, val4.x, val4.y), text2, Color.yellow, center: true, fontSize2);
	}

	private static bool IsLocalPlayer(PlayerAvatar player)
	{
		return _isLocalField != null && (bool)_isLocalField.GetValue(player);
	}

	private static bool IsPlayerDisabled(PlayerAvatar player)
	{
		return _isDisabledField != null && (bool)_isDisabledField.GetValue(player);
	}

	private static string GetPlayerName(PlayerAvatar player)
	{
		string text = (_playerNameField != null) ? ((string)_playerNameField.GetValue(player)) : null;
		if (string.IsNullOrEmpty(text))
		{
			text = "Player";
		}
		return text;
	}

	private static bool TryGetPlayerHealth(PlayerAvatar player, out int currentHealth, out int maxHealth)
	{
		currentHealth = 0;
		maxHealth = 0;
		if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null || (UnityEngine.Object)(object)player.playerHealth == (UnityEngine.Object)null)
		{
			return false;
		}
		currentHealth = (_healthCurrentField != null) ? ((int)_healthCurrentField.GetValue(player.playerHealth)) : 0;
		maxHealth = (_maxHealthField != null) ? ((int)_maxHealthField.GetValue(player.playerHealth)) : 100;
		return true;
	}

	private static bool ShouldUsePlayerRenderer(Renderer renderer)
	{
		return !ShouldIgnoreRenderer(renderer) && renderer.enabled && renderer.gameObject.activeInHierarchy;
	}

	private static bool TryGetStablePlayerRect(Camera camera, PlayerAvatar player, Renderer[] renderers, Vector3 fallbackCenter, Vector3 fallbackSize, float minWidth, float minHeight, out Rect rect)
	{
		rect = default(Rect);
		if (player == null || camera == null || !TryGetPlayerWorldBounds(player, renderers, out Bounds bounds))
			return false;

		if (!EspProjectionUtils.TryGetScreenRect(camera, bounds, minWidth, minHeight, out Rect measured))
			return false;
		measured = AddPlayerPaddingAndClip(measured, 2f);
		if (measured.width <= 0f || measured.height <= 0f)
			return false;

		int id = player.GetInstanceID();
		float now = Time.unscaledTime;
		PlayerBoxState state;
		if (_playerBoxStates.TryGetValue(id, out state))
		{
			float age = now - state.updatedAt;
			float centerDelta = Vector2.Distance(state.rect.center, measured.center);
			float sizeRatio = Mathf.Max(measured.width / Mathf.Max(state.rect.width, 1f), state.rect.width / Mathf.Max(measured.width, 1f),
				measured.height / Mathf.Max(state.rect.height, 1f), state.rect.height / Mathf.Max(measured.height, 1f));
			float spikeLimit = Mathf.Max(72f, Mathf.Max(state.rect.height, measured.height) * 1.35f);
			if (age < 0.12f && (centerDelta > spikeLimit || sizeRatio > 2.8f))
			{
				// A single remote-transform correction can produce a bogus renderer
				// bounds sample. Hold the last rendered rect briefly and recover as
				// soon as consecutive samples agree.
				rect = state.rect;
				return rect.width > 0f && rect.height > 0f;
			}

			float blend = age > 0.25f ? 1f : 1f - Mathf.Exp(-nowDelta() / 0.055f);
			Rect smoothed = LerpRect(state.rect, measured, Mathf.Clamp(blend, 0.35f, 1f));
			state.rect = smoothed;
			state.updatedAt = now;
			rect = smoothed;
		}
		else
		{
			state = new PlayerBoxState { rect = measured, updatedAt = now };
			_playerBoxStates[id] = state;
			rect = measured;
		}
		return rect.width > 0f && rect.height > 0f;
	}

	private static float nowDelta()
	{
		return Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.05f);
	}

	private static Rect LerpRect(Rect from, Rect to, float t)
	{
		Vector2 center = Vector2.Lerp(from.center, to.center, t);
		Vector2 size = Vector2.Lerp(new Vector2(from.width, from.height), new Vector2(to.width, to.height), t);
		return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
	}

	private static void PrunePlayerBoxStates()
	{
		if (_playerBoxStates.Count == 0)
			return;
		List<int> stale = new List<int>();
		foreach (KeyValuePair<int, PlayerBoxState> entry in _playerBoxStates)
			if (Time.unscaledTime - entry.Value.updatedAt > 2f)
				stale.Add(entry.Key);
		foreach (int id in stale)
			_playerBoxStates.Remove(id);
	}

	private static bool TryGetStableRect(Camera camera, Component target, Renderer[] renderers, Vector3 fallbackCenter, Vector3 fallbackSize, float minWidth, float minHeight, out Rect rect)
	{
		rect = default(Rect);
		if ((UnityEngine.Object)(object)camera == (UnityEngine.Object)null || (UnityEngine.Object)(object)target == (UnityEngine.Object)null)
			return false;
		if (!EspProjectionUtils.TryGetScreenRect(camera, target.transform, renderers, ShouldUsePlayerRenderer,
			fallbackCenter, fallbackSize, minWidth, minHeight, out rect))
			return false;
		rect = AddPlayerPaddingAndClip(rect, 2f);
		return rect.width > 0f && rect.height > 0f;
	}

	private static Transform GetPlayerVisualRoot(PlayerAvatar player)
	{
		if (player == null)
			return null;
		PlayerAvatarVisuals visuals = player.playerAvatarVisuals;
		// PlayerAvatarVisuals is moved each frame using the game's interpolated
		// remote position. meshParent is only the model container and can have
		// its own local offset, so it must not be used as the positional anchor.
		return visuals != null ? visuals.transform : player.transform;
	}

	private static Renderer[] GetPlayerRenderers(PlayerAvatar player)
	{
		if (player == null)
			return System.Array.Empty<Renderer>();
		PlayerAvatarVisuals visuals = player.playerAvatarVisuals;
		if (visuals != null && visuals.meshParent != null)
			return visuals.meshParent.GetComponentsInChildren<Renderer>(true);
		Transform visualRoot = GetPlayerVisualRoot(player);
		return visualRoot != null ? visualRoot.GetComponentsInChildren<Renderer>(true) : player.GetComponentsInChildren<Renderer>(true);
	}

	private static bool TryGetPlayerWorldBounds(PlayerAvatar player, Renderer[] renderers, out Bounds bounds)
	{
		bounds = default(Bounds);
		if (player == null)
			return false;

		bool hasBounds = false;
		Transform visualRoot = GetPlayerVisualRoot(player);
		foreach (Renderer renderer in renderers ?? System.Array.Empty<Renderer>())
		{
			if (!ShouldUsePlayerRenderer(renderer))
				continue;

			AddBounds(renderer.bounds, ref bounds, ref hasBounds);
			SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
			if (skinned != null)
				AddBounds(TransformLocalBounds(skinned.localBounds, skinned.transform), ref bounds, ref hasBounds);
			MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
			if (meshFilter != null && meshFilter.sharedMesh != null)
				AddBounds(TransformLocalBounds(meshFilter.sharedMesh.bounds, meshFilter.transform), ref bounds, ref hasBounds);
		}

		// PlayerTumble owns the native body collider used while the avatar is
		// physically tumbling. Fuse it with the rendered model when available.
		PlayerTumble tumble = ReflectionUtils.GetFieldValue<PlayerTumble>(player, "tumble");
		if (tumble != null)
		{
			Collider[] colliders = tumble.GetComponentsInChildren<Collider>(true);
			foreach (Collider collider in colliders)
			{
				if (collider != null && collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy)
					AddBounds(collider.bounds, ref bounds, ref hasBounds);
			}
		}

		Transform anchor = visualRoot != null ? visualRoot : player.transform;
		if (!hasBounds)
		{
			bool crawling = _isCrawlingField != null && (bool)_isCrawlingField.GetValue(player);
			bool crouching = _isCrouchingField != null && (bool)_isCrouchingField.GetValue(player);
			float fallbackHeight = crawling ? 0.85f : crouching ? 1.25f : 1.8f;
			bounds = new Bounds(anchor.position + Vector3.up * (fallbackHeight * 0.5f), new Vector3(0.8f, fallbackHeight, 0.8f));
			hasBounds = true;
		}

		// Some player skins ship with incomplete SkinnedMeshRenderer bounds.
		// Expand undersized bounds around the interpolated visual root so the
		// outline encloses the full body instead of a single mesh fragment.
		if (hasBounds && (player.playerAvatarVisuals == null || player.playerAvatarVisuals.currentPose != PlayerAvatarVisuals.Pose.Tumble))
		{
			bool crawling = player.playerAvatarVisuals != null
				? player.playerAvatarVisuals.currentPose == PlayerAvatarVisuals.Pose.Crawl
				: _isCrawlingField != null && (bool)_isCrawlingField.GetValue(player);
			bool crouching = player.playerAvatarVisuals != null
				? player.playerAvatarVisuals.currentPose == PlayerAvatarVisuals.Pose.Crouch
				: _isCrouchingField != null && (bool)_isCrouchingField.GetValue(player);
			float minimumHeight = crawling ? 0.9f : crouching ? 1.35f : 1.75f;
			float minimumWidth = crawling ? 1.15f : 0.8f;
			Bounds poseFallback = new Bounds(anchor.position + Vector3.up * (minimumHeight * 0.5f), new Vector3(minimumWidth, minimumHeight, minimumWidth));
			// Always include the complete pose envelope: some cosmetic/model
			// renderer bounds cover only the torso or a single animated part.
			bounds.Encapsulate(poseFallback);
		}

		return hasBounds;
	}

	private static Bounds TransformLocalBounds(Bounds localBounds, Transform target)
	{
		Vector3 center = target.TransformPoint(localBounds.center);
		Vector3 extents = localBounds.extents;
		Vector3 axisX = target.TransformVector(new Vector3(extents.x, 0f, 0f));
		Vector3 axisY = target.TransformVector(new Vector3(0f, extents.y, 0f));
		Vector3 axisZ = target.TransformVector(new Vector3(0f, 0f, extents.z));
		Vector3 worldExtents = new Vector3(
			Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
			Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
			Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
		return new Bounds(center, worldExtents * 2f);
	}

	private static void AddBounds(Bounds candidate, ref Bounds combined, ref bool hasBounds)
	{
		Vector3 size = candidate.size;
		Vector3 center = candidate.center;
		if (float.IsNaN(center.x) || float.IsNaN(center.y) || float.IsNaN(center.z) ||
			float.IsInfinity(center.x) || float.IsInfinity(center.y) || float.IsInfinity(center.z) ||
			float.IsNaN(size.x) || float.IsNaN(size.y) || float.IsNaN(size.z) ||
			float.IsInfinity(size.x) || float.IsInfinity(size.y) || float.IsInfinity(size.z) || size.sqrMagnitude < 0.0001f)
			return;
		if (!hasBounds)
		{
			combined = candidate;
			hasBounds = true;
		}
		else
		{
			combined.Encapsulate(candidate);
		}
	}

	private static Rect AddPlayerPaddingAndClip(Rect rect, float padding)
	{
		float left = Mathf.Clamp(rect.xMin - padding, 0f, Screen.width);
		float right = Mathf.Clamp(rect.xMax + padding, 0f, Screen.width);
		float top = Mathf.Clamp(rect.yMin - padding, 0f, Screen.height);
		float bottom = Mathf.Clamp(rect.yMax + padding, 0f, Screen.height);
		return Rect.MinMaxRect(left, top, Mathf.Max(left, right), Mathf.Max(top, bottom));
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
}
