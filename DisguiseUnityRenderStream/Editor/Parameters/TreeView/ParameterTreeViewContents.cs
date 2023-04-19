using UnityEditor;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    partial class ParameterTreeView
    {
        /// <summary>
        /// Contains text, textures, and other contents for the UI.
        /// </summary>
        static class Contents
        {
            public static readonly string UndoChangeSelection = L10n.Tr("Change parameter selection");
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
            public static readonly string UndoDragAndDropReorderParameters = L10n.Tr("Drag and drop reorder parameters");

            public static readonly string NameColumnHeader = L10n.Tr("Name");
            public static readonly string ObjectColumnHeader = L10n.Tr("Object");
            public static readonly string ComponentColumnHeader = L10n.Tr("Component");
            public static readonly string PropertyColumnHeader = L10n.Tr("Property");

            public static readonly string ContextMenuCreateNewGroup = L10n.Tr("Create New Group");
            public static readonly string ContextMenuCreateNewParameter = L10n.Tr("Create New Parameter");
            public static readonly string ContextMenuAddNewParameter = L10n.Tr("Add New Parameter");
            public static readonly string ContextMenuRename = L10n.Tr("Rename");
            public static readonly string ContextMenuDuplicate = L10n.Tr("Duplicate");
            public static readonly string ContextMenuDelete = L10n.Tr("Delete");

            public static readonly string NewGroupName = L10n.Tr("New Group");
            public static readonly string NewParameterName = L10n.Tr("New Parameter");

            public static readonly string EmptyGroupSuffix = L10n.Tr(" (Empty)");
            public static readonly string DropdownNoneLabel = L10n.Tr("None");
            public static readonly string DropdownMissingComponentLabel = L10n.Tr("Missing (Component)");
            public static readonly string DropdownMissingScriptLabel = L10n.Tr("Missing (Script)");

            public static readonly Texture WarningIcon = L10n.IconContent("console.warnicon").image;
            static Texture s_GameObjectIcon;
            public static Texture GameObjectIcon
            {
                get
                {
                    if (s_GameObjectIcon == null)
                    {
                        // Needs deferred creation
                        s_GameObjectIcon = EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image;
                    }
                
                    return s_GameObjectIcon;
                }
            }

            public static readonly string DragTitle = L10n.Tr("Parameter(s)");
            public const string DragDataKey = "Disguise.DragData";
        }
    }
}
