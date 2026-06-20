# Changelog

## 2026-06-20

### 仓库发布整理
- 更新 `.gitignore`，补充忽略 `Debug/`、`Release/`、`.vscode/`、`*.rsuser`、`*.suo`、`*.sln.docstates` 等本地 IDE 配置与构建产物，减少无关文件误提交到 GitHub。
- 继续忽略 `Libs/` 与 `GameData/`，明确游戏原生依赖与大体积目录不进入仓库，构建时由使用者按文档自行补齐。
- 新增对 `.dbg/`、`build*.log`、`debug-*.md` 的忽略规则，避免本地调试脚本、日志和排障笔记混入发布仓库。

## 2026-05-17

### V1.5.0
- 重构配置持久化链路：移除不稳定的空壳 JSON 包装写法，改为按字段逐项落盘的文本 `cfg` 格式，确保菜单内所有可配置开关、滑块、颜色、位置与快捷键都能被真实写入配置文件。
- 升级配置读取兼容逻辑：优先读取新的文本 `cfg`，同时兼容旧版 `json` 与旧版二进制 `cfg`，保证历史配置可迁移、当前配置可稳定恢复。
- 补齐菜单热键持久化：保存前自动采集运行态菜单开关键，加载配置后自动恢复，未保存状态提示同步适配。
- 恢复并移植老项目中的代币箱子生成功能到新 UI，支持 `Common`、`Uncommon`、`Rare`、`UltraRare` 四档代币箱直接生成。
- 放开物品/装备生成入口对“仅主机”文案的限制，联机环境下统一走当前生成链路，保持与新代币箱逻辑一致。
- 菜单叠加版本号更新为 `V1.5.0`，程序集版本同步提升到 `1.5.0`。

### V1.4.1
- 修复 `FOV` 滑块调整后无效果的问题：除直接写入 `Camera.main.fieldOfView` 外，同时同步游戏内部 `CameraZoom.playerZoomDefault` 与其管理的相机列表，避免被原版相机缩放系统在下一帧覆盖。
- 优化怪物透视清理逻辑：`EnemyManager` 现在会在怪物进入死亡流程、死亡标记生效或血量归零时主动移除缓存与路径/线框组件，不再等对象彻底销毁后才消失。
- 为 `EnemyEsp` 与 `EnemyEspLineRenderer` 增加死亡状态兜底判断，避免怪物已死但透视名字、状态、血量 0/Max 等信息仍残留在屏幕上。
- 菜单叠加版本号更新为 `V1.4.1`，程序集版本同步提升到 `1.4.1`。

### 敌人透视稳定性优化
- 移除 `EnemyManager.EnforceVisibility()` 中将 `SkinnedMeshRenderer.localBounds` 强制放大到 `10000` 的逻辑，避免动画怪物的渲染包围盒被污染后导致 ESP 方框严重偏移、拉伸或整框消失。
- 调整 `EspProjectionUtils` 的投影策略，优先按单个 `Renderer` 分别投影后做 2D 合并，不再直接依赖合并后的 3D 总包围盒，降低大体型、多部件怪物的偏框/掉框概率。
- 为包围盒投影补充中心点与六个面中心采样，在镜头边缘和近裁剪面附近提供更稳定的矩形估算，减少“框只剩一截”或信息悬空的问题。

### 非主机怪物辅助击杀
- 重构 `MonsterSpawner.KillEnemy()` / `KillAllEnemies()`：主机或单机继续走原生 `EnemyHealth.Hurt()` 击杀路径，保留原有贵重物掉落修复逻辑。
- 新增非主机网络辅助击杀分支：优先扫描场景内可参与敌人伤害的 `HurtCollider + PhysGrabObject` 网络物体，通过主机侧认可的 `PhysGrabObject.Teleport()` 同步链尝试触发敌人受击。
- 若场景中未找到可用伤害代理，则回退为敌人刚体同步处置方案，至少为非主机单体击杀提供一个可触发的保底路径。
- 在怪物控制页面增加当前击杀模式与最近一次击杀状态提示，方便区分当前是否在走“主机直杀”还是“非主机网络辅助击杀”。

### 构建修复
- 修复 `WorldEsp` 缺少 `System.Reflection` 导致的 `Assembly` 未解析编译错误。
- 修复 `MonsterSpawner` 缺少 `Cheat.Utils` 导致的 `EnemyNameResolver` 未解析编译错误。
- 重新验证 `REPO.Cheat.csproj` 的 `Debug` 构建，当前可正常生成 `REPO New Cheat.dll`。

### 抓取力量修复
- 将菜单内 `抓取力量` 滑块上限从 `50x` 收紧到 `5x`，避免配置层允许输入远超稳定范围的倍率。
- 在 `LocalPlayerManager` 内对抓取力量实际应用值增加钳制，确保旧配置文件即使保存了超限数值，运行时也会被限制在稳定区间。
- 调整抓取增强实现方式，仅保留对 `PhysGrabber.forceMax` 和 `PhysGrabber.forceConstant` 的稳定倍率增强。
- 移除当前版本中额外叠加的 `grabStrength`、`OverrideGrabStrength` 以及对手持物体的重复 override 路径，避免高倍下受力叠加导致左右快速甩动。
- 保留抓取距离增强逻辑，不影响正常远距抓取功能。

