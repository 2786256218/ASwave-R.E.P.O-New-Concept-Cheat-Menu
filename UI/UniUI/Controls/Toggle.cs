using System;
using Cheat.UI.UniUI.Core;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Controls;

public class Toggle : Widget<VisualElement>
{
	private VisualElement _checkBox;

	private VisualElement _checkMark;

	private Label _label;

	private bool _value;

	private Action<bool> _onChanged;

	public Toggle(string label, bool initialValue, Action<bool> onChanged)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Expected O, but got Unknown
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Expected O, but got Unknown
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		_value = initialValue;
		_onChanged = onChanged;
		base.Element.style.flexDirection = (FlexDirection)2;
		base.Element.style.alignItems = (Align)2;
		base.Element.style.marginBottom = 5f;
		base.Element.style.height = 25f;
		((CallbackEventHandler)base.Element).RegisterCallback<MouseDownEvent>((EventCallback<MouseDownEvent>)OnMouseDown, (TrickleDown)0);
		_checkBox = new VisualElement();
		_checkBox.style.width = 18f;
		_checkBox.style.height = 18f;
		_checkBox.style.backgroundColor = UniTheme.PanelBackground;
		_checkBox.style.borderTopLeftRadius = 2f;
		_checkBox.style.borderTopRightRadius = 2f;
		_checkBox.style.borderBottomLeftRadius = 2f;
		_checkBox.style.borderBottomRightRadius = 2f;
		_checkBox.style.justifyContent = (Justify)1;
		_checkBox.style.alignItems = (Align)2;
		base.Element.Add(_checkBox);
		_checkMark = new VisualElement();
		_checkMark.style.width = 10f;
		_checkMark.style.height = 10f;
		_checkMark.style.backgroundColor = UniTheme.Accent;
		_checkMark.style.display = (DisplayStyle)(!_value ? 1 : 0);
		_checkBox.Add(_checkMark);
		_label = new Label(label);
		((VisualElement)_label).style.color = UniTheme.Text;
		((VisualElement)_label).style.marginLeft = 8f;
		((VisualElement)_label).style.fontSize = 12f;
		base.Element.Add((VisualElement)(object)_label);
	}

	private void OnMouseDown(MouseDownEvent evt)
	{
		if (((MouseEventBase<MouseDownEvent>)(object)evt).button == 0)
		{
			SetValue(!_value);
			_onChanged?.Invoke(_value);
			((EventBase)evt).StopPropagation();
		}
	}

	public void SetValue(bool value)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		_value = value;
		_checkMark.style.display = (DisplayStyle)(!_value ? 1 : 0);
	}
}
