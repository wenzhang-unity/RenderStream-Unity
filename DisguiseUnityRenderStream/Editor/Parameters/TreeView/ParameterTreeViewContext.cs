using System;
using UnityEditor;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Handle right-click on empty area.
        /// </summary>
        protected override void ContextClicked()
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent(Contents.ContextMenuCreateNewGroup), false, CreateNewGroup);
            menu.AddItem(new GUIContent(Contents.ContextMenuCreateNewParameter), false, () => CreateNewParameter());
            
            menu.ShowAsContext();
            Event.current.Use();
        }
        
        /// <summary>
        /// Handle right-click on an item.
        /// </summary>
        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            var menu = new GenericMenu();
        
            if (item is ParameterGroupTreeViewItem groupItem)
            {
                menu.AddItem(new GUIContent(Contents.ContextMenuAddNewParameter), false, () => CreateNewParameter(item));
                if (groupItem.Group.IsDefaultGroup)
                {
                    // Disable rename and delete on the default group
                    menu.AddDisabledItem(new GUIContent(Contents.ContextMenuRename), false);
                    menu.AddItem(new GUIContent(Contents.ContextMenuDuplicate), false, DuplicateSelection);
                    menu.AddSeparator(string.Empty);
                    menu.AddDisabledItem(new GUIContent(Contents.ContextMenuDelete), false);
                }
                else
                {
                    menu.AddItem(new GUIContent(Contents.ContextMenuRename), false, RenameSelection);
                    menu.AddItem(new GUIContent(Contents.ContextMenuDuplicate), false, DuplicateSelection);
                    menu.AddSeparator(string.Empty);
                    menu.AddItem(new GUIContent(Contents.ContextMenuDelete), false, DeleteSelection);
                }
            }
            else if (item is ParameterTreeViewItem)
            {
                menu.AddItem(new GUIContent(Contents.ContextMenuRename), false, RenameSelection);
                menu.AddItem(new GUIContent(Contents.ContextMenuDuplicate), false, DuplicateSelection);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent(Contents.ContextMenuDelete), false, DeleteSelection);
            }
            else
            {
                throw new NotImplementedException();
            }
            
            menu.ShowAsContext();
            Event.current.Use();
        }
    }
}
