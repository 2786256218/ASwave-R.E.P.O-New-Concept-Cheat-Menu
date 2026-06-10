using System;

namespace REPO.Cheat.Wallhack.AuxMenu
{
    public enum AuxMenuThemeKind
    {
        Tech,
        Dark,
        Plum,
        Sakura
    }

    public sealed class AuxMenuThemeRotator
    {
        private int _nextIndex;

        public AuxMenuThemeKind NextTheme()
        {
            AuxMenuThemeKind theme = GetThemeAt(_nextIndex);
            _nextIndex = (_nextIndex + 1) % 4;
            return theme;
        }

        public void Reset(int nextIndex)
        {
            _nextIndex = Normalize(nextIndex);
        }

        private static AuxMenuThemeKind GetThemeAt(int index)
        {
            switch (Normalize(index))
            {
                case 1:
                    return AuxMenuThemeKind.Dark;
                case 2:
                    return AuxMenuThemeKind.Plum;
                case 3:
                    return AuxMenuThemeKind.Sakura;
                default:
                    return AuxMenuThemeKind.Tech;
            }
        }

        private static int Normalize(int index)
        {
            int normalized = index % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }
    }

    public sealed class AuxMenuAnimationSession : IDisposable
    {
        private long _allocatedBytes;

        public AuxMenuAnimationSession(AuxMenuThemeKind theme)
        {
            Theme = theme;
            IsRunning = true;
        }

        public AuxMenuThemeKind Theme { get; private set; }

        public bool IsRunning { get; private set; }

        public long AllocatedBytes
        {
            get { return _allocatedBytes; }
        }

        public void TrackTextureAllocation(int width, int height)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Animation session is not running.");
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException("width");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException("height");
            }