### 透视投影重构
- 重写 `MathUtils.WorldToScreen`，加入基于游戏内部 `RenderTextureMain.textureWidth/textureHeight` 的分辨率缩放适配。
- 新投影逻辑优先使用游戏真实渲染分辨率，再映射到屏幕 UI 坐标，修复窗口分辨率与游戏渲染分辨率不一致时的严重偏移。
- 为 `MathUtils` 增加带 `Camera` 参数的重载，统一所有 ESP 与世界坐标转屏幕坐标的入口。
- 更新 `EspProjectionUtils`，包围盒投影与后备投影全部改为统一 GUI 坐标系，避免名字、方框、血量、距离与实际目标脱节。
- 更新 `LootEsp` 的屏幕投影调用，移除旧的手动缩放代码，改为走统一投影方法。
- 更新 `PlayerEsp` 的持物连线投影调用，修复玩家手持物品连线在不同分辨率下的位置误差。

### 透视渲染修复
- 修复 `Render.DrawBox` 与 `Render.DrawLine` 对 `Theme.WhiteTexture` 的硬依赖问题。
- 在 `Render` 内增加主题资源兜底初始化，缺失 `WhiteTexture`、`CircleTexture`、`ShadowTexture` 或 `RoundedBoxStyle` 时自动调用 `Theme.Init()`。
- 为所有基础绘制函数加入纹理 fallback，在线框纹理未准备好时回退到 `Texture2D.whiteTexture`，避免“名字能显示但方框和射线完全不画”的问题。
- 修复 `Render.DrawLine` 的角度计算，使用 `Mathf.Atan2` 替代手写除法计算，避免接近垂直方向时出现角度异常或不可见线段。
- 为 `Render.DrawLine` 增加零长度保护，避免极端情况下绘制无效线段。

### 世界目标透视修复
- 调整 `EspProjectionUtils.GetWorldBounds` 的优先级，改为 `角色包围盒 -> Renderer 包围盒 -> Collider 包围盒`，降低大触发器污染包围盒的概率。
- 在 `EspProjectionUtils.TryGetColliderBounds` 中忽略被禁用或 `isTrigger` 的碰撞体，避免 `ExtractionPoint` 一类的大范围触发区把包围盒拉到天上地下。
- 重构 `WorldEsp` 的运行时目标缓存结构，加入独立的 `BoundsRoot` 与锚点逻辑。
- 为 `TruckDoor` 优先使用 `doorMesh` 作为门透视包围盒根节点。
- 为 `TutorialDoor` 优先使用 `animationTransform` 作为门透视包围盒根节点。
- 为 `ShopKeycardDoor` 优先使用 `hingedPhysGrabObject` 作为门透视包围盒根节点。
- 为 `ExtractionPoint` 优先使用 `extractionArea` 或 `platform` 作为透视定位参考，避免直接拿错误根节点位置绘制。
- 为世界目标增加锚点优先级，距离与后备矩形中心优先跟随实际锚点 `Transform`，避免门体移动后标签位置滞后。
- 收紧世界目标 Renderer 过滤条件，忽略粒子、拖尾、线渲染器以及异常巨大的渲染器 bounds，减少错误包围盒。

### 物资与场景透视扩展
- 为 `LootEsp` 增加物品方框绘制开关，物资透视现在支持同步显示名称、射线与屏幕包围框。
- 为 `LootEsp` 增加基于 `PhysGrabObject` 与 Renderer 集合的物品矩形投影逻辑，降低小型战利品方框漂移问题。
- 为 `EspProjectionUtils` 增加直接接收 `Bounds` 的屏幕投影重载，便于世界目标按自定义包围盒精确绘制。
- 重构 `ExtractionPoint` 的透视包围盒来源，改为综合 `platform`、`buttonGrabObject`、`button`、`extractionTube`、`emojiScreen` 求包围范围，修复提取点位置不准的问题。
- 为世界目标透视新增 `陷阱` 类别，接入 `MuseumLaserLogic`、`DeathPitForce` 以及环境型 `HurtCollider` 的危险区域显示。
- 为陷阱透视增加坑洞、激光、毒池、岩浆池等标签识别与专用绘制样式，统一使用橙红色高亮显示。

### 世界目标逻辑调整
- 移除门透视相关配置、菜单入口与 `WorldEsp` 中的门目标定义，不再尝试显示 `TruckDoor`、`TutorialDoor`、`ShopKeycardDoor`。
- 保留提取点、撤离点与陷阱透视，简化场景目标菜单，避免无效门透视逻辑继续干扰包围盒与筛选流程。

### 怪物功能修复
- 修复辅助击杀怪物后不掉落敌人贵重物的问题：在 `MonsterSpawner.KillEnemy()` 中补齐 `EnemyParent.valuableSpawnTimer` 与 `playerClose` 前置状态，再调用原生受伤死亡流程。
- 调整辅助击杀时传入的受击方向，避免使用零向量造成死亡表现异常。
- 为怪物上色新增 `上色最大距离` 滑块配置，默认 `80m`，菜单上限限制为 `150m` 以避免过高距离导致持续换材质卡顿。
- 在 `EnemyChams` 内按玩家或主相机位置对上色目标做距离过滤，超出阈值的怪物自动恢复原材质，减少远距离全图上色的性能开销。

### 开关动画优化
- 为 `ToggleConfig` 和 `DrawSwitch` 的默认控制 ID 生成逻辑加入 `CallerLineNumber`，自动为每个调用点生成稳定且唯一的动画键。
- 修复多个不同分组使用同名开关文案时共用同一套动画状态的问题，例如 `绘制射线`、`显示距离`、`显示名字` 这类重复文案不再互相串动画。
- 保留已有显式 `controlId` 的调用不变，只修正未显式传入 ID 的开关，减少改动范围并避免影响现有配置绑定。

### 验证
- 对 `Render.cs`、`EspProjectionUtils.cs`、`Program.cs`、`WorldEsp.cs` 做了诊断检查，未发现新增诊断错误。
- 执行 `dotnet build .\REPO.Cheat.csproj -c Debug`，构建结果为 `0` 错误、`0` 警告。
