using UnityEditor;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    /// <summary>
    /// This editor hides everything and displays a button that opens the <see cref="ParameterWindow"/>.
    /// <see cref="ParameterWindow"/> is the only place where <see cref="DisguiseParameterList"/> is meant to be edited.
    /// 
    /// <remarks>
    /// In a pinch, the Inspector window in Debug mode can accurately display <see cref="DisguiseParameterList"/>.
    /// </remarks>
    /// </summary>
    [CustomEditor(typeof(DisguiseParameterList))]
    class DisguiseParameterListEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            var inspectButton = new Button();
            root.Add(inspectButton);

            inspectButton.text = "Open in Remote Parameters Window";
            inspectButton.clicked += OnInspectButtonClicked;

            return root;
        }

        void OnInspectButtonClicked()
        {
            ParameterWindow.Open();
        }
    }
}
