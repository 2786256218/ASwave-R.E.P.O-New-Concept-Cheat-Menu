using System;
using System.Collections.Generic;

namespace Cheat.UI;

public class SidebarManager
{
	public class SidebarSection
	{
		public string Title;

		public string Icon;

		public bool IsExpanded;

		public List<SubTab> SubTabs = new List<SubTab>();

		public int ID;

		public Action RenderAction;
	}

	public class SubTab
	{
		public string Title;

		public Action RenderAction;

		public int ID;
	}

	private List<SidebarSection> _sections = new List<SidebarSection>();

	private int _activeSectionId = -1;

	private int _activeSubTabId = -1;

	public void AddSection(string title, string icon, int id, Action renderAction = null, bool expandedByDefault = false)
	{
		_sections.Add(new SidebarSection
		{
			Title = title,
			Icon = icon,
			ID = id,
			IsExpanded = expandedByDefault,
			RenderAction = renderAction
		});
	}

	public void AddSubTab(int sectionId, string title, int subTabId, Action renderAction)
	{
		_sections.Find((SidebarSection s) => s.ID == sectionId)?.SubTabs.Add(new SubTab
		{
			Title = title,
			ID = subTabId,
			RenderAction = renderAction
		});
	}

	public void Select(int sectionId, int subTabId = -1)
	{
		_activeSectionId = sectionId;
		_activeSubTabId = subTabId;
		SidebarSection sidebarSection = _sections.Find((SidebarSection s) => s.ID == sectionId);
		if (sidebarSection != null)
		{
			sidebarSection.IsExpanded = true;
			if (subTabId == -1 && sidebarSection.SubTabs.Count > 0)
			{
				_activeSubTabId = sidebarSection.SubTabs[0].ID;
			}
		}
	}

	public Action GetActiveRenderAction()
	{
		SidebarSection sidebarSection = _sections.Find((SidebarSection s) => s.ID == _activeSectionId);
		if (sidebarSection == null)
		{
			return null;
		}
		if (sidebarSection.SubTabs.Count > 0)
		{
			SubTab subTab = sidebarSection.SubTabs.Find((SubTab t) => t.ID == _activeSubTabId);
			if (subTab != null)
			{
				return subTab.RenderAction;
			}
			if (sidebarSection.SubTabs.Count > 0)
			{
				return sidebarSection.SubTabs[0].RenderAction;
			}
		}
		return sidebarSection.RenderAction;
	}

	public void DrawSidebar(float x, float y, float width)
	{
		float num = y;
		foreach (SidebarSection section in _sections)
		{
			bool flag = _activeSectionId == section.ID;
			bool flag2 = section.SubTabs.Count > 0;
			if (Elements.SidebarButton(section.Title, section.Icon, flag, section.IsExpanded, flag2, x, num, width))
			{
				if (flag2)
				{
					section.IsExpanded = !section.IsExpanded;
					if (section.IsExpanded && !flag)
					{
						Select(section.ID);
					}
				}
				else
				{
					Select(section.ID);
				}
			}
			num += 45f;
			if (!flag2 || !section.IsExpanded)
			{
				continue;
			}
			foreach (SubTab subTab in section.SubTabs)
			{
				bool active = flag && _activeSubTabId == subTab.ID;
				if (Elements.SubTabButton(subTab.Title, active, x, num, width))
				{
					Select(section.ID, subTab.ID);
				}
				num += 35f;
			}
			num += 5f;
		}
	}
}
