using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace Disguise.RenderStream.Parameters
{
    static class TreeViewExtensions
    {
        static readonly PropertyInfo s_DeselectOnUnhandledMouseDown = typeof(TreeView)
            .GetProperty("deselectOnUnhandledMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// When <paramref name="value"/> is <see langword="true"/>, clicking on the empty area in
        /// <paramref name="treeView"/> will clear its selection.
        /// </summary>
        public static void DeselectOnUnhandledMouseDown(this TreeView treeView, bool value)
        {
            if (treeView == null)
            {
                throw new ArgumentNullException(nameof(treeView));
            }

            Debug.Assert(s_DeselectOnUnhandledMouseDown != null);

            s_DeselectOnUnhandledMouseDown.SetValue(treeView, value);
        }
    }
}
