using System;
using UnityEngine;

namespace Cheat.Config;

[Serializable]
public class ConfigData
{
	[Serializable]
	public class LaserSightSettings
	{
		public bool Enabled = false;

		public KeyCode ToggleKey = (KeyCode)0;

		public bool ShowLocal = true;

		public KeyCode ShowLocalKey = (KeyCode)0;

		public bool ShowOthers = true;

		public KeyCode ShowOthersKey = (KeyCode)0;

		public bool ShowHitInfo = true;

		public KeyCode ShowHitInfoKey = (KeyCode)0;

		public Color Color = Color.red;

		public float Width = 0.02f;
	}

	[Serializable]
	public class LootSettings
	{
		public bool Enabled = false;

		public bool DrawTracers = false;

		public KeyCode DrawTracersKey = (KeyCode)0;

		public bool DrawBox = true;

		public KeyCode DrawBoxKey = (KeyCode)0;

		public bool DrawName = false;

		public KeyCode DrawNameKey = (KeyCode)0;

		public float MaxDistance = 50f;

		public int MinValue = 0;

		public bool UseClustering = true;

		public KeyCode UseClusteringKey = (KeyCode)0;

		public bool DynamicOpacity = true;

		public KeyCode DynamicOpacityKey = (KeyCode)0;

		public bool ShowCartUI = true;

		public KeyCode ShowCartUIKey = (KeyCode)0;

		public bool ItemInvulnerable = false;

		public KeyCode ItemInvulnerableKey = (KeyCode)0;

		public Color EspColor = new Color(0.25f, 1f, 0.35f, 0.95f);

		public bool HighlightEnabled = false;

		public KeyCode HighlightEnabledKey = (KeyCode)0;

		public float HighlightDistance = 5f;

		public Color HighlightColorVisible = Color.green;

		public Color HighlightColorOccluded = Color.yellow;

		public KeyCode ToggleKey = (KeyCode)0;
	}

	[Serializable]
	public class EnemySettings
	{
		public bool EspEnabled = false;

		public bool DrawTracers = false;

		public KeyCode DrawTracersKey = (KeyCode)0;

		public bool DrawBox = false;

		public KeyCode DrawBoxKey = (KeyCode)0;

		public int BoxType = 0;

		public bool DrawHealth = false;

		public KeyCode DrawHealthKey = (KeyCode)0;

		public bool DrawInfo = false;

		public KeyCode DrawInfoKey = (KeyCode)0;

		public bool DrawDistance = true;

		public KeyCode DrawDistanceKey = (KeyCode)0;

		public bool DrawStatus = false;

		public KeyCode DrawStatusKey = (KeyCode)0;

		public bool DrawPath = false;

		public KeyCode DrawPathKey = (KeyCode)0;

		public bool TargetWarning = false;

		public KeyCode TargetWarningKey = (KeyCode)0;

		public float MaxDistance = 200f;

		public Color EspColor = Color.red;

		public bool HighlightEnabled = false;

		public KeyCode HighlightEnabledKey = (KeyCode)0;

		public float HighlightMaxDistance = 80f;

		public Color HighlightColor = Color.red;

		public int RenderMethod = 0;

		public KeyCode ToggleKey = (KeyCode)0;
	}

	[Serializable]
	public class MinimapSettings
	{
		public bool Enabled = false;

		public bool ShowIcons = true;

		public KeyCode ShowIconsKey = (KeyCode)0;

		public int RenderMode = 0;

		public Color RingColor = Color.cyan;

		public bool AutoCenter = false;

		public KeyCode AutoCenterKey = (KeyCode)0;

		public float Zoom = 1f;

		public float ZoomSpeed = 0.5f;

		public float Size = 300f;

		public bool ShowPath = false;

		public KeyCode ShowPathKey = (KeyCode)0;

		public Vector2 Position = new Vector2(-1f, -1f);

		public KeyCode ToggleFocusKey = (KeyCode)109;

		public KeyCode ToggleRenderModeKey = (KeyCode)110;

		public KeyCode ToggleKey = (KeyCode)0;
	}

	[Serializable]
	public class CompassSettings
	{
		public bool Enabled = false;

		public float Size = 200f;

		public float Range = 30f;

		public float Scale = 1f;

		public float YOffset = 0f;

		public KeyCode ToggleKey = (KeyCode)0;
	}

	[Serializable]
	public class LocalSettings
	{
		public bool GodMode = false;

		public bool InfiniteStamina = false;

		public KeyCode InfiniteStaminaKey = (KeyCode)0;

