namespace SideGigPh.Maui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
#if ANDROID
            // The page also makes room for the keyboard, so the WebView shrinks above it and a focused text box scrolls
            // into view instead of hiding behind it (the "Others" box, chat, forms). iOS's WebView does that itself.
            SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container | SafeAreaRegions.SoftInput);
#endif
        }
    }
}
