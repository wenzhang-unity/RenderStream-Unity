using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        protected override void KeyboardNavigation(KeyboardNavigationOperation operation, EventBase evt)
        {
            if (operation == KeyboardNavigationOperation.SelectAll)
            {
                if (HasSelection())
                {
                    var firstItem = (ItemData)selectedItem;

                    if (firstItem.IsGroup)
                    {
                        RegisterSelectionUndoRedo();
                        
                        SelectAllGroups();
                    }
                    else if (firstItem.IsParameter)
                    {
                        RegisterSelectionUndoRedo();

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
                    RegisterSelectionUndoRedo();
                    
                    SelectAllGroups();
                }
                
                evt.StopImmediatePropagation();
            }
        }

        void SelectAllGroups()
        {
            var IDsToSelect = m_ParameterList.m_Groups.Select(group => group.ID);
            SelectRevealAndFrame(IDsToSelect);
        }

        ParameterGroup ResolveItemGroup(ItemData item)
        {
            if (item.IsGroup)
                return item.Group;
            else if (item.IsParameter)
                return GetGroupOfParameter(item.Parameter);
            else
                throw new NotSupportedException();
        }
        
        void SelectAllParametersInGroup(ParameterGroup group)
        {
            var IDsToSelect = group.m_Parameters.Select(parameter => parameter.ID);
            SelectRevealAndFrame(IDsToSelect);
        }
    }
}
