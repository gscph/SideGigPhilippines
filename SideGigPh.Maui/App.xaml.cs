using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Maui.Services.Diagnostics;

namespace SideGigPh.Maui
{
    public partial class App : Application
    {
        private readonly AppVisibility _visibility;
        private readonly ServerWarmup _warmup;
        private readonly ErrorReporting _errorReporting;
        private readonly AnalyticsUploader _analytics;

        public App(AppVisibility visibility, ErrorReporting errorReporting, ServerWarmup warmup, AnalyticsUploader analytics)
        {
            _visibility = visibility;
            _warmup = warmup;
            _errorReporting = errorReporting;
            _analytics = analytics;
            InitializeComponent();

            // Crashes, stuck loading screens and logged errors go into the error log, sent in the background.
            errorReporting.Start();
            // Usage events kept on the phone are uploaded in the background.
            analytics.Start();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "Tarbaho" };

            // Background work (an open chat's poll) pauses while the app is hidden.
            window.Stopped += (_, _) =>
            {
                _visibility.Set(false);
                _errorReporting.AppHidden(); // a problem just had goes now, not at the next launch
                _analytics.AppHidden(); // so do the usage events waiting on the phone
            };
            window.Resumed += (_, _) =>
            {
                _visibility.Set(true);
                _analytics.AppShown(); // back after 30+ minutes: a new session
                _warmup.WakeIfIdle(); // back after a while: the API and database may have gone cold
            };

            // Opening the app: wake the API and the database behind the splash.
            _warmup.WakeIfIdle();
            return window;
        }
    }
}
