namespace Disguise.RenderStream.Overlay
{
    static class OverlayConstants
    {
#if DISGUISE_RENDERSTREAM_PACKAGE
    // TODO (mirror UI Builder's strategy)
#else
        public const string LayoutsPath = "Layouts";
        public const string StylesPath = "Styles";
        public const string ImagesPath = "Images";
#endif

        public const string LayoutFullscren = "layout-fullscreen";
        public const string LayoutHorizontal = "layout-horizontal";
        public const string LayoutShrink = "layout-shrink";
        public const string LayoutNoShrink = "layout-no-shrink";
        public const string LayoutGrow = "layout-grow";
        public const string LayoutAlignStart = "layout-align-start";
        public const string LayoutAlignEnd = "layout-align-end";
        public const string LayoutTextAlignMiddleLeft = "layout-text-align-middle-left";
        public const string LayoutAlignCenter = "layout-align-center";
        public const string LayoutSmallVSpace = "layout-small-vspace";
        public const string LayoutMediumVSpace = "layout-medium-vspace";
    }
}
