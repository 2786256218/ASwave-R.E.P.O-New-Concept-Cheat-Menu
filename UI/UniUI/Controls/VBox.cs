using Cheat.UI.UniUI.Core;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Controls;

public class VBox : Widget<VisualElement>
{
	public VBox()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		base.Element.style.flexDirection = (FlexDirection)0;
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
