using System;
using Cheat.Config;
using UnityEngine;

namespace Cheat.Features.Loot;

public class LootChams
{
	private static Material _chamsMaterial;

	private static float _lastShaderAttempt;

	public static void OnRenderObject()
	{
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Expected O, but got Unknown
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Expected O, but got Unknown
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		if (!ConfigManager.Config.Loot.Enabled || !ConfigManager.Config.Loot.HighlightEnabled || LootManager.LootItems == null || LootManager.LootItems.Count == 0)
		{
			return;
		}
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)main == (UnityEngine.Object)null)
		{
			return;
		}
		if ((UnityEngine.Object)(object)_chamsMaterial == (UnityEngine.Object)null)
		{
			if (Time.time - _lastShaderAttempt < 0.5f)
			{
				return;
			}
			_lastShaderAttempt = Time.time;
			Shader val = Shader.Find("Hidden/Internal-Colored");
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				val = Shader.Find("GUI/Text Shader");
			}
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				Console.WriteLine("[LootChams] Failed to find shader, will retry...");
				return;
			}
			_chamsMaterial = new Material(val);
			((UnityEngine.Object)_chamsMaterial).hideFlags = (HideFlags)61;
			try
			{
				_chamsMaterial.SetInt("_ZTest", 8);
				_chamsMaterial.SetInt("_ZWrite", 0);
				_chamsMaterial.SetInt("_Cull", 0);
			}
			catch
			{
			}
			Console.WriteLine("[LootChams] Material created successfully");
		}
		float num = ConfigManager.Config.Loot.HighlightDistance * ConfigManager.Config.Loot.HighlightDistance;
		Vector3 position = ((Component)main).transform.position;
		foreach (LootItem lootItem in LootManager.LootItems)
		{
			if ((UnityEngine.Object)(object)lootItem.PhysGrabObject == (UnityEngine.Object)null || lootItem.InCart)
			{
				continue;
			}
			Vector3 val2 = lootItem.Position - position;
			float sqrMagnitude = val2.sqrMagnitude;
			if (sqrMagnitude > num)
			{
				continue;
			}
			bool flag = false;
			if (!Physics.Linecast(position, lootItem.Position, LayerMask.GetMask(new string[3] { "Default", "Static", "Terrain" })))
			{
				flag = true;
			}
			Color val3 = (flag ? ConfigManager.Config.Loot.HighlightColorVisible : ConfigManager.Config.Loot.HighlightColorOccluded);
			if (_chamsMaterial.HasProperty("_Color"))
			{
				_chamsMaterial.SetColor("_Color", val3);
			}
			else if (_chamsMaterial.HasProperty("_TintColor"))
			{
				_chamsMaterial.SetColor("_TintColor", val3);
			}
			for (int i = 0; i < lootItem.Renderers.Count; i++)
			{
				Renderer val4 = lootItem.Renderers[i];
				if ((UnityEngine.Object)(object)val4 == (UnityEngine.Object)null || !((Component)val4).gameObject.activeInHierarchy)
				{
					continue;
				}
				SkinnedMeshRenderer val5 = (SkinnedMeshRenderer)(object)((val4 is SkinnedMeshRenderer) ? val4 : null);
				if (val5 != null)
				{
					Mesh val6 = new Mesh();
					val5.BakeMesh(val6, true);
					if (val6.vertexCount > 0)
					{
						Graphics.DrawMesh(val6, ((Component)val5).transform.localToWorldMatrix, _chamsMaterial, ((Component)val4).gameObject.layer);
					}
					UnityEngine.Object.Destroy((UnityEngine.Object)(object)val6);
					continue;
				}
				MeshRenderer val7 = (MeshRenderer)(object)((val4 is MeshRenderer) ? val4 : null);
				if (val7 != null)
				{
					MeshFilter component = ((Component)val4).GetComponent<MeshFilter>();
					if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null && (UnityEngine.Object)(object)component.sharedMesh != (UnityEngine.Object)null)
					{
						Graphics.DrawMesh(component.sharedMesh, ((Component)val7).transform.localToWorldMatrix, _chamsMaterial, ((Component)val4).gameObject.layer);
					}
				}
			}
		}
	}

	public static void Cleanup()
	{
		if ((UnityEngine.Object)(object)_chamsMaterial != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_chamsMaterial);
			_chamsMaterial = null;
		}
	}
}
