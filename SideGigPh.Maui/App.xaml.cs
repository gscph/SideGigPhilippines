using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui
{
    public partial class App : Application
    {
        private readonly AppVisibility _visibility;
        private readonly ServerWarmup _warmup;

        public App(AppVisibility visibility, ErrorReporting errorReporting, ServerWarmup warmup)
        {
            _visibility = visibility;
            _warmup = warmup;
            InitializeComponent();

            // Crashes, stuck loading screens and logged errors go into the error log, sent in the background.
            errorReporting.Start();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "Tarbaho" };

            // Background work (an open chat's poll) pauses while the app is hidden.
            window.Stopped += (_, _) => _visibility.Set(false);
            window.Resumed += (_, _) =>
            {
                _visibility.Set(true);
                _warmup.WakeIfIdle(); // back after a while: the API and database may have gone cold
            };

            // Opening the app: wake the API and the database behind the splash.
            _warmup.WakeIfIdle();
            return window;
        }
    }
}
