using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Selection events are not registered in the undo system when true.
        /// </summary>
        static bool s_SelectionReentryGuard;

        /// <summary>
        /// Selection events are not registered in the undo system inside this scope.
        /// </summary>
        class SelectionReEntryGuard : IDisposable
        {
            public SelectionReEntryGuard()
            {
                s_SelectionReentryGuard = true;
            }

            public void Dispose()
            {
                s_SelectionReentryGuard = false;
            }
        }

        /// <summary>
        /// <see cref="TreeView.SelectionChanged"/> notifies us after the selection has been changed, but the undo
        /// system expects the selection before it was changed to be registered. So we keep track of the previous
        /// selection to provide it to the undo system.
        /// </summary>
        List<int> m_PreviousSelection = new List<int>();

        /// <summary>
        /// Selects the provided items, expanding the hierarchy and ensuring that the last item in the selection is framed.
        /// </summary>
        void SelectRevealAndFrame(IList<int> selectedIds)
        {
            SetSelection(selectedIds, TreeViewSelectionOptions.RevealAndFrame);
        }

        /// <summary>
        /// Registers the new selection in the undo system.
        /// </summary>
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            RegisterSelectionUndoRedo();
            
            base.SelectionChanged(selectedIds);
        }

        /// <summary>
        /// Handles the details of registering the new selection.
        /// </summary>
        void RegisterSelectionUndoRedo()
        {
            if (!s_SelectionReentryGuard)
            {
                // The undo system expects the state of the object before the change.
                // We register the previous selection in the undo system before restoring the current selection.
                
                var currentSelection = m_ParameterList.TreeViewState.selectedIDs;
                m_ParameterList.TreeViewState.selectedIDs = m_PreviousSelection;
                
                Undo.RegisterCompleteObjectUndo(m_ParameterList, "Change parameter selection");
                
                m_ParameterList.TreeViewState.selectedIDs = currentSelection;
                m_PreviousSelection = currentSelection;
            }
        }

        /// <summary>
        /// Applies the registered selection after an undo/redo event.
        /// </summary>
        void OnUndoRedoPerformed()
        {
            Reload();

            using (new SelectionReEntryGuard())
            {
                SetSelection(m_ParameterList.TreeViewState.selectedIDs);
            }
        }
        
        /// <summary>
        /// Ensures multi-selection only includes items of the same type: either all groups or all parameters.
        /// </summary>
        protected override bool CanMultiSelect(TreeViewItem item)
        {
            if (!HasSelection())
            {
                return true;
            }

            var firstItemIndex = GetSelection()[0];
            var firstItem = FindItem(firstItemIndex, rootItem);

            return firstItem != null && item.GetType() == firstItem.GetType();
        }
    }
}
