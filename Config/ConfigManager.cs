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

	private const int ConfigVersion = 1;

	private const int TextConfigVersion = 3;

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
		config.Minimap ??= new ConfigData.MinimapSettings();
		config.Local ??= new ConfigData.LocalSettings();
		config.Misc ??= new ConfigData.MiscSettings();
		config.Compass ??= new ConfigData.CompassSettings();
		config.PlayerEsp ??= new ConfigData.PlayerEspSettings();
		config.LaserSight ??= new ConfigData.LaserSightSettings();
		config.Structures ??= new ConfigData.StructureEspSettings();
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
			WriteLoot(writer, config.Loot);
			WriteEnemies(writer, config.Enemies);
			WriteMinimap(writer, config.Minimap);
			WriteLocal(writer, config.Local);
			WriteMisc(writer, config.Misc);
			WriteCompass(writer, config.Compass);
			WritePlayerEsp(writer, config.PlayerEsp);
			WriteLaserSight(writer, config.LaserSight);
			WriteStructures(writer, config.Structures);
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
		if (version != ConfigVersion)
		{
			throw new InvalidDataException("Unsupported config version: " + version);
		}

		ConfigData config = new ConfigData();
		ReadLoot(reader, config.Loot);
		ReadEnemies(reader, config.Enemies);
		ReadMinimap(reader, config.Minimap);
		ReadLocal(reader, config.Local);
		ReadMisc(reader, config.Misc);
		ReadCompass(reader, config.Compass);
		ReadPlayerEsp(reader, config.PlayerEsp);
		ReadLaserSight(reader, config.LaserSight);
		ReadStructures(reader, config.Structures);
		return config;
	}

	private static void WriteLoot(BinaryWriter writer, ConfigData.LootSettings settings)
	{
		writer.Write(settings.Enabled);
		writer.Write(settings.DrawTracers);
		writer.Write(settings.DrawBox);
		writer.Write(settings.DrawName);
		writer.Write(settings.MaxDistance);
		writer.Write(settings.MinValue);
		writer.Write(settings.UseClustering);
		writer.Write(settings.DynamicOpacity);
		writer.Write(settings.ShowCartUI);
		WriteColor(writer, settings.EspColor);
		writer.Write(settings.HighlightEnabled);
		writer.Write(settings.HighlightDistance);
		WriteColor(writer, settings.HighlightColorVisible);
		WriteColor(writer, settings.HighlightColorOccluded);
		WriteKeyCode(writer, settings.ToggleKey);
	}

	private static void ReadLoot(BinaryReader reader, ConfigData.LootSettings settings)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.DrawTracers = reader.ReadBoolean();
		settings.DrawBox = reader.ReadBoolean();
		settings.DrawName = reader.ReadBoolean();
		settings.MaxDistance = reader.ReadSingle();
		settings.MinValue = reader.ReadInt32();
		settings.UseClustering = reader.ReadBoolean();
		settings.DynamicOpacity = reader.ReadBoolean();
		settings.ShowCartUI = reader.ReadBoolean();
		settings.EspColor = ReadColor(reader);
		settings.HighlightEnabled = reader.ReadBoolean();
		settings.HighlightDistance = reader.ReadSingle();
		settings.HighlightColorVisible = ReadColor(reader);
		settings.HighlightColorOccluded = ReadColor(reader);
		settings.ToggleKey = ReadKeyCode(reader);
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
		writer.Write(settings.MaxDistance);
		WriteColor(writer, settings.EspColor);
		writer.Write(settings.HighlightEnabled);
		writer.Write(settings.HighlightMaxDistance);
		WriteColor(writer, settings.HighlightColor);
		writer.Write(settings.RenderMethod);
		WriteKeyCode(writer, settings.ToggleKey);
	}

	private static void ReadEnemies(BinaryReader reader, ConfigData.EnemySettings settings)
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
		settings.MaxDistance = reader.ReadSingle();
		settings.EspColor = ReadColor(reader);
		settings.HighlightEnabled = reader.ReadBoolean();
		settings.HighlightMaxDistance = reader.ReadSingle();
		settings.HighlightColor = ReadColor(reader);
		settings.RenderMethod = reader.ReadInt32();
		settings.ToggleKey = ReadKeyCode(reader);
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
	}

	private static void ReadMinimap(BinaryReader reader, ConfigData.MinimapSettings settings)
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
		writer.Write(settings.RunSpeed);
		writer.Write(settings.NoClipSpeed);
		WriteKeyCode(writer, settings.GodModeKey);
		WriteKeyCode(writer, settings.NoClipKey);
		writer.Write(settings.FreeCamSpeed);
		writer.Write(settings.FreeCamFastMultiplier);
		writer.Write(settings.FreeCamSensitivity);
	}

	private static void ReadLocal(BinaryReader reader, ConfigData.LocalSettings settings)
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
		settings.RunSpeed = reader.ReadSingle();
		settings.NoClipSpeed = reader.ReadSingle();
		settings.GodModeKey = ReadKeyCode(reader);
		settings.NoClipKey = ReadKeyCode(reader);
		settings.FreeCamSpeed = reader.ReadSingle();
		settings.FreeCamFastMultiplier = reader.ReadSingle();
		settings.FreeCamSensitivity = reader.ReadSingle();
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
	}

	private static void ReadMisc(BinaryReader reader, ConfigData.MiscSettings settings)
	{
		settings.ToggleKey = ReadKeyCode(reader);
		settings.Crosshair = reader.ReadBoolean();
		settings.ShowFps = reader.ReadBoolean();
		settings.ShowKeybinds = reader.ReadBoolean();
		settings.MenuAccent = ReadColor(reader);
		settings.FOV = reader.ReadSingle();
		settings.Fullbright = reader.ReadBoolean();
		settings.FullbrightIntensity = reader.ReadSingle();
		settings.NoFog = reader.ReadBoolean();
		settings.SetItemValue = reader.ReadInt32();
	}

	private static void WriteCompass(BinaryWriter writer, ConfigData.CompassSettings settings)
	{
		writer.Write(settings.Enabled);
		writer.Write(settings.Size);
		writer.Write(settings.Range);
		writer.Write(settings.Scale);
		writer.Write(settings.YOffset);
		WriteKeyCode(writer, settings.ToggleKey);
	}

	private static void ReadCompass(BinaryReader reader, ConfigData.CompassSettings settings)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.Size = reader.ReadSingle();
		settings.Range = reader.ReadSingle();
		settings.Scale = reader.ReadSingle();
		settings.YOffset = reader.ReadSingle();
		settings.ToggleKey = ReadKeyCode(reader);
	}

	private static void WritePlayerEsp(BinaryWriter writer, ConfigData.PlayerEspSettings settings)
	{
		writer.Write(settings.Enabled);
		writer.Write(settings.DrawName);
		writer.Write(settings.DrawHealth);
		writer.Write(settings.DrawDistance);
		writer.Write(settings.DrawHeldItem);
		WriteColor(writer, settings.Color);
		WriteColor(writer, settings.EquipmentColor);
	}

	private static void ReadPlayerEsp(BinaryReader reader, ConfigData.PlayerEspSettings settings)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.DrawName = reader.ReadBoolean();
		settings.DrawHealth = reader.ReadBoolean();
		settings.DrawDistance = reader.ReadBoolean();
		settings.DrawHeldItem = reader.ReadBoolean();
		settings.Color = ReadColor(reader);
		settings.EquipmentColor = ReadColor(reader);
	}

	private static void WriteLaserSight(BinaryWriter writer, ConfigData.LaserSightSettings settings)
	{
		writer.Write(settings.Enabled);
		writer.Write(settings.ShowLocal);
		writer.Write(settings.ShowOthers);
		writer.Write(settings.ShowHitInfo);
		WriteColor(writer, settings.Color);
		writer.Write(settings.Width);
	}

	private static void ReadLaserSight(BinaryReader reader, ConfigData.LaserSightSettings settings)
	{
		settings.Enabled = reader.ReadBoolean();
		settings.ShowLocal = reader.ReadBoolean();
		settings.ShowOthers = reader.ReadBoolean();
		settings.ShowHitInfo = reader.ReadBoolean();
		settings.Color = ReadColor(reader);
		settings.Width = reader.ReadSingle();
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
	}

	private static void ReadStructures(BinaryReader reader, ConfigData.StructureEspSettings settings)
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
