using UnityEditor;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        class ComponentColumn : ParameterTreeViewColumn<ComponentColumnCell>
        {
            public ComponentColumn(ParameterTreeView treeView) :
                base(treeView)
            {
                title = Contents.ComponentColumnHeader;
                width = 100f;
                minWidth = 60f;
                sortable = false;
                optional = false;
                stretchable = true;
            }
        }
        
        class ComponentColumnCell : DropdownButtonCell
        {
            public ComponentColumnCell()
            {
                ShowIcon = true;
            }

            protected override void UpdateDisplay()
            {
                if (m_Parameter is not { Object: GameObject go }|| go == null)
                {
                    this.SetDisplay(false);
                    return;
                }
                
                this.SetDisplay(true);
                
                string componentButtonLabel;
                
                if (IsMissingReference(m_Parameter.Component))
                    componentButtonLabel = Contents.DropdownMissingComponentLabel;
                else if (m_Parameter.Component == null)
                    componentButtonLabel = Contents.DropdownNoneLabel;
                else if (IsMissingComponentScript(m_Parameter.Component))
                    componentButtonLabel = Contents.DropdownMissingScriptLabel;
                else
                    componentButtonLabel = ObjectNames.NicifyVariableName(m_Parameter.Component.GetType().Name);

                textElement.text = componentButtonLabel;
                
                var componentIcon = ResolveComponentIcon(m_Parameter.Component, Contents.WarningIcon, Contents.GameObjectIcon);
                Icon = componentIcon;
            }

            protected override void ShowGenericMenu()
            {
                if (m_Parameter is not { Object: GameObject go } || go == null)
                    return;
                
                var components = go.GetComponents<Component>();
                
                var menu = new GenericMenu();
                
                foreach (var component in components)
                {
                    // This is a missing component reference (ex deleted script), ignore it
                    if (component == null)
                        continue;
                    
                    var componentLabel = ObjectNames.NicifyVariableName(component.GetType().Name);
                    
                    menu.AddItem(new GUIContent(componentLabel), component == m_Parameter.Component, () =>
                    {
                        if (m_Parameter.Component != component)
                        {
                            m_TreeView.RegisterUndo(Contents.UndoAssignComponent);
                            m_Parameter.Component = component;
                        }
                        
                        UpdateDisplay();
                        m_TreeView.RefreshItemById(m_Parameter.ID);
                    });
                }
                
                menu.DropDown(worldBound);
            }
        }
    }
}
