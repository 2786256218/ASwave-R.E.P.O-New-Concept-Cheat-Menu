using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Reflection;
using System.Threading;

namespace Cheat.Utils;

/// <summary>Downloads signed-by-release-checksum update assets and starts the standalone updater.</summary>
public static class DirectUpdateManager
{
	private static readonly object Sync = new object();
	private static int _busy;
	private static bool _resultPending;
	private static bool _resultReady;
	private static string _resultMessage;
	private static string _updaterPath;
	private static string _payloadPath;
	private static string _tempDirectory;
	private static bool _handedOffToUpdater;
	private static int _cancelRequested;

	public static void CancelPreparedUpdate()
	{
		if (Volatile.Read(ref _busy) != 0)
		{
			Interlocked.Exchange(ref _cancelRequested, 1);
			return;
		}
		lock (Sync)
		{
			if (_handedOffToUpdater)
				return;
			TryDeleteDirectory(_tempDirectory);
			_tempDirectory = null;
			_updaterPath = null;
			_payloadPath = null;
		}
	}

	public static void Begin(UpdateReleaseInfo release)
	{
		if (release == null || string.IsNullOrWhiteSpace(release.DllUrl))
		{
			PublishResult(false, "该版本没有可下载的模组文件，请访问 GitHub 查看版本。", null, null);
			return;
		}
		if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
			return;
		Interlocked.Exchange(ref _cancelRequested, 0);

		ThreadPool.QueueUserWorkItem(_ => DownloadWorker(release));
	}

	public static bool TryConsumeResult(out bool ready, out string message, out string updaterPath, out string payloadPath)
	{
		lock (Sync)
		{
			if (!_resultPending)
			{
				ready = false;
				message = null;
				updaterPath = null;
				payloadPath = null;
				return false;
			}
			_resultPending = false;
			ready = _resultReady;
			message = _resultMessage;
			updaterPath = _updaterPath;
			payloadPath = _payloadPath;
			return true;
		}
	}

	public static void LaunchUpdater(string updaterPath, string payloadPath, string releaseVersion)
	{
		if (!File.Exists(updaterPath) || !File.Exists(payloadPath))
			throw new FileNotFoundException("更新文件不完整，请重新下载。", updaterPath);

		string gamePath = Process.GetCurrentProcess().MainModule.FileName;
		string dllPath = Assembly.GetExecutingAssembly().Location;
		if (string.IsNullOrWhiteSpace(gamePath) || !File.Exists(gamePath))
			throw new InvalidOperationException("无法定位游戏程序。");
		if (string.IsNullOrWhiteSpace(dllPath) || !File.Exists(dllPath))
			throw new InvalidOperationException("无法定位当前模组文件。");

		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = updaterPath,
			Arguments = "--pid " + Process.GetCurrentProcess().Id +
				" --game " + Quote(gamePath) +
				" --dll " + Quote(dllPath) +
				" --payload " + Quote(payloadPath) +
				" --temp " + Quote(_tempDirectory ?? Path.GetDirectoryName(updaterPath)) +
				" --release " + Quote(releaseVersion ?? string.Empty),
			WorkingDirectory = Path.GetDirectoryName(updaterPath),
			UseShellExecute = true,
			Verb = "runas",
			WindowStyle = ProcessWindowStyle.Normal,
			ErrorDialog = true
		};
		// ShellExecute + runas starts the extracted updater as a separate elevated process.
		// Dispose only the local process handle; do not wait for or own the updater lifetime.
		using (Process updater = Process.Start(startInfo))
		{
			if (updater == null)
				throw new InvalidOperationException("Windows 未能启动独立更新程序。");
			if (updater.Id == Process.GetCurrentProcess().Id)
				throw new InvalidOperationException("更新程序未能脱离游戏进程启动。");
		}
		lock (Sync)
			_handedOffToUpdater = true;
	}

	private static void DownloadWorker(UpdateReleaseInfo release)
	{
		string directory = null;
		try
		{
			directory = Path.Combine(Path.GetTempPath(), "REPO-Mod-Update-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			string updaterPath = Path.Combine(directory, "REPOUpdater.exe");
			string dllPath = Path.Combine(directory, "mod-update.dll");
			using (Stream updaterResource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Cheat.Resources.REPOUpdater.exe"))
			{
				if (updaterResource == null)
					throw new InvalidDataException("当前版本未包含更新程序，请重新安装完整构建版本后重试。");
				using (FileStream updaterFile = new FileStream(updaterPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
					updaterResource.CopyTo(updaterFile);
			}
			DownloadFile(release.DllUrl, dllPath, release.DllSha256);
			ThrowIfCancellationRequested();
			if (new FileInfo(dllPath).Length < 4096 || new FileInfo(updaterPath).Length < 4096)
				throw new InvalidDataException("GitHub 返回的更新文件内容不完整。");

			lock (Sync)
			{
				_tempDirectory = directory;
				_updaterPath = updaterPath;
			}
			ThrowIfCancellationRequested();
			PublishResult(true, null, updaterPath, dllPath);
		}
		catch (Exception ex)
		{
			TryDeleteDirectory(directory);
			lock (Sync)
			{
				_tempDirectory = null;
				_updaterPath = null;
			}
			PublishResult(false, "下载更新失败：" + ex.Message, null, null);
		}
		finally
		{
			Interlocked.Exchange(ref _busy, 0);
		}
	}

	private static void ThrowIfCancellationRequested()
	{
		if (Volatile.Read(ref _cancelRequested) != 0)
			throw new OperationCanceledException("更新下载已取消。");
	}

	private static void TryDeleteDirectory(string directory)
	{
		try
		{
			if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
				Directory.Delete(directory, true);
		}
		catch { }
	}

	private static void DownloadFile(string url, string destination, string expectedSha256)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps)
			throw new InvalidDataException("GitHub 更新地址无效。");

		HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
		request.Method = "GET";
		request.UserAgent = "REPO-Mod-Updater";
		request.Timeout = 30000;
		request.ReadWriteTimeout = 30000;
		using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
		using (Stream input = response.GetResponseStream())
		using (FileStream output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
		{
			byte[] buffer = new byte[81920];
			long total = 0;
			int read;
			while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				total += read;
				if (total > 100L * 1024L * 1024L)
					throw new InvalidDataException("更新文件超过允许大小。");
				output.Write(buffer, 0, read);
			}
		}

		if (!string.IsNullOrWhiteSpace(expectedSha256))
		{
			using (SHA256 sha = SHA256.Create())
		using (FileStream stream = File.OpenRead(destination))
			{
				string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
				if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
					throw new InvalidDataException("更新文件校验失败，请重新检查 GitHub 发布文件。");
			}
		}
	}

	private static void PublishResult(bool ready, string message, string updaterPath, string payloadPath)
	{
		lock (Sync)
		{
			_resultReady = ready;
			_resultMessage = message;
			_updaterPath = updaterPath;
			_payloadPath = payloadPath;
			_resultPending = true;
		}
	}

	private static string Quote(string value)
	{
		return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
	}
}
