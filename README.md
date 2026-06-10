<div align="center">
  <a href="#chinese-version">🇨🇳 简体中文</a> | <a href="#english-version">🇬🇧 English</a>
</div>

---

<a id="chinese-version"></a>
# 🇨🇳 REPO New Cheat (中文版)

这是一个为游戏 **REPO** 编写的内部注入辅助（Cheat/Mod）。项目基于 Unity Engine 的 Mono 运行环境，并使用了 Photon (PUN) 进行网络同步相关的处理。它提供了丰富的透视、修改和游戏增强功能。

## ✨ 功能特性

通过分析源代码，本项目包含以下主要功能模块：

| 模块分类 | 功能名称 | 详细说明 |
| :--- | :--- | :--- |
| **玩家增强 (LocalPlayer)** | **基础属性修改** | 无敌模式、无限体力、无限电量 |
| | **运动修改** | 移速、跳跃高度、重力调整 |
| | **状态修改** | 穿墙模式、自由视角、防布娃娃系统 |
| **透视功能 (ESP & Chams)** | **敌人透视** | 方框 (2D/3D)、轮廓高亮、路径可视化 |
| | **战利品透视 (Loot)** | 物品位置方框、轮廓高亮显示 |
| | **玩家透视 (Player)** | 队友位置及状态透视 |
| **生成器 (Spawner)** | **物品生成器** | 本地/全局生成游戏内各类物品 |
| | **怪物生成器** | 动态生成、召唤各类怪物实体 |
| **视觉与UI增强 (Visuals & UI)** | **导航辅助** | 小地图 (支持多渲染模式)、顶部指南针方位指示 |
| | **视觉增强** | 武器激光瞄准、全图高亮、去雾 |
| | **定制化图形界面 (GUI)** | 基于 Unity IMGUI 编写，支持多套动态主题（HolographicScan, ObsidianPulse, SakuraDrift 等），带炫酷过渡动画 |

## 🛠️ 技术栈

*   **开发语言:** C# (.NET Framework 4.8)
*   **游戏引擎:** Unity 3D (Mono 架构)
*   **UI 框架:** 纯 Unity IMGUI 编写，自定义渲染逻辑与动画系统。
*   **网络同步:** Photon Unity Networking (PUN) 挂钩与 RPC 修复 (RPCFixManager)。

## 🚀 使用方法

默认菜单呼出快捷键：`Insert`
默认卸载辅助快捷键：`End`

### 使用 SharpMonoInjector (smi) 注入

#### 1. 命令行方式 (CLI)
打开命令行窗口（CMD/PowerShell），使用以下命令将 DLL 注入到运行中的 REPO 游戏进程：
```bat
smi.exe inject -p REPO -a "REPO New Cheat.dll" -n Cheat -c Loader -m Init
```

#### 2. GUI 版填写方式
如果你使用的是 SharpMonoInjector 的图形界面版本，请按照以下信息填写：
*   **Process (进程):** `REPO`
*   **Assembly to inject (注入的程序集):** 选择编译好的 DLL 文件（如 `REPO New Cheat.dll`）
*   **Namespace (命名空间):** `Cheat`
*   **Class name (类名):** `Loader`
*   **Method name (方法名):** `Init`

![smi注入器填写示例](e:\Projects\REPOCheat\REPO_New_Cheat\REPO_Cheat.png)

## 📦 安装方法

1.  启动游戏 **REPO** 并进入主菜单或战局中。
2.  下载最新版本的 **SharpMonoInjector** (或其他 Mono 注入器)。
3.  获取本项目的发布版 DLL 文件（例如 `REPO New Cheat.dll`）。
4.  参考上述的 **使用方法** 通过命令行或 GUI 注入到游戏进程中。
5.  注入成功后，游戏内会弹出控制台提示，按下 `Insert` 键即可呼出菜单。

## 👨‍💻 面向开发者的构建方法

本项目可以直接在 Visual Studio 或 Rider 中进行编译。

1.  **前置依赖检查:**
    *   确保已安装 **Visual Studio 2022** 或 **JetBrains Rider**。
    *   安装 **.NET Framework 4.8 Developer Pack**。
2.  **打开项目:**
    双击打开 `REPO.Cheat.Wallhack.csproj`。
3.  **准备游戏依赖项 (Libs):**
    出于版权和仓库体积考虑，本项目**不包含**游戏原生的 DLL 文件。在编译前，你需要手动补充这些依赖：
    *   在项目根目录下创建一个名为 `Libs/Managed/` 的文件夹（如果不存在）。
    *   进入你的游戏安装目录 `REPO_Data/Managed/`。
    *   将该目录下的所有相关的依赖库（特别是 `Assembly-CSharp.dll`, `UnityEngine.dll`, `UnityEngine.*.dll`, `PhotonRealtime.dll`, `PhotonUnityNetworking.dll` 等）复制到你刚刚创建的 `Libs/Managed/` 文件夹中。
    *   *(可选)* 如果你需要查看或修改游戏源码逻辑以扩展功能，请自行使用 **dnSpy** 或 **ILSpy** 等工具反编译 `Assembly-CSharp.dll`。
