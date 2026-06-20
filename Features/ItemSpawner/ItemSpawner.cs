using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace Cheat.Features.ItemSpawner;

public class ItemSpawner : MonoBehaviour
{
	public class SpawnableItemDef
	{
		public string Name;

		public string NativeId;

		public string ResourcePath;

		public Func<GameObject> PrefabGetter;

		public Func<Vector3, Quaternion, bool> SpawnAction;
	}

	private sealed class PendingSpawnRequest
	{
		public SpawnableItemDef ItemDef;

		public Vector3 Position;

		public Quaternion Rotation;

		public int RetriesRemaining;

		public float NextAttemptTime;
	}

	public static ItemSpawner Instance;

	public List<SpawnableItemDef> SpawnableItems = new List<SpawnableItemDef>();

	public List<SpawnableItemDef> EquipmentItems = new List<SpawnableItemDef>();

	public SpawnableItemDef SelectedItem;

	public SpawnableItemDef SelectedEquipment;

	private readonly HashSet<string> _valuablePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _equipmentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private bool _valuableListVerified;

	private string _searchQuery = "";

	private string _equipmentSearchQuery = "";

	private readonly List<PendingSpawnRequest> _pendingSpawnRequests = new List<PendingSpawnRequest>();

	private const int MaxSpawnAttempts = 3;

	private const int MaxSpawnsPerUpdate = 3;

