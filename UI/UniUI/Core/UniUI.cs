using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Core;

public static class UniUI
{
	private static GameObject _uiObject;

	private static UIDocument _doc;

	private static PanelSettings _panelSettings;

	public static VisualElement Root => ((UnityEngine.Object)(object)_doc != (UnityEngine.Object)null) ? _doc.rootVisualElement : null;

	public static void Init()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)_uiObject != (UnityEngine.Object)null)
		{
			return;
		}
		try
		{
			_uiObject = new GameObject("UniUI_Overlay");
			UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)(object)_uiObject);
			_panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
			_panelSettings.scaleMode = (PanelScaleMode)2;
			_panelSettings.referenceResolution = new Vector2Int(1920, 1080);
			_panelSettings.match = 0.5f;
			_panelSettings.sortingOrder = 999f;
			_doc = _uiObject.AddComponent<UIDocument>();
			_doc.panelSettings = _panelSettings;
			Root.style.width = Length.Percent(100f);
			Root.style.height = Length.Percent(100f);
			Root.style.justifyContent = (Justify)0;
			Root.style.alignItems = (Align)1;
			Root.pickingMode = (PickingMode)1;
			try
			{
				Font val = Resources.GetBuiltinResource<Font>("Arial.ttf");
				if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
				{
					val = Resources.FindObjectsOfTypeAll<Font>()[0];
				}
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					Root.style.unityFont = val;
					Console.WriteLine("[UniUI] Applied font: " + ((UnityEngine.Object)val).name);
				}
			}
			catch
			{
				Console.WriteLine("[UniUI] Failed to load default font.");
			}
			Console.WriteLine("[UniUI] Initialized successfully.");
		}
		catch (Exception arg)
		{
			Console.WriteLine($"[UniUI] Failed to initialize: {arg}");
		}
	}

	public static void Shutdown()
	{
		if ((UnityEngine.Object)(object)_uiObject != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)_uiObject);
			_uiObject = null;
			_doc = null;
			_panelSettings = null;
		}
	}
}
