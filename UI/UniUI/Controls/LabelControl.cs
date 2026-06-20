using Cheat.UI.UniUI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cheat.UI.UniUI.Controls;

public class LabelControl : Widget<Label>
{
	public LabelControl(string text, bool header = false)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		((TextElement)base.Element).text = text;
		((VisualElement)base.Element).style.color = header ? UniTheme.Accent : UniTheme.Text;
		((VisualElement)base.Element).style.fontSize = (float)(header ? 14 : 12);
		((VisualElement)base.Element).style.unityFontStyleAndWeight = (FontStyle)(header ? 1 : 0);
		((VisualElement)base.Element).style.marginBottom = 5f;
		if (header)
		{
			((VisualElement)base.Element).style.marginTop = 10f;
			((VisualElement)base.Element).style.borderBottomWidth = 1f;
			((VisualElement)base.Element).style.borderBottomColor = UniTheme.Separator;
			((VisualElement)base.Element).style.paddingBottom = 2f;
		}
	}
}
