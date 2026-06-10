using UnityEngine;

namespace Cheat.Features.Minimap;

public class MinimapRenderHook : MonoBehaviour
{
	private void OnPreCull()
	{
		if ((UnityEngine.Object)(object)Minimap.Instance != (UnityEngine.Object)null)
		{
			Minimap.Instance.OnPreMinimapRender();
		}
	}

	private void OnPostRender()
	{
		if ((UnityEngine.Object)(object)Minimap.Instance != (UnityEngine.Object)null)
		{
			Minimap.Instance.OnPostMinimapRender();
		}
	}
}
