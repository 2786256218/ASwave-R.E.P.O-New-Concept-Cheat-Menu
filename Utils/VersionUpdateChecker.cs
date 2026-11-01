using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Cheat.Utils;

/// <summary>检查菜单是否有可用更新。</summary>
public static class VersionUpdateChecker
{
	private const string Repository = "";
	private static int _startupCheckStarted;
	private static int _checking;
	private static volatile string _status = "尚未检查版本更新。";

	public static string Status => _status;
	public static bool IsChecking => Volatile.Read(ref _checking) != 0;

	public static void CheckOnStartupOnce()
	{
		if (Interlocked.Exchange(ref _startupCheckStarted, 1) == 0)
			CheckForUpdates();
	}

	public static void CheckForUpdates()
	{
		if (string.IsNullOrWhiteSpace(Repository))
		{
			_status = "版本检查暂未配置。";
			return;
		}

		if (Interlocked.CompareExchange(ref _checking, 1, 0) != 0)
		{
			_status = "正在检查版本更新……";
			return;
		}

		_status = "正在检查版本更新……";
		ThreadPool.QueueUserWorkItem(_ => CheckWorker());
	}

	private static void CheckWorker()
	{
		try
		{
			if (!TryBuildLatestReleaseUrl(Repository, out string url))
			{
				_status = "暂时无法检查更新，请稍后再试。";
				return;
			}

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";
			request.UserAgent = "REPO-Mod-Version-Checker";
			request.Accept = "application/vnd.github+json";
			request.Timeout = 5000;
			request.ReadWriteTimeout = 5000;
			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (System.IO.Stream stream = response.GetResponseStream())
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				string json = reader.ReadToEnd();
				Match tagMatch = Regex.Match(json, "\\\"tag_name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
				Match urlMatch = Regex.Match(json, "\\\"html_url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
				if (!tagMatch.Success)
				{
					_status = "暂未找到可用的新版本信息。";
					return;
				}

				string latestTag = tagMatch.Groups[1].Value;
				string releaseUrl = urlMatch.Success ? urlMatch.Groups[1].Value : string.Empty;
				if (TryParseVersion(latestTag, out System.Version latest) && latest.CompareTo(GetCurrentVersion()) > 0)
					_status = "发现新版本：" + latestTag + (string.IsNullOrEmpty(releaseUrl) ? string.Empty : " | " + releaseUrl);
				else
					_status = "当前已是最新版本（" + latestTag + "）。";
			}
		}
		catch (WebException)
		{
			_status = "暂时无法检查更新，请稍后再试。";
		}
		catch (Exception)
		{
			_status = "暂时无法检查更新，请稍后再试。";
		}
		finally
		{
			Interlocked.Exchange(ref _checking, 0);
		}
	}

	private static bool TryBuildLatestReleaseUrl(string repository, out string url)
	{
		url = null;
		string value = repository.Trim().TrimEnd('/');
		if (value.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
			value = value.Substring("https://github.com/".Length);
		if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
			value = value.Substring(0, value.Length - 4);

		string[] parts = value.Split('/');
		if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
			return false;

		url = "https://api.github.com/repos/" + Uri.EscapeDataString(parts[0]) + "/" + Uri.EscapeDataString(parts[1]) + "/releases/latest";
		return true;
	}

	private static System.Version GetCurrentVersion()
	{
		return Assembly.GetExecutingAssembly().GetName().Version ?? new System.Version(0, 0);
	}

	private static bool TryParseVersion(string tag, out System.Version version)
	{
		version = null;
		string value = (tag ?? string.Empty).Trim();
		while (value.Length > 0 && (value[0] == 'v' || value[0] == 'V'))
			value = value.Substring(1);

		Match match = Regex.Match(value, "\\d+(?:\\.\\d+){1,3}");
		return match.Success && System.Version.TryParse(match.Value, out version);
	}
}
