using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Draws a row in the tree.
        /// </summary>
        protected override void RowGUI(RowGUIArgs args)
        {
            Contents.OnGUILazyLoad();
            
            if (args.item is ParameterGroupTreeViewItem groupItem)
            {
                GroupRowGUI(args, groupItem.Group);
            }
            else if (args.item is ParameterTreeViewItem parameterItem)
            {
                // Refresh displayName in case the auto-name has changed
                args.item.displayName = parameterItem.Parameter.Name;
                
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    ParameterCellGUI(args.GetCellRect(i), parameterItem.Parameter, (Columns)args.GetColumn(i), ref args);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        
        /// <summary>
        /// Draws a cell for a Parameter row.
        /// </summary>
        void ParameterCellGUI(Rect rect, Parameter parameter, Columns column, ref RowGUIArgs args)
        {
            switch (column)
            {
                case Columns.Name:
                    ParameterNameGUI(rect, parameter, ref args);
                    break;
                
                case Columns.Object:
                    ParameterObjectGUI(rect, parameter, ref args);
                    break;
                
                case Columns.Component:
                    ParameterComponentGUI(rect, parameter, ref args);
                    break;
                
                case Columns.Property:
                    ParameterPropertyGUI(rect, parameter, ref args);
                    break;
                
                default:
                    throw new NotImplementedException();
            }
        }
        
        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            if (item is ParameterGroupTreeViewItem)
            {
                return base.GetRenameRect(rowRect, row, item);
            }
            else if (item is ParameterTreeViewItem)
            {
                var cellRect = GetCellRectForTreeFoldouts(rowRect);
                CenterRectUsingSingleLineHeight(ref cellRect);
                return base.GetRenameRect(cellRect, row, item);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void GroupRowGUI(RowGUIArgs args, ParameterGroup group)
        {
            var label = group.m_Parameters.Count == 0
                ? args.label + Contents.EmptyGroupSuffix
                : args.label;
                
            var rect = args.rowRect;
            rect.xMin += GetContentIndent(args.item) + extraSpaceBeforeIconAndLabel;
            rect.yMin += 2f;
                
            var toggleRect = rect;
            toggleRect.xMin -= 16f;
            toggleRect.width = 18f;
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var enabled = EditorGUI.Toggle(toggleRect, group.Enabled);

                if (check.changed)
                {
                    RegisterUndo(Contents.UndoToggleEnableParameterGroup);
                    group.Enabled = enabled;
                }
            }
                
            // GUIStyle is only designed to handle the Repaint event
            if (Event.current.rawType == EventType.Repaint)
                DefaultStyles.boldLabel.Draw(rect, label, false, false, args.selected, args.focused);
        }

        void ParameterNameGUI(Rect rect, Parameter parameter, ref RowGUIArgs args)
        {
            rect.xMin += GetContentIndent(args.item) + extraSpaceBeforeIconAndLabel;
                    
            var toggleRect = rect;
            toggleRect.xMin -= 18f;
            toggleRect.width = 18f;
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var enabled = EditorGUI.Toggle(toggleRect, parameter.Enabled);

                if (check.changed)
                {
                    RegisterUndo(Contents.UndoToggleEnableParameter);
                    parameter.Enabled = enabled;
                }
            }
                    
            EditorGUI.LabelField(rect, args.label);
        }
        
        void ParameterObjectGUI(Rect rect, Parameter parameter, ref RowGUIArgs args)
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var selectedObject = EditorGUI.ObjectField(rect, GUIContent.none, parameter.Object, typeof(UnityEngine.Object), true);

                if (check.changed)
                {
                    if (selectedObject is Component component)
                    {
                        RegisterUndo(Contents.UndoAssignComponent);
                        parameter.Component = component;
                    }
                    else
                    {
                        RegisterUndo(Contents.UndoAssignObject);
                        parameter.Object = selectedObject;
                    }
                }
            }
        }
        
        void ParameterComponentGUI(Rect rect, Parameter parameter, ref RowGUIArgs args)
        {
            if (parameter.Object is GameObject go && go != null)
            {
                string componentButtonLabel;

                if (parameter.Component == null)
                    componentButtonLabel = Contents.DropdownNoneLabel;
                else if (IsMissingComponent(parameter.Component))
                    componentButtonLabel = Contents.DropdownMissingScriptLabel;
                else
                    componentButtonLabel = ObjectNames.NicifyVariableName(parameter.Component.GetType().Name);
                
                if (GUI.Button(rect, componentButtonLabel, Contents.ComponentPopupStyle))
                {
                    var components = go.GetComponents<Component>();

                    var menu = new GenericMenu();

                    foreach (var component in components)
                    {
                        // This is a missing component reference (ex deleted script), ignore it
                        if (component == null)
                            continue;
                        
                        var componentLabel = ObjectNames.NicifyVariableName(component.GetType().Name);
                        
                        menu.AddItem(new GUIContent(componentLabel), component == parameter.Component, () =>
                        {
                            if (parameter.Component != component)
                            {
                                RegisterUndo(Contents.UndoAssignComponent);
                                parameter.Component = component;
                            }
                        });
                    }

                    menu.DropDown(rect);
                }
                
                var iconRect = rect;
                iconRect.width = 16f;
                iconRect.x += 2f;
                DrawComponentIcon(iconRect, parameter.Component, Contents.WarningIcon, Contents.GameObjectIcon);
            }
        }
        
        void ParameterPropertyGUI(Rect rect, Parameter parameter, ref RowGUIArgs args)
        {
            if (parameter.ReflectedObject != null && !IsMissingComponent(parameter.ReflectedObject))
            {
                var propertyButtonContent = new GUIContent();

                // Show the property name or a warning icon if invalid
                if (parameter.MemberInfoForEditor.IsValid())
                {
                    propertyButtonContent.text = parameter.MemberInfoForEditor.UINameWithGroupPrefix;
                }
                else
                {
                    propertyButtonContent.text = Contents.DropdownNoneLabel;
                    propertyButtonContent.image = Contents.WarningIcon;
                }

                if (GUI.Button(rect, propertyButtonContent, Contents.PropertyPopupStyle) && parameter.ReflectedObject != null)
                {
                    void AddPropertyItem(GenericMenu menu, MemberInfoForEditor memberInfo)
                    {
                        // Show the real name in parentheses when the display name is different
                        var propertyLabel = string.IsNullOrWhiteSpace(memberInfo.DisplayName)
                            ? $"{memberInfo.ValueType.Name} {memberInfo.RealName}"
                            : $"{memberInfo.ValueType.Name} {memberInfo.DisplayName} ({memberInfo.RealName})";

                        if (!string.IsNullOrWhiteSpace(memberInfo.GroupPrefix))
                            propertyLabel = $"{memberInfo.GroupPrefix}/{propertyLabel}";

                        menu.AddItem(new GUIContent(propertyLabel), memberInfo == parameter.MemberInfoForEditor, () =>
                        {
                            if (parameter.MemberInfoForEditor != memberInfo)
                            {
                                RegisterUndo(Contents.UndoAssignProperty);
                                parameter.MemberInfoForEditor = memberInfo;
                            }
                        });
                    }
                    
                    var menu = new GenericMenu();
                    var (mainInfo, extendedInfo) = ReflectionHelper.GetSupportedMemberInfos(parameter.ReflectedObject);

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

                    menu.DropDown(rect);
                }
            }
        }

        void DrawComponentIcon(Rect rect, Component component, Texture noneIcon, Texture fallbackIcon)
        {
            var icon = noneIcon;

            if (component != null && !IsMissingComponent(component))
            {
                icon = EditorGUIUtility.ObjectContent(null, component.GetType()).image;
                if (icon == null)
                    icon = fallbackIcon;
            }
                
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
        }

        bool IsMissingComponent(UnityEngine.Object component)
        {
            return component.GetType() == typeof(Component);
        }
    }
}
