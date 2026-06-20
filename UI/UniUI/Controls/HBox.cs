using Cheat.UI.UniUI.Core;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Controls;

public class HBox : Widget<VisualElement>
{
	public HBox()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		base.Element.style.flexDirection = (FlexDirection)2;
	}

	public void Add(VisualElement child)
	{
		base.Element.Add(child);
	}

	public void AddWidget<T>(Widget<T> widget) where T : VisualElement, new()
	{
		base.Element.Add((VisualElement)(object)widget.Element);
	}
}
