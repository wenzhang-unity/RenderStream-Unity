using UnityEditor;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        static class Contents
        {
            public static readonly string UndoToggleEnableParameterGroup = L10n.Tr("Toggle parameter group enabled");
            public static readonly string UndoToggleEnableParameter = L10n.Tr("Toggle parameter enabled");
            public static readonly string UndoRenameParameter = L10n.Tr("Rename parameter");
            public static readonly string UndoCreateNewParameterGroup = L10n.Tr("Create new parameter group");
            public static readonly string UndoCreateNewParameter = L10n.Tr("Create new parameter");
            public static readonly string UndoDeleteParameter = L10n.Tr("Delete parameter(s)");
            public static readonly string UndoDuplicateParameter = L10n.Tr("Duplicate parameter(s)");
            public static readonly string UndoAssignObject = L10n.Tr("Assigned object to parameter");
            public static readonly string UndoAssignComponent = L10n.Tr("Assigned component to parameter");
            public static readonly string UndoAssignProperty = L10n.Tr("Assigned property to parameter");
            public static readonly string UndoDragAndDropNewParameters = L10n.Tr("Drag and drop new parameters");
            public static readonly string UndoDragAndDropAssignParameters = L10n.Tr("Drag and drop assign parameters");
            
            public static readonly GUIContent NameColumnHeader = L10n.TextContent("Name");
            public static readonly GUIContent ObjectColumnHeader = L10n.TextContent("Object");
            public static readonly GUIContent ComponentColumnHeader = L10n.TextContent("Component");
            public static readonly GUIContent PropertyColumnHeader = L10n.TextContent("Property");
            
            public static readonly GUIContent ContextMenuCreateNewGroup = L10n.TextContent("Create New Group");
            public static readonly GUIContent ContextMenuCreateNewParameter = L10n.TextContent("Create New Parameter");
            public static readonly GUIContent ContextMenuAddNewParameter = L10n.TextContent("Add New Parameter");
            public static readonly GUIContent ContextMenuRename = L10n.TextContent("Rename");
            public static readonly GUIContent ContextMenuDuplicate = L10n.TextContent("Duplicate");
            public static readonly GUIContent ContextMenuDelete = L10n.TextContent("Delete");
            
            public static readonly string NewGroupName = L10n.Tr("New Group");
            public static readonly string NewParameterName = L10n.Tr("New Parameter");
            
            public static readonly string EmptyGroupSuffix = L10n.Tr(" (Empty)");
            public static readonly string DropdownNoneLabel = L10n.Tr("None");
            public static readonly string DropdownMissingScriptLabel = L10n.Tr("Missing Script");

            public static GUIStyle ComponentPopupStyle;
            public static GUIStyle PropertyPopupStyle;
            public static readonly Texture WarningIcon = L10n.IconContent("console.warnicon").image;
            public static Texture GameObjectIcon;

            public static readonly string DragTitle = L10n.Tr("Parameter(s)");
            public const string DragDataKey = "Disguise.DragData";

            // EditorStyles is only guaranteed to be loaded in OnGUI
            public static void OnGUILazyLoad()
            {
                if (ComponentPopupStyle == null)
                {
                    ComponentPopupStyle = new GUIStyle(EditorStyles.popup);
                    ComponentPopupStyle.fixedHeight = 20f;
                    ComponentPopupStyle.padding.left = 20;
                }

                if (PropertyPopupStyle == null)
                {
                    PropertyPopupStyle = new GUIStyle(EditorStyles.popup);
                    PropertyPopupStyle.fixedHeight = 20f;
                }

                if (GameObjectIcon == null)
                {
                    GameObjectIcon = EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image;
                }
            }
        }
    }
}
