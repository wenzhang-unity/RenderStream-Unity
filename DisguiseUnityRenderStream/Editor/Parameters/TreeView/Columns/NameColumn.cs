using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        class NameColumn : ParameterTreeViewColumn<NameColumnCell>
        {
            public NameColumn(ParameterTreeView treeView) :
                base(treeView)
            {
                title = Contents.NameColumnHeader;
                width = 120f;
                minWidth = 120f;
                sortable = false;
                optional = false;
                stretchable = true;
            }
        }
        
        class NameColumnCell : VisualElement, ICell
        {
            readonly Toggle m_Toggle;
            readonly RenameableLabel m_Label;

            ParameterTreeView m_TreeView;
            ItemData m_Data;
            
            public NameColumnCell()
            {
                m_Toggle = new Toggle();
                Add(m_Toggle);
                
                m_Label = new RenameableLabel();
                Add(m_Label);

                style.flexDirection = FlexDirection.Row;
                
                m_Toggle.style.marginTop = StyleKeyword.Auto;
                m_Toggle.style.marginBottom = StyleKeyword.Auto;
                
                m_Label.style.marginTop = StyleKeyword.Auto;
                m_Label.style.marginBottom = StyleKeyword.Auto;

                m_Toggle.RegisterValueChangedCallback(OnToggleValueChanged);

                m_Label.renameStarting += OnStartRenaming;
            }
            
            public void Initialize(ParameterTreeView treeView)
            {
                m_TreeView = treeView;
                
                style.height = m_TreeView.fixedItemHeight;
            }
            
            public void Bind(ItemData data)
            {
                if (data.Group is { } group)
                {
                    m_Toggle.SetValueWithoutNotify(group.Enabled);
                    
                    m_Label.text = group.m_Parameters.Count == 0
                        ? group.UnityDisplayName + Contents.EmptyGroupSuffix
                        : group.UnityDisplayName;

                    style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else if (data.Parameter is { } parameter)
                {
                    m_Toggle.SetValueWithoutNotify(parameter.Enabled);
                    
                    m_Label.text = parameter.Name;
                    
                    style.unityFontStyleAndWeight = FontStyle.Normal;
                }
                
                m_Data = data;
            }

            public void Unbind()
            {
                m_Label.text = string.Empty;
                m_Data = default;
            }

            void OnToggleValueChanged(ChangeEvent<bool> evt)
            {
                if (m_Data.Group is { } group)
                {
                    group.Enabled = evt.newValue;
                    
                    // Groups may affect children
                    m_TreeView.Rebuild();
                }
                else if (m_Data.Parameter is { } parameter)
                {
                    parameter.Enabled = evt.newValue;
                }
            }

            void OnStartRenaming()
            {
                // Drop any qualifiers (ex Contents.EmptyGroupSuffix) for the rename
                
                m_Label.text = m_Data switch
                {
                    { IsGroup: true } => m_Data.Group.Name,
                    { IsParameter: true } => m_Data.Parameter.Name,
                    _ => throw new NotSupportedException()
                };
            }
        }
    }
}
