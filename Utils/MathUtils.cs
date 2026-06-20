using System;
using UnityEngine;

namespace Cheat.Utils;

public static class MathUtils
{
	private static object _cachedRtMain;

	private static bool _rtMainCached;

	private static Type _rtMainType;

	public static Vector3 WorldToScreen(Vector3 worldPos)
	{
		return WorldToScreen(Camera.main, worldPos);
	}

	public static Vector3 WorldToScreen(Camera camera, Vector3 worldPos)
	{
		if ((UnityEngine.Object)(object)camera == (UnityEngine.Object)null)
		{
			return new Vector3(0f, 0f, -1f);
		}

		Vector3 val = camera.WorldToScreenPoint(worldPos);
		float sourceWidth = 0f;
		float sourceHeight = 0f;
		if (!TryGetRenderTextureDimensions(out sourceWidth, out sourceHeight))
		{
			sourceWidth = camera.pixelWidth;
			sourceHeight = camera.pixelHeight;
		}

		if (sourceWidth > 0f && sourceHeight > 0f)
		{
			val.x *= (float)Screen.width / sourceWidth;
			val.y *= (float)Screen.height / sourceHeight;
		}
		val.y = (float)Screen.height - val.y;
		return val;
	}

	private static bool TryGetRenderTextureDimensions(out float width, out float height)
	{
		width = 0f;
		height = 0f;
		if (!_rtMainCached)
		{
			_rtMainType = Type.GetType("RenderTextureMain, Assembly-CSharp");
			if (_rtMainType == null)
			{
				_rtMainType = Type.GetType("RenderTextureMain");
			}
			if (_rtMainType != null)
			{
				_cachedRtMain = UnityEngine.Object.FindObjectOfType(_rtMainType);
			}
			_rtMainCached = true;
		}

		if (_cachedRtMain == null)
		{
			return false;
		}

		object cachedRtMain = _cachedRtMain;
		if ((UnityEngine.Object)((cachedRtMain is UnityEngine.Object) ? cachedRtMain : null) == (UnityEngine.Object)null)
		{
			if (_rtMainType == null)
			{
				return false;
			}

			_cachedRtMain = UnityEngine.Object.FindObjectOfType(_rtMainType);
			if (_cachedRtMain == null)
			{
				return false;
			}
		}

		width = ReflectionUtils.GetFieldValue<int>(_cachedRtMain, "textureWidth");
		height = ReflectionUtils.GetFieldValue<int>(_cachedRtMain, "textureHeight");
		return width > 0f && height > 0f;
	}

	public static bool IsOnScreen(Vector3 screenPos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		return screenPos.z > 0f && screenPos.x > 0f && screenPos.x < (float)Screen.width && screenPos.y > 0f && screenPos.y < (float)Screen.height;
	}
}
