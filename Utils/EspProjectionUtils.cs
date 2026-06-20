using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cheat.Utils;

public static class EspProjectionUtils
{
	private static readonly Vector3[] BoundsPointCache = new Vector3[15];

	public static bool TryGetScreenRect(Camera camera, Transform root, IEnumerable<Renderer> renderers, Func<Renderer, bool> rendererFilter, Vector3 fallbackCenter, Vector3 fallbackSize, float minWidth, float minHeight, out Rect rect)
	{
		rect = default(Rect);
		if ((UnityEngine.Object)(object)camera == (UnityEngine.Object)null)
		{
			return false;
		}

		if (TryProjectRenderers(camera, renderers, rendererFilter, out rect))
		{
			rect = FinalizeRect(camera, rect, fallbackCenter, fallbackSize, minWidth, minHeight);
			return IntersectsScreen(rect);
		}

		Bounds bounds = GetWorldBounds(root, renderers, rendererFilter, fallbackCenter, fallbackSize);
		if (!TryProjectBounds(camera, bounds, out rect))
		{
			Vector3 screen = MathUtils.WorldToScreen(camera, bounds.center);
			if (screen.z <= 0.05f)
			{
				return false;
			}

			rect = CreateFallbackRect(new Vector2(screen.x, screen.y), minWidth, minHeight);
		}

		rect = FinalizeRect(camera, rect, fallbackCenter, fallbackSize, minWidth, minHeight);
		return IntersectsScreen(rect);
	}

	public static bool TryGetScreenRect(Camera camera, Bounds bounds, float minWidth, float minHeight, out Rect rect)
	{
		rect = default(Rect);
		if ((UnityEngine.Object)(object)camera == (UnityEngine.Object)null)
		{
			return false;
		}

		if (!TryProjectBounds(camera, bounds, out rect))
		{
			Vector3 screen = MathUtils.WorldToScreen(camera, bounds.center);
			if (screen.z <= 0.05f)
			{
				return false;
			}

			rect = CreateFallbackRect(new Vector2(screen.x, screen.y), minWidth, minHeight);
		}

		rect = FinalizeRect(camera, rect, bounds.center, bounds.size, minWidth, minHeight);
		return IntersectsScreen(rect);
	}

	public static Bounds GetWorldBounds(Transform root, IEnumerable<Renderer> renderers, Func<Renderer, bool> rendererFilter, Vector3 fallbackCenter, Vector3 fallbackSize)
	{
		Bounds bounds;
		if (TryGetCharacterBounds(root, out bounds) || TryGetRendererBounds(renderers, rendererFilter, out bounds) || TryGetColliderBounds(root, out bounds))
		{
			return bounds;
		}

		Vector3 size = fallbackSize.sqrMagnitude > 0.001f ? fallbackSize : new Vector3(0.8f, 1.8f, 0.8f);
		return new Bounds(fallbackCenter, size);
	}

	private static bool TryGetCharacterBounds(Transform root, out Bounds bounds)
	{
		bounds = default(Bounds);
		if ((UnityEngine.Object)(object)root == (UnityEngine.Object)null)
		{
			return false;
		}

		CharacterController characterController = root.GetComponent<CharacterController>();
		if ((UnityEngine.Object)(object)characterController != (UnityEngine.Object)null && characterController.bounds.size.sqrMagnitude > 0.001f)
		{
			bounds = characterController.bounds;
			return true;
		}

		CapsuleCollider capsuleCollider = root.GetComponent<CapsuleCollider>();
		if ((UnityEngine.Object)(object)capsuleCollider != (UnityEngine.Object)null && capsuleCollider.bounds.size.sqrMagnitude > 0.001f)
		{
			bounds = capsuleCollider.bounds;
			return true;
		}

		return false;
	}

