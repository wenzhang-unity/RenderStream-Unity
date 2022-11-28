using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Overlay.Elements
{
    public class ExtendedList : VisualElement
    {
        static class ResourcePaths
        {
            public const string Layout = OverlayConstants.LayoutsPath + "/ExtendedList";
        }

        static class StyleClass
        {
            public const string List = "extended-list";
            public const string Border = "extended-list-border";
        }

        public ListView List
        {
            get => m_List;
        }

        ListView m_List;

        public ExtendedList()
        {
            var doc = Resources.Load<VisualTreeAsset>(ResourcePaths.Layout);
            doc.CloneTree(this);

            AddToClassList(StyleClass.List);

            m_List = this.Q<ListView>();

            UpdateHeight();
        }

        public void ItemsChanged()
        {
            m_List.Rebuild();
            UpdateHeight();
        }

        void UpdateHeight()
        {
            if (m_List.itemsSource == null || m_List.itemsSource.Count == 0)
                m_List.style.height = m_List.fixedItemHeight * 0.5f;
            else
                m_List.style.height = m_List.itemsSource.Count * m_List.fixedItemHeight;
        }

        public new class UxmlFactory : UxmlFactory<ExtendedList> { }
    }
}
