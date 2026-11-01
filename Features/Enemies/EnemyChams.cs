using System;
using System.Collections.Generic;
using Cheat.Config;
using UnityEngine;

namespace Cheat.Features.Enemies;

public class EnemyChams : MonoBehaviour
{
	private static EnemyChams _instance;

	private Material _chamMaterial;

	private float _lastUpdate;

	private const float UpdateInterval = 0.5f;

	private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();

	private List<Renderer> _processedRenderers = new List<Renderer>();

	private Dictionary<SkinnedMeshRenderer, Mesh> _bakedMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

	private readonly MaterialPropertyBlock _glowProperties = new MaterialPropertyBlock();

	public static EnemyChams Instance
	{
		get
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			if ((UnityEngine.Object)(object)_instance == (UnityEngine.Object)null)
			{
				_instance = new GameObject("EnemyChamsController").AddComponent<EnemyChams>();
				UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)(object)((Component)_instance).gameObject);
			}
			return _instance;
		}
	}

	public static Material ChamsMaterial => Instance._chamMaterial;

	private void Start()
	{
		CreateChamMaterial();
	}

	private void CreateChamMaterial()
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		Shader val = Shader.Find("Hidden/Internal-Colored");
		if (val == null)
		{
			val = Shader.Find("GUI/Text Shader");
		}
		if (val == null)
		{
			val = Shader.Find("Sprites/Default");
		}
		if (val == null)
		{
			val = Shader.Find("Legacy Shaders/Transparent/Diffuse");
		}
		if (val == null)
		{
			val = Shader.Find("Unlit/Color");
		}
		if (val != null)
		{
			_chamMaterial = new Material(val);
			((UnityEngine.Object)_chamMaterial).hideFlags = (HideFlags)61;
			_chamMaterial.renderQueue = 4000;
			_chamMaterial.SetInt("_SrcBlend", 5);
			_chamMaterial.SetInt("_DstBlend", 10);
			_chamMaterial.SetInt("_ZTest", 8);
			_chamMaterial.SetInt("_ZWrite", 0);
			_chamMaterial.SetInt("_Cull", 0);
			if (_chamMaterial.HasProperty("_Color"))
			{
				_chamMaterial.SetColor("_Color", ConfigManager.Config.Enemies.HighlightColor);
			}
			if (_chamMaterial.HasProperty("_TintColor"))
			{
				_chamMaterial.SetColor("_TintColor", ConfigManager.Config.Enemies.HighlightColor);
			}
		}
		else
		{
			Console.WriteLine("[EnemyChams] Critical Error: No suitable shader found!");
		}
	}

	private void Update()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)_chamMaterial != (UnityEngine.Object)null)
		{
			Color highlightColor = ConfigManager.Config.Enemies.HighlightColor;
			if (_chamMaterial.HasProperty("_Color"))
			{
				_chamMaterial.SetColor("_Color", highlightColor);
			}
			if (_chamMaterial.HasProperty("_TintColor"))
			{
				_chamMaterial.SetColor("_TintColor", highlightColor);
			}
		}
		if (Time.time - _lastUpdate > 0.5f)
		{
			_lastUpdate = Time.time;
			bool shouldGlow = ConfigManager.Config.Enemies.EspEnabled && ConfigManager.Config.Enemies.RenderMethod == 2;
			ConfigManager.Config.Enemies.HighlightEnabled = shouldGlow;
			if (shouldGlow)
			{
				if ((UnityEngine.Object)(object)_chamMaterial == (UnityEngine.Object)null)
					CreateChamMaterial();
			}
			else
			{
				RemoveChams();
			}
		}
	}

	public void ToggleChams(bool enabled)
	{
		ConfigManager.Config.Enemies.HighlightEnabled = enabled;
		if (enabled)
		{
			if ((UnityEngine.Object)(object)_chamMaterial == (UnityEngine.Object)null)
				CreateChamMaterial();
		}
		else
		{
			RemoveChams();
		}
	}

	public static void DrawOverlay()
	{
		if ((UnityEngine.Object)(object)_instance == (UnityEngine.Object)null || ConfigManager.Config == null || !ConfigManager.Config.Enemies.EspEnabled || ConfigManager.Config.Enemies.RenderMethod != 2)
			return;
		_instance.DrawOverlayMeshes();
	}

	private void DrawOverlayMeshes()
	{
		if ((UnityEngine.Object)(object)_chamMaterial == (UnityEngine.Object)null)
			CreateChamMaterial();
		if ((UnityEngine.Object)(object)_chamMaterial == (UnityEngine.Object)null || (UnityEngine.Object)(object)Camera.main == (UnityEngine.Object)null || (UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null)
			return;

		Camera camera = Camera.main;
		Vector3 cameraPosition = camera.transform.position;
		float maxDistance = Mathf.Clamp(ConfigManager.Config.Enemies.HighlightMaxDistance, 10f, 5000f);
		float maxDistanceSqr = maxDistance * maxDistance;
		foreach (EnemyParent parent in EnemyDirector.instance.enemiesSpawned)
		{
			if ((UnityEngine.Object)(object)parent == (UnityEngine.Object)null || !((Component)parent).gameObject.activeInHierarchy || (((Component)parent).transform.position - cameraPosition).sqrMagnitude > maxDistanceSqr)
				continue;

			float distanceRatio = Mathf.Clamp01(Vector3.Distance(((Component)parent).transform.position, cameraPosition) / maxDistance);
			float hueOffset = Mathf.Repeat(Time.unscaledTime * 0.32f + distanceRatio * 0.35f, 1f);
			Color configuredColor = ConfigManager.Config.Enemies.HighlightColor;
			float alpha = Mathf.Clamp01(configuredColor.a);

			Renderer[] renderers = ((Component)parent).GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < renderers.Length; i++)
			{
				Renderer renderer = renderers[i];
				// The game disables distant renderers for its own LOD/culling. We can still
				// draw their mesh here, so only skip inactive objects and non-mesh effects.
				if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null || !((Component)renderer).gameObject.activeInHierarchy || renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
					continue;

				// MaterialPropertyBlock is captured per draw call. Setting the shared material
				// itself made every queued draw use the last color (often the saved red tint).
				float rendererHue = Mathf.Repeat(hueOffset + i * 0.075f, 1f);
				Color glowColor = Color.HSVToRGB(rendererHue, 0.95f, 1f);
				glowColor.a = alpha;
				_glowProperties.Clear();
				_glowProperties.SetColor("_Color", glowColor);
				_glowProperties.SetColor("_TintColor", glowColor);
				_glowProperties.SetColor("_BaseColor", glowColor);
				int drawLayer = renderer.gameObject.layer;
				if ((camera.cullingMask & (1 << drawLayer)) == 0)
				{
					drawLayer = 0;
					if ((camera.cullingMask & 1) == 0)
						for (int layer = 1; layer < 32; layer++)
							if ((camera.cullingMask & (1 << layer)) != 0)
							{
								drawLayer = layer;
								break;
							}
				}

				SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
				if (skinned != null)
				{
					if (!_bakedMeshes.TryGetValue(skinned, out Mesh bakedMesh) || (UnityEngine.Object)(object)bakedMesh == (UnityEngine.Object)null)
					{
						bakedMesh = new Mesh();
						bakedMesh.hideFlags = HideFlags.HideAndDontSave;
						_bakedMeshes[skinned] = bakedMesh;
					}
					skinned.BakeMesh(bakedMesh, true);
					if (bakedMesh.vertexCount > 0)
						for (int subMesh = 0; subMesh < bakedMesh.subMeshCount; subMesh++)
						Graphics.DrawMesh(bakedMesh, skinned.transform.localToWorldMatrix, _chamMaterial, drawLayer, camera, subMesh, _glowProperties);
					continue;
				}

				MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
				if (meshFilter != null && (UnityEngine.Object)(object)meshFilter.sharedMesh != (UnityEngine.Object)null)
					for (int subMesh = 0; subMesh < meshFilter.sharedMesh.subMeshCount; subMesh++)
					Graphics.DrawMesh(meshFilter.sharedMesh, renderer.transform.localToWorldMatrix, _chamMaterial, drawLayer, camera, subMesh, _glowProperties);
			}
		}
		CleanupBakedMeshes();
	}

	private void CleanupBakedMeshes()
	{
		List<SkinnedMeshRenderer> deadRenderers = null;
		foreach (KeyValuePair<SkinnedMeshRenderer, Mesh> entry in _bakedMeshes)
		{
			if ((UnityEngine.Object)(object)entry.Key == (UnityEngine.Object)null)
			{
				if (deadRenderers == null)
					deadRenderers = new List<SkinnedMeshRenderer>();
				deadRenderers.Add(entry.Key);
				if ((UnityEngine.Object)(object)entry.Value != (UnityEngine.Object)null)
					UnityEngine.Object.Destroy((UnityEngine.Object)(object)entry.Value);
			}
		}
		if (deadRenderers != null)
			for (int i = 0; i < deadRenderers.Count; i++)
				_bakedMeshes.Remove(deadRenderers[i]);
	}

	private void ApplyChams()
	{
		if ((UnityEngine.Object)(object)_chamMaterial == (UnityEngine.Object)null)
		{
			CreateChamMaterial();
		}
		if ((UnityEngine.Object)(object)_chamMaterial == (UnityEngine.Object)null || (UnityEngine.Object)(object)EnemyDirector.instance == (UnityEngine.Object)null)
		{
			return;
		}
		Vector3 referencePosition = GetReferencePosition();
		float num = Mathf.Clamp(ConfigManager.Config.Enemies.HighlightMaxDistance, 10f, 5000f);
		float num2 = num * num;
		foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
		{
			if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null)
			{
				continue;
			}
			if ((((Component)item).transform.position - referencePosition).sqrMagnitude > num2)
			{
				RestoreOriginalMaterials(item);
				continue;
			}
			Renderer[] componentsInChildren = ((Component)item).GetComponentsInChildren<Renderer>(true);
			Renderer[] array = componentsInChildren;
			foreach (Renderer val in array)
			{
				if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null || val is ParticleSystemRenderer || val is TrailRenderer || val is LineRenderer || !((Component)val).gameObject.activeInHierarchy)
				{
					continue;
				}
				if (!_originalMaterials.ContainsKey(val))
				{
					_originalMaterials[val] = val.sharedMaterials;
					_processedRenderers.Add(val);
				}
				bool flag = false;
				if (val.sharedMaterials.Length == 0)
				{
					continue;
				}
				if ((UnityEngine.Object)(object)val.sharedMaterials[0] != (UnityEngine.Object)(object)_chamMaterial)
				{
					flag = true;
				}
				if (flag)
				{
					Material[] array2 = (Material[])(object)new Material[val.sharedMaterials.Length];
					for (int j = 0; j < array2.Length; j++)
					{
						array2[j] = _chamMaterial;
					}
					val.sharedMaterials = array2;
				}
			}
		}
		CleanupDictionary();
	}

	private void RestoreOriginalMaterials(Component root)
	{
		if ((UnityEngine.Object)(object)root == (UnityEngine.Object)null)
		{
			return;
		}
		Renderer[] componentsInChildren = ((Component)root).GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Renderer val = componentsInChildren[i];
			if (!((UnityEngine.Object)(object)val == (UnityEngine.Object)null) && _originalMaterials.TryGetValue(val, out var value) && val.sharedMaterials.Length != 0 && (UnityEngine.Object)(object)val.sharedMaterials[0] == (UnityEngine.Object)(object)_chamMaterial)
			{
				val.sharedMaterials = value;
			}
		}
	}

	private static Vector3 GetReferencePosition()
	{
		if ((UnityEngine.Object)(object)Camera.main != (UnityEngine.Object)null)
		{
			return ((Component)Camera.main).transform.position;
		}
		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
		{
			return ((Component)PlayerController.instance).transform.position;
		}
		return Vector3.zero;
	}

	private void RemoveChams()
	{
		foreach (KeyValuePair<Renderer, Material[]> originalMaterial in _originalMaterials)
		{
			Renderer key = originalMaterial.Key;
			if ((UnityEngine.Object)(object)key != (UnityEngine.Object)null && key.sharedMaterials.Length != 0 && (UnityEngine.Object)(object)key.sharedMaterials[0] == (UnityEngine.Object)(object)_chamMaterial)
			{
				key.sharedMaterials = originalMaterial.Value;
			}
		}
		_originalMaterials.Clear();
		_processedRenderers.Clear();
	}

	private void CleanupDictionary()
	{
		for (int num = _processedRenderers.Count - 1; num >= 0; num--)
		{
			if ((UnityEngine.Object)(object)_processedRenderers[num] == (UnityEngine.Object)null)
			{
				_originalMaterials.Remove(_processedRenderers[num]);
				_processedRenderers.RemoveAt(num);
			}
		}
	}

	public void Refresh()
	{
		RemoveChams();
		if (ConfigManager.Config.Enemies.EspEnabled && ConfigManager.Config.Enemies.RenderMethod == 2)
		{
			if ((UnityEngine.Object)(object)_chamMaterial == (UnityEngine.Object)null)
				CreateChamMaterial();
		}
	}

	public static void Cleanup()
	{
		if ((UnityEngine.Object)(object)_instance != (UnityEngine.Object)null)
		{
			_instance.RemoveChams();
			foreach (Mesh mesh in _instance._bakedMeshes.Values)
				if ((UnityEngine.Object)(object)mesh != (UnityEngine.Object)null)
					UnityEngine.Object.Destroy((UnityEngine.Object)(object)mesh);
			_instance._bakedMeshes.Clear();
			if ((UnityEngine.Object)(object)_instance._chamMaterial != (UnityEngine.Object)null)
				UnityEngine.Object.Destroy((UnityEngine.Object)(object)_instance._chamMaterial);
			_instance._chamMaterial = null;
		}
	}
}
