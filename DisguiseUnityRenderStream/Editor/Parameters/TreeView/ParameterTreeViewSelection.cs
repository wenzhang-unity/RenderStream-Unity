using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        bool HasSelection()
        {
            return selectedIndex >= 0;
        }
        
        void SetupSelection()
        {
            selectionType = SelectionType.Multiple;

            selectedIndicesChanged += SelectionChanged;
        }
        
        public interface ITreeViewStateStorage
        {
            List<int> SelectedIDs { get; set; }

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
        /// <see cref="UnityEditor.IMGUI.Controls.TreeView.SelectionChanged"/> notifies us after the selection has been changed, but the undo
        /// system expects the selection before it was changed to be registered. So we keep track of the previous
        /// selection to provide it to the undo system.
        /// </summary>
        List<int> m_PreviousSelection = new List<int>();

        /// <summary>
        /// Selects the provided items, expanding the hierarchy and ensuring that the last item in the selection is framed.
        /// </summary>
        void SelectRevealAndFrame(IEnumerable<int> selectedIds)
        {
            var enumerable = selectedIds.ToList();
            SetSelectionById(enumerable);
            ScrollToItemById(enumerable.Last());
        }

        /// <summary>
        /// Registers the new selection in the undo system.
        /// </summary>
        void SelectionChanged(IEnumerable<int> selectedIds)
        {
            EnforceSelectionType();
            
            RegisterPostSelectionUndoRedo();
        }

        /// <summary>
        /// Handles the details of registering the new selection.
        /// </summary>
        void RegisterPostSelectionUndoRedo()
        {
            // The undo system expects the state of the object before the change.
            // We register the previous selection in the undo system before restoring the current selection.
            
            var currentSelection = selectedIndices.Select(GetIdForIndex).ToList();
            m_StateStorage.SelectedIDs = m_PreviousSelection;
            
            Undo.RegisterCompleteObjectUndo(m_StateStorage.GetStorageObject(), "Change parameter selection");
            
            m_StateStorage.SelectedIDs = currentSelection;
            m_PreviousSelection = currentSelection;
        }

        /// <summary>
        /// Applies the registered selection after an undo/redo event.
        /// </summary>
        void OnUndoRedoPerformed()
        {
            RebuildAfterUndoRedo();
            
            SetSelectionByIdWithoutNotify(m_StateStorage.SelectedIDs);
        }

        /// <summary>
        /// Ensures only groups or only parameters are multi-selected at any given time.
        /// </summary>
        void EnforceSelectionType()
        {
            if (!HasSelection())
                return;
            
            var currentItem = (ItemData)selectedItems.Last();

            if (currentItem == null)
                return;
            
            var filteredSelection = selectedItems.Cast<ItemData>().Where(x => x.IsGroup == currentItem.IsGroup).Select(x => x switch
            {
                { IsGroup: true } => x.Group.ID,
                { IsParameter: true } => x.Parameter.ID,
                _ => throw new NotSupportedException()
            });
                
            SetSelectionByIdWithoutNotify(filteredSelection);
        }
    }
}
