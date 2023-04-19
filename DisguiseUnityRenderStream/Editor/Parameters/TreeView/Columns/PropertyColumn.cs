using UnityEditor;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        class PropertyColumn : ParameterTreeViewColumn<PropertyColumnCell>
        {
            public PropertyColumn(ParameterTreeView treeView) :
                base(treeView)
            {
                title = Contents.PropertyColumnHeader;
                width = 100f;
                minWidth = 60f;
                sortable = false;
                optional = false;
                stretchable = true;
            }
        }
        
        class PropertyColumnCell : DropdownButtonCell
        {
            public PropertyColumnCell()
            {
                
            }

            protected override void UpdateDisplay()
            {
                if (m_Parameter == null || m_Parameter.ReflectedObject == null || IsMissingComponent(m_Parameter.ReflectedObject))
                {
                    this.SetDisplay(false);
                    return;
                }
                
                this.SetDisplay(true);
                
                // Show the property name or a warning icon if invalid
                if (m_Parameter.MemberInfoForEditor.IsValid())
                {
                    textElement.text = m_Parameter.MemberInfoForEditor.UINameWithGroupPrefix;
                    ShowIcon = false;
                }
                else
                {
                    textElement.text = Contents.DropdownNoneLabel;
                    ShowIcon = true;
                    Icon = Contents.WarningIcon;
                }
            }

            protected override void ShowGenericMenu()
            {
                if (m_Parameter == null || m_Parameter.ReflectedObject == null || IsMissingComponent(m_Parameter.ReflectedObject))
                    return;
                
                void AddPropertyItem(GenericMenu menu, MemberInfoForEditor memberInfo)
                {
                    // Show the real name in parentheses when the display name is different
                    var propertyLabel = string.IsNullOrWhiteSpace(memberInfo.DisplayName)
                        ? $"{memberInfo.ValueType.Name} {memberInfo.RealName}"
                        : $"{memberInfo.ValueType.Name} {memberInfo.DisplayName} ({memberInfo.RealName})";
        
                    if (!string.IsNullOrWhiteSpace(memberInfo.GroupPrefix))
                        propertyLabel = $"{memberInfo.GroupPrefix}/{propertyLabel}";
        
                    menu.AddItem(new GUIContent(propertyLabel), memberInfo == m_Parameter.MemberInfoForEditor, () =>
                    {
                        if (m_Parameter.MemberInfoForEditor != memberInfo)
                        {
                            m_TreeView.RegisterUndo(Contents.UndoAssignProperty);
                            m_Parameter.MemberInfoForEditor = memberInfo;
                        }
                        
                        UpdateDisplay();
                        m_TreeView.RefreshItemById(m_Parameter.ID);
                    });
                }
                
                var menu = new GenericMenu();
                var (mainInfo, extendedInfo) = ReflectionHelper.GetSupportedMemberInfos(m_Parameter.ReflectedObject);
        
                foreach (var memberInfo in mainInfo)
                {
                    AddPropertyItem(menu, memberInfo);
        
                    if (memberInfo.MemberType == MemberInfoForRuntime.MemberType.This && mainInfo.Count > 1)
                    {
                        menu.AddSeparator(string.Empty);
                    }
                }
        
                if (extendedInfo.Count > 0)
                {
                    menu.AddSeparator(string.Empty);
                }
                
                foreach (var memberInfo in extendedInfo)
                {
                    AddPropertyItem(menu, memberInfo);
                }
                
                menu.DropDown(worldBound);
            }
        }
    }
}
