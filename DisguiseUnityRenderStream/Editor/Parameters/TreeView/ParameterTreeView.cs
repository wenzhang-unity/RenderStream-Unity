using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView : TreeView
    {
        enum Columns
        {
            Name,
            Object,
            Component,
            Property
        }
        
        class ParameterGroupTreeViewItem : TreeViewItem
        {
            public ParameterGroup Group { get; private set; }
            
            public ParameterGroupTreeViewItem(ParameterGroup group, int id, int depth, string displayName) :
                base(id, depth, displayName)
            {
                Group = group;
            }
        }
        
        class ParameterTreeViewItem : TreeViewItem
        {
            public Parameter Parameter { get; private set; }
            
            public ParameterTreeViewItem(Parameter parameter, int id, int depth, string displayName) :
                base(id, depth, displayName)
            {
                Parameter = parameter;
            }
        }

        readonly DisguiseParameterList m_ParameterList;

        public ParameterTreeView(DisguiseParameterList parameterList) :
            base(parameterList.TreeViewState)
        {
            m_ParameterList = parameterList;
            m_PreviousSelection = state.selectedIDs;

            InitializeReflectionInfo();
            
            showAlternatingRowBackgrounds = true;
            this.DeselectOnUnhandledMouseDown(true);
            
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            DragAndDrop.AddDropHandler(SceneDropHandler);
            
            rowHeight = 20;
            customFoldoutYOffset = (20 - EditorGUIUtility.singleLineHeight) * 0.5f;
            extraSpaceBeforeIconAndLabel = 18;

            var multiColumnHeaderState = new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column()
                {
                    headerContent = Contents.NameColumnHeader,
                    width = 120f,
                    minWidth = 120f,
                    canSort = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent = Contents.ObjectColumnHeader,
                    width = 100f,
                    minWidth = 60f,
                    canSort = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent = Contents.ComponentColumnHeader,
                    width = 100f,
                    minWidth = 60f,
                    canSort = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent = Contents.PropertyColumnHeader,
                    width = 100f,
                    minWidth = 60f,
                    canSort = false,
                    allowToggleVisibility = false
                }
            });
            
            multiColumnHeader = new MultiColumnHeader(multiColumnHeaderState)
            {
                canSort = false,
                height = MultiColumnHeader.DefaultGUI.minimumHeight
            };
            multiColumnHeader.ResizeToFit();
            
            Reload();
        }

        public void Destroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            DragAndDrop.RemoveDropHandler(SceneDropHandler);
        }
        
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var allItems = new List<TreeViewItem>();

            foreach (var group in m_ParameterList.m_Groups)
            {
                allItems.Add(new ParameterGroupTreeViewItem(group, group.ID, 0, group.UnityDisplayName));

                foreach (var parameter in group.m_Parameters)
                {
                    allItems.Add(new ParameterTreeViewItem(parameter, parameter.ID, 1, parameter.Name));
                }
            }

            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        void InitializeReflectionInfo()
        {
            foreach (var group in m_ParameterList.m_Groups)
            {
                foreach (var parameter in group.m_Parameters)
                {
                    if (parameter.MemberInfo is { } memberInfo &&
                        ReflectionHelper.TryCreateMemberInfo(memberInfo, out var memberInfoForEditor))
                    {
                        parameter.MemberInfoForEditor = memberInfoForEditor;
                    }
                }
            }
        }
        
        ParameterGroup GetGroupOfParameterItem(ParameterTreeViewItem item)
        {
            if (item.parent is ParameterGroupTreeViewItem groupItem)
            {
                return groupItem.Group;
            }
        
            throw new InvalidOperationException();
        }

        void RegisterUndo(string message)
        {
            Undo.RegisterCompleteObjectUndo(m_ParameterList, message);
        }

        public void CreateNewGroup()
        {
            RegisterUndo(Contents.UndoCreateNewParameterGroup);
            
            var newGroup = new ParameterGroup
            {
                Name = GetUniqueGroupName(Contents.NewGroupName),
                ID = m_ParameterList.ReserveID()
            };
            
            m_ParameterList.m_Groups.Add(newGroup);
            
            Reload();
            
            SelectRevealAndFrame(new []{ newGroup.ID });
        }
        
        /// <summary>
        /// Creates a new parameter for the current selection.
        /// If the current selection is a group, the parameter will be added to it.
        /// If the current selection is a parameter, the new parameter will be added to its group.
        /// If the current selection is empty, a new group will be automatically created.
        /// </summary>
        /// <param name="selectionOverride">Specifies an item to treat as the current selection instead of the current UI selection.</param>
        public void CreateNewParameter(TreeViewItem selectionOverride = null)
        {
            RegisterUndo(Contents.UndoCreateNewParameter);
            
            ParameterGroup targetGroup;

            if (!HasSelection() && selectionOverride == null)
            {
                targetGroup = m_ParameterList.DefaultGroup;
            }
            else
            {
                var item = selectionOverride ?? FindItem(GetSelection()[0], rootItem);
                
                if (item is ParameterGroupTreeViewItem selectedGroupItem)
                {
                    targetGroup = selectedGroupItem.Group;
                }
                else if (item is ParameterTreeViewItem selectedParameterItem)
                {
                    var parentGroup = GetGroupOfParameterItem(selectedParameterItem);
                    targetGroup = parentGroup;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var newParameter = new Parameter
            {
                Name = GetUniqueParameterName(Contents.NewParameterName, targetGroup),
                ID = m_ParameterList.ReserveID()
            };
            
            targetGroup.m_Parameters.Add(newParameter);
            
            Reload();
            
            SelectRevealAndFrame(new []{ newParameter.ID });
        }

        void DeleteItem(TreeViewItem item)
        {
            if (item is ParameterGroupTreeViewItem groupItem)
            {
                // Cannot delete the default group
                if (groupItem.Group != m_ParameterList.DefaultGroup)
                    m_ParameterList.m_Groups.Remove(groupItem.Group);
            }
            else if (item is ParameterTreeViewItem parameterItem)
            {
                var parameterGroup = GetGroupOfParameterItem(parameterItem);
                parameterGroup.m_Parameters.Remove(parameterItem.Parameter);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        
        void DeleteSelection()
        {
            if (!HasSelection())
                return;
            
            RegisterUndo(Contents.UndoDeleteParameter);
            
            foreach (var item in FindRows(GetSelection()))
                DeleteItem(item);
            
            SetSelection(Array.Empty<int>());
            
            Reload();
        }
        
        void DuplicateItem(TreeViewItem item, out int duplicateID)
        {
            if (item is ParameterGroupTreeViewItem groupItem)
            {
                var clone = groupItem.Group.Clone();
                clone.Name = GetUniqueGroupName(clone.Name);
                clone.ID = m_ParameterList.ReserveID();
                m_ParameterList.m_Groups.Add(clone);
                duplicateID = clone.ID;

                foreach (var cloneParameter in clone.m_Parameters)
                {
                    cloneParameter.ID = m_ParameterList.ReserveID();
                }
            }
            else if (item is ParameterTreeViewItem parameterItem)
            {
                var parameterGroup = GetGroupOfParameterItem(parameterItem);
                var clone = parameterItem.Parameter.Clone();
                clone.Name = GetUniqueParameterName(clone.Name, parameterGroup);
                clone.ID = m_ParameterList.ReserveID();
                parameterGroup.m_Parameters.Add(clone);
                duplicateID = clone.ID;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        
        void DuplicateSelection()
        {
            if (!HasSelection())
                return;

            RegisterUndo(Contents.UndoDuplicateParameter);

            var selection = GetSelection();
            var newSelection = new int[selection.Count];
            var i = 0;

            foreach (var item in FindRows(selection))
            {
                DuplicateItem(item, out newSelection[i++]);
            }
            
            Reload();
            
            SelectRevealAndFrame(newSelection);
        }

        void RenameSelection()
        {
            var firstID = GetSelection()[0];
            var firstItem = FindItem(firstID, rootItem);
            BeginRename(firstItem);
        }
    }
}
