using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Cheat.Config;

public static class ConfigManager
{
	public static bool IsMenuVisible = false;

	public const string DefaultProfileName = "default";

	private const string ActiveProfileFileName = "active_profile.txt";

	private const string LegacyMetaFileName = "meta.json";

	private const string ConfigFileExtension = ".cfg";

	private const string LegacyConfigFileExtension = ".json";

	private const int ConfigMagic = 1381259348;
	private const int ConfigVersion = 21;

	private const int TextConfigVersion = 8;

	private static string _configDirectory;

	private static string _currentProfile = DefaultProfileName;

	public static ConfigData Config { get; private set; }

	public static void Init()
	{
		_configDirectory = Path.Combine(Application.persistentDataPath, "CheatConfigs");
		EnsureConfigDirectoryExists();
		if (Config == null)
		{
			Config = new ConfigData();
		}
		Config = EnsureConfigDefaults(Config);
		LoadLastConfig();
	}

	public static void LoadLastConfig()
	{
		EnsureConfigDirectoryExists();
		string profileName = ReadActiveProfileName();
		LoadConfig(profileName);
	}

	public static void SaveConfig(string profileName)
	{
		EnsureConfigDirectoryExists();
		if (Config == null)
		{
			Config = new ConfigData();
		}
		Config = EnsureConfigDefaults(Config);

		profileName = NormalizeProfileName(profileName);
		string configPath = GetBinaryConfigPath(profileName);
		WriteConfigDocument(configPath, Config);
		Config = ReadConfigDocument(configPath);
		DeleteLegacyJsonIfPresent(profileName);
		_currentProfile = profileName;
		WriteActiveProfileName(profileName);
	}

	public static void LoadConfig(string profileName)
	{
		EnsureConfigDirectoryExists();
		profileName = NormalizeProfileName(profileName);
		string binaryPath = GetBinaryConfigPath(profileName);
		string legacyPath = GetLegacyConfigPath(profileName);

		if (TryReadConfigDocument(binaryPath, out ConfigData currentConfig))
		{
			Config = currentConfig;
		}
		else if (File.Exists(binaryPath))
		{
			if (TryReadBinaryConfig(binaryPath, out ConfigData binaryConfig))
			{
				Config = binaryConfig;
				WriteConfigDocument(binaryPath, Config);
			}
			else
			{
				BackupUnreadableConfig(binaryPath);
				Config = new ConfigData();
			}
		}
		else if (TryLoadLegacyConfig(legacyPath, out ConfigData legacyConfig))
		{
			Config = legacyConfig;
			WriteConfigDocument(binaryPath, Config);
			DeleteLegacyJsonIfPresent(profileName);
		}
		else
		{
			Config = new ConfigData();
		}

		Config = EnsureConfigDefaults(Config);
		_currentProfile = profileName;
		WriteActiveProfileName(profileName);
	}

	public static List<string> GetProfiles()
	{
		EnsureConfigDirectoryExists();
		HashSet<string> profileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddProfilesFromPattern(profileNames, "*" + ConfigFileExtension);
		AddProfilesFromPattern(profileNames, "*" + LegacyConfigFileExtension);
		profileNames.Remove(Path.GetFileNameWithoutExtension(LegacyMetaFileName));
		List<string> profiles = new List<string>(profileNames);
		profiles.Sort(StringComparer.OrdinalIgnoreCase);
		return profiles;
	}

	public static string GetCurrentProfile()
	{
		return _currentProfile;
	}

	public static string GetConfigDirectory()
	{
		return _configDirectory;
	}

	public static int DeleteAllSavedConfigs()
	{
		EnsureConfigDirectoryExists();
		int deletedCount = 0;
		deletedCount += DeleteFilesByPattern("*" + ConfigFileExtension);
		deletedCount += DeleteFilesByPattern("*" + LegacyConfigFileExtension, Path.GetFileName(LegacyMetaFileName));
		DeleteFileIfExists(GetActiveProfilePath());
		Config = new ConfigData();
		_currentProfile = DefaultProfileName;
		WriteActiveProfileName(_currentProfile);
		return deletedCount;
	}

	public static bool DeleteConfig(string profileName)
	{
		EnsureConfigDirectoryExists();
		profileName = NormalizeProfileName(profileName);
		bool deleted = false;
		deleted |= DeleteFileIfExists(GetBinaryConfigPath(profileName));
		deleted |= DeleteFileIfExists(GetLegacyConfigPath(profileName));
		if (!deleted)
		{
			return false;
		}

		if (string.Equals(_currentProfile, profileName, StringComparison.OrdinalIgnoreCase))
		{
			List<string> profiles = GetProfiles();
			if (profiles.Count > 0)
			{
				LoadConfig(profiles[0]);
			}
			else
			{
				Config = new ConfigData();
				_currentProfile = DefaultProfileName;
				WriteActiveProfileName(_currentProfile);
			}
		}
		else
		{
			WriteActiveProfileName(_currentProfile);
		}

		return true;
	}

	private static void EnsureConfigDirectoryExists()
	{
		if (!Directory.Exists(_configDirectory))
		{
			Directory.CreateDirectory(_configDirectory);
		}
	}

	private static string NormalizeProfileName(string profileName)
	{
		if (string.IsNullOrWhiteSpace(profileName))
		{
			return DefaultProfileName;
		}

		foreach (char invalidFileNameChar in Path.GetInvalidFileNameChars())
		{
			profileName = profileName.Replace(invalidFileNameChar, '_');
		}

		profileName = profileName.Trim();
		return profileName.Length == 0 ? DefaultProfileName : profileName;
	}

	private static string GetBinaryConfigPath(string profileName)
	{
		return Path.Combine(_configDirectory, profileName + ConfigFileExtension);
	}

	private static string GetLegacyConfigPath(string profileName)
	{
		return Path.Combine(_configDirectory, profileName + LegacyConfigFileExtension);
	}

	private static string GetActiveProfilePath()
	{
		return Path.Combine(_configDirectory, ActiveProfileFileName);
	}

	private static string ReadActiveProfileName()
	{
		string activeProfilePath = GetActiveProfilePath();
		if (File.Exists(activeProfilePath))
		{
			try
			{
				return NormalizeProfileName(File.ReadAllText(activeProfilePath, Encoding.UTF8));
			}
			catch
			{
			}
		}

		string legacyMetaPath = Path.Combine(_configDirectory, LegacyMetaFileName);
		if (File.Exists(legacyMetaPath))
		{
			try
			{
				string text = File.ReadAllText(legacyMetaPath, Encoding.UTF8);
				int keyIndex = text.IndexOf("LastProfile", StringComparison.OrdinalIgnoreCase);
				if (keyIndex >= 0)
				{
					int firstQuote = text.IndexOf('"', keyIndex + "LastProfile".Length);
					if (firstQuote >= 0)
					{
						int secondQuote = text.IndexOf('"', firstQuote + 1);
						if (secondQuote > firstQuote)
						{
							string legacyProfile = text.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
							return NormalizeProfileName(legacyProfile);
						}
					}
				}
			}
			catch
			{
			}
		}

		return DefaultProfileName;
	}

	private static void WriteActiveProfileName(string profileName)
	{
		try
		{
			WriteTextAtomic(GetActiveProfilePath(), NormalizeProfileName(profileName));
		}
		catch
		{
		}
	}

	private static void AddProfilesFromPattern(HashSet<string> profileNames, string pattern)
	{
		if (!Directory.Exists(_configDirectory))
		{
			return;
		}

		string[] files = Directory.GetFiles(_configDirectory, pattern);
		for (int i = 0; i < files.Length; i++)
		{
			profileNames.Add(Path.GetFileNameWithoutExtension(files[i]));
		}
	}

