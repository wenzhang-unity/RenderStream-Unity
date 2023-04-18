using System;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        void RenameSelection()
        {
            BeginRenameAtIndex(selectedIndex);
        }
        
        protected override bool CanRename(int index)
        {
            var item = GetItemDataForIndex<ItemData>(index);
            
            // Cannot rename the default group
            return item is not { Group: { IsDefaultGroup: true } };
        }

        protected override void RenameEnded(int id, bool canceled = false)
        {
            var index = viewController.GetIndexForId(id);
            
            if (canceled)
            {
                RefreshItem(index);
                return;
            }

            var root = GetRootElementForId(id);
            var label = root.Q<RenameableLabel>();
            var newName = label.text;
            var item = GetItemDataForId<ItemData>(id);
            
            if (item.Group is { } group)
            {
                if (group.Name == newName)
                {
                    RefreshItem(index);
                    return;
                }

                if (string.IsNullOrWhiteSpace(newName))
                {
                    RefreshItem(index);
                    return;
                }

                RegisterUndo(Contents.UndoRenameParameter);
                
                group.Name = GetUniqueGroupName(newName);
            }
            else if (item.Parameter is { } parameter)
            {
                if (parameter.Name == newName)
                {
                    RefreshItem(index);
                    return;
                }

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
            
            RefreshItem(index);
        }
        
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
        
        string GetUniqueGroupName(string baseName)
        {
            return ObjectNames.GetUniqueName(m_ParameterList.m_Groups.Select(x => x.Name).ToArray(), baseName);
        }
        
        string GetUniqueParameterName(string baseName, ParameterGroup group)
        {
            return ObjectNames.GetUniqueName(group.m_Parameters.Select(x => x.Name).ToArray(), baseName);
        }
    }
}
