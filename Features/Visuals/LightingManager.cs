using Cheat.Config;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Cheat.Features.Visuals;

public class LightingManager : MonoBehaviour
{
	private Color _originalAmbientLight;

	private AmbientMode _originalAmbientMode;

	private bool _originalFog;

	private bool _ambientCaptured;

	private bool _fogCaptured;

	private int _capturedSceneHandle = -1;

	private Light _fullbrightLight;

	private void LateUpdate()
	{
		if (ConfigManager.Config == null)
		{
			return;
		}

		bool fullbright = ConfigManager.Config.Misc.Fullbright;
		bool noFog = ConfigManager.Config.Misc.NoFog;
		RefreshCapturedStateIfSceneChanged();
		if (fullbright)
		{
			if (_fullbrightLight == null)
			{
				GameObject lightObj = new GameObject("FullbrightLight");
				_fullbrightLight = lightObj.AddComponent<Light>();
				_fullbrightLight.type = LightType.Directional;
				_fullbrightLight.shadows = LightShadows.None;
				_fullbrightLight.renderMode = LightRenderMode.ForcePixel;
				DontDestroyOnLoad(lightObj);
			}
			_fullbrightLight.enabled = true;
			_fullbrightLight.intensity = ConfigManager.Config.Misc.FullbrightIntensity;
			_fullbrightLight.color = Color.white;
			
			if (Camera.main != null)
			{
				_fullbrightLight.transform.rotation = Camera.main.transform.rotation;
			}
		}
		else
		{
			if (_fullbrightLight != null)
			{
				_fullbrightLight.enabled = false;
			}
		}

		if (fullbright)
		{
			CaptureAmbientState();
			float fullbrightIntensity = ConfigManager.Config.Misc.FullbrightIntensity;
			if (!Mathf.Approximately(RenderSettings.ambientLight.r, fullbrightIntensity) || RenderSettings.ambientMode != AmbientMode.Flat)
			{
				RenderSettings.ambientLight = new Color(fullbrightIntensity, fullbrightIntensity, fullbrightIntensity, 1f);
				RenderSettings.ambientMode = AmbientMode.Flat;
			}
		}
		else
		{
			RestoreAmbientState();
		}

		if (noFog)
		{
			CaptureFogState();
			if (RenderSettings.fog)
			{
				RenderSettings.fog = false;
			}
		}
		else
		{
			RestoreFogState();
		}
	}

	public void OnDisable()
	{
		RestoreAmbientState();
		RestoreFogState();
		if (_fullbrightLight != null)
		{
			_fullbrightLight.enabled = false;
		}
	}

	private void RefreshCapturedStateIfSceneChanged()
	{
		Scene activeScene = SceneManager.GetActiveScene();
		if (!activeScene.IsValid())
		{
			return;
		}
		if (_capturedSceneHandle == activeScene.handle)
		{
			return;
		}
		_capturedSceneHandle = activeScene.handle;
		_ambientCaptured = false;
		_fogCaptured = false;
	}

	private void CaptureAmbientState()
	{
		if (_ambientCaptured)
		{
			return;
		}
		_originalAmbientLight = RenderSettings.ambientLight;
		_originalAmbientMode = RenderSettings.ambientMode;
		_ambientCaptured = true;
	}

	private void CaptureFogState()
	{
		if (_fogCaptured)
		{
			return;
		}
		_originalFog = RenderSettings.fog;
		_fogCaptured = true;
	}

	private void RestoreAmbientState()
	{
		if (!_ambientCaptured)
		{
			return;
		}
		RenderSettings.ambientLight = _originalAmbientLight;
		RenderSettings.ambientMode = _originalAmbientMode;
		_ambientCaptured = false;
	}

	private void RestoreFogState()
	{
		if (!_fogCaptured)
		{
			return;
		}
		RenderSettings.fog = _originalFog;
		_fogCaptured = false;
	}
}
