using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Cheat.Utils;

public static class EnemyNameResolver
{
	private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly FieldInfo SpawnObjectsField = typeof(EnemySetup).GetField("spawnObjects", InstanceFlags);

	private static readonly Dictionary<string, string> ExactAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "apexpredator", "顶级掠食者" },
		{ "shadowchild", "影子小孩" },
		{ "duckling", "小鸭子" },
		{ "shopkeycarddoor", "门禁门" },
		{ "truckdoor", "卡车门" }
	};

	private static readonly Dictionary<string, string> TermAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "Enemy", "敌人" },
		{ "Monster", "怪物" },
		{ "Apex", "顶级" },
		{ "Predator", "掠食者" },
		{ "Shadow", "影子" },
		{ "Child", "小孩" },
		{ "Duck", "鸭子" },
		{ "Duckling", "小鸭子" },
		{ "Clown", "小丑" },
		{ "Reaper", "死神" },
		{ "Robe", "长袍" },
		{ "Hunter", "猎手" },
		{ "Hidden", "潜伏者" },
		{ "Head", "头颅" },
		{ "Beast", "野兽" },
		{ "Animal", "野兽" },
		{ "Watcher", "观察者" },
		{ "Patient", "病人" },
		{ "Crawler", "爬行者" },
		{ "Spewer", "喷吐者" },
		{ "Chef", "厨师" },
		{ "Gnome", "地精" }
	};

	public static string GetEnemyDisplayName(Enemy enemy, EnemyParent parent = null)
	{
		if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
		{
			return "未知怪物";
		}

		if ((UnityEngine.Object)(object)parent == (UnityEngine.Object)null)
		{
			parent = ((Component)enemy).GetComponentInParent<EnemyParent>();
		}

		string localizedName = GetLocalizedParentName(parent);
		if (!string.IsNullOrWhiteSpace(localizedName))
		{
			return NormalizeDisplayName(localizedName);
		}

		return ResolveBestName(new string[4]
		{
			((UnityEngine.Object)(object)parent != (UnityEngine.Object)null) ? parent.enemyName : null,
			((UnityEngine.Object)enemy).name,
			((Component)enemy).gameObject.name,
			enemy.GetType().Name
		}, "未知怪物");
	}

	public static string GetSetupDisplayName(EnemySetup setup)
	{
		if ((UnityEngine.Object)(object)setup == (UnityEngine.Object)null)
		{
			return "未知怪物";
		}

		List<string> candidates = new List<string>
		{
			((UnityEngine.Object)setup).name
		};
		List<GameObject> spawnObjects = GetSpawnObjects(setup);
		if (spawnObjects != null)
		{
			foreach (GameObject spawnObject in spawnObjects)
			{
				if (!((UnityEngine.Object)(object)spawnObject == (UnityEngine.Object)null))
				{
					candidates.Add(((UnityEngine.Object)spawnObject).name);
				}
			}
		}

		return ResolveBestName(candidates, "未知怪物");
	}

	private static string ResolveBestName(IEnumerable<string> candidates, string fallback)
	{
		foreach (string candidate in candidates)
		{
			string normalized = NormalizeDisplayName(candidate);
			if (!IsInvalidDisplayName(normalized))
			{
				return normalized;
			}
		}

		return fallback;
	}

	private static string GetLocalizedParentName(EnemyParent parent)
	{
		if ((UnityEngine.Object)(object)parent == (UnityEngine.Object)null || (UnityEngine.Object)(object)parent.enemyNameLocalized == (UnityEngine.Object)null)
		{
			return null;
		}

		try
		{
			string localized = parent.enemyNameLocalized.GetLocalizedString();
			return string.IsNullOrWhiteSpace(localized) ? null : localized.Trim();
		}
		catch
		{
			return null;
		}
	}

	private static List<GameObject> GetSpawnObjects(EnemySetup setup)
	{
		if ((UnityEngine.Object)(object)setup == (UnityEngine.Object)null || SpawnObjectsField == null)
		{
			return null;
		}

		try
		{
			return SpawnObjectsField.GetValue(setup) as List<GameObject>;
		}
		catch
		{
			return null;
		}
	}

	private static string NormalizeDisplayName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		string trimmed = value.Trim().Replace("(Clone)", string.Empty).Trim();
		trimmed = trimmed.Replace("Enemy - ", string.Empty).Trim();

		int separatorIndex = Math.Max(trimmed.LastIndexOf('/'), trimmed.LastIndexOf('\\'));
		if (separatorIndex >= 0 && separatorIndex < trimmed.Length - 1)
		{
			trimmed = trimmed.Substring(separatorIndex + 1);
		}

		char previous = '\0';
		System.Text.StringBuilder builder = new System.Text.StringBuilder(trimmed.Length + 8);
		for (int i = 0; i < trimmed.Length; i++)
		{
			char current = trimmed[i];
			if (current == '_' || current == '-' || current == '.')
			{
				current = ' ';
			}

			if (builder.Length > 0 && current != ' ' && previous != '\0' && char.IsLetterOrDigit(previous) && char.IsLetterOrDigit(current) && ((char.IsLower(previous) && char.IsUpper(current)) || (char.IsLetter(previous) && char.IsDigit(current)) || (char.IsDigit(previous) && char.IsLetter(current))))
			{
				builder.Append(' ');
			}

			if (current == ' ' && (builder.Length == 0 || builder[builder.Length - 1] == ' '))
			{
				previous = current;
				continue;
			}

			builder.Append(current);
			previous = current;
		}

		string normalized = builder.ToString().Trim();
		if (normalized.Length == 0)
		{
			return string.Empty;
		}

		string looseKey = NormalizeLooseKey(normalized);
		if (!string.IsNullOrWhiteSpace(looseKey) && ExactAliases.TryGetValue(looseKey, out string exactAlias))
		{
			return exactAlias;
		}

		foreach (KeyValuePair<string, string> alias in TermAliases)
		{
			normalized = ReplaceIgnoreCase(normalized, alias.Key, alias.Value);
		}

		while (normalized.Contains("  "))
		{
			normalized = normalized.Replace("  ", " ");
		}

		return normalized.Trim();
	}

	private static bool IsInvalidDisplayName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return true;
		}

		string lowered = value.Trim().ToLowerInvariant();
		return lowered == "error" || lowered == "null" || lowered == "none" || lowered == "unknown" || lowered.Contains("missing");
	}

	private static string NormalizeLooseKey(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		char[] buffer = new char[value.Length];
		int index = 0;
		for (int i = 0; i < value.Length; i++)
		{
			char current = char.ToLowerInvariant(value[i]);
			if (char.IsLetterOrDigit(current))
			{
				buffer[index++] = current;
			}
		}

		return index == 0 ? null : new string(buffer, 0, index);
	}

	private static string ReplaceIgnoreCase(string source, string search, string replacement)
	{
		if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(search))
		{
			return source;
		}

		int startIndex = 0;
		while (true)
		{
			int matchIndex = source.IndexOf(search, startIndex, StringComparison.OrdinalIgnoreCase);
			if (matchIndex < 0)
			{
				break;
			}

			source = source.Substring(0, matchIndex) + replacement + source.Substring(matchIndex + search.Length);
			startIndex = matchIndex + replacement.Length;
		}

		return source;
	}
}
