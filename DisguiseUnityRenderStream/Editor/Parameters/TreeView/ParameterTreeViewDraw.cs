using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        void SetupDraw()
        {
            fixedItemHeight = 20f;
            
            showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            
            columns.Add(new NameColumn(this));
            columns.Add(new ObjectColumn(this));
            columns.Add(new ComponentColumn(this));
            columns.Add(new PropertyColumn(this));
        }

        interface ICell
        {
            void Initialize(ParameterTreeView treeView);
            void Bind(ItemData data);
            void Unbind();
        }

        abstract class ParameterTreeViewColumn<T> : Column where T : VisualElement, ICell, new()
        {
            protected readonly ParameterTreeView m_TreeView;
            
            protected ParameterTreeViewColumn(ParameterTreeView treeView)
            {
                m_TreeView = treeView;
                
                makeCell = MakeCell;
                bindCell = BindCell;
                unbindCell = UnbindCell;
                destroyCell = DestroyCell;
            }

            VisualElement MakeCell()
            {
                var cell = new T();
                cell.Initialize(m_TreeView);

                m_TreeView.MakeItem(cell);
                
                return cell;
            }

            void BindCell(VisualElement ve, int index)
            {
                var data = m_TreeView.GetItemDataForIndex<ItemData>(index);
                
                m_TreeView.BindItem(ve, index);
                
                var cell = (ICell)ve;
                cell.Bind(data);
            }

            void UnbindCell(VisualElement ve, int index)
            {
                var cell = (ICell)ve;
                cell.Unbind();

                m_TreeView.UnbindItem(ve, index);
            }
            
            void DestroyCell(VisualElement ve)
            {
                m_TreeView.DestroyItem(ve);
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
            
            public void Initialize(ParameterTreeView treeView)
            {
                m_TreeView = treeView;
            }

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
                else if (IsMissingComponent(m_Parameter.Component))
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
        
        static Texture ResolveComponentIcon(Component component, Texture noneIcon, Texture fallbackIcon)
        {
            var icon = noneIcon;

            if (component != null && !IsMissingComponent(component))
            {
                icon = EditorGUIUtility.ObjectContent(null, component.GetType()).image;
                if (icon == null)
                    icon = fallbackIcon;
            }

            return icon;
        }

        static bool IsMissingReference<T>(T obj) where T : UnityEngine.Object
        {
            // Unity has overriden the == operator to check for destroyed objects
            return obj is T && obj == null;
        }

        static bool IsMissingComponent(UnityEngine.Object component)
        {
            return component.GetType() == typeof(Component);
        }
    }
}
