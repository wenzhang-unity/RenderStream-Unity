using UnityEngine.UIElements;

namespace Disguise.RenderStream
{
    static class UIToolkitUtility
    {
        public static void SetDisplay(this VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
