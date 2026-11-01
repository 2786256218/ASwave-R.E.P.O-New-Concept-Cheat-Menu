using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cheat.Utils;

public static class EspProjectionUtils
{
	private static readonly Vector3[] BoundsPointCache = new Vector3[8];
	private static readonly int[,] BoundsEdges = new int[,] { { 0, 1 }, { 0, 2 }, { 0, 4 }, { 1, 3 }, { 1, 5 }, { 2, 3 }, { 2, 6 }, { 3, 7 }, { 4, 5 }, { 4, 6 }, { 5, 7 }, { 6, 7 } };

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
			if (!IntersectsScreen(rect))
				return false;
			rect = ClipToScreen(rect);
			return rect.width > 0f && rect.height > 0f;
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
		if (!IntersectsScreen(rect))
			return false;
		rect = ClipToScreen(rect);
		return rect.width > 0f && rect.height > 0f;
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
		if (TryGetRendererBounds(renderers, rendererFilter, out bounds) || TryGetMeshBounds(root, out bounds) || TryGetColliderBounds(root, out bounds) || TryGetCharacterBounds(root, out bounds))
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
			if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
			{
				continue;
			}

			if (rendererFilter != null && !rendererFilter(renderer))
			{
				continue;
			}

			Bounds rendererBounds;
			if (!TryGetRendererWorldBounds(renderer, out rendererBounds) || !IsValidBounds(rendererBounds))
			{
				continue;
			}

