using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView : TreeViewExtended
    {
        public class ItemData
        {
            public bool IsGroup => m_Group != null;
            public bool IsParameter => m_Parameter != null;

            public ParameterGroup Group => m_Group;
            public Parameter Parameter => m_Parameter;
            
            readonly ParameterGroup m_Group;
            readonly Parameter m_Parameter;

            public ItemData(ParameterGroup group)
            {
                m_Group = group;
                m_Parameter = default;
            }

            public ItemData(Parameter parameter)
            {
                m_Group = default;
                m_Parameter = parameter;
            }
        }

        DisguiseParameterList m_ParameterList;
        ITreeViewStateStorage m_StateStorage;
        readonly Dictionary<Parameter, ParameterGroup> m_ParameterGroups = new Dictionary<Parameter, ParameterGroup>();
        string m_SearchString;

        public ParameterTreeView()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            SetupSelection();
            SetupDraw();
            // SetupDragAndDrop();
        }

        public void SetData(DisguiseParameterList parameterList, ITreeViewStateStorage stateStorage)
        {
            m_ParameterList = parameterList;
            m_StateStorage = stateStorage;

            InitializeReflectionInfo();

            var rootItems = GenerateDataTree();
            SetRootItems(rootItems);
            
            Rebuild();
        }

        public void Destroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            // DragAndDrop.RemoveDropHandler(SceneDropHandler); (ShutdownDragAndDrop();
        }
        
        List<TreeViewItemData<ItemData>> GenerateDataTree()
        {
            m_ParameterGroups.Clear();
            
            var allItems = new List<TreeViewItemData<ItemData>>();

            foreach (var group in m_ParameterList.m_Groups)
            {
                var groupItemData = new ItemData(group);
                var groupMatchesSearch = !HasSearch || MatchesSearch(groupItemData, SearchString);
                
                var childItems = new List<TreeViewItemData<ItemData>>();

                foreach (var parameter in group.m_Parameters)
                {
                    var parameterItemData = new ItemData(parameter);
                    if (HasSearch && !MatchesSearch(parameterItemData, SearchString))
                        continue;

                    groupMatchesSearch = true;
                    var parameterItem = new TreeViewItemData<ItemData>(parameter.ID, parameterItemData);
                    childItems.Add(parameterItem);
                    
                    m_ParameterGroups.Add(parameter, group);
                }

                if (groupMatchesSearch)
                {
                    var groupItem = new TreeViewItemData<ItemData>(group.ID, groupItemData, childItems);
                    allItems.Add(groupItem);
                }
            }

            return allItems;
        }

        void InitializeReflectionInfo()
        {
            foreach (var group in m_ParameterList.m_Groups)
            {
                foreach (var parameter in group.m_Parameters)
                {
                    parameter.RefreshMemberInfoForEditor();
                }
            }
        }

        void RegisterUndo(string message)
        {
            Undo.RegisterCompleteObjectUndo(m_ParameterList, message);
        }

        void RebuildAfterUndoRedo()
        {
            SetData(m_ParameterList, m_StateStorage);
        }
        
        ParameterGroup GetGroupOfParameter(Parameter parameter)
        {
            return m_ParameterGroups[parameter];
        }

        public void CreateNewGroup()
        {
            RegisterSelectionUndoRedo();
            RegisterUndo(Contents.UndoCreateNewParameterGroup);
            
            var newGroup = new ParameterGroup
            {
                Name = GetUniqueGroupName(Contents.NewGroupName),
                ID = m_ParameterList.ReserveID()
            };
            
            m_ParameterList.m_Groups.Add(newGroup);
            
            AddGroupToTree(newGroup);
            
            SelectRevealAndFrame(new []{ newGroup.ID });
        }
        
        /// <summary>
        /// Creates a new parameter for the current selection.
        /// If the current selection is a group, the parameter will be added to it.
        /// If the current selection is a parameter, the new parameter will be added to its group.
        /// If the current selection is empty, a new group will be automatically created.
        /// </summary>
        /// <param name="selectionOverride">Specifies an item to treat as the current selection instead of the current UI selection.</param>
        public void CreateNewParameter(ItemData selectionOverride = null)
        {
            RegisterSelectionUndoRedo();
            RegisterUndo(Contents.UndoCreateNewParameter);
            
            ParameterGroup targetGroup;

            if (!HasSelection() && selectionOverride != null)
            {
                targetGroup = m_ParameterList.DefaultGroup;
            }
            else
            {
                var item = selectionOverride ?? (ItemData)selectedItem;
                
                if (item.Group is { } selectedGroup)
                {
                    targetGroup = selectedGroup;
                }
                else if (item.Parameter is { } selectedParameter)
                {
                    var parentGroup = GetGroupOfParameter(selectedParameter);
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
            
            AddParameterToTree(newParameter, targetGroup);
            
            schedule.Execute(() => SelectRevealAndFrame(new []{ newParameter.ID }));
        }

        protected override void DeleteSelectedItems()
        {
            if (!HasSelection())
                return;
            
            RegisterSelectionUndoRedo();
            RegisterUndo(Contents.UndoDeleteParameter);
            
            var toDelete = selectedItems.ToList();
            
            foreach (var item in toDelete)
            {
                var itemData = (ItemData)item;

                if (itemData.Group is { } group)
                {
                    // Cannot delete the default group
                    if (group != m_ParameterList.DefaultGroup)
                    {
                        m_ParameterList.m_Groups.Remove(group);
                        TryRemoveItem(group.ID);
                    }
                }
                else if (itemData.Parameter is { } parameter)
                {
                    var parameterGroup = GetGroupOfParameter(parameter);
                    parameterGroup.m_Parameters.Remove(parameter);
                    TryRemoveItem(parameter.ID);
                }
            }
        }

        protected override void DuplicateSelectedItems()
        {
            if (!HasSelection())
                return;
            
            RegisterSelectionUndoRedo();
            RegisterUndo(Contents.UndoDuplicateParameter);
            
            var selection = selectedItems.ToList();
            var newSelection = new int[selection.Count];
            var i = 0;
            
            foreach (var item in selection)
            {
                var itemData = (ItemData)item;
                DuplicateItem(itemData, out newSelection[i++]);
            }
            
            SelectRevealAndFrame(newSelection);
        }
        
        void DuplicateItem(ItemData item, out int duplicateID)
        {
            if (item.Group is { } group)
            {
                var clone = group.Clone();
                clone.Name = GetUniqueGroupName(clone.Name);
                clone.ID = m_ParameterList.ReserveID();
                m_ParameterList.m_Groups.Add(clone);
                duplicateID = clone.ID;
                
                AddGroupToTree(clone);
        
                foreach (var cloneParameter in clone.m_Parameters)
                {
                    cloneParameter.ID = m_ParameterList.ReserveID();
                    
                    AddParameterToTree(cloneParameter, clone);
                }
            }
            else if (item.Parameter is { } parameter)
            {
                var parameterGroup = GetGroupOfParameter(parameter);
                var clone = parameter.Clone();
                clone.Name = GetUniqueParameterName(clone.Name, parameterGroup);
                clone.ID = m_ParameterList.ReserveID();
                parameterGroup.m_Parameters.Add(clone);
                duplicateID = clone.ID;
                
                AddParameterToTree(clone, parameterGroup);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        
        void AddGroupToTree(ParameterGroup group)
        {
            var item = new TreeViewItemData<ItemData>(group.ID, new ItemData(group));
            AddItem(item);
        }
        
        void AddParameterToTree(Parameter parameter, ParameterGroup group)
        {
            var item = new TreeViewItemData<ItemData>(parameter.ID, new ItemData(parameter));
            AddItem(item, group.ID);
        }
        
        /// <summary>
        /// Imitates IMGUI's TreeViewControl.searchString
        /// </summary>
        public string SearchString
        {
            get => m_SearchString;
            set
            {
                if (m_SearchString != value)
                {
                    m_SearchString = value;
                    
                    RebuildAfterUndoRedo();
                }
            }
        }

        /// <summary>
        /// Imitates IMGUI's TreeViewControl.hasSearch
        /// </summary>
        public bool HasSearch => !string.IsNullOrWhiteSpace(m_SearchString);

        /// <summary>
        /// Imitates IMGUI's TreeViewControl.searchString behavior
        /// </summary>
        bool MatchesSearch(ItemData item, string searchString)
        {
            var itemName = item switch
            {
                { IsGroup: true } => item.Group.Name,
                { IsParameter: true } => item.Parameter.Name,
                _ => throw new NotSupportedException()
            };
            
            return itemName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