	private static int DeleteFilesByPattern(string pattern, string excludedFileName = null)
	{
		int deletedCount = 0;
		string[] files = Directory.GetFiles(_configDirectory, pattern);
		for (int i = 0; i < files.Length; i++)
		{
			if (!string.IsNullOrEmpty(excludedFileName) && string.Equals(Path.GetFileName(files[i]), excludedFileName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (DeleteFileIfExists(files[i]))
			{
				deletedCount++;
			}
		}

		return deletedCount;
	}

	private static bool DeleteFileIfExists(string path)
	{
		if (!File.Exists(path))
		{
			return false;
		}

		try
		{
			File.Delete(path);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryLoadLegacyConfig(string legacyPath, out ConfigData configData)
	{
		configData = null;
		if (!File.Exists(legacyPath))
		{
			return false;
		}

		try
		{
			string text = File.ReadAllText(legacyPath, Encoding.UTF8);
			ConfigData loaded = JsonUtility.FromJson<ConfigData>(text);
			configData = EnsureConfigDefaults(loaded);
			return true;
		}
		catch
		{
			BackupUnreadableConfig(legacyPath);
			return false;
		}
	}

	private static void DeleteLegacyJsonIfPresent(string profileName)
	{
		DeleteFileIfExists(GetLegacyConfigPath(profileName));
	}

	private static ConfigData EnsureConfigDefaults(ConfigData config)
	{
		config ??= new ConfigData();
		config.Loot ??= new ConfigData.LootSettings();
		config.Enemies ??= new ConfigData.EnemySettings();
		config.Enemies.RenderMethod = Math.Max(0, Math.Min(2, config.Enemies.RenderMethod));
		config.Minimap ??= new ConfigData.MinimapSettings();
		config.Local ??= new ConfigData.LocalSettings();
		config.Misc ??= new ConfigData.MiscSettings();
		config.Compass ??= new ConfigData.CompassSettings();
		config.PlayerEsp ??= new ConfigData.PlayerEspSettings();
		config.PlayerEquipmentEsp ??= new ConfigData.PlayerEquipmentEspSettings();
		config.LaserSight ??= new ConfigData.LaserSightSettings();
		config.Structures ??= new ConfigData.StructureEspSettings();
		config.Carts ??= new ConfigData.CartEspSettings();
		config.Specials ??= new ConfigData.SpecialsSettings();
		config.GameModifiers ??= new ConfigData.GameModifiersSettings();
		config.Teleport ??= new ConfigData.TeleportSettings();
		config.Aimbot ??= new ConfigData.AimbotSettings();
		config.Skills ??= new ConfigData.SkillSettings();
		config.UI ??= new ConfigData.UiSettings();
		return config;
	}

	private static bool TryReadConfigDocument(string path, out ConfigData configData)
	{
		configData = null;
		if (!File.Exists(path))
		{
			return false;
		}

		try
		{
			string text = File.ReadAllText(path, Encoding.UTF8);
			if (!LooksLikeJson(text))
			{
				return TryReadTextConfigDocument(text, out configData);
			}

			return TryReadJsonConfigDocument(text, out configData);
		}
		catch
		{
		}

		return false;
	}

	private static ConfigData ReadConfigDocument(string path)
	{
		if (TryReadConfigDocument(path, out ConfigData configData))
		{
			return configData;
		}

		throw new InvalidDataException("Unreadable config document.");
	}

	private static void WriteConfigDocument(string path, ConfigData config)
	{
		ConfigData safeConfig = EnsureConfigDefaults(config);
		StringBuilder builder = new StringBuilder(4096);
		builder.AppendLine("# REPO Cheat Config");
		builder.Append("Version=").Append(TextConfigVersion.ToString(CultureInfo.InvariantCulture)).AppendLine();
		AppendObjectFields(builder, safeConfig, null);
		WriteTextAtomic(path, builder.ToString());
	}

	private static bool LooksLikeJson(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		for (int i = 0; i < text.Length; i++)
		{
			if (!char.IsWhiteSpace(text[i]))
			{
				return text[i] == '{';
			}
		}

		return false;
	}

	private static bool TryReadBinaryConfig(string path, out ConfigData configData)
	{
		configData = null;
		try
		{
			configData = EnsureConfigDefaults(ReadConfigBinary(path));
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryReadJsonConfigDocument(string text, out ConfigData configData)
	{
		configData = null;
		ConfigData directConfig = JsonUtility.FromJson<ConfigData>(text);
		if (directConfig != null)
		{
			configData = EnsureConfigDefaults(directConfig);
			if (configData.Enemies.HighlightMaxDistance <= 150f)
				configData.Enemies.HighlightMaxDistance = 1000f;
			return true;
		}

		// Accept earlier broken placeholder documents and fall back to defaults.
		if (text.IndexOf("\"Version\"", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			configData = new ConfigData();
			return true;
		}

		return false;
	}

	private static bool TryReadTextConfigDocument(string text, out ConfigData configData)
	{
		configData = new ConfigData();
		bool sawValue = false;
		int documentVersion = 1;
		string[] lines = text.Replace("\r", string.Empty).Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i].Trim();
			if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
			{
				continue;
			}

			int separatorIndex = line.IndexOf('=');
			if (separatorIndex <= 0)
			{
				continue;
			}

			string key = line.Substring(0, separatorIndex).Trim();
			string value = line.Substring(separatorIndex + 1).Trim();
			if (string.Equals(key, "Version", StringComparison.OrdinalIgnoreCase))
			{
				sawValue = true;
				int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out documentVersion);
				continue;
			}

			if (ApplyFieldValue(configData, key, value))
			{
				sawValue = true;
			}
		}

		if (!sawValue)
		{
			configData = null;
			return false;
		}

		configData = EnsureConfigDefaults(configData);
		if (documentVersion < 8 && configData.Enemies.HighlightMaxDistance <= 150f)
			configData.Enemies.HighlightMaxDistance = 1000f;
		return true;
	}

	private static void AppendObjectFields(StringBuilder builder, object instance, string prefix)
	{
		if (instance == null)
		{
			return;
		}

		FieldInfo[] fields = GetSerializableFields(instance.GetType());
		for (int i = 0; i < fields.Length; i++)
		{
			FieldInfo field = fields[i];
			object value = field.GetValue(instance);
			string fieldPath = string.IsNullOrEmpty(prefix) ? field.Name : prefix + "." + field.Name;
			if (IsScalarFieldType(field.FieldType))
			{
				builder.Append(fieldPath).Append('=').Append(SerializeScalarValue(value, field.FieldType)).AppendLine();
				continue;
			}

			if (value == null)
			{
				value = Activator.CreateInstance(field.FieldType);
				field.SetValue(instance, value);
			}

			AppendObjectFields(builder, value, fieldPath);
		}
	}

	private static FieldInfo[] GetSerializableFields(Type type)
	{
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
		Array.Sort(fields, (left, right) => string.CompareOrdinal(left.Name, right.Name));
		return fields;
	}

	private static bool IsScalarFieldType(Type type)
	{
		return type == typeof(bool) || type == typeof(int) || type == typeof(float) || type == typeof(string) || type.IsEnum || type == typeof(Color) || type == typeof(Vector2);
	}

	private static string SerializeScalarValue(object value, Type type)
	{
		if (type == typeof(bool))
		{
			return ((bool)(value ?? false)) ? "true" : "false";
		}

		if (type == typeof(int))
		{
			return ((int)(value ?? 0)).ToString(CultureInfo.InvariantCulture);
		}

		if (type == typeof(float))
		{
			return ((float)(value ?? 0f)).ToString("R", CultureInfo.InvariantCulture);
		}

		if (type == typeof(string))
		{
			return EscapeString(value as string ?? string.Empty);
		}

		if (type.IsEnum)
		{
			return value?.ToString() ?? Activator.CreateInstance(type).ToString();
		}

		if (type == typeof(Color))
		{
			Color color = (Color)(value ?? default(Color));
			return string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R},{3:R}", color.r, color.g, color.b, color.a);
		}

		if (type == typeof(Vector2))
		{
			Vector2 vector = (Vector2)(value ?? default(Vector2));
			return string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R}", vector.x, vector.y);
		}

		return EscapeString(value?.ToString() ?? string.Empty);
	}

	private static bool ApplyFieldValue(object root, string path, string rawValue)
	{
		if (root == null || string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		string[] segments = path.Split('.');
		object current = root;
		Type currentType = root.GetType();
		for (int i = 0; i < segments.Length; i++)
		{
			FieldInfo field = currentType.GetField(segments[i], BindingFlags.Instance | BindingFlags.Public);
			if (field == null)
			{
				return false;
			}

			bool isLast = i == segments.Length - 1;
			if (isLast)
			{
				if (!TryParseScalarValue(rawValue, field.FieldType, out object parsedValue))
				{
					return false;
				}

				field.SetValue(current, parsedValue);
				return true;
			}

			object next = field.GetValue(current);
			if (next == null)
			{
				next = Activator.CreateInstance(field.FieldType);
				field.SetValue(current, next);
			}

			current = next;
			currentType = field.FieldType;
		}

		return false;
	}

	private static bool TryParseScalarValue(string rawValue, Type type, out object value)
	{
		value = null;
		if (type == typeof(bool))
		{
			if (bool.TryParse(rawValue, out bool boolValue))
			{
				value = boolValue;
				return true;
			}

			if (rawValue == "1" || string.Equals(rawValue, "on", StringComparison.OrdinalIgnoreCase))
			{
				value = true;
				return true;
			}

			if (rawValue == "0" || string.Equals(rawValue, "off", StringComparison.OrdinalIgnoreCase))
			{
				value = false;
				return true;
			}

			return false;
		}

		if (type == typeof(int))
		{
			if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
			{
				value = intValue;
				return true;
			}

			return false;
		}

		if (type == typeof(float))
		{
			if (float.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float floatValue))
			{
				value = floatValue;
				return true;
			}

			return false;
		}

		if (type == typeof(string))
		{
			value = UnescapeString(rawValue);
			return true;
		}

		if (type.IsEnum)
		{
			try
			{
				value = Enum.Parse(type, rawValue, true);
				return true;
			}
			catch
			{
				Type enumUnderlyingType = Enum.GetUnderlyingType(type);
				if (TryParseScalarValue(rawValue, enumUnderlyingType, out object numericValue))
				{
					value = Enum.ToObject(type, numericValue);
					return true;
				}

				return false;
			}
		}

		if (type == typeof(Color))
		{
			string[] parts = rawValue.Split(',');
			if (parts.Length == 4
				&& float.TryParse(parts[0], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float r)
				&& float.TryParse(parts[1], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float g)
				&& float.TryParse(parts[2], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float b)
				&& float.TryParse(parts[3], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float a))
			{
				value = new Color(r, g, b, a);
				return true;
			}

			return false;
		}

		if (type == typeof(Vector2))
		{
			string[] parts = rawValue.Split(',');
			if (parts.Length == 2
				&& float.TryParse(parts[0], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float x)
				&& float.TryParse(parts[1], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float y))
			{
				value = new Vector2(x, y);
				return true;
			}

			return false;
		}

		return false;
	}

	private static string EscapeString(string value)
	{
		return value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r");
	}

	private static string UnescapeString(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		StringBuilder builder = new StringBuilder(value.Length);
		bool escaped = false;
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (!escaped)
			{
				if (c == '\\')
				{
					escaped = true;
					continue;
				}

				builder.Append(c);
				continue;
			}

			switch (c)
			{
				case 'n':
					builder.Append('\n');
					break;
				case 'r':
					builder.Append('\r');
					break;
				case '\\':
					builder.Append('\\');
					break;
				default:
					builder.Append(c);
					break;
			}
			escaped = false;
		}

		if (escaped)
		{
			builder.Append('\\');
		}

		return builder.ToString();
	}

	private static void WriteConfigBinary(string path, ConfigData config)
	{
		WriteBinaryAtomic(path, writer =>
		{
			writer.Write(ConfigMagic);
			writer.Write(ConfigVersion);
			WriteLoot(writer, config);
			WriteEnemies(writer, config.Enemies);
			WriteMinimap(writer, config.Minimap);
			WriteLocal(writer, config.Local);
			WriteMisc(writer, config.Misc);
			WriteCompass(writer, config.Compass);
			WritePlayerEsp(writer, config.PlayerEsp);
			WriteLaserSight(writer, config.LaserSight);
			WriteStructures(writer, config.Structures);
			WriteUI(writer, config.UI);
			WriteAimbot(writer, config.Aimbot);
			WriteSkills(writer, config.Skills);
			WriteGameModifiers(writer, config.GameModifiers);
			WriteTeleport(writer, config.Teleport);
			WritePlayerEquipmentEsp(writer, config.PlayerEquipmentEsp);
		});
	}

	private static ConfigData ReadConfigBinary(string path)
	{
		using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8);
		if (reader.ReadInt32() != ConfigMagic)
		{
			throw new InvalidDataException("Invalid config header.");
		}

		int version = reader.ReadInt32();
		if (version < 1 || version > ConfigVersion)
		{
			throw new InvalidDataException("Unsupported config version: " + version);
		}

		ConfigData config = new ConfigData();
		ReadLoot(reader, config, version);
		ReadEnemies(reader, config.Enemies, version);
		ReadMinimap(reader, config.Minimap, version);
		ReadLocal(reader, config.Local, version);
		ReadMisc(reader, config.Misc, version);
		ReadCompass(reader, config.Compass, version);
		ReadPlayerEsp(reader, config.PlayerEsp, version);
		ReadLaserSight(reader, config.LaserSight, version);
		ReadStructures(reader, config.Structures, version);
		if (version >= 3)
		{
			ReadUI(reader, config.UI, version);
		}
		if (version >= 7)
		{
			ReadAimbot(reader, config.Aimbot, version);
		}
		if (version >= 12)
		{
			ReadSkills(reader, config.Skills, version);
		}
		if (version >= 11)
		{
			ReadGameModifiers(reader, config.GameModifiers, version);
		}
		if (version >= 17)
		{
			ReadTeleport(reader, config.Teleport);
		}
		if (version >= 18)
		{
			ReadPlayerEquipmentEsp(reader, config.PlayerEquipmentEsp);
		}
		return config;
	}

