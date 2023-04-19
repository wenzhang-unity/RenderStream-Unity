using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Configures UI visual properties.
        /// </summary>
        void SetupDraw()
        {
            fixedItemHeight = 20f;
            
            showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            
            columns.Add(new NameColumn(this));
            columns.Add(new ObjectColumn(this));
            columns.Add(new ComponentColumn(this));
            columns.Add(new PropertyColumn(this));
        }

        /// <summary>
        /// Returns the icon for a component.
        /// </summary>
        /// <param name="component">The target component.</param>
        /// <param name="noneIcon">Icon for a component that is missing, destroyed, or unassigned.</param>
        /// <param name="fallbackIcon">Icon for a component that has no specific icon defined.</param>
        /// <returns></returns>
        static Texture ResolveComponentIcon(Component component, Texture noneIcon, Texture fallbackIcon)
        {
            var icon = noneIcon;

            if (component != null && !IsMissingComponentScript(component))
            {
                icon = EditorGUIUtility.ObjectContent(null, component.GetType()).image;
                if (icon == null)
                    icon = fallbackIcon;
            }

            return icon;
        }

        /// <summary>
        /// Returns true if the specified object is destroyed (or missing).
        /// </summary>
        static bool IsMissingReference<T>(T obj) where T : UnityEngine.Object
        {
            // Unity has overriden the == operator to check for destroyed objects
            return obj is T && obj == null;
        }

        /// <summary>
        /// Returns true if the specified component's script is missing.
        /// </summary>
        static bool IsMissingComponentScript(UnityEngine.Object component)
        {
            // A component with a missing script presents itself as Component instance
            return component.GetType() == typeof(Component);
        }
    }
}
