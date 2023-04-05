using System;
using System.Linq;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Handle keyboard shortcuts for delete and duplicate
        /// </summary>
        protected override void KeyEvent()
        {
            var evt = Event.current;
            
            if (evt.type != EventType.KeyDown)
                return;
            
            if (evt.keyCode is KeyCode.Delete or KeyCode.Backspace)
            {
                DeleteSelection();
                evt.Use();
            }
            
            if ((evt.control || evt.command) && evt.keyCode == KeyCode.D)
            {
                DuplicateSelection();
                evt.Use();
            }
        }

        /// <summary>
        /// Handle Ctrl-A select all command
        /// </summary>
        protected override void CommandEventHandling()
        {
            var current = Event.current;
            
            if (HasFocus() && current.type is EventType.ExecuteCommand)
                Debug.Log(current.commandName);
            
            if (HasFocus() &&
                current.type is EventType.ExecuteCommand &&
                current.commandName == "SelectAll")
            {
                if (HasSelection())
                {
                    var firstID = GetSelection()[0];
                    var firstItem = FindItem(firstID, rootItem);

                    if (firstItem is ParameterGroupTreeViewItem)
                    {
                        // Select all groups
                        SetSelection(rootItem.children.Select(x => x.id).ToArray());
                    }
                    else if (firstItem is ParameterTreeViewItem)
                    {
                        // Select all parameters under the current group
                        SetSelection(firstItem.parent.children.Select(x => x.id).ToArray());
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    // Select all groups
                    SetSelection(rootItem.children.Select(x => x.id).ToArray());
                }
                
                current.Use();
                GUIUtility.ExitGUI();
                return;
            }
            
            base.CommandEventHandling();
        }
    }
}
