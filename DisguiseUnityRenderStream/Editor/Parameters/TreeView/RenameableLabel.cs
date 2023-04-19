using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    /// <summary>
    /// <para>
    /// Adapted from the com.unity.sequences package.
    /// </para>
    /// <para>
    /// Label that can be renamed.
    /// </para>
    /// </summary>
    class RenameableLabel : VisualElement
    {
        public static readonly string className = "renameable-label";
        
        /// <summary>
        /// Text to display.
        /// </summary>
        internal string text
        {
            get => label.text;
            set => label.text = value;
        }

        bool isRenaming { get; set; }

        internal event Action renameStarting;
        
        internal event Action<RenameableLabel, bool> renameEnding;

        Label label { get; } = new();
        TextField textField { get; } = new();

        internal RenameableLabel()
        {
            name = className;
            AddToClassList(className);

            focusable = true;
            delegatesFocus = false;

            Add(label);
            Add(textField);

            textField.selectAllOnFocus = true;
            textField.selectAllOnMouseUp = false;
            textField.style.display = DisplayStyle.None;

            textField.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            textField.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            textField.RegisterCallback<BlurEvent>(OnBlurEvent);
        }

        internal void BeginRename()
        {
            if (isRenaming)
                return;
                
            renameStarting?.Invoke();

            isRenaming = true;
            delegatesFocus = true;

            label.style.display = DisplayStyle.None;
            textField.style.display = DisplayStyle.Flex;

            textField.value = text;
            textField.Q<TextElement>().Focus();
        }

        internal void CancelRename()
        {
            if (isRenaming)
                EndRename();
        }

        void EndRename(bool canceled = false)
        {
            isRenaming = false;
            delegatesFocus = false;
            schedule.Execute(Focus);

            textField.style.display = DisplayStyle.None;
            label.style.display = DisplayStyle.Flex;

            if (!canceled) // When the rename is canceled, the label keep its current value.
                label.text = textField.value;

            renameEnding?.Invoke(this, canceled);
        }

        void OnMouseUpEvent(MouseUpEvent evt)
        {
            if (!isRenaming)
                return;

            textField.Q<TextElement>().Focus();
            evt.StopPropagation();
        }

        void OnKeyDownEvent(KeyDownEvent evt)
        {
            if (isRenaming && evt.keyCode == KeyCode.Escape)
                EndRename(true);
        }

        void OnBlurEvent(BlurEvent evt)
        {
            if (!isRenaming)
                return;

            EndRename();
        }
    }
}