	private static bool TryGetColliderBounds(Transform root, out Bounds bounds)
	{
		bounds = default(Bounds);
		if ((UnityEngine.Object)(object)root == (UnityEngine.Object)null)
		{
			return false;
		}

		Collider[] componentsInChildren = root.GetComponentsInChildren<Collider>(true);
		bool hasBounds = false;
		bounds = new Bounds(root.position, Vector3.zero);
		foreach (Collider collider in componentsInChildren)
		{
			if ((UnityEngine.Object)(object)collider == (UnityEngine.Object)null || !collider.enabled || collider.isTrigger)
			{
				continue;
			}

			if (!hasBounds)
			{
				bounds = collider.bounds;
				hasBounds = true;
			}
			else
			{
				bounds.Encapsulate(collider.bounds);
			}
		}

		return hasBounds && bounds.size.sqrMagnitude > 0.001f;
	}

	private static bool TryGetRendererBounds(IEnumerable<Renderer> renderers, Func<Renderer, bool> rendererFilter, out Bounds bounds)
	{
		bounds = default(Bounds);
		bool hasBounds = false;
		foreach (Renderer renderer in renderers ?? Array.Empty<Renderer>())
		{
			if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null)
			{
				continue;
			}

			if (rendererFilter != null && !rendererFilter(renderer))
			{
				continue;
			}

			if (!hasBounds)
			{
				bounds = renderer.bounds;
				hasBounds = true;
			}
			else
			{
				bounds.Encapsulate(renderer.bounds);
			}
		}

