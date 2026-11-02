using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using SystemVersion = System.Version;

namespace Cheat.Utils;

public enum UpdateCheckNotice
{
	None,
	TimedOut,
	UpdateAvailable
}

public sealed class UpdateReleaseInfo
{
	public string Version { get; internal set; }
	public string PageUrl { get; internal set; }
	public string DllUrl { get; internal set; }
	public string DllSha256 { get; internal set; }
	public string InjectorUrl { get; internal set; }
	public string InjectorSha256 { get; internal set; }
}

/// <summary>Checks the configured GitHub repository for a newer release.</summary>
public static class VersionUpdateChecker
{
	public const string Repository = "2786256218/ASwave-R.E.P.O-New-Concept-Cheat-Menu";
	public const string RepositoryPage = "https://github.com/2786256218/ASwave-R.E.P.O-New-Concept-Cheat-Menu";
	private const int RequestTimeoutMilliseconds = 10000;
	private static readonly object Sync = new object();
	private static int _startupCheckStarted;
	private static int _checking;
	private static int _pendingNotice;
	private static UpdateReleaseInfo _availableRelease;
	private static volatile string _status = "尚未检查版本更新。";

	public static string Status => _status;
	public static bool IsChecking => Volatile.Read(ref _checking) != 0;
	public static UpdateReleaseInfo AvailableRelease
	{
		get
		{
			lock (Sync)
				return _availableRelease;
		}
	}

	public static void CheckOnStartupOnce()
	{
		if (Interlocked.Exchange(ref _startupCheckStarted, 1) == 0)
			CheckForUpdates();
	}

	public static void CheckForUpdates()
	{
		if (Interlocked.CompareExchange(ref _checking, 1, 0) != 0)
		{
			_status = "正在检查版本更新……";
			return;
		}

		_status = "正在检查版本更新……";
		lock (Sync)
			_availableRelease = null;
		ThreadPool.QueueUserWorkItem(_ => CheckWorker());
	}

	public static bool TryConsumeNotice(out UpdateCheckNotice notice, out UpdateReleaseInfo release)
	{
		int pending = Interlocked.Exchange(ref _pendingNotice, 0);
		notice = (UpdateCheckNotice)pending;
		lock (Sync)
			release = _availableRelease;
		return notice != UpdateCheckNotice.None;
	}

	private static void CheckWorker()
	{
		try
		{
			if (!TryBuildLatestReleaseUrl(Repository, out string url))
			{
				_status = "暂时无法检查更新。";
				return;
			}

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";
			request.UserAgent = "REPO-Mod-Version-Checker";
			request.Accept = "application/vnd.github+json";
			request.Timeout = RequestTimeoutMilliseconds;
			request.ReadWriteTimeout = RequestTimeoutMilliseconds;
			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (System.IO.Stream stream = response.GetResponseStream())
			using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
			{
				string json = reader.ReadToEnd();
				if (!TryReadRelease(json, out UpdateReleaseInfo release, out SystemVersion latestVersion))
				{
					_status = "暂未找到可用的新版本信息。";
					return;
				}

				if (SystemVersion.TryParse(AppVersion.Number, out SystemVersion currentVersion) && latestVersion.CompareTo(currentVersion) > 0)
				{
					lock (Sync)
						_availableRelease = release;
					_status = "发现新版本：" + release.Version;
					Interlocked.Exchange(ref _pendingNotice, (int)UpdateCheckNotice.UpdateAvailable);
				}
				else
				{
					lock (Sync)
						_availableRelease = null;
					_status = "当前已是最新版本（" + AppVersion.Display + "）。";
				}
			}
		}
		catch (WebException ex)
		{
			if (ex.Status == WebExceptionStatus.Timeout)
			{
				_status = "连接 GitHub 超时，请检查网络后重试。";
				Interlocked.Exchange(ref _pendingNotice, (int)UpdateCheckNotice.TimedOut);
			}
			else
			{
				_status = "暂时无法连接 GitHub，请稍后重试。";
			}
		}
		catch (Exception)
		{
			_status = "暂时无法检查更新，请稍后重试。";
		}
		finally
		{
			Interlocked.Exchange(ref _checking, 0);
		}
	}

