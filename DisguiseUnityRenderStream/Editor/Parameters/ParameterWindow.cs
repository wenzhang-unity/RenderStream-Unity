using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    public class ParameterWindow : EditorWindow, ParameterTreeView.ITreeViewStateStorage
    {
        List<int> ParameterTreeView.ITreeViewStateStorage.SelectedIDs
        {
            get => m_TreeSelectedIds;
            set => m_TreeSelectedIds = value;
        }

        Object ParameterTreeView.ITreeViewStateStorage.GetStorageObject()
        {
            return this;
        }
        
        static class Contents
        {
            public static readonly GUIContent WindowTitle = L10n.TextContent("Remote Parameters");
            public const string MenuPath = "Disguise/Remote Parameters";

            public static readonly string ContextCreateNewGroup = L10n.Tr("Create New Group");
            public static readonly string ContextCreateNewParameter = L10n.Tr("Create New Parameter");
            
            public const string SceneDirtySuffix = "*";
        }
    
        [MenuItem(Contents.MenuPath)]
        public static void Open()
        {
            var projectBrowserWindowType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
            var window = GetWindow<ParameterWindow>(projectBrowserWindowType);
            window.titleContent = Contents.WindowTitle;
            window.Show();
        }

        [SerializeField]
        VisualTreeAsset m_Layout;
        
        [SerializeField]
        StyleSheet m_Style;

        [SerializeField]
        List<int> m_TreeSelectedIds;

        DisguiseParameterList m_ParameterList;

        bool m_GUICreated;
        ToolbarSearchField m_SearchField;
        Label m_SceneNameLabel;
        Button m_PatchSchemaButton;
        VisualElement m_CreateParameterListSection;
        Button m_CreateParameterListButton;
        VisualElement m_ExtraParameterListSection;
        ExtraInstanceListView m_ExtraParameterListNames;
        ParameterTreeView m_TreeView;
        IVisualElementScheduledItem m_ScheduledPoll;

        void OnEnable()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneLoaded;
        }

        void OnDisable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneLoaded;
        }

        void CreateGUI()
        {
            m_Layout.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(m_Style);
            rootVisualElement.focusable = true;
            rootVisualElement.pickingMode = PickingMode.Position;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

            m_SceneNameLabel = rootVisualElement.Q<Label>("scene-name-label");

            m_SearchField = rootVisualElement.Q<ToolbarSearchField>();
            m_SearchField.RegisterValueChangedCallback(evt => SetSearchString(evt.newValue));

            m_PatchSchemaButton = rootVisualElement.Q<Button>("patch-schema-button");
            m_PatchSchemaButton.clicked += OnPatchSchemaButtonClicked;

            var settingsButton = rootVisualElement.Q<ToolbarButton>("disguise-settings-button");
            settingsButton.clicked += OnDisguiseSettingsButtonClicked;

            var createButton = rootVisualElement.Q<ToolbarMenu>();
            createButton.menu.AppendAction(Contents.ContextCreateNewGroup, _ => CreateNewGroup());
            createButton.menu.AppendAction(Contents.ContextCreateNewParameter, _ => CreateNewParameter());

            m_CreateParameterListSection = rootVisualElement.Q<VisualElement>("create-parameter-list-section");
            m_CreateParameterListSection.SetDisplay(false);
            m_CreateParameterListButton = rootVisualElement.Q<Button>("create-parameter-list-button");
            m_CreateParameterListButton.clicked += OnCreateParameterListButtonClicked;

            m_ExtraParameterListSection = rootVisualElement.Q<VisualElement>("extra-parameter-list-section");
            m_ExtraParameterListNames = rootVisualElement.Q<ExtraInstanceListView>();

            m_TreeView = new ParameterTreeView();
            rootVisualElement.Add(m_TreeView);

            m_GUICreated = true;

            PollScene();
            
            if (rootVisualElement.panel != null)
                OnAttachToPanel(); 
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(_ => OnAttachToPanel());
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(_ => OnDetachFromPanel());
        }

        void OnAttachToPanel()
        {
            m_ScheduledPoll = rootVisualElement.schedule.Execute(PollScene).Every(250);
        }

        void OnDetachFromPanel()
        {
            m_ScheduledPoll.Pause();
        }
        
        void CreateNewGroup()
        {
            m_TreeView.CreateNewGroup();
        }
        
        void CreateNewParameter()
        {
            m_TreeView.CreateNewParameter();
        }
        
        void SetSearchString(string searchString)
        {
            m_TreeView.SearchString = searchString;
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.ctrlKey && evt.keyCode == KeyCode.F)
            {
                m_SearchField.Focus();
            }
        }

        void OnCreateParameterListButtonClicked()
        {
            AddParameterListToScene();
            PollScene();
        }

        /// <summary>
        /// Generates a Disguise schema file without running a build.
        /// </summary>
        void OnPatchSchemaButtonClicked()
        {
            var build = new DisguiseRenderStreamBuild();
            build.PatchSchema();
        }

        void OnDisguiseSettingsButtonClicked()
        {
            SettingsService.OpenProjectSettings(DisguiseRenderStreamSettingsProvider.SettingsPath);
        }

        /// <summary>
        /// Resolves the <see cref="DisguiseParameterList"/> instance to edit. Handles the none and multiple instances cases.
        /// </summary>
        void PollScene()
        {
            // During a build this function can be called in between window refreshes
            if (!m_GUICreated)
                return;
            
            var lists = FindObjectsByType<DisguiseParameterList>(FindObjectsSortMode.InstanceID);
            m_ExtraParameterListNames.itemsSource = lists;
            
            if (lists.Length == 0)
            {
                m_CreateParameterListSection.SetDisplay(true);
                m_ExtraParameterListSection.SetDisplay(false);
                m_TreeView.SetDisplay(false);

                m_ParameterList = null;
            }
            else if (lists.Length == 1)
            {
                m_CreateParameterListSection.SetDisplay(false);
                m_ExtraParameterListSection.SetDisplay(false);
                m_TreeView.SetDisplay(true);

                var newParameterList = lists[0];
                if (m_ParameterList != newParameterList || m_TreeView.itemsSource == null)
                {
                    m_TreeView.SetData(newParameterList, this);
                    m_ParameterList = newParameterList;
                }
            }
            else
            {
                m_CreateParameterListSection.SetDisplay(false);
                m_ExtraParameterListSection.SetDisplay(true);
                m_TreeView.SetDisplay(false);
                
                m_ParameterList = null;

                m_ExtraParameterListNames.itemsSource = lists;
            }

            if (m_ParameterList != null)
            {
                var scene = m_ParameterList.gameObject.scene;
                m_SceneNameLabel.text = scene.isDirty
                    ? scene.name + Contents.SceneDirtySuffix
                    : scene.name;
            }
            else
            {
                m_SceneNameLabel.text = string.Empty;
            }
            
            m_PatchSchemaButton.SetDisplay(DisguiseRenderStreamSettings.GetOrCreateSettings().showPatchSchemaButton);
        }

        void OnSceneLoaded(Scene oldScene, Scene newScene)
        {
            PollScene();
        }

        static DisguiseParameterList AddParameterListToScene()
        {
            var go = new GameObject(ObjectNames.NicifyVariableName(nameof(DisguiseParameterList)));
            var parameterList = go.AddComponent<DisguiseParameterList>();
            
            StageUtility.PlaceGameObjectInCurrentStage(go);
            Undo.RegisterCreatedObjectUndo(go, $"Created {go.name}");
            Selection.activeObject = go;
            
            return parameterList;
        }
    }
    
    /// <summary>
    /// Displays a list of clickable <see cref="DisguiseParameterList"/> buttons.
    /// The button labels are the scene hierarchy paths of the GameObjects.
    /// On button click: the corresponding GameObject is selected and highlighted in the scene hierarchy.
    /// </summary>
    class ExtraInstanceListView : ListView
    {
        public const string ClassName = "extra-instance-list-view";
            
        public new class UxmlFactory : UxmlFactory<ExtraInstanceListView> {}
            
        static string GetPathInHierarchy(GameObject go)
        {
            var transform = go.transform;
            var path = transform.name;
            
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            
            return path;
        }
            
        public ExtraInstanceListView()
        {
            AddToClassList(ClassName);
                
            makeItem = MakeItem;
            bindItem = BindItem;

            selectionType = SelectionType.None;
        }

        VisualElement MakeItem()
        {
            var button = new Button();
            button.clickable.clickedWithEventInfo += OnButtonClicked;
            return button;
        }
            
        void BindItem(VisualElement element, int index)
        {
            var button = (Button)element;
            var source = (DisguiseParameterList)itemsSource[index];

            button.text = GetPathInHierarchy(source.gameObject);
            button.userData = source;
        }

        void OnButtonClicked(EventBase evt)
        {
            var button = (Button)evt.target;
            var source = (DisguiseParameterList)button.userData;

            var go = source.gameObject;
            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