	private static void WriteLoot(BinaryWriter writer, ConfigData config)
	{
		var settings = config.Loot;
		writer.Write(settings.Enabled);
		writer.Write(settings.DrawTracers);
		writer.Write(settings.DrawBox);
		writer.Write(settings.DrawName);
		writer.Write(settings.MaxDistance);
		writer.Write(config.Specials.TokenBoxMaxDistance);
		writer.Write(config.Specials.KeycardMaxDistance);
		writer.Write(settings.MinValue);
		writer.Write(settings.UseClustering);
		writer.Write(settings.DynamicOpacity);
		writer.Write(settings.ShowCartUI);
		writer.Write(config.Specials.Enabled);
		writer.Write(config.Specials.TokenBoxEnabled);
		writer.Write(config.Specials.TokenBoxTracers);
		writer.Write(config.Specials.TokenBoxBox);
		writer.Write(config.Specials.TokenBoxName);
		writer.Write(config.Specials.TokenBoxDistance);
		writer.Write(config.Specials.TokenBoxInfo);
		writer.Write(config.Specials.KeycardEnabled);
		writer.Write(config.Specials.KeycardTracers);
		writer.Write(config.Specials.KeycardBox);
		writer.Write(config.Specials.KeycardName);
		writer.Write(config.Specials.KeycardDistance);
		writer.Write(settings.ItemInvulnerable);
		WriteColor(writer, settings.EspColor);
		writer.Write(settings.HighlightEnabled);
		writer.Write(settings.HighlightDistance);
		WriteColor(writer, settings.HighlightColorVisible);
		WriteColor(writer, settings.HighlightColorOccluded);
		WriteKeyCode(writer, settings.ToggleKey);
		WriteKeyCode(writer, config.Specials.ToggleKey);
		WriteKeyCode(writer, config.Specials.TokenBoxToggleKey);
		WriteKeyCode(writer, config.Specials.KeycardToggleKey);
		WriteKeyCode(writer, config.Specials.TokenBoxTracersKey);
		WriteKeyCode(writer, config.Specials.TokenBoxBoxKey);
		WriteKeyCode(writer, config.Specials.TokenBoxNameKey);
		WriteKeyCode(writer, config.Specials.TokenBoxDistanceKey);
		WriteKeyCode(writer, config.Specials.TokenBoxInfoKey);
		WriteKeyCode(writer, config.Specials.KeycardTracersKey);
		WriteKeyCode(writer, config.Specials.KeycardBoxKey);
		WriteKeyCode(writer, config.Specials.KeycardNameKey);
		WriteKeyCode(writer, config.Specials.KeycardDistanceKey);
		WriteKeyCode(writer, settings.DrawTracersKey);
		WriteKeyCode(writer, settings.DrawBoxKey);
		WriteKeyCode(writer, settings.DrawNameKey);
		WriteKeyCode(writer, settings.UseClusteringKey);
		WriteKeyCode(writer, settings.DynamicOpacityKey);
		WriteKeyCode(writer, settings.ShowCartUIKey);
		WriteKeyCode(writer, settings.ItemInvulnerableKey);
		WriteKeyCode(writer, settings.HighlightEnabledKey);
		WriteKeyCode(writer, config.Carts.ToggleKey);
		WriteKeyCode(writer, config.Carts.DrawTracersKey);
		WriteKeyCode(writer, config.Carts.DrawBoxKey);
		WriteKeyCode(writer, config.Carts.DrawNameKey);
		WriteKeyCode(writer, config.Carts.DrawDistanceKey);
		WriteKeyCode(writer, config.Carts.DrawInfoKey);
		WriteColor(writer, config.Carts.Color);
		WriteColor(writer, config.Specials.TokenBoxColor);
		WriteColor(writer, config.Specials.KeycardColor);
	}

