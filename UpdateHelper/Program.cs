using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace REPOUpdater
{
    internal static class Program
    {
        private const int MovefileDelayUntilReboot = 0x4;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                string pidText = ReadArg(args, "--pid");
                string gamePath = ReadArg(args, "--game");
                string dllPath = ReadArg(args, "--dll");
                string payloadPath = ReadArg(args, "--payload");
                string tempPath = ReadArg(args, "--temp");
                string version = ReadArg(args, "--release");

                if (!int.TryParse(pidText, out int oldPid) || !File.Exists(gamePath) || !File.Exists(dllPath) || !File.Exists(payloadPath) || !Directory.Exists(tempPath))
                    throw new InvalidDataException("更新参数不完整或更新文件已丢失。");

                WaitForOldGame(oldPid);
                ReplaceMod(dllPath, payloadPath);
                StartGame(gamePath);
                WaitForGameLaunch(gamePath);
                Thread.Sleep(6000);

                string injectorPath = FindInjector(gamePath, dllPath, tempPath);
                if (injectorPath == null)
                {
                    MessageBox.Show("游戏已重新启动，模组文件已更新至 " + version + "。\n\n未找到 SharpMonoInjector（smi.exe），请使用原加载器重新注入。", "更新完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ScheduleCleanup(tempPath);
                    return 0;
                }

                string processName = Path.GetFileNameWithoutExtension(gamePath);
                string injectionArgs = "inject -p " + Quote(processName) + " -a " + Quote(dllPath) + " -n Cheat -c Loader -m Init";
                ProcessStartInfo inject = new ProcessStartInfo
                {
                    FileName = injectorPath,
                    Arguments = injectionArgs,
                    WorkingDirectory = Path.GetDirectoryName(injectorPath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                bool injected = false;
                using (Process loader = Process.Start(inject))
                {
                    injected = loader != null && loader.WaitForExit(30000) && loader.ExitCode == 0;
                }

                if (!injected)
                {
                    MessageBox.Show("游戏已重新启动，模组文件已更新，但自动注入没有成功。请重新运行加载器。", "更新完成", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ScheduleCleanup(tempPath);
                    return 2;
                }

                MessageBox.Show("更新至 " + version + " 并重新启动完成。", "更新完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ScheduleCleanup(tempPath);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新未能完成：\n" + ex.Message, "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static void WaitForOldGame(int processId)
        {
            try
            {
                using (Process old = Process.GetProcessById(processId))
                {
                    if (!old.WaitForExit(120000))
                        throw new TimeoutException("等待游戏关闭超时，请关闭游戏后重试更新。");
                }
            }
            catch (ArgumentException)
            {
                // The game exited before the updater was scheduled.
            }
        }

        private static void ReplaceMod(string target, string payload)
        {
            string fullTarget = Path.GetFullPath(target);
            string fullPayload = Path.GetFullPath(payload);
            if (!string.Equals(Path.GetFileName(fullTarget), "REPO New Cheat.dll", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Path.GetFileName(fullTarget), "REPO.New.Cheat.dll", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("目标文件名不符合预期，已停止替换。");

            string backup = fullTarget + ".previous";
            string staged = fullTarget + ".new";
            if (File.Exists(backup))
                File.Delete(backup);
            if (File.Exists(staged))
                File.Delete(staged);
            File.Copy(fullPayload, staged, false);
            using (FileStream stagedFile = new FileStream(staged, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                stagedFile.Flush(true);
            File.Replace(staged, fullTarget, backup, true);
            try { File.Delete(backup); } catch { MoveFileEx(backup, null, MovefileDelayUntilReboot); }
        }

        private static void StartGame(string gamePath)
        {
            // Explorer starts the game through the normal shell token even though the updater used UAC.
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = Quote(gamePath),
                WorkingDirectory = Path.GetDirectoryName(gamePath),
                UseShellExecute = true
            };
            Process.Start(start);
        }

        private static void WaitForGameLaunch(string gamePath)
        {
            string processName = Path.GetFileNameWithoutExtension(gamePath);
            DateTime deadline = DateTime.UtcNow.AddMinutes(3);
            while (DateTime.UtcNow < deadline)
            {
                Process[] games = Process.GetProcessesByName(processName);
                foreach (Process game in games)
                {
                    using (game)
                        if (game.Id != Process.GetCurrentProcess().Id) return;
                }
                Thread.Sleep(500);
            }
            throw new TimeoutException("等待游戏重新启动超时，更新文件已替换，请手动启动游戏并重新加载模组。");
        }

        private static string FindInjector(string gamePath, string dllPath, string tempPath)
        {
            string tempInjector = Path.Combine(tempPath, "smi.exe");
            if (File.Exists(tempInjector)) return tempInjector;
            string configured = Environment.GetEnvironmentVariable("REPO_MOD_INJECTOR");
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
                return configured;
            string[] roots = { Path.GetDirectoryName(gamePath), Path.GetDirectoryName(dllPath), AppDomain.CurrentDomain.BaseDirectory };
            string[] names = { "smi.exe", "SharpMonoInjector.exe" };
            foreach (string root in roots.Where(Directory.Exists))
                foreach (string name in names)
                {
                    string candidate = Path.Combine(root, name);
                    if (File.Exists(candidate)) return candidate;
                }
            return null;
        }

        private static string ReadArg(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return string.Empty;
        }

        private static string Quote(string value)
        {
            StringBuilder result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char ch in value ?? string.Empty)
            {
                if (ch == '\\') { slashes++; continue; }
                if (ch == '"') result.Append('\\', slashes * 2 + 1);
                else result.Append('\\', slashes);
                result.Append(ch);
                slashes = 0;
            }
            result.Append('\\', slashes * 2);
            result.Append('"');
            return result.ToString();
        }

        private static void ScheduleCleanup(string directory)
        {
            try
            {
                string cleanupPath = Path.Combine(Path.GetTempPath(), "REPO-Update-Cleanup-" + Guid.NewGuid().ToString("N") + ".cmd");
                string script = "@echo off\r\ntimeout /t 3 /nobreak >nul\r\nrmdir /s /q " + Quote(directory) + "\r\ndel /f /q \"%~f0\"\r";
                File.WriteAllText(cleanupPath, script, Encoding.ASCII);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + Quote(cleanupPath),
                    WorkingDirectory = Path.GetTempPath(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                    MoveFileEx(file, null, MovefileDelayUntilReboot);
            }
            catch { }
        }
    }
}
