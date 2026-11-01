using Cheat.Utils;
using UnityEngine;

namespace Cheat.Features.Enemies;

internal static class EnemyVisibilityUtility
{
	public static readonly Color OccludedEspColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);

	private static readonly Vector3[] SampleOffsets = BuildSampleOffsets();
	private static readonly RaycastHit[] RaycastHits = new RaycastHit[64];

	public static Vector3 GetFocusPoint(EnemyManager.EnemyData enemyData)
	{
		if (enemyData == null || (UnityEngine.Object)(object)enemyData.Enemy == (UnityEngine.Object)null)
			return Vector3.zero;
		return GetFocusPoint(enemyData.Enemy);
	}

	public static Vector3 GetFocusPoint(Enemy enemy)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
			return Vector3.zero;
		if ((UnityEngine.Object)(object)enemy.CenterTransform != (UnityEngine.Object)null)
			return enemy.CenterTransform.position;
		return ((Component)enemy).transform.position + Vector3.up;
	}

	/// <summary>
	/// A target is visible if any bounds sample has an unobstructed ray from the
	/// supplied player camera. This avoids hiding a partly exposed monster because
	/// its center happens to sit behind a wall.
	/// </summary>
	public static bool IsEnemyVisible(Enemy enemy, Bounds bounds, Camera camera)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null || (UnityEngine.Object)(object)camera == (UnityEngine.Object)null)
			return false;

		Vector3 origin = camera.transform.position;
		int layerMask = (int)SemiFunc.LayerMaskGetVisionObstruct() & ~LayerMask.GetMask("Ignore Raycast", "CollisionCheck");
		Transform enemyRoot = ((Component)enemy).transform;
		Transform localRoot = ResolveLocalRoot();

		for (int i = 0; i < SampleOffsets.Length; i++)
		{
			Vector3 sample = bounds.center + Vector3.Scale(bounds.extents, SampleOffsets[i]);
			Vector3 delta = sample - origin;
			float distance = delta.magnitude;
			if (distance <= 0.01f)
				return true;

			int hitCount = Physics.RaycastNonAlloc(origin, delta / distance, RaycastHits,
				Mathf.Max(0f, distance - 0.025f), layerMask, QueryTriggerInteraction.Ignore);
			bool blocked = false;
			for (int h = 0; h < hitCount; h++)
			{
				Collider collider = RaycastHits[h].collider;
				if ((UnityEngine.Object)(object)collider == (UnityEngine.Object)null || collider.isTrigger)
					continue;

				Transform hit = collider.transform;
				if (hit == enemyRoot || hit.IsChildOf(enemyRoot))
					continue;
				Enemy parentEnemy = hit.GetComponentInParent<Enemy>();
				if ((UnityEngine.Object)(object)parentEnemy == (UnityEngine.Object)(object)enemy)
					continue;
				if ((UnityEngine.Object)(object)localRoot != (UnityEngine.Object)null && (hit == localRoot || hit.IsChildOf(localRoot)))
					continue;

				blocked = true;
				break;
			}

			if (!blocked)
				return true;
		}

		return false;
	}

	private static Vector3[] BuildSampleOffsets()
	{
		Vector3[] offsets = new Vector3[27];
		int index = 0;
		for (int x = -1; x <= 1; x++)
			for (int y = -1; y <= 1; y++)
				for (int z = -1; z <= 1; z++)
					offsets[index++] = new Vector3(x, y, z);
		return offsets;
	}

	private static Transform ResolveLocalRoot()
	{
		if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
			return ((Component)PlayerController.instance).transform;
		if ((UnityEngine.Object)(object)PlayerAvatar.instance != (UnityEngine.Object)null)
			return ((Component)PlayerAvatar.instance).transform;
		return null;
	}
}
