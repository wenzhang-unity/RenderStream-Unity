using UnityEditor;
using UnityEngine.UIElements;

namespace Disguise.RenderStream
{
    static class DisguiseRenderStreamSettingsProvider
    {
        public const string SettingsPath = "Project/DisguiseRenderStream";

        class Contents
        {
            public const string SettingsName = "Disguise RenderStream";
        }

        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider() =>
            new (SettingsPath, SettingsScope.Project)
            {
                label = Contents.SettingsName,
                activateHandler = (searchContext, parentElement) =>
                {
                    var settings = DisguiseRenderStreamSettings.GetOrCreateSettings();
                    var editor = Editor.CreateEditor(settings);

                    var gui = editor.CreateInspectorGUI();
                    gui.AddToClassList(DisguiseRenderStreamSettingsEditor.Style.DisguiseSettingsContainer);
                    
                    var title = new Label { text = Contents.SettingsName };
                    title.AddToClassList(DisguiseRenderStreamSettingsEditor.Style.DisguiseSettingsTitle);
                    gui.Insert(0, title);
                    
                    parentElement.Add(gui);
                },
                keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(new SerializedObject(DisguiseRenderStreamSettings.GetOrCreateSettings()))
            };
    }
}
