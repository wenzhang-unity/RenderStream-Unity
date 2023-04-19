using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    /// <summary>
    /// <para>
    /// Adapted from SequencesTreeView from the com.unity.sequences package:
    /// * Extends MultiColumnTreeView instead of TreeView
    /// * Added OnKeyboardNavigation
    /// * Added ContextClickedEmptyAreaInternal
    /// * Added BeginRenameAsync for an additional CanRename check
    /// * MakeItem/BindItem/UnbindItem/DestroyItem refactored to accept externally created items
    /// * Not every cell will have a RenameableLabel, handle null cases
    /// * UnregisterItemEvents: many cells deregister the same manipulator, handle this case
    /// </para>
    /// <para>
    /// TreeViewExtended offers more control on its items than the base UI Toolkit TreeView.
    /// It allows to delete and rename items and having a custom context menu per items. This tree view mimic the
    /// behaviors that exists on an IMGUI TreeView.
    /// </para>
    /// </summary>
    abstract class TreeViewExtended : MultiColumnTreeView
    {
        public static readonly string treeViewExtendedClassName = "tree-view-extended";
        public static readonly string itemClassName = treeViewExtendedClassName + "__item";

        internal readonly VisualElement scrollViewContainer;
        internal readonly VisualElement scrollViewEmptyClickContainer;

        bool m_DidFocusWindowThisFrame;

        EventCallback<PointerDownEvent> m_PointerDownEventCallback;

        Dictionary<VisualElement, IManipulator> m_Manipulators;
        KeyboardNavigationManipulator m_NavigationManipulator;
        ContextualMenuManipulator m_EmptyAreaContextClick;

        /// <summary>
        /// Keeps a reference of the renaming scheduler used to schedule a BeginRenameAtIndex.
        /// Useful in cases where it needs to be canceled.
        /// </summary>
        IVisualElementScheduledItem m_ScheduledItem;

        protected TreeViewExtended()
        {
            name = treeViewExtendedClassName;
            AddToClassList(treeViewExtendedClassName);
            
            selectionType = SelectionType.Multiple;

            scrollViewContainer = this.Q<ScrollView>().contentContainer;
            scrollViewEmptyClickContainer = this.Q<ScrollView>().contentViewport.parent;

            m_Manipulators = new Dictionary<VisualElement, IManipulator>();
            m_PointerDownEventCallback = OnPointerDownEvent;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            m_NavigationManipulator = new KeyboardNavigationManipulator(OnKeyboardNavigation);
            m_EmptyAreaContextClick = new ContextualMenuManipulator(ContextClickedEmptyAreaInternal);
        }

        /// <summary>
        /// Implement this function to get the text that should be displayed by the RenameableLabel element.
        /// </summary>
        /// <param name="index">The item's index.</param>
        /// <returns>A string that can be used to set the RenameableLabel.text property.</returns>
        protected abstract string GetItemTextForIndex(int index);
        
        /// <summary>
        /// Implement this function to populate the contextual menu of the selected tree view items.
        /// </summary>
        /// <param name="menu">The context menu to populate.</param>
        protected abstract void ContextClicked(DropdownMenu menu);
        
        /// <summary>
        /// Implement this function to populate the contextual menu when the click was outside any items.
        /// </summary>
        /// <param name="menu">The context menu to populate.</param>
        protected abstract void ContextClickedEmptyArea(DropdownMenu menu);

        /// <summary>
        /// Implement this function to delete all currently selected items.
        /// </summary>
        protected abstract void DeleteSelectedItems();

        /// <summary>
        /// Implement this function to define any post-process when the renaming of an item ends.
        /// </summary>
        /// <param name="id">The id of the item that was renamed.</param>
        /// <param name="canceled">True, if the renaming process was canceled, false otherwise.</param>
        protected abstract void RenameEnded(int id, bool canceled = false);
        
        /// <summary>
        /// Implement this function to override keyboard navigation.
        /// </summary>
        protected virtual void KeyboardNavigation(KeyboardNavigationOperation operation, EventBase evt) {}

        /// <summary>
        /// Implement this function to react to a double click action on the data at the provided <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The item's index.</param>
        protected virtual void DoubleClicked(int index) {}

        /// <summary>
        /// Implement this function to duplicate all currently selected items.
        /// </summary>
        protected virtual void DuplicateSelectedItems() {}

        /// <summary>
        /// Initialize the ClassList of a specific item. This function is called at the end of the BindItem function.
        /// </summary>
        /// <param name="ve">The TreeView item VisualElement.</param>
        /// <param name="index">The item's index.</param>
        protected virtual void InitClassListAtIndex(VisualElement ve, int index) {}

        /// <summary>
        /// Reset the ClassList of a specific item. This function is called at the end of the UnbindItem function.
        /// </summary>
        /// <param name="ve">The TreeView item VisualElement.</param>
        /// <param name="index">The item's index.</param>
        protected virtual void ResetClassListAtIndex(VisualElement ve, int index) {}

        /// <summary>
        /// Get the tooltip string to set at the specified index item.
        /// </summary>
        /// <param name="index">The item's index.</param>
        /// <returns>The tooltip string to set.</returns>
        protected virtual string GetTooltipForIndex(int index)
        {
            return "";
        }

        /// <summary>
        /// Implement this function to specify if the item at <paramref name="index"/> can be renamed.
        /// </summary>
        /// <param name="index">The item's index.</param>
        /// <returns>True if the item at the specified index can be renamed, False otherwise.</returns>
        protected virtual bool CanRename(int index)
        {
            return true;
        }

        /// <summary>
        /// Register events for the whole tree view. By default, this function allows to handle keyboard control for
        /// rename and delete operations.
        /// Override this function to register more events callback.
        /// </summary>
        protected virtual void RegisterEvents()
        {
            scrollViewContainer.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            scrollViewContainer.RegisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            scrollViewContainer.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);
            scrollViewContainer.RegisterCallback<FocusEvent>(OnFocusEvent, TrickleDown.TrickleDown);
            scrollViewContainer.AddManipulator(m_NavigationManipulator);

            scrollViewEmptyClickContainer.RegisterCallback<PointerDownEvent>(OnContainerPointerDownEvent);
            scrollViewEmptyClickContainer.AddManipulator(m_EmptyAreaContextClick);
            
#if UNITY_2022_2_OR_NEWER
            selectionChanged += OnSelectionChanged;
#else
            onSelectionChange  += OnSelectionChanged;
#endif
        }

        /// <summary>
        /// Unregister events for the whole tree view. See the RegisterEvents function.
        /// </summary>
        protected virtual void UnregisterEvents()
        {
            scrollViewContainer.UnregisterCallback<KeyDownEvent>(OnKeyDownEvent);
            scrollViewContainer.UnregisterCallback<ValidateCommandEvent>(OnValidateCommandEvent);
            scrollViewContainer.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommandEvent);
            scrollViewContainer.UnregisterCallback<FocusEvent>(OnFocusEvent);
            scrollViewContainer.RemoveManipulator(m_NavigationManipulator);

            scrollViewEmptyClickContainer.UnregisterCallback<PointerDownEvent>(OnContainerPointerDownEvent);
            scrollViewEmptyClickContainer.RemoveManipulator(m_EmptyAreaContextClick);
            