	private const float SpawnRetryDelay = 0.08f;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		CacheItems();
	}

	private void Update()
	{
		ProcessPendingSpawns();
	}

	public void CacheItems()
	{
		string nativeId = SelectedItem?.NativeId;
		string nativeId2 = SelectedEquipment?.NativeId;
		SpawnableItems.Clear();
		EquipmentItems.Clear();
		_valuablePaths.Clear();
		_equipmentPaths.Clear();
		_valuableListVerified = false;
		AddFixedValuables();
		AddFixedEquipment();
		SpawnableItems.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
		EquipmentItems.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
		SelectedItem = FindByNativeId(SpawnableItems, nativeId) ?? FindByResourcePath(SpawnableItems, SelectedItem?.ResourcePath) ?? SpawnableItems.FirstOrDefault();
		SelectedEquipment = FindByNativeId(EquipmentItems, nativeId2) ?? FindByResourcePath(EquipmentItems, SelectedEquipment?.ResourcePath) ?? EquipmentItems.FirstOrDefault();
	}

	private SpawnableItemDef FindByResourcePath(List<SpawnableItemDef> items, string resourcePath)
	{
		if (string.IsNullOrWhiteSpace(resourcePath))
		{
			return null;
		}
		return items.FirstOrDefault(item => string.Equals(item.ResourcePath, resourcePath, StringComparison.OrdinalIgnoreCase));
	}

	private SpawnableItemDef FindByNativeId(List<SpawnableItemDef> items, string nativeId)
	{
		if (string.IsNullOrWhiteSpace(nativeId))
		{
			return null;
		}
		return items.FirstOrDefault(item => string.Equals(item.NativeId, nativeId, StringComparison.OrdinalIgnoreCase));
	}

	private void AddFixedValuables()
	{
		AddDefinitions(SpawnableItems, _valuablePaths, FixedSpawnRegistry.BuildValuableDefs(), NativeSpawnResolver.ResolveVerifiedValuable);
	}

	private void AddFixedEquipment()
	{
		AddDefinitions(EquipmentItems, _equipmentPaths, FixedSpawnRegistry.BuildEquipmentDefs(), NativeSpawnResolver.ResolveEquipment);
	}

	private void AddDefinitions(List<SpawnableItemDef> targetItems, HashSet<string> knownPaths, IEnumerable<SpawnableItemDef> sourceItems, Func<SpawnableItemDef, SpawnableItemDef> resolver)
	{
		foreach (SpawnableItemDef sourceItem in sourceItems)
		{
			SpawnableItemDef spawnableItemDef = resolver?.Invoke(sourceItem) ?? sourceItem;
			string text = spawnableItemDef?.ResourcePath ?? spawnableItemDef?.NativeId;
			if (spawnableItemDef != null && !string.IsNullOrWhiteSpace(text) && knownPaths.Add(text))
			{
				targetItems.Add(spawnableItemDef);
			}
		}
	}

	private bool MatchesQuery(SpawnableItemDef item, string query)
	{
		if (item == null)
		{
			return false;
		}
		return ContainsIgnoreCase(item.Name, query) || ContainsIgnoreCase(item.ResourcePath, query) || ContainsIgnoreCase(item.NativeId, query);
	}

	private void EnsureListsReady()
	{
		if (SpawnableItems.Count == 0 && (UnityEngine.Object)(object)ValuableDirector.instance != (UnityEngine.Object)null)
		{
			CacheItems();
			return;
		}
		VerifyValuableEntriesIfReady();
		if (EquipmentItems.Count == 0 && ((UnityEngine.Object)(object)ShopManager.instance != (UnityEngine.Object)null || (UnityEngine.Object)(object)ItemManager.instance != (UnityEngine.Object)null))
		{
			CacheItems();
		}
	}

	private void VerifyValuableEntriesIfReady()
	{
		if (_valuableListVerified || SpawnableItems.Count == 0 || (UnityEngine.Object)(object)ValuableDirector.instance == (UnityEngine.Object)null)
		{
			return;
		}
		List<SpawnableItemDef> list = new List<SpawnableItemDef>(SpawnableItems.Count);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (SpawnableItemDef spawnableItem in SpawnableItems)
		{
			SpawnableItemDef spawnableItemDef = NativeSpawnResolver.ResolveVerifiedValuable(spawnableItem);
			string text = spawnableItemDef?.ResourcePath ?? spawnableItemDef?.NativeId;
			if (spawnableItemDef == null || string.IsNullOrWhiteSpace(text) || spawnableItemDef.SpawnAction == null || !hashSet.Add(text))
			{
				continue;
			}
			spawnableItemDef.Name = spawnableItem.Name;
			list.Add(spawnableItemDef);
		}
		if (list.Count == 0)
		{
			return;
		}
		SpawnableItems = list.OrderBy(item => item.Name, StringComparer.Ordinal).ToList();
		_valuablePaths.Clear();
		foreach (SpawnableItemDef spawnableItem2 in SpawnableItems)
		{
			string text2 = spawnableItem2?.ResourcePath ?? spawnableItem2?.NativeId;
			if (!string.IsNullOrWhiteSpace(text2))
			{
				_valuablePaths.Add(text2);
			}
		}
		SelectedItem = FindByNativeId(SpawnableItems, SelectedItem?.NativeId) ?? FindByResourcePath(SpawnableItems, SelectedItem?.ResourcePath) ?? SpawnableItems.FirstOrDefault();
		_valuableListVerified = true;
	}

	private bool ContainsIgnoreCase(string source, string value)
	{
		return !string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(value) && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public void SpawnSelectedItem()
	{
		if (SelectedItem != null)
		{
			SpawnItem(SelectedItem);
		}
	}

	public void SpawnSelectedEquipment()
	{
		if (SelectedEquipment != null)
		{
			SpawnItem(SelectedEquipment);
		}
	}

	public void SpawnItem(SpawnableItemDef itemDef)
	{
		if (itemDef == null)
		{
			return;
		}
		Camera main = Camera.main;
		if ((UnityEngine.Object)(object)main == (UnityEngine.Object)null)
		{
			Debug.LogError((object)"Cannot spawn item: Camera.main is null.");
			return;
		}
		Ray ray = new Ray(((Component)main).transform.position, ((Component)main).transform.forward);
		RaycastHit hitInfo;
		Vector3 position = (!Physics.Raycast(ray, out hitInfo, 10f, LayerMask.GetMask(new string[2] { "Default", "StaticGrabObject" })) ? (((Component)main).transform.position + ((Component)main).transform.forward * 2f) : (hitInfo.point + Vector3.up * 0.5f));
		EnqueueSpawn(itemDef, position, Quaternion.identity);
	}

	public void SpawnItemAt(SpawnableItemDef itemDef, Vector3 position, Quaternion rotation)
	{
		EnqueueSpawn(itemDef, position, rotation);
	}

	private void EnqueueSpawn(SpawnableItemDef itemDef, Vector3 position, Quaternion rotation)
	{
		if (itemDef == null)
		{
			return;
		}
		_pendingSpawnRequests.Add(new PendingSpawnRequest
		{
			ItemDef = itemDef,
			Position = position,
			Rotation = rotation,
			RetriesRemaining = MaxSpawnAttempts - 1,
			NextAttemptTime = Time.unscaledTime
		});
	}

	private void ProcessPendingSpawns()
	{
		if (_pendingSpawnRequests.Count == 0)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		int num = 0;
		for (int i = 0; i < _pendingSpawnRequests.Count && num < MaxSpawnsPerUpdate; )
		{
			PendingSpawnRequest pendingSpawnRequest = _pendingSpawnRequests[i];
			if (unscaledTime < pendingSpawnRequest.NextAttemptTime)
			{
				i++;
				continue;
			}
			num++;
			if (TrySpawnItemAt(pendingSpawnRequest.ItemDef, pendingSpawnRequest.Position, pendingSpawnRequest.Rotation))
			{
				_pendingSpawnRequests.RemoveAt(i);
				continue;
			}
			if (pendingSpawnRequest.RetriesRemaining > 0)
			{
				pendingSpawnRequest.RetriesRemaining--;
				pendingSpawnRequest.NextAttemptTime = unscaledTime + SpawnRetryDelay;
				_pendingSpawnRequests[i] = pendingSpawnRequest;
				i++;
				continue;
			}
			string text = pendingSpawnRequest.ItemDef?.ResourcePath ?? pendingSpawnRequest.ItemDef?.NativeId ?? pendingSpawnRequest.ItemDef?.Name ?? "Unknown";
			Debug.LogError((object)("Failed to spawn item after retries: " + text));
			_pendingSpawnRequests.RemoveAt(i);
		}
	}

	private bool TrySpawnItemAt(SpawnableItemDef itemDef, Vector3 position, Quaternion rotation)
	{
		if (itemDef == null)
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(itemDef.ResourcePath) && itemDef.ResourcePath.StartsWith("Valuables/", StringComparison.OrdinalIgnoreCase))
		{
			itemDef = NativeSpawnResolver.ResolveVerifiedValuable(itemDef) ?? itemDef;
		}
		string resourcePath = itemDef.ResourcePath;
		try
		{
			Func<Vector3, Quaternion, bool> spawnAction = itemDef.SpawnAction;
			if (spawnAction != null && spawnAction(position, rotation))
			{
				return true;
			}
			if (SemiFunc.IsMultiplayer())
			{
				if (string.IsNullOrWhiteSpace(resourcePath))
				{
					Debug.LogError((object)("Cannot spawn " + itemDef.Name + " in multiplayer: resource path is empty."));
					return false;
				}
				PhotonNetwork.Instantiate(resourcePath, position, rotation, (byte)0, null);
				return true;
			}
			GameObject prefab = itemDef.PrefabGetter?.Invoke();
			if ((UnityEngine.Object)(object)prefab == (UnityEngine.Object)null && !string.IsNullOrWhiteSpace(resourcePath))
			{
				prefab = Resources.Load<GameObject>(resourcePath);
			}
			if ((UnityEngine.Object)(object)prefab == (UnityEngine.Object)null)
			{
				Debug.LogError((object)("Failed to load prefab for " + itemDef.Name + " at path: " + resourcePath));
				return false;
			}
			UnityEngine.Object.Instantiate(prefab, position, rotation);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("Failed to spawn " + itemDef.Name + " (" + resourcePath + "): " + ex));
			return false;
		}
	}

	public void SelectItem(SpawnableItemDef item)
	{
		SelectedItem = item;
	}

	public void SelectEquipment(SpawnableItemDef item)
	{
		SelectedEquipment = item;
	}

	public List<SpawnableItemDef> GetFilteredItems()
	{
		EnsureListsReady();
		if (string.IsNullOrEmpty(_searchQuery))
		{
			return SpawnableItems;
		}
		return SpawnableItems.Where(i => MatchesQuery(i, _searchQuery)).ToList();
	}

	public List<SpawnableItemDef> GetFilteredEquipmentItems()
	{
		EnsureListsReady();
		if (string.IsNullOrEmpty(_equipmentSearchQuery))
		{
			return EquipmentItems;
		}
		return EquipmentItems.Where(i => MatchesQuery(i, _equipmentSearchQuery)).ToList();
	}

	public void UpdateSearch(string query)
	{
		_searchQuery = query;
	}

	public void UpdateEquipmentSearch(string query)
	{
		_equipmentSearchQuery = query;
	}

	public string GetSearchQuery()
	{
		return _searchQuery;
	}

	public string GetEquipmentSearchQuery()
	{
		return _equipmentSearchQuery;
	}
}
