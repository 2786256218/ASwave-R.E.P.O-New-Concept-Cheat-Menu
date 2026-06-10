using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace Cheat.Utils;

public static class EnemyHealthResolver
{
	private static readonly Dictionary<Type, FieldInfo> SyncedHealthFieldCache = new Dictionary<Type, FieldInfo>();

	public static bool TryGetDisplayHealth(Enemy enemy, EnemyHealth healthComponent, out int currentHealth, out int maxHealth)
	{
		currentHealth = 0;
		maxHealth = 0;
		if ((UnityEngine.Object)(object)healthComponent == (UnityEngine.Object)null)
		{
			return false;
		}

		maxHealth = healthComponent.health;
		if (maxHealth <= 0)
		{
			return false;
		}

		bool dead = ReflectionUtils.GetFieldValue<bool>(healthComponent, "dead");
		currentHealth = Mathf.Clamp(ReflectionUtils.GetFieldValue<int>(healthComponent, "healthCurrent"), 0, maxHealth);
		if (dead)
		{
			currentHealth = 0;
			return true;
		}

		if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
		{
			return true;
		}

		if (TryGetSyncedHealth(enemy, maxHealth, out int syncedHealth))
		{
			currentHealth = syncedHealth;
		}

		return true;
	}

	private static bool TryGetSyncedHealth(Enemy enemy, int maxHealth, out int syncedHealth)
	{
		syncedHealth = 0;
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return false;
		}

		MonoBehaviour[] components = ((Component)enemy).GetComponents<MonoBehaviour>();
		for (int i = 0; i < components.Length; i++)
		{
			MonoBehaviour component = components[i];
			if ((UnityEngine.Object)(object)component == (UnityEngine.Object)null)
			{
				continue;
			}

			FieldInfo syncedField = GetSyncedHealthField(component.GetType());
			if (syncedField == null || syncedField.FieldType != typeof(int))
			{
				continue;
			}

			try
			{
				int value = (int)syncedField.GetValue(component);
				if (value >= 0 && value <= maxHealth)
				{
					syncedHealth = value;
					return true;
				}
			}
			catch
			{
			}
		}

		return false;
	}

	private static FieldInfo GetSyncedHealthField(Type componentType)
	{
		if (componentType == null)
		{
			return null;
		}

		if (SyncedHealthFieldCache.TryGetValue(componentType, out FieldInfo field))
		{
			return field;
		}

		field = componentType.GetField("syncedHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		SyncedHealthFieldCache[componentType] = field;
		return field;
	}
}
