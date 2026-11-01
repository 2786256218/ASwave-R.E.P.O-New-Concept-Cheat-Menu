using System;
using System.Collections.Generic;
using System.Reflection;
using Cheat.Config;
using TMPro;
using UnityEngine;

namespace Cheat.Features.Visuals;

/// <summary>
/// Runtime-safe adaptations of client-side presentation options from the
/// supplied reference project. This project is not BepInEx/Harmony based, so
/// these controls operate on the game's live components and restore originals.
/// </summary>
public static class ReferenceExperienceManager
{
	private static readonly Dictionary<int, bool> CameraBobStates = new Dictionary<int, bool>();
	private static readonly Dictionary<int, bool> CameraTiltStates = new Dictionary<int, bool>();
	private static readonly Dictionary<int, bool> AnimNoiseStates = new Dictionary<int, bool>();
	private static readonly Dictionary<int, bool> CameraShakeStates = new Dictionary<int, bool>();
	private static readonly Dictionary<int, float> GunRecoilValues = new Dictionary<int, float>();
	private static readonly Dictionary<int, object> ItalicStyles = new Dictionary<int, object>();
	private static readonly FieldInfo GunStateCurrent = typeof(ItemGun).GetField("stateCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly FieldInfo GunStateTimeMax = typeof(ItemGun).GetField("stateTimeMax", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly FieldInfo GunRecoilForce = typeof(ItemGun).GetField("gunRecoilForce", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly PropertyInfo TmpFontStyle = typeof(TMP_Text).GetProperty("fontStyle", BindingFlags.Instance | BindingFlags.Public);

	public static void Update()
	{
		if (ConfigManager.Config == null)
			return;

		ConfigData.MiscSettings settings = ConfigManager.Config.Misc;
		SetDisabled<CameraBob>(settings.DisableCameraMotion, CameraBobStates);
		SetDisabled<CameraTilt>(settings.DisableCameraMotion, CameraTiltStates);
		SetDisabled<AnimNoise>(settings.DisableCameraMotion, AnimNoiseStates);
		SetDisabled<CameraShake>(settings.DisableCameraShakeEffects, CameraShakeStates);
		UpdateGunControls(settings.DisableGunRecoil, settings.DisableGunCooldown);
		UpdateItalicText(settings.DisableItalicText);
	}

	public static void Reset()
	{
		RestoreBehaviours<CameraBob>(CameraBobStates);
		RestoreBehaviours<CameraTilt>(CameraTiltStates);
		RestoreBehaviours<AnimNoise>(AnimNoiseStates);
		RestoreBehaviours<CameraShake>(CameraShakeStates);
		RestoreGunRecoil();
		RestoreItalicText();
	}

	private static void SetDisabled<T>(bool disabled, Dictionary<int, bool> states) where T : Behaviour
	{
		try
		{
			T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			HashSet<int> alive = new HashSet<int>();
			for (int i = 0; i < components.Length; i++)
			{
				T component = components[i];
				if (component == null)
					continue;
				int id = component.GetInstanceID();
				alive.Add(id);
				if (disabled)
				{
					if (!states.ContainsKey(id))
						states[id] = component.enabled;
					component.enabled = false;
				}
				else if (states.TryGetValue(id, out bool wasEnabled))
				{
					component.enabled = wasEnabled;
					states.Remove(id);
				}
			}
			if (!disabled)
			{
				List<int> stale = new List<int>();
				foreach (int id in states.Keys)
					if (!alive.Contains(id))
						stale.Add(id);
				for (int i = 0; i < stale.Count; i++)
					states.Remove(stale[i]);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[REPO New Cheat] Camera presentation toggle failed: " + ex.Message);
		}
	}

	private static void RestoreBehaviours<T>(Dictionary<int, bool> states) where T : Behaviour
	{
		try
		{
			T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (int i = 0; i < components.Length; i++)
			{
				T component = components[i];
				if (component != null && states.TryGetValue(component.GetInstanceID(), out bool wasEnabled))
					component.enabled = wasEnabled;
			}
			states.Clear();
		}
		catch { }
	}

	private static void UpdateGunControls(bool noRecoil, bool noCooldown)
	{
		try
		{
			ItemGun[] guns = UnityEngine.Object.FindObjectsByType<ItemGun>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			HashSet<int> alive = new HashSet<int>();
			for (int i = 0; i < guns.Length; i++)
			{
				ItemGun gun = guns[i];
				if (gun == null)
					continue;
				int id = gun.GetInstanceID();
				alive.Add(id);
				if (noRecoil && GunRecoilForce != null)
				{
					if (!GunRecoilValues.ContainsKey(id))
						GunRecoilValues[id] = (float)GunRecoilForce.GetValue(gun);
					GunRecoilForce.SetValue(gun, 0f);
				}
				else if (GunRecoilValues.TryGetValue(id, out float recoil))
				{
					GunRecoilForce?.SetValue(gun, recoil);
					GunRecoilValues.Remove(id);
				}

				if (noCooldown && GunStateCurrent != null && GunStateTimeMax != null)
				{
					object state = GunStateCurrent.GetValue(gun);
					if (state != null && string.Equals(state.ToString(), "Reloading", StringComparison.OrdinalIgnoreCase))
						GunStateTimeMax.SetValue(gun, 0f);
				}
			}
			if (!noRecoil)
				RestoreGunRecoil();
			if (GunRecoilValues.Count > 0)
			{
				List<int> stale = new List<int>();
				foreach (int id in GunRecoilValues.Keys)
					if (!alive.Contains(id))
						stale.Add(id);
				for (int i = 0; i < stale.Count; i++)
					GunRecoilValues.Remove(stale[i]);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[REPO New Cheat] Gun presentation toggle failed: " + ex.Message);
		}
	}

	private static void RestoreGunRecoil()
	{
		if (GunRecoilValues.Count == 0 || GunRecoilForce == null)
			return;
		try
		{
			ItemGun[] guns = UnityEngine.Object.FindObjectsByType<ItemGun>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (int i = 0; i < guns.Length; i++)
			{
				ItemGun gun = guns[i];
				if (gun != null && GunRecoilValues.TryGetValue(gun.GetInstanceID(), out float recoil))
					GunRecoilForce.SetValue(gun, recoil);
			}
		}
		catch { }
		GunRecoilValues.Clear();
	}

	private static void UpdateItalicText(bool disabled)
	{
		if (TmpFontStyle == null)
			return;
		try
		{
			TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (int i = 0; i < texts.Length; i++)
			{
				TMP_Text text = texts[i];
				if (text == null)
					continue;
				int id = text.GetInstanceID();
				if (disabled)
				{
					if (!ItalicStyles.ContainsKey(id))
						ItalicStyles[id] = TmpFontStyle.GetValue(text, null);
					object style = TmpFontStyle.GetValue(text, null);
					if (style != null)
					{
						object italicValue = Enum.Parse(TmpFontStyle.PropertyType, "Italic", ignoreCase: true);
						long currentBits = Convert.ToInt64(style);
						long italicBits = Convert.ToInt64(italicValue);
						long nonItalicBits = currentBits & ~italicBits;
						if (nonItalicBits != currentBits)
							TmpFontStyle.SetValue(text, Enum.ToObject(TmpFontStyle.PropertyType, nonItalicBits), null);
					}
				}
				else if (ItalicStyles.TryGetValue(id, out object original))
				{
					TmpFontStyle.SetValue(text, original, null);
					ItalicStyles.Remove(id);
				}
			}
			if (!disabled)
				ItalicStyles.Clear();
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[REPO New Cheat] Text style toggle failed: " + ex.Message);
		}
	}

	private static void RestoreItalicText()
	{
		if (ItalicStyles.Count == 0 || TmpFontStyle == null)
			return;
		try
		{
			TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (int i = 0; i < texts.Length; i++)
				if (texts[i] != null && ItalicStyles.TryGetValue(texts[i].GetInstanceID(), out object original))
					TmpFontStyle.SetValue(texts[i], original, null);
		}
		catch { }
		ItalicStyles.Clear();
	}
}
