using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// True while drag &amp; drop contains items from this tree.
        /// <remarks>
        /// Not entirely accurate (<see cref="OnDragExited"/>) but good enough for our purposes.
        /// We only care about the first 500ms of a drag &amp; drop interaction to prevent <see cref="RenameableLabel"/> from activating.
        /// </remarks>
        /// </summary>
        bool m_IsReorderingItems;
        
        /// <summary>
        /// Called when drag & drop is released or exits the current panel. Good enough for our purposes.
        /// </summary>
        /// <param name="evt"></param>
        void OnDragExited(DragExitedEvent evt)
        {
            m_IsReorderingItems = false;
        }
        
        /// <summary>
        /// Prevents drag & drop when the default group is selected.
        /// </summary>
        bool CanStartDrag(CanStartDragArgs args)
        {
            foreach (var item in selectedItems.Cast<ItemData>())
            {
                // Cannot re-order the default group
                if (item is { Group: { IsDefaultGroup: true } })
                    return false;
            }

            return true;
        }
        
        /// <summary>
        /// Configures a drag & drop originating from this tree.
        /// </summary>
        StartDragArgs SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var selection = args.selectedIds.Select(GetItemDataForId<ItemData>).Where(x => x != null);

            m_IsReorderingItems = true;

            var startArgs = new StartDragArgs(Contents.DragTitle, DragVisualMode.Move);
            startArgs.SetGenericData(Contents.DragDataKey, selection.ToArray());
            return startArgs;
        }
        
        /// <summary>
        /// Called while items are being re-ordered in the tree.
        /// </summary>
        DragVisualMode DragAndDropUpdateReorder(HandleDragAndDropArgs args, IEnumerable<ItemData> data)
        {
            var firstItem = data.First();
            var parentItem = GetItemDataForId<ItemData>(args.parentId);
            
            switch (args.dropPosition)
            {
                case DragAndDropPosition.OverItem:
                    return (firstItem, parentItem) switch
                    {
                        { firstItem: { IsParameter: true }, parentItem: { IsGroup: true } } => DragVisualMode.Move, // Drop parameter(s) on a group
                        _ => DragVisualMode.Rejected
                    };
            
                case DragAndDropPosition.BetweenItems:
                    // Cannot place anything before the default group
                    if (args.insertAtIndex == 0)
                        return DragVisualMode.Rejected;
                    
                    return (firstItem, parentItem) switch
                    {
                        { firstItem: { IsGroup: true }, parentItem: null } => DragVisualMode.Move, // Drop group(s) between groups
                        { firstItem: { IsParameter: true }, parentItem: { IsGroup: true } } => DragVisualMode.Move, // Drop parameter(s) between parameters
                        _ => DragVisualMode.Rejected
                    };
            
                case DragAndDropPosition.OutsideItems:
                    return firstItem.IsParameter
                        ? DragVisualMode.Move // Drop parameter(s) on an empty space
                        : DragVisualMode.Rejected;
            
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Called after items have been re-ordered in the tree.
        /// </summary>
        DragVisualMode HandleDropReorder(HandleDragAndDropArgs args, IList<ItemData> data, out int[] newSelection)
        {
            DragVisualMode mode = args.dropPosition switch
            {
                DragAndDropPosition.OverItem => ReorderOntoItem(args, data, out newSelection),
                DragAndDropPosition.BetweenItems => ReorderBetweenItems(args, data, out newSelection),
                DragAndDropPosition.OutsideItems => ReorderOutsideItems(args, data, out newSelection),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            return mode;
        }
        
        DragVisualMode ReorderOntoItem(HandleDragAndDropArgs args, IList<ItemData> data, out int[] newSelection)
        {
            var parentItem = GetItemDataForId<ItemData>(args.parentId);

            if (parentItem.Group is { } newGroup)
            {
                RegisterUndo(Contents.UndoDragAndDropReorderParameters);
                
                foreach (var item in data)
                {
                    var oldGroup = GetGroupOfParameter(item.Parameter);
                    ReparentParameter(item.Parameter, oldGroup, newGroup);
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
            
            newSelection = data.Select(x => x.Parameter.ID).ToArray();
            return DragVisualMode.Move;
        }
        
        DragVisualMode ReorderBetweenItems(HandleDragAndDropArgs args, IList<ItemData> data, out int[] newSelection)
        {
            var parentItem = GetItemDataForId<ItemData>(args.parentId);

            if (parentItem == null)
            {
                RegisterUndo(Contents.UndoDragAndDropReorderParameters);
                newSelection = data.Select(x => x.Group.ID).ToArray();
                
                var insertAtIndex = args.childIndex;

                foreach (var item in data)
                {
                    ReorderGroup(item.Group, insertAtIndex++);
                }
            }
            else if (parentItem.Group is { } newGroup)
            {
                RegisterUndo(Contents.UndoDragAndDropReorderParameters);
                newSelection = data.Select(x => x.Parameter.ID).ToArray();
                
                var insertAtIndex = args.childIndex;
                
                foreach (var item in data)
                {
                    var oldGroup = GetGroupOfParameter(item.Parameter);
                    ReparentParameter(item.Parameter, oldGroup, newGroup, insertAtIndex);
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
            
            return DragVisualMode.Move;
        }
        
        DragVisualMode ReorderOutsideItems(HandleDragAndDropArgs args, IList<ItemData> data, out int[] newSelection)
        {
            RegisterUndo(Contents.UndoDragAndDropReorderParameters);
        
            var newGroup = new ParameterGroup
            {
                Name = GetUniqueGroupName(Contents.NewGroupName),
                ID = m_ParameterList.ReserveID()
            };
            
            m_ParameterList.m_Groups.Add(newGroup);
            AddGroupToTree(newGroup);
        
            newSelection = data.Select(x => x.Parameter.ID).ToArray();
            foreach (var item in data)
            {
                var oldGroup = GetGroupOfParameter(item.Parameter);
                ReparentParameter(item.Parameter, oldGroup, newGroup);
            }
            
            ExpandItem(newGroup.ID);
        
            return DragVisualMode.Move;
        }
        
        /// <summary>
        /// Change a group's position in the group list.
        /// </summary>
        void ReorderGroup(ParameterGroup group, int childIndex)
        {
            m_ParameterList.m_Groups.Remove(group);
            if (childIndex >= 0 && childIndex <= m_ParameterList.m_Groups.Count)
                m_ParameterList.m_Groups.Insert(childIndex, group);
            else
                m_ParameterList.m_Groups.Add(group);
            
            viewController.Move(group.ID, -1, childIndex);
        }

        /// <summary>
        /// Reparent or reorder a parameter.
        /// </summary>
        void ReparentParameter(Parameter parameter, ParameterGroup oldGroup, ParameterGroup newGroup, int childIndex = -1)
        {
            oldGroup.m_Parameters.Remove(parameter);

            if (childIndex >= 0 && childIndex <= oldGroup.m_Parameters.Count)
                newGroup.m_Parameters.Insert(childIndex, parameter);
            else
                newGroup.m_Parameters.Add(parameter);
            
            m_ParameterGroups[parameter] = newGroup;

            viewController.Move(parameter.ID, newGroup.ID, childIndex);
        }
    }
}