			if (!hasBounds)
			{
				bounds = rendererBounds;
				hasBounds = true;
			}
			else
			{
				bounds.Encapsulate(rendererBounds);
			}
		}

		return hasBounds && bounds.size.sqrMagnitude > 0.001f;
	}

	private static bool TryProjectBounds(Camera camera, Bounds bounds, out Rect rect)
	{
		rect = default(Rect);
		if (!IsValidBounds(bounds))
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

		bool hasProjectedPoint = false;
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		for (int i = 0; i < BoundsPointCache.Length; i++)
		{
			Vector3 cameraPoint = camera.transform.InverseTransformPoint(BoundsPointCache[i]);
			if (cameraPoint.z < camera.nearClipPlane)
			{
				continue;
			}
			IncludeScreenPoint(camera, BoundsPointCache[i], ref minX, ref minY, ref maxX, ref maxY, ref hasProjectedPoint);
		}

		// Clip the 12 AABB edges against the near plane. Without this, a close
		// bounds can lose all projected corners for one frame and drop its outline.
		for (int i = 0; i < 12; i++)
		{
			Vector3 a = BoundsPointCache[BoundsEdges[i, 0]];
			Vector3 b = BoundsPointCache[BoundsEdges[i, 1]];
			float za = camera.transform.InverseTransformPoint(a).z;
			float zb = camera.transform.InverseTransformPoint(b).z;
			bool aFront = za >= camera.nearClipPlane;
			bool bFront = zb >= camera.nearClipPlane;
			if (aFront == bFront)
				continue;
			float t = (camera.nearClipPlane - za) / (zb - za);
			IncludeScreenPoint(camera, Vector3.Lerp(a, b, t), ref minX, ref minY, ref maxX, ref maxY, ref hasProjectedPoint);
		}

		rect = hasProjectedPoint ? Rect.MinMaxRect(minX, minY, maxX, maxY) : default(Rect);
		return hasProjectedPoint;
	}

	private static void IncludeScreenPoint(Camera camera, Vector3 worldPoint, ref float minX, ref float minY, ref float maxX, ref float maxY, ref bool hasPoint)
	{
		Vector3 screen = MathUtils.WorldToScreen(camera, worldPoint);
		if (screen.z <= 0f || float.IsNaN(screen.x) || float.IsNaN(screen.y) || float.IsInfinity(screen.x) || float.IsInfinity(screen.y))
			return;
		minX = Mathf.Min(minX, screen.x);
		minY = Mathf.Min(minY, screen.y);
		maxX = Mathf.Max(maxX, screen.x);
		maxY = Mathf.Max(maxY, screen.y);
		hasPoint = true;
	}

	private static bool TryProjectRenderers(Camera camera, IEnumerable<Renderer> renderers, Func<Renderer, bool> rendererFilter, out Rect rect)
	{
		rect = default(Rect);
		bool hasRect = false;
		foreach (Renderer renderer in renderers ?? Array.Empty<Renderer>())
		{
			if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
			{
				continue;
			}

			if (rendererFilter != null && !rendererFilter(renderer))
			{
				continue;
			}

			Bounds rendererBounds;
			if (!TryGetRendererWorldBounds(renderer, out rendererBounds) || !TryProjectBounds(camera, rendererBounds, out Rect currentRect))
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

	private static bool TryGetMeshBounds(Transform root, out Bounds bounds)
	{
		bounds = default(Bounds);
		if ((UnityEngine.Object)(object)root == (UnityEngine.Object)null)
			return false;
		MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
		bool found = false;
		for (int i = 0; i < filters.Length; i++)
		{
			MeshFilter filter = filters[i];
			if ((UnityEngine.Object)(object)filter == (UnityEngine.Object)null || !filter.gameObject.activeInHierarchy || (UnityEngine.Object)(object)filter.sharedMesh == (UnityEngine.Object)null)
				continue;
			Bounds worldBounds;
			if (!TryTransformBounds(filter.sharedMesh.bounds, filter.transform, out worldBounds))
				continue;
			if (!found)
				bounds = worldBounds;
			else
				bounds.Encapsulate(worldBounds);
			found = true;
		}
		return found;
	}

	private static bool TryGetRendererWorldBounds(Renderer renderer, out Bounds bounds)
	{
		bounds = default(Bounds);
		if ((UnityEngine.Object)(object)renderer == (UnityEngine.Object)null)
			return false;
		if (IsValidBounds(renderer.bounds))
		{
			bounds = renderer.bounds;
			return true;
		}

		SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
		if ((UnityEngine.Object)(object)skinned != (UnityEngine.Object)null)
			return TryTransformBounds(skinned.localBounds, skinned.transform, out bounds);

		MeshFilter filter = ((Component)renderer).GetComponent<MeshFilter>();
		return (UnityEngine.Object)(object)filter != (UnityEngine.Object)null
			&& (UnityEngine.Object)(object)filter.sharedMesh != (UnityEngine.Object)null
			&& TryTransformBounds(filter.sharedMesh.bounds, filter.transform, out bounds);
	}

	private static bool TryTransformBounds(Bounds localBounds, Transform transform, out Bounds worldBounds)
	{
		worldBounds = default(Bounds);
		Vector3 center = localBounds.center;
		Vector3 extents = localBounds.extents;
		Vector3 first = transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z));
		worldBounds = new Bounds(first, Vector3.zero);
		for (int x = -1; x <= 1; x += 2)
			for (int y = -1; y <= 1; y += 2)
				for (int z = -1; z <= 1; z += 2)
					worldBounds.Encapsulate(transform.TransformPoint(center + Vector3.Scale(extents, new Vector3(x, y, z))));
		return IsValidBounds(worldBounds);
	}

	private static Rect ExpandRectToMinimum(Rect rect, float minWidth, float minHeight)
	{
		float width = Mathf.Max(rect.width, minWidth);
		float height = Mathf.Max(rect.height, minHeight);
		return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
	}

	private static Rect FinalizeRect(Camera camera, Rect rect, Vector3 fallbackCenter, Vector3 fallbackSize, float minWidth, float minHeight)
	{
		return ExpandRectToMinimum(rect, minWidth, minHeight);
	}

	private static Rect UnionRects(Rect left, Rect right)
	{
		return Rect.MinMaxRect(Mathf.Min(left.xMin, right.xMin), Mathf.Min(left.yMin, right.yMin), Mathf.Max(left.xMax, right.xMax), Mathf.Max(left.yMax, right.yMax));
	}

	private static bool IsValidBounds(Bounds bounds)
	{
		Vector3 size = bounds.size;
		Vector3 center = bounds.center;
		return !float.IsNaN(center.x) && !float.IsNaN(center.y) && !float.IsNaN(center.z)
			&& !float.IsInfinity(center.x) && !float.IsInfinity(center.y) && !float.IsInfinity(center.z)
			&& !float.IsNaN(size.x) && !float.IsNaN(size.y) && !float.IsNaN(size.z)
			&& !float.IsInfinity(size.x) && !float.IsInfinity(size.y) && !float.IsInfinity(size.z)
			&& size.sqrMagnitude > 0.0001f;
	}

	private static Rect ClipToScreen(Rect rect)
	{
		float left = Mathf.Clamp(rect.xMin, 0f, Screen.width);
		float right = Mathf.Clamp(rect.xMax, 0f, Screen.width);
		float top = Mathf.Clamp(rect.yMin, 0f, Screen.height);
		float bottom = Mathf.Clamp(rect.yMax, 0f, Screen.height);
		return Rect.MinMaxRect(left, top, Mathf.Max(left, right), Mathf.Max(top, bottom));
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
