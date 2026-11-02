# REPO Cheat Menu By ASwave

**Version: V3.25.15**

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

The build compiles and embeds the updater in the mod DLL. Version checks only query GitHub. When you choose **Update**, the mod creates a temporary updater EXE as the download begins, then the helper requests Windows UAC, replaces the DLL after the game exits, restarts the game, runs the temporary loader, and removes the temporary files when finished. Attach the mod DLL and standalone `smi.exe` loader to each GitHub release for automatic updates. If a release asset includes a GitHub SHA-256 digest, the menu verifies it before starting the updater.

## Source layout

- `Features/` — game features and visual displays
- `Config/` — settings and configuration persistence
- `UI/`, `AuxMenu/`, `Render/` — menu and rendering code
- `Utils/` — shared helpers and version information
- `REPO.Cheat.csproj` — project references and build settings

The repository excludes local dependencies, extracted game assets, temporary files, and build output.

## Version

The menu and assembly version is `3.25.15`. The release notes are in [CHANGELOG.md](CHANGELOG.md).

---

# REPO Cheat Menu By ASwave (简体中文)

**版本：V3.25.15**

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

构建时会编译更新程序并将它嵌入模组 DLL。检查更新只查询 GitHub；选择“更新”开始下载时，才会在临时目录生成更新程序。它会请求系统权限，等待游戏关闭后替换 DLL、重启游戏、调用临时加载器，并在完成后清理临时文件。发布 GitHub Release 时，请同时附上模组 DLL 和独立的 `smi.exe` 加载器。GitHub 发布信息包含 SHA-256 摘要时，菜单会在启动更新前校验文件。

## 源码目录

- `Features/`：游戏功能与画面显示
- `Config/`：设置与配置保存
- `UI/`、`AuxMenu/`、`Render/`：菜单与绘制代码
- `Utils/`：通用工具和版本信息
- `REPO.Cheat.csproj`：项目引用与构建设置

仓库不包含本地依赖、解包游戏资产、临时文件和构建产物。

版本为 `3.25.15`，更新内容见 [CHANGELOG.md](CHANGELOG.md)。
