using UnityEngine;

namespace REPO.Cheat.Wallhack.UI
{
    public sealed class GuiAnimationClock
    {
        private int _lastFrame = -1;
        private float _lastTime = float.NegativeInfinity;
        private float _cachedDeltaTime;

        public float GetDeltaTime(int frame, float unscaledTime)
        {
            if (_lastFrame == frame)
            {
                return _cachedDeltaTime;
            }

            _cachedDeltaTime = float.IsNegativeInfinity(_lastTime)
                ? 0f
                : Mathf.Clamp(unscaledTime - _lastTime, 0f, 0.05f);

            _lastTime = unscaledTime;
            _lastFrame = frame;
            return _cachedDeltaTime;
        }

        public void Reset()
        {
            _lastFrame = -1;
            _lastTime = float.NegativeInfinity;
            _cachedDeltaTime = 0f;
        }
    }

    public struct ToggleSwitchLayout
    {
        public Rect KnobRect;
        public float KnobCenterX;
        public float KnobCenterY;
        public float KnobSize;
        public float TravelDistance;
    }

    public static class ToggleSwitchMath
    {
        public const float KnobPaddingRatio = 0.08f;
        public const float KnobSizeRatio = 0.84f;

        public static float EvaluateSoftBezier(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public static float AdvanceToggleAmount(float currentAmount, bool toggledOn, float deltaTime, float durationSeconds)
        {
            float target = toggledOn ? 1f : 0f;
            if (durationSeconds <= 0f)
            {
                return target;
            }

            return Mathf.MoveTowards(currentAmount, target, deltaTime / durationSeconds);
        }

        public static ToggleSwitchLayout CalculateLayout(Rect trackRect, float toggleAmount, bool hovered, float unscaledTime)
        {
            float knobPadding = trackRect.height * KnobPaddingRatio;
            float knobSize = trackRect.height * KnobSizeRatio;
            float knobTravel = Mathf.Max(0f, trackRect.width - knobSize - knobPadding * 2f);
            float knobPulse = (0.5f + 0.5f * Mathf.Sin(unscaledTime * 8.0f + Mathf.Clamp01(toggleAmount) * Mathf.PI)) * 0.6f;
            float knobYOffset = hovered ? -0.6f - knobPulse * 0.6f : -0.2f;
            float knobX = trackRect.x + knobPadding + knobTravel * Mathf.Clamp01(toggleAmount);
            Rect knobRect = new Rect(knobX, trackRect.y + knobPadding + knobYOffset, knobSize, knobSize);

            return new ToggleSwitchLayout
            {
                KnobRect = knobRect,
                KnobCenterX = knobRect.center.x,
                KnobCenterY = knobRect.center.y,
                KnobSize = knobSize,
                TravelDistance = knobTravel
            };
        }
    }
}