	private static void ReadLoot(BinaryReader reader, ConfigData config, int version)
	{
		var settings = config.Loot;
		settings.Enabled = reader.ReadBoolean();
		settings.DrawTracers = reader.ReadBoolean();
		settings.DrawBox = reader.ReadBoolean();
		settings.DrawName = reader.ReadBoolean();
		settings.MaxDistance = reader.ReadSingle();
		if (version >= 7)
		{
			config.Specials.TokenBoxMaxDistance = reader.ReadSingle();
			config.Specials.KeycardMaxDistance = reader.ReadSingle();
		}
		settings.MinValue = reader.ReadInt32();
		settings.UseClustering = reader.ReadBoolean();
		settings.DynamicOpacity = reader.ReadBoolean();
		settings.ShowCartUI = reader.ReadBoolean();
		if (version >= 7)
		{
			config.Specials.Enabled = reader.ReadBoolean();
			config.Specials.TokenBoxEnabled = reader.ReadBoolean();
			config.Specials.TokenBoxTracers = reader.ReadBoolean();
			config.Specials.TokenBoxBox = reader.ReadBoolean();
			config.Specials.TokenBoxName = reader.ReadBoolean();
			config.Specials.TokenBoxDistance = reader.ReadBoolean();
			config.Specials.TokenBoxInfo = reader.ReadBoolean();
			config.Specials.KeycardEnabled = reader.ReadBoolean();
			config.Specials.KeycardTracers = reader.ReadBoolean();
			config.Specials.KeycardBox = reader.ReadBoolean();
			config.Specials.KeycardName = reader.ReadBoolean();
			config.Specials.KeycardDistance = reader.ReadBoolean();
		}

		settings.ItemInvulnerable = reader.ReadBoolean();
		settings.EspColor = ReadColor(reader);
		settings.HighlightEnabled = reader.ReadBoolean();
		settings.HighlightDistance = reader.ReadSingle();
		settings.HighlightColorVisible = ReadColor(reader);
		settings.HighlightColorOccluded = ReadColor(reader);
		settings.ToggleKey = ReadKeyCode(reader);
		if (version >= 7)
		{
			config.Specials.ToggleKey = ReadKeyCode(reader);
			config.Specials.TokenBoxToggleKey = ReadKeyCode(reader);
			config.Specials.KeycardToggleKey = ReadKeyCode(reader);
			if (version >= 10)
			{
				config.Specials.TokenBoxTracersKey = ReadKeyCode(reader);
				config.Specials.TokenBoxBoxKey = ReadKeyCode(reader);
				config.Specials.TokenBoxNameKey = ReadKeyCode(reader);
				config.Specials.TokenBoxDistanceKey = ReadKeyCode(reader);
				config.Specials.TokenBoxInfoKey = ReadKeyCode(reader);
				config.Specials.KeycardTracersKey = ReadKeyCode(reader);
				config.Specials.KeycardBoxKey = ReadKeyCode(reader);
				config.Specials.KeycardNameKey = ReadKeyCode(reader);
				config.Specials.KeycardDistanceKey = ReadKeyCode(reader);
			}
		}
		if (version >= 9)
		{
			try
			{
				settings.DrawTracersKey = ReadKeyCode(reader);
				settings.DrawBoxKey = ReadKeyCode(reader);
				settings.DrawNameKey = ReadKeyCode(reader);
				settings.UseClusteringKey = ReadKeyCode(reader);
				settings.DynamicOpacityKey = ReadKeyCode(reader);
				settings.ShowCartUIKey = ReadKeyCode(reader);
				settings.ItemInvulnerableKey = ReadKeyCode(reader);
				settings.HighlightEnabledKey = ReadKeyCode(reader);
				config.Carts.ToggleKey = ReadKeyCode(reader);
				config.Carts.DrawTracersKey = ReadKeyCode(reader);
				config.Carts.DrawBoxKey = ReadKeyCode(reader);
				config.Carts.DrawNameKey = ReadKeyCode(reader);
				config.Carts.DrawDistanceKey = ReadKeyCode(reader);
				config.Carts.DrawInfoKey = ReadKeyCode(reader);
			} catch { }
		}
		if (version >= 15)
		{
			config.Carts.Color = ReadColor(reader);
			config.Specials.TokenBoxColor = ReadColor(reader);
			config.Specials.KeycardColor = ReadColor(reader);
		}
	}

	private static void WriteEnemies(BinaryWriter writer, ConfigData.EnemySettings settings)
	{
		writer.Write(settings.EspEnabled);
		writer.Write(settings.DrawTracers);
		writer.Write(settings.DrawBox);
		writer.Write(settings.BoxType);
		writer.Write(settings.DrawHealth);
		writer.Write(settings.DrawInfo);
		writer.Write(settings.DrawDistance);
		writer.Write(settings.DrawStatus);
		writer.Write(settings.DrawPath);
		writer.Write(settings.TargetWarning);
		writer.Write(settings.VisibilityCheck);
		writer.Write(settings.MaxDistance);
		writer.Write(settings.BoxPaddingPixels);
		writer.Write(settings.BoxSmoothingSeconds);
		writer.Write(settings.BoxMinimumWidth);
		writer.Write(settings.BoxMinimumHeight);
		writer.Write(settings.BoxMaximumScreenFraction);
		WriteColor(writer, settings.EspColor);
		writer.Write(settings.HighlightEnabled);
		writer.Write(settings.HighlightMaxDistance);
		WriteColor(writer, settings.HighlightColor);
		writer.Write(settings.RenderMethod);
		WriteKeyCode(writer, settings.ToggleKey);
		WriteKeyCode(writer, settings.VisibilityCheckKey);
		WriteKeyCode(writer, settings.DrawTracersKey);
		WriteKeyCode(writer, settings.DrawBoxKey);
		WriteKeyCode(writer, settings.DrawHealthKey);
		WriteKeyCode(writer, settings.DrawInfoKey);
		WriteKeyCode(writer, settings.DrawDistanceKey);
		WriteKeyCode(writer, settings.DrawStatusKey);
		WriteKeyCode(writer, settings.DrawPathKey);
		WriteKeyCode(writer, settings.TargetWarningKey);
		WriteKeyCode(writer, settings.HighlightEnabledKey);
		WriteColor(writer, settings.OccludedEspColor);
	}

