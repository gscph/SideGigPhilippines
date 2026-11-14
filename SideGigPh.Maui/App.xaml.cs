namespace SideGigPh.Maui
{
    public partial class App : Application
    {
        private readonly AppVisibility _visibility;

        public App(AppVisibility visibility)
        {
            _visibility = visibility;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "SideGigPh" };

            // Background work (an open chat's poll) pauses while the app is hidden.
            window.Stopped += (_, _) => _visibility.Set(false);
            window.Resumed += (_, _) => _visibility.Set(true);
            return window;
        }
    }
}
