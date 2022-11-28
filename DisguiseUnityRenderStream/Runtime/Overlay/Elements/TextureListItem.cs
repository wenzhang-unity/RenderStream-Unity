using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Overlay.Elements
{
    class TextureListItem : VisualElement
    {
        static class ResourcePaths
        {
            public const string Layout = OverlayConstants.LayoutsPath + "/TextureListItem";
        }
        
        static class StyleClass
        {
            public const string NameLabel = "texture-list-item-name";
            public const string InspectButton = "texture-list-item-button";
        }
        
        string m_Name;
        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                m_NameLabel.text = m_Name;
            }
        }
        
        bool m_IsInspectable;
        public bool IsInspectable
        {
            get => m_IsInspectable;
            set
            {
                m_IsInspectable = value;
                UIUtils.ShowElement(m_InspectButton, m_IsInspectable);
            }
        }

        public Clickable OnInspect => m_InspectButton.clickable;
        
        Label m_NameLabel;
        Button m_InspectButton;

        public TextureListItem()
        {
            var doc = Resources.Load<VisualTreeAsset>(ResourcePaths.Layout);
            doc.CloneTree(this);

            m_NameLabel = this.Q<Label>(null, StyleClass.NameLabel);
            m_InspectButton = this.Q<Button>(null, StyleClass.InspectButton);
        }

        public new class UxmlFactory : UxmlFactory<TextureListItem, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                TextureListItem item = (TextureListItem)ve;

                item.Name = m_Name.GetValueFromBag(bag, cc);
            }
        }
    }
}
