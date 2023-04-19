using System;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Starts renaming the selected item.
        /// </summary>
        void RenameSelection()
        {
            BeginRenameAtIndex(selectedIndex);
        }
        
        /// <inheritdoc/>
        protected override bool CanRename(int index)
        {
            // The same click event that starts a drag and drop interaction can start a rename interaction, cancel the rename
            if (m_IsReorderingItems)
                return false;
            
            var item = GetItemDataForIndex<ItemData>(index);
            
            // Cannot rename the default group
            return item is not { Group: { IsDefaultGroup: true } };
        }

        /// <summary>
        /// Ensures an item is refreshed when leaving a block of code.
        /// </summary>
        readonly struct RefreshItemScope : IDisposable
        {
            readonly ParameterTreeView m_TreeView;
            readonly int m_ItemID;
            
            public RefreshItemScope(ParameterTreeView treeView, int itemID)
            {
                m_TreeView = treeView;
                m_ItemID = itemID;
            }
            
            public void Dispose()
            {
                m_TreeView.RefreshItemById(m_ItemID);
            }
        }

        /// <inheritdoc/>
        protected override void RenameEnded(int id, bool canceled = false)
        {
            using var refreshItemScope = new RefreshItemScope(this, id);

            if (canceled)
                return;

            var root = GetRootElementForId(id);
            var label = root.Q<RenameableLabel>();
            var newName = label.text;
            var item = GetItemDataForId<ItemData>(id);
            
            if (item.Group is { } group)
            {
                if (group.Name == newName)
                    return;

                if (string.IsNullOrWhiteSpace(newName))
                    return;

                RegisterUndo(Contents.UndoRenameParameter);
                
                group.Name = GetUniqueGroupName(newName);
            }
            else if (item.Parameter is { } parameter)
            {
                if (parameter.Name == newName)
                    return;

                // Auto-name
                if (string.IsNullOrEmpty(newName) && parameter.m_HasCustomName)
                {
                    RegisterUndo(Contents.UndoRenameParameter);
                    
                    parameter.m_HasCustomName = false;
                    parameter.AutoAssignName();
                }
                // Custom name
                else
                {
                    RegisterUndo(Contents.UndoRenameParameter);
                    
                    var parameterGroup = GetGroupOfParameter(parameter);
                    parameter.Name = GetUniqueParameterName(newName, parameterGroup);
                    parameter.m_HasCustomName = true;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        
        /// <inheritdoc/>
        protected override string GetItemTextForIndex(int index)
        {
            var itemData = GetItemDataForIndex<ItemData>(index);

            if (itemData.Group is { } group)
                return group.Name;
            else if (itemData.Parameter is { } parameter)
                return parameter.Name;
            else
                throw new NotSupportedException();
        }
        
        /// <summary>
        /// Ensures a unique group name among all groups.
        /// </summary>
        string GetUniqueGroupName(string baseName)
        {
            return ObjectNames.GetUniqueName(m_ParameterList.m_Groups.Select(x => x.Name).ToArray(), baseName);
        }
        
        /// <summary>
        /// Ensures a unique parameter name among a group's parameters.
        /// </summary>
        string GetUniqueParameterName(string baseName, ParameterGroup group)
        {
            return ObjectNames.GetUniqueName(group.m_Parameters.Select(x => x.Name).ToArray(), baseName);
        }
    }
}