            checked
            {
                _allocatedBytes += (long)width * height * 4L;
            }
        }

        public void ReleaseAll()
        {
            _allocatedBytes = 0L;
            IsRunning = false;
        }

        public bool IsWithinBudget(long byteBudget)
        {
            return _allocatedBytes < byteBudget;
        }

        public void Dispose()
        {
            ReleaseAll();
        }
    }

    public sealed class AuxMenuLifecycleController
    {
        private readonly AuxMenuThemeRotator _rotator;
        private readonly float _transitionDurationSeconds;
        private AuxMenuAnimationSession _activeSession;
        private AuxMenuThemeKind _currentTheme;
        private AuxMenuThemeKind _previousTheme;
        private float _transitionStartTime = float.NegativeInfinity;

        public AuxMenuLifecycleController(AuxMenuThemeRotator rotator, float transitionDurationSeconds)
        {
            if (rotator == null)
            {
                throw new ArgumentNullException("rotator");
            }

            if (transitionDurationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException("transitionDurationSeconds");
            }

            _rotator = rotator;
            _transitionDurationSeconds = transitionDurationSeconds;
            _currentTheme = AuxMenuThemeKind.Tech;
            _previousTheme = AuxMenuThemeKind.Tech;
        }

        public AuxMenuAnimationSession ActiveSession
        {
            get { return _activeSession; }
        }

        public AuxMenuThemeKind CurrentTheme
        {
            get { return _currentTheme; }
        }

        public AuxMenuThemeKind PreviousTheme
        {
            get { return _previousTheme; }
        }

        public bool IsVisible { get; private set; }

        public long CurrentAllocatedBytes
        {
            get { return _activeSession != null ? _activeSession.AllocatedBytes : 0L; }
        }

        public AuxMenuThemeKind Show(float now)
        {
            _previousTheme = _currentTheme;
            _currentTheme = _rotator.NextTheme();
            ReplaceSession(new AuxMenuAnimationSession(_currentTheme));
            _transitionStartTime = now;
            IsVisible = true;
            return _currentTheme;
        }

        public void Hide()
        {
            IsVisible = false;
            _transitionStartTime = float.NegativeInfinity;
            ReleaseSession();
        }

        public void RegisterTextureAllocation(int width, int height)
        {
            if (_activeSession == null)
            {
                throw new InvalidOperationException("Animation session has not been created.");
            }

            _activeSession.TrackTextureAllocation(width, height);
        }

        public float GetTransitionAlpha(float now)
        {
            if (!IsVisible)
            {
                return 0f;
            }

            if (float.IsNegativeInfinity(_transitionStartTime))
            {
                return 1f;
            }

            float progress = (now - _transitionStartTime) / _transitionDurationSeconds;
            if (progress <= 0f)
            {
                return 0f;
            }

            if (progress >= 1f)
            {
                return 1f;
            }

            return progress;
        }

        private void ReplaceSession(AuxMenuAnimationSession session)
        {
            ReleaseSession();
            _activeSession = session;
        }

        private void ReleaseSession()
        {
            if (_activeSession == null)
            {
                return;
            }

            _activeSession.Dispose();
            _activeSession = null;
        }
    }

    public enum AuxMenuThemeLayoutKind
    {
        DiagonalGolden,
        CenteredSymmetry
    }

    public sealed class AuxMenuMotionProfile
    {
        public AuxMenuMotionProfile(string easing, int durationMs, int delayMs, string transform)
        {
            if (string.IsNullOrEmpty(easing))
            {
                throw new ArgumentException("easing");
            }

            if (durationMs <= 0)
            {
                throw new ArgumentOutOfRangeException("durationMs");
            }

            if (delayMs < 0)
            {
                throw new ArgumentOutOfRangeException("delayMs");
            }

            Easing = easing;
            DurationMs = durationMs;
            DelayMs = delayMs;
            Transform = transform ?? string.Empty;
        }

        public string Easing { get; private set; }

        public int DurationMs { get; private set; }

        public int DelayMs { get; private set; }

        public string Transform { get; private set; }
    }

    public sealed class AuxMenuThemeProfile
    {
        public AuxMenuThemeProfile(
            AuxMenuThemeKind theme,
            AuxMenuThemeLayoutKind layoutKind,
            float whitespaceRatio,
            float mainVisualAnchorX,
            float mainVisualAnchorY,
            float secondaryFlowAnchorX,
            float secondaryFlowAnchorY,
            float petalTiltMinDegrees,
            float petalTiltMaxDegrees,
            int particleLimit,
            AuxMenuMotionProfile primaryMotion,
            AuxMenuMotionProfile secondaryMotion)
        {
            if (whitespaceRatio <= 0f || whitespaceRatio >= 1f)
            {
                throw new ArgumentOutOfRangeException("whitespaceRatio");
            }

            if (mainVisualAnchorX < 0f || mainVisualAnchorX > 1f)
            {
                throw new ArgumentOutOfRangeException("mainVisualAnchorX");
            }

            if (mainVisualAnchorY < 0f || mainVisualAnchorY > 1f)
            {
                throw new ArgumentOutOfRangeException("mainVisualAnchorY");
            }

            if (secondaryFlowAnchorX < 0f || secondaryFlowAnchorX > 1f)
            {
                throw new ArgumentOutOfRangeException("secondaryFlowAnchorX");
            }

            if (secondaryFlowAnchorY < 0f || secondaryFlowAnchorY > 1f)
            {
                throw new ArgumentOutOfRangeException("secondaryFlowAnchorY");
            }

            if (petalTiltMinDegrees < 0f || petalTiltMaxDegrees < petalTiltMinDegrees)
            {
                throw new ArgumentOutOfRangeException("petalTiltMinDegrees");
            }

            if (particleLimit < 0)
            {
                throw new ArgumentOutOfRangeException("particleLimit");
            }

            if (primaryMotion == null)
            {
                throw new ArgumentNullException("primaryMotion");
            }

            if (secondaryMotion == null)
            {
                throw new ArgumentNullException("secondaryMotion");
            }

            Theme = theme;
            LayoutKind = layoutKind;
            WhitespaceRatio = whitespaceRatio;
            MainVisualAnchorX = mainVisualAnchorX;
            MainVisualAnchorY = mainVisualAnchorY;
            SecondaryFlowAnchorX = secondaryFlowAnchorX;
            SecondaryFlowAnchorY = secondaryFlowAnchorY;
            PetalTiltMinDegrees = petalTiltMinDegrees;
            PetalTiltMaxDegrees = petalTiltMaxDegrees;
            ParticleLimit = particleLimit;
            PrimaryMotion = primaryMotion;
            SecondaryMotion = secondaryMotion;
        }

        public AuxMenuThemeKind Theme { get; private set; }

        public AuxMenuThemeLayoutKind LayoutKind { get; private set; }

        public float WhitespaceRatio { get; private set; }

        public float MainVisualAnchorX { get; private set; }

        public float MainVisualAnchorY { get; private set; }

        public float SecondaryFlowAnchorX { get; private set; }

        public float SecondaryFlowAnchorY { get; private set; }

        public float PetalTiltMinDegrees { get; private set; }

        public float PetalTiltMaxDegrees { get; private set; }

        public int ParticleLimit { get; private set; }

        public AuxMenuMotionProfile PrimaryMotion { get; private set; }

        public AuxMenuMotionProfile SecondaryMotion { get; private set; }
    }

    public static class AuxMenuThemeCatalog
    {
        private static readonly AuxMenuThemeProfile PlumProfile = new AuxMenuThemeProfile(
            AuxMenuThemeKind.Plum,
            AuxMenuThemeLayoutKind.DiagonalGolden,
            0.62f,
            0.28f,
            0.23f,
            0.74f,
            0.76f,
            3f,
            8f,
            24,
            new AuxMenuMotionProfile("cubic-bezier(0.4,0,0.2,1)", 960, 0, "translate3d(0,-6px,0) scale(1.02)"),
            new AuxMenuMotionProfile("cubic-bezier(0.4,0,0.2,1)", 1100, 80, "rotate(6deg) translate3d(12px,8px,0)"));

        private static readonly AuxMenuThemeProfile SakuraProfile = new AuxMenuThemeProfile(
            AuxMenuThemeKind.Sakura,
            AuxMenuThemeLayoutKind.CenteredSymmetry,
            0.55f,
            0.50f,
            0.45f,
            0.50f,
            0.70f,
            3f,
            8f,
            80,
            new AuxMenuMotionProfile("cubic-bezier(0.4,0,0.2,1)", 400, 0, "translate3d(0,18px,0)"),
            new AuxMenuMotionProfile("cubic-bezier(0.4,0,0.2,1)", 900, 60, "translate3d(0,-15px,0) rotate(4deg)"));

        private static readonly AuxMenuThemeProfile DarkProfile = new AuxMenuThemeProfile(
            AuxMenuThemeKind.Dark,
            AuxMenuThemeLayoutKind.DiagonalGolden,
            0.50f,
            0.74f,
            0.20f,
            0.30f,
            0.42f,
            0f,
            0f,
            24,
            new AuxMenuMotionProfile("cubic-bezier(0.22,1,0.36,1)", 720, 0, "translate3d(0,0,0) skewX(-2deg)"),
            new AuxMenuMotionProfile("steps(6,end)", 540, 40, "translate3d(18px,-4px,0) scaleX(1.03)"));

        private static readonly AuxMenuThemeProfile TechProfile = new AuxMenuThemeProfile(
            AuxMenuThemeKind.Tech,
            AuxMenuThemeLayoutKind.DiagonalGolden,
            0.47f,
            0.24f,
            0.20f,
            0.70f,
            0.72f,
            0f,
            0f,
            28,
            new AuxMenuMotionProfile("linear", 860, 0, "translate3d(0,0,0) scanX(0 -> 100%)"),
            new AuxMenuMotionProfile("cubic-bezier(0.4,0,0.2,1)", 980, 60, "translate3d(0,-8px,0) opacity(0.20 -> 0.52)"));

        public static AuxMenuThemeProfile GetProfile(AuxMenuThemeKind theme)
        {
            switch (theme)
            {
                case AuxMenuThemeKind.Dark:
                    return DarkProfile;
                case AuxMenuThemeKind.Plum:
                    return PlumProfile;
                case AuxMenuThemeKind.Sakura:
                    return SakuraProfile;
                default:
                    return TechProfile;
            }
        }
    }

    public static class AuxMenuThemePerformanceBudget
    {
        public const int ThemeSwitchClassBudgetMs = 20;
        public const int StressSwitchIterations = 50;
        public const int InteractiveAnimationDurationMs = 240;
        public const int MenuRevealDurationMs = 600;
        public const int BackgroundParallaxMaxOffsetPx = 20;
        public const int PlumPetalsPerSecondMin = 30;
        public const int PlumPetalsPerSecondMax = 50;
        public const int SakuraPetalsPerSecondMin = 40;
        public const int SakuraPetalsPerSecondMax = 60;
        public const long MaxThemeAnimationBytes = 60L * 1024L * 1024L;
        public const int MinimumThemeAnimationFps = 45;
        public const int MaxSakuraScrollParticles = 80;
        public const float LighthouseClsTarget = 0.10f;
        public const float LighthouseLcpSecondsTarget = 2.5f;
        public const float PowerHeadroomRatio = 1.05f;
        public const int MinimumAnimationFpsUnderCpuThrottle = 50;
    }
}
