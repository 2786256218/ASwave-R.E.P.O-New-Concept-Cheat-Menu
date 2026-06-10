using UnityEngine;

namespace Cheat.Utils;

public static class ShaderUtils
{
	private static Shader _cachedShader;

	public static Shader GetChamsShader()
	{
		if ((UnityEngine.Object)(object)_cachedShader != (UnityEngine.Object)null)
		{
			return _cachedShader;
		}
		_cachedShader = Shader.Find("Hidden/Internal-Colored");
		if ((UnityEngine.Object)(object)_cachedShader != (UnityEngine.Object)null)
		{
			Logger.Info("Found shader: Hidden/Internal-Colored", "ShaderUtils");
		}
		if ((UnityEngine.Object)(object)_cachedShader == (UnityEngine.Object)null)
		{
			_cachedShader = Shader.Find("GUI/Text Shader");
			if ((UnityEngine.Object)(object)_cachedShader != (UnityEngine.Object)null)
			{
				Logger.Info("Found shader: GUI/Text Shader", "ShaderUtils");
			}
		}
		if ((UnityEngine.Object)(object)_cachedShader == (UnityEngine.Object)null)
		{
			Logger.Error("Failed to find any suitable shader for Chams!", "ShaderUtils");
		}
		return _cachedShader;
	}

	public static Material CreateChamsMaterial(Color color)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		Shader chamsShader = GetChamsShader();
		if ((UnityEngine.Object)(object)chamsShader == (UnityEngine.Object)null)
		{
			return null;
		}
		Material val = new Material(chamsShader);
		((UnityEngine.Object)val).hideFlags = (HideFlags)61;
		val.SetInt("_ZTest", 8);
		val.SetInt("_SrcBlend", 5);
		val.SetInt("_DstBlend", 10);
		val.SetInt("_Cull", 0);
		val.SetInt("_ZWrite", 0);
		val.renderQueue = 4000;
		val.color = color;
		if (val.HasProperty("_Color"))
		{
			val.SetColor("_Color", color);
		}
		else if (val.HasProperty("_TintColor"))
		{
			val.SetColor("_TintColor", color);
		}
		else if (val.HasProperty("_BaseColor"))
		{
			val.SetColor("_BaseColor", color);
		}
		return val;
	}
}
