using UnityEngine.UIElements;

namespace Disguise.RenderStream.Overlay
{
    public static class UIUtils
    {
        public static void ShowElement(VisualElement element, bool show = true)
        {
            element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
