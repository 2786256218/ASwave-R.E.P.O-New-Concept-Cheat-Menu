using UnityEngine;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Core;

public abstract class Widget<T> where T : VisualElement, new()
{
	public T Element { get; protected set; }

	protected Widget()
	{
		Element = new T();
		ApplyBaseStyles();
	}

	protected virtual void ApplyBaseStyles()
	{
	}

	public Widget<T> SetSize(float width, float height)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		((VisualElement)Element).style.width = width;
		((VisualElement)Element).style.height = height;
		return this;
	}

	public Widget<T> SetWidth(float width)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		((VisualElement)Element).style.width = width;
		return this;
	}

	public Widget<T> SetHeight(float height)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		((VisualElement)Element).style.height = height;
		return this;
	}

	public Widget<T> SetMargin(float margin)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		((VisualElement)Element).style.marginTop = margin;
		((VisualElement)Element).style.marginBottom = margin;
		((VisualElement)Element).style.marginLeft = margin;
		((VisualElement)Element).style.marginRight = margin;
		return this;
	}

	public Widget<T> SetPadding(float padding)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		((VisualElement)Element).style.paddingTop = padding;
		((VisualElement)Element).style.paddingBottom = padding;
		((VisualElement)Element).style.paddingLeft = padding;
		((VisualElement)Element).style.paddingRight = padding;
		return this;
	}

	public Widget<T> SetBackgroundColor(Color color)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		((VisualElement)Element).style.backgroundColor = color;
		return this;
	}

	public Widget<T> SetVisible(bool visible)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		((VisualElement)Element).style.display = (DisplayStyle)(!visible ? 1 : 0);
		return this;
	}

	public void AddTo(VisualElement parent)
	{
		parent.Add((VisualElement)(object)Element);
	}
}
