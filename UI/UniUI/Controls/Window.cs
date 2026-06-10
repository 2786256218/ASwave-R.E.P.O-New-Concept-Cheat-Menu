using Cheat.UI.UniUI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Controls;

public class Window : Widget<VisualElement>
{
	private VisualElement _header;

	private Label _titleLabel;

	private VisualElement _content;

	private bool _isDragging;

	private Vector2 _dragOffset;

	public Window(string title)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Expected O, but got Unknown
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Expected O, but got Unknown
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		base.Element.style.position = (Position)1;
		base.Element.style.backgroundColor = UniTheme.Background;
		base.Element.style.borderTopLeftRadius = UniTheme.CornerRadius;
		base.Element.style.borderTopRightRadius = UniTheme.CornerRadius;
		base.Element.style.borderBottomLeftRadius = UniTheme.CornerRadius;
		base.Element.style.borderBottomRightRadius = UniTheme.CornerRadius;
		base.Element.style.overflow = (Overflow)1;
		base.Element.pickingMode = (PickingMode)0;
		_header = new VisualElement();
		_header.style.height = UniTheme.HeaderHeight;
		_header.style.backgroundColor = new Color(0f, 0f, 0f, 0.2f);
		_header.style.flexDirection = (FlexDirection)2;
		_header.style.alignItems = (Align)2;
		_header.style.paddingLeft = 10f;
		((CallbackEventHandler)_header).RegisterCallback<MouseDownEvent>((EventCallback<MouseDownEvent>)OnHeaderDown, (TrickleDown)0);
		((CallbackEventHandler)_header).RegisterCallback<MouseUpEvent>((EventCallback<MouseUpEvent>)OnHeaderUp, (TrickleDown)0);
		((CallbackEventHandler)_header).RegisterCallback<MouseMoveEvent>((EventCallback<MouseMoveEvent>)OnHeaderMove, (TrickleDown)0);
		base.Element.Add(_header);
		_titleLabel = new Label(title);
		((VisualElement)_titleLabel).style.fontSize = 14f;
		((VisualElement)_titleLabel).style.unityFontStyleAndWeight = (FontStyle)1;
		((VisualElement)_titleLabel).style.color = UniTheme.Accent;
		_header.Add((VisualElement)(object)_titleLabel);
		_content = new VisualElement();
		_content.style.flexGrow = 1f;
		_content.style.paddingTop = 10f;
		_content.style.paddingBottom = 10f;
		_content.style.paddingLeft = 10f;
		_content.style.paddingRight = 10f;
		base.Element.Add(_content);
	}

	public void Add(VisualElement child)
	{
		_content.Add(child);
	}

	public void AddWidget<T>(Widget<T> widget) where T : VisualElement, new()
	{
		_content.Add((VisualElement)(object)widget.Element);
	}

	public Window SetPosition(float x, float y)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		base.Element.style.left = x;
		base.Element.style.top = y;
		return this;
	}

	private void OnHeaderDown(MouseDownEvent evt)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (((MouseEventBase<MouseDownEvent>)(object)evt).button == 0)
		{
			_isDragging = true;
			_dragOffset = ((MouseEventBase<MouseDownEvent>)(object)evt).localMousePosition;
			MouseCaptureController.CaptureMouse((IEventHandler)(object)_header);
			((EventBase)evt).StopPropagation();
		}
	}

	private void OnHeaderUp(MouseUpEvent evt)
	{
		if (_isDragging && ((MouseEventBase<MouseUpEvent>)(object)evt).button == 0)
		{
			_isDragging = false;
			MouseCaptureController.ReleaseMouse((IEventHandler)(object)_header);
			((EventBase)evt).StopPropagation();
		}
	}

	private void OnHeaderMove(MouseMoveEvent evt)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		if (_isDragging)
		{
			Vector2 mousePosition = ((MouseEventBase<MouseMoveEvent>)(object)evt).mousePosition;
			base.Element.style.left = mousePosition.x - _dragOffset.x;
			base.Element.style.top = mousePosition.y - _dragOffset.y;
			((EventBase)evt).StopPropagation();
		}
	}
}
