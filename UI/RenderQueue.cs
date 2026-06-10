using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cheat.UI;

public static class RenderQueue
{
	private static readonly List<Action> _drawQueue = new List<Action>();

	public static void Add(Action drawAction)
	{
		if (drawAction != null)
		{
			_drawQueue.Add(drawAction);
		}
	}

	public static void Clear()
	{
		_drawQueue.Clear();
	}

	public static void Draw()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		if (_drawQueue.Count == 0)
		{
			return;
		}
		Matrix4x4 matrix = GUI.matrix;
		Color color = GUI.color;
		int depth = GUI.depth;
		GUI.matrix = Matrix4x4.identity;
		GUI.depth = -2000;
		while (_drawQueue.Count > 0)
		{
			try
			{
				Action action = _drawQueue[0];
				_drawQueue.RemoveAt(0);
				action();
			}
			catch (Exception ex)
			{
				Console.WriteLine("[RenderQueue] Error executing draw action: " + ex.Message);
			}
		}
		GUI.matrix = matrix;
		GUI.color = color;
		GUI.depth = depth;
	}
}
