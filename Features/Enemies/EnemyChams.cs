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
			if (ConfigManager.Config.Enemies.HighlightEnabled)
			{
				ApplyChams();
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
			ApplyChams();
		}
		else
		{
			RemoveChams();
		}
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
		float num = Mathf.Clamp(ConfigManager.Config.Enemies.HighlightMaxDistance, 10f, 150f);
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
		if (ConfigManager.Config.Enemies.HighlightEnabled)
		{
			ApplyChams();
		}
	}

	public static void Cleanup()
	{
		if ((UnityEngine.Object)(object)_instance != (UnityEngine.Object)null)
		{
			_instance.RemoveChams();
		}
	}
}
