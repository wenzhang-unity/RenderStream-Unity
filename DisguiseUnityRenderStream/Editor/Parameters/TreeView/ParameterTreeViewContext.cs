using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Handle right-click on empty area.
        /// </summary>
        protected override void ContextClickedEmptyArea(DropdownMenu menu)
        {
            menu.AppendAction(Contents.ContextMenuCreateNewGroup, x => CreateNewGroup());
            menu.AppendAction(Contents.ContextMenuCreateNewParameter, x => CreateNewParameter());
        }
        
        /// <summary>
        /// Handle right-click on an item.
        /// </summary>
        protected override void ContextClicked(DropdownMenu menu)
        {
            var selection = selectedItems.ToList();

            if (selection.Count == 1)
            {
                var item = (ItemData)selection[0];
                HandleSingleItemContext(menu, item);
            }
            else
            {
                HandleMultipleItemsContext(menu);
            }
        }

        void HandleSingleItemContext(DropdownMenu menu, ItemData item)
        {
            if (item.Group is { } group)
            {
                var disabledIfDefaultGroup = group.IsDefaultGroup
                    ? DropdownMenuAction.Status.Disabled
                    : DropdownMenuAction.Status.Normal;
                
                menu.AppendAction(Contents.ContextMenuAddNewParameter, x => CreateNewParameter(item));
                menu.AppendAction(Contents.ContextMenuRename, x => RenameSelection(), disabledIfDefaultGroup);
                menu.AppendAction(Contents.ContextMenuDuplicate, x => DuplicateSelectedItems());
                menu.AppendSeparator(string.Empty);
                menu.AppendAction(Contents.ContextMenuDelete, x => DeleteSelectedItems(), disabledIfDefaultGroup);
            }
            else if (item.Parameter is { } parameter)
            {
                menu.AppendAction(Contents.ContextMenuRename, x => RenameSelection());
                menu.AppendAction(Contents.ContextMenuDuplicate, x => DuplicateSelectedItems());
                menu.AppendSeparator(string.Empty);
                menu.AppendAction(Contents.ContextMenuDelete, x => DeleteSelectedItems());
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void HandleMultipleItemsContext(DropdownMenu menu)
        {
            menu.AppendAction(Contents.ContextMenuDuplicate, x => DuplicateSelectedItems());
            menu.AppendSeparator(string.Empty);
            menu.AppendAction(Contents.ContextMenuDelete, x => DeleteSelectedItems());
        }
    }
}
