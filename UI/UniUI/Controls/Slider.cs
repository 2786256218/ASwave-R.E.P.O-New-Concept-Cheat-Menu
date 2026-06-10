using System;
using Cheat.UI.UniUI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Controls;

public class Slider : Widget<VisualElement>
{
	private VisualElement _track;

	private VisualElement _fill;

	private VisualElement _knob;

	private Label _valueLabel;

	private float _min;

	private float _max;

	private float _value;

	private string _format;

	private Action<float> _onChanged;

	private bool _isDragging;

	public Slider(string label, float min, float max, float initialValue, Action<float> onChanged, string format = "F2")
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Expected O, but got Unknown
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Expected O, but got Unknown
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Expected O, but got Unknown
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Expected O, but got Unknown
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0410: Unknown result type (might be due to invalid IL or missing references)
		//IL_0427: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Unknown result type (might be due to invalid IL or missing references)
		_min = min;
		_max = max;
		_value = Mathf.Clamp(initialValue, min, max);
		_format = format;
		_onChanged = onChanged;
		base.Element.style.marginBottom = 5f;
		VisualElement val = new VisualElement();
		val.style.flexDirection = (FlexDirection)2;
		val.style.justifyContent = (Justify)3;
		val.style.marginBottom = 2f;
		base.Element.Add(val);
		Label val2 = new Label(label);
		((VisualElement)val2).style.color = UniTheme.Text;
		((VisualElement)val2).style.fontSize = 12f;
		val.Add((VisualElement)(object)val2);
		_valueLabel = new Label(_value.ToString(_format));
		((VisualElement)_valueLabel).style.color = UniTheme.Accent;
		((VisualElement)_valueLabel).style.fontSize = 12f;
		val.Add((VisualElement)(object)_valueLabel);
		VisualElement val3 = new VisualElement();
		val3.style.height = 20f;
		val3.style.justifyContent = (Justify)1;
		base.Element.Add(val3);
		_track = new VisualElement();
		_track.style.height = 4f;
		_track.style.backgroundColor = UniTheme.PanelBackground;
		_track.style.width = Length.Percent(100f);
		_track.style.borderTopLeftRadius = 2f;
		_track.style.borderTopRightRadius = 2f;
		_track.style.borderBottomLeftRadius = 2f;
		_track.style.borderBottomRightRadius = 2f;
		val3.Add(_track);
		_fill = new VisualElement();
		_fill.style.height = 4f;
		_fill.style.backgroundColor = UniTheme.Accent;
		_fill.style.position = (Position)1;
		_fill.style.left = 0f;
		_fill.style.top = 8f;
		_fill.style.borderTopLeftRadius = 2f;
		_fill.style.borderTopRightRadius = 2f;
		_fill.style.borderBottomLeftRadius = 2f;
		_fill.style.borderBottomRightRadius = 2f;
		val3.Add(_fill);
		_knob = new VisualElement();
		_knob.style.width = 12f;
		_knob.style.height = 12f;
		_knob.style.backgroundColor = UniTheme.Text;
		_knob.style.borderTopLeftRadius = 6f;
		_knob.style.borderTopRightRadius = 6f;
		_knob.style.borderBottomLeftRadius = 6f;
		_knob.style.borderBottomRightRadius = 6f;
		_knob.style.position = (Position)1;
		_knob.style.top = 4f;
		_knob.pickingMode = (PickingMode)1;
		val3.Add(_knob);
		((CallbackEventHandler)val3).RegisterCallback<MouseDownEvent>((EventCallback<MouseDownEvent>)OnMouseDown, (TrickleDown)0);
		((CallbackEventHandler)val3).RegisterCallback<MouseUpEvent>((EventCallback<MouseUpEvent>)OnMouseUp, (TrickleDown)0);
		((CallbackEventHandler)val3).RegisterCallback<MouseMoveEvent>((EventCallback<MouseMoveEvent>)OnMouseMove, (TrickleDown)0);
		((CallbackEventHandler)val3).RegisterCallback<MouseLeaveEvent>((EventCallback<MouseLeaveEvent>)OnMouseLeave, (TrickleDown)0);
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		float num = (_value - _min) / (_max - _min);
		num = Mathf.Clamp01(num);
		_fill.style.width = Length.Percent(num * 100f);
		_knob.style.left = Length.Percent(num * 100f);
		_knob.style.marginLeft = -6f;
		((TextElement)_valueLabel).text = _value.ToString(_format);
	}

	private void UpdateValueFromMouse(float localX, float width)
	{
		if (!(width <= 0f))
		{
			float num = localX / width;
			num = Mathf.Clamp01(num);
			float num2 = _min + num * (_max - _min);
			if (Mathf.Abs(num2 - _value) > Mathf.Epsilon)
			{
				_value = num2;
				UpdateVisuals();
				_onChanged?.Invoke(_value);
			}
		}
	}

	private void OnMouseDown(MouseDownEvent evt)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (((MouseEventBase<MouseDownEvent>)(object)evt).button == 0)
		{
			_isDragging = true;
			IEventHandler currentTarget = ((EventBase)evt).currentTarget;
			VisualElement val = (VisualElement)(object)((currentTarget is VisualElement) ? currentTarget : null);
			float x = ((MouseEventBase<MouseDownEvent>)(object)evt).localMousePosition.x;
			Rect layout = val.layout;
			UpdateValueFromMouse(x, layout.width);
			((EventBase)evt).StopPropagation();
			MouseCaptureController.CaptureMouse((IEventHandler)(object)val);
		}
	}

	private void OnMouseUp(MouseUpEvent evt)
	{
		if (_isDragging && ((MouseEventBase<MouseUpEvent>)(object)evt).button == 0)
		{
			_isDragging = false;
			IEventHandler currentTarget = ((EventBase)evt).currentTarget;
			VisualElement val = (VisualElement)(object)((currentTarget is VisualElement) ? currentTarget : null);
			MouseCaptureController.ReleaseMouse((IEventHandler)(object)val);
			((EventBase)evt).StopPropagation();
		}
	}

	private void OnMouseMove(MouseMoveEvent evt)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (_isDragging)
		{
			IEventHandler currentTarget = ((EventBase)evt).currentTarget;
			VisualElement val = (VisualElement)(object)((currentTarget is VisualElement) ? currentTarget : null);
			float x = ((MouseEventBase<MouseMoveEvent>)(object)evt).localMousePosition.x;
			Rect layout = val.layout;
			UpdateValueFromMouse(x, layout.width);
			((EventBase)evt).StopPropagation();
		}
	}

	private void OnMouseLeave(MouseLeaveEvent evt)
	{
	}
}
