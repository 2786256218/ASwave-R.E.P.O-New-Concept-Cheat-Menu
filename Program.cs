using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Photon.Pun;
using Cheat.Config;
using Cheat.Features.MonsterSpawner;
using Cheat.Features.LocalPlayer;
using Cheat.Features.Loot;
using Cheat.Features.Enemies;
using Cheat.Features.Minimap;
using Cheat.Features.Compass;
using Cheat.Features.Visuals;
using Cheat.Features.ItemSpawner;
using Cheat.Features.World;
using Cheat.Utils;
using REPO.Cheat.Wallhack.AuxMenu;
using REPO.Cheat.Wallhack.UI;
using UnityEngine;

namespace Cheat
{
    internal static class HotkeyPoller
    {
        private const int KeyDownMask = 0x8000;
        private const int VkInsert = 0x2D;
        private const int VkEnd = 0x23;

        private static bool _toggleWasDown;
        private static bool _endWasDown;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public static bool TogglePressed(KeyCode keyCode)
        {
            KeyCode effectiveKey = keyCode == KeyCode.None ? KeyCode.Insert : keyCode;
            int virtualKey = effectiveKey == KeyCode.Insert ? VkInsert : 0;
            return ConsumePress(ref _toggleWasDown, effectiveKey, virtualKey);
        }

        public static bool UnloadPressed()
        {
            if (WasUnityKeyPressed(KeyCode.End))
            {
                _endWasDown = true;
                return true;
            }

            return ConsumePress(ref _endWasDown, KeyCode.End, VkEnd);
        }

        public static bool FeaturePressed(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None)
            {
                return false;
            }

            try
            {
                return Input.GetKeyDown(keyCode);
            }
            catch
            {
                return false;
            }
        }

        private static bool ConsumePress(ref bool wasDown, KeyCode keyCode, int virtualKey)
        {
            bool isDown = IsUnityKeyDown(keyCode) || (virtualKey != 0 && IsVirtualKeyDown(virtualKey));
            bool pressed = isDown && !wasDown;
            wasDown = isDown;
            return pressed;
        }

        private static bool IsUnityKeyDown(KeyCode keyCode)
        {
            try
            {
                return Input.GetKey(keyCode);
            }
            catch
            {
                return false;
            }
        }

        private static bool WasUnityKeyPressed(KeyCode keyCode)
        {
            try
            {
                return Input.GetKeyDown(keyCode);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsVirtualKeyDown(int virtualKey)
        {
            try
            {
                return (GetAsyncKeyState(virtualKey) & KeyDownMask) != 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public class Loader
    {
        private static GameObject _loadObject;

        private static bool _isUnloading;

        public static void Init()
        {
            try
            {
                if (_loadObject != null) return;

                _loadObject = new GameObject("REPO_New_Cheat_Injector");
                _loadObject.hideFlags = HideFlags.HideAndDontSave;
                _loadObject.AddComponent<InjectorBootstrap>();
                UnityEngine.Object.DontDestroyOnLoad(_loadObject);
                Debug.Log("[REPO New Cheat] Injector bootstrap created.");
            }
            catch (Exception ex)
            {
                Debug.Log($"[REPO New Cheat] Injection error: {ex}");
            }
        }

        public static void Load()
        {
            Init();
        }

        public static void Unload()
        {
            if (_isUnloading)
            {
                return;
            }

            _isUnloading = true;
            try
            {
                if (_loadObject != null)
                {
                    WallhackBehaviour component = _loadObject.GetComponent<WallhackBehaviour>();
                    if (component != null)
                    {
                        component.PrepareForUnload();
                    }

                    UnityEngine.Object.Destroy(_loadObject);
                    _loadObject = null;
                    Debug.Log("[REPO New Cheat] Unloaded.");
                }
            }
            finally
            {
                _isUnloading = false;
            }
        }
    }

    public class InjectorBootstrap : MonoBehaviour
    {
        private bool _initialized;

        private void Start()
        {
            TryInitialize();
        }

        private void Update()
        {
            if (!_initialized)
            {
                TryInitialize();
            }
        }

        private void TryInitialize()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                Type wallhackType = typeof(Loader).Assembly.GetType("Cheat.WallhackBehaviour", throwOnError: false);
                if (wallhackType == null)
                {
                    Debug.LogError("[REPO New Cheat] Unable to locate Cheat.WallhackBehaviour.");
                    return;
                }

                if (gameObject.GetComponent(wallhackType) == null)
                {
                    gameObject.AddComponent(wallhackType);
                }

                _initialized = true;
                Debug.Log("[REPO New Cheat] Main behaviour initialized.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[REPO New Cheat] Bootstrap initialization failed: {ex}");
            }
        }
    }

    public class WallhackBehaviour : MonoBehaviour
    {
        private enum MenuTab
        {
            Player,
            Esp,
            Spawner,
            Monster,
            Misc
        }

        private enum SpawnerCategory
        {
            Item,
            Monster,
            Weapon,
            TokenBox
        }

        private enum StartupAnimationStyle
        {
            HolographicScan,
            ObsidianPulse,
            PlumBlossomBloom,
            SakuraDrift
        }

        private sealed class ThemeTextureSet
        {
            public string DisplayName;
            public string Description;
            public bool IsDark;
            public Color PrimaryText;
            public Color MutedText;
            public Color Accent;
            public Color SecondaryAccent;
            public Color DecorativeTint;
            public Color HeaderOverlay;
            public Color PanelGlow;
            public Color ButtonText;
            public Texture2D Window;
            public Texture2D Header;
            public Texture2D Panel;
            public Texture2D Tab;
            public Texture2D ActiveTab;
            public Texture2D Button;
            public Texture2D SwitchOn;
            public Texture2D SwitchOff;
            public Texture2D ResizeHandle;
        }

        private struct PlumRippleState
        {
            public bool Active;
            public Vector2 Center;
            public Rect Bounds;
            public float StartTime;
        }

        private sealed class InteractiveAnimationState
        {
            public float HoverAmount;
            public float PressAmount;
            public float VisibleAmount;
            public float ToggleAmount;
            public int LastSeenFrame;
            public bool HasToggleValue;
        }

        private struct SakuraParticleState
        {
            public Vector2 Origin;
            public float SpawnTime;
            public float Lifetime;
            public float Speed;
            public float SwayAmplitude;
            public float SwayFrequency;
            public float Phase;
            public float RotationSpeed;
            public float Size;
        }

        private struct SakuraKnotMorphState
        {
            public bool Active;
            public Rect Bounds;
            public float StartTime;
        }

        private struct PlumBackgroundParticleState
        {
            public Vector2 Start;
            public Vector2 ControlA;
            public Vector2 ControlB;
            public Vector2 End;
            public float SpawnTime;
            public float Lifetime;
            public float VerticalSpeed;
            public float RotationSpeed;
            public float BaseRotation;
            public float Size;
            public float Alpha;
            public float Phase;
        }

        private struct SakuraBackgroundParticleState
        {
            public Vector2 Start;
            public float SpawnTime;
            public float Speed;
            public float RotationSpeed;
            public float BaseRotation;
            public float Size;
            public float Alpha;
            public float Phase;
            public float DpiScale;
            public float LandY;
            public float LandTime;
        }

        private struct TechBackgroundColumnState
        {
            public float X;
            public float SpawnTime;
            public float Speed;
            public float Width;
            public float SegmentHeight;
            public float GapHeight;
            public int SegmentCount;
            public float Alpha;
            public float Phase;
            public float DriftAmplitude;
        }

        private struct DarkGlitchBlockState
        {
            public Rect Bounds;
            public float SpawnTime;
            public float Lifetime;
            public float Alpha;
            public float HorizontalDrift;
            public float VerticalDrift;
            public float Jitter;
            public float Phase;
        }

        private bool _showMenu;
        private bool _isOpeningMenu;
        private float _menuVisibleSince;
        private Rect _windowRect;
        private bool _windowInitialized;
        private Vector2 _scrollPosition;
        private MenuTab _selectedTab = MenuTab.Player;
        private SpawnerCategory _selectedSpawnerCategory = SpawnerCategory.Item;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;
        private const float MenuThemeTransitionDuration = 0.30f;
        private const float InteractiveAnimationDuration = 0.24f;
        private const float MenuRevealDuration = 0.60f;
        private const float MenuRevealOpacityStart = 0.80f;
        private const float BackgroundParallaxLimit = 20f;
        private const float ThemeBackgroundUpdateInterval = 1f / 90f;
        private const int MaxThemeBackgroundParticles = 320;
        private const float PlumParticleGravity = 100f;
        private const float PlumParticleSpawnRateMin = 30f;
        private const float PlumParticleSpawnRateMax = 50f;
        private const float SakuraParticleSpawnRateMin = 40f;
        private const float SakuraParticleSpawnRateMax = 60f;
        private const float TechColumnSpawnRateMin = 8f;
        private const float TechColumnSpawnRateMax = 14f;
        private const float DarkGlitchSpawnRateMin = 10f;
        private const float DarkGlitchSpawnRateMax = 18f;
        private const float SakuraParticleFadeDuration = 0.30f;
        private const int MaxTechBackgroundColumns = 56;
        private const int MaxDarkGlitchBlocks = 48;
        private const string CurrentMenuVersion = "V1.5.0";

        private float _menuAspectRatio = 0.72f;
        private bool _isResizingWindow;
        private Rect _resizeStartRect;
        private Vector2 _resizeStartMouse;
        private bool _styleDarkMode;
        private readonly AuxMenuLifecycleController _menuLifecycle = new AuxMenuLifecycleController(new AuxMenuThemeRotator(), MenuThemeTransitionDuration);
        private StartupAnimationStyle _activeMenuThemeStyle = StartupAnimationStyle.HolographicScan;
        private StartupAnimationStyle _appliedMenuThemeStyle;
        private float _menuAnimationStartTime;
        private const float MenuAnimationDuration = 1f;
        private const float StartupCardAspectRatio = 1.92f;
        private StartupAnimationStyle _activeStartupAnimationStyle = StartupAnimationStyle.HolographicScan;
        private bool _menuVisualsTracked;

        private bool _menuInputCaptured;

        private bool _cleanupPrepared;

        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _versionStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _activeTabStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _sliderStyle;
        private GUIStyle _sliderThumbStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _resizeHandleStyle;
        private GUIStyle _startupDarkHeaderStyle;
        private GUIStyle _startupDarkHintStyle;
        private GUIStyle _startupDarkValueStyle;
        private GUIStyle _startupLightHeaderStyle;
        private GUIStyle _startupLightHintStyle;
        private GUIStyle _startupLightValueStyle;
        private Texture2D _windowTexture;
        private Texture2D _headerTexture;
        private Texture2D _panelTexture;
        private Texture2D _whiteTexture;
        private Texture2D _tabTexture;
        private Texture2D _activeTabTexture;
        private Texture2D _buttonTexture;
        private Texture2D _switchOnTexture;
        private Texture2D _switchOffTexture;
        private Texture2D _switchKnobTexture;
        private Texture2D _resizeHandleTexture;
        private Texture2D _startupGlowTexture;
        private Texture2D _startupRingTexture;
        private Texture2D _startupRepoTexture;
        private Texture2D _startupSparkTexture;
        private Texture2D _startupDiamondTexture;
        private Texture2D _startupChevronTexture;
        private Texture2D _startupBracketTexture;
        private Texture2D _startupPetalTexture;
        private Texture2D _startupPetalHighlightTexture;
        private Texture2D _startupSakuraPetalTexture;
        private Texture2D _startupSakuraPetalHighlightTexture;
        private ThemeTextureSet _holographicThemeTextures;
        private ThemeTextureSet _obsidianThemeTextures;
        private ThemeTextureSet _plumThemeTextures;
        private ThemeTextureSet _sakuraThemeTextures;
        private readonly List<SakuraParticleState> _sakuraScrollParticles = new List<SakuraParticleState>(AuxMenuThemePerformanceBudget.MaxSakuraScrollParticles);
        private readonly List<PlumBackgroundParticleState> _plumBackgroundParticles = new List<PlumBackgroundParticleState>(MaxThemeBackgroundParticles);
        private readonly List<SakuraBackgroundParticleState> _sakuraBackgroundParticles = new List<SakuraBackgroundParticleState>(MaxThemeBackgroundParticles);
        private readonly List<TechBackgroundColumnState> _techBackgroundColumns = new List<TechBackgroundColumnState>(MaxTechBackgroundColumns);
        private readonly List<DarkGlitchBlockState> _darkGlitchBlocks = new List<DarkGlitchBlockState>(MaxDarkGlitchBlocks);
        private readonly Dictionary<string, InteractiveAnimationState> _interactiveAnimations = new Dictionary<string, InteractiveAnimationState>(64);
        private PlumRippleState _plumRipple;
        private SakuraKnotMorphState _sakuraKnotMorph;
        private string _activeHotkeyBinderId;
        private KeyCode _menuToggleKey = KeyCode.Insert;
        private Rect _hoveredInteractiveRect;
        private bool _hasHoveredInteractiveRect;
        private float _plumHoverStartTime = float.NegativeInfinity;
        private float _nextPlumParticleSpawnTime;
        private float _nextSakuraParticleSpawnTime;
        private float _nextTechColumnSpawnTime;
        private float _nextDarkGlitchSpawnTime;
        private float _lastThemeBackgroundUpdateTime = float.NegativeInfinity;
        private readonly GuiAnimationClock _guiAnimationClock = new GuiAnimationClock();
        private const int MenuWindowId = 8721;
        private bool _hasUnsavedConfigChanges;
        private string _configProfileName = ConfigManager.DefaultProfileName;
        private string _selectedConfigProfile = ConfigManager.DefaultProfileName;
        private Vector2 _configProfilesScroll;
        private string _configStatusMessage;
        private static readonly string[] MinimapRenderModeLabels = new string[3] { "图标模式", "纯地图", "轮廓高亮" };
        private static readonly string[] EnemyRenderMethodLabels = new string[4] { "角框", "GL 方框", "线框渲染", "实线方框" };
        private static readonly string[] ColorPresetLabels = new string[6] { "红色", "绿色", "青色", "黄色", "粉色", "白色" };
        private static readonly Color[] ColorPresets = new Color[6]
        {
            Color.red,
            Color.green,
            Color.cyan,
            Color.yellow,
            new Color(1f, 0.4f, 0.8f, 1f),
            Color.white
        };

        private void Start()
        {
            try
            {
                Debug.Log("[Wallhack] Initializing WallhackBehaviour...");
                Cheat.Config.ConfigManager.Init();
                InitializeConfigProfileUi();
                ApplyLoadedConfigState();
                
                // Initialize REPO cheat components
                gameObject.AddComponent<Minimap>();
                gameObject.AddComponent<Cheat.Features.Compass.Compass>();
                gameObject.AddComponent<Cheat.Features.RPCFixManager>();
                gameObject.AddComponent<Cheat.Features.MonsterSpawner.MonsterSpawner>();
                gameObject.AddComponent<Cheat.Features.ItemSpawner.ItemSpawner>();
                gameObject.AddComponent<Cheat.Features.Visuals.LightingManager>();

                Debug.Log("[Wallhack] WallhackBehaviour started successfully.");
                Debug.Log("[Wallhack] Hotkeys: Insert = Toggle Menu, End = Unload.");
            }
            catch (Exception ex)
            {
                Debug.Log($"[Wallhack] Error in Start: {ex}");
            }
        }

        private void Update()
        {
            try
            {
                if (HotkeyPoller.TogglePressed(GetEffectiveMenuToggleKey()))
                {
                    ToggleMenu();
                }

                if (HotkeyPoller.UnloadPressed())
                {
                    UnloadSafely();
                    return;
                }

                HandleFeatureHotkeys();

                if (_isOpeningMenu)
                {
                    if (Time.unscaledTime - _menuAnimationStartTime >= MenuAnimationDuration)
                    {
                        _isOpeningMenu = false;
                        _showMenu = true;
                        _menuVisibleSince = Time.unscaledTime;
                    }
                }

                if (_showMenu || _isOpeningMenu)
                {
                    EnsureMenuCursorState();
                    CaptureGameInputForMenu();
                    TryUpdateThemeBackgroundParticles(Time.unscaledTime);
                }
                else if (_menuInputCaptured)
                {
                    RestoreGameInputPriority();
                }

                EnemyManager.Update();
                LootManager.Update();
                LocalPlayerManager.Update();
                FreeCam.Update();
                LaserSight.Update();
            }
            catch (Exception ex)
            {
                Debug.Log($"[Wallhack] Error in Update: {ex}");
            }
        }

        private void LateUpdate()
        {
            Cheat.Config.ConfigManager.IsMenuVisible = _showMenu;
            if (_showMenu || _isOpeningMenu)
            {
                EnsureMenuCursorState();
            }
        }

        private void OnGUI()
        {
            try
            {
                if ((_showMenu || _isOpeningMenu) && _whiteTexture != null)
                {
                    DrawFullscreenThemeBackground();
                }

                if (_isOpeningMenu)
                {
                    EnsureWindowInitialized();
                    EnsureStyles();
                    DrawMenuStartupAnimation();
                }

                if (_showMenu)
                {
                    EnsureWindowInitialized();
                    EnsureStyles();
                    GUI.depth = 0;
                    Color previousColor = GUI.color;
                    float reveal = EvaluateSoftBezier(Mathf.Clamp01((Time.unscaledTime - _menuVisibleSince) / MenuRevealDuration));
                    float alpha = Mathf.Lerp(MenuRevealOpacityStart, 1f, reveal);
                    GUI.color = new Color(1f, 1f, 1f, alpha);
                    _windowRect = GUI.Window(MenuWindowId, _windowRect, DrawMenuWindow, string.Empty, _windowStyle);
                    GUI.color = previousColor;
                    GUI.BringWindowToFront(MenuWindowId);
                    GUI.FocusWindow(MenuWindowId);
                }

                if ((_showMenu || _isOpeningMenu) && _whiteTexture != null)
                {
                    DrawMenuThemeTransitionOverlay();
                }

                if (Event.current.type == EventType.Repaint)
                {
                    DrawRepoOverlays();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[Wallhack] Error in OnGUI: {ex}");
            }
        }

        private void OnDestroy()
        {
            PrepareForUnload();
        }

        public void PrepareForUnload()
        {
            if (_cleanupPrepared)
            {
                return;
            }

            _cleanupPrepared = true;
            RestoreGameInputPriority();
            RestoreCursorState();
            ReleaseMenuVisualResources(true);

            Cheat.Features.Loot.LootChams.Cleanup();
            Cheat.Features.Enemies.EnemyChams.Cleanup();
            Cheat.Features.Loot.LootManager.Cleanup();
            Cheat.Features.Enemies.EnemyManager.Cleanup();
            Cheat.Features.LocalPlayer.LocalPlayerManager.Cleanup();
        }

        private void ToggleMenu()
        {
            bool willOpen = !_showMenu && !_isOpeningMenu;

            _showMenu = false;
            _isOpeningMenu = false;

            if (willOpen)
            {
                _previousCursorLockMode = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                EnsureMenuCursorState();
                ResetInputAxesSafe();
                float now = Time.unscaledTime;
                _activeMenuThemeStyle = AdvanceMenuThemeStyle(now);
                _activeStartupAnimationStyle = _activeMenuThemeStyle;
                _menuAnimationStartTime = now;
                _menuVisibleSince = now + MenuAnimationDuration;
                _nextPlumParticleSpawnTime = now;
                _nextSakuraParticleSpawnTime = now;
                _nextTechColumnSpawnTime = now;
                _nextDarkGlitchSpawnTime = now;
                _lastThemeBackgroundUpdateTime = float.NegativeInfinity;
                _plumBackgroundParticles.Clear();
                _sakuraBackgroundParticles.Clear();
                _techBackgroundColumns.Clear();
                _darkGlitchBlocks.Clear();
                _isOpeningMenu = true;
            }
            else
            {
                ReleaseMenuVisualResources(false);
                RestoreGameInputPriority();
                RestoreCursorState();
            }

            Debug.Log($"[Wallhack] Menu Toggled: {willOpen}");
        }

        private StartupAnimationStyle AdvanceMenuThemeStyle(float now)
        {
            switch (_menuLifecycle.Show(now))
            {
                case AuxMenuThemeKind.Dark:
                    return StartupAnimationStyle.ObsidianPulse;
                case AuxMenuThemeKind.Plum:
                    return StartupAnimationStyle.PlumBlossomBloom;
                case AuxMenuThemeKind.Sakura:
                    return StartupAnimationStyle.SakuraDrift;
                default:
                    return StartupAnimationStyle.HolographicScan;
            }
        }

        private AuxMenuThemeKind GetActiveThemeKind()
        {
            switch (_activeMenuThemeStyle)
            {
                case StartupAnimationStyle.ObsidianPulse:
                    return AuxMenuThemeKind.Dark;
                case StartupAnimationStyle.PlumBlossomBloom:
                    return AuxMenuThemeKind.Plum;
                case StartupAnimationStyle.SakuraDrift:
                    return AuxMenuThemeKind.Sakura;
                default:
                    return AuxMenuThemeKind.Tech;
            }
        }

        private AuxMenuThemeProfile GetActiveThemeProfile()
        {
            return AuxMenuThemeCatalog.GetProfile(GetActiveThemeKind());
        }

        private void RestoreCursorState()
        {
            Cursor.lockState = _previousCursorLockMode;
            Cursor.visible = _previousCursorVisible;
        }

        private static void EnsureMenuCursorState()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private static void ResetInputAxesSafe()
        {
            try
            {
                Input.ResetInputAxes();
            }
            catch
            {
            }
        }

        private void CaptureGameInputForMenu()
        {
            _menuInputCaptured = true;
            ResetInputAxesSafe();
        }

        private void RestoreGameInputPriority()
        {
            if (!_menuInputCaptured)
            {
                return;
            }

            _menuInputCaptured = false;
            ResetInputAxesSafe();
        }

        private void EnsureAllLocalFeaturesDisabled()
        {
            _menuInputCaptured = false;
            ConfigData config = ConfigManager.Config;
            if (config == null)
            {
                return;
            }

            config.Local.GodMode = false;
            config.Local.InfiniteStamina = false;
            config.Local.InfiniteBattery = false;
            config.Local.RunSpeedEnabled = false;
            config.Local.JumpForceEnabled = false;
            config.Local.GravityEnabled = false;
            config.Local.NoClip = false;
            config.Local.NoRagdoll = false;
            config.PlayerEsp.Enabled = false;
            config.Loot.Enabled = false;
            config.Loot.HighlightEnabled = false;
            config.Enemies.EspEnabled = false;
            config.Enemies.HighlightEnabled = false;
            config.Minimap.Enabled = false;
            config.Compass.Enabled = false;
            config.LaserSight.Enabled = false;
            config.Misc.Crosshair = false;
            config.Misc.ShowFps = false;
            config.Misc.ShowKeybinds = false;
            config.Misc.Fullbright = false;
            config.Misc.NoFog = false;
        }

        private void EnsureWindowInitialized()
        {
            if (_windowInitialized) return;

            float width = Mathf.Clamp(Screen.width * 0.46f, 560f, 840f);
            float height = Mathf.Clamp(Screen.height * 0.76f, 560f, 820f);
            _windowRect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            _menuAspectRatio = _windowRect.width / _windowRect.height;
            _windowInitialized = true;
        }

        private void EnsureStyles()
        {
            EnsureThemeTextureSetsInitialized();
            ThemeTextureSet activeTheme = GetThemeTextureSet(_activeMenuThemeStyle);

            if (_whiteTexture != null && _windowStyle != null && _appliedMenuThemeStyle == _activeMenuThemeStyle)
            {
                return;
            }

            _styleDarkMode = activeTheme.IsDark;
            _appliedMenuThemeStyle = _activeMenuThemeStyle;
            if (_whiteTexture == null)
            {
                _whiteTexture = CreateTexture(new Color32(255, 255, 255, 255));
            }
            Color darkText = activeTheme.PrimaryText;
            Color mutedText = activeTheme.MutedText;
            Color accentText = activeTheme.Accent;
            Color buttonText = activeTheme.ButtonText;

            _windowTexture = activeTheme.Window;
            _headerTexture = activeTheme.Header;
            _panelTexture = activeTheme.Panel;
            _tabTexture = activeTheme.Tab;
            _activeTabTexture = activeTheme.ActiveTab;
            _buttonTexture = activeTheme.Button;
            _switchOnTexture = activeTheme.SwitchOn;
            _switchOffTexture = activeTheme.SwitchOff;
            _resizeHandleTexture = activeTheme.ResizeHandle;

            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.padding = new RectOffset(18, 18, 18, 18);
            _windowStyle.border = new RectOffset(18, 18, 18, 18);
            _windowStyle.fontSize = 17;
            SetAllStates(_windowStyle, _windowTexture, darkText);

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.padding = new RectOffset(16, 16, 14, 14);
            _panelStyle.margin = new RectOffset(0, 0, 0, 10);
            _panelStyle.border = new RectOffset(16, 16, 16, 16);
            SetAllStates(_panelStyle, _panelTexture, darkText);

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 24;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = darkText;
            _headerStyle.wordWrap = true;
            _headerStyle.clipping = TextClipping.Overflow;
            _headerStyle.alignment = TextAnchor.UpperLeft;

            _hintStyle = new GUIStyle(GUI.skin.label);
            _hintStyle.fontSize = 13;
            _hintStyle.normal.textColor = mutedText;
            _hintStyle.wordWrap = true;
            _hintStyle.clipping = TextClipping.Overflow;
            _hintStyle.alignment = TextAnchor.UpperLeft;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 14;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = mutedText;
            _titleStyle.clipping = TextClipping.Overflow;
            _titleStyle.alignment = TextAnchor.UpperRight;

            _versionStyle = new GUIStyle(GUI.skin.label);
            _versionStyle.fontSize = 10;
            _versionStyle.fontStyle = FontStyle.Bold;
            _versionStyle.normal.textColor = new Color(mutedText.r, mutedText.g, mutedText.b, 0.82f);
            _versionStyle.clipping = TextClipping.Overflow;
            _versionStyle.alignment = TextAnchor.UpperRight;

            _sectionStyle = new GUIStyle(GUI.skin.label);
            _sectionStyle.fontSize = 17;
            _sectionStyle.fontStyle = FontStyle.Bold;
            _sectionStyle.normal.textColor = accentText;
            _sectionStyle.clipping = TextClipping.Overflow;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 15;
            _labelStyle.normal.textColor = darkText;
            _labelStyle.wordWrap = true;
            _labelStyle.clipping = TextClipping.Overflow;

            _valueStyle = new GUIStyle(GUI.skin.label);
            _valueStyle.fontSize = 12;
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.alignment = TextAnchor.MiddleRight;
            _valueStyle.normal.textColor = mutedText;
            _valueStyle.clipping = TextClipping.Overflow;

            _startupDarkHeaderStyle = new GUIStyle(_headerStyle);
            _startupDarkHeaderStyle.alignment = TextAnchor.MiddleCenter;
            _startupDarkHeaderStyle.normal.textColor = new Color(1f, 0.99f, 1f);

            _startupDarkHintStyle = new GUIStyle(_hintStyle);
            _startupDarkHintStyle.alignment = TextAnchor.MiddleCenter;
            _startupDarkHintStyle.normal.textColor = new Color(0.94f, 0.90f, 0.99f);

            _startupDarkValueStyle = new GUIStyle(_titleStyle);
            _startupDarkValueStyle.alignment = TextAnchor.MiddleCenter;
            _startupDarkValueStyle.normal.textColor = new Color(0.88f, 0.96f, 1f);

            _startupLightHeaderStyle = new GUIStyle(_headerStyle);
            _startupLightHeaderStyle.alignment = TextAnchor.MiddleCenter;
            _startupLightHeaderStyle.normal.textColor = new Color(0.30f, 0.14f, 0.22f);

            _startupLightHintStyle = new GUIStyle(_hintStyle);
            _startupLightHintStyle.alignment = TextAnchor.MiddleCenter;
            _startupLightHintStyle.normal.textColor = new Color(0.40f, 0.22f, 0.30f);

            _startupLightValueStyle = new GUIStyle(_titleStyle);
            _startupLightValueStyle.alignment = TextAnchor.MiddleCenter;
            _startupLightValueStyle.normal.textColor = new Color(0.36f, 0.18f, 0.26f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 15;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.fixedHeight = 40f;
            _buttonStyle.border = new RectOffset(14, 14, 14, 14);
            _buttonStyle.padding = new RectOffset(14, 14, 9, 11);
            _buttonStyle.clipping = TextClipping.Overflow;
            SetAllStates(_buttonStyle, _buttonTexture, buttonText);

            _tabStyle = new GUIStyle(GUI.skin.button);
            _tabStyle.fontSize = 15;
            _tabStyle.fontStyle = FontStyle.Bold;
            _tabStyle.fixedHeight = 42f;
            _tabStyle.border = new RectOffset(16, 16, 16, 16);
            _tabStyle.padding = new RectOffset(14, 14, 10, 12);
            _tabStyle.clipping = TextClipping.Overflow;
            SetAllStates(_tabStyle, _tabTexture, darkText);

            _activeTabStyle = new GUIStyle(_tabStyle);
            SetAllStates(_activeTabStyle, _activeTabTexture, buttonText);

            _sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            _sliderStyle.fixedHeight = 12f;
            _sliderStyle.border = new RectOffset(8, 8, 8, 8);
            SetStyleState(_sliderStyle.normal, _tabTexture, Color.white);
            SetStyleState(_sliderStyle.hover, _tabTexture, Color.white);
            SetStyleState(_sliderStyle.active, _tabTexture, Color.white);

            _sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            _sliderThumbStyle.fixedWidth = 20f;
            _sliderThumbStyle.fixedHeight = 20f;
            _sliderThumbStyle.border = new RectOffset(10, 10, 10, 10);
            SetStyleState(_sliderThumbStyle.normal, _switchKnobTexture, Color.white);
            SetStyleState(_sliderThumbStyle.hover, _switchKnobTexture, Color.white);
            SetStyleState(_sliderThumbStyle.active, _switchKnobTexture, Color.white);

            _textFieldStyle = new GUIStyle(GUI.skin.textField);
            _textFieldStyle.fontSize = 15;
            _textFieldStyle.padding = new RectOffset(8, 8, 8, 8);
            _textFieldStyle.margin = new RectOffset(4, 4, 4, 4);

            _resizeHandleStyle = new GUIStyle(GUI.skin.label);
            _resizeHandleStyle.normal.background = _resizeHandleTexture;
        }

        private static Texture2D CreateTexture(Color32 color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void EnsureThemeTextureSetsInitialized()
        {
            if (_holographicThemeTextures == null)
            {
                _holographicThemeTextures = CreateThemeTextureSet(StartupAnimationStyle.HolographicScan);
            }

            if (_obsidianThemeTextures == null)
            {
                _obsidianThemeTextures = CreateThemeTextureSet(StartupAnimationStyle.ObsidianPulse);
            }

            if (_plumThemeTextures == null)
            {
                _plumThemeTextures = CreateThemeTextureSet(StartupAnimationStyle.PlumBlossomBloom);
            }

            if (_sakuraThemeTextures == null)
            {
                _sakuraThemeTextures = CreateThemeTextureSet(StartupAnimationStyle.SakuraDrift);
            }

            if (_switchKnobTexture == null)
            {
                _switchKnobTexture = CreateCircleTexture(32, new Color32(250, 252, 255, 255), new Color32(204, 212, 222, 255), 1);
            }

            if (_startupGlowTexture == null)
            {
                _startupGlowTexture = CreateSoftCircleTexture(96, new Color32(255, 255, 255, 210), new Color32(255, 255, 255, 0));
            }

            if (_startupRingTexture == null)
            {
                _startupRingTexture = CreateRingTexture(96, 4.5f, new Color32(255, 255, 255, 255));
            }

            if (_startupRepoTexture == null)
            {
                _startupRepoTexture = CreateRepoIconTexture(72, new Color32(255, 255, 255, 255));
            }

            if (_startupSparkTexture == null)
            {
                _startupSparkTexture = CreateSparkTexture(28, new Color32(255, 255, 255, 255));
            }

            if (_startupDiamondTexture == null)
            {
                _startupDiamondTexture = CreateDiamondIconTexture(72, new Color32(255, 255, 255, 255));
            }

            if (_startupChevronTexture == null)
            {
                _startupChevronTexture = CreateChevronTexture(28, new Color32(255, 255, 255, 255));
            }

            if (_startupBracketTexture == null)
            {
                _startupBracketTexture = CreateBracketTexture(32, new Color32(255, 255, 255, 255));
            }

            if (_startupPetalTexture == null)
            {
                _startupPetalTexture = CreatePetalTexture(92, 118, new Color32(255, 228, 225, 252), new Color32(255, 182, 193, 246), 1.6f);
            }

            if (_startupPetalHighlightTexture == null)
            {
                _startupPetalHighlightTexture = CreatePetalTexture(92, 118, new Color32(255, 236, 234, 220), new Color32(255, 196, 205, 184), 1.0f);
            }

            if (_startupSakuraPetalTexture == null)
            {
                _startupSakuraPetalTexture = CreateSakuraPetalTexture(96, 108, new Color32(255, 240, 245, 252), new Color32(255, 192, 203, 248), new Color32(255, 182, 193, 244), 10f);
            }

            if (_startupSakuraPetalHighlightTexture == null)
            {
                _startupSakuraPetalHighlightTexture = CreateSakuraPetalTexture(96, 108, new Color32(255, 248, 250, 224), new Color32(255, 224, 232, 218), new Color32(255, 196, 205, 196), 8f);
            }

            if (!_menuVisualsTracked && _menuLifecycle.ActiveSession != null)
            {
                TrackMenuVisualTextures();
            }
        }

        private ThemeTextureSet GetThemeTextureSet(StartupAnimationStyle style)
        {
            switch (style)
            {
                case StartupAnimationStyle.ObsidianPulse:
                    return _obsidianThemeTextures;
                case StartupAnimationStyle.PlumBlossomBloom:
                    return _plumThemeTextures;
                case StartupAnimationStyle.SakuraDrift:
                    return _sakuraThemeTextures;
                default:
                    return _holographicThemeTextures;
            }
        }

        private static ThemeTextureSet CreateThemeTextureSet(StartupAnimationStyle style)
        {
            ThemeTextureSet theme = new ThemeTextureSet();
            switch (style)
            {
                case StartupAnimationStyle.ObsidianPulse:
                    theme.DisplayName = "暗黑系";
                    theme.Description = "深灰与纯黑为基底，叠加暗红脉冲与金属拉丝光泽。";
                    theme.IsDark = true;
                    theme.PrimaryText = new Color(0.95f, 0.95f, 0.96f);
                    theme.MutedText = new Color(0.78f, 0.78f, 0.80f);
                    theme.Accent = new Color(0.86f, 0.18f, 0.22f);
                    theme.SecondaryAccent = new Color(0.62f, 0.08f, 0.11f);
                    theme.DecorativeTint = new Color(0.66f, 0.10f, 0.12f, 0.22f);
                    theme.HeaderOverlay = new Color(0.08f, 0.08f, 0.09f, 0.36f);
                    theme.PanelGlow = new Color(0.18f, 0.05f, 0.06f, 0.20f);
                    theme.ButtonText = Color.white;
                    theme.Window = CreateRoundedTexture(128, 128, new Color32(10, 10, 12, 236), 22, new Color32(118, 28, 30, 80), 1);
                    theme.Header = CreateRoundedTexture(128, 70, new Color32(18, 18, 20, 244), 10, new Color32(144, 34, 36, 76), 1);
                    theme.Panel = CreateRoundedTexture(128, 96, new Color32(28, 28, 31, 224), 16, new Color32(122, 34, 36, 58), 1);
                    theme.Tab = CreateRoundedTexture(96, 42, new Color32(40, 40, 44, 242), 16, new Color32(108, 30, 34, 50), 1);
                    theme.ActiveTab = CreateRoundedTexture(96, 42, new Color32(132, 28, 32, 250), 16, new Color32(208, 118, 118, 84), 1);
                    theme.Button = CreateRoundedTexture(96, 40, new Color32(98, 24, 28, 246), 14, new Color32(198, 104, 104, 72), 1);
                    theme.SwitchOn = CreateRoundedTexture(72, 36, new Color32(126, 28, 32, 255), 18, new Color32(214, 126, 126, 76), 1);
                    theme.SwitchOff = CreateRoundedTexture(72, 36, new Color32(56, 56, 60, 255), 18, new Color32(142, 142, 146, 48), 1);
                    theme.ResizeHandle = CreateResizeHandleTexture(18, new Color32(204, 170, 170, 220));
                    break;
                case StartupAnimationStyle.PlumBlossomBloom:
                    theme.DisplayName = "梅花风格";
                    theme.Description = "国风留白、水墨山形、竹影与梅枝共同构成对角线构图。";
                    theme.IsDark = false;
                    theme.PrimaryText = new Color(0.22f, 0.14f, 0.14f);
                    theme.MutedText = new Color(0.35f, 0.26f, 0.24f);
                    theme.Accent = new Color(1f, 182f / 255f, 193f / 255f);
                    theme.SecondaryAccent = new Color(1f, 228f / 255f, 225f / 255f);
                    theme.DecorativeTint = new Color(0.86f, 0.89f, 0.83f, 0.22f);
                    theme.HeaderOverlay = new Color(0.98f, 0.95f, 0.92f, 0.40f);
                    theme.PanelGlow = new Color(0.92f, 0.90f, 0.86f, 0.20f);
                    theme.ButtonText = new Color(0.18f, 0.11f, 0.11f);
                    theme.Window = CreateRoundedTexture(128, 128, new Color32(244, 239, 230, 236), 22, new Color32(255, 255, 255, 104), 2);
                    theme.Header = CreateRoundedTexture(128, 70, new Color32(248, 243, 236, 244), 10, new Color32(255, 255, 255, 108), 1);
                    theme.Panel = CreateRoundedTexture(128, 96, new Color32(245, 240, 233, 224), 16, new Color32(255, 255, 255, 92), 1);
                    theme.Tab = CreateRoundedTexture(96, 42, new Color32(238, 232, 224, 244), 16, new Color32(255, 255, 255, 104), 1);
                    theme.ActiveTab = CreateRoundedTexture(96, 42, new Color32(212, 141, 152, 250), 16, new Color32(255, 246, 249, 108), 1);
                    theme.Button = CreateRoundedTexture(96, 40, new Color32(229, 194, 184, 246), 14, new Color32(255, 250, 252, 88), 1);
                    theme.SwitchOn = CreateRoundedTexture(72, 36, new Color32(205, 138, 143, 255), 18, new Color32(255, 250, 252, 90), 1);
                    theme.SwitchOff = CreateRoundedTexture(72, 36, new Color32(182, 174, 163, 255), 18, new Color32(255, 250, 252, 80), 1);
                    theme.ResizeHandle = CreateResizeHandleTexture(18, new Color32(122, 102, 88, 220));
                    break;
                case StartupAnimationStyle.SakuraDrift:
                    theme.DisplayName = "樱花风格";
                    theme.Description = "桜色、空色与抹茶绿通过居中对称与卡片层叠建立日系秩序。";
                    theme.IsDark = false;
                    theme.PrimaryText = new Color(0.23f, 0.16f, 0.19f);
                    theme.MutedText = new Color(0.40f, 0.28f, 0.32f);
                    theme.Accent = new Color(1f, 192f / 255f, 203f / 255f);
                    theme.SecondaryAccent = new Color(1f, 240f / 255f, 245f / 255f);
                    theme.DecorativeTint = new Color(0.73f, 0.84f, 0.64f, 0.28f);
                    theme.HeaderOverlay = new Color(0.99f, 0.96f, 0.97f, 0.34f);
                    theme.PanelGlow = new Color(0.94f, 0.97f, 0.95f, 0.22f);
                    theme.ButtonText = new Color(0.18f, 0.10f, 0.13f);
                    theme.Window = CreateRoundedTexture(128, 128, new Color32(246, 240, 241, 236), 22, new Color32(255, 255, 255, 106), 2);
                    theme.Header = CreateRoundedTexture(128, 70, new Color32(250, 245, 246, 244), 10, new Color32(255, 255, 255, 110), 1);
                    theme.Panel = CreateRoundedTexture(128, 96, new Color32(248, 244, 244, 224), 16, new Color32(255, 255, 255, 94), 1);
                    theme.Tab = CreateRoundedTexture(96, 42, new Color32(240, 231, 232, 244), 16, new Color32(255, 255, 255, 106), 1);
                    theme.ActiveTab = CreateRoundedTexture(96, 42, new Color32(238, 181, 194, 250), 16, new Color32(255, 249, 250, 110), 1);
                    theme.Button = CreateRoundedTexture(96, 40, new Color32(234, 201, 210, 246), 14, new Color32(255, 250, 252, 92), 1);
                    theme.SwitchOn = CreateRoundedTexture(72, 36, new Color32(229, 170, 191, 255), 18, new Color32(255, 250, 252, 92), 1);
                    theme.SwitchOff = CreateRoundedTexture(72, 36, new Color32(194, 201, 191, 255), 18, new Color32(255, 250, 252, 82), 1);
                    theme.ResizeHandle = CreateResizeHandleTexture(18, new Color32(134, 118, 124, 220));
                    break;
                default:
                    theme.DisplayName = "科技风";
                    theme.Description = "霓虹蓝 HUD、电路纹理与扫描流光共同构成未来科技感。";
                    theme.IsDark = true;
                    theme.PrimaryText = new Color(0.90f, 0.96f, 1.00f);
                    theme.MutedText = new Color(0.70f, 0.82f, 0.90f);
                    theme.Accent = new Color(0.42f, 0.92f, 0.98f);
                    theme.SecondaryAccent = new Color(0.80f, 0.98f, 1.00f);
                    theme.DecorativeTint = new Color(0.48f, 0.93f, 1.00f, 0.22f);
                    theme.HeaderOverlay = new Color(0.06f, 0.12f, 0.16f, 0.30f);
                    theme.PanelGlow = new Color(0.08f, 0.18f, 0.22f, 0.18f);
                    theme.ButtonText = Color.white;
                    theme.Window = CreateRoundedTexture(128, 128, new Color32(18, 27, 36, 220), 22, new Color32(144, 214, 224, 56), 1);
                    theme.Header = CreateRoundedTexture(128, 70, new Color32(24, 40, 50, 236), 10, new Color32(174, 231, 237, 62), 1);
                    theme.Panel = CreateRoundedTexture(128, 96, new Color32(30, 49, 60, 212), 16, new Color32(170, 225, 233, 48), 1);
                    theme.Tab = CreateRoundedTexture(96, 42, new Color32(44, 70, 83, 238), 16, new Color32(176, 230, 236, 42), 1);
                    theme.ActiveTab = CreateRoundedTexture(96, 42, new Color32(58, 164, 189, 248), 16, new Color32(220, 247, 251, 80), 1);
                    theme.Button = CreateRoundedTexture(96, 40, new Color32(52, 129, 170, 244), 14, new Color32(221, 245, 250, 64), 1);
                    theme.SwitchOn = CreateRoundedTexture(72, 36, new Color32(60, 190, 170, 255), 18, new Color32(225, 247, 241, 64), 1);
                    theme.SwitchOff = CreateRoundedTexture(72, 36, new Color32(74, 98, 112, 255), 18, new Color32(221, 232, 241, 42), 1);
                    theme.ResizeHandle = CreateResizeHandleTexture(18, new Color32(194, 230, 236, 220));
                    break;
            }
            return theme;
        }

        private static Texture2D CreateRoundedTexture(int width, int height, Color32 fillColor, int radius, Color32 borderColor, int borderThickness)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.wrapMode = TextureWrapMode.Clamp;

            float maxX = width - 1f;
            float maxY = height - 1f;
            float innerRadius = Mathf.Max(0f, radius - borderThickness);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool insideOuter = IsInsideRoundedRect(x, y, maxX, maxY, radius);
                    if (!insideOuter)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    bool insideInner = borderThickness <= 0 || IsInsideRoundedRect(x, y, maxX, maxY, innerRadius, borderThickness);
                    texture.SetPixel(x, y, insideInner ? fillColor : borderColor);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateCircleTexture(int size, Color32 fillColor, Color32 borderColor, int borderThickness)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;
            float radius = (size - 1f) * 0.5f;
            float innerRadius = Mathf.Max(0f, radius - borderThickness);
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        texture.SetPixel(x, y, distance <= innerRadius ? fillColor : borderColor);
                    }
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSoftCircleTexture(int size, Color32 centerColor, Color32 edgeColor)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
            float radius = (size - 1f) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), center) / radius);
                    texture.SetPixel(x, y, Color.Lerp(centerColor, edgeColor, t * t));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateRingTexture(int size, float thickness, Color32 color)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
            float radius = (size - 1f) * 0.5f - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float delta = Mathf.Abs(distance - radius);
                    if (delta > thickness)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float alpha = 1f - Mathf.Clamp01(delta / thickness);
                    Color pixel = color;
                    pixel.a *= alpha;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateRepoIconTexture(int size, Color32 lineColor)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            Vector2 leftBase = new Vector2(size * 0.18f, size * 0.76f);
            Vector2 apex = new Vector2(size * 0.47f, size * 0.24f);
            Vector2 rightBase = new Vector2(size * 0.80f, size * 0.74f);
            Vector2 midBase = new Vector2(size * 0.33f, size * 0.76f);
            Vector2 ridgeApex = new Vector2(size * 0.59f, size * 0.47f);
            Vector2 beaconTop = new Vector2(size * 0.47f, size * 0.10f);

            DrawLine(texture, leftBase, apex, lineColor);
            DrawLine(texture, apex, rightBase, lineColor);
            DrawLine(texture, midBase, ridgeApex, lineColor);
            DrawLine(texture, apex, ridgeApex, lineColor);
            DrawLine(texture, apex, beaconTop, lineColor);
            DrawLine(texture, apex, apex + new Vector2(size * 0.08f, size * 0.10f), lineColor);
            DrawLine(texture, apex, apex + new Vector2(-size * 0.06f, size * 0.11f), lineColor);

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSparkTexture(int size, Color32 lineColor)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
            DrawLine(texture, center, center + Vector2.up * (size * 0.42f), lineColor);
            DrawLine(texture, center, center - Vector2.up * (size * 0.42f), lineColor);
            DrawLine(texture, center, center + Vector2.right * (size * 0.42f), lineColor);
            DrawLine(texture, center, center - Vector2.right * (size * 0.42f), lineColor);
            DrawLine(texture, center, center + new Vector2(1f, 1f).normalized * (size * 0.26f), lineColor);
            DrawLine(texture, center, center + new Vector2(-1f, 1f).normalized * (size * 0.26f), lineColor);
            DrawLine(texture, center, center + new Vector2(1f, -1f).normalized * (size * 0.26f), lineColor);
            DrawLine(texture, center, center + new Vector2(-1f, -1f).normalized * (size * 0.26f), lineColor);

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateDiamondIconTexture(int size, Color32 lineColor)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            Vector2 top = new Vector2(size * 0.5f, size * 0.12f);
            Vector2 right = new Vector2(size * 0.86f, size * 0.50f);
            Vector2 bottom = new Vector2(size * 0.5f, size * 0.88f);
            Vector2 left = new Vector2(size * 0.14f, size * 0.50f);
            Vector2 coreTop = new Vector2(size * 0.5f, size * 0.28f);
            Vector2 coreBottom = new Vector2(size * 0.5f, size * 0.72f);

            DrawLine(texture, top, right, lineColor);
            DrawLine(texture, right, bottom, lineColor);
            DrawLine(texture, bottom, left, lineColor);
            DrawLine(texture, left, top, lineColor);
            DrawLine(texture, coreTop, right, lineColor);
            DrawLine(texture, coreTop, left, lineColor);
            DrawLine(texture, left, coreBottom, lineColor);
            DrawLine(texture, right, coreBottom, lineColor);
            DrawLine(texture, coreTop, coreBottom, lineColor);

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateChevronTexture(int size, Color32 lineColor)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            Vector2 leftTop = new Vector2(size * 0.20f, size * 0.24f);
            Vector2 tip = new Vector2(size * 0.76f, size * 0.50f);
            Vector2 leftBottom = new Vector2(size * 0.20f, size * 0.76f);
            DrawLine(texture, leftTop, tip, lineColor);
            DrawLine(texture, leftBottom, tip, lineColor);

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateBracketTexture(int size, Color32 lineColor)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;
            int margin = Mathf.RoundToInt(size * 0.18f);
            int span = Mathf.RoundToInt(size * 0.28f);
            int thickness = 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            for (int x = margin; x < size - margin; x++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    texture.SetPixel(x, margin + t, lineColor);
                    texture.SetPixel(x, size - margin - 1 - t, lineColor);
                }
            }

            for (int y = margin; y < margin + span; y++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    texture.SetPixel(margin + t, y, lineColor);
                    texture.SetPixel(size - margin - 1 - t, y, lineColor);
                    texture.SetPixel(margin + t, size - 1 - y, lineColor);
                    texture.SetPixel(size - margin - 1 - t, size - 1 - y, lineColor);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreatePetalTexture(int width, int height, Color32 fillColor, Color32 edgeColor, float edgeThickness)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2((width - 1f) * 0.5f, height * 0.56f);
            float radiusX = width * 0.26f;
            float radiusY = height * 0.32f;
            float baseY = height * 0.78f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float dx = (point.x - center.x) / radiusX;
                    float dy = (point.y - center.y) / radiusY;
                    bool insideBody = dx * dx + dy * dy <= 1f;
                    float topT = Mathf.Clamp01(point.y / Mathf.Max(1f, baseY));
                    float taper = Mathf.Lerp(width * 0.03f, width * 0.22f, Mathf.Sqrt(topT));
                    bool insideTip = point.y <= baseY && Mathf.Abs(point.x - center.x) <= taper;
                    bool insideShape = insideBody || insideTip;
                    if (!insideShape)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float bodyDistance = Mathf.Sqrt(Mathf.Min(1f, dx * dx + dy * dy));
                    float tipEdge = insideTip ? Mathf.InverseLerp(0f, taper, Mathf.Abs(point.x - center.x)) : 0f;
                    float edgeFactor = Mathf.Max(bodyDistance, tipEdge);
                    float edgeBlend = Mathf.SmoothStep(1f - edgeThickness * 0.16f, 1f, edgeFactor);
                    float gradient = Mathf.Clamp01(point.y / Mathf.Max(1f, height - 1f));
                    Color pureGradient = Color.Lerp(fillColor, edgeColor, gradient * 0.92f);
                    Color pixel = Color.Lerp(pureGradient, edgeColor, edgeBlend * 0.88f);
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSakuraPetalTexture(int width, int height, Color32 topColor, Color32 middleColor, Color32 edgeColor, float notchRadius)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2((width - 1f) * 0.5f, height * 0.52f);
            Vector2 topBulge = new Vector2(center.x, height * 0.26f);
            Vector2 notchCenter = new Vector2(center.x, height * 0.07f);
            float radiusX = width * 0.31f;
            float radiusY = height * 0.34f;
            float topRadius = width * 0.25f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float dx = (point.x - center.x) / radiusX;
                    float dy = (point.y - center.y) / radiusY;
                    bool insideBody = dx * dx + dy * dy <= 1f;
                    bool insideTop = Vector2.Distance(point, topBulge) <= topRadius;
                    bool insideNotch = Vector2.Distance(point, notchCenter) <= notchRadius;
                    bool insideShape = (insideBody || insideTop) && !insideNotch;
                    if (!insideShape)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float bodyDistance = Mathf.Sqrt(Mathf.Min(1f, dx * dx + dy * dy));
                    float edgeBlend = Mathf.SmoothStep(0.72f, 1f, bodyDistance);
                    float topHighlight = Mathf.Clamp01(1f - Vector2.Distance(point, topBulge) / (topRadius * 1.05f));
                    float verticalT = Mathf.Clamp01(point.y / Mathf.Max(1f, height - 1f));
                    Color topToMiddle = Color.Lerp(topColor, middleColor, Mathf.Clamp01(verticalT * 1.8f));
                    Color layeredGradient = Color.Lerp(topToMiddle, edgeColor, Mathf.Clamp01((verticalT - 0.42f) / 0.58f));
                    Color baseColor = Color.Lerp(layeredGradient, edgeColor, edgeBlend * 0.76f);
                    Color pixel = Color.Lerp(baseColor, new Color(1f, 1f, 1f, topColor.a / 255f), topHighlight * 0.18f);
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateResizeHandleTexture(int size, Color32 lineColor)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                int offset = 4 + i * 4;
                for (int x = offset; x < size; x++)
                {
                    int y = size - 1 - (x - offset);
                    if (y >= 0 && y < size)
                    {
                        texture.SetPixel(x, y, lineColor);
                    }
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSunIconTexture(int size, Color32 lineColor)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float outerRadius = size * 0.22f;
            float innerRadius = outerRadius - 1.2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance <= outerRadius && distance >= innerRadius)
                    {
                        texture.SetPixel(x, y, lineColor);
                    }
                }
            }

