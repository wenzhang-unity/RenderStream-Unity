using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Overrides keyboard navigation for <see cref="KeyboardNavigationOperation.SelectAll"/>.
        /// (Selects either all groups or all parameters inside a group depending on the current selection).
        /// </summary>
        protected override void KeyboardNavigation(KeyboardNavigationOperation operation, EventBase evt)
        {
            if (operation == KeyboardNavigationOperation.SelectAll)
            {
                if (HasSelection())
                {
                    var firstItem = (ItemData)selectedItem;

                    if (firstItem.IsGroup)
                    {
                        SelectAllGroups();
                    }
                    else if (firstItem.IsParameter)
                    {
                        var targetGroup = ResolveItemGroup(firstItem);
                        SelectAllParametersInGroup(targetGroup);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    SelectAllGroups();
                }
                
                evt.StopImmediatePropagation();
            }
        }

        /// <summary>
        /// Selects all the groups in the tree.
        /// </summary>
        void SelectAllGroups()
        {
            var IDsToSelect = m_ParameterList.m_Groups.Select(group => group.ID);
            SelectRevealAndFrame(IDsToSelect);
        }

        /// <summary>
        /// Returns the group that an item represents or belongs to.
        /// </summary>
        ParameterGroup ResolveItemGroup(ItemData item)
        {
            if (item.IsGroup)
                return item.Group;
            else if (item.IsParameter)
                return GetGroupOfParameter(item.Parameter);
            else
                throw new NotSupportedException();
        }
        
        /// <summary>
        /// Selects all the parameters inside the specified group.
        /// </summary>
        void SelectAllParametersInGroup(ParameterGroup group)
        {
            var IDsToSelect = group.m_Parameters.Select(parameter => parameter.ID);
            SelectRevealAndFrame(IDsToSelect);
        }
    }
}
