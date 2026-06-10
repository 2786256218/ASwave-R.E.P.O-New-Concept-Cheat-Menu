using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cheat.UI;

public static class UIState
{
	public static class Easing
	{
		public static float EaseOutQuad(float t)
		{
			return t * (2f - t);
		}

		public static float EaseOutBack(float t)
		{
			float num = 1.70158f;
			float num2 = num + 1f;
			return 1f + num2 * Mathf.Pow(t - 1f, 3f) + num * Mathf.Pow(t - 1f, 2f);
		}

		public static float EaseOutCubic(float t)
		{
			return 1f - Mathf.Pow(1f - t, 3f);
		}
	}

	private static Dictionary<string, float> _animations = new Dictionary<string, float>();

	public static float BootTime = 0f;

	public static bool IsBooting = false;

	public static bool HasBootSequencePlayed = false;

	public static float BootDuration = 1.5f;

	public static float LogoScale = 0f;

	public static float WindowOpacity = 0f;

	public static void StartBoot()
	{
		if (HasBootSequencePlayed)
		{
			IsBooting = false;
			WindowOpacity = 1f;
			LogoScale = 1f;
		}
		else
		{
			IsBooting = true;
			HasBootSequencePlayed = true;
			BootTime = 0f;
			LogoScale = 0f;
			WindowOpacity = 0f;
		}
	}

	public static void UpdateBoot(float deltaTime)
	{
		if (IsBooting)
		{
			BootTime += deltaTime;
			float num = Mathf.Clamp01(BootTime / BootDuration);
			if (num < 0.3f)
			{
				LogoScale = Easing.EaseOutBack(num / 0.3f) * 0.5f;
			}
			else if (num < 0.8f)
			{
				float t = (num - 0.3f) / 0.5f;
				LogoScale = 0.5f + Easing.EaseOutCubic(t) * 0.5f;
			}
			else
			{
				float windowOpacity = (num - 0.8f) / 0.2f;
				WindowOpacity = windowOpacity;
			}
			if (num >= 1f)
			{
				IsBooting = false;
				LogoScale = 1f;
				WindowOpacity = 1f;
			}
		}
	}

	public static float GetAnimationValue(string id, bool active, float speed)
	{
		if (!_animations.ContainsKey(id))
		{
			_animations[id] = (active ? 1f : 0f);
		}
		float num = (active ? 1f : 0f);
		float num2 = _animations[id];
		if (Mathf.Abs(num2 - num) > 0.001f)
		{
			float num3 = Time.unscaledDeltaTime * speed;
			num2 = ((!(num2 < num)) ? Mathf.Max(num2 - num3, num) : Mathf.Min(num2 + num3, num));
			_animations[id] = num2;
		}
		else
		{
			_animations[id] = num;
		}
		return _animations[id];
	}

	public static float GetEasedAnimationValue(string id, bool active, float speed, Func<float, float> easing)
	{
		float animationValue = GetAnimationValue(id, active, speed);
		return easing(animationValue);
	}

	public static void Clear()
	{
		_animations.Clear();
		IsBooting = false;
	}
}
