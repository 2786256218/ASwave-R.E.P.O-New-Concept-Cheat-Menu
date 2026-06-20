using System;
using System.Collections.Generic;

namespace Cheat.Utils;

public static class Logger
{
	private static readonly object _lock;

	private static readonly HashSet<string> _ignoredCategories;

	public static bool EnableDebug { get; set; }

	public static bool EnableTimestamp { get; set; }

	static Logger()
	{
		_lock = new object();
		_ignoredCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		EnableDebug = true;
		EnableTimestamp = true;
		_ignoredCategories.Add("ESP");
		_ignoredCategories.Add("Render");
	}

	public static void Log(string message, LogLevel level = LogLevel.Info, string category = "General")
	{
		if (_ignoredCategories.Contains(category) || (level == LogLevel.Debug && !EnableDebug))
		{
			return;
		}
		lock (_lock)
		{
			ConsoleColor foregroundColor = Console.ForegroundColor;
			string value = (EnableTimestamp ? $"[{DateTime.Now:HH:mm:ss}] " : "");
			string text = "[" + level.ToString().ToUpper() + "]";
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write(value);
			switch (level)
			{
			case LogLevel.Debug:
				Console.ForegroundColor = ConsoleColor.DarkGray;
				break;
			case LogLevel.Info:
				Console.ForegroundColor = ConsoleColor.White;
				break;
			case LogLevel.Warning:
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
			case LogLevel.Error:
				Console.ForegroundColor = ConsoleColor.Red;
				break;
			case LogLevel.Critical:
				Console.ForegroundColor = ConsoleColor.DarkRed;
				break;
			}
			Console.Write(text + " ");
			if (!string.IsNullOrEmpty(category) && category != "General")
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write("[" + category + "] ");
			}
			Console.ForegroundColor = foregroundColor;
			Console.WriteLine(message);
		}
	}

	public static void Debug(string message, string category = "General")
	{
		Log(message, LogLevel.Debug, category);
	}

	public static void Info(string message, string category = "General")
	{
		Log(message, LogLevel.Info, category);
	}

	public static void Warn(string message, string category = "General")
	{
		Log(message, LogLevel.Warning, category);
	}

	public static void Error(string message, string category = "General")
	{
		Log(message, LogLevel.Error, category);
	}

	public static void Critical(string message, string category = "General")
	{
		Log(message, LogLevel.Critical, category);
	}

	public static void IgnoreCategory(string category)
	{
		lock (_lock)
		{
			if (!_ignoredCategories.Contains(category))
			{
				_ignoredCategories.Add(category);
			}
		}
	}

	public static void EnableCategory(string category)
	{
		lock (_lock)
		{
			if (_ignoredCategories.Contains(category))
			{
				_ignoredCategories.Remove(category);
			}
		}
	}
}
