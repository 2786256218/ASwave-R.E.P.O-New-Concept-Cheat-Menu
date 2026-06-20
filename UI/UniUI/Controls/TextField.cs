using System;
using Cheat.UI.UniUI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Controls;

public class TextField : Widget<VisualElement>
{
	private Label _label;

	private Label _inputDisplay;

	private string _value;

	private Action<string> _onChanged;

	private bool _isFocused;

	public TextField(string label, string initialValue, Action<string> onChanged)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected O, but got Unknown
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		_value = initialValue ?? "";
		_onChanged = onChanged;
		base.Element.style.marginBottom = 5f;
		_label = new Label(label);
		((VisualElement)_label).style.color = UniTheme.Text;
		((VisualElement)_label).style.fontSize = 12f;
		((VisualElement)_label).style.marginBottom = 2f;
		base.Element.Add((VisualElement)(object)_label);
		_inputDisplay = new Label(_value);
		((VisualElement)_inputDisplay).style.height = 25f;
		((VisualElement)_inputDisplay).style.backgroundColor = UniTheme.PanelBackground;
		((VisualElement)_inputDisplay).style.borderTopLeftRadius = 4f;
		((VisualElement)_inputDisplay).style.borderTopRightRadius = 4f;
		((VisualElement)_inputDisplay).style.borderBottomLeftRadius = 4f;
		((VisualElement)_inputDisplay).style.borderBottomRightRadius = 4f;
		((VisualElement)_inputDisplay).style.borderLeftWidth = 1f;
		((VisualElement)_inputDisplay).style.borderRightWidth = 1f;
		((VisualElement)_inputDisplay).style.borderTopWidth = 1f;
		((VisualElement)_inputDisplay).style.borderBottomWidth = 1f;
		((VisualElement)_inputDisplay).style.borderLeftColor = UniTheme.Separator;
		((VisualElement)_inputDisplay).style.borderRightColor = UniTheme.Separator;
		((VisualElement)_inputDisplay).style.borderTopColor = UniTheme.Separator;
		((VisualElement)_inputDisplay).style.borderBottomColor = UniTheme.Separator;
		((VisualElement)_inputDisplay).style.color = UniTheme.Text;
		((VisualElement)_inputDisplay).style.paddingLeft = 5f;
		((VisualElement)_inputDisplay).style.paddingRight = 5f;
		((VisualElement)_inputDisplay).style.paddingTop = 4f;
		((VisualElement)_inputDisplay).style.fontSize = 12f;
		base.Element.Add((VisualElement)(object)_inputDisplay);
		((CallbackEventHandler)_inputDisplay).RegisterCallback<MouseDownEvent>((EventCallback<MouseDownEvent>)OnMouseDown, (TrickleDown)0);
		base.Element.schedule.Execute((Action)UpdateInput).Every(16L);
	}

	private void OnMouseDown(MouseDownEvent evt)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if (((MouseEventBase<MouseDownEvent>)(object)evt).button == 0)
		{
			_isFocused = true;
			((VisualElement)_inputDisplay).style.borderLeftColor = UniTheme.Accent;
			((VisualElement)_inputDisplay).style.borderRightColor = UniTheme.Accent;
			((VisualElement)_inputDisplay).style.borderTopColor = UniTheme.Accent;
			((VisualElement)_inputDisplay).style.borderBottomColor = UniTheme.Accent;
			((EventBase)evt).StopPropagation();
		}
	}

	private void UpdateInput()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		if (!_isFocused)
		{
			return;
		}
		if (Input.GetKeyDown((KeyCode)13) || Input.GetKeyDown((KeyCode)271) || Input.GetKeyDown((KeyCode)27))
		{
			_isFocused = false;
			((VisualElement)_inputDisplay).style.borderLeftColor = UniTheme.Separator;
			((VisualElement)_inputDisplay).style.borderRightColor = UniTheme.Separator;
			((VisualElement)_inputDisplay).style.borderTopColor = UniTheme.Separator;
			((VisualElement)_inputDisplay).style.borderBottomColor = UniTheme.Separator;
			return;
		}
		if (Input.GetKeyDown((KeyCode)8))
		{
			if (_value.Length > 0)
			{
				_value = _value.Substring(0, _value.Length - 1);
				UpdateDisplay();
			}
			return;
		}
		string inputString = Input.inputString;
		if (string.IsNullOrEmpty(inputString))
		{
			return;
		}
		string text = inputString;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c != '\b' && c != '\n' && c != '\r')
			{
				_value += c;
			}
		}
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		((TextElement)_inputDisplay).text = _value + (_isFocused ? "|" : "");
		_onChanged?.Invoke(_value);
	}
}