            DrawLine(texture, center, center + Vector2.up * (outerRadius + 3f), lineColor);
            DrawLine(texture, center, center - Vector2.up * (outerRadius + 3f), lineColor);
            DrawLine(texture, center, center + Vector2.right * (outerRadius + 3f), lineColor);
            DrawLine(texture, center, center - Vector2.right * (outerRadius + 3f), lineColor);
            DrawLine(texture, center, center + new Vector2(1f, 1f).normalized * (outerRadius + 3f), lineColor);
            DrawLine(texture, center, center + new Vector2(-1f, 1f).normalized * (outerRadius + 3f), lineColor);
            DrawLine(texture, center, center + new Vector2(1f, -1f).normalized * (outerRadius + 3f), lineColor);
            DrawLine(texture, center, center + new Vector2(-1f, -1f).normalized * (outerRadius + 3f), lineColor);

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateMoonIconTexture(int size, Color32 lineColor)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            Vector2 cutCenter = center + new Vector2(size * 0.16f, -size * 0.03f);
            float radius = size * 0.34f;
            float thickness = 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float outer = Vector2.Distance(point, center);
                    float inner = Vector2.Distance(point, cutCenter);
                    bool onCrescent = outer <= radius && outer >= radius - thickness && inner >= radius - thickness;
                    texture.SetPixel(x, y, onCrescent ? lineColor : new Color32(0, 0, 0, 0));
                }
            }

            texture.Apply();
            return texture;
        }

        private static void DrawLine(Texture2D texture, Vector2 center, Vector2 target, Color32 color)
        {
            Vector2 direction = (target - center).normalized;
            float start = 5f;
            float end = Vector2.Distance(center, target);
            for (float t = start; t <= end; t += 0.5f)
            {
                int x = Mathf.RoundToInt(center.x + direction.x * t);
                int y = Mathf.RoundToInt(center.y + direction.y * t);
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawCenteredTexture(Vector2 center, float size, Texture2D texture, Color color)
        {
            DrawCenteredTexture(center, new Vector2(size, size), texture, color);
        }

        private static void DrawCenteredTexture(Vector2 center, Vector2 size, Texture2D texture, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y), texture);
            GUI.color = previousColor;
        }

        private static void DrawRotatedTexture(Vector2 center, Vector2 size, float angle, Texture2D texture, Color color)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(angle, center);
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y), texture);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawStartupTextBackdrop(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = previousColor;
        }

        private Rect GetStartupAnimationRect(float eased, float pulse)
        {
            float width = Mathf.Lerp(260f, _windowRect.width * 0.74f, eased) * pulse;
            float height = width / StartupCardAspectRatio;
            float minHeight = 178f;
            float maxHeight = 286f;

            height = Mathf.Clamp(height, minHeight, maxHeight);
            width = height * StartupCardAspectRatio;

            return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        }

        private static bool IsInsideRoundedRect(float x, float y, float maxX, float maxY, float radius, int inset = 0)
        {
            float left = inset;
            float right = maxX - inset;
            float top = inset;
            float bottom = maxY - inset;

            if (x >= left + radius && x <= right - radius) return y >= top && y <= bottom;
            if (y >= top + radius && y <= bottom - radius) return x >= left && x <= right;

            Vector2 closestCorner;
            if (x < left + radius && y < top + radius) closestCorner = new Vector2(left + radius, top + radius);
            else if (x > right - radius && y < top + radius) closestCorner = new Vector2(right - radius, top + radius);
            else if (x < left + radius && y > bottom - radius) closestCorner = new Vector2(left + radius, bottom - radius);
            else closestCorner = new Vector2(right - radius, bottom - radius);

            return Vector2.Distance(new Vector2(x, y), closestCorner) <= radius;
        }

        private static void SetStyleState(GUIStyleState state, Texture2D background, Color textColor)
        {
            state.background = background;
            state.textColor = textColor;
        }

        private static void SetAllStates(GUIStyle style, Texture2D background, Color textColor)
        {
            SetStyleState(style.normal, background, textColor);
            SetStyleState(style.hover, background, textColor);
            SetStyleState(style.active, background, textColor);
            SetStyleState(style.focused, background, textColor);
            SetStyleState(style.onNormal, background, textColor);
            SetStyleState(style.onHover, background, textColor);
            SetStyleState(style.onActive, background, textColor);
            SetStyleState(style.onFocused, background, textColor);
        }

        private void DrawMenuWindow(int windowId)
        {
            ConfigData config = Cheat.Config.ConfigManager.Config;
            if (config == null)
            {
                GUILayout.Label("配置尚未初始化。", _labelStyle);
                GUI.DragWindow(new Rect(0, 0, 10000, 22));
                return;
            }

            bool configChanged = false;
            Rect fullRect = new Rect(0f, 0f, _windowRect.width, _windowRect.height);
            _hasHoveredInteractiveRect = false;
            ProcessThemeInput(fullRect);
            DrawMenuAnimatedBackground(fullRect);

            DrawHeaderPanel();

            GUILayout.Space(12);

            GUILayout.BeginHorizontal();
            if (DrawTabButton("玩家", MenuTab.Player))
            {
                _selectedTab = MenuTab.Player;
                _scrollPosition = Vector2.zero;
            }

            if (DrawTabButton("透视", MenuTab.Esp))
            {
                _selectedTab = MenuTab.Esp;
                _scrollPosition = Vector2.zero;
            }
            if (DrawTabButton("生成", MenuTab.Spawner))
            {
                _selectedTab = MenuTab.Spawner;
                _scrollPosition = Vector2.zero;
            }
            if (DrawTabButton("怪物", MenuTab.Monster))
            {
                _selectedTab = MenuTab.Monster;
                _scrollPosition = Vector2.zero;
            }
            if (DrawTabButton("杂项", MenuTab.Misc))
            {
                _selectedTab = MenuTab.Misc;
                _scrollPosition = Vector2.zero;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            if (_selectedTab == MenuTab.Player)
            {
                DrawPlayerPage(config, ref configChanged);
            }
            else if (_selectedTab == MenuTab.Esp)
            {
                DrawEspPage(config, ref configChanged);
            }
            else if (_selectedTab == MenuTab.Spawner)
            {
                DrawSpawnerPage();
            }
            else if (_selectedTab == MenuTab.Monster)
            {
                DrawMonsterPage();
            }
            else
            {
                DrawMiscPage(config, ref configChanged);
            }

            GUILayout.EndScrollView();

            _hasUnsavedConfigChanges |= configChanged;

            DrawThemeInteractionOverlays(fullRect);
            DrawResizeHandle();
            GUI.DragWindow(new Rect(0, 0, _windowRect.width - 26f, 92f));
        }

        private bool DrawTabButton(string label, MenuTab tab)
        {
            GUIStyle style = _selectedTab == tab ? _activeTabStyle : _tabStyle;
            Rect baseRect = GUILayoutUtility.GetRect(new GUIContent(label), style, GUILayout.Height(style.fixedHeight), GUILayout.ExpandWidth(true));
            Event currentEvent = Event.current;
            bool hovered = currentEvent != null && baseRect.Contains(currentEvent.mousePosition);
            float alpha;
            Rect animatedRect = GetAnimatedControlRect("tab:" + label, baseRect, hovered, false, _selectedTab == tab, 0.016f, 4f, out alpha);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            bool clicked = GUI.Button(animatedRect, label, style);
            GUI.color = previousColor;
            RegisterInteractiveRect(animatedRect, clicked, false);
            return clicked;
        }

        private bool DrawSpawnerCategoryButton(string label, SpawnerCategory category)
        {
            bool active = _selectedSpawnerCategory == category;
            GUIStyle style = active ? _activeTabStyle : _tabStyle;
            Rect baseRect = GUILayoutUtility.GetRect(new GUIContent(label), style, GUILayout.Height(style.fixedHeight), GUILayout.Width(132f));
            Event currentEvent = Event.current;
            bool hovered = currentEvent != null && baseRect.Contains(currentEvent.mousePosition);
            float alpha;
            Rect animatedRect = GetAnimatedControlRect("spawner-category:" + label, baseRect, hovered, false, active, 0.016f, 4f, out alpha);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            bool clicked = GUI.Button(animatedRect, label, style);
            GUI.color = previousColor;
            RegisterInteractiveRect(animatedRect, clicked, false);
            return clicked;
        }

        private bool DrawActionButton(string label)
        {
            Rect baseRect = GUILayoutUtility.GetRect(new GUIContent(label), _buttonStyle, GUILayout.Height(_buttonStyle.fixedHeight), GUILayout.ExpandWidth(true));
            Event currentEvent = Event.current;
            bool hovered = currentEvent != null && baseRect.Contains(currentEvent.mousePosition);
            float alpha;
            Rect animatedRect = GetAnimatedControlRect("button:" + label, baseRect, hovered, false, false, 0.018f, 5f, out alpha);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            bool clicked = GUI.Button(animatedRect, label, _buttonStyle);
            GUI.color = previousColor;
            RegisterInteractiveRect(animatedRect, clicked, true);
            return clicked;
        }

        private void DrawHeaderPanel()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 82f, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, _headerTexture);
            DrawHeaderThemeOverlay(rect);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 9f, rect.width - 36f, 32f), "REPO Cheat Menu By ASwave", _headerStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 14f, rect.width - 36f, 14f), CurrentMenuVersion, _versionStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 46f, rect.width - 36f, 24f), "Ins 打开或关闭菜单，End 卸载辅助。", _hintStyle);
        }

        private void BeginPanel(string title, string description)
        {
            GUILayout.BeginVertical(_panelStyle);
            GUILayout.Label(title, _sectionStyle);
            if (!string.IsNullOrEmpty(description))
            {
                GUILayout.Label(description, _hintStyle);
                GUILayout.Space(6f);
            }
        }

        private void DrawPlayerPage(ConfigData config, ref bool configChanged)
        {
            BeginPanel("生存控制", "常用角色状态开关，适合快速调整人物生存能力。");
            configChanged |= ToggleConfig(ref config.Local.InfiniteStamina, "无限体力");
            configChanged |= ToggleConfig(ref config.Local.InfiniteBattery, "无限电池");
            configChanged |= DrawLocalFeatureSwitch("上帝模式", ref config.Local.GodMode, ref config.Local.GodModeKey);
            configChanged |= DrawLocalFeatureSwitch("飞行模式", ref config.Local.NoClip, ref config.Local.NoClipKey);
            GUILayout.EndVertical();

            BeginPanel("移动增强", "增强移动与跳跃表现，飞行模式可直接控制垂直位移。");
            if (config.Local.NoClip)
            {
                configChanged |= DrawSliderField("飞行速度", ref config.Local.NoClipSpeed, 1f, 50f, "x", "F1");
            }
            configChanged |= ToggleConfig(ref config.Local.RunSpeedEnabled, "启用奔跑速度倍率");
            if (config.Local.RunSpeedEnabled)
            {
                configChanged |= DrawSliderField("奔跑速度倍率", ref config.Local.RunSpeed, 1f, 10f, "x", "F1");
            }
            configChanged |= ToggleConfig(ref config.Local.JumpForceEnabled, "启用跳跃倍率");
            if (config.Local.JumpForceEnabled)
            {
                configChanged |= DrawSliderField("跳跃倍率", ref config.Local.JumpForce, 1f, 20f, "x", "F1");
            }
            configChanged |= ToggleConfig(ref config.Local.GravityEnabled, "启用重力倍率");
            if (config.Local.GravityEnabled)
            {
                configChanged |= DrawSliderField("重力倍率", ref config.Local.Gravity, 0.1f, 5f, "x", "F1");
            }
            GUILayout.EndVertical();

            BeginPanel("交互增强", "增强抓取物品的能力。");
            configChanged |= DrawSliderField("抓取距离", ref config.Local.GrabRange, 1f, 20f, "m", "F1");
            configChanged |= DrawSliderField("抓取力量", ref config.Local.GrabStrength, 1f, 5f, "x", "F1");
            GUILayout.EndVertical();
        }

        private void DrawEspPage(ConfigData config, ref bool configChanged)
        {
            BeginPanel("玩家透视", "显示其他玩家信息。");
            configChanged |= ToggleConfig(ref config.PlayerEsp.Enabled, "启用玩家透视", "player-esp-enabled");
            if (config.PlayerEsp.Enabled)
            {
                configChanged |= ToggleConfig(ref config.PlayerEsp.DrawName, "显示名字", "player-esp-name");
                configChanged |= ToggleConfig(ref config.PlayerEsp.DrawHealth, "显示血量", "player-esp-health");
                configChanged |= ToggleConfig(ref config.PlayerEsp.DrawDistance, "显示距离", "player-esp-distance");
                configChanged |= ToggleConfig(ref config.PlayerEsp.DrawHeldItem, "显示手持物品", "player-esp-held-item");
                GUILayout.Label("玩家信息固定蓝色，玩家装备固定黄色。", _hintStyle);
            }
            GUILayout.EndVertical();

            BeginPanel("敌对生物", "显示怪物与敌对单位的信息。");
            configChanged |= DrawLocalFeatureSwitch("启用敌人透视", ref config.Enemies.EspEnabled, ref config.Enemies.ToggleKey, "enemy-esp-enabled");
            if (config.Enemies.EspEnabled)
            {
                configChanged |= ToggleConfig(ref config.Enemies.DrawTracers, "绘制射线", "enemy-esp-tracers");
                configChanged |= ToggleConfig(ref config.Enemies.DrawBox, "绘制方框", "enemy-esp-box");
                configChanged |= ToggleConfig(ref config.Enemies.DrawHealth, "显示血量", "enemy-esp-health");
                configChanged |= ToggleConfig(ref config.Enemies.DrawInfo, "显示名字", "enemy-esp-name");
                configChanged |= ToggleConfig(ref config.Enemies.DrawDistance, "显示距离", "enemy-esp-distance");
                configChanged |= ToggleConfig(ref config.Enemies.DrawStatus, "显示状态", "enemy-esp-status");
                configChanged |= ToggleConfig(ref config.Enemies.DrawPath, "绘制路径", "enemy-esp-path");
                configChanged |= ToggleConfig(ref config.Enemies.TargetWarning, "目标警告", "enemy-esp-warning");
                configChanged |= DrawSliderField("最大显示距离", ref config.Enemies.MaxDistance, 10f, 1000f, "m", "F0");
                GUILayout.Label("方框样式: " + GetEnemyRenderMethodLabel(config.Enemies.RenderMethod), _hintStyle);
                if (DrawActionButton("切换方框样式"))
                {
                    config.Enemies.RenderMethod = (config.Enemies.RenderMethod + 1) % EnemyRenderMethodLabels.Length;
                    configChanged = true;
                }
                GUILayout.Label("敌人射线、方框、名字与距离统一为红色。", _hintStyle);
            }
            bool enemyHighlightChanged = ToggleConfig(ref config.Enemies.HighlightEnabled, "敌人上色");
            if (enemyHighlightChanged)
            {
                EnemyChams.Instance.ToggleChams(config.Enemies.HighlightEnabled);
                configChanged = true;
            }
            if (config.Enemies.HighlightEnabled)
            {
                configChanged |= DrawSliderField("上色最大距离", ref config.Enemies.HighlightMaxDistance, 10f, 150f, "m", "F0");
                float enemyHighlightAlpha = config.Enemies.HighlightColor.a;
                if (DrawSliderField("上色透明度", ref enemyHighlightAlpha, 0.05f, 1f, string.Empty, "F2"))
                {
                    Color highlightColor = config.Enemies.HighlightColor;
                    highlightColor.a = enemyHighlightAlpha;
                    config.Enemies.HighlightColor = highlightColor;
                    EnemyChams.Instance.Refresh();
                    configChanged = true;
                }
                GUILayout.Label("上色颜色: " + GetColorPresetLabel(config.Enemies.HighlightColor), _hintStyle);
                if (DrawActionButton("切换上色颜色"))
                {
                    CycleColorPreset(ref config.Enemies.HighlightColor);
                    EnemyChams.Instance.Refresh();
                    configChanged = true;
                }
            }
            GUILayout.EndVertical();

            BeginPanel("物资透视", "显示战利品与物品信息。");
            configChanged |= DrawLocalFeatureSwitch("启用物资透视", ref config.Loot.Enabled, ref config.Loot.ToggleKey);
            if (config.Loot.Enabled)
            {
                configChanged |= ToggleConfig(ref config.Loot.DrawTracers, "绘制射线");
                configChanged |= ToggleConfig(ref config.Loot.DrawBox, "绘制方框");
                configChanged |= ToggleConfig(ref config.Loot.DrawName, "显示名称");
                configChanged |= ToggleConfig(ref config.Loot.UseClustering, "使用聚类");
                configChanged |= ToggleConfig(ref config.Loot.DynamicOpacity, "动态透明度");
                configChanged |= ToggleConfig(ref config.Loot.ShowCartUI, "显示购物车界面");
                configChanged |= DrawSliderField("最大显示距离", ref config.Loot.MaxDistance, 10f, 500f, "m", "F0");
                GUILayout.Label("可回收贵重物品的射线、名称和距离统一为绿色。", _hintStyle);
            }
            configChanged |= ToggleConfig(ref config.Loot.HighlightEnabled, "近距轮廓");
            if (config.Loot.HighlightEnabled)
            {
                configChanged |= DrawSliderField("轮廓距离", ref config.Loot.HighlightDistance, 1f, 20f, "m", "F1");
                GUILayout.Label("可见颜色: " + GetColorPresetLabel(config.Loot.HighlightColorVisible), _hintStyle);
                if (DrawActionButton("切换可见颜色"))
                {
                    CycleColorPreset(ref config.Loot.HighlightColorVisible);
                    configChanged = true;
                }
                GUILayout.Label("遮挡颜色: " + GetColorPresetLabel(config.Loot.HighlightColorOccluded), _hintStyle);
                if (DrawActionButton("切换遮挡颜色"))
                {
                    CycleColorPreset(ref config.Loot.HighlightColorOccluded);
                    configChanged = true;
                }
            }
            GUILayout.EndVertical();

            BeginPanel("场景目标", "显示提取点、撤离点和陷阱。");
            configChanged |= ToggleConfig(ref config.Structures.ExtractionPointsEnabled, "显示提取点");
            configChanged |= ToggleConfig(ref config.Structures.EvacuationPointsEnabled, "显示撤离点");
            configChanged |= ToggleConfig(ref config.Structures.TrapsEnabled, "显示陷阱");
            if (config.Structures.ExtractionPointsEnabled || config.Structures.EvacuationPointsEnabled || config.Structures.TrapsEnabled)
            {
                configChanged |= ToggleConfig(ref config.Structures.DrawTracers, "绘制射线");
                configChanged |= ToggleConfig(ref config.Structures.DrawBox, "绘制方框");
                configChanged |= ToggleConfig(ref config.Structures.DrawName, "显示名字");
                configChanged |= ToggleConfig(ref config.Structures.DrawDistance, "显示距离");
                configChanged |= DrawSliderField("最大显示距离", ref config.Structures.MaxDistance, 10f, 800f, "m", "F0");
                GUILayout.Label("提取点固定白色，撤离点固定紫色，陷阱固定橙红色。", _hintStyle);
            }
            GUILayout.EndVertical();

            BeginPanel("小地图", "屏幕上的小雷达。");
            configChanged |= DrawLocalFeatureSwitch("启用小地图", ref config.Minimap.Enabled, ref config.Minimap.ToggleKey);
            if (config.Minimap.Enabled)
            {
                configChanged |= ToggleConfig(ref config.Minimap.ShowIcons, "显示图标");
                configChanged |= ToggleConfig(ref config.Minimap.AutoCenter, "自动居中");
                configChanged |= ToggleConfig(ref config.Minimap.ShowPath, "显示路径");
                configChanged |= DrawSliderField("地图大小", ref config.Minimap.Size, 100f, 800f, "px", "F0");
                configChanged |= DrawSliderField("地图缩放", ref config.Minimap.Zoom, 0.1f, 5f, "x", "F1");
                GUILayout.Label("显示模式: " + GetMinimapRenderModeLabel(config.Minimap.RenderMode), _hintStyle);
                if (DrawActionButton("切换小地图模式"))
                {
                    config.Minimap.RenderMode = (config.Minimap.RenderMode + 1) % MinimapRenderModeLabels.Length;
                    configChanged = true;
                }
                GUILayout.Label("敌人轮廓颜色: " + GetColorPresetLabel(config.Minimap.RingColor), _hintStyle);
                if (DrawActionButton("切换轮廓颜色"))
                {
                    CycleColorPreset(ref config.Minimap.RingColor);
                    configChanged = true;
                }
                if (DrawActionButton("重置小地图位置"))
                {
                    config.Minimap.Position = new Vector2(-1f, -1f);
                    configChanged = true;
                }
            }
            GUILayout.EndVertical();

            BeginPanel("激光瞄准器", "复刻原版激光瞄准器，并兼容队友透视框。");
            configChanged |= ToggleConfig(ref config.LaserSight.Enabled, "启用激光");
            if (config.LaserSight.Enabled)
            {
                configChanged |= ToggleConfig(ref config.LaserSight.ShowLocal, "显示本地");
                configChanged |= ToggleConfig(ref config.LaserSight.ShowOthers, "显示其他人");
                configChanged |= ToggleConfig(ref config.LaserSight.ShowHitInfo, "显示命中信息");
                configChanged |= DrawSliderField("激光宽度", ref config.LaserSight.Width, 0.01f, 0.1f, string.Empty, "F2");
                GUILayout.Label("激光颜色: " + GetColorPresetLabel(config.LaserSight.Color), _hintStyle);
                if (DrawActionButton("切换激光颜色"))
                {
                    CycleColorPreset(ref config.LaserSight.Color);
                    configChanged = true;
                }
            }
            GUILayout.EndVertical();
            
            BeginPanel("指南针", "屏幕顶部的指南针。");
            configChanged |= DrawLocalFeatureSwitch("启用指南针", ref config.Compass.Enabled, ref config.Compass.ToggleKey);
            if (config.Compass.Enabled)
            {
                configChanged |= DrawSliderField("指南针大小", ref config.Compass.Size, 100f, 800f, "px", "F0");
                configChanged |= DrawSliderField("探测范围", ref config.Compass.Range, 10f, 200f, "m", "F0");
            }
            GUILayout.EndVertical();
        }

        private void DrawSpawnerPage()
        {
            BeginPanel("生成菜单", "左侧切换分类，右侧使用竖向列表选择并生成内容。");
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(144f));
            if (DrawSpawnerCategoryButton("物品生成", SpawnerCategory.Item))
            {
                _selectedSpawnerCategory = SpawnerCategory.Item;
            }
            if (DrawSpawnerCategoryButton("怪物生成", SpawnerCategory.Monster))
            {
                _selectedSpawnerCategory = SpawnerCategory.Monster;
            }
            if (DrawSpawnerCategoryButton("武器生成", SpawnerCategory.Weapon))
            {
                _selectedSpawnerCategory = SpawnerCategory.Weapon;
            }
            if (DrawSpawnerCategoryButton("代币箱", SpawnerCategory.TokenBox))
            {
                _selectedSpawnerCategory = SpawnerCategory.TokenBox;
            }
            GUILayout.EndVertical();

            GUILayout.Space(12f);
            GUILayout.BeginVertical();
            if (_selectedSpawnerCategory == SpawnerCategory.Item)
            {
                DrawItemSpawnerCategory();
            }
            else if (_selectedSpawnerCategory == SpawnerCategory.Monster)
            {
                DrawMonsterSpawnerCategory();
            }
            else if (_selectedSpawnerCategory == SpawnerCategory.TokenBox)
            {
                DrawTokenBoxSpawnerCategory();
            }
            else
            {
                DrawWeaponSpawnerCategory();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawMonsterSpawnerCategory()
        {
            GUILayout.Label("怪物生成", _sectionStyle);
            GUILayout.Label("从竖向列表选择怪物后生成到前方。", _hintStyle);
            if (Cheat.Features.MonsterSpawner.MonsterSpawner.Instance == null)
            {
                GUILayout.Label("怪物生成器未初始化。", _hintStyle);
                return;
            }

            Cheat.Features.MonsterSpawner.MonsterSpawner monsterSpawner = Cheat.Features.MonsterSpawner.MonsterSpawner.Instance;
            List<EnemySetup> enemies = monsterSpawner.SpawnableEnemies;
            GUILayout.Label("当前选择: " + (monsterSpawner.SelectedEnemy != null ? EnemyNameResolver.GetSetupDisplayName(monsterSpawner.SelectedEnemy) : "未选择"), _hintStyle);
            GUILayout.Space(6f);
            foreach (EnemySetup enemy in enemies)
            {
                string name = EnemyNameResolver.GetSetupDisplayName(enemy);
                if (DrawActionButton(monsterSpawner.SelectedEnemy == enemy ? "[已选] " + name : name))
                {
                    monsterSpawner.SelectEnemy(enemy);
                }
            }

            if (monsterSpawner.SelectedEnemy != null)
            {
                GUILayout.Space(10f);
                if (DrawActionButton("生成选中怪物 (仅主机)"))
                {
                    monsterSpawner.SpawnSelectedEnemy();
                }
            }
        }

        private void DrawItemSpawnerCategory()
        {
            GUILayout.Label("物品生成", _sectionStyle);
            GUILayout.Label("搜索后从竖向列表选择物品。结果过多时请继续缩小关键字。", _hintStyle);
            if (Cheat.Features.ItemSpawner.ItemSpawner.Instance == null)
            {
                GUILayout.Label("物品生成器未初始化。", _hintStyle);
                return;
            }

            Cheat.Features.ItemSpawner.ItemSpawner itemSpawner = Cheat.Features.ItemSpawner.ItemSpawner.Instance;
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", _labelStyle, GUILayout.Width(44f));
            string newQuery = GUILayout.TextField(itemSpawner.GetSearchQuery(), _textFieldStyle, GUILayout.Height(30f));
            if (newQuery != itemSpawner.GetSearchQuery())
            {
                itemSpawner.UpdateSearch(newQuery);
            }
            GUILayout.EndHorizontal();

            List<Cheat.Features.ItemSpawner.ItemSpawner.SpawnableItemDef> items = itemSpawner.GetFilteredItems();
            GUILayout.Space(8f);
            GUILayout.Label("当前选择: " + (itemSpawner.SelectedItem != null ? itemSpawner.SelectedItem.Name : "未选择"), _hintStyle);
            if (itemSpawner.SelectedItem != null && DrawActionButton("生成选中物品"))
            {
                itemSpawner.SpawnSelectedItem();
            }
            GUILayout.Space(6f);
            if (items.Count == 0)
            {
                GUILayout.Label("未找到任何物品。", _hintStyle);
                return;
            }

            int shownCount = 0;
            foreach (Cheat.Features.ItemSpawner.ItemSpawner.SpawnableItemDef item in items)
            {
                if (shownCount >= 80)
                {
                    GUILayout.Label("匹配结果过多，仅显示前 80 项，请继续搜索。", _hintStyle);
                    break;
                }

                if (DrawActionButton(itemSpawner.SelectedItem == item ? "[已选] " + item.Name : item.Name))
                {
                    itemSpawner.SelectItem(item);
                }
                shownCount++;
            }
        }

        private void DrawWeaponSpawnerCategory()
        {
            GUILayout.Label("武器生成", _sectionStyle);
            GUILayout.Label("参考原版菜单补回武器/装备生成，使用竖向列表避免超出菜单。", _hintStyle);
            if (Cheat.Features.ItemSpawner.ItemSpawner.Instance == null)
            {
                GUILayout.Label("物品生成器未初始化。", _hintStyle);
                return;
            }

            Cheat.Features.ItemSpawner.ItemSpawner itemSpawner = Cheat.Features.ItemSpawner.ItemSpawner.Instance;
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", _labelStyle, GUILayout.Width(44f));
            string newQuery = GUILayout.TextField(itemSpawner.GetEquipmentSearchQuery(), _textFieldStyle, GUILayout.Height(30f));
            if (newQuery != itemSpawner.GetEquipmentSearchQuery())
            {
                itemSpawner.UpdateEquipmentSearch(newQuery);
            }
            GUILayout.EndHorizontal();

            List<Cheat.Features.ItemSpawner.ItemSpawner.SpawnableItemDef> items = itemSpawner.GetFilteredEquipmentItems();
            GUILayout.Space(8f);
            GUILayout.Label("当前选择: " + (itemSpawner.SelectedEquipment != null ? itemSpawner.SelectedEquipment.Name : "未选择"), _hintStyle);
            if (itemSpawner.SelectedEquipment != null && DrawActionButton("生成选中武器/装备"))
            {
                itemSpawner.SpawnSelectedEquipment();
            }
            GUILayout.Space(6f);
            if (items.Count == 0)
            {
                GUILayout.Label("未找到任何武器或装备。", _hintStyle);
                return;
            }

            int shownCount = 0;
            foreach (Cheat.Features.ItemSpawner.ItemSpawner.SpawnableItemDef item in items)
            {
                if (shownCount >= 80)
                {
                    GUILayout.Label("匹配结果过多，仅显示前 80 项，请继续搜索。", _hintStyle);
                    break;
                }

                if (DrawActionButton(itemSpawner.SelectedEquipment == item ? "[已选] " + item.Name : item.Name))
                {
                    itemSpawner.SelectEquipment(item);
                }
                shownCount++;
            }
        }

        private void DrawTokenBoxSpawnerCategory()
        {
            GUILayout.Label("代币箱", _sectionStyle);
            GUILayout.Label("移植老项目的代币箱物品生成逻辑，单机与联机都会优先走原生资源生成。", _hintStyle);
            GUILayout.Space(6f);
            if (DrawActionButton("生成普通代币箱 (Common)"))
            {
                SpawnCosmeticBox(SemiFunc.Rarity.Common);
            }
            if (DrawActionButton("生成罕见代币箱 (Uncommon)"))
            {
                SpawnCosmeticBox(SemiFunc.Rarity.Uncommon);
            }
            if (DrawActionButton("生成稀有代币箱 (Rare)"))
            {
                SpawnCosmeticBox(SemiFunc.Rarity.Rare);
            }
            if (DrawActionButton("生成超稀有代币箱 (UltraRare)"))
            {
                SpawnCosmeticBox(SemiFunc.Rarity.UltraRare);
            }
        }

        private void DrawMonsterPage()
        {
            BeginPanel("怪物控制", "补回旧版的怪物控制功能，支持一键清怪和当前地图怪物列表。");
            MonsterSpawner monsterSpawner = MonsterSpawner.Instance;
            if ((UnityEngine.Object)(object)monsterSpawner == (UnityEngine.Object)null)
            {
                GUILayout.Label("怪物控制器未初始化。", _hintStyle);
                GUILayout.EndVertical();
                return;
            }

            if (DrawActionButton("一键杀死全部怪物"))
            {
                monsterSpawner.KillAllEnemies();
            }

            string killModeText = (!GameManager.Multiplayer() || Photon.Pun.PhotonNetwork.IsMasterClient) ? "当前为主机/单机，击杀走原生伤害流程。" : "当前为非主机，单体/清怪会优先尝试网络辅助击杀。";
            GUILayout.Label(killModeText, _hintStyle);
            if (!string.IsNullOrEmpty(monsterSpawner.LastKillStatus))
            {
                GUILayout.Label(monsterSpawner.LastKillStatus, _hintStyle);
            }

            GUILayout.Space(10f);
            GUILayout.Label("当前地图怪物", _sectionStyle);

            List<Enemy> activeEnemies = new List<Enemy>(monsterSpawner.GetActiveEnemiesSnapshot(forceRefresh: true));
            activeEnemies.Sort((left, right) => string.Compare(GetEnemyDisplayName(monsterSpawner, left), GetEnemyDisplayName(monsterSpawner, right), StringComparison.OrdinalIgnoreCase));
            GUILayout.Label(string.Format("实时同步中，当前检测到 {0} 只怪物。", activeEnemies.Count), _hintStyle);
            if (activeEnemies.Count == 0)
            {
                GUILayout.Label("未发现活跃怪物，正在等待场景同步。", _hintStyle);
                GUILayout.EndVertical();
                return;
            }

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                Enemy enemy = activeEnemies[i];
                if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                GUILayout.Label(GetEnemyDisplayName(monsterSpawner, enemy), _labelStyle);
                GUILayout.Label(GetEnemyListSubtext(monsterSpawner, enemy), _hintStyle);
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(136f));
                if (DrawActionButton("杀死该怪"))
                {
                    monsterSpawner.KillEnemy(enemy);
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }

            GUILayout.EndVertical();
        }

        private static string GetEnemyDisplayName(MonsterSpawner monsterSpawner, Enemy enemy)
        {
            if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
            {
                return "未知怪物";
            }

            EnemyParent enemyParent = ((UnityEngine.Object)(object)monsterSpawner != (UnityEngine.Object)null) ? monsterSpawner.GetEnemyParent(enemy) : null;
            return EnemyNameResolver.GetEnemyDisplayName(enemy, enemyParent);
        }

        private static string GetEnemyListSubtext(MonsterSpawner monsterSpawner, Enemy enemy)
        {
            if ((UnityEngine.Object)(object)enemy == (UnityEngine.Object)null)
            {
                return "状态未知";
            }

            EnemyHealth health = ((Component)enemy).GetComponent<EnemyHealth>();
            string hpText = "Invuln";
            if (EnemyHealthResolver.TryGetDisplayHealth(enemy, health, out int currentHealth, out int maxHealth) && maxHealth > 0)
            {
                hpText = string.Format("{0}/{1} HP", currentHealth, maxHealth);
            }

            string stateText = enemy.CurrentState.ToString();
            EnemyParent enemyParent = ((UnityEngine.Object)(object)monsterSpawner != (UnityEngine.Object)null) ? monsterSpawner.GetEnemyParent(enemy) : null;
            string distanceText = string.Empty;
            if ((UnityEngine.Object)(object)PlayerController.instance != (UnityEngine.Object)null)
            {
                float num = Vector3.Distance(((Component)PlayerController.instance).transform.position, ((Component)enemy).transform.position);
                distanceText = string.Format(" | {0:F0}m", num);
            }

            string syncText = ((UnityEngine.Object)(object)enemyParent != (UnityEngine.Object)null) ? "已同步" : "场景发现";
            return string.Format("{0} | {1}{2} | {3}", hpText, stateText, distanceText, syncText);
        }

        private void DrawMiscPage(ConfigData config, ref bool configChanged)
        {
            BeginPanel("杂项设置", "其他游戏相关设置。");
            configChanged |= ToggleConfig(ref config.Misc.Crosshair, "显示准星");
            configChanged |= ToggleConfig(ref config.Misc.ShowFps, "显示FPS");
            configChanged |= ToggleConfig(ref config.Misc.ShowKeybinds, "显示快捷键");
            configChanged |= ToggleConfig(ref config.Misc.Fullbright, "全亮模式");
            configChanged |= ToggleConfig(ref config.Misc.NoFog, "无雾模式");
            configChanged |= DrawSliderField("视场角(FOV)", ref config.Misc.FOV, 60f, 120f, "°", "F0");
            if (config.Misc.Fullbright)
            {
                configChanged |= DrawSliderField("全亮强度", ref config.Misc.FullbrightIntensity, 0.1f, 2f, "x", "F1");
            }
            GUILayout.EndVertical();

            BeginPanel("菜单热键", "会保存到配置文件，加载配置后自动恢复。");
            GUILayout.Label("当前开关菜单热键: " + GetEffectiveMenuToggleKey(), _hintStyle);
            configChanged |= DrawHotkeyBinderRow("隐藏菜单开关", ref _menuToggleKey, allowEnd: false);
            GUILayout.Label("卸载菜单固定为 End，不参与自定义。", _hintStyle);
            GUILayout.EndVertical();

            BeginPanel("代币功能", "支持直接添加代币。代币箱刷新功能已移除。");
            if (DrawActionButton("添加普通代币 (Common)"))
            {
                AddCosmeticToken(SemiFunc.Rarity.Common);
            }
            if (DrawActionButton("添加罕见代币 (Uncommon)"))
            {
                AddCosmeticToken(SemiFunc.Rarity.Uncommon);
            }
            if (DrawActionButton("添加稀有代币 (Rare)"))
            {
                AddCosmeticToken(SemiFunc.Rarity.Rare);
            }
            if (DrawActionButton("添加超稀有代币 (UltraRare)"))
            {
                AddCosmeticToken(SemiFunc.Rarity.UltraRare);
            }
            GUILayout.EndVertical();

            BeginPanel("配置管理", "支持多个配置存档，可按名称保存并从列表中选择加载。");
            RefreshConfigProfileSelection();
            GUILayout.Label(_hasUnsavedConfigChanges ? "当前有未保存改动，点击保存配置后才会写入文件。" : "当前配置已与已保存文件同步。", _hintStyle);
            GUILayout.Label("当前配置: " + Cheat.Config.ConfigManager.GetCurrentProfile(), _hintStyle);
            GUILayout.Label("配置目录: " + Cheat.Config.ConfigManager.GetConfigDirectory(), _hintStyle);
            if (!string.IsNullOrEmpty(_configStatusMessage))
            {
                GUILayout.Label(_configStatusMessage, _hintStyle);
            }
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("配置名", _hintStyle, GUILayout.Width(52f));
            string profileNameInput = GUILayout.TextField(_configProfileName ?? string.Empty, _textFieldStyle, GUILayout.Height(30f));
            if (profileNameInput != _configProfileName)
            {
                _configProfileName = profileNameInput;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (DrawActionButton("按名称保存"))
            {
                SaveConfigProfile(_configProfileName);
            }
            if (DrawActionButton("覆盖当前配置"))
            {
                SaveConfigProfile(Cheat.Config.ConfigManager.GetCurrentProfile());
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            List<string> savedProfiles = Cheat.Config.ConfigManager.GetProfiles();
            GUILayout.Label("已保存配置: " + savedProfiles.Count, _hintStyle);
            if (savedProfiles.Count == 0)
            {
                GUILayout.Label("暂无已保存配置。", _hintStyle);
            }
            else
            {
                _configProfilesScroll = GUILayout.BeginScrollView(_configProfilesScroll, GUILayout.Height(180f));
                for (int i = 0; i < savedProfiles.Count; i++)
                {
                    string profile = savedProfiles[i];
                    bool isSelected = string.Equals(profile, _selectedConfigProfile, StringComparison.OrdinalIgnoreCase);
                    bool isCurrent = string.Equals(profile, Cheat.Config.ConfigManager.GetCurrentProfile(), StringComparison.OrdinalIgnoreCase);
                    string label = (isSelected ? "[选中] " : string.Empty) + profile + (isCurrent ? " (当前)" : string.Empty);
                    Color previousColor = GUI.color;
                    GUI.color = isSelected ? Color.white : new Color(1f, 1f, 1f, 0.88f);
                    bool clicked = GUILayout.Button(label, _buttonStyle, GUILayout.Height(_buttonStyle.fixedHeight), GUILayout.ExpandWidth(true));
                    GUI.color = previousColor;
                    if (clicked)
                    {
                        _selectedConfigProfile = profile;
                        _configProfileName = profile;
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (DrawActionButton("加载选中配置"))
            {
                LoadConfigProfile(_selectedConfigProfile);
            }
            if (DrawActionButton("删除选中配置"))
            {
                DeleteSelectedConfigProfile();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (DrawActionButton("删除全部已保存配置"))
            {
                Cheat.Config.ConfigManager.DeleteAllSavedConfigs();
                ResetTransientMenuState();
                ApplyLoadedConfigState();
                RefreshConfigProfileSelection(preferCurrentProfile: true);
                _configProfileName = Cheat.Config.ConfigManager.GetCurrentProfile();
                _configStatusMessage = "已清空全部配置存档。";
                _hasUnsavedConfigChanges = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private bool DrawHotkeyBinderRow(string label, ref KeyCode key, bool allowEnd = true)
        {
            string binderId = "hotkey:" + label;
            Rect rowRect = GUILayoutUtility.GetRect(0f, 42f, GUILayout.ExpandWidth(true));
            Rect labelRect = new Rect(rowRect.x, rowRect.y + 7f, rowRect.width - 140f, 28f);
            Rect binderRect = new Rect(rowRect.xMax - 120f, rowRect.y + 9f, 112f, 24f);

            Event currentEvent = Event.current;
            bool hovered = currentEvent != null && rowRect.Contains(currentEvent.mousePosition);
            bool binding = _activeHotkeyBinderId == binderId;
            float alpha;
            Rect animatedRowRect = GetAnimatedControlRect(binderId, rowRect, hovered, false, binding, 0.010f, 2f, out alpha);
            Rect animatedLabelRect = RemapRect(rowRect, animatedRowRect, labelRect);
            Rect animatedBinderRect = RemapRect(rowRect, animatedRowRect, binderRect);

            bool changed = false;
            if (binding && currentEvent != null && currentEvent.isKey && currentEvent.type == EventType.KeyDown)
            {
                if (TryResolveCapturedHotkey(currentEvent.keyCode, allowEnd, out KeyCode resolvedKey))
                {
                    key = resolvedKey;
                    _activeHotkeyBinderId = null;
                    binding = false;
                    changed = true;
                }
                currentEvent.Use();
            }
            else if (binding && currentEvent != null && currentEvent.type == EventType.MouseDown && !animatedBinderRect.Contains(currentEvent.mousePosition))
            {
                _activeHotkeyBinderId = null;
                binding = false;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(animatedLabelRect, label, _labelStyle);
            bool binderClicked = GUI.Button(animatedBinderRect, binding ? "..." : GetHotkeyButtonText(key, "未绑定"), GetHotkeyButtonStyle(binding));
            if (binderClicked)
            {
                _activeHotkeyBinderId = binding ? null : binderId;
                binding = _activeHotkeyBinderId == binderId;
            }
            GUI.color = previousColor;

            RegisterInteractiveRect(animatedRowRect, false, false);
            RegisterInteractiveRect(animatedBinderRect, binderClicked, true);
            return changed;
        }

        private void InitializeConfigProfileUi()
        {
            string currentProfile = Cheat.Config.ConfigManager.GetCurrentProfile();
            if (string.IsNullOrWhiteSpace(currentProfile))
            {
                currentProfile = ConfigManager.DefaultProfileName;
            }

            _configProfileName = currentProfile;
            _selectedConfigProfile = currentProfile;
            _configStatusMessage = null;
        }

        private void ApplyLoadedConfigState()
        {
            ConfigData config = Cheat.Config.ConfigManager.Config;
            if (config == null)
            {
                return;
            }

            _menuToggleKey = config.UI?.MenuToggleKey ?? KeyCode.Insert;
            EnemyChams.Instance.Refresh();
        }

        private void CaptureRuntimeConfigState()
        {
            ConfigData config = Cheat.Config.ConfigManager.Config;
            if (config == null)
            {
                return;
            }

            if (config.UI == null)
            {
                config.UI = new ConfigData.UiSettings();
            }

            config.UI.MenuToggleKey = GetEffectiveMenuToggleKey();
        }

        private void RefreshConfigProfileSelection(bool preferCurrentProfile = false)
        {
            List<string> savedProfiles = Cheat.Config.ConfigManager.GetProfiles();
            string currentProfile = Cheat.Config.ConfigManager.GetCurrentProfile();
            if (string.IsNullOrWhiteSpace(currentProfile))
            {
                currentProfile = ConfigManager.DefaultProfileName;
            }

            if (string.IsNullOrWhiteSpace(_configProfileName))
            {
                _configProfileName = currentProfile;
            }

            if (preferCurrentProfile || string.IsNullOrWhiteSpace(_selectedConfigProfile) || !ContainsProfile(savedProfiles, _selectedConfigProfile))
            {
                _selectedConfigProfile = ContainsProfile(savedProfiles, currentProfile) ? currentProfile : ((savedProfiles.Count > 0) ? savedProfiles[0] : currentProfile);
            }
        }

        private static bool ContainsProfile(List<string> savedProfiles, string profileName)
        {
            if (savedProfiles == null || string.IsNullOrWhiteSpace(profileName))
            {
                return false;
            }

            for (int i = 0; i < savedProfiles.Count; i++)
            {
                if (string.Equals(savedProfiles[i], profileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void SaveConfigProfile(string profileName)
        {
            CaptureRuntimeConfigState();
            Cheat.Config.ConfigManager.SaveConfig(profileName);
            string currentProfile = Cheat.Config.ConfigManager.GetCurrentProfile();
            _configProfileName = currentProfile;
            _selectedConfigProfile = currentProfile;
            _configStatusMessage = "已保存配置: " + currentProfile;
            _hasUnsavedConfigChanges = false;
        }

        private void LoadConfigProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                _configStatusMessage = "请先从列表中选择一个配置。";
                return;
            }

            Cheat.Config.ConfigManager.LoadConfig(profileName);
            ResetTransientMenuState();
            ApplyLoadedConfigState();
            string currentProfile = Cheat.Config.ConfigManager.GetCurrentProfile();
            _configProfileName = currentProfile;
            _selectedConfigProfile = currentProfile;
            _configStatusMessage = "已加载配置: " + currentProfile;
            _hasUnsavedConfigChanges = false;
        }

        private void DeleteSelectedConfigProfile()
        {
            if (string.IsNullOrWhiteSpace(_selectedConfigProfile))
            {
                _configStatusMessage = "请先从列表中选择一个配置。";
                return;
            }

            string deletedProfile = _selectedConfigProfile;
            if (!Cheat.Config.ConfigManager.DeleteConfig(deletedProfile))
            {
                _configStatusMessage = "删除配置失败: " + deletedProfile;
                return;
            }

            ResetTransientMenuState();
            ApplyLoadedConfigState();
            RefreshConfigProfileSelection(preferCurrentProfile: true);
            _configProfileName = Cheat.Config.ConfigManager.GetCurrentProfile();
            _configStatusMessage = "已删除配置: " + deletedProfile;
            _hasUnsavedConfigChanges = !ContainsProfile(Cheat.Config.ConfigManager.GetProfiles(), Cheat.Config.ConfigManager.GetCurrentProfile());
        }

        private void SpawnCosmeticBox(SemiFunc.Rarity rarity)
        {
            ValuableDirector valuableDirector = ValuableDirector.instance;
            if (valuableDirector == null || valuableDirector.cosmeticWorldObjectSetups == null)
            {
                _configStatusMessage = "当前场景还没有同步到代币箱资源。";
                return;
            }

            Cheat.Features.ItemSpawner.ItemSpawner itemSpawner = Cheat.Features.ItemSpawner.ItemSpawner.Instance;
            if (itemSpawner == null)
            {
                _configStatusMessage = "物品生成器未初始化。";
                return;
            }

            foreach (var setup in valuableDirector.cosmeticWorldObjectSetups)
            {
                if (setup == null || setup.rarity != rarity || setup.prefab == null || !setup.prefab.IsValid())
                {
                    continue;
                }

                PrefabRef prefabRef = setup.prefab;
                itemSpawner.SpawnItem(new Cheat.Features.ItemSpawner.ItemSpawner.SpawnableItemDef
                {
                    Name = "代币箱 (" + GetRarityLabel(rarity) + ")",
                    NativeId = prefabRef.ResourcePath,
                    ResourcePath = prefabRef.ResourcePath,
                    PrefabGetter = () => prefabRef.Prefab,
                    SpawnAction = (position, rotation) => NativeSpawnResolver.SpawnNativePrefab(prefabRef, position, rotation)
                });
                _configStatusMessage = "已触发代币箱生成: " + GetRarityLabel(rarity);
                return;
            }

            _configStatusMessage = "没有找到对应稀有度的代币箱: " + GetRarityLabel(rarity);
        }

        private static string GetRarityLabel(SemiFunc.Rarity rarity)
        {
            switch (rarity)
            {
                case SemiFunc.Rarity.Uncommon:
                    return "Uncommon";
                case SemiFunc.Rarity.Rare:
                    return "Rare";
                case SemiFunc.Rarity.UltraRare:
                    return "UltraRare";
                default:
                    return "Common";
            }
        }

        private bool DrawSliderField(string label, ref float value, float minValue, float maxValue, string suffix, string numericFormat)
        {
            float previousValue = value;
            Rect baseRect = GUILayoutUtility.GetRect(0f, 56f, GUILayout.ExpandWidth(true));
            Rect titleRowRect = new Rect(baseRect.x, baseRect.y, baseRect.width, 20f);
            Rect labelRect = new Rect(baseRect.x, baseRect.y, baseRect.width - 82f, 20f);
            Rect valueRect = new Rect(baseRect.xMax - 76f, baseRect.y, 76f, 20f);
            Rect sliderRect = new Rect(baseRect.x, baseRect.y + 26f, baseRect.width, 20f);

            Event currentEvent = Event.current;
            bool hovered = currentEvent != null && baseRect.Contains(currentEvent.mousePosition);
            bool pressed = currentEvent != null && currentEvent.type == EventType.MouseDown && sliderRect.Contains(currentEvent.mousePosition);
            float alpha;
            Rect animatedRect = GetAnimatedControlRect("slider:" + label, baseRect, hovered, pressed, false, 0.010f, 3f, out alpha);
            Rect animatedTitleRow = RemapRect(baseRect, animatedRect, titleRowRect);
            Rect animatedLabelRect = RemapRect(baseRect, animatedRect, labelRect);
            Rect animatedValueRect = RemapRect(baseRect, animatedRect, valueRect);
            Rect animatedSliderRect = RemapRect(baseRect, animatedRect, sliderRect);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(animatedLabelRect, label, _labelStyle);
            GUI.Label(animatedValueRect, string.Format("{0:" + numericFormat + "}{1}", value, suffix), _valueStyle);
            value = GUI.HorizontalSlider(animatedSliderRect, value, minValue, maxValue, _sliderStyle, _sliderThumbStyle);
            GUI.color = previousColor;
            RegisterInteractiveRect(animatedRect, pressed, false);
            return Mathf.Abs(previousValue - value) > 0.0001f;
        }

        private bool DrawLocalFeatureSwitch(string label, ref bool value, ref KeyCode hotkey)
        {
            return DrawLocalFeatureSwitch(label, ref value, ref hotkey, label);
        }

        private bool DrawLocalFeatureSwitch(string label, ref bool value, ref KeyCode hotkey, string controlId)
        {
            bool previousValue = value;
            bool hotkeyChanged;
            value = DrawSwitch(label, value, ref hotkey, controlId, out hotkeyChanged);
            return hotkeyChanged || previousValue != value;
        }

        private bool ToggleConfig(ref bool value, ref KeyCode hotkey, string label, [CallerLineNumber] int callerLineNumber = 0)
        {
            return ToggleConfig(ref value, ref hotkey, label, label + ":" + callerLineNumber.ToString());
        }

        private bool ToggleConfig(ref bool value, ref KeyCode hotkey, string label, string controlId)
        {
            bool hotkeyChanged;
            bool newValue = DrawSwitch(label, value, ref hotkey, controlId, out hotkeyChanged);
            if (newValue == value)
            {
                return hotkeyChanged;
            }

            value = newValue;
            return true;
        }

        private bool ToggleConfig(ref bool value, string label, [CallerLineNumber] int callerLineNumber = 0)
        {
            return ToggleConfig(ref value, label, label + ":" + callerLineNumber.ToString());
        }

        private bool ToggleConfig(ref bool value, string label, string controlId)
        {
            bool newValue = DrawSwitch(label, value, controlId);
            if (newValue == value)
            {
                return false;
            }

            value = newValue;
            return true;
        }

        private bool DrawSwitch(string label, bool value, [CallerLineNumber] int callerLineNumber = 0)
        {
            return DrawSwitch(label, value, label + ":" + callerLineNumber.ToString());
        }

        private bool DrawSwitch(string label, bool value, string controlId)
        {
            KeyCode unusedHotkey = KeyCode.None;
            bool ignored;
            return DrawSwitch(label, value, ref unusedHotkey, controlId, out ignored);
        }

        private bool DrawSwitch(string label, bool value, ref KeyCode hotkey, out bool hotkeyChanged, [CallerLineNumber] int callerLineNumber = 0)
        {
            return DrawSwitch(label, value, ref hotkey, label + ":" + callerLineNumber.ToString(), out hotkeyChanged);
        }

        private bool DrawSwitch(string label, bool value, ref KeyCode hotkey, string controlId, out bool hotkeyChanged)
        {
            return DrawSwitchInternal(controlId, label, value, ref hotkey, value ? "ON" : "OFF", null, out hotkeyChanged);
        }

        private void HandleFeatureHotkeys()
        {
            if (!string.IsNullOrEmpty(_activeHotkeyBinderId))
            {
                return;
            }

            ConfigData config = ConfigManager.Config;
            if (config == null)
            {
                return;
            }

            _hasUnsavedConfigChanges |= ToggleWhenPressed(config.Local.GodModeKey, ref config.Local.GodMode);
            _hasUnsavedConfigChanges |= ToggleWhenPressed(config.Local.NoClipKey, ref config.Local.NoClip);
            _hasUnsavedConfigChanges |= ToggleWhenPressed(config.Loot.ToggleKey, ref config.Loot.Enabled);
            _hasUnsavedConfigChanges |= ToggleWhenPressed(config.Enemies.ToggleKey, ref config.Enemies.EspEnabled);
            _hasUnsavedConfigChanges |= ToggleWhenPressed(config.Minimap.ToggleKey, ref config.Minimap.Enabled);
            if (HotkeyPoller.FeaturePressed(config.Minimap.ToggleRenderModeKey))
            {
                config.Minimap.RenderMode = (config.Minimap.RenderMode + 1) % MinimapRenderModeLabels.Length;
                _hasUnsavedConfigChanges = true;
            }
            _hasUnsavedConfigChanges |= ToggleWhenPressed(config.Compass.ToggleKey, ref config.Compass.Enabled);
        }

        private static string GetMinimapRenderModeLabel(int renderMode)
        {
            if (renderMode < 0 || renderMode >= MinimapRenderModeLabels.Length)
            {
                return MinimapRenderModeLabels[0];
            }
            return MinimapRenderModeLabels[renderMode];
        }

        private static string GetEnemyRenderMethodLabel(int renderMethod)
        {
            if (renderMethod < 0 || renderMethod >= EnemyRenderMethodLabels.Length)
            {
                return EnemyRenderMethodLabels[0];
            }
            return EnemyRenderMethodLabels[renderMethod];
        }

        private static string GetColorPresetLabel(Color color)
        {
            return ColorPresetLabels[GetNearestColorPresetIndex(color)];
        }

        private static void CycleColorPreset(ref Color color)
        {
            float alpha = color.a;
            int nextIndex = (GetNearestColorPresetIndex(color) + 1) % ColorPresets.Length;
            color = ColorPresets[nextIndex];
            color.a = alpha;
        }

        private static int GetNearestColorPresetIndex(Color color)
        {
            int result = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < ColorPresets.Length; i++)
            {
                Color preset = ColorPresets[i];
                float delta = Mathf.Abs(color.r - preset.r) + Mathf.Abs(color.g - preset.g) + Mathf.Abs(color.b - preset.b);
                if (delta < minDistance)
                {
                    minDistance = delta;
                    result = i;
                }
            }
            return result;
        }

        private bool DrawSwitchInternal(string controlId, string label, bool value, ref KeyCode hotkey, string stateText, Texture2D knobIcon, out bool hotkeyChanged)
        {
            string animationKey = "switch:" + controlId;
            string binderId = "switch-hotkey:" + controlId;
            Rect rowRect = GUILayoutUtility.GetRect(0f, 42f, GUILayout.ExpandWidth(true));
            Rect labelRect = new Rect(rowRect.x, rowRect.y + 7f, rowRect.width - 224f, 28f);
            Rect stateRect = new Rect(rowRect.xMax - 208f, rowRect.y + 10f, 46f, 22f);
            Rect trackRect = new Rect(rowRect.xMax - 154f, rowRect.y + 6f, 56f, 28f);
            Rect binderRect = new Rect(rowRect.xMax - 90f, rowRect.y + 6f, 86f, 28f);

            Event currentEvent = Event.current;
            bool hovered = currentEvent != null && rowRect.Contains(currentEvent.mousePosition);
            bool binding = _activeHotkeyBinderId == binderId;
            hotkeyChanged = false;
            bool targetValue = value;
            if (currentEvent != null && currentEvent.type == EventType.MouseDown && rowRect.Contains(currentEvent.mousePosition) && !binderRect.Contains(currentEvent.mousePosition) && !binding)
            {
                targetValue = !value;
            }
            float alpha;
            Rect animatedRowRect = GetAnimatedControlRect(animationKey, rowRect, hovered, false, targetValue || binding, 0.012f, 3f, out alpha);
            Rect animatedLabelRect = RemapRect(rowRect, animatedRowRect, labelRect);
            Rect animatedStateRect = RemapRect(rowRect, animatedRowRect, stateRect);
            Rect animatedTrackRect = RemapRect(rowRect, animatedRowRect, trackRect);
            Rect animatedBinderRect = RemapRect(rowRect, animatedRowRect, binderRect);
            Rect toggleHitRect = new Rect(animatedRowRect.x, animatedRowRect.y, Mathf.Max(0f, animatedBinderRect.x - animatedRowRect.x - 8f), animatedRowRect.height);

            if (binding && currentEvent != null && currentEvent.isKey && currentEvent.type == EventType.KeyDown)
            {
                hotkey = ResolveCapturedHotkey(currentEvent.keyCode);
                _activeHotkeyBinderId = null;
                binding = false;
                hotkeyChanged = true;
                currentEvent.Use();
            }
            else if (binding && currentEvent != null && currentEvent.type == EventType.MouseDown && !animatedBinderRect.Contains(currentEvent.mousePosition))
            {
                _activeHotkeyBinderId = null;
                binding = false;
                currentEvent.Use();
            }

            InteractiveAnimationState animationState = GetInteractiveAnimationState(animationKey);
            if (!animationState.HasToggleValue)
            {
                animationState.ToggleAmount = value ? 1f : 0f;
                animationState.HasToggleValue = true;
            }
            float knobT = ToggleSwitchMath.EvaluateSoftBezier(Mathf.Clamp01(animationState.ToggleAmount));
            ToggleSwitchLayout switchLayout = ToggleSwitchMath.CalculateLayout(animatedTrackRect, knobT, hovered, Time.unscaledTime);
            Rect animatedKnobRect = switchLayout.KnobRect;

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            bool binderClicked = GUI.Button(animatedBinderRect, binding ? "..." : GetHotkeyButtonText(hotkey), GetHotkeyButtonStyle(binding));
            if (binderClicked)
            {
                _activeHotkeyBinderId = binding ? null : binderId;
                binding = _activeHotkeyBinderId == binderId;
            }

            bool clicked = !binding && GUI.Button(toggleHitRect, GUIContent.none, GUIStyle.none);
            GUI.Label(animatedLabelRect, label, _labelStyle);
            if (!string.IsNullOrEmpty(stateText))
            {
                GUI.Label(animatedStateRect, stateText, _valueStyle);
            }
            DrawSwitchThemeGlow(animatedTrackRect, value);
            DrawCenteredTexture(animatedTrackRect.center, new Vector2(animatedTrackRect.width + 8f + knobT * 6f, animatedTrackRect.height + 6f), _startupGlowTexture, new Color(1f, 1f, 1f, 0.04f + knobT * 0.05f));
            GUI.DrawTexture(animatedTrackRect, value ? _switchOnTexture : _switchOffTexture);
            DrawCenteredTexture(animatedKnobRect.center, new Vector2(animatedKnobRect.width + 10f, animatedKnobRect.height + 10f), _startupGlowTexture, new Color(1f, 1f, 1f, 0.12f + knobT * 0.08f));
            GUI.DrawTexture(animatedKnobRect, _switchKnobTexture);
            if (knobIcon != null)
            {
                Rect iconRect = new Rect(animatedKnobRect.x + 3f, animatedKnobRect.y + 3f, animatedKnobRect.width - 6f, animatedKnobRect.height - 6f);
                GUI.DrawTexture(iconRect, knobIcon);
            }
            GUI.color = previousColor;

            RegisterInteractiveRect(animatedRowRect, clicked, false);
            RegisterInteractiveRect(animatedBinderRect, binderClicked, true);
            return clicked ? !value : value;
        }

        private GUIStyle GetHotkeyButtonStyle(bool binding)
        {
            GUIStyle style = new GUIStyle(_buttonStyle);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;
            style.padding = new RectOffset(6, 6, 4, 4);
            style.fixedHeight = 0f;
            style.normal.textColor = binding ? Color.white : _buttonStyle.normal.textColor;
            return style;
        }

        private static string GetHotkeyButtonText(KeyCode key, string emptyText = "")
        {
            return key == KeyCode.None ? emptyText : key.ToString();
        }

        private static KeyCode ResolveCapturedHotkey(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Backspace || keyCode == KeyCode.Escape)
            {
                return KeyCode.None;
            }

            return keyCode;
        }

        private static bool TryResolveCapturedHotkey(KeyCode keyCode, bool allowEnd, out KeyCode resolvedKey)
        {
            resolvedKey = ResolveCapturedHotkey(keyCode);
            if (!allowEnd && resolvedKey == KeyCode.End)
            {
                return false;
            }

            return true;
        }

        private void ResetTransientMenuState()
        {
            RestoreGameInputPriority();
            _activeHotkeyBinderId = null;
        }

        private KeyCode GetEffectiveMenuToggleKey()
        {
            return _menuToggleKey == KeyCode.None ? KeyCode.Insert : _menuToggleKey;
        }

        private static bool ToggleWhenPressed(KeyCode hotkey, ref bool value)
        {
            if (!HotkeyPoller.FeaturePressed(hotkey))
            {
                return false;
            }

            value = !value;
            return true;
        }

        private static void AddCosmeticToken(SemiFunc.Rarity rarity)
        {
            if (MetaManager.instance != null)
            {
                MetaManager.instance.CosmeticTokenAdd(rarity);
            }
        }

        private static void DrawCrosshairOverlay()
        {
            if (ConfigManager.Config == null || !ConfigManager.Config.Misc.Crosshair)
            {
                return;
            }

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            DrawOverlayRect(new Rect(centerX - 9f, centerY - 0.5f, 19f, 1f), Color.black);
            DrawOverlayRect(new Rect(centerX - 0.5f, centerY - 9f, 1f, 19f), Color.black);
            DrawOverlayRect(new Rect(centerX - 7f, centerY - 0.5f, 15f, 1f), Color.white);
            DrawOverlayRect(new Rect(centerX - 0.5f, centerY - 7f, 1f, 15f), Color.white);
        }

        private static void DrawFpsOverlay()
        {
            if (ConfigManager.Config == null || !ConfigManager.Config.Misc.ShowFps)
            {
                return;
            }

            float deltaTime = Time.smoothDeltaTime > 0f ? Time.smoothDeltaTime : Time.unscaledDeltaTime;
            float fps = 1f / Mathf.Max(0.0001f, deltaTime);
            Cheat.UI.Render.DrawStringOutlined(new Rect(16f, 14f, 140f, 24f), "FPS " + Mathf.RoundToInt(fps), Color.white, center: false, 13, bold: true);
        }

        private static void DrawOverlayRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void ProcessThemeInput(Rect rect)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            AuxMenuThemeKind themeKind = GetActiveThemeKind();
            if (themeKind == AuxMenuThemeKind.Sakura && currentEvent.type == EventType.ScrollWheel)
            {
                int particleCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(currentEvent.delta.y) * 12f), 8, 20);
                SpawnSakuraScrollParticles(rect, particleCount);
            }
        }

        private void RegisterInteractiveRect(Rect rect, bool clicked, bool buttonAction)
        {
            Event currentEvent = Event.current;
            if (currentEvent != null && rect.Contains(currentEvent.mousePosition))
            {
                _hoveredInteractiveRect = rect;
                _hasHoveredInteractiveRect = true;
                if (GetActiveThemeKind() == AuxMenuThemeKind.Plum)
                {
                    _plumHoverStartTime = Time.unscaledTime;
                }
            }

            if (!clicked)
            {
                return;
            }

            switch (GetActiveThemeKind())
            {
                case AuxMenuThemeKind.Plum:
                    // Keep the third rotating theme visually quiet on click.
                    break;
                case AuxMenuThemeKind.Sakura:
                    if (buttonAction)
                    {
                        StartSakuraKnotMorph(rect);
                    }
                    break;
            }
        }

        private void DrawThemeInteractionOverlays(Rect rect)
        {
            ThemeTextureSet theme = GetThemeTextureSet(_activeMenuThemeStyle);
            DrawPlumHoverOverlay(theme);
            DrawPlumRippleOverlay(theme);
            DrawSakuraScrollParticles(rect, theme);
            DrawSakuraKnotMorphOverlay(theme);
        }

        private void DrawPlumHoverOverlay(ThemeTextureSet theme)
        {
            if (GetActiveThemeKind() != AuxMenuThemeKind.Plum || !_hasHoveredInteractiveRect)
            {
                return;
            }

            float hoverProgress = Mathf.Clamp01((Time.unscaledTime - _plumHoverStartTime) / 0.96f);
            if (hoverProgress <= 0f)
            {
                return;
            }

            float eased = EvaluateSoftBezier(hoverProgress);
            Color previousColor = GUI.color;
            Color[] inkColors =
            {
                new Color(0.12f, 0.08f, 0.06f, 0.05f * eased),
                new Color(0.23f, 0.18f, 0.10f, 0.05f * eased),
                new Color(0.30f, 0.17f, 0.18f, 0.05f * eased),
                new Color(0.17f, 0.24f, 0.19f, 0.04f * eased),
                new Color(0.08f, 0.10f, 0.22f, 0.03f * eased)
            };

            for (int i = 0; i < inkColors.Length; i++)
            {
                float inset = i * 2f;
                GUI.color = inkColors[i];
                GUI.DrawTexture(new Rect(_hoveredInteractiveRect.x - 6f + inset, _hoveredInteractiveRect.y - 4f + inset * 0.3f, _hoveredInteractiveRect.width + 12f - inset * 2f, _hoveredInteractiveRect.height + 8f - inset * 0.6f), _whiteTexture);
            }

            Vector2 hoverCenter = _hoveredInteractiveRect.center;
            for (int i = 0; i < 3; i++)
            {
                float pulse = 16f + i * 10f + Mathf.Sin(Time.unscaledTime * (2.0f + i * 0.3f)) * 2f;
                DrawCenteredTexture(hoverCenter + new Vector2(-14f + i * 11f, 6f - i * 3f), pulse, _startupGlowTexture, new Color(0.16f + i * 0.04f, 0.12f, 0.10f + i * 0.03f, 0.08f * eased));
            }

            GUI.color = previousColor;
        }

        private void DrawPlumRippleOverlay(ThemeTextureSet theme)
        {
            if (GetActiveThemeKind() != AuxMenuThemeKind.Plum || !_plumRipple.Active)
            {
                return;
            }

            float progress = Mathf.Clamp01((Time.unscaledTime - _plumRipple.StartTime) / 0.92f);
            if (progress >= 1f)
            {
                _plumRipple.Active = false;
                return;
            }

            float eased = EvaluateSoftBezier(progress);
            float radius = Mathf.Lerp(16f, Mathf.Max(_plumRipple.Bounds.width, _plumRipple.Bounds.height) * 0.92f, eased);
            float alpha = (1f - progress) * 0.20f;
            DrawCenteredTexture(_plumRipple.Center, radius * 2f, _startupRingTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, alpha));
            DrawCenteredTexture(_plumRipple.Center, radius * 1.28f, _startupGlowTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, alpha * 0.8f));
        }

        private void DrawSakuraScrollParticles(Rect rect, ThemeTextureSet theme)
        {
            if (GetActiveThemeKind() != AuxMenuThemeKind.Sakura || _sakuraScrollParticles.Count == 0)
            {
                return;
            }

            float time = Time.unscaledTime;
            Texture2D petalTexture = _startupSakuraPetalTexture;
            for (int i = _sakuraScrollParticles.Count - 1; i >= 0; i--)
            {
                SakuraParticleState particle = _sakuraScrollParticles[i];
                float age = time - particle.SpawnTime;
                float progress = age / particle.Lifetime;
                if (progress >= 1f)
                {
                    _sakuraScrollParticles.RemoveAt(i);
                    continue;
                }

                float x = particle.Origin.x + Mathf.Sin(age * particle.SwayFrequency + particle.Phase) * particle.SwayAmplitude;
                float y = particle.Origin.y + age * particle.Speed;
                if (y > rect.height + 32f)
                {
                    _sakuraScrollParticles.RemoveAt(i);
                    continue;
                }

                float alpha = (1f - progress) * 0.52f;
                float rotation = age * particle.RotationSpeed + particle.Phase * Mathf.Rad2Deg;
                DrawRotatedTexture(new Vector2(x, y), new Vector2(particle.Size, particle.Size * 1.02f), rotation, petalTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, alpha));
            }
        }

        private void DrawSakuraKnotMorphOverlay(ThemeTextureSet theme)
        {
            if (GetActiveThemeKind() != AuxMenuThemeKind.Sakura || !_sakuraKnotMorph.Active)
            {
                return;
            }

            float progress = Mathf.Clamp01((Time.unscaledTime - _sakuraKnotMorph.StartTime) / 0.40f);
            if (progress >= 1f)
            {
                _sakuraKnotMorph.Active = false;
                return;
            }

            float eased = EvaluateSoftBezier(progress);
            Rect bounds = _sakuraKnotMorph.Bounds;
            Vector2 center = bounds.center;
            float horizontal = Mathf.Lerp(bounds.width * 0.42f, bounds.width * 0.20f, eased);
            float vertical = Mathf.Lerp(bounds.height * 0.16f, bounds.height * 0.06f, eased);
            Color ropeColor = new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, (1f - progress) * 0.68f);

            DrawRotatedTexture(new Vector2(center.x - horizontal * 0.5f, center.y), new Vector2(horizontal, 2f), -14f, _whiteTexture, ropeColor);
            DrawRotatedTexture(new Vector2(center.x + horizontal * 0.5f, center.y), new Vector2(horizontal, 2f), 14f, _whiteTexture, ropeColor);
            DrawRotatedTexture(center + new Vector2(0f, -vertical * 1.8f), new Vector2(bounds.width * 0.14f, 2f), 90f, _whiteTexture, ropeColor);
            DrawCenteredTexture(center, 18f - eased * 5f, _startupGlowTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, (1f - progress) * 0.16f));
        }

        private void SpawnSakuraScrollParticles(Rect rect, int count)
        {
            AuxMenuThemeProfile profile = GetActiveThemeProfile();
            count = Mathf.Clamp(count, 0, profile.ParticleLimit);
            for (int i = 0; i < count; i++)
            {
                if (_sakuraScrollParticles.Count >= profile.ParticleLimit)
                {
                    _sakuraScrollParticles.RemoveAt(0);
                }

                SakuraParticleState particle = new SakuraParticleState();
                particle.Origin = new Vector2(UnityEngine.Random.Range(rect.width * 0.18f, rect.width * 0.82f), UnityEngine.Random.Range(26f, 94f));
                particle.SpawnTime = Time.unscaledTime;
                particle.Lifetime = UnityEngine.Random.Range(1.2f, 1.9f);
                particle.Speed = UnityEngine.Random.Range(76f, 118f);
                particle.SwayAmplitude = UnityEngine.Random.Range(8f, 15f);
                particle.SwayFrequency = UnityEngine.Random.Range(2.0f, 3.8f);
                particle.Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                particle.RotationSpeed = UnityEngine.Random.Range(26f, 48f);
                particle.Size = UnityEngine.Random.Range(10.5f, 16.5f);
                _sakuraScrollParticles.Add(particle);
            }
        }

        private void StartPlumRipple(Rect rect)
        {
            _plumRipple.Active = true;
            _plumRipple.Center = rect.center;
            _plumRipple.Bounds = rect;
            _plumRipple.StartTime = Time.unscaledTime;
        }

        private void StartSakuraKnotMorph(Rect rect)
        {
            _sakuraKnotMorph.Active = true;
            _sakuraKnotMorph.Bounds = rect;
            _sakuraKnotMorph.StartTime = Time.unscaledTime;
        }

        private static float EvaluateSoftBezier(float t)
        {
            return ToggleSwitchMath.EvaluateSoftBezier(t);
        }

        private float GetGuiAnimationDeltaTime()
        {
            return _guiAnimationClock.GetDeltaTime(Time.frameCount, Time.unscaledTime);
        }

        private InteractiveAnimationState GetInteractiveAnimationState(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                key = "interactive";
            }

            InteractiveAnimationState state;
            if (!_interactiveAnimations.TryGetValue(key, out state))
            {
                state = new InteractiveAnimationState();
                _interactiveAnimations[key] = state;
            }

            return state;
        }

        private Rect GetAnimatedControlRect(string key, Rect baseRect, bool hovered, bool clicked, bool toggled, float scaleBoost, float translateDistance, out float alpha)
        {
            InteractiveAnimationState state = GetInteractiveAnimationState(key);
            float deltaTime = GetGuiAnimationDeltaTime();

            state.HoverAmount = Mathf.MoveTowards(state.HoverAmount, hovered ? 1f : 0f, deltaTime / InteractiveAnimationDuration);
            state.PressAmount = clicked ? 1f : Mathf.MoveTowards(state.PressAmount, 0f, deltaTime / InteractiveAnimationDuration);
            state.VisibleAmount = Mathf.MoveTowards(state.VisibleAmount, 1f, deltaTime / InteractiveAnimationDuration);
            state.ToggleAmount = ToggleSwitchMath.AdvanceToggleAmount(state.ToggleAmount, toggled, deltaTime, InteractiveAnimationDuration);
            state.HasToggleValue = true;
            state.LastSeenFrame = Time.frameCount;

            float easedHover = EvaluateSoftBezier(state.HoverAmount);
            float easedPress = EvaluateSoftBezier(state.PressAmount);
            float easedVisible = EvaluateSoftBezier(state.VisibleAmount);
            float scale = 1f + easedHover * scaleBoost + easedPress * (scaleBoost * 0.9f);
            Vector2 offset = new Vector2(0f, -translateDistance * (0.55f * easedVisible + 0.30f * easedHover + 0.15f * easedPress));
            alpha = Mathf.Clamp01(0.82f + easedVisible * 0.12f + easedHover * 0.08f + easedPress * 0.04f);

            Vector2 size = baseRect.size * scale;
            Vector2 center = baseRect.center + offset;
            return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
        }

        private static Rect RemapRect(Rect sourceParent, Rect targetParent, Rect childRect)
        {
            if (sourceParent.width <= 0f || sourceParent.height <= 0f)
            {
                return childRect;
            }

            float relativeX = (childRect.x - sourceParent.x) / sourceParent.width;
            float relativeY = (childRect.y - sourceParent.y) / sourceParent.height;
            float relativeWidth = childRect.width / sourceParent.width;
            float relativeHeight = childRect.height / sourceParent.height;
            return new Rect(
                targetParent.x + targetParent.width * relativeX,
                targetParent.y + targetParent.height * relativeY,
                targetParent.width * relativeWidth,
                targetParent.height * relativeHeight);
        }

        private static float EvaluateCubicBezier(float p0, float p1, float p2, float p3, float t)
        {
            float omt = 1f - t;
            return omt * omt * omt * p0
                + 3f * omt * omt * t * p1
                + 3f * omt * t * t * p2
                + t * t * t * p3;
        }

        private float GetDpiScaleFactor()
        {
            float dpi = Screen.dpi;
            if (dpi <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp(dpi / 96f, 1f, 2f);
        }

        private Vector2 GetParallaxOffset(float maxOffset)
        {
            Vector2 fallback = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 mousePosition = Event.current != null ? Event.current.mousePosition : fallback;
            float normalizedX = Mathf.Clamp(mousePosition.x / Mathf.Max(1f, Screen.width), 0f, 1f) * 2f - 1f;
            float normalizedY = Mathf.Clamp(mousePosition.y / Mathf.Max(1f, Screen.height), 0f, 1f) * 2f - 1f;
            return new Vector2(normalizedX * maxOffset, normalizedY * maxOffset);
        }

        private void TryUpdateThemeBackgroundParticles(float now)
        {
            if (!float.IsNegativeInfinity(_lastThemeBackgroundUpdateTime) && now - _lastThemeBackgroundUpdateTime < ThemeBackgroundUpdateInterval)
            {
                return;
            }

            _lastThemeBackgroundUpdateTime = now;
            UpdateThemeBackgroundParticles(now);
        }

        private void UpdateThemeBackgroundParticles(float now)
        {
            if (_activeMenuThemeStyle == StartupAnimationStyle.HolographicScan)
            {
                while (now >= _nextTechColumnSpawnTime)
                {
                    SpawnTechBackgroundColumn(now);
                    _nextTechColumnSpawnTime += 1f / UnityEngine.Random.Range(TechColumnSpawnRateMin, TechColumnSpawnRateMax);
                }

                _plumBackgroundParticles.Clear();
                _sakuraBackgroundParticles.Clear();
                _darkGlitchBlocks.Clear();
                _nextPlumParticleSpawnTime = now;
                _nextSakuraParticleSpawnTime = now;
                _nextDarkGlitchSpawnTime = now;
                return;
            }

            if (_activeMenuThemeStyle == StartupAnimationStyle.ObsidianPulse)
            {
                while (now >= _nextDarkGlitchSpawnTime)
                {
                    SpawnDarkGlitchBlock(now);
                    _nextDarkGlitchSpawnTime += 1f / UnityEngine.Random.Range(DarkGlitchSpawnRateMin, DarkGlitchSpawnRateMax);
                }

                _plumBackgroundParticles.Clear();
                _sakuraBackgroundParticles.Clear();
                _techBackgroundColumns.Clear();
                _nextPlumParticleSpawnTime = now;
                _nextSakuraParticleSpawnTime = now;
                _nextTechColumnSpawnTime = now;
                return;
            }

            if (_activeMenuThemeStyle == StartupAnimationStyle.PlumBlossomBloom)
            {
                while (now >= _nextPlumParticleSpawnTime)
                {
                    SpawnPlumBackgroundParticle(now);
                    _nextPlumParticleSpawnTime += 1f / UnityEngine.Random.Range(PlumParticleSpawnRateMin, PlumParticleSpawnRateMax);
                }

                _sakuraBackgroundParticles.Clear();
                _techBackgroundColumns.Clear();
                _darkGlitchBlocks.Clear();
                _nextSakuraParticleSpawnTime = now;
                _nextTechColumnSpawnTime = now;
                _nextDarkGlitchSpawnTime = now;
                return;
            }

            if (_activeMenuThemeStyle == StartupAnimationStyle.SakuraDrift)
            {
                while (now >= _nextSakuraParticleSpawnTime)
                {
                    SpawnSakuraBackgroundParticle(now);
                    _nextSakuraParticleSpawnTime += 1f / UnityEngine.Random.Range(SakuraParticleSpawnRateMin, SakuraParticleSpawnRateMax);
                }

                _plumBackgroundParticles.Clear();
                _techBackgroundColumns.Clear();
                _darkGlitchBlocks.Clear();
                _nextPlumParticleSpawnTime = now;
                _nextTechColumnSpawnTime = now;
                _nextDarkGlitchSpawnTime = now;
                return;
            }

            _plumBackgroundParticles.Clear();
            _sakuraBackgroundParticles.Clear();
            _techBackgroundColumns.Clear();
            _darkGlitchBlocks.Clear();
            _nextPlumParticleSpawnTime = now;
            _nextSakuraParticleSpawnTime = now;
            _nextTechColumnSpawnTime = now;
            _nextDarkGlitchSpawnTime = now;
        }

        private void SpawnTechBackgroundColumn(float now)
        {
            if (_techBackgroundColumns.Count >= MaxTechBackgroundColumns)
            {
                _techBackgroundColumns.RemoveAt(0);
            }

            float width = Mathf.Max(1f, Screen.width);
            TechBackgroundColumnState column = new TechBackgroundColumnState();
            column.X = UnityEngine.Random.Range(-width * 0.04f, width * 1.04f);
            column.SpawnTime = now;
            column.Speed = UnityEngine.Random.Range(126f, 212f);
            column.Width = UnityEngine.Random.Range(6f, 11f);
            column.SegmentHeight = UnityEngine.Random.Range(7f, 13f);
            column.GapHeight = UnityEngine.Random.Range(8f, 14f);
            column.SegmentCount = UnityEngine.Random.Range(11, 24);
            column.Alpha = UnityEngine.Random.Range(0.12f, 0.24f);
            column.Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            column.DriftAmplitude = UnityEngine.Random.Range(6f, 18f);
            _techBackgroundColumns.Add(column);
        }

        private void SpawnPlumBackgroundParticle(float now)
        {
            if (_plumBackgroundParticles.Count >= MaxThemeBackgroundParticles)
            {
                _plumBackgroundParticles.RemoveAt(0);
            }

            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            float lifetime = UnityEngine.Random.Range(4f, 6f);
            float startX = UnityEngine.Random.Range(-width * 0.10f, width * 1.10f);
            float startY = UnityEngine.Random.Range(-height * 0.08f, -12f);
            float horizontalTravel = UnityEngine.Random.Range(width * 0.16f, width * 0.32f) * (UnityEngine.Random.value > 0.18f ? 1f : -1f);

            PlumBackgroundParticleState particle = new PlumBackgroundParticleState();
            particle.Start = new Vector2(startX, startY);
            particle.ControlA = new Vector2(startX + horizontalTravel * 0.22f, startY + UnityEngine.Random.Range(height * 0.10f, height * 0.24f));
            particle.ControlB = new Vector2(startX + horizontalTravel * 0.74f, startY + UnityEngine.Random.Range(height * 0.32f, height * 0.58f));
            particle.End = new Vector2(startX + horizontalTravel, startY + UnityEngine.Random.Range(height * 0.72f, height * 1.04f));
            particle.SpawnTime = now;
            particle.Lifetime = lifetime;
            particle.VerticalSpeed = UnityEngine.Random.Range(18f, 42f);
            particle.RotationSpeed = UnityEngine.Random.Range(60f, 120f) * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
            particle.BaseRotation = UnityEngine.Random.Range(0f, 360f);
            particle.Size = UnityEngine.Random.Range(15f, 24f) * Mathf.Lerp(0.94f, 1.14f, GetDpiScaleFactor() - 1f);
            particle.Alpha = UnityEngine.Random.Range(0.22f, 0.42f);
            particle.Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _plumBackgroundParticles.Add(particle);
        }

        private void SpawnDarkGlitchBlock(float now)
        {
            if (_darkGlitchBlocks.Count >= MaxDarkGlitchBlocks)
            {
                _darkGlitchBlocks.RemoveAt(0);
            }

            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            float blockWidth = UnityEngine.Random.Range(54f, 188f);
            float blockHeight = UnityEngine.Random.Range(10f, 28f);

            DarkGlitchBlockState block = new DarkGlitchBlockState();
            block.Bounds = new Rect(
                UnityEngine.Random.Range(-blockWidth * 0.12f, width - blockWidth * 0.20f),
                UnityEngine.Random.Range(26f, height - 42f),
                blockWidth,
                blockHeight);
            block.SpawnTime = now;
            block.Lifetime = UnityEngine.Random.Range(0.45f, 1.10f);
            block.Alpha = UnityEngine.Random.Range(0.08f, 0.18f);
            block.HorizontalDrift = UnityEngine.Random.Range(24f, 96f) * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
            block.VerticalDrift = UnityEngine.Random.Range(-6f, 6f);
            block.Jitter = UnityEngine.Random.Range(3f, 12f);
            block.Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _darkGlitchBlocks.Add(block);
        }

        private void SpawnSakuraBackgroundParticle(float now)
        {
            if (_sakuraBackgroundParticles.Count >= MaxThemeBackgroundParticles)
            {
                _sakuraBackgroundParticles.RemoveAt(0);
            }

            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            float dpiScale = GetDpiScaleFactor();
            SakuraBackgroundParticleState particle = new SakuraBackgroundParticleState();
            particle.Start = new Vector2(UnityEngine.Random.Range(0f, width), UnityEngine.Random.Range(-height * 0.08f, -8f));
            particle.SpawnTime = now;
            particle.Speed = UnityEngine.Random.Range(80f, 120f);
            particle.RotationSpeed = UnityEngine.Random.Range(42f, 92f) * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
            particle.BaseRotation = UnityEngine.Random.Range(0f, 360f);
            particle.Size = UnityEngine.Random.Range(18f, 28f) * UnityEngine.Random.Range(0.6f, 1.0f);
            particle.Alpha = UnityEngine.Random.Range(0.24f, 0.42f);
            particle.Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            particle.DpiScale = dpiScale;
            particle.LandY = height + UnityEngine.Random.Range(-12f, 20f);
            particle.LandTime = float.NegativeInfinity;
            _sakuraBackgroundParticles.Add(particle);
        }

        private void DrawFullscreenThemeBackground()
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            ThemeTextureSet theme = GetThemeTextureSet(_activeMenuThemeStyle);
            float now = Time.unscaledTime;
            if (_activeMenuThemeStyle == StartupAnimationStyle.HolographicScan)
            {
                DrawTechFullscreenColumns(theme, now);
            }
            else if (_activeMenuThemeStyle == StartupAnimationStyle.ObsidianPulse)
            {
                DrawDarkFullscreenGlitches(theme, now);
            }
            else if (_activeMenuThemeStyle == StartupAnimationStyle.PlumBlossomBloom)
            {
                DrawPlumFullscreenParticles(theme, now);
            }
            else if (_activeMenuThemeStyle == StartupAnimationStyle.SakuraDrift)
            {
                DrawSakuraFullscreenParticles(theme, now);
            }
        }

        private void DrawTechFullscreenColumns(ThemeTextureSet theme, float now)
        {
            Color previousColor = GUI.color;
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            for (int lane = 0; lane < 8; lane++)
            {
                float laneX = screenWidth * (0.06f + lane * 0.115f) + Mathf.Sin(now * 0.7f + lane * 0.6f) * 12f;
                float laneAlpha = 0.03f + lane * 0.004f;
                GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, laneAlpha);
                GUI.DrawTexture(new Rect(laneX, 0f, 1f, screenHeight), _whiteTexture);
            }

            for (int i = _techBackgroundColumns.Count - 1; i >= 0; i--)
            {
                TechBackgroundColumnState column = _techBackgroundColumns[i];
                float age = now - column.SpawnTime;
                float headY = -screenHeight * 0.18f + age * column.Speed;
                float trailHeight = column.SegmentCount * (column.SegmentHeight + column.GapHeight);
                if (headY - trailHeight > screenHeight + 60f)
                {
                    _techBackgroundColumns.RemoveAt(i);
                    continue;
                }

                for (int segment = 0; segment < column.SegmentCount; segment++)
                {
                    float progress = segment / (float)Mathf.Max(1, column.SegmentCount - 1);
                    float segmentY = headY - segment * (column.SegmentHeight + column.GapHeight);
                    if (segmentY < -40f || segmentY > screenHeight + 40f)
                    {
                        continue;
                    }

                    float sway = Mathf.Sin(now * 1.4f + column.Phase + segment * 0.35f) * column.DriftAmplitude;
                    float widthScale = segment % 4 == 0 ? 2.2f : (segment % 2 == 0 ? 1.0f : 1.5f);
                    float flicker = 0.70f + 0.30f * (0.5f + 0.5f * Mathf.Sin(now * 10f + column.Phase + segment));
                    float alpha = column.Alpha * (1f - progress * 0.78f) * flicker;
                    Color tint = Color.Lerp(
                        new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, alpha),
                        new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, alpha),
                        0.5f + 0.5f * Mathf.Sin(column.Phase + segment * 0.22f));
                    GUI.color = tint;
                    GUI.DrawTexture(new Rect(column.X + sway, segmentY, column.Width * widthScale, column.SegmentHeight), _whiteTexture);

                    if (segment % 5 == 0)
                    {
                        GUI.color = new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, alpha * 0.72f);
                        GUI.DrawTexture(new Rect(column.X + sway + column.Width * 1.8f, segmentY + 1f, column.Width * 0.6f, Mathf.Max(2f, column.SegmentHeight - 2f)), _whiteTexture);
                    }
                }
            }

            GUI.color = previousColor;
        }

        private void DrawPlumFullscreenParticles(ThemeTextureSet theme, float now)
        {
            Texture2D petalTexture = _startupPetalTexture;
            if (petalTexture == null)
            {
                return;
            }

            for (int i = _plumBackgroundParticles.Count - 1; i >= 0; i--)
            {
                PlumBackgroundParticleState particle = _plumBackgroundParticles[i];
                float age = now - particle.SpawnTime;
                float progress = age / particle.Lifetime;
                if (progress >= 1f)
                {
                    _plumBackgroundParticles.RemoveAt(i);
                    continue;
                }

                float x = EvaluateCubicBezier(particle.Start.x, particle.ControlA.x, particle.ControlB.x, particle.End.x, progress);
                float y = particle.Start.y + particle.VerticalSpeed * age + 0.5f * PlumParticleGravity * age * age;
                if (y > Screen.height + 48f || x < -Screen.width * 0.24f || x > Screen.width * 1.24f)
                {
                    _plumBackgroundParticles.RemoveAt(i);
                    continue;
                }

                float angle = particle.BaseRotation + particle.RotationSpeed * age;
                float alpha = particle.Alpha * (1f - progress * 0.82f);
                float size = particle.Size * (0.92f + 0.10f * Mathf.Sin(now * 2.1f + particle.Phase));
                Color tint = Color.Lerp(
                    new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, alpha),
                    new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, alpha),
                    0.5f + 0.5f * Mathf.Sin(now * 1.4f + particle.Phase));
                DrawRotatedTexture(new Vector2(x, y), new Vector2(size, size * 1.22f), angle, petalTexture, tint);
            }
        }

        private void DrawSakuraFullscreenParticles(ThemeTextureSet theme, float now)
        {
            Texture2D petalTexture = _startupSakuraPetalTexture;
            if (petalTexture == null)
            {
                return;
            }

            for (int i = _sakuraBackgroundParticles.Count - 1; i >= 0; i--)
            {
                SakuraBackgroundParticleState particle = _sakuraBackgroundParticles[i];
                float age = now - particle.SpawnTime;
                float x = particle.Start.x + Mathf.Sin(age * (Mathf.PI * 2f / 3f) + particle.Phase) * 30f;
                float y = particle.Start.y + age * particle.Speed;
                if (float.IsNegativeInfinity(particle.LandTime) && y >= particle.LandY)
                {
                    particle.LandTime = now;
                    y = particle.LandY;
                    _sakuraBackgroundParticles[i] = particle;
                }

                float fade = 1f;
                if (!float.IsNegativeInfinity(particle.LandTime))
                {
                    fade = 1f - Mathf.Clamp01((now - particle.LandTime) / SakuraParticleFadeDuration);
                    y = particle.LandY;
                    if (fade <= 0f)
                    {
                        _sakuraBackgroundParticles.RemoveAt(i);
                        continue;
                    }
                }

                if (y > Screen.height + 36f)
                {
                    _sakuraBackgroundParticles.RemoveAt(i);
                    continue;
                }

                float angle = particle.BaseRotation + particle.RotationSpeed * age;
                float alphaJitter = 0.80f + 0.20f * (0.5f + 0.5f * Mathf.Sin(now * 8.0f + particle.Phase));
                float alpha = particle.Alpha * alphaJitter * fade;
                float size = particle.Size * particle.DpiScale;
                DrawRotatedTexture(new Vector2(x, y), new Vector2(size, size * 1.02f), angle, petalTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, alpha));
            }
        }

        private void DrawDarkFullscreenGlitches(ThemeTextureSet theme, float now)
        {
            Color previousColor = GUI.color;
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            for (int i = 0; i < 10; i++)
            {
                float lineProgress = Mathf.Repeat(now * (0.08f + i * 0.01f) + i * 0.14f, 1f);
                float y = Mathf.Lerp(0f, screenHeight, lineProgress);
                GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.025f + i * 0.003f);
                GUI.DrawTexture(new Rect(0f, y, screenWidth, 1f), _whiteTexture);
            }

            for (int i = _darkGlitchBlocks.Count - 1; i >= 0; i--)
            {
                DarkGlitchBlockState block = _darkGlitchBlocks[i];
                float progress = (now - block.SpawnTime) / block.Lifetime;
                if (progress >= 1f)
                {
                    _darkGlitchBlocks.RemoveAt(i);
                    continue;
                }

                float jitterX = Mathf.Sin(now * 24f + block.Phase) * block.Jitter * (1f - progress) + block.HorizontalDrift * progress;
                float jitterY = Mathf.Cos(now * 14f + block.Phase) * block.Jitter * 0.18f + block.VerticalDrift * progress;
                float alpha = block.Alpha * (1f - progress) * (0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(now * 18f + block.Phase)));
                Rect rect = new Rect(block.Bounds.x + jitterX, block.Bounds.y + jitterY, block.Bounds.width, block.Bounds.height);

                GUI.color = new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, alpha * 0.52f);
                GUI.DrawTexture(new Rect(rect.x - 3f, rect.y, rect.width, rect.height), _whiteTexture);
                GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, alpha);
                GUI.DrawTexture(rect, _whiteTexture);
                GUI.color = new Color(theme.DecorativeTint.r, theme.DecorativeTint.g, theme.DecorativeTint.b, alpha * 0.72f);
                GUI.DrawTexture(new Rect(rect.x + 4f, rect.y + rect.height * 0.62f, rect.width * 0.66f, Mathf.Max(2f, rect.height * 0.22f)), _whiteTexture);
            }

            GUI.color = previousColor;
        }

        private void DrawMenuAnimatedBackground(Rect rect)
        {
            ThemeTextureSet theme = GetThemeTextureSet(_activeMenuThemeStyle);
            AuxMenuThemeProfile profile = GetActiveThemeProfile();
            float time = Time.unscaledTime;
            Vector2 parallax = _activeMenuThemeStyle == StartupAnimationStyle.ObsidianPulse ? GetParallaxOffset(BackgroundParallaxLimit) : Vector2.zero;

            Color previousColor = GUI.color;
            GUI.color = new Color(theme.PanelGlow.r, theme.PanelGlow.g, theme.PanelGlow.b, theme.PanelGlow.a + 0.08f);
            GUI.DrawTexture(new Rect(rect.x + 10f + parallax.x * 0.18f, rect.y + 10f + parallax.y * 0.18f, rect.width - 20f, rect.height - 20f), _whiteTexture);
            DrawMenuAnimatedFrame(rect, theme, time);
            DrawMenuStyleSignature(rect, theme, time);

            switch (_activeMenuThemeStyle)
            {
                case StartupAnimationStyle.ObsidianPulse:
                    DrawDarkMenuBackdrop(rect, theme, time, parallax);
                    break;
                case StartupAnimationStyle.PlumBlossomBloom:
                    DrawPlumMenuBackdrop(rect, theme, profile, time);
                    DrawMenuPetalDrift(rect, theme, false);
                    DrawMenuFlowerAccent(new Vector2(rect.width * profile.MainVisualAnchorX, rect.height * profile.MainVisualAnchorY), 54f + Mathf.Sin(time * 2.2f) * 3f, theme, false);
                    break;
                case StartupAnimationStyle.SakuraDrift:
                    DrawSakuraMenuBackdrop(rect, theme, profile, time);
                    DrawMenuPetalDrift(rect, theme, true);
                    DrawMenuFlowerAccent(new Vector2(rect.width * profile.MainVisualAnchorX, rect.height * profile.MainVisualAnchorY), 50f + Mathf.Sin(time * 2.0f) * 3f, theme, true);
                    break;
                default:
                    DrawTechMenuBackdrop(rect, theme, profile, time);
                    break;
            }

            GUI.color = previousColor;
        }

        private void DrawHeaderThemeOverlay(Rect rect)
        {
            ThemeTextureSet theme = GetThemeTextureSet(_activeMenuThemeStyle);
            float time = Time.unscaledTime;
            Color previousColor = GUI.color;

            GUI.color = theme.HeaderOverlay;
            GUI.DrawTexture(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, rect.height - 20f), _whiteTexture);

            switch (_activeMenuThemeStyle)
            {
                case StartupAnimationStyle.ObsidianPulse:
                    DrawRotatedTexture(new Vector2(rect.xMax - 48f, rect.center.y), new Vector2(26f, 26f), time * 74f, _startupRingTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.44f));
                    DrawCenteredTexture(new Vector2(rect.xMax - 48f, rect.center.y), 12f + Mathf.Sin(time * 5.4f) * 1.5f, _switchKnobTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.54f));
                    break;
                case StartupAnimationStyle.PlumBlossomBloom:
                    DrawRotatedTexture(new Vector2(rect.x + 34f, rect.center.y - 1f), new Vector2(92f, 2f), -24f, _whiteTexture, new Color(0.36f, 0.26f, 0.22f, 0.32f));
                    DrawMenuFlowerAccent(new Vector2(rect.x + 64f, rect.center.y + 2f), 22f, theme, false);
                    break;
                case StartupAnimationStyle.SakuraDrift:
                    DrawCenteredTexture(new Vector2(rect.center.x, rect.center.y), 32f, _startupGlowTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.14f));
                    DrawMenuFlowerAccent(new Vector2(rect.xMax - 56f, rect.center.y + 2f), 20f, theme, true);
                    break;
                default:
                    float lineX = Mathf.Lerp(rect.x + 18f, rect.xMax - 88f, Mathf.Repeat(time * 0.75f, 1f));
                    GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.46f);
                    GUI.DrawTexture(new Rect(lineX, rect.yMax - 8f, 64f, 3f), _whiteTexture);
                    DrawCenteredTexture(new Vector2(rect.xMax - 48f, rect.center.y), 18f, _startupDiamondTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.58f));
                    break;
            }

            GUI.color = previousColor;
        }

        private void DrawSwitchThemeGlow(Rect trackRect, bool enabled)
        {
            ThemeTextureSet theme = GetThemeTextureSet(_activeMenuThemeStyle);
            float pulse = 0.16f + (enabled ? 0.20f : 0.08f) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.4f));
            DrawCenteredTexture(trackRect.center, new Vector2(trackRect.width + 18f, trackRect.height + 12f), _startupGlowTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, pulse));
        }

        private void DrawMenuAnimatedFrame(Rect rect, ThemeTextureSet theme, float time)
        {
            if (_activeMenuThemeStyle == StartupAnimationStyle.PlumBlossomBloom)
            {
                GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.18f);
                DrawRotatedTexture(new Vector2(rect.width * 0.28f, rect.height * 0.22f), new Vector2(rect.width * 0.52f, 2f), -26f, _whiteTexture, GUI.color);
                DrawRotatedTexture(new Vector2(rect.width * 0.74f, rect.height * 0.76f), new Vector2(rect.width * 0.32f, 2f), -26f, _whiteTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.14f));
                GUI.DrawTexture(new Rect(10f, 10f, rect.width - 20f, 1f), _whiteTexture);
                GUI.DrawTexture(new Rect(10f, rect.height - 12f, rect.width - 20f, 1f), _whiteTexture);
                return;
            }

            if (_activeMenuThemeStyle == StartupAnimationStyle.SakuraDrift)
            {
                GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.15f);
                GUI.DrawTexture(new Rect(rect.width * 0.20f, 14f, rect.width * 0.60f, 2f), _whiteTexture);
                GUI.DrawTexture(new Rect(rect.width * 0.24f, rect.height - 14f, rect.width * 0.52f, 2f), _whiteTexture);
                GUI.color = new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.10f);
                GUI.DrawTexture(new Rect(12f, rect.height * 0.22f, 2f, rect.height * 0.56f), _whiteTexture);
                GUI.DrawTexture(new Rect(rect.width - 14f, rect.height * 0.22f, 2f, rect.height * 0.56f), _whiteTexture);
                return;
            }

            GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.24f + Mathf.Sin(time * 2.8f) * 0.04f);
            GUI.DrawTexture(new Rect(10f, 10f, rect.width - 20f, 2f), _whiteTexture);
            GUI.DrawTexture(new Rect(10f, rect.height - 12f, rect.width - 20f, 2f), _whiteTexture);
            GUI.DrawTexture(new Rect(10f, 10f, 2f, rect.height - 20f), _whiteTexture);
            GUI.DrawTexture(new Rect(rect.width - 12f, 10f, 2f, rect.height - 20f), _whiteTexture);

            float sweepWidth = Mathf.Clamp(rect.width * 0.20f, 100f, 180f);
            float sweepX = Mathf.Repeat(time * 82f, rect.width + sweepWidth) - sweepWidth;
            GUI.color = new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.12f);
            GUI.DrawTexture(new Rect(sweepX, 12f, sweepWidth, 3f), _whiteTexture);
            GUI.DrawTexture(new Rect(rect.width - sweepX - sweepWidth, rect.height - 15f, sweepWidth * 0.66f, 3f), _whiteTexture);
        }

        private void DrawMenuStyleSignature(Rect rect, ThemeTextureSet theme, float time)
        {
            Rect badgeRect = new Rect(rect.width - 170f, rect.height - 76f, 138f, 44f);
            GUI.color = new Color(theme.HeaderOverlay.r, theme.HeaderOverlay.g, theme.HeaderOverlay.b, Mathf.Clamp01(theme.HeaderOverlay.a + 0.10f));
            GUI.DrawTexture(badgeRect, _panelTexture);
            GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.34f);
            GUI.DrawTexture(new Rect(badgeRect.x + 10f, badgeRect.y + 8f, badgeRect.width - 20f, 2f), _whiteTexture);

            switch (_activeMenuThemeStyle)
            {
                case StartupAnimationStyle.ObsidianPulse:
                    DrawRotatedTexture(new Vector2(badgeRect.x + 28f, badgeRect.center.y), new Vector2(42f, 2f), 0f, _whiteTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.42f));
                    DrawRotatedTexture(new Vector2(badgeRect.xMax - 22f, badgeRect.center.y), new Vector2(16f, 16f), time * 90f, _startupRingTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.70f));
                    break;
                case StartupAnimationStyle.PlumBlossomBloom:
                    DrawRotatedTexture(new Vector2(badgeRect.x + 28f, badgeRect.center.y), new Vector2(40f, 2f), -24f, _whiteTexture, new Color(0.36f, 0.26f, 0.22f, 0.42f));
                    DrawMenuFlowerAccent(new Vector2(badgeRect.x + 56f, badgeRect.center.y + 1f), 12f + Mathf.Sin(time * 2.6f) * 0.8f, theme, false);
                    break;
                case StartupAnimationStyle.SakuraDrift:
                    GUI.DrawTexture(new Rect(badgeRect.x + 24f, badgeRect.y + 13f, 54f, 8f), _whiteTexture);
                    GUI.DrawTexture(new Rect(badgeRect.x + 32f, badgeRect.y + 23f, 38f, 8f), _whiteTexture);
                    DrawRotatedTexture(new Vector2(badgeRect.xMax - 22f, badgeRect.center.y), new Vector2(14f, 14f), time * 36f, _startupSakuraPetalTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.82f));
                    break;
                default:
                    GUI.DrawTexture(new Rect(badgeRect.x + 18f, badgeRect.y + 16f, 34f, 2f), _whiteTexture);
                    GUI.DrawTexture(new Rect(badgeRect.x + 18f, badgeRect.y + 26f, 22f, 2f), _whiteTexture);
                    DrawRotatedTexture(new Vector2(badgeRect.xMax - 22f, badgeRect.center.y), new Vector2(15f, 15f), time * 62f, _startupDiamondTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.78f));
                    break;
            }
        }

        private void DrawPlumMenuBackdrop(Rect rect, ThemeTextureSet theme, AuxMenuThemeProfile profile, float time)
        {
            Vector2 blossomCenter = new Vector2(rect.width * profile.MainVisualAnchorX, rect.height * profile.MainVisualAnchorY);
            DrawCenteredTexture(blossomCenter, 158f + Mathf.Sin(time * 1.4f) * 10f, _startupGlowTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.18f));
            DrawRotatedTexture(new Vector2(rect.width * 0.16f, rect.height * 0.34f), new Vector2(188f, 4f), -62f, _whiteTexture, new Color(0.35f, 0.26f, 0.21f, 0.42f));
            DrawRotatedTexture(new Vector2(rect.width * 0.12f, rect.height * 0.26f), new Vector2(112f, 2f), -78f, _whiteTexture, new Color(0.22f, 0.34f, 0.24f, 0.28f));
            DrawRotatedTexture(new Vector2(rect.width * 0.18f, rect.height * 0.22f), new Vector2(92f, 2f), -78f, _whiteTexture, new Color(0.20f, 0.32f, 0.22f, 0.22f));
            DrawCenteredTexture(new Vector2(rect.width * 0.70f, rect.height * 0.66f), new Vector2(rect.width * 0.38f, rect.height * 0.18f), _startupGlowTexture, new Color(0.12f, 0.14f, 0.12f, 0.06f));
            GUI.color = new Color(0.18f, 0.18f, 0.16f, 0.06f + Mathf.Sin(time * 1.6f) * 0.01f);
            GUI.DrawTexture(new Rect(rect.width * 0.48f, rect.height * 0.60f, rect.width * 0.34f, rect.height * 0.16f), _whiteTexture);
            GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.10f);
            GUI.DrawTexture(new Rect(rect.width * 0.55f, rect.height * 0.70f, rect.width * 0.20f, 2f), _whiteTexture);
        }

        private void DrawTechMenuBackdrop(Rect rect, ThemeTextureSet theme, AuxMenuThemeProfile profile, float time)
        {
            Vector2 hubCenter = new Vector2(rect.width * profile.MainVisualAnchorX, rect.height * profile.MainVisualAnchorY);
            DrawCenteredTexture(hubCenter, 174f + Mathf.Sin(time * 1.6f) * 10f, _startupGlowTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.16f));

            Rect codeWallRect = new Rect(rect.width * 0.08f, rect.height * 0.16f, rect.width * 0.34f, rect.height * 0.56f);
            GUI.color = new Color(theme.HeaderOverlay.r, theme.HeaderOverlay.g, theme.HeaderOverlay.b, 0.24f);
            GUI.DrawTexture(codeWallRect, _panelTexture);

            for (int row = 0; row < 8; row++)
            {
                float rowY = codeWallRect.y + 18f + row * (codeWallRect.height / 9f);
                float lineWidth = Mathf.Lerp(codeWallRect.width * 0.24f, codeWallRect.width * 0.88f, 0.5f + 0.5f * Mathf.Sin(time * 1.5f + row * 0.7f));
                GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.10f + row * 0.01f);
                GUI.DrawTexture(new Rect(codeWallRect.x + 14f, rowY, lineWidth, 2f), _whiteTexture);
                GUI.color = new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.08f + row * 0.008f);
                GUI.DrawTexture(new Rect(codeWallRect.x + 14f, rowY + 6f, lineWidth * 0.62f, 1f), _whiteTexture);
            }

            for (int column = 0; column < 5; column++)
            {
                float bitX = rect.width * 0.58f + column * 28f;
                for (int bit = 0; bit < 10; bit++)
                {
                    float bitY = rect.height * 0.16f + bit * 28f + Mathf.Sin(time * (1.4f + column * 0.08f) + bit * 0.5f) * 4f;
                    float width = bit % 3 == 0 ? 18f : 8f;
                    Color bitColor = bit % 2 == 0
                        ? new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.16f)
                        : new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.12f);
                    GUI.color = bitColor;
                    GUI.DrawTexture(new Rect(bitX, bitY, width, 7f), _whiteTexture);
                }
            }

            float scanX = Mathf.Repeat(time * 126f, Mathf.Max(1f, rect.width + 120f)) - 60f;
            GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.12f);
            GUI.DrawTexture(new Rect(scanX, 18f, 62f, rect.height - 36f), _whiteTexture);
            DrawRotatedTexture(new Vector2(rect.width - 104f, 104f), new Vector2(64f, 64f), time * 52f, _startupDiamondTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.48f));
        }

        private void DrawDarkMenuBackdrop(Rect rect, ThemeTextureSet theme, float time, Vector2 parallax)
        {
            Vector2 coreCenter = new Vector2(rect.width - 108f, 102f) + parallax;
            DrawCenteredTexture(coreCenter + new Vector2(0f, -4f), 146f + Mathf.Sin(time * 2.8f) * 12f, _startupGlowTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.18f));
            DrawRotatedTexture(coreCenter, new Vector2(108f, 108f), time * 28f, _startupRingTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.34f));
            DrawRotatedTexture(coreCenter, new Vector2(72f, 72f), -time * 44f, _startupRingTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.28f));

            for (int i = 0; i < 5; i++)
            {
                float orbitAngle = time * 1.4f + i * (Mathf.PI * 2f / 5f);
                Vector2 nodeCenter = coreCenter + new Vector2(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle)) * 42f;
                DrawCenteredTexture(nodeCenter, 8f + Mathf.Sin(time * 5.6f + i) * 1.5f, _switchKnobTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.72f));
            }

            Rect glitchBand = new Rect(rect.width * 0.12f + parallax.x * 0.12f, rect.height * 0.26f + parallax.y * 0.12f, rect.width * 0.44f, rect.height * 0.28f);
            GUI.color = new Color(theme.HeaderOverlay.r, theme.HeaderOverlay.g, theme.HeaderOverlay.b, 0.22f);
            GUI.DrawTexture(glitchBand, _panelTexture);

            for (int i = 0; i < 7; i++)
            {
                float sliceY = glitchBand.y + 8f + i * 18f;
                float sliceWidth = Mathf.Lerp(glitchBand.width * 0.28f, glitchBand.width * 0.92f, 0.5f + 0.5f * Mathf.Sin(time * (3.8f + i * 0.16f) + i));
                float offset = Mathf.Sin(time * (10f + i) + i * 0.6f) * (6f + i * 0.9f);
                GUI.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.14f + i * 0.01f);
                GUI.DrawTexture(new Rect(glitchBand.x + 10f + offset, sliceY, sliceWidth, 4f), _whiteTexture);
                GUI.color = new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.10f);
                GUI.DrawTexture(new Rect(glitchBand.x + 14f - offset * 0.5f, sliceY + 5f, sliceWidth * 0.62f, 2f), _whiteTexture);
            }
        }

        private void DrawSakuraMenuBackdrop(Rect rect, ThemeTextureSet theme, AuxMenuThemeProfile profile, float time)
        {
            Vector2 center = new Vector2(rect.width * profile.MainVisualAnchorX, rect.height * profile.MainVisualAnchorY);
            DrawCenteredTexture(center, 168f + Mathf.Sin(time * 1.3f) * 8f, _startupGlowTexture, new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.18f));
            DrawCenteredTexture(center, 118f, _startupGlowTexture, new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.14f));

            Rect cardA = new Rect(center.x - 118f, rect.height * 0.60f, 236f, 82f);
            Rect cardB = new Rect(center.x - 98f, rect.height * 0.66f, 196f, 72f);
            float cardEase = EvaluateSoftBezier(Mathf.Repeat(time * 0.16f, 1f));
            GUI.color = new Color(1f, 1f, 1f, 0.16f);
            GUI.DrawTexture(new Rect(cardA.x, cardA.y - (1f - cardEase) * 18f, cardA.width, cardA.height), _panelTexture);
            GUI.color = new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.14f);
            GUI.DrawTexture(new Rect(cardB.x, cardB.y - (1f - cardEase) * 12f, cardB.width, cardB.height), _panelTexture);
            GUI.color = new Color(0.88f, 0.95f, 1.00f, 0.06f);
            GUI.DrawTexture(new Rect(rect.width * 0.18f, rect.height * 0.18f, rect.width * 0.64f, 1f), _whiteTexture);
        }

        private void DrawMenuPetalDrift(Rect rect, ThemeTextureSet theme, bool sakura)
        {
            Texture2D petalTexture = sakura ? _startupSakuraPetalTexture : _startupPetalTexture;
            float time = Time.unscaledTime;
            AuxMenuThemeProfile profile = GetActiveThemeProfile();
            Vector2 mousePosition = Event.current != null ? Event.current.mousePosition : new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            float mouseInfluence = Mathf.InverseLerp(0f, rect.width, mousePosition.x) * 2f - 1f;
            for (int i = 0; i < 12; i++)
            {
                float drift = Mathf.Repeat(time * (0.10f + i * 0.006f) + i * 0.13f, 1f);
                float x = sakura
                    ? Mathf.Lerp(rect.width * 0.26f, rect.width * 0.74f, Mathf.Repeat(i * 0.17f + 0.08f, 1f))
                    : Mathf.Lerp(rect.width * 0.42f, rect.width - 26f, Mathf.Repeat(i * 0.17f + 0.08f, 1f));
                float y = Mathf.Lerp(18f, rect.height - 20f, drift);
                float sway = Mathf.Sin(time * (1.6f + i * 0.12f) + i) * (sakura ? 15f : 7f + i * 0.3f);
                float tilt = mouseInfluence * Mathf.Lerp(profile.PetalTiltMinDegrees, profile.PetalTiltMaxDegrees, 0.5f + 0.5f * Mathf.Sin(time * 0.8f + i));
                float angle = Mathf.Repeat(time * (26f + i * 3f) + i * 22f, 360f) + tilt;
                float size = (sakura ? 10.5f : 9.5f) + (i % 3) * (sakura ? 2.4f : 2.0f);
                Color tint = i % 2 == 0
                    ? new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.12f + (1f - drift) * 0.08f)
                    : new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, 0.10f + (1f - drift) * 0.07f);
                DrawRotatedTexture(new Vector2(x + sway, y), new Vector2(size, size * (sakura ? 1.04f : 1.22f)), angle, petalTexture, tint);
            }
        }

        private void DrawMenuFlowerAccent(Vector2 center, float scale, ThemeTextureSet theme, bool sakura)
        {
            Texture2D petalTexture = sakura ? _startupSakuraPetalTexture : _startupPetalTexture;
            for (int i = 0; i < 5; i++)
            {
                float angle = -90f + i * 72f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (scale * 0.22f);
                float width = scale * (sakura ? 0.86f : 0.74f);
                float height = scale * (sakura ? 0.94f : 1.08f);
                Color petalTint = sakura
                    ? new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.72f)
                    : new Color(theme.SecondaryAccent.r, theme.SecondaryAccent.g, theme.SecondaryAccent.b, 0.64f);
                DrawRotatedTexture(center + offset, new Vector2(width, height), angle + 90f, petalTexture, petalTint);
            }

            if (sakura)
            {
                DrawCenteredTexture(center, scale * 0.15f, _startupGlowTexture, new Color(1f, 244f / 255f, 202f / 255f, 0.24f));
                DrawSakuraStamenCluster(center, scale * 0.04f, 0.82f);
                return;
            }

            DrawCenteredTexture(center, scale * 0.14f, _switchKnobTexture, new Color(1f, 0.88f, 0.54f, 0.72f));
        }

        private void DrawSakuraStamenCluster(Vector2 center, float radius, float alpha)
        {
            Color stamenColor = new Color(1f, 244f / 255f, 202f / 255f, alpha);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * 1.8f;
                DrawCenteredTexture(center + offset, radius * 1.2f, _switchKnobTexture, stamenColor);
            }

            DrawCenteredTexture(center, radius * 1.8f, _switchKnobTexture, new Color(1f, 244f / 255f, 202f / 255f, alpha * 0.92f));
        }

        private void DrawMenuThemeTransitionOverlay()
        {
            float reveal = _menuLifecycle.GetTransitionAlpha(Time.unscaledTime);
            if (reveal >= 1f)
            {
                return;
            }

            ThemeTextureSet theme = GetThemeTextureSet(_activeMenuThemeStyle);
            Color previousColor = GUI.color;
            float alpha = 1f - reveal;
            GUI.color = new Color(theme.HeaderOverlay.r, theme.HeaderOverlay.g, theme.HeaderOverlay.b, Mathf.Clamp01(0.72f * alpha));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _whiteTexture);
            GUI.color = previousColor;
        }

        private void TrackMenuVisualTextures()
        {
            HashSet<int> trackedIds = new HashSet<int>();
            TrackThemeTextureSet(_holographicThemeTextures, trackedIds);
            TrackThemeTextureSet(_obsidianThemeTextures, trackedIds);
            TrackThemeTextureSet(_plumThemeTextures, trackedIds);
            TrackThemeTextureSet(_sakuraThemeTextures, trackedIds);
            TrackTexture(_whiteTexture, trackedIds);
            TrackTexture(_switchKnobTexture, trackedIds);
            TrackTexture(_startupGlowTexture, trackedIds);
            TrackTexture(_startupRingTexture, trackedIds);
            TrackTexture(_startupRepoTexture, trackedIds);
            TrackTexture(_startupSparkTexture, trackedIds);
            TrackTexture(_startupDiamondTexture, trackedIds);
            TrackTexture(_startupChevronTexture, trackedIds);
            TrackTexture(_startupBracketTexture, trackedIds);
            TrackTexture(_startupPetalTexture, trackedIds);
            TrackTexture(_startupPetalHighlightTexture, trackedIds);
            TrackTexture(_startupSakuraPetalTexture, trackedIds);
            TrackTexture(_startupSakuraPetalHighlightTexture, trackedIds);
            _menuVisualsTracked = true;
        }

        private void TrackThemeTextureSet(ThemeTextureSet theme, HashSet<int> trackedIds)
        {
            if (theme == null)
            {
                return;
            }

            TrackTexture(theme.Window, trackedIds);
            TrackTexture(theme.Header, trackedIds);
            TrackTexture(theme.Panel, trackedIds);
            TrackTexture(theme.Tab, trackedIds);
            TrackTexture(theme.ActiveTab, trackedIds);
            TrackTexture(theme.Button, trackedIds);
            TrackTexture(theme.SwitchOn, trackedIds);
            TrackTexture(theme.SwitchOff, trackedIds);
            TrackTexture(theme.ResizeHandle, trackedIds);
        }

        private void TrackTexture(Texture2D texture, HashSet<int> trackedIds)
        {
            if (texture == null)
            {
                return;
            }

            int instanceId = texture.GetInstanceID();
            if (!trackedIds.Add(instanceId))
            {
                return;
            }

            _menuLifecycle.RegisterTextureAllocation(texture.width, texture.height);
        }

        private void ReleaseMenuVisualResources(bool releaseCachedAssets)
        {
            if (releaseCachedAssets)
            {
                _windowStyle = null;
                _headerStyle = null;
                _hintStyle = null;
                _titleStyle = null;
                _sectionStyle = null;
                _labelStyle = null;
                _buttonStyle = null;
                _tabStyle = null;
                _activeTabStyle = null;
                _panelStyle = null;
                _valueStyle = null;
                _sliderStyle = null;
                _sliderThumbStyle = null;
                _resizeHandleStyle = null;
                _startupDarkHeaderStyle = null;
                _startupDarkHintStyle = null;
                _startupDarkValueStyle = null;
                _startupLightHeaderStyle = null;
                _startupLightHintStyle = null;
                _startupLightValueStyle = null;

                ReleaseThemeTextureSet(ref _holographicThemeTextures);
                ReleaseThemeTextureSet(ref _obsidianThemeTextures);
                ReleaseThemeTextureSet(ref _plumThemeTextures);
                ReleaseThemeTextureSet(ref _sakuraThemeTextures);

                DestroyTexture(ref _windowTexture);
                DestroyTexture(ref _headerTexture);
                DestroyTexture(ref _panelTexture);
                DestroyTexture(ref _whiteTexture);
                DestroyTexture(ref _tabTexture);
                DestroyTexture(ref _activeTabTexture);
                DestroyTexture(ref _buttonTexture);
                DestroyTexture(ref _switchOnTexture);
                DestroyTexture(ref _switchOffTexture);
                DestroyTexture(ref _switchKnobTexture);
                DestroyTexture(ref _resizeHandleTexture);
                DestroyTexture(ref _startupGlowTexture);
                DestroyTexture(ref _startupRingTexture);
                DestroyTexture(ref _startupRepoTexture);
                DestroyTexture(ref _startupSparkTexture);
                DestroyTexture(ref _startupDiamondTexture);
                DestroyTexture(ref _startupChevronTexture);
                DestroyTexture(ref _startupBracketTexture);
                DestroyTexture(ref _startupPetalTexture);
                DestroyTexture(ref _startupPetalHighlightTexture);
                DestroyTexture(ref _startupSakuraPetalTexture);
                DestroyTexture(ref _startupSakuraPetalHighlightTexture);
            }

            _plumRipple = default(PlumRippleState);
            _sakuraKnotMorph = default(SakuraKnotMorphState);
            _sakuraScrollParticles.Clear();
            _plumBackgroundParticles.Clear();
            _sakuraBackgroundParticles.Clear();
            _techBackgroundColumns.Clear();
            _darkGlitchBlocks.Clear();
            _interactiveAnimations.Clear();
            _hasHoveredInteractiveRect = false;
            _plumHoverStartTime = float.NegativeInfinity;
            _lastThemeBackgroundUpdateTime = float.NegativeInfinity;
            _guiAnimationClock.Reset();

            _menuVisualsTracked = !releaseCachedAssets && _menuLifecycle.ActiveSession != null;
            _menuLifecycle.Hide();
            if (releaseCachedAssets)
            {
                _menuVisualsTracked = false;
                Resources.UnloadUnusedAssets();
                GC.Collect();
            }
        }

        private static void ReleaseThemeTextureSet(ref ThemeTextureSet theme)
        {
            if (theme == null)
            {
                return;
            }

            DestroyTexture(ref theme.Window);
            DestroyTexture(ref theme.Header);
            DestroyTexture(ref theme.Panel);
            DestroyTexture(ref theme.Tab);
            DestroyTexture(ref theme.ActiveTab);
            DestroyTexture(ref theme.Button);
            DestroyTexture(ref theme.SwitchOn);
            DestroyTexture(ref theme.SwitchOff);
            DestroyTexture(ref theme.ResizeHandle);
            theme = null;
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(texture);
            texture = null;
        }

        private void DrawResizeHandle()
        {
            Rect handleRect = new Rect(_windowRect.width - 26f, _windowRect.height - 26f, 18f, 18f);
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown && handleRect.Contains(currentEvent.mousePosition))
            {
                _isResizingWindow = true;
                _resizeStartRect = _windowRect;
                _resizeStartMouse = currentEvent.mousePosition;
                currentEvent.Use();
            }
            else if (_isResizingWindow && currentEvent.type == EventType.MouseDrag)
            {
                Vector2 delta = currentEvent.mousePosition - _resizeStartMouse;
                float targetWidth = Mathf.Max(_resizeStartRect.width + delta.x, _resizeStartRect.width + delta.y * _menuAspectRatio);
                float maxWidthByScreen = Mathf.Max(560f, Screen.width - _resizeStartRect.x - 12f);
                float maxWidthByHeight = Mathf.Max(560f, (Screen.height - _resizeStartRect.y - 12f) * _menuAspectRatio);
                float maxWidth = Mathf.Min(980f, maxWidthByScreen, maxWidthByHeight);
                targetWidth = Mathf.Clamp(targetWidth, 560f, maxWidth);

                _windowRect.width = targetWidth;
                _windowRect.height = targetWidth / _menuAspectRatio;
                currentEvent.Use();
            }
            else if (_isResizingWindow && (currentEvent.type == EventType.MouseUp || currentEvent.rawType == EventType.MouseUp))
            {
                _isResizingWindow = false;
                currentEvent.Use();
            }

            GUI.Label(handleRect, GUIContent.none, _resizeHandleStyle);
        }

        private void DrawMenuStartupAnimation()
        {
            switch (_activeStartupAnimationStyle)
            {
                case StartupAnimationStyle.ObsidianPulse:
                    DrawObsidianPulseStartupAnimation();
                    break;
                case StartupAnimationStyle.PlumBlossomBloom:
                    DrawPlumBlossomStartupAnimation();
                    break;
                case StartupAnimationStyle.SakuraDrift:
                    DrawSakuraStartupAnimation();
                    break;
                default:
                    DrawHolographicScanStartupAnimation();
                    break;
            }
        }

        private void DrawHolographicScanStartupAnimation()
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - _menuAnimationStartTime) / MenuAnimationDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            float time = Time.unscaledTime - _menuAnimationStartTime;
            float pulse = 1f + Mathf.Sin(time * 8.4f) * 0.018f;

            Color overlayColor = _styleDarkMode
                ? new Color(0.03f, 0.05f, 0.07f, 0.28f)
                : new Color(0.78f, 0.84f, 0.90f, 0.18f);
            Color accentColor = _styleDarkMode
                ? new Color(0.38f, 0.92f, 0.92f, 0.96f)
                : new Color(0.10f, 0.64f, 0.88f, 0.92f);
            Color secondaryAccent = _styleDarkMode
                ? new Color(0.86f, 1f, 0.96f, 0.95f)
                : new Color(0.90f, 0.98f, 1f, 0.95f);
            Color mutedAccent = _styleDarkMode
                ? new Color(0.63f, 0.83f, 0.85f, 0.82f)
                : new Color(0.28f, 0.49f, 0.61f, 0.74f);

            Color previousColor = GUI.color;
            GUI.color = overlayColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _whiteTexture);

            Rect cardRect = GetStartupAnimationRect(eased, pulse);
            float height = cardRect.height;

            GUI.color = Color.white;
            GUI.DrawTexture(cardRect, _headerTexture);

            Rect innerGlowRect = new Rect(cardRect.x + 10f, cardRect.y + 10f, cardRect.width - 20f, cardRect.height - 20f);
            GUI.color = _styleDarkMode ? new Color(0.18f, 0.26f, 0.34f, 0.16f) : new Color(1f, 1f, 1f, 0.16f);
            GUI.DrawTexture(innerGlowRect, _panelTexture);

            Vector2 iconCenter = new Vector2(cardRect.x + 76f, cardRect.center.y);
            float coreSize = Mathf.Lerp(30f, 40f, eased) * (1f + Mathf.Sin(time * 10f) * 0.04f);
            float waveBase = Mathf.Lerp(42f, 60f, eased);
            Rect beamRect = new Rect(iconCenter.x - 6f, cardRect.y + 20f, 12f, cardRect.height - 40f);

            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.14f + eased * 0.10f);
            GUI.DrawTexture(beamRect, _whiteTexture);

            for (int i = 0; i < 3; i++)
            {
                float waveProgress = Mathf.Repeat(progress * 1.15f + i * 0.24f, 1f);
                float waveSize = waveBase + waveProgress * 28f;
                float waveAlpha = (1f - waveProgress) * 0.30f;
                DrawCenteredTexture(iconCenter, waveSize, _startupDiamondTexture, new Color(accentColor.r, accentColor.g, accentColor.b, waveAlpha));
            }

            float scanPhase = Mathf.Repeat(time * 1.55f, 1f);
            float scanY = Mathf.Lerp(cardRect.y + 28f, cardRect.yMax - 28f, scanPhase);
            Rect scanRect = new Rect(iconCenter.x - 34f, scanY - 2f, 68f, 4f);
            GUI.color = new Color(secondaryAccent.r, secondaryAccent.g, secondaryAccent.b, 0.80f);
            GUI.DrawTexture(scanRect, _whiteTexture);

            DrawCenteredTexture(iconCenter, coreSize * 1.8f, _startupGlowTexture, new Color(accentColor.r, accentColor.g, accentColor.b, 0.22f + eased * 0.16f));
            DrawCenteredTexture(iconCenter, coreSize * 1.12f, _startupDiamondTexture, new Color(secondaryAccent.r, secondaryAccent.g, secondaryAccent.b, 0.84f));
            DrawCenteredTexture(iconCenter, coreSize * 0.72f, _startupGlowTexture, new Color(secondaryAccent.r, secondaryAccent.g, secondaryAccent.b, 0.92f));

            float chevronOffset = 28f + Mathf.Sin(time * 6.2f) * 6f;
            Vector2 leftChevron = new Vector2(iconCenter.x - chevronOffset, iconCenter.y);
            Vector2 rightChevron = new Vector2(iconCenter.x + chevronOffset, iconCenter.y);
            DrawRotatedTexture(leftChevron, new Vector2(16f, 16f), 180f, _startupChevronTexture, new Color(accentColor.r, accentColor.g, accentColor.b, 0.75f));
            DrawRotatedTexture(rightChevron, new Vector2(16f, 16f), 0f, _startupChevronTexture, new Color(accentColor.r, accentColor.g, accentColor.b, 0.75f));

            Rect titleRect = new Rect(cardRect.x + 132f, cardRect.y + 18f, cardRect.width - 166f, 34f);
            Rect subtitleRect = new Rect(cardRect.x + 132f, cardRect.y + 54f, cardRect.width - 166f, 26f);
            Rect percentRect = new Rect(cardRect.x + cardRect.width - 88f, cardRect.y + 18f, 70f, 28f);
            Rect statusTextRect = new Rect(cardRect.x + 148f, cardRect.y + height - 50f, cardRect.width - 182f, 24f);

            DrawStartupTextBackdrop(new Rect(titleRect.x - 4f, titleRect.y - 2f, titleRect.width, 30f), new Color(0.05f, 0.08f, 0.11f, _styleDarkMode ? 0.34f : 0.12f));
            DrawStartupTextBackdrop(new Rect(subtitleRect.x - 4f, subtitleRect.y - 1f, subtitleRect.width, 22f), new Color(0.05f, 0.08f, 0.11f, _styleDarkMode ? 0.24f : 0.08f));

            GUI.color = Color.white;
            GUI.Label(titleRect, "REPO Cheat Menu By ASwave", _headerStyle);
            GUI.Label(subtitleRect, "系统正在校准界面布局与功能模块...", _hintStyle);

            GUI.color = secondaryAccent;
            DrawStartupTextBackdrop(new Rect(percentRect.x - 2f, percentRect.y - 1f, percentRect.width, 22f), new Color(0.03f, 0.07f, 0.10f, _styleDarkMode ? 0.34f : 0.10f));
            GUI.Label(percentRect, $"{Mathf.RoundToInt(progress * 100f)}%", _titleStyle);

            float segmentBaseX = cardRect.x + 138f;
            float segmentY = cardRect.y + height - 26f;
            for (int i = 0; i < 4; i++)
            {
                float segmentPulse = Mathf.Clamp01(0.18f + 0.82f * (0.5f + 0.5f * Mathf.Sin(time * 8f - i * 0.42f)));
                GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, segmentPulse);
                GUI.DrawTexture(new Rect(segmentBaseX + i * 18f, segmentY, 12f, 4f), _whiteTexture);
            }

            GUI.color = mutedAccent;
            DrawStartupTextBackdrop(new Rect(statusTextRect.x - 4f, statusTextRect.y - 1f, statusTextRect.width, 22f), new Color(0.04f, 0.07f, 0.10f, _styleDarkMode ? 0.28f : 0.08f));
            GUI.Label(statusTextRect, "Synchronizing interface matrix...", _hintStyle);
            GUI.color = previousColor;
        }

        private void DrawObsidianPulseStartupAnimation()
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - _menuAnimationStartTime) / MenuAnimationDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            float time = Time.unscaledTime - _menuAnimationStartTime;
            float pulse = 1f + Mathf.Sin(time * 4.8f) * 0.010f;

            Color overlayColor = new Color(0.01f, 0.01f, 0.04f, 0.58f);
            Color stageColor = new Color(0.06f, 0.04f, 0.10f, 0.94f);
            Color accentColor = new Color(0.83f, 0.38f, 1f, 0.96f);
            Color secondaryAccent = new Color(0.36f, 0.78f, 1f, 0.96f);
            Color tertiaryAccent = new Color(1f, 0.63f, 0.86f, 0.92f);
            Color darkCoreColor = new Color(0.04f, 0.03f, 0.08f, 0.98f);

            Color previousColor = GUI.color;
            GUI.color = overlayColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _whiteTexture);

            Rect stageRect = GetStartupAnimationRect(eased, pulse);

            Rect innerStage = new Rect(stageRect.x + 18f, stageRect.y + 18f, stageRect.width - 36f, stageRect.height - 36f);
            GUI.color = stageColor;
            GUI.DrawTexture(stageRect, _windowTexture);

            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.12f);
            GUI.DrawTexture(new Rect(stageRect.x + 12f, stageRect.y + 12f, stageRect.width - 24f, 1f), _whiteTexture);
            GUI.DrawTexture(new Rect(stageRect.x + 12f, stageRect.yMax - 13f, stageRect.width - 24f, 1f), _whiteTexture);

            Vector2 portalCenter = new Vector2(stageRect.center.x, stageRect.center.y + 10f);
            float portalSize = Mathf.Lerp(102f, 142f, eased);
            float outerSize = portalSize * 1.18f;
            float innerSize = portalSize * 0.82f;

            DrawCenteredTexture(portalCenter, outerSize * 1.12f, _startupGlowTexture, new Color(accentColor.r, accentColor.g, accentColor.b, 0.10f + eased * 0.08f));
            DrawRotatedTexture(portalCenter, new Vector2(outerSize, outerSize), time * 48f, _startupRingTexture, new Color(accentColor.r, accentColor.g, accentColor.b, 0.58f));
            DrawRotatedTexture(portalCenter, new Vector2(innerSize, innerSize), -time * 74f, _startupRingTexture, new Color(secondaryAccent.r, secondaryAccent.g, secondaryAccent.b, 0.46f));
            DrawCenteredTexture(portalCenter, portalSize * 0.58f, _startupGlowTexture, darkCoreColor);

            for (int i = 0; i < 2; i++)
            {
                float bracketDistance = portalSize * 0.78f + i * 10f;
                float bracketSize = 34f + i * 8f;
                DrawRotatedTexture(new Vector2(portalCenter.x - bracketDistance, portalCenter.y), new Vector2(bracketSize, bracketSize), 0f, _startupBracketTexture, new Color(tertiaryAccent.r, tertiaryAccent.g, tertiaryAccent.b, 0.74f - i * 0.18f));
                DrawRotatedTexture(new Vector2(portalCenter.x + bracketDistance, portalCenter.y), new Vector2(bracketSize, bracketSize), 180f, _startupBracketTexture, new Color(tertiaryAccent.r, tertiaryAccent.g, tertiaryAccent.b, 0.74f - i * 0.18f));
            }

            for (int i = 0; i < 6; i++)
            {
                float orbitAngle = time * 1.4f + i * (Mathf.PI * 2f / 6f);
                Vector2 nodeCenter = portalCenter + new Vector2(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle)) * (portalSize * 0.46f);
                float nodeSize = 6f + Mathf.Abs(Mathf.Sin(time * 7f + i)) * 2.4f;
                Color nodeColor = i % 2 == 0
                    ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.90f)
                    : new Color(secondaryAccent.r, secondaryAccent.g, secondaryAccent.b, 0.90f);
                DrawCenteredTexture(nodeCenter, nodeSize, _switchKnobTexture, nodeColor);
            }

            for (int i = 0; i < 12; i++)
            {
                float beamProgress = Mathf.Repeat(progress * 1.15f + i * 0.08f, 1f);
                float beamHeight = Mathf.Lerp(14f, 48f, 1f - beamProgress);
                float beamAlpha = (1f - beamProgress) * 0.22f;
                float beamX = innerStage.x + i * ((innerStage.width - 12f) / 11f);
                GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, beamAlpha);
                GUI.DrawTexture(new Rect(beamX, stageRect.yMax - 30f - beamHeight, 2f, beamHeight), _whiteTexture);
            }

            Rect titleRect = new Rect(stageRect.x + 28f, stageRect.y + 18f, stageRect.width - 56f, 30f);
            Rect subtitleRect = new Rect(stageRect.x + 42f, stageRect.y + 50f, stageRect.width - 84f, 22f);
            Rect percentRect = new Rect(stageRect.center.x - 42f, stageRect.yMax - 108f, 84f, 24f);
            Rect statusRect = new Rect(stageRect.x + 34f, stageRect.yMax - 34f, stageRect.width - 68f, 20f);

            DrawStartupTextBackdrop(new Rect(titleRect.x + 18f, titleRect.y - 1f, titleRect.width - 36f, 24f), new Color(0.03f, 0.02f, 0.07f, 0.48f));
            DrawStartupTextBackdrop(new Rect(subtitleRect.x + 14f, subtitleRect.y - 1f, subtitleRect.width - 28f, 20f), new Color(0.03f, 0.02f, 0.07f, 0.34f));
            DrawStartupTextBackdrop(new Rect(percentRect.x + 8f, percentRect.y - 1f, percentRect.width - 16f, 20f), new Color(0.03f, 0.02f, 0.08f, 0.42f));

            GUI.color = Color.white;
            GUI.Label(titleRect, "REPO Cheat Menu By ASwave", _startupDarkHeaderStyle);
            GUI.Label(subtitleRect, "高对比界面层与核心动画序列正在对齐", _startupDarkHintStyle);
            GUI.Label(percentRect, $"SYNC {Mathf.RoundToInt(progress * 100f)}%", _startupDarkValueStyle);

            float waveformWidth = 112f;
            float waveformX = stageRect.center.x - waveformWidth * 0.5f;
            float waveformY = stageRect.yMax - 56f;
            for (int i = 0; i < 14; i++)
            {
                float barHeight = 4f + Mathf.Abs(Mathf.Sin(time * 7.5f + i * 0.55f)) * 10f * Mathf.Clamp01(0.35f + progress);
                GUI.color = i % 2 == 0
                    ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.58f)
                    : new Color(secondaryAccent.r, secondaryAccent.g, secondaryAccent.b, 0.54f);
                GUI.DrawTexture(new Rect(waveformX + i * 8f, waveformY - barHeight, 4f, barHeight), _whiteTexture);
            }

            DrawStartupTextBackdrop(new Rect(statusRect.x + 42f, statusRect.y - 1f, statusRect.width - 84f, 18f), new Color(0.03f, 0.02f, 0.08f, 0.44f));
            GUI.Label(statusRect, "High-contrast composition aligning render cadence", _startupDarkHintStyle);
            GUI.color = previousColor;
        }

        private void DrawPlumBlossomStartupAnimation()
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - _menuAnimationStartTime) / MenuAnimationDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            float time = Time.unscaledTime - _menuAnimationStartTime;
            float pulse = 1f + Mathf.Sin(time * 5.4f) * 0.014f;
            float blossomBreath = 1f + Mathf.Sin(time * 3.6f) * 0.018f;

            Color overlayColor = new Color(0.20f, 0.08f, 0.14f, 0.28f);
            Color stageTint = new Color(0.33f, 0.10f, 0.21f, 0.82f);
            Color softRose = new Color(1f, 228f / 255f, 225f / 255f, 0.96f);
            Color plumPink = new Color(1f, 182f / 255f, 193f / 255f, 0.94f);
            Color blossomWhite = new Color(1f, 236f / 255f, 232f / 255f, 0.98f);
            Color goldAccent = new Color(1f, 0.88f, 0.50f, 0.94f);
            Color branchAccent = new Color(0.75f, 0.45f, 0.62f, 0.60f);
            Color deepBranch = new Color(0.44f, 0.18f, 0.27f, 0.86f);

            Color previousColor = GUI.color;
            GUI.color = overlayColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _whiteTexture);

            Rect stageRect = GetStartupAnimationRect(eased, pulse);
            Rect innerStage = new Rect(stageRect.x + 12f, stageRect.y + 12f, stageRect.width - 24f, stageRect.height - 24f);

            GUI.color = Color.white;
            GUI.DrawTexture(stageRect, _headerTexture);
            GUI.color = new Color(stageTint.r, stageTint.g, stageTint.b, stageTint.a);
            GUI.DrawTexture(innerStage, _panelTexture);

            GUI.color = new Color(1f, 0.92f, 0.97f, 0.08f + eased * 0.05f);
            GUI.DrawTexture(new Rect(stageRect.x + 22f, stageRect.y + 18f, stageRect.width - 44f, 18f), _whiteTexture);

            GUI.color = new Color(blossomWhite.r, blossomWhite.g, blossomWhite.b, 0.12f);
            GUI.DrawTexture(new Rect(stageRect.x + 18f, stageRect.y + 16f, stageRect.width - 36f, 1f), _whiteTexture);
            GUI.DrawTexture(new Rect(stageRect.x + 18f, stageRect.yMax - 17f, stageRect.width - 36f, 1f), _whiteTexture);

            Rect mistRect = new Rect(stageRect.x + 18f, stageRect.center.y - 36f, stageRect.width - 36f, 72f);
            GUI.color = new Color(1f, 0.90f, 0.95f, 0.06f + eased * 0.04f);
            GUI.DrawTexture(mistRect, _whiteTexture);
            GUI.color = new Color(0.98f, 0.80f, 0.90f, 0.05f + Mathf.Sin(time * 1.8f) * 0.01f);
            GUI.DrawTexture(new Rect(stageRect.x + 26f, stageRect.y + 34f, stageRect.width - 52f, stageRect.height - 68f), _whiteTexture);

            Vector2 blossomCenter = new Vector2(stageRect.x + 118f, stageRect.center.y + 4f);
            float blossomScale = Mathf.Lerp(76f, 90f, eased) * blossomBreath;
            float petalDistance = Mathf.Lerp(18f, 24f, eased);
            float petalSway = Mathf.Sin(time * 4.8f) * 2.2f;
            Vector2 branchOrigin = new Vector2(stageRect.x + 62f, stageRect.yMax - 40f);
            Vector2 branchUpper = blossomCenter + new Vector2(16f, 28f);
            Vector2 branchLower = blossomCenter + new Vector2(56f, -16f);

            DrawCenteredTexture(blossomCenter, blossomScale * 2.0f, _startupGlowTexture, new Color(plumPink.r, plumPink.g, plumPink.b, 0.18f + eased * 0.10f));
            DrawCenteredTexture(blossomCenter + new Vector2(4f, -2f), blossomScale * 1.32f, _startupGlowTexture, new Color(1f, 0.97f, 0.99f, 0.15f + eased * 0.06f));

            GUI.color = deepBranch;
            DrawRotatedTexture((branchOrigin + blossomCenter) * 0.5f + new Vector2(-6f, 0f), new Vector2(Vector2.Distance(branchOrigin, blossomCenter) + 24f, 6f), -34f, _whiteTexture, deepBranch);
            DrawRotatedTexture((branchUpper + blossomCenter) * 0.5f, new Vector2(Vector2.Distance(branchUpper, blossomCenter) + 12f, 3f), 34f, _whiteTexture, branchAccent);
            DrawRotatedTexture((branchLower + blossomCenter) * 0.5f, new Vector2(Vector2.Distance(branchLower, blossomCenter) + 10f, 3f), -18f, _whiteTexture, branchAccent);
            DrawRotatedTexture(new Vector2(branchOrigin.x + 42f, branchOrigin.y - 18f), new Vector2(96f, 2f), -12f, _whiteTexture, new Color(branchAccent.r, branchAccent.g, branchAccent.b, 0.42f));
            DrawRotatedTexture(new Vector2(branchOrigin.x + 28f, branchOrigin.y - 38f), new Vector2(64f, 2f), -48f, _whiteTexture, new Color(branchAccent.r, branchAccent.g, branchAccent.b, 0.32f));

            for (int i = 0; i < 5; i++)
            {
                float angle = -90f + i * 72f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 backOffset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (petalDistance + 6f + Mathf.Sin(time * 2.1f + i * 0.6f) * 1.5f);
                Vector2 backCenter = blossomCenter + backOffset + new Vector2(Mathf.Sin(i * 1.7f) * 2.2f, Mathf.Cos(i * 1.9f) * 2.6f);
                DrawRotatedTexture(backCenter, new Vector2(blossomScale * 0.70f, blossomScale * 0.88f), angle + 90f - 8f, _startupPetalTexture, new Color(plumPink.r, plumPink.g, plumPink.b, 0.22f));
            }

            for (int i = 0; i < 5; i++)
            {
                float angle = -90f + i * 72f;
                float radians = angle * Mathf.Deg2Rad;
                float distanceVariance = Mathf.Sin(i * 1.37f + 0.25f) * 2.8f;
                Vector2 petalOffset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (petalDistance + distanceVariance + Mathf.Sin(time * 3.4f + i) * 1.6f);
                petalOffset += new Vector2(Mathf.Sin(i * 1.9f) * 2.6f, Mathf.Cos(i * 1.3f) * 2.1f);
                Vector2 petalCenter = blossomCenter + petalOffset;
                float petalRotation = angle + 90f + Mathf.Sin(time * 4.2f + i * 0.7f) * 7f + petalSway + Mathf.Cos(i * 1.2f) * 4f;
                float widthScale = 0.74f + Mathf.Sin(i * 2.2f + 0.4f) * 0.06f;
                float heightScale = 0.94f + Mathf.Cos(i * 1.6f + 0.2f) * 0.07f;
                Vector2 petalSize = new Vector2(blossomScale * widthScale, blossomScale * heightScale);
                Color petalColor = i % 2 == 0
                    ? new Color(softRose.r, softRose.g, softRose.b, 0.94f)
                    : new Color(plumPink.r, plumPink.g, plumPink.b, 0.92f);
                DrawRotatedTexture(petalCenter, petalSize, petalRotation, _startupPetalTexture, petalColor);
            }

            DrawCenteredTexture(blossomCenter, blossomScale * 0.46f, _startupGlowTexture, new Color(goldAccent.r, goldAccent.g, goldAccent.b, 0.46f));
            DrawCenteredTexture(blossomCenter, blossomScale * 0.22f, _switchKnobTexture, new Color(1f, 0.88f, 0.56f, 0.98f));

            Vector2 blossomSecondary = blossomCenter + new Vector2(-42f, 48f);
            Vector2 blossomTertiary = blossomCenter + new Vector2(58f, -44f);
            float sideScale = blossomScale * 0.40f;
            DrawCenteredTexture(blossomSecondary, sideScale * 1.6f, _startupGlowTexture, new Color(plumPink.r, plumPink.g, plumPink.b, 0.14f));
            DrawCenteredTexture(blossomTertiary, sideScale * 1.4f, _startupGlowTexture, new Color(softRose.r, softRose.g, softRose.b, 0.10f));
            for (int i = 0; i < 5; i++)
            {
                float angle = -90f + i * 72f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (sideScale * 0.16f);
                DrawRotatedTexture(blossomSecondary + offset, new Vector2(sideScale * 0.70f, sideScale * 0.86f), angle + 90f, _startupPetalTexture, new Color(softRose.r, softRose.g, softRose.b, 0.78f));
                DrawRotatedTexture(blossomTertiary + offset * 0.9f, new Vector2(sideScale * 0.58f, sideScale * 0.74f), angle + 90f + 8f, _startupPetalTexture, new Color(plumPink.r, plumPink.g, plumPink.b, 0.72f));
            }
            DrawCenteredTexture(blossomSecondary, sideScale * 0.16f, _switchKnobTexture, new Color(1f, 0.87f, 0.55f, 0.86f));
            DrawCenteredTexture(blossomTertiary, sideScale * 0.14f, _switchKnobTexture, new Color(1f, 0.87f, 0.55f, 0.78f));

            float orbitRadius = blossomScale * 0.82f;
            for (int i = 0; i < 10; i++)
            {
                float orbitAngle = time * 120f + i * 36f;
                float radians = orbitAngle * Mathf.Deg2Rad;
                Vector2 orbitCenter = blossomCenter + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * orbitRadius;
                float intensity = Mathf.Repeat(progress * 1.8f + i * 0.10f, 1f);
                float alpha = 0.18f + (1f - intensity) * 0.55f;
                Vector2 orbitPetalSize = new Vector2(14f, 18f) * (0.86f + (1f - intensity) * 0.24f);
                DrawRotatedTexture(orbitCenter, orbitPetalSize, orbitAngle + 90f, _startupPetalTexture, new Color(plumPink.r, plumPink.g, plumPink.b, alpha));
            }

            for (int i = 0; i < 8; i++)
            {
                float orbitAngle = -time * 92f + i * 45f;
                float radians = orbitAngle * Mathf.Deg2Rad;
                Vector2 orbitCenter = blossomCenter + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (orbitRadius * 0.62f);
                float sparkle = 0.22f + 0.30f * (0.5f + 0.5f * Mathf.Sin(time * 6.4f + i));
                DrawCenteredTexture(orbitCenter, 4f + sparkle * 4f, _switchKnobTexture, new Color(blossomWhite.r, blossomWhite.g, blossomWhite.b, sparkle));
            }

            for (int i = 0; i < 3; i++)
            {
                float ringProgress = Mathf.Repeat(progress * 1.05f + i * 0.24f, 1f);
                float ringSize = blossomScale * (1.32f + ringProgress * 0.42f);
                float ringAlpha = (1f - ringProgress) * 0.18f;
                DrawCenteredTexture(blossomCenter, ringSize, _startupRingTexture, new Color(softRose.r, softRose.g, softRose.b, ringAlpha));
            }

            for (int i = 0; i < 12; i++)
            {
                float fallSeed = i * 0.17f;
                float fallT = Mathf.Repeat(progress * 1.18f + fallSeed + time * (0.10f + i * 0.01f), 1f);
                float fallX = Mathf.Lerp(stageRect.x + stageRect.width * 0.52f, stageRect.x + stageRect.width - 34f, Mathf.Repeat(i * 0.13f + 0.18f, 1f));
                float sway = Mathf.Sin(time * (2.4f + i * 0.18f) + i) * (9f + i * 0.4f);
                float fallY = Mathf.Lerp(stageRect.y + 22f, stageRect.yMax - 26f, fallT);
                float petalAngle = Mathf.Repeat(time * (54f + i * 8f) + i * 24f, 360f);
                float petalSize = 8f + (i % 3) * 2.2f + (1f - fallT) * 2f;
                Color fallColor = i % 2 == 0
                    ? new Color(plumPink.r, plumPink.g, plumPink.b, 0.18f + (1f - fallT) * 0.30f)
                    : new Color(softRose.r, softRose.g, softRose.b, 0.14f + (1f - fallT) * 0.28f);
                DrawRotatedTexture(new Vector2(fallX + sway, fallY), new Vector2(petalSize, petalSize * 1.26f), petalAngle, _startupPetalTexture, fallColor);
            }

            float stemX = blossomCenter.x + 72f;
            GUI.color = branchAccent;
            GUI.DrawTexture(new Rect(stemX, stageRect.y + 30f, 2f, stageRect.height - 60f), _whiteTexture);
            GUI.DrawTexture(new Rect(stemX - 14f, blossomCenter.y + 8f, 28f, 2f), _whiteTexture);
            GUI.DrawTexture(new Rect(stageRect.x + 196f, stageRect.yMax - 34f, stageRect.width - 232f, 1f), _whiteTexture);

            Rect titleRect = new Rect(stageRect.x + 206f, stageRect.y + 20f, stageRect.width - 240f, 30f);
            Rect subtitleRect = new Rect(stageRect.x + 206f, stageRect.y + 52f, stageRect.width - 240f, 24f);
            Rect percentRect = new Rect(stageRect.x + stageRect.width - 98f, stageRect.y + 18f, 76f, 26f);
            Rect statusRect = new Rect(stageRect.x + 206f, stageRect.yMax - 52f, stageRect.width - 244f, 22f);
            Rect progressBarRect = new Rect(stageRect.x + 208f, stageRect.yMax - 32f, stageRect.width - 300f, 6f);

            DrawStartupTextBackdrop(new Rect(titleRect.x - 4f, titleRect.y - 1f, titleRect.width, 24f), new Color(1f, 0.95f, 0.97f, 0.30f));
            DrawStartupTextBackdrop(new Rect(subtitleRect.x - 4f, subtitleRect.y - 1f, titleRect.width, 20f), new Color(1f, 0.95f, 0.97f, 0.20f));
            DrawStartupTextBackdrop(new Rect(percentRect.x - 2f, percentRect.y - 1f, percentRect.width, 22f), new Color(1f, 0.95f, 0.97f, 0.26f));

            GUI.color = new Color(1f, 0.90f, 0.95f, 0.14f);
            GUI.DrawTexture(progressBarRect, _whiteTexture);
            GUI.color = new Color(plumPink.r, plumPink.g, plumPink.b, 0.70f);
            GUI.DrawTexture(new Rect(progressBarRect.x, progressBarRect.y, progressBarRect.width * progress, progressBarRect.height), _whiteTexture);
            GUI.color = new Color(blossomWhite.r, blossomWhite.g, blossomWhite.b, 0.34f);
            GUI.DrawTexture(new Rect(progressBarRect.x + Mathf.Max(0f, progressBarRect.width * progress - 12f), progressBarRect.y - 1f, 12f, progressBarRect.height + 2f), _whiteTexture);

            float petalBarX = stageRect.x + 214f;
            float petalBarY = stageRect.yMax - 26f;
            for (int i = 0; i < 5; i++)
            {
                float bloom = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(time * 5.2f - i * 0.65f));
                Color barColor = i % 2 == 0
                    ? new Color(plumPink.r, plumPink.g, plumPink.b, bloom)
                    : new Color(softRose.r, softRose.g, softRose.b, bloom);
                DrawRotatedTexture(new Vector2(petalBarX + i * 18f, petalBarY), new Vector2(11f, 14f), i * 10f, _startupPetalTexture, barColor);
            }

            float rightAccentX = stageRect.x + stageRect.width - 94f;
            GUI.color = new Color(branchAccent.r, branchAccent.g, branchAccent.b, 0.34f);
            DrawRotatedTexture(new Vector2(rightAccentX, stageRect.center.y + 10f), new Vector2(92f, 2f), 76f, _whiteTexture, new Color(branchAccent.r, branchAccent.g, branchAccent.b, 0.34f));
            DrawRotatedTexture(new Vector2(rightAccentX - 16f, stageRect.center.y - 24f), new Vector2(48f, 2f), 118f, _whiteTexture, new Color(branchAccent.r, branchAccent.g, branchAccent.b, 0.24f));
            for (int i = 0; i < 3; i++)
            {
                Vector2 sideBloomCenter = new Vector2(rightAccentX + (i - 1) * 14f, stageRect.y + 52f + i * 34f);
                float sideBloomScale = 18f + i * 2f;
                for (int p = 0; p < 5; p++)
                {
                    float angle = -90f + p * 72f;
                    float radians = angle * Mathf.Deg2Rad;
                    Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (sideBloomScale * 0.18f);
                    DrawRotatedTexture(sideBloomCenter + offset, new Vector2(sideBloomScale * 0.76f, sideBloomScale * 0.94f), angle + 90f + i * 6f, _startupPetalTexture, new Color(blossomWhite.r, blossomWhite.g, blossomWhite.b, 0.58f - i * 0.08f));
                }
                DrawCenteredTexture(sideBloomCenter, sideBloomScale * 0.14f, _switchKnobTexture, new Color(1f, 0.87f, 0.55f, 0.62f));
            }
            for (int i = 0; i < 5; i++)
            {
                float driftY = Mathf.Lerp(stageRect.y + 26f, stageRect.yMax - 28f, Mathf.Repeat(progress * 0.9f + i * 0.18f, 1f));
                float driftX = stageRect.x + stageRect.width - 42f + Mathf.Sin(time * (1.8f + i * 0.15f) + i) * 10f;
                DrawRotatedTexture(new Vector2(driftX, driftY), new Vector2(8f + i, 10f + i), time * (28f + i * 6f), _startupPetalTexture, new Color(plumPink.r, plumPink.g, plumPink.b, 0.18f + i * 0.04f));
            }

            DrawStartupTextBackdrop(new Rect(statusRect.x - 4f, statusRect.y - 1f, statusRect.width, 18f), new Color(1f, 0.95f, 0.97f, 0.28f));
            GUI.Label(titleRect, "REPO Cheat Menu By ASwave", _startupLightHeaderStyle);
            GUI.Label(subtitleRect, "卷轴、墨韵与留白构图正在缓慢展开", _startupLightHintStyle);
            GUI.Label(percentRect, $"{Mathf.RoundToInt(progress * 100f)}%", _startupLightValueStyle);
            GUI.Label(statusRect, "Diagonal composition calibrating motion layers", _startupLightHintStyle);
            GUI.color = previousColor;
        }

        private void DrawSakuraStartupAnimation()
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - _menuAnimationStartTime) / MenuAnimationDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            float time = Time.unscaledTime - _menuAnimationStartTime;
            float pulse = 1f + Mathf.Sin(time * 4.6f) * 0.012f;

            Color overlayColor = new Color(0.25f, 0.12f, 0.18f, 0.24f);
            Color stageTint = new Color(0.92f, 0.68f, 0.78f, 0.26f);
            Color sakuraPink = new Color(1f, 0.76f, 0.86f, 0.96f);
            Color sakuraWhite = new Color(1f, 0.97f, 0.99f, 0.98f);
            Color roseMist = new Color(1f, 0.88f, 0.93f, 0.92f);
            Color branchColor = new Color(0.56f, 0.28f, 0.34f, 0.74f);
            Color goldCore = new Color(1f, 0.86f, 0.52f, 0.96f);

            Color previousColor = GUI.color;
            GUI.color = overlayColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _whiteTexture);

            Rect stageRect = GetStartupAnimationRect(eased, pulse);
            Rect innerStage = new Rect(stageRect.x + 12f, stageRect.y + 12f, stageRect.width - 24f, stageRect.height - 24f);

            GUI.color = Color.white;
            GUI.DrawTexture(stageRect, _headerTexture);
            GUI.color = new Color(stageTint.r, stageTint.g, stageTint.b, stageTint.a);
            GUI.DrawTexture(innerStage, _panelTexture);

            GUI.color = new Color(1f, 0.95f, 0.97f, 0.16f);
            GUI.DrawTexture(new Rect(stageRect.x + 20f, stageRect.y + 18f, stageRect.width - 40f, 20f), _whiteTexture);
            GUI.color = new Color(1f, 0.90f, 0.94f, 0.09f + eased * 0.04f);
            GUI.DrawTexture(new Rect(stageRect.x + 22f, stageRect.y + 42f, stageRect.width - 44f, stageRect.height - 84f), _whiteTexture);

            Vector2 blossomCenter = new Vector2(stageRect.x + 124f, stageRect.center.y + 4f);
            float blossomScale = Mathf.Lerp(78f, 92f, eased) * (1f + Mathf.Sin(time * 3.8f) * 0.016f);
            float petalDistance = blossomScale * 0.24f;

            DrawCenteredTexture(blossomCenter, blossomScale * 2.2f, _startupGlowTexture, new Color(sakuraPink.r, sakuraPink.g, sakuraPink.b, 0.20f));
            DrawCenteredTexture(blossomCenter + new Vector2(4f, -4f), blossomScale * 1.34f, _startupGlowTexture, new Color(sakuraWhite.r, sakuraWhite.g, sakuraWhite.b, 0.18f));

            DrawRotatedTexture(new Vector2(stageRect.x + 66f, stageRect.yMax - 40f), new Vector2(152f, 5f), -34f, _whiteTexture, branchColor);
            DrawRotatedTexture(new Vector2(stageRect.x + 112f, stageRect.center.y + 10f), new Vector2(94f, 3f), 28f, _whiteTexture, new Color(branchColor.r, branchColor.g, branchColor.b, 0.56f));
            DrawRotatedTexture(new Vector2(stageRect.x + 144f, stageRect.center.y - 26f), new Vector2(72f, 2f), -18f, _whiteTexture, new Color(branchColor.r, branchColor.g, branchColor.b, 0.44f));
            DrawRotatedTexture(new Vector2(stageRect.x + 86f, stageRect.center.y - 42f), new Vector2(52f, 2f), -54f, _whiteTexture, new Color(branchColor.r, branchColor.g, branchColor.b, 0.26f));

            for (int i = 0; i < 6; i++)
            {
                float angle = -90f + i * 60f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 hazeCenter = blossomCenter + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (blossomScale * 0.56f);
                DrawRotatedTexture(hazeCenter, new Vector2(blossomScale * 0.52f, blossomScale * 0.56f), angle + 90f, _startupSakuraPetalTexture, new Color(sakuraPink.r, sakuraPink.g, sakuraPink.b, 0.15f));
            }

            for (int i = 0; i < 5; i++)
            {
                float angle = -90f + i * 72f;
                float radians = angle * Mathf.Deg2Rad;
                float distanceVariance = Mathf.Cos(i * 1.55f + 0.4f) * 3.4f;
                Vector2 petalCenter = blossomCenter + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (petalDistance + distanceVariance + Mathf.Sin(time * 3.2f + i) * 1.8f);
                petalCenter += new Vector2(Mathf.Sin(i * 1.7f) * 2.8f, Mathf.Cos(i * 1.2f) * 2.0f);
                float petalRotation = angle + 90f + Mathf.Sin(time * 4.6f + i * 0.6f) * 8f + Mathf.Cos(i * 1.1f) * 5f;
                float widthScale = 0.80f + Mathf.Sin(i * 1.8f + 0.3f) * 0.07f;
                float heightScale = 0.88f + Mathf.Cos(i * 1.5f + 0.2f) * 0.06f;
                Vector2 petalSize = new Vector2(blossomScale * widthScale, blossomScale * heightScale);
                Color petalColor = i % 2 == 0
                    ? new Color(sakuraWhite.r, sakuraWhite.g, sakuraWhite.b, 0.98f)
                    : new Color(roseMist.r, roseMist.g, roseMist.b, 0.95f);
                DrawRotatedTexture(petalCenter, petalSize, petalRotation, _startupSakuraPetalTexture, petalColor);
                DrawRotatedTexture(petalCenter + new Vector2(-2f, -4f), petalSize * 0.76f, petalRotation, _startupSakuraPetalHighlightTexture, new Color(1f, 1f, 1f, 0.28f));
            }

            DrawCenteredTexture(blossomCenter, blossomScale * 0.40f, _startupGlowTexture, new Color(goldCore.r, goldCore.g, goldCore.b, 0.40f));
            DrawCenteredTexture(blossomCenter, blossomScale * 0.18f, _switchKnobTexture, new Color(1f, 0.89f, 0.56f, 0.96f));
            DrawSakuraStamenCluster(blossomCenter, blossomScale * 0.042f, 0.86f);

            Vector2 clusterA = blossomCenter + new Vector2(-48f, 44f);
            Vector2 clusterB = blossomCenter + new Vector2(62f, -40f);
            for (int c = 0; c < 2; c++)
            {
                Vector2 center = c == 0 ? clusterA : clusterB;
                float miniScale = c == 0 ? blossomScale * 0.34f : blossomScale * 0.30f;
                DrawCenteredTexture(center, miniScale * 1.5f, _startupGlowTexture, new Color(sakuraPink.r, sakuraPink.g, sakuraPink.b, 0.12f));
                for (int i = 0; i < 5; i++)
                {
                    float angle = -90f + i * 72f;
                    float radians = angle * Mathf.Deg2Rad;
                    Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (miniScale * 0.18f);
                    DrawRotatedTexture(center + offset, new Vector2(miniScale * 0.88f, miniScale * 0.94f), angle + 90f, _startupSakuraPetalTexture, new Color(sakuraWhite.r, sakuraWhite.g, sakuraWhite.b, 0.76f));
                }
                DrawCenteredTexture(center, miniScale * 0.16f, _switchKnobTexture, new Color(1f, 0.88f, 0.54f, 0.82f));
                DrawSakuraStamenCluster(center, miniScale * 0.04f, 0.72f);
            }

            float orbitRadius = blossomScale * 0.92f;
            for (int i = 0; i < 12; i++)
            {
                float orbitAngle = time * 82f + i * 30f;
                float radians = orbitAngle * Mathf.Deg2Rad;
                Vector2 orbitCenter = blossomCenter + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * orbitRadius;
                float orbitAlpha = 0.12f + 0.44f * (1f - Mathf.Repeat(progress * 1.15f + i * 0.07f, 1f));
                DrawRotatedTexture(orbitCenter, new Vector2(12f, 13f), orbitAngle + 90f, _startupSakuraPetalTexture, new Color(sakuraPink.r, sakuraPink.g, sakuraPink.b, orbitAlpha));
            }

            for (int i = 0; i < 18; i++)
            {
                float driftSeed = i * 0.11f;
                float driftT = Mathf.Repeat(progress * 1.10f + driftSeed + time * (0.08f + i * 0.004f), 1f);
                float driftX = Mathf.Lerp(stageRect.x + stageRect.width * 0.46f, stageRect.x + stageRect.width - 24f, Mathf.Repeat(i * 0.09f + 0.22f, 1f));
                float driftY = Mathf.Lerp(stageRect.y + 16f, stageRect.yMax - 18f, driftT);
                float sway = Mathf.Sin(time * (2.1f + i * 0.12f) + i * 0.8f) * (10f + i * 0.35f);
                float rotate = Mathf.Repeat(time * (38f + i * 4f) + i * 14f, 360f);
                float petalSize = 8f + (i % 4) * 1.8f;
                Color driftColor = i % 3 == 0
                    ? new Color(sakuraWhite.r, sakuraWhite.g, sakuraWhite.b, 0.18f + (1f - driftT) * 0.30f)
                    : new Color(sakuraPink.r, sakuraPink.g, sakuraPink.b, 0.16f + (1f - driftT) * 0.28f);
                DrawRotatedTexture(new Vector2(driftX + sway, driftY), new Vector2(petalSize, petalSize * 1.04f), rotate, _startupSakuraPetalTexture, driftColor);
            }

            Rect titleRect = new Rect(stageRect.x + 214f, stageRect.y + 18f, stageRect.width - 246f, 30f);
            Rect subtitleRect = new Rect(stageRect.x + 214f, stageRect.y + 50f, stageRect.width - 246f, 24f);
            Rect percentRect = new Rect(stageRect.x + stageRect.width - 98f, stageRect.y + 18f, 76f, 26f);
            Rect statusRect = new Rect(stageRect.x + 214f, stageRect.yMax - 52f, stageRect.width - 250f, 22f);
            Rect progressBarRect = new Rect(stageRect.x + 216f, stageRect.yMax - 31f, stageRect.width - 304f, 6f);

            float rightDecorX = stageRect.x + stageRect.width - 92f;
            GUI.color = new Color(branchColor.r, branchColor.g, branchColor.b, 0.24f);
            DrawRotatedTexture(new Vector2(rightDecorX, stageRect.center.y + 6f), new Vector2(104f, 2f), 74f, _whiteTexture, new Color(branchColor.r, branchColor.g, branchColor.b, 0.24f));
            DrawRotatedTexture(new Vector2(rightDecorX - 18f, stageRect.center.y - 22f), new Vector2(56f, 2f), 116f, _whiteTexture, new Color(branchColor.r, branchColor.g, branchColor.b, 0.18f));
            for (int i = 0; i < 4; i++)
            {
                Vector2 sideCenter = new Vector2(rightDecorX + (i % 2 == 0 ? -8f : 8f), stageRect.y + 54f + i * 28f);
                float sideScale = 16f + i * 1.8f;
                for (int p = 0; p < 5; p++)
                {
                    float angle = -90f + p * 72f;
                    float radians = angle * Mathf.Deg2Rad;
                    Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (sideScale * 0.18f);
                    DrawRotatedTexture(sideCenter + offset, new Vector2(sideScale * 0.82f, sideScale * 0.88f), angle + 90f + i * 4f, _startupSakuraPetalTexture, new Color(sakuraWhite.r, sakuraWhite.g, sakuraWhite.b, 0.54f - i * 0.06f));
                }
                DrawCenteredTexture(sideCenter, sideScale * 0.14f, _switchKnobTexture, new Color(1f, 0.88f, 0.54f, 0.54f));
            }
            for (int i = 0; i < 7; i++)
            {
                float driftY = Mathf.Lerp(stageRect.y + 24f, stageRect.yMax - 24f, Mathf.Repeat(progress * 0.92f + i * 0.13f, 1f));
                float driftX = stageRect.x + stageRect.width - 46f + Mathf.Sin(time * (1.7f + i * 0.12f) + i * 0.7f) * 11f;
                DrawRotatedTexture(new Vector2(driftX, driftY), new Vector2(8f + (i % 3), 9f + (i % 3)), time * (24f + i * 4f), _startupSakuraPetalTexture, new Color(sakuraPink.r, sakuraPink.g, sakuraPink.b, 0.16f + i * 0.03f));
            }

            DrawStartupTextBackdrop(new Rect(titleRect.x - 4f, titleRect.y - 1f, titleRect.width, 24f), new Color(1f, 0.95f, 0.97f, 0.26f));
            DrawStartupTextBackdrop(new Rect(subtitleRect.x - 4f, subtitleRect.y - 1f, subtitleRect.width, 20f), new Color(1f, 0.95f, 0.97f, 0.18f));
            DrawStartupTextBackdrop(new Rect(percentRect.x - 2f, percentRect.y - 1f, percentRect.width, 22f), new Color(1f, 0.95f, 0.97f, 0.24f));

            GUI.color = Color.white;
            GUI.Label(titleRect, "REPO Cheat Menu By ASwave", _startupLightHeaderStyle);
            GUI.Label(subtitleRect, "中心花序、卡片层叠与柔光正在入场", _startupLightHintStyle);
            GUI.Label(percentRect, $"{Mathf.RoundToInt(progress * 100f)}%", _startupLightValueStyle);

            GUI.color = new Color(1f, 0.92f, 0.95f, 0.18f);
            GUI.DrawTexture(progressBarRect, _whiteTexture);
            GUI.color = new Color(sakuraPink.r, sakuraPink.g, sakuraPink.b, 0.74f);
            GUI.DrawTexture(new Rect(progressBarRect.x, progressBarRect.y, progressBarRect.width * progress, progressBarRect.height), _whiteTexture);
            GUI.color = new Color(sakuraWhite.r, sakuraWhite.g, sakuraWhite.b, 0.40f);
            GUI.DrawTexture(new Rect(progressBarRect.x + Mathf.Max(0f, progressBarRect.width * progress - 14f), progressBarRect.y - 1f, 14f, progressBarRect.height + 2f), _whiteTexture);

            float statusFlowerX = stageRect.x + 224f;
            float statusFlowerY = stageRect.yMax - 24f;
            for (int i = 0; i < 5; i++)
            {
                float shimmer = 0.36f + 0.60f * (0.5f + 0.5f * Mathf.Sin(time * 4.8f - i * 0.55f));
                DrawRotatedTexture(new Vector2(statusFlowerX + i * 18f, statusFlowerY), new Vector2(10f, 11f), i * 12f, _startupSakuraPetalTexture, new Color(roseMist.r, roseMist.g, roseMist.b, shimmer));
            }

            DrawStartupTextBackdrop(new Rect(statusRect.x - 4f, statusRect.y - 1f, statusRect.width, 18f), new Color(1f, 0.95f, 0.97f, 0.22f));
            GUI.Label(statusRect, "Centered particle lattice aligning interface rhythm", _startupLightHintStyle);
            GUI.color = previousColor;
        }

        private void UnloadSafely()
        {
            PrepareForUnload();
            Debug.Log("[Wallhack] Menu unloaded safely.");
            Loader.Unload();
        }

        private static void DrawRepoOverlays()
        {
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.depth = -1000;
            GUI.color = Color.white;
            try
            {
                EnemyEsp.Draw();
                WorldEsp.Draw();
                LootChams.OnRenderObject();
                LootEsp.Draw();
                LootEsp.DrawCartUI();
                PlayerEsp.Draw();
                LaserSight.Draw();
                DrawCrosshairOverlay();
                DrawFpsOverlay();
                if (ConfigManager.Config != null && ConfigManager.Config.Misc.ShowKeybinds)
                {
                    Cheat.UI.KeybindsUI.Draw();
                }
                EnemyPreview.Update();
                if (FreeCam.Enabled)
                {
                    FreeCam.DrawOverlay();
                }
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
                GUI.depth = previousDepth;
            }
        }
    }
}
