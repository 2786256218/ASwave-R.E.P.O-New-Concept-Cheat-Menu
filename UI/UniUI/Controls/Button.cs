using System;
using Cheat.UI.UniUI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Controls;

public class Button : Widget<VisualElement>
{
	private Label _label;

	private Action _onClick;

	public Button(string text, Action onClick)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		_onClick = onClick;
		base.Element.style.height = 30f;
		base.Element.style.backgroundColor = UniTheme.PanelBackground;
		base.Element.style.borderTopLeftRadius = 4f;
		base.Element.style.borderTopRightRadius = 4f;
		base.Element.style.borderBottomLeftRadius = 4f;
		base.Element.style.borderBottomRightRadius = 4f;
		base.Element.style.marginBottom = 5f;
		base.Element.style.justifyContent = (Justify)1;
		base.Element.style.alignItems = (Align)2;
		_label = new Label(text);
		((VisualElement)_label).style.color = UniTheme.Text;
		((VisualElement)_label).style.unityFontStyleAndWeight = (FontStyle)1;
		((VisualElement)_label).style.fontSize = 12f;
		base.Element.Add((VisualElement)(object)_label);
		((CallbackEventHandler)base.Element).RegisterCallback<MouseDownEvent>((EventCallback<MouseDownEvent>)OnMouseDown, (TrickleDown)0);
		((CallbackEventHandler)base.Element).RegisterCallback<MouseUpEvent>((EventCallback<MouseUpEvent>)OnMouseUp, (TrickleDown)0);
		((CallbackEventHandler)base.Element).RegisterCallback<MouseEnterEvent>((EventCallback<MouseEnterEvent>)OnMouseEnter, (TrickleDown)0);
		((CallbackEventHandler)base.Element).RegisterCallback<MouseLeaveEvent>((EventCallback<MouseLeaveEvent>)OnMouseLeave, (TrickleDown)0);
	}

	private void OnMouseDown(MouseDownEvent evt)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (((MouseEventBase<MouseDownEvent>)(object)evt).button == 0)
		{
			base.Element.style.backgroundColor = UniTheme.ButtonActive;
			_onClick?.Invoke();
			((EventBase)evt).StopPropagation();
		}
	}

	private void OnMouseUp(MouseUpEvent evt)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (((MouseEventBase<MouseUpEvent>)(object)evt).button == 0)
		{
			base.Element.style.backgroundColor = UniTheme.ButtonHover;
		}
	}

	private void OnMouseEnter(MouseEnterEvent evt)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		base.Element.style.backgroundColor = UniTheme.ButtonHover;
	}

	private void OnMouseLeave(MouseLeaveEvent evt)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		base.Element.style.backgroundColor = UniTheme.PanelBackground;
	}
}
