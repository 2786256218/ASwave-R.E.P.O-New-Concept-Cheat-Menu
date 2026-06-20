using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace Cheat.Features.ItemSpawner;

internal static class NativeSpawnResolver
{
	private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

	private static readonly MethodInfo ValuableSpawnMethod = typeof(ValuableDirector).GetMethod("SpawnValuable", InstanceFlags);

	private static readonly MethodInfo ItemNameLocalizedMethod = typeof(ItemAttributes).GetMethod("GetItemNameLocalized", InstanceFlags);

	private static readonly HashSet<string> ValuableListFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"tinyValuables",
		"smallValuables",
		"mediumValuables",
		"bigValuables",
		"tallValuables",
		"veryTallValuables",
		"wideValuables"
	};

	private static readonly List<ItemSpawner.SpawnableItemDef> CachedValuableDefs = new List<ItemSpawner.SpawnableItemDef>();

	private static readonly Dictionary<string, ItemSpawner.SpawnableItemDef> CachedValuableLookup = new Dictionary<string, ItemSpawner.SpawnableItemDef>(PathComparer);

	private static readonly List<ItemSpawner.SpawnableItemDef> CachedEquipmentDefs = new List<ItemSpawner.SpawnableItemDef>();

	private static readonly Dictionary<string, ItemSpawner.SpawnableItemDef> CachedEquipmentLookup = new Dictionary<string, ItemSpawner.SpawnableItemDef>(PathComparer);

	private static float _valuableCacheRefreshAt;

	private static float _equipmentCacheRefreshAt;

	private static int _valuableCacheSourceId;

	private static int _equipmentCacheSourceId;

	private const float CacheRefreshInterval = 1f;

	public static List<ItemSpawner.SpawnableItemDef> BuildResolvedValuableDefs()
	{
		EnsureValuableCache();
		return CachedValuableDefs.Select(CloneDef).ToList();
	}

	public static List<ItemSpawner.SpawnableItemDef> BuildResolvedEquipmentDefs()
	{
		EnsureEquipmentCache();
		return CachedEquipmentDefs.Select(CloneDef).ToList();
	}

	public static ItemSpawner.SpawnableItemDef ResolveVerifiedValuable(ItemSpawner.SpawnableItemDef fixedDef)
	{
		if (fixedDef == null || string.IsNullOrWhiteSpace(fixedDef.ResourcePath))
		{
			return fixedDef;
		}
		EnsureValuableCache();
		ItemSpawner.SpawnableItemDef resolvedDef = FindResolvedDef(CachedValuableLookup, fixedDef.ResourcePath);
		if (resolvedDef != null)
		{
			ItemSpawner.SpawnableItemDef spawnableItemDef = CloneDef(resolvedDef);
			spawnableItemDef.Name = fixedDef.Name;
			return spawnableItemDef;
		}
		return fixedDef;
	}

	public static ItemSpawner.SpawnableItemDef ResolveValuable(ItemSpawner.SpawnableItemDef fixedDef)
	{
		return ResolveVerifiedValuable(fixedDef);
	}

	public static ItemSpawner.SpawnableItemDef ResolveEquipment(ItemSpawner.SpawnableItemDef fixedDef)
	{
		if (fixedDef == null || string.IsNullOrWhiteSpace(fixedDef.ResourcePath))
		{
			return fixedDef;
		}
		EnsureEquipmentCache();
		ItemSpawner.SpawnableItemDef resolvedDef = FindResolvedDef(CachedEquipmentLookup, fixedDef.ResourcePath);
		if (resolvedDef != null)
		{
			ItemSpawner.SpawnableItemDef spawnableItemDef = CloneDef(resolvedDef);
			spawnableItemDef.Name = fixedDef.Name;
			return spawnableItemDef;
		}
		return fixedDef;
	}

	public static bool SpawnNativePrefab(PrefabRef prefabRef, Vector3 position, Quaternion rotation)
	{
		return SpawnPrefab(prefabRef, position, rotation);
	}

	private static List<ItemSpawner.SpawnableItemDef> BuildValuableDefs()
	{
		ValuableDirector instance = ValuableDirector.instance;
		if ((UnityEngine.Object)(object)instance == (UnityEngine.Object)null)
		{
			return new List<ItemSpawner.SpawnableItemDef>();
		}
		Dictionary<string, ItemSpawner.SpawnableItemDef> dictionary = new Dictionary<string, ItemSpawner.SpawnableItemDef>(PathComparer);
		foreach (FieldInfo item in typeof(ValuableDirector).GetFields(InstanceFlags))
		{
			if (!ValuableListFieldNames.Contains(item.Name) || !typeof(List<PrefabRef>).IsAssignableFrom(item.FieldType))
			{
				continue;
			}
			string text = item.Name.Substring(0, item.Name.Length - "Valuables".Length);
			List<PrefabRef> list = item.GetValue(instance) as List<PrefabRef>;
			List<ValuableVolume> volumes = ReflectionUtils.GetFieldValue<List<ValuableVolume>>(instance, text + "Volumes") ?? new List<ValuableVolume>();
			string valuablePath = ReflectionUtils.GetFieldValue<string>(instance, text + "Path");
			if (list == null)
			{
				continue;
			}
			foreach (PrefabRef item2 in list)
			{
				if (TryBuildValuableDef(item2, valuablePath, volumes, out var def) && !string.IsNullOrWhiteSpace(def.ResourcePath))
				{
					dictionary[def.ResourcePath] = def;
				}
			}
		}
		return dictionary.Values.ToList();
	}

	private static List<ItemSpawner.SpawnableItemDef> BuildEquipmentDefs()
	{
		Dictionary<string, ItemSpawner.SpawnableItemDef> dictionary = new Dictionary<string, ItemSpawner.SpawnableItemDef>(PathComparer);
		foreach (Item item in CollectNativeItems())
		{
			if (TryBuildEquipmentDef(item, out var def) && !string.IsNullOrWhiteSpace(def.ResourcePath))
			{
				dictionary[def.ResourcePath] = def;
			}
		}
		return dictionary.Values.ToList();
	}

	private static void EnsureValuableCache()
	{
		ValuableDirector instance = ValuableDirector.instance;
		int num = (((UnityEngine.Object)(object)instance != (UnityEngine.Object)null) ? ((UnityEngine.Object)instance).GetInstanceID() : 0);
		if (CachedValuableDefs.Count != 0 && _valuableCacheSourceId == num && Time.realtimeSinceStartup < _valuableCacheRefreshAt)
		{
			return;
		}
		RebuildCache(BuildValuableDefs(), CachedValuableDefs, CachedValuableLookup);
		_valuableCacheSourceId = num;
		_valuableCacheRefreshAt = Time.realtimeSinceStartup + CacheRefreshInterval;
	}

	private static void EnsureEquipmentCache()
	{
		int equipmentCacheSourceId = GetEquipmentCacheSourceId();
		if (CachedEquipmentDefs.Count != 0 && _equipmentCacheSourceId == equipmentCacheSourceId && Time.realtimeSinceStartup < _equipmentCacheRefreshAt)
		{
			return;
		}
		RebuildCache(BuildEquipmentDefs(), CachedEquipmentDefs, CachedEquipmentLookup);
		_equipmentCacheSourceId = equipmentCacheSourceId;
		_equipmentCacheRefreshAt = Time.realtimeSinceStartup + CacheRefreshInterval;
	}

	private static void RebuildCache(IEnumerable<ItemSpawner.SpawnableItemDef> sourceDefs, List<ItemSpawner.SpawnableItemDef> targetDefs, Dictionary<string, ItemSpawner.SpawnableItemDef> lookup)
	{
		targetDefs.Clear();
		lookup.Clear();
		if (sourceDefs == null)
		{
			return;
		}
		foreach (ItemSpawner.SpawnableItemDef sourceDef in sourceDefs)
		{
			if (sourceDef == null)
			{
				continue;
			}
			targetDefs.Add(sourceDef);
			AddLookupKey(lookup, sourceDef.ResourcePath, sourceDef);
			AddLookupKey(lookup, sourceDef.NativeId, sourceDef);
			AddLookupKey(lookup, NormalizeResourceKey(sourceDef.ResourcePath), sourceDef);
			AddLookupKey(lookup, NormalizeResourceKey(sourceDef.NativeId), sourceDef);
		}
	}

	private static void AddLookupKey(Dictionary<string, ItemSpawner.SpawnableItemDef> lookup, string key, ItemSpawner.SpawnableItemDef def)
	{
		if (!string.IsNullOrWhiteSpace(key) && !lookup.ContainsKey(key))
		{
			lookup[key] = def;
		}
	}

	private static int GetEquipmentCacheSourceId()
	{
		int num = 17;
		ShopManager instance = ShopManager.instance;
		if ((UnityEngine.Object)(object)instance != (UnityEngine.Object)null)
		{
			num = num * 31 + ((UnityEngine.Object)instance).GetInstanceID();
		}
		ItemManager instance2 = ItemManager.instance;
		if ((UnityEngine.Object)(object)instance2 != (UnityEngine.Object)null)
		{
			num = num * 31 + ((UnityEngine.Object)instance2).GetInstanceID();
		}
		return num;
	}

	private static IEnumerable<Item> CollectNativeItems()
	{
		HashSet<Item> hashSet = new HashSet<Item>();
		ShopManager instance = ShopManager.instance;
		if ((UnityEngine.Object)(object)instance != (UnityEngine.Object)null)
		{
			TryAddItems(hashSet, instance.potentialItems);
			TryAddItems(hashSet, instance.potentialItemConsumables);
			TryAddItems(hashSet, instance.potentialItemHealthPacks);
			TryAddItems(hashSet, instance.potentialItemUpgrades);
			if (instance.potentialSecretItems != null)
			{
				foreach (KeyValuePair<SemiFunc.itemSecretShopType, List<Item>> potentialSecretItem in instance.potentialSecretItems)
				{
					TryAddItems(hashSet, potentialSecretItem.Value);
				}
			}
		}
		ItemManager instance2 = ItemManager.instance;
		if ((UnityEngine.Object)(object)instance2 != (UnityEngine.Object)null)
		{
			TryAddItems(hashSet, instance2.purchasedItems);
			if (instance2.spawnedItems != null)
			{
				foreach (ItemAttributes spawnedItem in instance2.spawnedItems)
				{
					if (!((UnityEngine.Object)(object)spawnedItem == (UnityEngine.Object)null))
					{
						TryAddItem(hashSet, spawnedItem.item);
					}
				}
			}
			if (instance2.itemVolumes != null)
			{
				foreach (ItemVolume itemVolume in instance2.itemVolumes)
				{
					ItemAttributes fieldValue = ReflectionUtils.GetFieldValue<ItemAttributes>(itemVolume, "itemAttributes");
					if (!((UnityEngine.Object)(object)itemVolume == (UnityEngine.Object)null) && !((UnityEngine.Object)(object)fieldValue == (UnityEngine.Object)null))
					{
						TryAddItem(hashSet, fieldValue.item);
					}
				}
			}
		}
		return hashSet;
	}

	private static void TryAddItems(HashSet<Item> items, IEnumerable<Item> source)
	{
		if (source == null)
		{
			return;
		}
		foreach (Item item in source)
		{
			TryAddItem(items, item);
		}
	}

	private static void TryAddItem(HashSet<Item> items, Item item)
	{
		if (!((UnityEngine.Object)(object)item == (UnityEngine.Object)null) && !item.disabled && item.prefab != null && item.prefab.IsValid())
		{
			items.Add(item);
		}
	}

	private static bool TryBuildValuableDef(PrefabRef prefabRef, string valuablePath, List<ValuableVolume> volumes, out ItemSpawner.SpawnableItemDef def)
	{
		def = null;
		if (prefabRef == null || !prefabRef.IsValid())
		{
			return false;
		}
		GameObject prefab = prefabRef.Prefab;
		string text = FirstNonEmpty(prefabRef.ResourcePath, prefabRef.PrefabName, ((UnityEngine.Object)(object)prefab != (UnityEngine.Object)null) ? ((UnityEngine.Object)prefab).name : null);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		def = new ItemSpawner.SpawnableItemDef
		{
			Name = BuildValuableDisplayName(prefabRef, prefab),
			ResourcePath = prefabRef.ResourcePath,
			NativeId = text,
			PrefabGetter = () => prefabRef.Prefab,
			SpawnAction = delegate(Vector3 position, Quaternion rotation)
			{
				return SpawnValuable(prefabRef, valuablePath, volumes, position, rotation);
			}
		};
		return true;
	}

	private static string BuildValuableDisplayName(PrefabRef prefabRef, GameObject prefab)
	{
		if ((UnityEngine.Object)(object)prefab != (UnityEngine.Object)null)
		{
			return CleanName(((UnityEngine.Object)prefab).name);
		}
		if (!string.IsNullOrWhiteSpace(prefabRef?.PrefabName))
		{
			return CleanName(prefabRef.PrefabName);
		}
		if (!string.IsNullOrWhiteSpace(prefabRef?.ResourcePath))
		{
			int num = prefabRef.ResourcePath.LastIndexOf('/');
			string value = (num >= 0 && num < prefabRef.ResourcePath.Length - 1) ? prefabRef.ResourcePath.Substring(num + 1) : prefabRef.ResourcePath;
			return CleanName(value);
		}
		return "Unknown";
	}

	private static bool TryBuildEquipmentDef(Item item, out ItemSpawner.SpawnableItemDef def)
	{
		def = null;
		if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null || item.prefab == null || !item.prefab.IsValid())
		{
			return false;
		}
		GameObject prefab = item.prefab.Prefab;
		if ((UnityEngine.Object)(object)prefab == (UnityEngine.Object)null)
		{
			return false;
		}
		string text = FirstNonEmpty(item.prefab.ResourcePath, item.prefab.PrefabName, item.itemName, ((UnityEngine.Object)item).name);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		def = new ItemSpawner.SpawnableItemDef
		{
			Name = BuildEquipmentDisplayName(item, prefab),
			ResourcePath = item.prefab.ResourcePath,
			NativeId = text,
			PrefabGetter = () => item.prefab.Prefab,
			SpawnAction = delegate(Vector3 position, Quaternion rotation)
			{
				return SpawnEquipment(item.prefab, position, rotation);
			}
		};
		return true;
	}

	private static bool SpawnValuable(PrefabRef prefabRef, string valuablePath, List<ValuableVolume> volumes, Vector3 position, Quaternion rotation)
	{
		ValuableDirector instance = ValuableDirector.instance;
		if ((UnityEngine.Object)(object)instance != (UnityEngine.Object)null)
		{
			ValuableVolume valuableVolume = PickNearestVolume(volumes, position);
			if ((UnityEngine.Object)(object)valuableVolume != (UnityEngine.Object)null && !string.IsNullOrWhiteSpace(valuablePath))
			{
				return InvokeSpawnValuable(instance, prefabRef, valuableVolume, valuablePath);
			}
		}
		return SpawnPrefab(prefabRef, position, rotation);
	}

	private static bool SpawnEquipment(PrefabRef prefabRef, Vector3 position, Quaternion rotation)
	{
		return SpawnPrefab(prefabRef, position, rotation);
	}

	private static bool SpawnPrefab(PrefabRef prefabRef, Vector3 position, Quaternion rotation)
	{
		if (prefabRef == null || !prefabRef.IsValid())
		{
			return false;
		}
		string resourcePath = prefabRef.ResourcePath;
		if (SemiFunc.IsMultiplayer())
		{
			if (string.IsNullOrWhiteSpace(resourcePath))
			{
				Debug.LogError((object)"Failed to spawn native prefab in multiplayer: resource path is empty.");
				return false;
			}
			PhotonNetwork.Instantiate(resourcePath, position, rotation, (byte)0, null);
			return true;
		}
		GameObject prefab = prefabRef.Prefab;
		if ((UnityEngine.Object)(object)prefab == (UnityEngine.Object)null && !string.IsNullOrWhiteSpace(resourcePath))
		{
			prefab = Resources.Load<GameObject>(resourcePath);
		}
		if ((UnityEngine.Object)(object)prefab == (UnityEngine.Object)null)
		{
			return false;
		}
		UnityEngine.Object.Instantiate(prefab, position, rotation);
		return true;
	}

	private static ValuableVolume PickNearestVolume(IEnumerable<ValuableVolume> volumes, Vector3 position)
	{
		ValuableVolume valuableVolume = null;
		float num = float.MaxValue;
		if (volumes == null)
		{
			return null;
		}
		foreach (ValuableVolume volume in volumes)
		{
			if ((UnityEngine.Object)(object)volume == (UnityEngine.Object)null)
			{
				continue;
			}
			float sqrMagnitude = (((Component)volume).transform.position - position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				valuableVolume = volume;
			}
		}
		return valuableVolume;
	}

	private static string BuildEquipmentDisplayName(Item item, GameObject prefab)
	{
		if (!string.IsNullOrWhiteSpace(item.itemName))
		{
			return item.itemName.Trim();
		}
		ItemAttributes component = prefab.GetComponent<ItemAttributes>();
		if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
		{
			string itemNameLocalized = GetItemNameLocalized(component);
			if (!string.IsNullOrWhiteSpace(itemNameLocalized))
			{
				return itemNameLocalized.Trim();
			}
			string fieldValue = ReflectionUtils.GetFieldValue<string>(component, "itemName");
			if (!string.IsNullOrWhiteSpace(fieldValue))
			{
				return fieldValue.Trim();
			}
			string fieldValue2 = ReflectionUtils.GetFieldValue<string>(component, "itemAssetName");
			if (!string.IsNullOrWhiteSpace(fieldValue2))
			{
				return fieldValue2.Trim();
			}
		}
		return CleanName(((UnityEngine.Object)prefab).name);
	}

	private static bool InvokeSpawnValuable(ValuableDirector director, PrefabRef prefabRef, ValuableVolume volume, string valuablePath)
	{
		if ((UnityEngine.Object)(object)director == (UnityEngine.Object)null || ValuableSpawnMethod == null)
		{
			return false;
		}
		try
		{
			ValuableSpawnMethod.Invoke(director, new object[3] { prefabRef, volume, valuablePath });
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("Failed to invoke native valuable spawn: " + ex));
			return false;
		}
	}

	private static string GetItemNameLocalized(ItemAttributes itemAttributes)
	{
		if ((UnityEngine.Object)(object)itemAttributes == (UnityEngine.Object)null || ItemNameLocalizedMethod == null)
		{
			return null;
		}
		try
		{
			return ItemNameLocalizedMethod.Invoke(itemAttributes, null) as string;
		}
		catch
		{
			return null;
		}
	}

	private static ItemSpawner.SpawnableItemDef FindResolvedDef(Dictionary<string, ItemSpawner.SpawnableItemDef> lookup, string resourcePath)
	{
		if (lookup == null || string.IsNullOrWhiteSpace(resourcePath))
		{
			return null;
		}
		if (lookup.TryGetValue(resourcePath, out var value))
		{
			return value;
		}
		string normalizedResourcePath = NormalizeResourceKey(resourcePath);
		if (!string.IsNullOrWhiteSpace(normalizedResourcePath) && lookup.TryGetValue(normalizedResourcePath, out value))
		{
			return value;
		}
		return null;
	}

	private static string NormalizeResourceKey(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		string text = value.Trim().Replace('\\', '/');
		int num = text.LastIndexOf('/');
		string text2 = ((num >= 0 && num < text.Length - 1) ? text.Substring(num + 1) : text).Trim();
		return NormalizeLooseKey(text2);
	}

	private static string NormalizeLooseKey(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		char[] array = value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray();
		return (array.Length != 0) ? new string(array) : null;
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string text in values)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		return null;
	}

	private static ItemSpawner.SpawnableItemDef CloneDef(ItemSpawner.SpawnableItemDef source)
	{
		if (source == null)
		{
			return null;
		}
		return new ItemSpawner.SpawnableItemDef
		{
			Name = source.Name,
			NativeId = source.NativeId,
			ResourcePath = source.ResourcePath,
			PrefabGetter = source.PrefabGetter,
			SpawnAction = source.SpawnAction
		};
	}

	private static string CleanName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "Unknown";
		}
		return value.Replace("(Clone)", "").Trim();
	}
}