		public bool InfiniteBattery = false;

		public KeyCode InfiniteBatteryKey = (KeyCode)0;

		public float GrabRange = 1f;

		public float GrabStrength = 1f;

		public bool RunSpeedEnabled = false;

		public KeyCode RunSpeedEnabledKey = (KeyCode)0;

		public bool JumpForceEnabled = false;

		public KeyCode JumpForceEnabledKey = (KeyCode)0;

		public bool GravityEnabled = false;

		public KeyCode GravityEnabledKey = (KeyCode)0;

		public float JumpForce = 1f;

		public float Gravity = 1f;

		public bool NoClip = false;

		public bool NoRagdoll = false;

		public float RunSpeed = 1f;

		public float NoClipSpeed = 10f;

		public float NoClipFastMultiplier = 2f;

		public KeyCode GodModeKey = (KeyCode)0;

		public KeyCode NoClipKey = (KeyCode)0;
		
		public KeyCode FreeCamToggleKey = (KeyCode)0;

		public float FreeCamSpeed = 10f;

		public float FreeCamFastMultiplier = 3f;

		public float FreeCamSensitivity = 2f;
	}

	[Serializable]
	public class PlayerEspSettings
	{
		public bool Enabled = false;

		public KeyCode ToggleKey = (KeyCode)0;

		public bool DrawName = true;

		public KeyCode DrawNameKey = (KeyCode)0;

		public bool DrawHealth = true;

		public KeyCode DrawHealthKey = (KeyCode)0;

		public bool DrawDistance = true;

		public KeyCode DrawDistanceKey = (KeyCode)0;

		public bool DrawHeldItem = true;

		public KeyCode DrawHeldItemKey = (KeyCode)0;

		public Color Color = new Color(0.35f, 0.68f, 1f, 0.95f);

		public Color EquipmentColor = new Color(1f, 0.88f, 0.2f, 0.95f);
	}

	[Serializable]
	public class StructureEspSettings
	{
		public bool ExtractionPointsEnabled = false;

		public KeyCode ExtractionPointsEnabledKey = (KeyCode)0;

		public bool EvacuationPointsEnabled = false;

		public KeyCode EvacuationPointsEnabledKey = (KeyCode)0;

		public bool TrapsEnabled = false;

		public KeyCode TrapsEnabledKey = (KeyCode)0;

		public bool DrawTracers = false;

		public KeyCode DrawTracersKey = (KeyCode)0;

		public bool DrawBox = true;

		public KeyCode DrawBoxKey = (KeyCode)0;

		public bool DrawName = true;

		public KeyCode DrawNameKey = (KeyCode)0;

		public bool DrawDistance = true;

		public KeyCode DrawDistanceKey = (KeyCode)0;

		public float MaxDistance = 250f;

		public Color ExtractionPointColor = Color.white;

		public Color EvacuationPointColor = new Color(0.76f, 0.34f, 1f, 0.95f);

		public Color TrapColor = new Color(1f, 0.38f, 0.12f, 0.95f);
	}

	[Serializable]
	public class MiscSettings
	{
		public KeyCode ToggleKey = KeyCode.Insert;

		public bool Crosshair = false;

		public KeyCode CrosshairKey = (KeyCode)0;

		public bool ShowFps = false;

		public KeyCode ShowFpsKey = (KeyCode)0;

		public bool ShowKeybinds = false;

		public KeyCode ShowKeybindsKey = (KeyCode)0;

		public Color MenuAccent = new Color(0.02f, 0.59f, 1f);

		public float FOV = 60f;

		public bool Fullbright = false;

		public KeyCode FullbrightKey = (KeyCode)0;

		public float FullbrightIntensity = 0.5f;

		public bool NoFog = false;

		public KeyCode NoFogKey = (KeyCode)0;

		public int SetItemValue = 10000;
	}

	[Serializable]
	public class UiSettings
	{
		public KeyCode MenuToggleKey = KeyCode.Insert;
	}

	public LootSettings Loot = new LootSettings();

	public EnemySettings Enemies = new EnemySettings();

	public MinimapSettings Minimap = new MinimapSettings();

	public LocalSettings Local = new LocalSettings();

	public MiscSettings Misc = new MiscSettings();

	public CompassSettings Compass = new CompassSettings();

	public PlayerEspSettings PlayerEsp = new PlayerEspSettings();

	public LaserSightSettings LaserSight = new LaserSightSettings();

	public StructureEspSettings Structures = new StructureEspSettings();

	public UiSettings UI = new UiSettings();
}