	private static bool TryReadRelease(string json, out UpdateReleaseInfo release, out SystemVersion version)
	{
		release = null;
		version = null;
		Match tagMatch = Regex.Match(json, "\\\"tag_name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
		Match pageMatch = Regex.Match(json, "\\\"html_url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
		if (!tagMatch.Success || !TryParseVersion(tagMatch.Groups[1].Value, out version))
			return false;

		string assetJson = string.Empty;
		Match assetsMatch = Regex.Match(json, "\\\"assets\\\"\\s*:\\s*\\[(.*?)\\]\\s*,\\s*\\\"tarball_url\\\"", RegexOptions.Singleline);
		if (assetsMatch.Success)
			assetJson = assetsMatch.Groups[1].Value;

		List<AssetInfo> assets = ReadAssets(assetJson);
		AssetInfo dll = null;
		AssetInfo injector = null;
		for (int i = 0; i < assets.Count; i++)
		{
			AssetInfo asset = assets[i];
			if (asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
				(asset.Name.IndexOf("REPO", StringComparison.OrdinalIgnoreCase) >= 0 || asset.Name.IndexOf("Cheat", StringComparison.OrdinalIgnoreCase) >= 0))
			{
				if (dll == null || asset.Name.Equals("REPO.New.Cheat.dll", StringComparison.OrdinalIgnoreCase) || asset.Name.Equals("REPO New Cheat.dll", StringComparison.OrdinalIgnoreCase))
					dll = asset;
			}
			if (asset.Name.Equals("smi.exe", StringComparison.OrdinalIgnoreCase) || asset.Name.Equals("SharpMonoInjector.exe", StringComparison.OrdinalIgnoreCase))
				injector = asset;
		}

		release = new UpdateReleaseInfo
		{
			Version = tagMatch.Groups[1].Value,
			PageUrl = pageMatch.Success ? UnescapeJson(pageMatch.Groups[1].Value) : "https://github.com/" + Repository + "/releases/latest",
			DllUrl = dll?.Url,
			DllSha256 = dll?.Sha256,
			InjectorUrl = injector?.Url,
			InjectorSha256 = injector?.Sha256
		};
		return true;
	}

	private sealed class AssetInfo
	{
		public string Name;
		public string Url;
		public string Sha256;
	}

	private static List<AssetInfo> ReadAssets(string json)
	{
		List<AssetInfo> result = new List<AssetInfo>();
		MatchCollection names = Regex.Matches(json, "\\\"name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
		for (int i = 0; i < names.Count; i++)
		{
			int start = names[i].Index;
			int end = i + 1 < names.Count ? names[i + 1].Index : json.Length;
			string block = json.Substring(start, end - start);
			Match url = Regex.Match(block, "\\\"browser_download_url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
			if (!url.Success)
				continue;
			Match digest = Regex.Match(block, "\\\"digest\\\"\\s*:\\s*\\\"sha256:([0-9a-fA-F]{64})\\\"");
			result.Add(new AssetInfo
			{
				Name = UnescapeJson(names[i].Groups[1].Value),
				Url = UnescapeJson(url.Groups[1].Value),
				Sha256 = digest.Success ? digest.Groups[1].Value : null
			});
		}
		return result;
	}

	private static string UnescapeJson(string value)
	{
		return (value ?? string.Empty).Replace("\\/", "/").Replace("\\u0026", "&").Replace("\\u002F", "/");
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

	private static bool TryParseVersion(string tag, out SystemVersion version)
	{
		version = null;
		string value = (tag ?? string.Empty).Trim();
		while (value.Length > 0 && (value[0] == 'v' || value[0] == 'V'))
			value = value.Substring(1);

		Match match = Regex.Match(value, "\\d+(?:\\.\\d+){1,3}");
		return match.Success && SystemVersion.TryParse(match.Value, out version);
	}
}
