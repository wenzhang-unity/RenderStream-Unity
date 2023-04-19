using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        class ObjectColumn : ParameterTreeViewColumn<ObjectColumnCell>
        {
            public ObjectColumn(ParameterTreeView treeView) :
                base(treeView)
            {
                title = Contents.ObjectColumnHeader;
                width = 100f;
                minWidth = 60f;
                sortable = false;
                optional = false;
                stretchable = true;
            }
        }
        
        class ObjectColumnCell : ObjectField, ICell
        {
            ParameterTreeView m_TreeView;
            Parameter m_Parameter;
            
            public ObjectColumnCell()
            {
                label = null;
                
                style.marginTop = StyleKeyword.Auto;
                style.marginBottom = StyleKeyword.Auto;
                
                this.SetDisplay(false);

                this.RegisterValueChangedCallback(OnValueChanged);
            }
            
            /// <inheritdoc/>
            public void Initialize(ParameterTreeView treeView)
            {
                m_TreeView = treeView;
            }

            /// <inheritdoc/>
            public void Bind(ItemData data)
            {
                if (data.Parameter is { } parameter)
                {
                    SetValueWithoutNotify(parameter.Object);
                    m_Parameter = parameter;
                    
                    this.SetDisplay(true);
                }
                else
                {
                    SetValueWithoutNotify(null);
                    m_Parameter = null;
                    
                    this.SetDisplay(false);
                }
            }

            /// <inheritdoc/>
            public void Unbind()
            {
                m_Parameter = default;
                SetValueWithoutNotify(null);
            }
            
            private void OnValueChanged(ChangeEvent<UnityEngine.Object> evt)
            {
                if (m_Parameter == null)
                    return;
                
                if (value is Component component)
                {
                    m_TreeView.RegisterUndo(Contents.UndoAssignComponent);
                    m_Parameter.Component = component;
                }
                else
                {
                    m_TreeView.RegisterUndo(Contents.UndoAssignObject);
                    m_Parameter.Object = value;
                }
                
                m_TreeView.RefreshItemById(m_Parameter.ID);
            }
        }
    }
}
