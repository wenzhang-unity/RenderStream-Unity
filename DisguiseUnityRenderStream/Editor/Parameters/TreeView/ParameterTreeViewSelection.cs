using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        public interface ITreeViewStateStorage
        {
            UnityEngine.Object GetStorageObject();
        }

        /// <summary>
        /// Call just before changing the selection.
        /// </summary>
        void RegisterSelectionUndoRedo()
        {
            Undo.RegisterCompleteObjectUndo(m_StateStorage.GetStorageObject(), "Change parameter selection");
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
            RegisterPostSelectionUndoRedo();
            
            base.SelectionChanged(selectedIds);
        }

        /// <summary>
        /// Handles the details of registering the new selection.
        /// </summary>
        void RegisterPostSelectionUndoRedo()
        {
            // The undo system expects the state of the object before the change.
            // We register the previous selection in the undo system before restoring the current selection.
            
            var currentSelection = state.selectedIDs;
            state.selectedIDs = m_PreviousSelection;
            
            Undo.RegisterCompleteObjectUndo(m_StateStorage.GetStorageObject(), "Change parameter selection");
            
            state.selectedIDs = currentSelection;
            m_PreviousSelection = new List<int>(currentSelection);
        }

        /// <summary>
        /// Applies the registered selection after an undo/redo event.
        /// </summary>
        void OnUndoRedoPerformed()
        {
            Reload();

            SetSelection(state.selectedIDs);
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