	private static void ReadEnemies(BinaryReader reader, ConfigData.EnemySettings settings, int version)
	{
		settings.EspEnabled = reader.ReadBoolean();
		settings.DrawTracers = reader.ReadBoolean();
		settings.DrawBox = reader.ReadBoolean();
		settings.BoxType = reader.ReadInt32();
		settings.DrawHealth = reader.ReadBoolean();
		settings.DrawInfo = reader.ReadBoolean();
		settings.DrawDistance = reader.ReadBoolean();
		settings.DrawStatus = reader.ReadBoolean();
		settings.DrawPath = reader.ReadBoolean();
		settings.TargetWarning = reader.ReadBoolean();
		if (version >= 8)
		{
			settings.VisibilityCheck = reader.ReadBoolean();
		}
		settings.MaxDistance = reader.ReadSingle();
		if (version >= 16)
		{
			settings.BoxPaddingPixels = reader.ReadSingle();
			settings.BoxSmoothingSeconds = reader.ReadSingle();
			settings.BoxMinimumWidth = reader.ReadSingle();
			settings.BoxMinimumHeight = reader.ReadSingle();
			settings.BoxMaximumScreenFraction = reader.ReadSingle();
		}
		settings.EspColor = ReadColor(reader);
		settings.HighlightEnabled = reader.ReadBoolean();
		settings.HighlightMaxDistance = reader.ReadSingle();
		if (version < 20)
			settings.HighlightMaxDistance = 1000f;
		settings.HighlightColor = ReadColor(reader);
		int renderMethod = reader.ReadInt32();
		settings.RenderMethod = renderMethod == 2 ? 2 : renderMethod == 1 ? 1 : 0;
		// Migrate the former standalone enemy highlight toggle into the unified style.
		if (settings.HighlightEnabled)
		{
			settings.RenderMethod = 2;
		}
		settings.ToggleKey = ReadKeyCode(reader);
		if (version >= 8)
		{
			settings.VisibilityCheckKey = ReadKeyCode(reader);
		}
		if (version >= 9)
		{
			try
			{
				settings.DrawTracersKey = ReadKeyCode(reader);
				settings.DrawBoxKey = ReadKeyCode(reader);
				settings.DrawHealthKey = ReadKeyCode(reader);
				settings.DrawInfoKey = ReadKeyCode(reader);
				settings.DrawDistanceKey = ReadKeyCode(reader);
				settings.DrawStatusKey = ReadKeyCode(reader);
				settings.DrawPathKey = ReadKeyCode(reader);
				settings.TargetWarningKey = ReadKeyCode(reader);
				settings.HighlightEnabledKey = ReadKeyCode(reader);
			} catch { }
		}
		if (version >= 15)
		{
			settings.OccludedEspColor = ReadColor(reader);
		}
	}

	private static void WriteMinimap(BinaryWriter writer, ConfigData.MinimapSettings settings)
	{
		writer.Write(settings.Enabled);
		writer.Write(settings.ShowIcons);
		writer.Write(settings.RenderMode);
		WriteColor(writer, settings.RingColor);
		writer.Write(settings.AutoCenter);
		writer.Write(settings.Zoom);
		writer.Write(settings.ZoomSpeed);
		writer.Write(settings.Size);
		writer.Write(settings.ShowPath);
		WriteVector2(writer, settings.Position);
		WriteKeyCode(writer, settings.ToggleFocusKey);
		WriteKeyCode(writer, settings.ToggleRenderModeKey);
		WriteKeyCode(writer, settings.ToggleKey);
		WriteKeyCode(writer, settings.ShowIconsKey);
		WriteKeyCode(writer, settings.AutoCenterKey);
		WriteKeyCode(writer, settings.ShowPathKey);
	}