4.  **编译:**
    在 IDE 中选择 `Release` 或 `Debug` 配置，然后点击生成 (Build)。编译后的 DLL 文件将输出到 `bin/Release/net48/` 或 `bin/Debug/net48/` 目录下。

## ⚙️ 依赖环境

*   **运行环境:** .NET Framework 4.8, Unity Mono
*   **游戏客户端:** REPO
*   **第三方工具:** SharpMonoInjector (或其他同类的 Unity Mono 注入器)

<br/>
<br/>

---

<a id="english-version"></a>
# 🇬🇧 REPO New Cheat (English Version)

This is an internal injected cheat/mod written for the game **REPO**. The project is based on the Unity Engine's Mono runtime environment and uses Photon (PUN) for network synchronization handling. It provides a rich set of ESP, modification, and game enhancement features.

## ✨ Features

By analyzing the source code, this project includes the following main feature modules:

| Module | Feature | Description |
| :--- | :--- | :--- |
| **LocalPlayer** | **Basic Stats** | God Mode, Infinite Stamina, Infinite Battery |
| | **Movement** | Speed, Jump Height, Gravity Adjustment |
| | **Status** | NoClip, FreeCam, NoRagdoll |
| **ESP & Chams** | **Enemy ESP** | 2D/3D Box, Chams, Path Visualizer |
| | **Loot ESP** | Item Box, Chams |
| | **Player ESP** | Teammate ESP |
| **Spawner** | **Item Spawner** | Spawn items locally/globally |
| | **Monster Spawner** | Spawn monster entities dynamically |
| **Visuals & UI** | **Navigation** | Minimap (multiple render modes), Compass |
| | **Visual Enhancements** | Laser Sight, Fullbright, No Fog |
| | **Custom GUI** | Pure Unity IMGUI with custom rendering logic, dynamic themes (HolographicScan, ObsidianPulse, SakuraDrift, etc.), and transition animations |

## 🛠️ Tech Stack

*   **Language:** C# (.NET Framework 4.8)
*   **Engine:** Unity 3D (Mono Architecture)
*   **UI Framework:** Pure Unity IMGUI with custom rendering logic and animation system.
*   **Networking:** Photon Unity Networking (PUN) hooking and RPCFixManager.

## 🚀 Usage

Default Menu Toggle Key: `Insert`
Default Unload Key: `End`

### Inject with SharpMonoInjector (smi)

#### 1. Command Line Interface (CLI)
Open a command prompt (CMD/PowerShell) and use the following command to inject the DLL into the running REPO game process:
```bat
smi.exe inject -p REPO -a "REPO New Cheat.dll" -n Cheat -c Loader -m Init
```

#### 2. GUI Configuration
If you are using the GUI version of SharpMonoInjector, please fill in the following information:
*   **Process:** `REPO`
*   **Assembly to inject:** Select the compiled DLL file (e.g., `REPO New Cheat.dll`)
*   **Namespace:** `Cheat`
*   **Class name:** `Loader`
*   **Method name:** `Init`

![smi injector example](e:\Projects\REPOCheat\REPO_New_Cheat\REPO_Cheat.png)

## 📦 Installation

1.  Start the game **REPO** and enter the main menu or a match.
2.  Download the latest version of **SharpMonoInjector** (or another Mono injector).
3.  Obtain the release DLL file for this project (e.g., `REPO New Cheat.dll`).
4.  Refer to the **Usage** section above to inject it into the game process via CLI or GUI.
5.  Upon successful injection, a console prompt will appear in-game; press `Insert` to open the menu.

## 👨‍💻 Build Instructions

This project can be compiled directly in Visual Studio or Rider.

1.  **Prerequisites:**
    *   Ensure **Visual Studio 2022** or **JetBrains Rider** is installed.
    *   Install **.NET Framework 4.8 Developer Pack**.
2.  **Open Project:**
    Double-click `REPO.Cheat.Wallhack.csproj` to open.
3.  **Prepare Game Dependencies (Libs):**
    For copyright and repository size reasons, this project **does not include** the native game DLL files. You need to manually add these dependencies before building:
    *   Create a folder named `Libs/Managed/` in the project root directory (if it doesn't exist).
    *   Navigate to your game installation directory `REPO_Data/Managed/`.
    *   Copy all relevant dependency libraries (especially `Assembly-CSharp.dll`, `UnityEngine.dll`, `UnityEngine.*.dll`, `PhotonRealtime.dll`, `PhotonUnityNetworking.dll`, etc.) from there into your newly created `Libs/Managed/` folder.
    *   *(Optional)* If you need to inspect or modify the game's source code logic to extend features, use tools like **dnSpy** or **ILSpy** to decompile `Assembly-CSharp.dll` yourself.
4.  **Build:**
    Select `Release` or `Debug` configuration in your IDE and click Build. The compiled DLL will be output to the `bin/Release/net48/` or `bin/Debug/net48/` directory.

## ⚙️ Dependencies

*   **Runtime:** .NET Framework 4.8, Unity Mono
*   **Game Client:** REPO
*   **Third-party Tools:** SharpMonoInjector (or similar Unity Mono injectors)
