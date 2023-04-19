using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        abstract class DropdownButtonCell : DropdownField, ICell
        {
            protected ParameterTreeView m_TreeView;
            protected Parameter m_Parameter;
            readonly Action m_AsyncOnProjectOrHierarchyChangedCallback;

            Image m_Icon;
            bool m_ShowIcon;

            public bool ShowIcon
            {
                get => m_ShowIcon;
                set
                {
                    if (m_ShowIcon == value)
                        return;
                    
                    m_ShowIcon = value;

                    if (m_ShowIcon)
                    {
                        m_Icon.SetDisplay(true);
                        textElement.style.paddingLeft = 17f;
                    }
                    else
                    {
                        m_Icon.SetDisplay(false);
                        textElement.style.paddingLeft = 0f;
                    }
                }
            }

            public Texture Icon
            {
                get => m_Icon.image;
                set => m_Icon.image = value;
            }
            
            protected DropdownButtonCell()
            {
                label = null;
                
                style.marginTop = StyleKeyword.Auto;
                style.marginBottom = StyleKeyword.Auto;
                
                this.SetDisplay(false);

                var clickable = new Clickable(OnClick);
                this.AddManipulator(clickable);
                
                RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
                
                m_AsyncOnProjectOrHierarchyChangedCallback = () => schedule.Execute(OnProjectOrHierarchyChangedCallback);
                
                RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    EditorApplication.projectChanged += m_AsyncOnProjectOrHierarchyChangedCallback;
                    EditorApplication.hierarchyChanged += m_AsyncOnProjectOrHierarchyChangedCallback;
                });
                RegisterCallback<DetachFromPanelEvent>(_ =>
                {
                    EditorApplication.projectChanged -= m_AsyncOnProjectOrHierarchyChangedCallback;
                    EditorApplication.hierarchyChanged -= m_AsyncOnProjectOrHierarchyChangedCallback;
                });

                SetupIcon();
            }
            
            public void Initialize(ParameterTreeView treeView)
            {
                m_TreeView = treeView;
                
                style.height = m_TreeView.fixedItemHeight;
            }

            void SetupIcon()
            {
                m_Icon = new Image
                {
                    scaleMode = ScaleMode.ScaleToFit,
                    style =
                    {
                        position = Position.Absolute,
                        left = 2f,
                        width = 16f,
                        height = 16f,
                        marginTop = StyleKeyword.Auto,
                        marginBottom = StyleKeyword.Auto,
                        alignSelf = Align.Center
                    }
                };
                
                m_Icon.SetDisplay(false);
                
                Add(m_Icon);
            }

            public void Bind(ItemData data)
            {
                if (data.Parameter is { } parameter)
                {
                    m_Parameter = parameter;
                }
                else
                {
                    m_Parameter = null;
                }
                
                UpdateDisplay();
            }

            public void Unbind()
            {
                m_Parameter = default;
                SetValueWithoutNotify(null);
                
                UpdateDisplay();
            }

            void OnProjectOrHierarchyChangedCallback()
            {
                UpdateDisplay();
            }

            void OnClick()
            {
                ShowGenericMenu();
            }

            void OnNavigationSubmit(NavigationSubmitEvent evt)
            {
                ShowGenericMenu();
                evt.StopPropagation();
            }
            
            protected abstract void UpdateDisplay();

            protected abstract void ShowGenericMenu();
        }
    }
}