#if UNITY_2022_2_OR_NEWER
            selectionChanged -= OnSelectionChanged;
#else
            onSelectionChange  -= OnSelectionChanged;
#endif
        }

        /// <summary>
        /// Define what should be the behavior when the selection in the tree view change.
        /// </summary>
        /// <param name="objects">The list of the new selected objects</param>
        protected virtual void OnSelectionChanged(IEnumerable<object> objects) {}

        /// <summary>
        /// Begin the rename process of the item at the specified index.
        /// </summary>
        /// <param name="index">The item's index to rename.</param>
        /// <param name="delayMs">Apply a delay in milliseconds before beginning the rename process.</param>
        protected void BeginRenameAtIndex(int index, long delayMs = 0)
        {
            if (!CanRename(index))
                return;

            var label = GetLabelAtIndex(index);
            if (label == null)
                return;

            m_ScheduledItem = schedule.Execute(() => BeginRenameAsync(index, label));
            m_ScheduledItem.ExecuteLater(delayMs);
        }

        void BeginRenameAsync(int index, RenameableLabel label)
        {
            // Perform an additional check since we last saw this label
            if (!CanRename(index))
                return;

            label.BeginRename();
        }

        /// <summary>
        /// Get the RenameableLabel element at the specified index.
        /// </summary>
        /// <param name="index">The item's index.</param>
        /// <returns>The RenameableLabel at the specified index or null if no element is found.</returns>
        RenameableLabel GetLabelAtIndex(int index)
        {
            var root = GetRootElementForIndex(index);
            return root?.Q<RenameableLabel>();
        }

        /// <summary>
        /// Get the Label element in the specified tree view item VisualElement. The Label element is exclusively used
        /// to be the icon of the tree view item.
        /// </summary>
        /// <param name="ve">The item's VisualElement.</param>
        /// <returns>The RenameableLabel in the specified VisualElement or null if no element is found.</returns>
        protected RenameableLabel GetLabelElement(VisualElement ve)
        {
            return ve?.Q<RenameableLabel>();
        }

        protected void MakeItem(VisualElement ve)
        {
            ve.name = itemClassName;
            
            ve.RegisterCallback<AttachToPanelEvent, VisualElement>(OnAttachItemToPanel, ve);
            ve.RegisterCallback<DetachFromPanelEvent, VisualElement>(OnDetachItemFromPanel, ve);
        }

        protected void DestroyItem(VisualElement ve)
        {
            ve.UnregisterCallback<AttachToPanelEvent, VisualElement>(OnAttachItemToPanel);
            ve.UnregisterCallback<DetachFromPanelEvent, VisualElement>(OnDetachItemFromPanel);
        }

        protected void BindItem(VisualElement ve, int index)
        {
            var id = GetIdForIndex(index);

            ve.userData = id;
            ve.tooltip = GetTooltipForIndex(index);

            var label = ve.Q<RenameableLabel>();
            if (label != null)
                label.text = GetItemTextForIndex(index);

            InitClassListAtIndex(ve, index);
        }

        protected void UnbindItem(VisualElement ve, int index)
        {
            var label = GetLabelElement(ve);
            label?.CancelRename();

            ve.userData = null;
            ve.tooltip = "";

            ResetClassListAtIndex(ve, index);
        }

        void RegisterItemEvents(VisualElement ve)
        {
            if (ve.Q<RenameableLabel>() is { } renameableLabel)
                renameableLabel.renameEnding += OnRenameItemEnding;

            var root = GetRootElement(ve);
            root.RegisterCallback(m_PointerDownEventCallback, TrickleDown.TrickleDown);

            m_Manipulators[root] = new ContextualMenuManipulator(ContextClickedInternal);
            root.AddManipulator(m_Manipulators[root]);
        }

        void UnregisterItemEvents(VisualElement ve)
        {
            if (ve.Q<RenameableLabel>() is { } renameableLabel)
                renameableLabel.renameEnding -= OnRenameItemEnding;

            var root = GetRootElement(ve);
            root.UnregisterCallback(m_PointerDownEventCallback);

            // When there's multiple columns, another item from the same row may have already removed the manipulator
            if (m_Manipulators.ContainsKey(root))
            {
                root.RemoveManipulator(m_Manipulators[root]);
                m_Manipulators.Remove(root);
            }
        }

        void OnRenameItemEnding(RenameableLabel label, bool canceled)
        {
            var id = (int)label.hierarchy.parent.userData;
            RenameEnded(id, canceled);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterEvents();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterEvents();
        }

        void OnAttachItemToPanel(AttachToPanelEvent evt, VisualElement ve)
        {
            RegisterItemEvents(ve);
        }

        void OnDetachItemFromPanel(DetachFromPanelEvent evt, VisualElement ve)
        {
            UnregisterItemEvents(ve);
        }

        void OnPointerDownEvent(PointerDownEvent evt)
        {
            var currentTarget = evt.currentTarget as VisualElement;
            var id = (int)currentTarget.Q<VisualElement>(itemClassName).userData;
            var index = viewController.GetIndexForId(id);

            if (evt.button == (int)MouseButton.RightMouse)
            {
                if (!IsSelected(index))
                    SetSelection(index);

                return;
            }

            if (!(evt.target is Label))
                return;

            // If the user focused the window with this same click, we don't want to immediately start renaming.
            if (m_DidFocusWindowThisFrame)
                return;

            // Handle double click on an item.
            if (evt.clickCount == 2)
            {
                // Cancel the previously scheduled BeginRenameAtIndex()
                // as OnPointerDownEvent is called with clickCount = 1 in the case of a double click.
                StopRenameScheduler();
                DoubleClicked(index);
                return;
            }

            if (selectedIndices.Count() == 1 && IsSelected(index))
            {
                // Item is already selected. It's the second time users click on it. Start renaming.
                BeginRenameAtIndex(index, 500);
            }
            else
            {
                // Cancel scheduled rename if selection has changed.
                StopRenameScheduler();
            }
        }

        void OnContainerPointerDownEvent(PointerDownEvent evt)
        {
            // We left-clicked on an empty space of the tree: clear selection
            if (evt.button == 0 && evt.target == evt.currentTarget)
            {
                SetSelection(Enumerable.Empty<int>());
            }
        }

        void OnKeyboardNavigation(KeyboardNavigationOperation operation, EventBase evt)
        {
            KeyboardNavigation(operation, evt);
        }

        void OnKeyDownEvent(KeyDownEvent evt)
        {
            bool isOSXEditor = Application.platform == RuntimePlatform.OSXEditor;
            if (selectedIndices.Count() != 1 ||
                (isOSXEditor && evt.keyCode != KeyCode.Return) ||
                (!isOSXEditor && evt.keyCode != KeyCode.F2))
            {
                return;
            }

            evt.StopPropagation();
            BeginRenameAtIndex(selectedIndex);
        }

        void OnValidateCommandEvent(ValidateCommandEvent evt)
        {
            switch (evt.commandName, selectedIndices.Count())
            {
                // Some commands require that one or more items be selected, others exactly one.
                case ("Delete", > 0):
                case ("SoftDelete", > 0):
                case ("Duplicate", > 0):
                case ("Rename", 1):
                    evt.StopPropagation();
                    evt.imguiEvent?.Use();
                    break;
            }
        }

        internal void OnWindowFocused()
        {
            m_DidFocusWindowThisFrame = true;
            EditorApplication.delayCall += () => m_DidFocusWindowThisFrame = false;
        }

        void OnExecuteCommandEvent(ExecuteCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case "Delete":
                case "SoftDelete":
                    evt.StopPropagation();
                    DeleteSelectedItems();
                    break;
                case "Duplicate":
                    evt.StopPropagation();
                    DuplicateSelectedItems();
                    break;
                case "Rename":
                    evt.StopPropagation();
                    BeginRenameAtIndex(selectedIndex);
                    break;
            }
        }

        void OnFocusEvent(FocusEvent evt)
        {
            // Force a "re-selection" when the TreeView re-gain the focus.
            OnSelectionChanged(selectedItems);
        }

        void ContextClickedInternal(ContextualMenuPopulateEvent evt)
        {
            if (!selectedIndices.Any())
                return;

            ContextClicked(evt.menu);
            evt.StopPropagation(); // Avoid triggering any other contextual menu.
        }
        
        void ContextClickedEmptyAreaInternal(ContextualMenuPopulateEvent evt)
        {
            ContextClickedEmptyArea(evt.menu);
            evt.StopPropagation(); // Avoid triggering any other contextual menu.
        }

        protected bool IsSelected(int index)
        {
            return selectedIndices.Contains(index);
        }

        void StopRenameScheduler()
        {
            if (m_ScheduledItem == null)
                return;

            if (!m_ScheduledItem.isActive)
                return;

            m_ScheduledItem.Pause();
        }

        // Get the root VisualElement of a tree view item from its content element.
        // This function is similar to GetRootElementAtIndex except that it search the root VisualElement from
        // its content element (the one returned by MakeItem) instead of looking into the active items.
        VisualElement GetRootElement(VisualElement ve)
        {
            for (var veParent = ve.hierarchy.parent; veParent != null; veParent = veParent.hierarchy.parent)
            {
                if (veParent.ClassListContains(itemUssClassName))
                    return veParent;
            }
            return null;
        }
    }
}