	private static void ReadMinimap(BinaryReader reader, ConfigData.MinimapSettings settings, int version)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.ShowIcons = reader.ReadBoolean();
		settings.RenderMode = reader.ReadInt32();
		settings.RingColor = ReadColor(reader);
		settings.AutoCenter = reader.ReadBoolean();
		settings.Zoom = reader.ReadSingle();
		settings.ZoomSpeed = reader.ReadSingle();
		settings.Size = reader.ReadSingle();
		settings.ShowPath = reader.ReadBoolean();
		settings.Position = ReadVector2(reader);
		settings.ToggleFocusKey = ReadKeyCode(reader);
		settings.ToggleRenderModeKey = ReadKeyCode(reader);
		settings.ToggleKey = ReadKeyCode(reader);
		if (version >= 9)
		{
			try
			{
				settings.ShowIconsKey = ReadKeyCode(reader);
				settings.AutoCenterKey = ReadKeyCode(reader);
				settings.ShowPathKey = ReadKeyCode(reader);
			} catch { }
		}
	}

	private static void WriteLocal(BinaryWriter writer, ConfigData.LocalSettings settings)
	{
		writer.Write(settings.GodMode);
		writer.Write(settings.InfiniteStamina);
		writer.Write(settings.InfiniteBattery);
		writer.Write(settings.GrabRange);
		writer.Write(settings.GrabStrength);
		writer.Write(settings.RunSpeedEnabled);
		writer.Write(settings.JumpForceEnabled);
		writer.Write(settings.GravityEnabled);
		writer.Write(settings.JumpForce);
		writer.Write(settings.Gravity);
		writer.Write(settings.NoClip);
		writer.Write(settings.NoRagdoll);
		writer.Write(settings.SolidAim);
		writer.Write(settings.RunSpeed);
		writer.Write(settings.NoClipSpeed);
		writer.Write(settings.NoClipFastMultiplier);
		WriteKeyCode(writer, settings.GodModeKey);
		WriteKeyCode(writer, settings.NoClipKey);
		WriteKeyCode(writer, settings.NoRagdollKey);
		WriteKeyCode(writer, settings.SolidAimKey);
		WriteKeyCode(writer, settings.FreeCamToggleKey);
		writer.Write(settings.FreeCamSpeed);
		writer.Write(settings.FreeCamFastMultiplier);
		writer.Write(settings.FreeCamSensitivity);
		WriteKeyCode(writer, settings.InfiniteStaminaKey);
		WriteKeyCode(writer, settings.InfiniteBatteryKey);
		WriteKeyCode(writer, settings.RunSpeedEnabledKey);
		WriteKeyCode(writer, settings.JumpForceEnabledKey);
		WriteKeyCode(writer, settings.GravityEnabledKey);

		writer.Write(settings.TumbleFly);
		WriteKeyCode(writer, settings.TumbleFlyKey);
		writer.Write(settings.ThirdPerson);
		WriteKeyCode(writer, settings.ThirdPersonKey);
		writer.Write(settings.ThirdPersonDistance);
		writer.Write(settings.ThirdPersonHeight);
		writer.Write(settings.ThirdPersonModelScale);
		writer.Write(settings.ThirdPersonHorizontal);
	}

	private static void ReadLocal(BinaryReader reader, ConfigData.LocalSettings settings, int version)
	{
		settings.GodMode = reader.ReadBoolean();
		settings.InfiniteStamina = reader.ReadBoolean();
		settings.InfiniteBattery = reader.ReadBoolean();
		settings.GrabRange = reader.ReadSingle();
		settings.GrabStrength = reader.ReadSingle();
		settings.RunSpeedEnabled = reader.ReadBoolean();
		settings.JumpForceEnabled = reader.ReadBoolean();
		settings.GravityEnabled = reader.ReadBoolean();
		settings.JumpForce = reader.ReadSingle();
		settings.Gravity = reader.ReadSingle();
		settings.NoClip = reader.ReadBoolean();
		settings.NoRagdoll = reader.ReadBoolean();
		if (version >= 6)
		{
			settings.SolidAim = reader.ReadBoolean();
		}
		settings.RunSpeed = reader.ReadSingle();
		settings.NoClipSpeed = reader.ReadSingle();
		settings.NoClipFastMultiplier = ((version >= 2) ? reader.ReadSingle() : 2f);
		settings.GodModeKey = ReadKeyCode(reader);
		settings.NoClipKey = ReadKeyCode(reader);
		if (version >= 5)
		{
			settings.NoRagdollKey = ReadKeyCode(reader);
			if (version >= 6)
			{
				settings.SolidAimKey = ReadKeyCode(reader);
			}
		}
		if (version >= 4)
		{
			settings.FreeCamToggleKey = ReadKeyCode(reader);
		}
		settings.FreeCamSpeed = reader.ReadSingle();
		settings.FreeCamFastMultiplier = reader.ReadSingle();
		settings.FreeCamSensitivity = reader.ReadSingle();
		if (version >= 9)
		{
			try
			{
				settings.InfiniteStaminaKey = ReadKeyCode(reader);
				settings.InfiniteBatteryKey = ReadKeyCode(reader);
				settings.RunSpeedEnabledKey = ReadKeyCode(reader);
				settings.JumpForceEnabledKey = ReadKeyCode(reader);
				settings.GravityEnabledKey = ReadKeyCode(reader);
			} catch { }
		}

		if (version >= 11)
		{
			try
			{
				settings.TumbleFly = reader.ReadBoolean();
				settings.TumbleFlyKey = ReadKeyCode(reader);
				settings.ThirdPerson = reader.ReadBoolean();
				settings.ThirdPersonKey = ReadKeyCode(reader);
				settings.ThirdPersonDistance = reader.ReadSingle();
				settings.ThirdPersonHeight = reader.ReadSingle();
				settings.ThirdPersonModelScale = reader.ReadSingle();
				settings.ThirdPersonHorizontal = reader.ReadSingle();
			} catch { }
		}
	}

	private static void WriteMisc(BinaryWriter writer, ConfigData.MiscSettings settings)
	{
		WriteKeyCode(writer, settings.ToggleKey);
		writer.Write(settings.Crosshair);
		writer.Write(settings.ShowFps);
		writer.Write(settings.ShowKeybinds);
		WriteColor(writer, settings.MenuAccent);
		writer.Write(settings.FOV);
		writer.Write(settings.Fullbright);
		writer.Write(settings.FullbrightIntensity);
		writer.Write(settings.NoFog);
		writer.Write(settings.SetItemValue);
		writer.Write(settings.GlobalItemInvincibility);
		WriteKeyCode(writer, settings.CrosshairKey);
		WriteKeyCode(writer, settings.ShowFpsKey);
		WriteKeyCode(writer, settings.ShowKeybindsKey);
		WriteKeyCode(writer, settings.FullbrightKey);
		WriteKeyCode(writer, settings.NoFogKey);
		WriteKeyCode(writer, settings.GlobalItemInvincibilityKey);
		writer.Write(settings.DisableCameraMotion);
		WriteKeyCode(writer, settings.DisableCameraMotionKey);
		writer.Write(settings.DisableCameraShakeEffects);
		WriteKeyCode(writer, settings.DisableCameraShakeEffectsKey);
		writer.Write(settings.DisableGunRecoil);
		WriteKeyCode(writer, settings.DisableGunRecoilKey);
		writer.Write(settings.DisableGunCooldown);
		WriteKeyCode(writer, settings.DisableGunCooldownKey);
		writer.Write(settings.DisableItalicText);
		WriteKeyCode(writer, settings.DisableItalicTextKey);
	}

	private static void ReadMisc(BinaryReader reader, ConfigData.MiscSettings settings, int version)
	{
		settings.ToggleKey = ReadKeyCode(reader);
		settings.Crosshair = reader.ReadBoolean();
		settings.ShowFps = reader.ReadBoolean();
		settings.ShowKeybinds = reader.ReadBoolean();
		settings.MenuAccent = ReadColor(reader);
		settings.FOV = reader.ReadSingle();
		if (version <= 10 && Math.Abs(settings.FOV - 60f) <= 0.01f)
		{
			settings.FOV = -1f;
		}
		settings.Fullbright = reader.ReadBoolean();
		settings.FullbrightIntensity = reader.ReadSingle();
		settings.NoFog = reader.ReadBoolean();
		settings.SetItemValue = reader.ReadInt32();
		if (version >= 9)
		{
			try
			{
				settings.GlobalItemInvincibility = reader.ReadBoolean();
				settings.CrosshairKey = ReadKeyCode(reader);
				settings.ShowFpsKey = ReadKeyCode(reader);
				settings.ShowKeybindsKey = ReadKeyCode(reader);
				settings.FullbrightKey = ReadKeyCode(reader);
				settings.NoFogKey = ReadKeyCode(reader);
				settings.GlobalItemInvincibilityKey = ReadKeyCode(reader);
			} catch { }
		}
		if (version >= 21)
		{
			settings.DisableCameraMotion = reader.ReadBoolean();
			settings.DisableCameraMotionKey = ReadKeyCode(reader);
			settings.DisableCameraShakeEffects = reader.ReadBoolean();
			settings.DisableCameraShakeEffectsKey = ReadKeyCode(reader);
			settings.DisableGunRecoil = reader.ReadBoolean();
			settings.DisableGunRecoilKey = ReadKeyCode(reader);
			settings.DisableGunCooldown = reader.ReadBoolean();
			settings.DisableGunCooldownKey = ReadKeyCode(reader);
			settings.DisableItalicText = reader.ReadBoolean();
			settings.DisableItalicTextKey = ReadKeyCode(reader);
		}
	}

	private static void WriteCompass(BinaryWriter writer, ConfigData.CompassSettings settings)
	{
		writer.Write(settings.Enabled);
		writer.Write(settings.Size);
		writer.Write(settings.Range);
		writer.Write(settings.Scale);
		writer.Write(settings.YOffset);
		WriteKeyCode(writer, settings.ToggleKey);
		writer.Write(settings.PositionX);
		writer.Write(settings.PositionY);
		writer.Write(settings.ShowPlayers);
		writer.Write(settings.ShowDeathHeads);
	}

	private static void ReadCompass(BinaryReader reader, ConfigData.CompassSettings settings, int version)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.Size = reader.ReadSingle();
		settings.Range = reader.ReadSingle();
		settings.Scale = reader.ReadSingle();
		settings.YOffset = reader.ReadSingle();
		settings.ToggleKey = ReadKeyCode(reader);
		if (version >= 19)
		{
			settings.PositionX = reader.ReadSingle();
			settings.PositionY = reader.ReadSingle();
			settings.ShowPlayers = reader.ReadBoolean();
			settings.ShowDeathHeads = reader.ReadBoolean();
		}
	}

	private static void WritePlayerEsp(BinaryWriter writer, ConfigData.PlayerEspSettings settings)
	{
		writer.Write(settings.Enabled);
		writer.Write(settings.DrawName);
		writer.Write(settings.DrawHealth);
		writer.Write(settings.DrawDistance);
		writer.Write(settings.DrawLine);
		writer.Write(settings.DrawHeldItem);
		WriteColor(writer, settings.Color);
		WriteColor(writer, settings.EquipmentColor);
		WriteKeyCode(writer, settings.ToggleKey);
		WriteKeyCode(writer, settings.DrawNameKey);
		WriteKeyCode(writer, settings.DrawHealthKey);
		WriteKeyCode(writer, settings.DrawDistanceKey);
		WriteKeyCode(writer, settings.DrawLineKey);
		WriteKeyCode(writer, settings.DrawHeldItemKey);
	}

	private static void ReadPlayerEsp(BinaryReader reader, ConfigData.PlayerEspSettings settings, int version)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.DrawName = reader.ReadBoolean();
		settings.DrawHealth = reader.ReadBoolean();
		settings.DrawDistance = reader.ReadBoolean();
		try { settings.DrawLine = reader.ReadBoolean(); } catch { }
		settings.DrawHeldItem = reader.ReadBoolean();
		settings.Color = ReadColor(reader);
		settings.EquipmentColor = ReadColor(reader);
		if (version >= 9)
		{
			try
			{
				settings.ToggleKey = ReadKeyCode(reader);
				settings.DrawNameKey = ReadKeyCode(reader);
				settings.DrawHealthKey = ReadKeyCode(reader);
				settings.DrawDistanceKey = ReadKeyCode(reader);
				settings.DrawLineKey = ReadKeyCode(reader);
				settings.DrawHeldItemKey = ReadKeyCode(reader);
			} catch { }
		}
	}

	private static void WriteLaserSight(BinaryWriter writer, ConfigData.LaserSightSettings settings)
	{
		writer.Write(settings.Enabled);
		writer.Write(settings.ShowLocal);
		writer.Write(settings.ShowOthers);
		writer.Write(settings.ShowHitInfo);
		WriteColor(writer, settings.Color);
		writer.Write(settings.Width);
		WriteKeyCode(writer, settings.ToggleKey);
		WriteKeyCode(writer, settings.ShowLocalKey);
		WriteKeyCode(writer, settings.ShowOthersKey);
		WriteKeyCode(writer, settings.ShowHitInfoKey);
	}

	private static void ReadLaserSight(BinaryReader reader, ConfigData.LaserSightSettings settings, int version)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.ShowLocal = reader.ReadBoolean();
		settings.ShowOthers = reader.ReadBoolean();
		settings.ShowHitInfo = reader.ReadBoolean();
		settings.Color = ReadColor(reader);
		settings.Width = reader.ReadSingle();
		if (version >= 9)
		{
			try
			{
				settings.ToggleKey = ReadKeyCode(reader);
				settings.ShowLocalKey = ReadKeyCode(reader);
				settings.ShowOthersKey = ReadKeyCode(reader);
				settings.ShowHitInfoKey = ReadKeyCode(reader);
			} catch { }
		}
	}

	private static void WriteStructures(BinaryWriter writer, ConfigData.StructureEspSettings settings)
	{
		writer.Write(settings.ExtractionPointsEnabled);
		writer.Write(settings.EvacuationPointsEnabled);
		writer.Write(settings.TrapsEnabled);
		writer.Write(settings.DrawTracers);
		writer.Write(settings.DrawBox);
		writer.Write(settings.DrawName);
		writer.Write(settings.DrawDistance);
		writer.Write(settings.MaxDistance);
		WriteColor(writer, settings.ExtractionPointColor);
		WriteColor(writer, settings.EvacuationPointColor);
		WriteColor(writer, settings.TrapColor);
		WriteKeyCode(writer, settings.ExtractionPointsEnabledKey);
		WriteKeyCode(writer, settings.EvacuationPointsEnabledKey);
		WriteKeyCode(writer, settings.TrapsEnabledKey);
		WriteKeyCode(writer, settings.DrawTracersKey);
		WriteKeyCode(writer, settings.DrawBoxKey);
		WriteKeyCode(writer, settings.DrawNameKey);
		WriteKeyCode(writer, settings.DrawDistanceKey);
	}

	private static void ReadStructures(BinaryReader reader, ConfigData.StructureEspSettings settings, int version)
	{
		settings.ExtractionPointsEnabled = reader.ReadBoolean();
		settings.EvacuationPointsEnabled = reader.ReadBoolean();
		settings.TrapsEnabled = reader.ReadBoolean();
		settings.DrawTracers = reader.ReadBoolean();
		settings.DrawBox = reader.ReadBoolean();
		settings.DrawName = reader.ReadBoolean();
		settings.DrawDistance = reader.ReadBoolean();
		settings.MaxDistance = reader.ReadSingle();
		settings.ExtractionPointColor = ReadColor(reader);
		settings.EvacuationPointColor = ReadColor(reader);
		settings.TrapColor = ReadColor(reader);
		if (version >= 9)
		{
			try
			{
				settings.ExtractionPointsEnabledKey = ReadKeyCode(reader);
				settings.EvacuationPointsEnabledKey = ReadKeyCode(reader);
				settings.TrapsEnabledKey = ReadKeyCode(reader);
				settings.DrawTracersKey = ReadKeyCode(reader);
				settings.DrawBoxKey = ReadKeyCode(reader);
				settings.DrawNameKey = ReadKeyCode(reader);
				settings.DrawDistanceKey = ReadKeyCode(reader);
			} catch { }
		}
	}

	private static void WriteAimbot(BinaryWriter writer, ConfigData.AimbotSettings settings)
	{
		writer.Write(settings.Enabled);
		WriteKeyCode(writer, settings.ToggleKey);
		writer.Write(settings.DrawFov);
		WriteKeyCode(writer, settings.DrawFovKey);
		writer.Write(settings.FovRadius);
		writer.Write(settings.Smoothness);
		writer.Write(settings.CheckVisibility);
		WriteKeyCode(writer, settings.CheckVisibilityKey);
		WriteKeyCode(writer, settings.AimKey);
	}

	private static void ReadAimbot(BinaryReader reader, ConfigData.AimbotSettings settings, int version)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.ToggleKey = ReadKeyCode(reader);
		settings.DrawFov = reader.ReadBoolean();
		settings.DrawFovKey = ReadKeyCode(reader);
		settings.FovRadius = reader.ReadSingle();
		settings.Smoothness = reader.ReadSingle();
		settings.CheckVisibility = reader.ReadBoolean();
		settings.CheckVisibilityKey = ReadKeyCode(reader);
		settings.AimKey = ReadKeyCode(reader);
	}

	private static void WriteSkills(BinaryWriter writer, ConfigData.SkillSettings settings)
	{
		writer.Write(settings.Stamina);
		writer.Write(settings.Health);
		writer.Write(settings.TumbleWings);
		writer.Write(settings.CrouchRest);
		writer.Write(settings.DeathHeadBattery);
		writer.Write(settings.Launch);
		writer.Write(settings.MapPlayerCount);
		writer.Write(settings.Throw);
		writer.Write(settings.TumbleClimb);
	}

	private static void ReadSkills(BinaryReader reader, ConfigData.SkillSettings settings, int version)
	{
		if (version >= 14)
		{
			try
			{
				settings.Stamina = reader.ReadInt32();
				settings.Health = reader.ReadInt32();
				settings.TumbleWings = reader.ReadInt32();
				settings.CrouchRest = reader.ReadInt32();
				settings.DeathHeadBattery = reader.ReadInt32();
				settings.Launch = reader.ReadInt32();
				settings.MapPlayerCount = reader.ReadInt32();
				settings.Throw = reader.ReadInt32();
				settings.TumbleClimb = reader.ReadInt32();
			} catch { }
		}
		else if (version >= 12)
		{
			// Consume the legacy 13-field block while retaining only skill values still shown in the UI.
			try
			{
				reader.ReadInt32(); // Speed, now controlled on the Player page.
				settings.Stamina = reader.ReadInt32();
				settings.Health = reader.ReadInt32();
				reader.ReadInt32(); // ExtraJump, superseded by the Player page jump control.
				settings.TumbleWings = reader.ReadInt32();
				settings.CrouchRest = reader.ReadInt32();
				settings.DeathHeadBattery = reader.ReadInt32();
				settings.Launch = reader.ReadInt32();
				settings.MapPlayerCount = reader.ReadInt32();
				reader.ReadInt32(); // Range, now controlled on the Player page.
				reader.ReadInt32(); // Strength, now controlled on the Player page.
				settings.Throw = reader.ReadInt32();
				settings.TumbleClimb = reader.ReadInt32();
			} catch { }
		}
	}

	private static void WriteGameModifiers(BinaryWriter writer, ConfigData.GameModifiersSettings settings)
	{
		writer.Write(settings.AllTeammatesGodMode);
		WriteKeyCode(writer, settings.AllTeammatesGodModeKey);
		writer.Write(settings.InfiniteWeaponBattery);
		WriteKeyCode(writer, settings.InfiniteWeaponBatteryKey);
		writer.Write(settings.InfiniteChargingStation);
		WriteKeyCode(writer, settings.InfiniteChargingStationKey);
		writer.Write(settings.ChargingStationHeal);
		WriteKeyCode(writer, settings.ChargingStationHealKey);
		writer.Write(settings.RemovePillarLimit);
		WriteKeyCode(writer, settings.RemovePillarLimitKey);
		writer.Write(settings.PillarDropMultiplierEnabled);
		writer.Write(settings.PillarDropMultiplier);
		writer.Write(settings.TeammateDeathDropBead);
		writer.Write(settings.TeammateReviveDropBead);
		writer.Write(settings.AutoReviveTeammates);
		WriteKeyCode(writer, settings.AutoReviveTeammatesKey);
		writer.Write(settings.AutoReviveWhenSeen);
		writer.Write(settings.ReviveHeadOnTruck);
		writer.Write(settings.ReviveHeadOnExtraction);
		writer.Write(settings.ReviveHeadOnCart);
		writer.Write(settings.ReviveAutoToTruck);
		writer.Write(settings.DeathAutoToTruck);
		writer.Write(settings.DeathAutoToExtraction);
		writer.Write(settings.SpawnRateMultiplier);
		writer.Write(settings.LimitActionAndOvercharge);
		WriteKeyCode(writer, settings.PillarDropMultiplierEnabledKey);
		WriteKeyCode(writer, settings.TeammateDeathDropBeadKey);
		WriteKeyCode(writer, settings.TeammateReviveDropBeadKey);
		WriteKeyCode(writer, settings.AutoReviveWhenSeenKey);
		WriteKeyCode(writer, settings.ReviveHeadOnTruckKey);
		WriteKeyCode(writer, settings.ReviveHeadOnExtractionKey);
		WriteKeyCode(writer, settings.ReviveHeadOnCartKey);
		WriteKeyCode(writer, settings.ReviveAutoToTruckKey);
		WriteKeyCode(writer, settings.DeathAutoToTruckKey);
		WriteKeyCode(writer, settings.DeathAutoToExtractionKey);
		WriteKeyCode(writer, settings.LimitActionAndOverchargeKey);
	}

	private static void WriteTeleport(BinaryWriter writer, ConfigData.TeleportSettings settings)
	{
		writer.Write(settings.MaxDistance);
		writer.Write(settings.CooldownSeconds);
		writer.Write(settings.KeepPhysics);
	}

	private static void ReadTeleport(BinaryReader reader, ConfigData.TeleportSettings settings)
	{
		settings.MaxDistance = Mathf.Clamp(reader.ReadSingle(), 1f, 10000f);
		settings.CooldownSeconds = Mathf.Clamp(reader.ReadSingle(), 0f, 30f);
		settings.KeepPhysics = reader.ReadBoolean();
	}

	private static void WritePlayerEquipmentEsp(BinaryWriter writer, ConfigData.PlayerEquipmentEspSettings settings)
	{
		writer.Write(settings.Enabled);
		WriteKeyCode(writer, settings.ToggleKey);
		writer.Write(settings.DrawBox);
		WriteKeyCode(writer, settings.DrawBoxKey);
		writer.Write(settings.DrawName);
		WriteKeyCode(writer, settings.DrawNameKey);
		writer.Write(settings.DrawDistance);
		WriteKeyCode(writer, settings.DrawDistanceKey);
		writer.Write(settings.DrawTracers);
		WriteKeyCode(writer, settings.DrawTracersKey);
		writer.Write(settings.MaxDistance);
		WriteColor(writer, settings.Color);
	}

	private static void ReadPlayerEquipmentEsp(BinaryReader reader, ConfigData.PlayerEquipmentEspSettings settings)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.ToggleKey = ReadKeyCode(reader);
		settings.DrawBox = reader.ReadBoolean();
		settings.DrawBoxKey = ReadKeyCode(reader);
		settings.DrawName = reader.ReadBoolean();
		settings.DrawNameKey = ReadKeyCode(reader);
		settings.DrawDistance = reader.ReadBoolean();
		settings.DrawDistanceKey = ReadKeyCode(reader);
		settings.DrawTracers = reader.ReadBoolean();
		settings.DrawTracersKey = ReadKeyCode(reader);
		settings.MaxDistance = Mathf.Clamp(reader.ReadSingle(), 10f, 1000f);
		settings.Color = ReadColor(reader);
	}

	private static void ReadGameModifiers(BinaryReader reader, ConfigData.GameModifiersSettings settings, int version)
	{
		if (version >= 11)
		{
			try
			{
				settings.AllTeammatesGodMode = reader.ReadBoolean();
				settings.AllTeammatesGodModeKey = ReadKeyCode(reader);
				settings.InfiniteWeaponBattery = reader.ReadBoolean();
				settings.InfiniteWeaponBatteryKey = ReadKeyCode(reader);
				settings.InfiniteChargingStation = reader.ReadBoolean();
				settings.InfiniteChargingStationKey = ReadKeyCode(reader);
				settings.ChargingStationHeal = reader.ReadBoolean();
				settings.ChargingStationHealKey = ReadKeyCode(reader);
				settings.RemovePillarLimit = reader.ReadBoolean();
				settings.RemovePillarLimitKey = ReadKeyCode(reader);
				settings.PillarDropMultiplierEnabled = reader.ReadBoolean();
				settings.PillarDropMultiplier = reader.ReadInt32();
				settings.TeammateDeathDropBead = reader.ReadBoolean();
				settings.TeammateReviveDropBead = reader.ReadBoolean();
				settings.AutoReviveTeammates = reader.ReadBoolean();
				settings.AutoReviveTeammatesKey = ReadKeyCode(reader);
				settings.AutoReviveWhenSeen = reader.ReadBoolean();
				settings.ReviveHeadOnTruck = reader.ReadBoolean();
				settings.ReviveHeadOnExtraction = reader.ReadBoolean();
				settings.ReviveHeadOnCart = reader.ReadBoolean();
				settings.ReviveAutoToTruck = reader.ReadBoolean();
				settings.DeathAutoToTruck = reader.ReadBoolean();
				settings.DeathAutoToExtraction = reader.ReadBoolean();
				settings.SpawnRateMultiplier = reader.ReadSingle();
				settings.LimitActionAndOvercharge = reader.ReadBoolean();
				if (version >= 13)
				{
					settings.PillarDropMultiplierEnabledKey = ReadKeyCode(reader);
					settings.TeammateDeathDropBeadKey = ReadKeyCode(reader);
					settings.TeammateReviveDropBeadKey = ReadKeyCode(reader);
					settings.AutoReviveWhenSeenKey = ReadKeyCode(reader);
					settings.ReviveHeadOnTruckKey = ReadKeyCode(reader);
					settings.ReviveHeadOnExtractionKey = ReadKeyCode(reader);
					settings.ReviveHeadOnCartKey = ReadKeyCode(reader);
					settings.ReviveAutoToTruckKey = ReadKeyCode(reader);
					settings.DeathAutoToTruckKey = ReadKeyCode(reader);
					settings.DeathAutoToExtractionKey = ReadKeyCode(reader);
					settings.LimitActionAndOverchargeKey = ReadKeyCode(reader);
				}
			} catch { }
		}
	}

	private static void WriteUI(BinaryWriter writer, ConfigData.UiSettings settings)
	{
		WriteKeyCode(writer, settings.MenuToggleKey);
	}

	private static void ReadUI(BinaryReader reader, ConfigData.UiSettings settings, int version)
	{
		settings.MenuToggleKey = ReadKeyCode(reader);
	}

	private static void WriteColor(BinaryWriter writer, Color color)
	{
		writer.Write(color.r);
		writer.Write(color.g);
		writer.Write(color.b);
		writer.Write(color.a);
	}

	private static Color ReadColor(BinaryReader reader)
	{
		return new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	private static void WriteVector2(BinaryWriter writer, Vector2 value)
	{
		writer.Write(value.x);
		writer.Write(value.y);
	}

	private static Vector2 ReadVector2(BinaryReader reader)
	{
		return new Vector2(reader.ReadSingle(), reader.ReadSingle());
	}

	private static void WriteKeyCode(BinaryWriter writer, KeyCode value)
	{
		writer.Write((int)value);
	}

	private static KeyCode ReadKeyCode(BinaryReader reader)
	{
		return (KeyCode)reader.ReadInt32();
	}

	private static void WriteBinaryAtomic(string path, Action<BinaryWriter> writeAction)
	{
		string tempPath = path + ".tmp";
		using (FileStream stream = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
		using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
		{
			writeAction(writer);
			writer.Flush();
			stream.Flush(true);
		}

		if (File.Exists(path))
		{
			File.Delete(path);
		}

		File.Move(tempPath, path);
	}

	private static void WriteTextAtomic(string path, string contents)
	{
		string tempPath = path + ".tmp";
		File.WriteAllText(tempPath, contents ?? string.Empty, Encoding.UTF8);
		if (File.Exists(path))
		{
			File.Delete(path);
		}

		File.Move(tempPath, path);
	}

	private static void BackupUnreadableConfig(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return;
			}

			string backupPath = path + ".corrupt";
			if (File.Exists(backupPath))
			{
				File.Delete(backupPath);
			}

			File.Move(path, backupPath);
		}
		catch
		{
		}
	}
}
