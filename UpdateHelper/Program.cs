using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SharpMonoInjector;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace REPOUpdater
{
    internal static class Program
    {
        private const int MovefileDelayUntilReboot = 0x4;
        private const string SteamGameLaunchUri = "steam://rungameid/3241660";

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);

        private const int GameExitTimeoutSeconds = 120;
        private const int SteamMonitorSettleSeconds = 20;

        [STAThread]
        private static int Main(string[] args)
        {
            string cleanupDirectory = ReadArg(args, "--temp");
            if (!HasArg(args, "--worker"))
            {
                try
                {
                    ValidateUpdateArguments(args);
                    StartWorkerThroughTaskScheduler(args, cleanupDirectory);
                    return 0;
                }
                catch (Exception ex)
                {
                    string details = ex.GetType().Name + " (0x" + ex.HResult.ToString("X8") + "): " + ex.Message;
                    ShowTimedMessage("无法启动独立更新流程", details, 7000, true);
                    ScheduleCleanup(cleanupDirectory);
                    return 1;
                }
            }

            string taskName = ReadArg(args, "--task");
            try
            {
                string pidText = ReadArg(args, "--pid");
                string gamePath = ReadArg(args, "--game");
                string dllPath = ReadArg(args, "--dll");
                string payloadPath = ReadArg(args, "--payload");
                string tempPath = cleanupDirectory;
                string version = ReadArg(args, "--release");

                ValidateUpdateArguments(args);
                if (!int.TryParse(pidText, out int oldPid))
                    throw new InvalidDataException("无法读取游戏进程编号。");

                WaitForSteamGameExit(oldPid, gamePath);
                ReplaceMod(dllPath, payloadPath);
                Thread.Sleep(5000);
                StartGame();
                int restartedGamePid = WaitForGameLaunch(gamePath);
                Thread.Sleep(6000);

                InjectUpdatedMod(restartedGamePid, dllPath);

                ShowTimedMessage("更新完成", "更新至 " + version + "，游戏已重新启动。", 1800, false);
                return 0;
            }
            catch (Exception ex)
            {
                ShowTimedMessage("更新未能完成", ex.Message, 4500, true);
                return 1;
            }
            finally
            {
                DeleteScheduledTask(taskName);
                ScheduleCleanup(cleanupDirectory);
            }
        }

        private static void ValidateUpdateArguments(string[] args)
        {
            string pidText = ReadArg(args, "--pid");
            string gamePath = ReadArg(args, "--game");
            string dllPath = ReadArg(args, "--dll");
            string payloadPath = ReadArg(args, "--payload");
            string tempPath = ReadArg(args, "--temp");
            if (!int.TryParse(pidText, out int oldPid) || oldPid == Process.GetCurrentProcess().Id ||
                !File.Exists(gamePath) || !File.Exists(dllPath) || !File.Exists(payloadPath) || !Directory.Exists(tempPath))
                throw new InvalidDataException("更新参数不完整，或更新程序未脱离游戏进程运行。");
        }

        private static void StartWorkerThroughTaskScheduler(string[] args, string tempPath)
        {
            string workerPath = Process.GetCurrentProcess().MainModule.FileName;
            if (string.IsNullOrWhiteSpace(workerPath) || !File.Exists(workerPath))
                throw new FileNotFoundException("无法定位临时更新程序。");

            string taskName = "REPO-Mod-Update-" + Guid.NewGuid().ToString("N");
            string arguments = "--worker --task " + Quote(taskName) +
                " --pid " + Quote(ReadArg(args, "--pid")) +
                " --game " + Quote(ReadArg(args, "--game")) +
                " --dll " + Quote(ReadArg(args, "--dll")) +
                " --payload " + Quote(ReadArg(args, "--payload")) +
                " --temp " + Quote(tempPath) +
                " --release " + Quote(ReadArg(args, "--release"));

            dynamic scheduler = null;
            dynamic folder = null;
            dynamic definition = null;
            dynamic registeredTask = null;
            dynamic trigger = null;
            dynamic action = null;
            try
            {
                Type schedulerType = Type.GetTypeFromProgID("Schedule.Service", true);
                scheduler = Activator.CreateInstance(schedulerType);
                scheduler.Connect(null, null, null, null);
                folder = scheduler.GetFolder("\\");
                definition = scheduler.NewTask(0);

                definition.RegistrationInfo.Description = "REPO 更新临时任务";
                definition.Principal.UserId = WindowsIdentity.GetCurrent().Name;
                definition.Principal.LogonType = 3; // TASK_LOGON_INTERACTIVE_TOKEN
                definition.Principal.RunLevel = 1; // TASK_RUNLEVEL_HIGHEST
                definition.Settings.Enabled = true;
                definition.Settings.AllowDemandStart = true;
                definition.Settings.StartWhenAvailable = true;
                definition.Settings.DisallowStartIfOnBatteries = false;
                definition.Settings.StopIfGoingOnBatteries = false;
                definition.Settings.ExecutionTimeLimit = "PT2H";
                definition.Settings.DeleteExpiredTaskAfter = "PT1H";

                trigger = definition.Triggers.Create(1); // TASK_TRIGGER_TIME; disabled, Run() starts immediately
                DateTimeOffset triggerStart = DateTimeOffset.Now.AddDays(1);
                trigger.StartBoundary = triggerStart.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
                trigger.EndBoundary = triggerStart.AddHours(1).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
                trigger.Enabled = false;

                action = definition.Actions.Create(0); // TASK_ACTION_EXEC
                action.Path = workerPath;
                action.Arguments = arguments;
                action.WorkingDirectory = tempPath;

                registeredTask = folder.RegisterTaskDefinition(taskName, definition, 6,
                    WindowsIdentity.GetCurrent().Name, null, 3, null); // TASK_CREATE_OR_UPDATE / INTERACTIVE_TOKEN
                registeredTask.Run(null);
            }
            finally
            {
                ReleaseComObject(registeredTask);
                ReleaseComObject(action);
                ReleaseComObject(trigger);
                ReleaseComObject(definition);
                ReleaseComObject(folder);
                ReleaseComObject(scheduler);
            }
        }

        private static void DeleteScheduledTask(string taskName)
        {
            if (string.IsNullOrWhiteSpace(taskName))
                return;
            dynamic scheduler = null;
            dynamic folder = null;
            try
            {
                Type schedulerType = Type.GetTypeFromProgID("Schedule.Service", false);
                if (schedulerType == null)
                    return;
                scheduler = Activator.CreateInstance(schedulerType);
                scheduler.Connect(null, null, null, null);
                folder = scheduler.GetFolder("\\");
                folder.DeleteTask(taskName, 0);
            }
            catch { }
            finally
            {
                ReleaseComObject(folder);
                ReleaseComObject(scheduler);
            }
        }

        private static void ReleaseComObject(object value)
        {
            if (value != null && Marshal.IsComObject(value))
            {
                try { Marshal.FinalReleaseComObject(value); } catch { }
            }
        }

        private static void WaitForSteamGameExit(int processId, string gamePath)
        {
            using (Form form = new Form())
            using (Panel accentBar = new Panel())
            using (Label header = new Label())
            using (Label message = new Label())
            using (Label countdown = new Label())
            using (ProgressBar settleProgress = new ProgressBar())
            using (Button skip = new Button())
            using (WinFormsTimer timer = new WinFormsTimer())
            {
                DateTime deadline = DateTime.UtcNow.AddSeconds(GameExitTimeoutSeconds);
                DateTime? exitedAt = null;
                bool completed = false;
                form.Text = "REPO 更新程序 · PID " + Process.GetCurrentProcess().Id;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowInTaskbar = true;
                form.TopMost = true;
                form.ClientSize = new Size(560, 258);
                form.BackColor = Color.FromArgb(9, 17, 31);
                form.ForeColor = Color.FromArgb(225, 238, 250);

                accentBar.SetBounds(0, 0, 560, 4);
                accentBar.BackColor = Color.FromArgb(36, 205, 232);
                header.SetBounds(24, 14, 512, 28);
                header.Text = "REPO UPDATE  /  STEAM SESSION";
                header.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11f, FontStyle.Bold);
                header.ForeColor = Color.FromArgb(36, 205, 232);
                header.TextAlign = ContentAlignment.MiddleLeft;

                message.SetBounds(24, 50, 512, 84);
                message.TextAlign = ContentAlignment.MiddleLeft;
                message.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 13f, FontStyle.Regular);
                message.ForeColor = Color.FromArgb(205, 220, 238);
                message.Text = "正在等待 Steam 中的游戏进程结束。\r\n也可以在 Steam 中手动点击“关闭”结束游戏；检测到进程退出后，可跳过倒计时。";
                countdown.SetBounds(24, 140, 512, 28);
                countdown.TextAlign = ContentAlignment.MiddleCenter;
                countdown.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 12f, FontStyle.Bold);
                countdown.ForeColor = Color.FromArgb(36, 205, 232);
                settleProgress.SetBounds(24, 174, 512, 8);
                settleProgress.Minimum = 0;
                settleProgress.Maximum = SteamMonitorSettleSeconds;
                settleProgress.Value = 0;
                settleProgress.Style = ProgressBarStyle.Continuous;
                skip.SetBounds(165, 198, 230, 40);
                skip.Text = "等待游戏进程退出…";
                skip.Enabled = false;
                skip.FlatStyle = FlatStyle.Flat;
                skip.FlatAppearance.BorderColor = Color.FromArgb(36, 205, 232);
                skip.FlatAppearance.BorderSize = 1;
                skip.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 48, 68);
                skip.BackColor = Color.FromArgb(13, 29, 46);
                skip.ForeColor = Color.FromArgb(135, 160, 180);
                skip.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11f, FontStyle.Bold);
                skip.Click += (_, __) =>
                {
                    if (!exitedAt.HasValue)
                        return;
                    if (IsProcessRunning(processId) || IsGameProcessRunning(gamePath))
                    {
                        exitedAt = null;
                        skip.Enabled = false;
                        skip.Text = "等待游戏进程退出…";
                        skip.ForeColor = Color.FromArgb(135, 160, 180);
                        return;
                    }
                    completed = true;
                    timer.Stop();
                    form.Close();
                };
                timer.Interval = 250;
                timer.Tick += (_, __) =>
                {
                    bool gameRunning = IsProcessRunning(processId) || IsGameProcessRunning(gamePath);
                    if (gameRunning && exitedAt.HasValue)
                    {
                        exitedAt = null;
                        skip.Enabled = false;
                        skip.Text = "等待游戏进程退出…";
                        skip.ForeColor = Color.FromArgb(135, 160, 180);
                        message.Text = "检测到 Steam 中的游戏进程仍在运行。\r\n请等待进程彻底退出，随后将进行 20 秒状态释放检查。";
                    }
                    if (!gameRunning && !exitedAt.HasValue)
                    {
                        exitedAt = DateTime.UtcNow;
                        skip.Enabled = true;
                        skip.Text = "跳过倒计时并继续";
                        skip.ForeColor = Color.FromArgb(36, 205, 232);
                        message.Text = "Steam 中的游戏进程已结束。\r\n正在等待 Steam 清理游戏状态；也可跳过倒计时继续。";
                    }

                    if (exitedAt.HasValue)
                    {
                        int remaining = Math.Max(0, SteamMonitorSettleSeconds - (int)(DateTime.UtcNow - exitedAt.Value).TotalSeconds);
                        countdown.Text = remaining > 0 ? "Steam 状态释放检查：" + remaining + " 秒" : "正在准备替换文件…";
                        settleProgress.Value = Math.Min(SteamMonitorSettleSeconds, SteamMonitorSettleSeconds - remaining);
                        if (remaining == 0)
                        {
                            completed = true;
                            timer.Stop();
                            form.Close();
                        }
                    }
                    else
                    {
                        int remaining = Math.Max(0, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds));
                        countdown.Text = "等待进程退出：" + remaining + " 秒";
                        settleProgress.Value = 0;
                        if (remaining == 0)
                        {
                            timer.Stop();
                            form.DialogResult = DialogResult.Abort;
                            form.Close();
                        }
                    }
                };
                form.Controls.Add(accentBar);
                form.Controls.Add(header);
                form.Controls.Add(message);
                form.Controls.Add(countdown);
                form.Controls.Add(settleProgress);
                form.Controls.Add(skip);
                form.Shown += (_, __) =>
                {
                    form.TopMost = true;
                    form.Activate();
                    form.BringToFront();
                    timer.Start();
                };
                form.FormClosed += (_, __) => timer.Stop();
                form.ShowDialog();
                if (!completed)
                    throw new TimeoutException("等待 Steam 中游戏进程结束超时，请关闭游戏后重新更新。");
            }
        }

        private static bool IsProcessRunning(int processId)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                    return !process.HasExited;
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        private static bool IsGameProcessRunning(string gamePath)
        {
            string processName = Path.GetFileNameWithoutExtension(gamePath);
            try
            {
                foreach (Process process in Process.GetProcessesByName(processName))
                {
                    using (process)
                    {
                        if (process.Id != Process.GetCurrentProcess().Id && !process.HasExited)
                            return true;
                    }
                }
            }
            catch { }
            return false;
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

        private static void StartGame()
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = SteamGameLaunchUri,
                UseShellExecute = true
            };
            Process.Start(start);
        }

        private static int WaitForGameLaunch(string gamePath)
        {
            string processName = Path.GetFileNameWithoutExtension(gamePath);
            DateTime deadline = DateTime.UtcNow.AddMinutes(3);
            while (DateTime.UtcNow < deadline)
            {
                Process[] games = Process.GetProcessesByName(processName);
                foreach (Process game in games)
                {
                    using (game)
                        if (game.Id != Process.GetCurrentProcess().Id) return game.Id;
                }
                Thread.Sleep(500);
            }
            throw new TimeoutException("等待游戏重新启动超时，更新文件已替换，请手动启动游戏并重新加载模组。");
        }

        private static void InjectUpdatedMod(int processId, string dllPath)
        {
            Injector injector = null;
            Exception lastError = null;
            DateTime deadline = DateTime.UtcNow.AddMinutes(2);
            while (DateTime.UtcNow < deadline && injector == null)
            {
                try
                {
                    injector = new Injector(processId);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(1000);
                }
            }

            if (injector == null)
                throw new TimeoutException("等待游戏 Mono 运行时就绪超时。" + (lastError == null ? string.Empty : " " + lastError.Message));

            using (injector)
            {
                byte[] assembly = File.ReadAllBytes(dllPath);
                IntPtr remoteAssembly = injector.Inject(assembly, "Cheat", "Loader", "Init");
                if (remoteAssembly == IntPtr.Zero)
                    throw new InvalidOperationException("SharpMonoInjector 未返回已加载程序集地址。");
            }
        }

        private static void ShowTimedMessage(string title, string message, int durationMilliseconds, bool isError)
        {
            using (Form form = new Form())
            using (WinFormsTimer timer = new WinFormsTimer())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowInTaskbar = true;
                form.TopMost = true;
                form.ClientSize = new Size(560, 150);
                form.BackColor = Color.FromArgb(9, 17, 31);
                form.ForeColor = Color.FromArgb(225, 238, 250);
                Panel accent = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 4,
                    BackColor = Color.FromArgb(36, 205, 232)
                };
                Label label = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = message,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding = new Padding(24, 14, 24, 14),
                    ForeColor = isError ? Color.FromArgb(255, 125, 135) : Color.FromArgb(205, 220, 238),
                    Font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size + 2f, FontStyle.Regular)
                };
                form.Controls.Add(label);
                form.Controls.Add(accent);
                timer.Interval = durationMilliseconds;
                timer.Tick += (_, __) =>
                {
                    timer.Stop();
                    form.Close();
                };
                form.Shown += (_, __) =>
                {
                    form.TopMost = true;
                    form.Activate();
                    form.BringToFront();
                    timer.Start();
                };
                form.ShowDialog();
            }
        }

        private static string ReadArg(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return string.Empty;
        }

        private static bool HasArg(string[] args, string name)
        {
            for (int i = 0; i < (args == null ? 0 : args.Length); i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
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
                if (string.IsNullOrWhiteSpace(directory))
                    return;

                string safeDirectory = directory.Replace("'", "''");
                int currentPid = Process.GetCurrentProcess().Id;
                string cleanupCommand = "$updateProcessId=" + currentPid + "; while (Get-Process -Id $updateProcessId -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 1 }; Remove-Item -LiteralPath '" + safeDirectory + "' -Recurse -Force -ErrorAction SilentlyContinue";
                string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(cleanupCommand));
                string powershellPath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
                using (Process cleanupProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = File.Exists(powershellPath) ? powershellPath : "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -EncodedCommand " + encodedCommand,
                    WorkingDirectory = Path.GetTempPath(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }))
                {
                    if (cleanupProcess == null)
                        throw new InvalidOperationException("无法启动临时文件清理进程。");
                }
            }
            catch
            {
                try
                {
                    if (Directory.Exists(directory))
                        foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                            MoveFileEx(file, null, MovefileDelayUntilReboot);
                }
                catch { }
            }
        }
    }
}
