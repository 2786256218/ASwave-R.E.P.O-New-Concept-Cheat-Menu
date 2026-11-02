# REPO Cheat Menu By ASwave

**Version: V3.27.31**

This is a C# game mod for R.E.P.O. It targets the Unity Mono runtime and .NET Framework 4.8. The source is built against the game’s managed assemblies, which are not included in this repository.

## Build

Use Windows with the .NET 10 SDK and .NET Framework 4.8 Developer Pack installed.

1. In the game installation, open `REPO_Data/Managed/`.
2. Copy its managed DLL files into this project’s `Libs/Managed/` directory. Create the directory if needed. The game assemblies are not redistributed here.
3. From this project directory, run:

   ```powershell
   dotnet build REPO.Cheat.csproj -c Release
   ```

   Or run `build.bat`.

The build output is `bin/Release/net48/REPO New Cheat.dll`.

The build compiles and embeds the updater, including its Mono injection code, in the mod DLL. Version checks only query GitHub. When you choose **Update**, the mod downloads the new DLL and extracts a temporary updater EXE. An elevated bootstrap hands the update to a one-time Windows Task Scheduler worker, so the update countdown and file replacement run independently of the game process. After the game exits, the worker waits for Steam's session state to clear (with an optional skip), replaces the DLL at its current injection path, waits five seconds, then starts Steam App ID `3241660` and injects the updated DLL. Timeouts and completed updates clean the updater's temporary directory. Each GitHub release only needs the mod DLL. If a release asset includes a GitHub SHA-256 digest, the menu verifies it before starting the updater. The injector source and its MIT license notice are in `UpdateHelper/`.

## Source layout

- `Features/` — game features and visual displays
- `Config/` — settings and configuration persistence
- `UI/`, `AuxMenu/`, `Render/` — menu and rendering code
- `Utils/` — shared helpers and version information
- `REPO.Cheat.csproj` — project references and build settings

The repository excludes local dependencies, extracted game assets, temporary files, and build output.

## Version

The menu and assembly version is `3.27.31`. The release notes are in [CHANGELOG.md](CHANGELOG.md).

---

# REPO Cheat Menu By ASwave (简体中文)

**版本：V3.27.31**

这是一个为 R.E.P.O. 编写的 C# 游戏模组，运行于 Unity Mono，目标框架为 .NET Framework 4.8。项目依赖游戏目录中的托管程序集；受游戏版权限制，仓库不包含这些 DLL。

## 构建

请使用 Windows，并安装 .NET 10 SDK 与 .NET Framework 4.8 Developer Pack。

1. 打开游戏安装目录中的 `REPO_Data/Managed/`。
2. 将其中的托管 DLL 复制到项目的 `Libs/Managed/` 目录；目录不存在时请先创建。仓库不分发游戏程序集。
3. 在项目目录运行：

   ```powershell
   dotnet build REPO.Cheat.csproj -c Release
   ```

   也可以运行 `build.bat`。

构建产物位于 `bin/Release/net48/REPO New Cheat.dll`。

构建时会将包含 Mono 注入代码的更新程序编译并嵌入模组 DLL。检查更新只查询 GitHub；选择“更新”后才会下载文件并释放临时更新程序。提权启动的引导程序会把更新交给 Windows 任务计划程序启动的独立进程，因此倒计时和替换流程不依赖游戏进程。游戏退出后，更新程序等待 Steam 清理游戏状态（可选择跳过倒计时），再替换原注入路径上的 DLL；替换完成后等待 5 秒，通过 Steam App ID `3241660` 启动游戏并注入新 DLL。超时或更新完成后会清理更新器的临时目录。GitHub Release 只需发布模组 DLL，无需额外发布加载器。GitHub 发布信息包含 SHA-256 摘要时，菜单会在启动更新前校验文件。注入代码来源及 MIT 许可说明见 `UpdateHelper/`。

## 源码目录

- `Features/`：游戏功能与画面显示
- `Config/`：设置与配置保存
- `UI/`、`AuxMenu/`、`Render/`：菜单与绘制代码
- `Utils/`：通用工具和版本信息
- `REPO.Cheat.csproj`：项目引用与构建设置

仓库不包含本地依赖、解包游戏资产、临时文件和构建产物。

版本为 `3.27.31`，更新内容见 [CHANGELOG.md](CHANGELOG.md)。
