using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui
{
    public partial class App : Application
    {
        private readonly AppVisibility _visibility;

        public App(AppVisibility visibility, ErrorReporting errorReporting)
        {
            _visibility = visibility;
            InitializeComponent();

            // Crashes, stuck loading screens and logged errors go into the error log, sent in the background.
            errorReporting.Start();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "Tarbaho" };

            // Background work (an open chat's poll) pauses while the app is hidden.
            window.Stopped += (_, _) => _visibility.Set(false);
            window.Resumed += (_, _) => _visibility.Set(true);
            return window;
        }
    }
}
