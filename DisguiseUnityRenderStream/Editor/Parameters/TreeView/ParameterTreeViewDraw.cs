using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        void SetupDraw()
        {
            fixedItemHeight = 20f;
            
            showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            
            columns.Add(new NameColumn(this));
            columns.Add(new ObjectColumn(this));
            columns.Add(new ComponentColumn(this));
            columns.Add(new PropertyColumn(this));
        }

        static Texture ResolveComponentIcon(Component component, Texture noneIcon, Texture fallbackIcon)
        {
            var icon = noneIcon;

            if (component != null && !IsMissingComponent(component))
            {
                icon = EditorGUIUtility.ObjectContent(null, component.GetType()).image;
                if (icon == null)
                    icon = fallbackIcon;
            }

            return icon;
        }

        static bool IsMissingReference<T>(T obj) where T : UnityEngine.Object
        {
            // Unity has overriden the == operator to check for destroyed objects
            return obj is T && obj == null;
        }

        static bool IsMissingComponent(UnityEngine.Object component)
        {
            return component.GetType() == typeof(Component);
        }
    }
}