		return hasBounds && bounds.size.sqrMagnitude > 0.001f;
	}

	private static bool TryProjectBounds(Camera camera, Bounds bounds, out Rect rect)
	{
		rect = default(Rect);
		if (bounds.size.sqrMagnitude <= 0.0001f)
		{
			return false;
		}

		Vector3 extents = bounds.extents;
		Vector3 center = bounds.center;
		BoundsPointCache[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
		BoundsPointCache[1] = center + new Vector3(-extents.x, -extents.y, extents.z);
		BoundsPointCache[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
		BoundsPointCache[3] = center + new Vector3(-extents.x, extents.y, extents.z);
		BoundsPointCache[4] = center + new Vector3(extents.x, -extents.y, -extents.z);
		BoundsPointCache[5] = center + new Vector3(extents.x, -extents.y, extents.z);
		BoundsPointCache[6] = center + new Vector3(extents.x, extents.y, -extents.z);
		BoundsPointCache[7] = center + new Vector3(extents.x, extents.y, extents.z);
		BoundsPointCache[8] = center;
		BoundsPointCache[9] = center + new Vector3(extents.x, 0f, 0f);
		BoundsPointCache[10] = center + new Vector3(-extents.x, 0f, 0f);
		BoundsPointCache[11] = center + new Vector3(0f, extents.y, 0f);
		BoundsPointCache[12] = center + new Vector3(0f, -extents.y, 0f);
		BoundsPointCache[13] = center + new Vector3(0f, 0f, extents.z);
		BoundsPointCache[14] = center + new Vector3(0f, 0f, -extents.z);

		bool hasProjectedPoint = false;
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		for (int i = 0; i < BoundsPointCache.Length; i++)
		{
			Vector3 screen = MathUtils.WorldToScreen(camera, BoundsPointCache[i]);
			if (screen.z <= 0.05f)
			{
				continue;
			}

			float guiX = screen.x;
			float guiY = screen.y;
			minX = Mathf.Min(minX, guiX);
			minY = Mathf.Min(minY, guiY);
			maxX = Mathf.Max(maxX, guiX);
			maxY = Mathf.Max(maxY, guiY);
			hasProjectedPoint = true;
		}

		rect = hasProjectedPoint ? Rect.MinMaxRect(minX, minY, maxX, maxY) : default(Rect);
		return hasProjectedPoint;
	}

	private static bool TryProjectRenderers(Camera camera, IEnumerable<Renderer> renderers, Func<Renderer, bool> rendererFilter, out Rect rect)
	{
		rect = default(Rect);
		bool hasRect = false;
		foreach (Renderer renderer in renderers ?? Array.Empty<Renderer>())
		{
			if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null)
			{
				continue;
			}

			if (rendererFilter != null && !rendererFilter(renderer))
			{
				continue;
			}

			if (!TryProjectBounds(camera, renderer.bounds, out Rect currentRect))
			{
				continue;
			}

			rect = hasRect ? UnionRects(rect, currentRect) : currentRect;
			hasRect = true;
		}

		return hasRect;
	}

	private static Rect CreateFallbackRect(Vector2 center, float minWidth, float minHeight)
	{
		return new Rect(center.x - minWidth * 0.5f, center.y - minHeight * 0.5f, minWidth, minHeight);
	}

	private static Rect ExpandRectToMinimum(Rect rect, float minWidth, float minHeight)
	{
		float width = Mathf.Max(rect.width, minWidth);
		float height = Mathf.Max(rect.height, minHeight);
		return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
	}

	private static Rect FinalizeRect(Camera camera, Rect rect, Vector3 fallbackCenter, Vector3 fallbackSize, float minWidth, float minHeight)
	{
		rect = ExpandRectToMinimum(rect, minWidth, minHeight);
		if (!IsProjectionRectReasonable(rect))
		{
			if (TryCreateStableRect(camera, fallbackCenter, fallbackSize, minWidth, minHeight, out Rect stableRect))
			{
				return stableRect;
			}
		}

		return rect;
	}

	private static Rect UnionRects(Rect left, Rect right)
	{
		return Rect.MinMaxRect(Mathf.Min(left.xMin, right.xMin), Mathf.Min(left.yMin, right.yMin), Mathf.Max(left.xMax, right.xMax), Mathf.Max(left.yMax, right.yMax));
	}

	private static bool IsProjectionRectReasonable(Rect rect)
	{
		if (rect.width <= 0f || rect.height <= 0f)
		{
			return false;
		}

		float maxWidth = Mathf.Max(Screen.width * 0.72f, 260f);
		float maxHeight = Mathf.Max(Screen.height * 0.82f, 320f);
		float maxArea = (float)(Screen.width * Screen.height) * 0.42f;
		return rect.width <= maxWidth && rect.height <= maxHeight && rect.width * rect.height <= maxArea;
	}

	private static bool TryCreateStableRect(Camera camera, Vector3 center, Vector3 size, float minWidth, float minHeight, out Rect rect)
	{
		rect = default(Rect);
		if ((UnityEngine.Object)(object)camera == (UnityEngine.Object)null)
		{
			return false;
		}

		Vector3 screenCenter = MathUtils.WorldToScreen(camera, center);
		if (screenCenter.z <= Mathf.Max(camera.nearClipPlane * 1.15f, 0.08f))
		{
			return false;
		}

		Vector3 safeSize = size.sqrMagnitude > 0.001f ? size : new Vector3(0.8f, 1.8f, 0.8f);
		float halfHeight = Mathf.Max(safeSize.y * 0.5f, 0.2f);
		float halfWidth = Mathf.Max(Mathf.Max(safeSize.x, safeSize.z) * 0.5f, 0.15f);
		Vector3 top = MathUtils.WorldToScreen(camera, center + camera.transform.up * halfHeight);
		Vector3 bottom = MathUtils.WorldToScreen(camera, center - camera.transform.up * halfHeight);
		Vector3 left = MathUtils.WorldToScreen(camera, center - camera.transform.right * halfWidth);
		Vector3 right = MathUtils.WorldToScreen(camera, center + camera.transform.right * halfWidth);
		if (top.z <= 0.05f || bottom.z <= 0.05f || left.z <= 0.05f || right.z <= 0.05f)
		{
			rect = CreateFallbackRect(new Vector2(screenCenter.x, screenCenter.y), minWidth, minHeight);
			return true;
		}

		float width = Mathf.Abs(right.x - left.x);
		float height = Mathf.Abs(bottom.y - top.y);
		rect = new Rect(screenCenter.x - width * 0.5f, screenCenter.y - height * 0.5f, width, height);
		rect = ExpandRectToMinimum(rect, minWidth, minHeight);
		return true;
	}

	private static bool IntersectsScreen(Rect rect)
	{
		return rect.xMax >= 0f && rect.yMax >= 0f && rect.xMin <= (float)Screen.width && rect.yMin <= (float)Screen.height;
	}
}
