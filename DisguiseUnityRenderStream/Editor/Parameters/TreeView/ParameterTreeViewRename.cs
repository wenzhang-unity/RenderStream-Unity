using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Checks if the specified item supports renaming.
        /// </summary>
        protected override bool CanRename(TreeViewItem item)
        {
            // Cannot rename the default group
            return item is not ParameterGroupTreeViewItem { Group: { IsDefaultGroup: true } };
        }

        /// <summary>
        /// Applies renaming onto an item.
        /// </summary>
        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename)
                return;
            
            if (args.newName == args.originalName)
                return;

            var item = FindItem(args.itemID, rootItem);
            
            if (item is ParameterGroupTreeViewItem groupItem)
            {
                if (string.IsNullOrWhiteSpace(args.newName))
                    return;
                
                RegisterUndo(Contents.UndoRenameParameter);
                
                groupItem.Group.Name = GetUniqueGroupName(args.newName);
                
                Reload();
            }
            else if (item is ParameterTreeViewItem parameterItem)
            {
                // Auto-name
                if (string.IsNullOrEmpty(args.newName) && parameterItem.Parameter.m_HasCustomName)
                {
                    RegisterUndo(Contents.UndoRenameParameter);
                    
                    parameterItem.Parameter.m_HasCustomName = false;
                    parameterItem.Parameter.AutoAssignName();
                    
                    Reload();
                }
                // Custom name
                else
                {
                    RegisterUndo(Contents.UndoRenameParameter);
                    
                    var parameterGroup = GetGroupOfParameterItem(parameterItem);
                    parameterItem.Parameter.Name = GetUniqueParameterName(args.newName, parameterGroup);
                    parameterItem.Parameter.m_HasCustomName = true;
                    
                    Reload();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
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
